using UnityEngine;

public class GravityBox : GravitySource
{
    [SerializeField] private float gravity = 9.81f;
    [SerializeField] private Vector3 boundaryDistance = Vector3.one;
    [SerializeField][Min(0.0f)] private float innerDistance = 0.0f;
    [SerializeField][Min(0.0f)] private float innerFalloffDistance = 0.0f;
    [SerializeField][Min(0.0f)] private float outerDistance = 0.0f;
    [SerializeField][Min(0.0f)] private float outerFalloffDistance = 0.0f;

    private float innerFalloffFactor;
    private float outerFalloffFactor;

    private void OnValidate()
    {
        boundaryDistance = Vector3.Max(boundaryDistance, Vector3.zero);

        float maxInner = Mathf.Min(Mathf.Min(boundaryDistance.x, boundaryDistance.y), boundaryDistance.z);

        innerDistance = Mathf.Min(innerDistance, maxInner);
        innerFalloffDistance = Mathf.Max(Mathf.Min(innerFalloffDistance, maxInner), innerDistance);

        outerFalloffDistance = Mathf.Max(outerFalloffDistance, outerDistance);

        innerFalloffFactor = 1.0f / (innerFalloffDistance - innerDistance);
        outerFalloffFactor = 1.0f / (outerFalloffDistance - outerDistance);
    }

    private void Awake()
    {
        OnValidate();
    }

    public override Vector3 GetGravity(Vector3 _position)
    {
        _position -= transform.InverseTransformDirection(_position - transform.position);

        Vector3 vector = Vector3.zero;

        int outside = 0;

        if (_position.x > boundaryDistance.x)
        {
            vector.x = boundaryDistance.x - _position.x;

            outside = 1;
        }
        else if (_position.x < -boundaryDistance.x)
        {
            vector.x = -boundaryDistance.x - _position.x;

            outside = 1;
        }

        if (_position.y > boundaryDistance.y)
        {
            vector.y = boundaryDistance.y - _position.y;

            outside += 1;
        }
        else if (_position.y < -boundaryDistance.y)
        {
            vector.y = -boundaryDistance.y - _position.y;

            outside += 1;
        }

        if (_position.z > boundaryDistance.z)
        {
            vector.z = boundaryDistance.z - _position.z;

            outside += 1;
        }
        else if (_position.z < -boundaryDistance.z)
        {
            vector.z = -boundaryDistance.z - _position.z;

            outside += 1;
        }

        if (outside > 0)
        {
            float distance = outside == 1 ? Mathf.Abs(vector.x + vector.y + vector.z) : vector.magnitude;

            if (distance > outerFalloffDistance)
            {
                return Vector3.zero;
            }

            float g = gravity / distance;

            if (distance > outerDistance)
            {
                g *= 1.0f - (distance - outerDistance) * outerFalloffFactor;
            }

            return transform.TransformDirection(g * vector);
        }

        Vector3 distances;
        distances.x = boundaryDistance.x - Mathf.Abs(_position.x);
        distances.y = boundaryDistance.y - Mathf.Abs(_position.y);
        distances.z = boundaryDistance.z - Mathf.Abs(_position.z);

        if (distances.x < distances.z)
        {
            if (distances.x < distances.z)
            {
                vector.x = GetGravityComponent(_position.x, distances.x);
            }
            else
            {
                vector.z = GetGravityComponent(_position.z, distances.z);
            }
        }
        else if (distances.y < distances.z)
        {
            vector.y = GetGravityComponent(_position.y, distances.y);
        }
        else
        {
            vector.z = GetGravityComponent(_position.z, distances.z);
        }

        return transform.TransformDirection(vector);
    }

    private float GetGravityComponent(float _coordinate, float _distance)
    {
        if (_distance > innerFalloffDistance)
        {
            return 0.0f;
        }
        
        float g = gravity;

        if (_distance > innerDistance)
        {
            g *= 1.0f - (_distance - innerDistance) * innerFalloffFactor;
        }

        return _coordinate > 0.0f ? -g : g;
    }

    private void OnDrawGizmos()
    {
        Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);

        Vector3 size;

        if (innerFalloffDistance > innerDistance)
        {
            Gizmos.color = Color.cyan;

            size.x = 2.0f * (boundaryDistance.x - innerFalloffDistance);
            size.y = 2.0f * (boundaryDistance.y - innerFalloffDistance);
            size.z = 2.0f * (boundaryDistance.z - innerFalloffDistance);

            Gizmos.DrawWireCube(Vector3.zero, size);
        }

        if (innerDistance > 0.0f)
        {
            Gizmos.color = Color.yellow;

            size.x = 2.0f * (boundaryDistance.x - innerDistance);
            size.y = 2.0f * (boundaryDistance.y - innerDistance);
            size.z = 2.0f * (boundaryDistance.z - innerDistance);

            Gizmos.DrawWireCube(Vector3.zero, size);
        }

        Gizmos.color = Color.red;

        Gizmos.DrawWireCube(Vector3.zero, 2.0f * boundaryDistance);

        if (outerDistance > 0.0f)
        {
            Gizmos.color = Color.yellow;

            DrawGizmosOuterCube(outerDistance);
        }

        if (outerFalloffDistance > outerDistance)
        {
            Gizmos.color = Color.cyan;

            DrawGizmosOuterCube(outerFalloffDistance);
        }
    }

    private void DrawGizmosRect(Vector3 _a, Vector3 _b, Vector3 _c, Vector3 _d)
    {
        Gizmos.DrawLine(_a, _b);
        Gizmos.DrawLine(_b, _c);
        Gizmos.DrawLine(_c, _d);
        Gizmos.DrawLine(_d, _a);
    }

    private void DrawGizmosOuterCube(float _distance)
    {
        Vector3 a;
        Vector3 b;
        Vector3 c;
        Vector3 d;

        a.y = b.y = boundaryDistance.y;
        d.y = c.y = -boundaryDistance.y;
        b.z = c.z = boundaryDistance.z;
        d.z = a.z = -boundaryDistance.z;
        a.x = b.x = c.x = d.x = boundaryDistance.x + _distance;

        DrawGizmosRect(a, b, c, d);

        a.x = b.x = c.x = d.x = -a.x;

        DrawGizmosRect(a, b, c, d);

        a.x = d.x = boundaryDistance.x;
        b.x = c.x = -boundaryDistance.x;
        a.z = b.z = boundaryDistance.z;
        c.z = d.z = -boundaryDistance.z;
        a.y = b.y = c.y = d.y = boundaryDistance.y + _distance;

        DrawGizmosRect(a, b, c, d);

        a.y = b.y = c.y = d.y = -a.y;

        DrawGizmosRect(a, b, c, d);

        a.x = d.x = boundaryDistance.x;
        b.x = c.x = -boundaryDistance.x;
        a.y = b.y = boundaryDistance.y;
        c.y = d.y = -boundaryDistance.y;
        a.z = b.z = c.z = d.z = boundaryDistance.z + _distance;

        DrawGizmosRect(a, b, c, d);

        a.z = b.z = c.z = d.z = -a.z;

        DrawGizmosRect(a, b, c, d);

        _distance *= 0.5773502692f;

        Vector3 size = boundaryDistance;
        size.x = 2.0f * (size.x + _distance);
        size.y = 2.0f * (size.y + _distance);
        size.z = 2.0f * (size.z + _distance);

        Gizmos.DrawWireCube(Vector3.zero, size);
    }
}