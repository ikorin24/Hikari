using Cysharp.Threading.Tasks;
using Hikari;
using System.Diagnostics;

namespace CannonCape;

public sealed class Shot
{
    private readonly Screen _screen;
    private readonly FrameObject _obj;
    private Vector3 _velocity;

    private const float G = 2.1f;

    public FrameObject Obj => _obj;

    public Shot(FrameObject source, Vector3 pos, Vector3 velocity)
    {
        var obj = source.Clone();
        obj.IsVisible = true;
        obj.Position = pos;
        _screen = obj.Screen;
        _obj = obj;
        _velocity = velocity;

        obj.Update.Subscribe(_ => Update());
    }

    private void Update()
    {
        var deltaSec = _screen.DeltaTimeSec;
        _obj.Position += _velocity;
        _velocity.Y -= deltaSec * G;
        if(_obj.Position.Y < -20 || _obj.Position.Length >= 500) {
            _obj.Terminate();
        }
    }
}

public sealed class ShotSource
{
    private readonly FrameObject _source;

    public ShotSource(FrameObject source)
    {
        _source = source;
    }

    public static async UniTask<ShotSource> Create()
    {
        var source = await GlbLoadHelper.LoadResource("shot.glb", false);
        source.Scale = new Vector3(2.3f);
        return new ShotSource(source);
    }

    public Shot NewShot(Vector3 pos, Vector3 velocity)
    {
        return new Shot(_source, pos, velocity);
    }
}
