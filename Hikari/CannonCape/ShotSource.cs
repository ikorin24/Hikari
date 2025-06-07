using Cysharp.Threading.Tasks;
using Hikari;
using System.Diagnostics;

namespace CannonCape;

public sealed class ShotSource
{
    private readonly FrameObject _sourceObj;
    private readonly SplashSource _splashSource;

    public ShotSource(FrameObject sourceObj, SplashSource splashSource)
    {
        _sourceObj = sourceObj;
        _splashSource = splashSource;
    }

    public static async UniTask<ShotSource> Create()
    {
        var (sourceObj, splashSource) = await UniTask.WhenAll(
            GlbLoadHelper.LoadResource("shot.glb", false),
            SplashSource.Create());
        sourceObj.Scale = new Vector3(2.3f);
        return new ShotSource(sourceObj, splashSource);
    }

    public void NewShot(Vector3 pos, Vector3 velocity)
    {
        _ = new Shot(_sourceObj, pos, velocity, _splashSource);
    }

    private sealed class Shot
    {
        private readonly Screen _screen;
        private readonly FrameObject _obj;
        private Vector3 _velocity;
        private readonly SplashSource _splashSource;

        private const float G = 2.1f;

        public FrameObject Obj => _obj;

        public Shot(FrameObject sourceObj, Vector3 pos, Vector3 velocity, SplashSource splashSource)
        {
            var obj = sourceObj.Clone();
            obj.IsVisible = true;
            obj.Position = pos;
            _screen = obj.Screen;
            _obj = obj;
            _velocity = velocity;
            _splashSource = splashSource;

            obj.Update.Subscribe(_ => Update());
        }

        private void Update()
        {
            var deltaSec = _screen.DeltaTimeSec;
            var prevPos = _obj.Position;
            var newPos = prevPos + _velocity;
            _obj.Position = newPos;
            _velocity.Y -= deltaSec * G;

            if(newPos.Y < 0) {
                // 移動前と移動後の位置を直線で結び、海面 (y = 0) との交点を着水点とする
                var vec = newPos - prevPos;
                var t = -prevPos.Y / vec.Y;
                var splashPos = prevPos + t * vec;
                _splashSource.NewSplash(splashPos);
                _obj.Terminate();
            }
        }
    }
}
