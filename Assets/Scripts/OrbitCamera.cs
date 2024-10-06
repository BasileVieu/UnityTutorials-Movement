using UnityEngine;

[RequireComponent(typeof(Camera))]
public class OrbitCamera : MonoBehaviour
{
    [SerializeField] private Transform focus;
    [SerializeField][Range(1.0f, 20.0f)] private float distance = 5.0f;
    [SerializeField][Min(0.0f)] private float focusRadius = 1.0f;
    [SerializeField][Range(0.0f, 1.0f)] private float focusCentering = 0.5f;
    [SerializeField][Range(1.0f, 360.0f)] private float rotationSpeed = 90.0f;
    [SerializeField][Range(-89.0f, 89.0f)] private float minVerticalAngle = -30.0f;
    [SerializeField][Range(-89.0f, 89.0f)] private float maxVerticalAngle = 60.0f;
    [SerializeField][Min(0.0f)] private float alignDelay = 5.0f;
    [SerializeField][Range(0.0f, 90.0f)] private float alignSmoothRange = 45.0f;
    [SerializeField][Min(0.0f)] private float upAlignmentSpeed = 360.0f;
    [SerializeField] private LayerMask obstructionMask = -1;

    private Camera regularCamera;

    private Quaternion gravityAlignment = Quaternion.identity;
    private Quaternion orbitRotation;

    private Vector3 focusPoint;
    private Vector3 previousFocusPoint;

    private Vector2 orbitAngles = new Vector2(45.0f, 0.0f);

    private float lastManualRotationTime;

    private Vector3 CameraHalfExtends
    {
        get
        {
            Vector3 halfExtends;
            halfExtends.y = regularCamera.nearClipPlane * Mathf.Tan(0.5f * Mathf.Deg2Rad * regularCamera.fieldOfView);
            halfExtends.x = halfExtends.y * regularCamera.aspect;
            halfExtends.z = 0.0f;

            return halfExtends;
        }
    }

    private void OnValidate()
    {
        if (maxVerticalAngle < minVerticalAngle)
        {
            maxVerticalAngle = minVerticalAngle;
        }
    }

    private void Awake()
    {
        regularCamera = GetComponent<Camera>();
        
        focusPoint = focus.position;

        transform.localRotation = orbitRotation = Quaternion.Euler(orbitAngles);
    }

    private void LateUpdate()
    {
        UpdateGravityAlignment();
        
        UpdateFocusPoint();

        if (ManualRotation()
            || AutomaticRotation())
        {
            ConstrainAngles();

            orbitRotation = Quaternion.Euler(orbitAngles);
        }

        Quaternion lookRotation = gravityAlignment * orbitRotation;

        Vector3 lookDirection = lookRotation * Vector3.forward;

        Vector3 lookPosition = focusPoint - lookDirection * distance;

        Vector3 rectOffset = lookDirection * regularCamera.nearClipPlane;
        Vector3 rectPosition = lookPosition + rectOffset;
        Vector3 castFrom = focus.position;
        Vector3 castLine = rectPosition - castFrom;

        float castDistance = castLine.magnitude;

        Vector3 castDirection = castLine / castDistance;

        if (Physics.BoxCast(castFrom, CameraHalfExtends, castDirection, out RaycastHit hit, lookRotation, castDistance, obstructionMask, QueryTriggerInteraction.Ignore))
        {
            rectPosition = castFrom + castDirection * hit.distance;

            lookPosition = rectPosition - rectOffset;
        }

        transform.SetPositionAndRotation(lookPosition, lookRotation);
    }

    private void UpdateGravityAlignment()
    {
        Vector3 fromUp = gravityAlignment * Vector3.up;
        Vector3 toUp = CustomGravity.GetUpAxis(focusPoint);

        float dot = Mathf.Clamp(Vector3.Dot(fromUp, toUp), -1.0f, 1.0f);
        float angle = Mathf.Acos(dot) * Mathf.Rad2Deg;
        float maxAngle = upAlignmentSpeed * Time.deltaTime;

        Quaternion newAlignment = Quaternion.FromToRotation(fromUp, toUp) * gravityAlignment;

        if (angle <= maxAngle)
        {
            gravityAlignment = newAlignment;
        }
        else
        {
            gravityAlignment = Quaternion.SlerpUnclamped(gravityAlignment, newAlignment, maxAngle / angle);
        }
    }

    private void UpdateFocusPoint()
    {
        previousFocusPoint = focusPoint;
        
        Vector3 targetPoint = focus.position;

        if (focusRadius > 0.0f)
        {
            float distance = Vector3.Distance(targetPoint, focusPoint);

            float t = 1.0f;

            if (distance > 0.01f
                && focusCentering > 0.0f)
            {
                t = Mathf.Pow(1.0f - focusCentering, Time.unscaledDeltaTime);
            }

            if (distance > focusRadius)
            {
                t = Mathf.Min(t, focusRadius / distance);
            }

            focusPoint = Vector3.Lerp(targetPoint, focusPoint, t);
        }
        else
        {
            focusPoint = targetPoint;
        }
    }

    private bool ManualRotation()
    {
        Vector2 input = new Vector2(Input.GetAxis("Vertical Camera"), Input.GetAxis("Horizontal Camera"));

        const float e = 0.001f;

        if (input.x < -e
            || input.x > e
            || input.y < -e
            || input.y > e)
        {
            orbitAngles += rotationSpeed * Time.unscaledDeltaTime * input;

            lastManualRotationTime = Time.unscaledTime;

            return true;
        }

        return false;
    }

    private bool AutomaticRotation()
    {
        if (Time.unscaledTime - lastManualRotationTime < alignDelay)
        {
            return false;
        }

        Vector3 alignedDelta = Quaternion.Inverse(gravityAlignment) * (focusPoint - previousFocusPoint);

        Vector2 movement = new Vector2(alignedDelta.x, alignedDelta.z);

        float movementDeltaSqr = movement.sqrMagnitude;

        if (movementDeltaSqr < 0.0001f)
        {
            return false;
        }

        float headingAngle = GetAngle(movement / Mathf.Sqrt(movementDeltaSqr));

        float deltaAbs = Mathf.Abs(Mathf.DeltaAngle(orbitAngles.y, headingAngle));

        float rotationChange = rotationSpeed * Mathf.Min(Time.unscaledDeltaTime, movementDeltaSqr);

        if (deltaAbs < alignSmoothRange)
        {
            rotationChange *= deltaAbs / alignSmoothRange;
        }
        else if (180.0f - deltaAbs < alignSmoothRange)
        {
            rotationChange *= (180.0f - deltaAbs) / alignSmoothRange;
        }

        orbitAngles.y = Mathf.MoveTowardsAngle(orbitAngles.y, headingAngle, rotationChange);

        return true;
    }

    private void ConstrainAngles()
    {
        orbitAngles.x = Mathf.Clamp(orbitAngles.x, minVerticalAngle, maxVerticalAngle);

        if (orbitAngles.y < 0.0f)
        {
            orbitAngles.y += 360.0f;
        }
        else if (orbitAngles.y >= 360.0f)
        {
            orbitAngles.y -= 360.0f;
        }
    }

    private static float GetAngle(Vector2 _direction)
    {
        float angle = Mathf.Acos(_direction.y) * Mathf.Rad2Deg;

        return _direction.x < 0.0f ? 360.0f - angle : angle;
    }
}