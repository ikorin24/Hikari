#nullable enable

namespace Elffy;

public abstract class Positionable<TLayer, TVertex, TShader, TMaterial>
    : FrameObject<TLayer, TVertex, TShader, TMaterial>
    where TLayer : ObjectLayer<TLayer, TVertex, TShader, TMaterial>
    where TVertex : unmanaged, IVertex
    where TShader : Shader<TShader, TMaterial>
    where TMaterial : Material<TMaterial, TShader>
{
    private Vector3 _position;
    private Quaternion _rotation = Quaternion.Identity;
    private float _scale = 1f;

    public Vector3 Position
    {
        get => _position;
        set => _position = value;
    }

    public Quaternion Rotation
    {
        get => _rotation;
        set => _rotation = value;
    }

    public float Scale
    {
        get => _scale;
        set => _scale = value;
    }

    protected Positionable(TLayer layer) : base(layer)
    {
    }

    public Matrix4 GetModel()
    {
        // TODO: cache
        // TODO: thread safe
        return CalcModel();
    }

    private Matrix4 CalcModel()
    {
        var s = _scale;
        var scaleMat = new Matrix4(
            s, 0, 0, 0,
            0, s, 0, 0,
            0, 0, s, 0,
            0, 0, 0, 1);
        return _position.ToTranslationMatrix4() * _rotation.ToMatrix4() * scaleMat;
    }
}
