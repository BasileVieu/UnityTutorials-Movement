using UnityEngine;

public class GravityPlane : GravitySource
{
    [SerializeField] private float gravity = 9.81f;
    [SerializeField][Min(0.0f)] private float range = 1.0f;

    public override Vector3 GetGravity(Vector3 _position)
    {
        Vector3 up = transform.up;

        float distance = Vector3.Dot(up, _position - transform.position);

        if (distance > range)
        {
            return Vector3.zero;
        }

        float g = -gravity;

        if (distance > 0.0f)
        {
            g *= 1.0f - gravity / range;
        }

        return g * up;
    }

    private void OnDrawGizmos()
    {
        Vector3 scale = transform.localScale;
        scale.y = range;
        
        Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, scale);

        Vector3 size = new Vector3(1.0f, 0.0f, 1.0f);

        Gizmos.color = Color.yellow;

        Gizmos.DrawWireCube(Vector3.zero, new Vector3(1.0f, 0.0f, 1.0f));

        if (range > 0.0f)
        {
            Gizmos.color = Color.cyan;

            Gizmos.DrawWireCube(Vector3.up, size);
        }
    }
}