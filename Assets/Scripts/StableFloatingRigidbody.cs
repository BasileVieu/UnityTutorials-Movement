using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class StableFloatingRigidbody : MonoBehaviour
{
    [SerializeField] private bool floatToSleep = false;
    [SerializeField] private bool safeFloating = false;
    [SerializeField] private float submergenceOffset = 0.5f;
    [SerializeField][Min(0.1f)] private float submergenceRange = 1.0f;
    [SerializeField][Min(0.0f)] private float buoyancy = 1.0f;
    [SerializeField] private Vector3[] buoyancyOffsets;
    [SerializeField][Range(0.0f, 10.0f)] private float waterDrag = 1.0f;
    [SerializeField] private LayerMask waterMask = 0;

    private Rigidbody body;

    private Vector3 gravity;

    private float floatDelay;
    private float[] submergence;

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        body.useGravity = false;

        submergence = new float[buoyancyOffsets.Length];
    }

    private void FixedUpdate()
    {
        if (floatToSleep)
        {
            if (body.IsSleeping())
            {
                floatDelay = 0.0f;

                return;
            }

            if (body.velocity.sqrMagnitude < 0.0001f)
            {
                floatDelay += Time.deltaTime;

                if (floatDelay >= 1.0f)
                {
                    return;
                }
            }
            else
            {
                floatDelay = 0.0f;
            }
        }

        gravity = CustomGravity.GetGravity(body.position);

        float dragFactor = waterDrag * Time.deltaTime / buoyancyOffsets.Length;
        float buoyancyFactor = -buoyancy / buoyancyOffsets.Length;

        for (int i = 0; i < buoyancyOffsets.Length; i++)
        {
            if (submergence[i] > 0.0f)
            {
                float drag = Mathf.Max(0.0f, 1.0f - dragFactor * submergence[i]);

                body.velocity *= drag;

                body.angularVelocity *= drag;

                body.AddForceAtPosition(gravity * (buoyancyFactor * submergence[i]), transform.TransformPoint(buoyancyOffsets[i]), ForceMode.Acceleration);

                submergence[i] = 0.0f;
            }
        }
        
        body.AddForce(gravity, ForceMode.Acceleration);
    }

    private void OnTriggerEnter(Collider _other)
    {
        if ((waterMask & (1 << _other.gameObject.layer)) != 0)
        {
            EvaluateSubmergence();
        }
    }

    private void OnTriggerStay(Collider _other)
    {
        if (!body.IsSleeping()
            && (waterMask & (1 << _other.gameObject.layer)) != 0)
        {
            EvaluateSubmergence();
        }
    }

    private void EvaluateSubmergence()
    {
        Vector3 down = gravity.normalized;
        Vector3 offset = down * -submergenceOffset;

        for (int i = 0; i < buoyancyOffsets.Length; i++)
        {
            Vector3 p = offset + transform.TransformPoint(buoyancyOffsets[i]);

            if (Physics.Raycast(p, down, out RaycastHit hit, submergenceRange + 1.0f, waterMask, QueryTriggerInteraction.Collide))
            {
                submergence[i] = 1.0f - hit.distance / submergenceRange;
            }
            else if (!safeFloating
                || Physics.CheckSphere(p, 0.01f, waterMask, QueryTriggerInteraction.Collide))
            {
                submergence[i] = 1.0f;
            }
        }
    }
}