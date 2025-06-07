using Cysharp.Threading.Tasks;
using Hikari;
using System.Collections.Generic;
using System.Diagnostics;

namespace CannonCape;

public sealed class ShotSource
{
    private readonly FrameObject _sourceObj;
    private readonly SplashSource _splashSource;
    private readonly IReadOnlyCollection<ISphereCollider> _enemies;
    private readonly ISphereCollider _player;

    private const float ShotRadius = 0.114f;

    private ShotSource(FrameObject sourceObj, SplashSource splashSource, IReadOnlyCollection<ISphereCollider> enemies, ISphereCollider player)
    {
        _sourceObj = sourceObj;
        _splashSource = splashSource;
        _enemies = enemies;
        _player = player;
    }

    public static async UniTask<ShotSource> Load(IReadOnlyCollection<ISphereCollider> enemies, ISphereCollider player)
    {
        var (sourceObj, splashSource) = await UniTask.WhenAll(
            GlbLoadHelper.LoadResource("shot.glb", false),
            SplashSource.Load());
        sourceObj.Scale = new Vector3(2.3f);
        return new ShotSource(sourceObj, splashSource, enemies, player);
    }

    public void NewShot(Vector3 pos, Vector3 velocity)
    {
        _ = new Shot(_sourceObj, pos, velocity, _splashSource, _enemies);
    }

    public void NewEnemyShot(Vector3 pos, Vector3 velocity)
    {
        _ = new EnemyShot(_sourceObj, pos, velocity, _player);
    }

    private sealed class Shot
    {
        private readonly FrameObject _obj;
        private Vector3 _velocity;
        private readonly SplashSource _splashSource;
        private readonly IReadOnlyCollection<ISphereCollider> _enemies;

        private const float G = 2.1f;

        public FrameObject Obj => _obj;

        public Shot(FrameObject sourceObj, Vector3 pos, Vector3 velocity, SplashSource splashSource, IReadOnlyCollection<ISphereCollider> enemies)
        {
            var obj = sourceObj.Clone();
            obj.IsVisible = true;
            obj.Position = pos;
            _obj = obj;
            _velocity = velocity;
            _splashSource = splashSource;
            _enemies = enemies;

            obj.Update.Subscribe(_ => Update());
        }

        private void Update()
        {
            var deltaSec = App.Screen.DeltaTimeSec;
            var prevPos = _obj.Position;
            var newPos = prevPos + _velocity;
            _obj.Position = newPos;
            _velocity.Y -= deltaSec * G;

            // 前のフレームとの移動差分
            var posDiff = newPos - prevPos;

            {
                // 弾の当たり判定は前のフレームと今のフレームの弾道の円柱
                // 敵の球体の当たり判定は球
                ISphereCollider? hit = null;
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
                hit?.OnColliderHit();
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

    private sealed class EnemyShot
    {
        private readonly FrameObject _obj;
        private readonly Vector3 _velocity;
        private readonly ISphereCollider _player;

        public EnemyShot(FrameObject sourceObj, Vector3 pos, Vector3 velocity, ISphereCollider player)
        {
            var obj = sourceObj.Clone();
            obj.IsVisible = true;
            obj.Position = pos;
            _obj = obj;
            _velocity = velocity;
            _player = player;

            obj.Update.Subscribe(_ => Update());
        }

        private void Update()
        {
            var prevPos = _obj.Position;
            var newPos = prevPos + _velocity;
            _obj.Position = newPos;

            // 前のフレームとの移動差分
            var posDiff = newPos - prevPos;

            // 弾の当たり判定は前のフレームと今のフレームの弾道の円柱
            // プレイヤーの当たり判定は球
            var posDiffLen = posDiff.Length;
            var shotRadius = ShotRadius * _obj.Scale.X;
            var vec = posDiff.Normalized();

            var (center, radius) = _player.SphereCollider;
            var t = vec.Dot(center - prevPos);
            if(t >= 0 && t <= posDiffLen) {
                var nearestPosOnLine = prevPos + t * vec;
                var distanceFromLine = (center - nearestPosOnLine).Length;
                if(distanceFromLine <= radius + shotRadius) {
                    _player.OnColliderHit();
                }
            }

            // プレイヤーの後方に通りすぎたら弾を削除
            if(newPos.Z > 20) {
                _obj.Terminate();
            }
        }
    }
}
