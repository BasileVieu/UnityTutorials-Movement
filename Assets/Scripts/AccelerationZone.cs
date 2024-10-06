using UnityEngine;

public class AccelerationZone : MonoBehaviour
{
    [SerializeField][Min(0.0f)] private float acceleration = 10.0f;
    [SerializeField][Min(0.0f)] private float speed = 10.0f;

    private void Accelerate(Rigidbody _body)
    {
        Vector3 velocity = transform.InverseTransformDirection(_body.velocity);

        if (velocity.y >= speed)
        {
            return;
        }

        if (acceleration > 0.0f)
        {
            velocity.y = Mathf.MoveTowards(velocity.y, speed, acceleration * Time.deltaTime);
        }
        else
        {
            velocity.y = speed;
        }

        _body.velocity = transform.TransformDirection(velocity);

        if (_body.TryGetComponent(out MovingSphere sphere))
        {
            sphere.PreventSnapToGround();
        }
    }

    private void OnTriggerEnter(Collider _other)
    {
        Rigidbody body = _other.attachedRigidbody;

        if (body)
        {
            Accelerate(body);
        }
    }

    private void OnTriggerStay(Collider _other)
    {
        Rigidbody body = _other.attachedRigidbody;

        if (body)
        {
            Accelerate(body);
        }
    }
}