using Cysharp.Threading.Tasks;
using Hikari;
using Hikari.Mathematics;
using Hikari.UI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace CannonCape;

public sealed class MainPlayScene
{
    private readonly DisposableBag _disposables;
    private readonly MainSceneUI _ui;
    private readonly Cannon _cannon;
    private readonly BoatSource _boatSource;
    private readonly HashSet<Boat> _enemies;
    private Vector3 _cameraShakingByFire;
    private Vector3 _cameraShakingByHit;
    private bool _canFire;
    private int _gamePlayerLife;

    private const int GameTimeSec = 177;
    private const int InitialLife = 5;
    private const int InitialEnemyCount = 4;
    private const int EnemySpawnInterval = 10;

    private static readonly float _yawSpeed = 0.08f.ToRadian();
    private static readonly float _pitchSpeed = 0.08f.ToRadian();

    private bool IsGamePlayerDead => _gamePlayerLife == 0;

    private MainPlayScene(MainSceneUI ui, Cannon cannon, Ground ground, BoatSource boatSource, ShotSource shotSource, HashSet<Boat> enemies)
    {
        var disposables = new DisposableBag();
        _ui = ui;
        _cannon = cannon;
        _boatSource = boatSource;
        _enemies = enemies;
        _canFire = true;

        disposables.Add(cannon);
        disposables.Add(ground);
        disposables.Add(boatSource);
        disposables.Add(shotSource);
        _disposables = disposables;
        _gamePlayerLife = InitialLife;
    }

    public static async UniTask<ScenarioState> Run()
    {
        var ui = CreateUI();
        MainPlayScene scene;
        try {
            scene = await LoadScene(ui);
        }
        finally {
            await FadeHelper.FadeIn(ui.UIOverlay);
        }
        try {
            await scene.RunMainGame();
            await FadeHelper.FadeOut(ui.UIOverlay, 2f);
            return ScenarioState.Home;
        }
        finally {
            scene.EndScene();
        }
    }

    private void EndScene()
    {
        _disposables.Dispose();
        foreach(var enemy in _enemies) {
            enemy.Dispose();
        }
        _enemies.Clear();
    }

    private void AdjustCamera()
    {
        var cannonObj = _cannon.Obj;
        var cannonBackward = cannonObj.Rotation * Vector3.UnitZ;
        var camPos = cannonObj.Position + cannonBackward * 7f + new Vector3(0, 3f, 0);
        var target = cannonObj.Position + new Vector3(0, 1.9f, 0) + _cameraShakingByFire + _cameraShakingByHit;
        App.Camera.LookAt(target, camPos);
    }

    private static MainSceneUI CreateUI()
    {
        UIElement uiOverlay;
        Label lifeLabel;
        Label pitchLabel;
        Label resultLabel;
        Panel timeBar;
        App.Screen.UITree.SetRoot(new Panel
        {
            Name = "root",
            Background = Brush.Transparent,
            Children =
            [
                new Panel
                {
                    Background = Brush.Transparent,
                    Color = Color4.White,
                    Height = LayoutLength.Length(40),
                    VerticalAlignment = VerticalAlignment.Top,
                    Flow = new Flow(FlowDirection.Row),
                    Children =
                    [
                        new Label
                        {
                            Text = "残り時間 持ちこたえろ！",
                            Width = LayoutLength.Proportion(0.3f),
                            Color = Color4.FromHexCode("#FF7200"),
                            Background = Brush.Transparent,
                            FontSize = 25,
                            Typeface = Typeface.FromFile(Resources.Path("07にくまるフォント.otf")),
                        },
                        timeBar = new Panel
                        {
                            Height = LayoutLength.Length(24),
                            Width = LayoutLength.Proportion(0.6f),
                            BorderRadius = new CornerRadius(12),
                            BorderWidth = new Thickness(5),
                            BorderColor = Brush.Solid(Color4.FromHexCode("#FF8A38")),
                            Background = Brush.Solid(Color4.FromHexCode("#FFAB6B")),
                        },
                    ],
                },
                resultLabel = new Label
                {
                    Background = Brush.Transparent,
                    Color = Color4.White,
                    FontSize = 90,
                    Typeface = Typeface.FromFile(Resources.Path("07にくまるフォント.otf")),
                },
                new Panel
                {
                    Background = Brush.LinearGradient(0,
                    [
                        new(Color4.FromHexCode("#000B"), 0.8f),
                        new(Color4.FromHexCode("#0005"), 0.9f),
                        new(Color4.FromHexCode("#0000"), 1f),
                    ]),
                    VerticalAlignment = VerticalAlignment.Bottom,
                    Height = LayoutLength.Length(110),
                    Flow = new Flow(FlowDirection.Column),
                    Children =
                    [
                        new Label
                        {
                            Height = LayoutLength.Length(60),
                            Flow = new Flow(FlowDirection.Row),
                            Background = Brush.Transparent,
                            Children =
                            [
                                new Panel
                                {
                                    Background = Brush.Transparent,
                                    Width = LayoutLength.Proportion(0.5f),
                                    Children =
                                    [
                                        lifeLabel = new Label
                                        {
                                            Background = Brush.Transparent,
                                            Color = Color4.White,
                                            FontSize = 30,
                                            Typeface = Typeface.FromFile(Resources.Path("07にくまるフォント.otf")),
                                        },
                                    ],
                                },
                                new Panel
                                {
                                    Background = Brush.Transparent,
                                    Children =
                                    [
                                        pitchLabel = new Label
                                        {
                                            Background = Brush.Transparent,
                                            Color = Color4.White,
                                            FontSize = 30,
                                            Typeface = Typeface.FromFile(Resources.Path("07にくまるフォント.otf")),
                                        },
                                    ],
                                },
                            ],
                        },
                        new Label
                        {
                            Height = LayoutLength.Length(50),
                            Flow = new Flow(FlowDirection.Row),
                            Background = Brush.Transparent,
                            Color = Color4.White,
                            FontSize = 22,
                            Typeface = Typeface.FromFile(Resources.Path("07にくまるフォント.otf")),
                            Text = "砲台回転 ← / →   砲身角度 ↑ / ↓   発射 Space",
                        },
                    ],
                },
                uiOverlay = new Panel
                {
                    Name = "uiOverlay",
                    Background = Brush.Black,
                },
            ],
        });
        return new MainSceneUI
        {
            ResultLabel = resultLabel,
            PitchLabel = pitchLabel,
            LifeLabel = lifeLabel,
            TimeBar = timeBar,
            UIOverlay = uiOverlay,
        };
    }

    private static async UniTask<MainPlayScene> LoadScene(MainSceneUI ui)
    {
        var ground = await Ground.Load();
        var enemies = new HashSet<Boat>();
        var shotSource = await ShotSource.Load(enemies, ground);
        var (cannon, boatSource) = await UniTask.WhenAll(Cannon.Load(shotSource), BoatSource.Load(shotSource));
        ground.Obj.Position = new Vector3(0, 5f, 0);
        cannon.Obj.Position = new Vector3(0, 5f, 0);
        ui.PitchLabel.Text = $"角度: {cannon.CurrentPitch.ToDegree():F1}°";
        cannon.PitchChanged += newPitch =>
        {
            ui.PitchLabel.Text = $"角度: {newPitch.ToDegree():F1}°";
        };
        var scene = new MainPlayScene(ui, cannon, ground, boatSource, shotSource, enemies);
        scene.SetLifeUILabel();
        scene.AdjustCamera();
        for(int i = 0; i < InitialEnemyCount; i++) {
            scene.SpawnEnemyBoat(true);
        }
        ground.EnemyShotHit += () =>
        {
            scene.OnEnemyShotHit();
        };
        return scene;
    }

    private async UniTask RunMainGame()
    {
        using var player = AudioPlayer.Play(Resources.Path("メインゲームBGM.wav"));
        var sw = Stopwatch.StartNew();
        using var control = App.Screen.Update.Subscribe(_ =>
        {
            var elapsedSec = (float)sw.Elapsed.TotalSeconds;
            var remainingTimeRatio = ((float)GameTimeSec - elapsedSec) / (float)GameTimeSec;
            _ui.SetRemainingTimeRatio(remainingTimeRatio);
            ControlMainGame();
        });

        // メインゲームの終了条件は二つ
        // 1. プレイヤーが既定の時間生き残る
        // 2. プレイヤーのライフが0になる
        var gameClear = false;
        var cts = new CancellationTokenSource();

        // プレイヤーのライフを毎フレーム監視して0になっていたらメインゲームを終了
        App.Screen.Update.Subscribe(_ =>
        {
            if(IsGamePlayerDead) {
                cts.Cancel();
            }
        });
        await UniTask.WhenAll(
            UniTask.Create(async () =>
            {
                // 既定の時間後にゲームクリアフラグを立ててメインゲームを終了
                try {
                    await App.Screen.Update.Delay(TimeSpan.FromSeconds(GameTimeSec), cts.Token);
                }
                catch(OperationCanceledException) {
                    return;
                }
                gameClear = true;
                cts.Cancel();
            }),
            UniTask.Create(async () =>
            {
                // メインゲームが継続している間、一定時間ごとに敵をスポーンする
                while(true) {
                    try {
                        await App.Screen.Update.Delay(TimeSpan.FromSeconds(EnemySpawnInterval), cts.Token);
                    }
                    catch(OperationCanceledException) {
                        return;
                    }
                    if(gameClear || IsGamePlayerDead) {
                        return;
                    }
                    SpawnEnemyBoat(true);
                }
            }));

        _canFire = false;
        if(gameClear) {
            foreach(var enemy in _enemies) {
                enemy.Destroy(false);
            }

            _ui.ResultLabel.Text = "Game Clear!!!";
            player.Dispose();
            await AudioPlayer.PlayAwaitable(Resources.Path("ゲームクリア.wav"));
            await App.Screen.Update.Delay(TimeSpan.FromSeconds(3));
        }
        else {
            _ui.ResultLabel.Text = "Game Over";
            await App.Screen.Update.Delay(TimeSpan.FromSeconds(5));
        }
    }

    private void SetLifeUILabel()
    {
        _ui.LifeLabel.Text = $"ライフ: {new string('♡', _gamePlayerLife)}";
    }

    private void OnEnemyShotHit()
    {
        AudioPlayer.Play(Resources.Path("プレイヤー被弾.wav"));
        _gamePlayerLife = int.Max(_gamePlayerLife - 1, 0);
        SetLifeUILabel();
        Debug.WriteLine($"プレイヤー被弾, (残りライフ: {_gamePlayerLife})");
        UniTask.Void(async () =>
        {
            const int FrameCount = 60;
            try {
                for(int i = 0; i < FrameCount; i++) {
                    _cameraShakingByHit = App.Camera.Rotation * new Vector3()
                    {
                        X = (1f - (float)i / FrameCount) * 0.06f * float.Sin(i / 3f * float.Pi),
                        Y = (1f - (float)i / FrameCount) * 0.1f * float.Sin(i / 2f * float.Pi),
                    };
                    await App.Screen.Update.Switch();
                }
            }
            finally {
                _cameraShakingByHit = Vector3.Zero;
            }
        });
    }

    private void SpawnEnemyBoat(bool useSpawnAnimation)
    {
        const float AngleRangeDegree = 45f;
        const float SpawnNear = 150f;
        const float SpawnFar = 250f;
        const float DestNear = 70f;
        const float DestFar = 100f;


        var rand = Random.Shared;
        var theta = (rand.NextSingle() - 0.5f) * AngleRangeDegree.ToRadian();
        var distance = rand.NextSingle() * (SpawnFar - SpawnNear) + SpawnNear;

        var pos = new Vector3
        {
            X = distance * float.Sin(theta),
            Y = 0,
            Z = -distance * float.Cos(theta),
        };

        var theta2 = (rand.NextSingle() - 0.5f) * AngleRangeDegree.ToRadian();
        var distance2 = rand.NextSingle() * (DestFar - DestNear) + DestNear;
        var dest = new Vector3
        {
            X = distance2 * float.Sin(theta2),
            Y = 0,
            Z = -distance2 * float.Cos(theta2),
        };
        var enemy = _boatSource.NewBoat(pos, dest, useSpawnAnimation);
        enemy.OnDestroy += enemy => _enemies.Remove(enemy);
        _enemies.Add(enemy);
    }

    private void ControlMainGame()
    {
        var cannon = _cannon;
        var input = App.Input;
        var changed = false;
        if(input.IsArrowLeftPressed()) {
            cannon.RotateYaw(_yawSpeed);
            changed = true;
        }
        if(input.IsArrowRightPressed()) {
            cannon.RotateYaw(-_yawSpeed);
            changed = true;
        }
        if(input.IsArrowUpPressed()) {
            cannon.RotatePitch(-_pitchSpeed);
            changed = true;
        }
        if(input.IsArrowDownPressed()) {
            cannon.RotatePitch(_pitchSpeed);
            changed = true;
        }
        if(_canFire && input.IsOkDown()) {
            cannon.Fire();
            _canFire = false;
            AudioPlayer.Play(Resources.Path("大砲発射.wav"));
            UniTask.Void(async () =>
            {
                const int FrameCount = 25;
                const float FrozenTime = 1f;
                try {
                    for(int i = 0; i < FrameCount; i++) {
                        _cameraShakingByFire = App.Camera.Rotation * new Vector3()
                        {
                            X = (1f - (float)i / FrameCount) * 0.02f * float.Sin(i / 5f * float.Pi),
                            Y = (1f - (float)i / FrameCount) * 0.01f * float.Sin(i / 4f * float.Pi),
                        };
                        await App.Screen.Update.Switch();
                    }
                    _cameraShakingByFire = Vector3.Zero;
                    await App.Screen.Update.Delay(TimeSpan.FromSeconds(FrozenTime));
                }
                finally {
                    _cameraShakingByFire = Vector3.Zero;
                    _canFire = true;
                }
            });
        }
        AdjustCamera();
        if(changed) {
            //Debug.WriteLine($"pitch: {cannon.CurrentPitch.ToDegree()}");
        }
    }

    private record MainSceneUI
    {
        public required Label ResultLabel { get; set; }
        public required Label LifeLabel { get; init; }
        public required Label PitchLabel { get; init; }
        public required Panel TimeBar { get; init; }
        public required UIElement UIOverlay { get; init; }

        public void SetRemainingTimeRatio(float ratio)
        {
            TimeBar.Background = Brush.LinearGradient(90.ToRadian(),
            [
                new(Color4.FromHexCode("#FFAB6B"), ratio),
                new(Color4.FromHexCode("#555"), ratio),
            ]);
        }
    }
}
