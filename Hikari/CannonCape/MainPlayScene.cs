using Cysharp.Threading.Tasks;
using Hikari;
using Hikari.Mathematics;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace CannonCape;

public sealed class MainPlayScene
{
    private readonly Scenario _scenario;
    private readonly Cannon _cannon;
    private readonly Ground _ground;
    private readonly BoatSource _boatSource;
    private readonly HashSet<Boat> _enemies;
    private Vector3 _cameraShakingByFire;
    private Vector3 _cameraShakingByHit;
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
        var target = cannonObj.Position + new Vector3(0, 1.9f, 0) + _cameraShakingByFire + _cameraShakingByHit;
        App.Camera.LookAt(target, camPos);
    }

    private static async UniTask<MainPlayScene> LoadScene(Scenario scenario)
    {
        var ground = await Ground.Load();
        var enemies = new HashSet<Boat>();
        var shotSource = await ShotSource.Load(enemies, ground);
        var (cannon, boatSource) = await UniTask.WhenAll(Cannon.Load(shotSource), BoatSource.Load(shotSource));
        ground.Obj.Position = new Vector3(0, 5f, 0);
        cannon.Obj.Position = new Vector3(0, 5f, 0);
        var scene = new MainPlayScene(scenario, cannon, ground, boatSource, enemies);
        scene.AdjustCamera();
        for(int i = 0; i < 5; i++) {
            scene.SpawnEnemyBoat(false);
        }
        ground.EnemyShotHit += () =>
        {
            scene.OnEnemyShotHit();
        };
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

    private void OnEnemyShotHit()
    {
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
}
