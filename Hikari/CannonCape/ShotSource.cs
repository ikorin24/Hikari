using Cysharp.Threading.Tasks;
using Hikari;
using System.Collections.Generic;
using System.Diagnostics;

namespace CannonCape;

public sealed class ShotSource
{
    private readonly FrameObject _sourceObj;
    private readonly SplashSource _splashSource;
    private readonly IReadOnlyCollection<Boat> _enemies;

    private const float ShotRadius = 0.114f;

    public ShotSource(FrameObject sourceObj, SplashSource splashSource, IReadOnlyCollection<Boat> enemies)
    {
        _sourceObj = sourceObj;
        _splashSource = splashSource;
        _enemies = enemies;
    }

    public static async UniTask<ShotSource> Create(IReadOnlyCollection<Boat> enemies)
    {
        var (sourceObj, splashSource) = await UniTask.WhenAll(
            GlbLoadHelper.LoadResource("shot.glb", false),
            SplashSource.Create());
        sourceObj.Scale = new Vector3(2.3f);
        return new ShotSource(sourceObj, splashSource, enemies);
    }

    public void NewShot(Vector3 pos, Vector3 velocity)
    {
        _ = new Shot(_sourceObj, pos, velocity, _splashSource, _enemies);
    }

    private sealed class Shot
    {
        private readonly Screen _screen;
        private readonly FrameObject _obj;
        private Vector3 _velocity;
        private readonly SplashSource _splashSource;
        private readonly IReadOnlyCollection<Boat> _enemies;

        private const float G = 2.1f;

        public FrameObject Obj => _obj;

        public Shot(FrameObject sourceObj, Vector3 pos, Vector3 velocity, SplashSource splashSource, IReadOnlyCollection<Boat> enemies)
        {
            var obj = sourceObj.Clone();
            obj.IsVisible = true;
            obj.Position = pos;
            _screen = obj.Screen;
            _obj = obj;
            _velocity = velocity;
            _splashSource = splashSource;
            _enemies = enemies;

            obj.Update.Subscribe(_ => Update());
        }

        private void Update()
        {
            var deltaSec = _screen.DeltaTimeSec;
            var prevPos = _obj.Position;
            var newPos = prevPos + _velocity;
            _obj.Position = newPos;
            _velocity.Y -= deltaSec * G;

            // 前のフレームとの移動差分
            var posDiff = newPos - prevPos;

            {
                // 弾の当たり判定は前のフレームと今のフレームの弾道の円柱
                // 敵の球体の当たり判定は球
                Boat? hit = null;
                var posDiffLen = posDiff.Length;
                var shotRadius = ShotRadius * _obj.Scale.X;
                var vec = posDiff.Normalized();
                foreach(var enemy in _enemies) {
                    var (center, radius) = enemy.SphereCollider;
                    var t = vec.Dot(center - prevPos);
                    if(t >= 0 && t <= posDiffLen) {
                        var nearestPosOnLine = prevPos + t * vec;
                        var distanceFromLine = (center - nearestPosOnLine).Length;
                        if(distanceFromLine <= radius + shotRadius) {
                            hit = enemy;
                            break;
                        }
                    }
                }
                hit?.Destroy();
            }

            // 着水点に水柱を出して、弾は削除する
            if(newPos.Y < 0) {
                // 移動前と移動後の位置を直線で結び、海面 (y = 0) との交点を着水点とする
                var t = -prevPos.Y / posDiff.Y;
                var splashPos = prevPos + t * posDiff;
                _splashSource.NewSplash(splashPos);
                _obj.Terminate();
            }
        }
    }
}
