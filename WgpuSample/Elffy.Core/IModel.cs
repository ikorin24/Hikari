#nullable enable

namespace Elffy;

public interface IModel<TScale> where TScale : unmanaged
{
    Vector3 Position { get; set; }
    Quaternion Rotation { get; set; }
    TScale Scale { get; set; }
    Matrix4 GetModel();
}
