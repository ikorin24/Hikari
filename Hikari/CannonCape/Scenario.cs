using Cysharp.Threading.Tasks;
using Hikari;
using Hikari.Mathematics;
using Hikari.UI;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace CannonCape;

public sealed class Scenario
{
    private readonly UIElement _uiOverlay;
    private readonly UIElement _gameUIRoot;

    private enum ScenarioState
    {
        Home,
        Play,
        Quit,
    }

    public Scenario()
    {
        App.Camera.LookAt(new Vector3(0, 0, -100), new Vector3(0, 10, 0));
        CreateSea();
        CreateSky();
        App.Screen.UITree.SetRoot(new Panel
        {
            Background = Brush.Transparent,
            Children =
            [
                _gameUIRoot = new Panel
                {
                    Background = Brush.Transparent,
                },
                _uiOverlay = new Panel
                {
                    Background = Brush.Transparent,
                },
            ],
        });
    }

    public async UniTask Start()
    {
        var state = ScenarioState.Play;
        while(true) {
            switch(state) {
                default:
                case ScenarioState.Home: {
                    state = await HomeScene();
                    continue;
                }
                case ScenarioState.Play:
                    await MainPlayScene.Start(this);
                    break;
                case ScenarioState.Quit: {
                    return;
                }
            }
        }
    }

    public async UniTask FadeIn(float seconds = 0.6f)
    {
        var span = TimeSpan.FromSeconds(seconds);
        var start = App.CurrentTime;
        while(true) {
            var elapsed = App.CurrentTime - start;
            if(elapsed >= span) {
                _uiOverlay.Background = Brush.Transparent;
                return;
            }
            var alpha = (float)elapsed.Ticks / span.Ticks;
            _uiOverlay.Background = Brush.Solid(0, 0, 0, 1f - alpha);
            await App.Screen.Update.Switch();
        }
    }

    public async UniTask FadeOut(float seconds = 0.6f)
    {
        var span = TimeSpan.FromSeconds(seconds);
        var start = App.CurrentTime;
        while(true) {
            var elapsed = App.CurrentTime - start;
            if(elapsed >= span) {
                _uiOverlay.Background = Brush.Black;
                return;
            }
            var alpha = (float)elapsed.Ticks / span.Ticks;
            _uiOverlay.Background = Brush.Solid(0, 0, 0, alpha);
            await App.Screen.Update.Switch();
        }
    }

    private async UniTask<ScenarioState> HomeScene()
    {
        var tcs = new UniTaskCompletionSource<ScenarioState>();
        var ui = HomeUI(state => tcs.TrySetResult(state));
        _gameUIRoot.Children.Add(ui);
        var nextState = await tcs.Task;
        ui.Remove();
        await FadeOut();
        return nextState;
    }

    private static void CreateSky()
    {
        var screen = App.Screen;
        var shader = SkyShader.Create(screen).DisposeOn(screen.Closed);
        var material = SkyMaterial.Create(shader).DisposeOn(screen.Closed);
        var mesh = PrimitiveShapes.SkySphere(screen, false).DisposeOn(screen.Closed);
        var sky = new FrameObject(mesh, material)
        {
            Name = "sky",
            Scale = new Vector3(1200),
        };
    }

    private static void CreateSea()
    {
        var screen = App.Screen;
        var albedo = Texture2D.Create1x1Rgba8UnormSrgb(screen, TextureUsages.TextureBinding, new ColorByte(45, 55, 110, 255)).DisposeOn(screen.Closed);
        var metallicRoughness = Texture2D.Create1x1Rgba8Unorm(screen, TextureUsages.TextureBinding, new ColorByte(0, 127, 0, 0)).DisposeOn(screen.Closed);
        var normal = Texture2D.Create1x1Rgba8Unorm(screen, TextureUsages.TextureBinding, new ColorByte(127, 127, 255, 255)).DisposeOn(screen.Closed);
        var material = PbrMaterial.Create(App.PbrShader, albedo, metallicRoughness, normal).DisposeOn(screen.Closed);
        var mesh = PrimitiveShapes.Plane(screen, true).DisposeOn(screen.Closed);
        var sea = new FrameObject(mesh, material)
        {
            Name = "sea",
            Rotation = Quaternion.FromAxisAngle(Vector3.UnitX, -90.ToRadian()),
            Scale = new Vector3(1200),
        };
    }

    private static Panel HomeUI(Action<ScenarioState> onStateChanged)
    {
        Panel homeUI = null!;
        homeUI = new Panel
        {
            Name = "HomeUI",
            Background = Brush.Transparent,
            Width = LayoutLength.Proportion(0.6f),
            Height = LayoutLength.Length(360),
            Flow = new Flow(FlowDirection.Column, FlowWrapMode.NoWrap),
            Children =
            [
                HomeUIButton(b =>
                {
                    b.Name = "ゲーム開始";
                    b.Text = "ゲーム開始";
                    b.Background = Brush.Red;
                    b.Clicked.Subscribe(_ =>
                    {
                        onStateChanged.Invoke(ScenarioState.Play);
                        return;
                    });
                }),
                HomeUIButton(b =>
                {
                    b.Text = "遊び方";
                    b.Background = Brush.Green;
                    b.Clicked.Subscribe(_ =>
                    {
                    });
                }),
                HomeUIButton(b =>
                {
                    b.Text = "ゲーム終了";
                    b.Background = Brush.Blue;
                    b.Clicked.Subscribe(_ =>
                    {
                        onStateChanged.Invoke(ScenarioState.Quit);
                        App.Screen.RequestClose();
                    });
                }),
            ],
        };
        return homeUI;
    }

    private static Button HomeUIButton(Action<Button>? modify = null)
    {
        var button = new Button
        {
            Typeface = Typeface.FromFile(Resources.Path("mplus-1p-regular.otf")),
            FontSize = 30,
            Width = LayoutLength.Length(600),
            Height = LayoutLength.Length(100),
            Margin = new Thickness(10),
            BorderRadius = new CornerRadius(20f),
            BorderWidth = new Thickness(2),
            BorderColor = Brush.White,
            Background = Brush.Red,
            HoverProps = new()
            {
            },
        };
        modify?.Invoke(button);
        return button;
    }
}

public sealed class MainPlayScene
{
    private readonly Scenario _scenario;
    private readonly Cannon _cannon;
    private readonly Ground _ground;
    private readonly BoatSource _boatSource;
    private readonly HashSet<Boat> _enemies;
    private Vector3 _cameraShaking;
    private bool _canFire;

    private static readonly float _yawSpeed = 0.08f.ToRadian();
    private static readonly float _pitchSpeed = 0.08f.ToRadian();

    private MainPlayScene(Scenario scenario, Cannon cannon, Ground ground, BoatSource boatSource, HashSet<Boat> enemies)
    {
        _scenario = scenario;
        _cannon = cannon;
        _ground = ground;
        _boatSource = boatSource;
        _enemies = enemies;
        _canFire = true;
    }

    public static async UniTask Start(Scenario scenario)
    {
        MainPlayScene scene;
        try {
            scene = await LoadScene(scenario);
        }
        finally {
            await scenario.FadeIn();
        }
        await scene.Run();
    }

    private void AdjustCamera()
    {
        var cannonObj = _cannon.Obj;
        var cannonBackward = cannonObj.Rotation * Vector3.UnitZ;
        var camPos = cannonObj.Position + cannonBackward * 7f + new Vector3(0, 3f, 0);
        var target = cannonObj.Position + new Vector3(0, 1.9f, 0) + _cameraShaking;
        App.Camera.LookAt(target, camPos);
    }

    private static async UniTask<MainPlayScene> LoadScene(Scenario scenario)
    {
        var enemies = new HashSet<Boat>();
        var (cannon, ground, boatSource) = await UniTask.WhenAll(Cannon.Load(enemies), Ground.Load(), BoatSource.Create());
        ground.Obj.Position = new Vector3(0, 5f, 0);
        cannon.Obj.Position = new Vector3(0, 5f, 0);
        var scene = new MainPlayScene(scenario, cannon, ground, boatSource, enemies);
        scene.AdjustCamera();
        for(int i = 0; i < 5; i++) {
            scene.SpawnEnemyBoat(false);
        }
        return scene;
    }

    private async UniTask Run()
    {
        _cannon.Obj.Update.Subscribe(_ => Update());

        while(true) {
            await App.Screen.Update.Delay(TimeSpan.FromSeconds(10));
            SpawnEnemyBoat(true);
        }

        // TODO:
        await UniTask.Never(default);
    }

    private void SpawnEnemyBoat(bool useSpawnAnimation)
    {
        const float AngleRangeDegree = 50f;
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

    private void Update()
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
            UniTask.Void(async () =>
            {
                try {
                    const int F = 25;
                    for(int i = 0; i < F; i++) {
                        _cameraShaking = App.Camera.Rotation * new Vector3()
                        {
                            X = (1f - (float)i / F) * 0.02f * float.Sin(i / 5f * float.Pi),
                            Y = (1f - (float)i / F) * 0.01f * float.Sin(i / 4f * float.Pi),
                        };
                        await App.Screen.Update.Switch();
                    }
                    _cameraShaking = Vector3.Zero;
                    await App.Screen.Update.Delay(TimeSpan.FromSeconds(1f));
                }
                finally {
                    _cameraShaking = Vector3.Zero;
                    _canFire = true;
                }
            });
        }
        AdjustCamera();
        if(changed) {
            //Debug.WriteLine($"pitch: {cannon.CurrentPitch.ToDegree()}");
        }
    }
}
