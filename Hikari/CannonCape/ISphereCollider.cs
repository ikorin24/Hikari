using Hikari;

namespace CannonCape;

public interface ISphereCollider
{
    (Vector3 Center, float Radius) SphereCollider { get; }

    void OnColliderHit();
}
