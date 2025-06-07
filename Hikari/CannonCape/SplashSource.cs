using Cysharp.Threading.Tasks;
using Hikari;
using System;

namespace CannonCape;

public sealed class SplashSource
{
    private readonly FrameObject _source;

    private SplashSource(FrameObject source)
    {
        _source = source;
    }

    public static async UniTask<SplashSource> Create()
    {
        var screen = App.Screen;
        var source = await UniTask.Run(() =>
        {
            var albedo = Texture2D.Create1x1Rgba8UnormSrgb(screen, TextureUsages.TextureBinding, new ColorByte(255, 255, 255, 255)).DisposeOn(screen.Closed);
            var metallicRoughness = Texture2D.Create1x1Rgba8Unorm(screen, TextureUsages.TextureBinding, new ColorByte(0, 255, 0, 0)).DisposeOn(screen.Closed);
            var normal = Texture2D.Create1x1Rgba8Unorm(screen, TextureUsages.TextureBinding, new ColorByte(127, 127, 255, 255)).DisposeOn(screen.Closed);
            var material = PbrMaterial.Create(App.PbrShader, albedo, metallicRoughness, normal).DisposeOn(screen.Closed);
            var mesh = PrimitiveShapes.Circle(screen, true).DisposeOn(screen.Closed);
            return new FrameObject(mesh, material, new FrameObjectInitArg
            {
                IsVisible = false,
                Name = "splash",
                Scale = new Vector3(1.3f, 10f, 1f),
            });
        });
        return new SplashSource(source);
    }

    public void NewSplash(Vector3 splashPos)
    {
        _ = new Splash(_source, splashPos);
    }

    private sealed class Splash
    {
        private readonly Screen _screen;
        private readonly FrameObject _obj;
        private readonly Vector3 _splashPos;
        private readonly float _splashHeight;
        private TimeSpan _elapsed;

        private const float SplashTimeSec = 1.5f;

        public Splash(FrameObject source, Vector3 splashPos)
        {
            var splashHeight = source.Scale.Y * 0.5f;
            _screen = source.Screen;
            _splashPos = splashPos;
            _splashHeight = splashHeight;
            _elapsed = TimeSpan.Zero;
            var obj = source.Clone();
            _obj = obj;
            obj.Position = new Vector3
            {
                X = splashPos.X,
                Y = CalcObjPosY(0, splashPos.Y, splashHeight),
                Z = splashPos.Z
            };
            obj.Rotation = Quaternion.FromTwoVectors(-Vector3.UnitZ, new Vector3(splashPos.X, 0, splashPos.Z).Normalized());
            obj.IsVisible = true;
            obj.Update.Subscribe(_ => Update());
        }

        private static float CalcObjPosY(float t, float splashPosY, float splashHeight)
        {
            return splashPosY - splashHeight * (1 - float.Sin(t / SplashTimeSec * float.Pi));
        }

        private void Update()
        {
            float t = (float)_elapsed.TotalSeconds;
            _obj.Position = new Vector3
            {
                X = _obj.Position.X,
                Y = CalcObjPosY(t, _splashPos.Y, _splashHeight),
                Z = _obj.Position.Z,
            };
            _elapsed += _screen.DeltaTime;
            if(_elapsed >= TimeSpan.FromSeconds(SplashTimeSec)) {
                _obj.Terminate();
            }
        }
    }
}
