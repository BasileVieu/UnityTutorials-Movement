using UnityEngine;

public class MovingSphere : MonoBehaviour
{
    [SerializeField] private Transform playerInputSpace;
    [SerializeField] private Transform ball;
    [SerializeField][Range(0.0f, 100.0f)] private float maxSpeed = 10.0f;
    [SerializeField][Range(0.0f, 100.0f)] private float maxClimbSpeed = 4.0f;
    [SerializeField][Range(0.0f, 100.0f)] private float maxSwimSpeed = 5.0f;
    [SerializeField][Range(0.0f, 100.0f)] private float maxAcceleration = 10.0f;
    [SerializeField][Range(0.0f, 100.0f)] private float maxAirAcceleration = 1.0f;
    [SerializeField][Range(0.0f, 100.0f)] private float maxClimbAcceleration = 40.0f;
    [SerializeField][Range(0.0f, 100.0f)] private float maxSwimAcceleration = 5.0f;
    [SerializeField][Range(0.0f, 10.0f)] private float jumpHeight = 2.0f;
    [SerializeField][Range(0, 5)] private int maxAirJumps = 0;
    [SerializeField][Range(0.0f, 90.0f)] private float maxGroundAngle = 25.0f;
    [SerializeField][Range(0.0f, 90.0f)] private float maxStairsAngle = 50.0f;
    [SerializeField][Range(90.0f, 180.0f)] private float maxClimbAngle = 140.0f;
    [SerializeField][Range(0.0f, 100.0f)] private float maxSnapSpeed = 100.0f;
    [SerializeField][Min(0.0f)] private float probeDistance = 1.0f;
    [SerializeField] private float submergenceOffset = 0.5f;
    [SerializeField][Min(0.1f)] private float submergenceRange = 1.0f;
    [SerializeField][Min(0.0f)] private float buoyancy = 1.0f;
    [SerializeField][Range(0.0f, 10.0f)] private float waterDrag = 1.0f;
    [SerializeField][Range(0.01f, 1.0f)] private float swimThreshold = 0.5f;
    [SerializeField] private LayerMask probeMask = -1;
    [SerializeField] private LayerMask stairsMask = -1;
    [SerializeField] private LayerMask climbMask = -1;
    [SerializeField] private LayerMask waterMask = -1;
    [SerializeField] private Material normalMaterial;
    [SerializeField] private Material climbingMaterial;
    [SerializeField] private Material swimmingMaterial;
    [SerializeField][Min(0.1f)] private float ballRadius = 0.5f;
    [SerializeField][Min(0.0f)] private float ballAlignSpeed = 180.0f;
    [SerializeField][Min(0.0f)] private float ballAirRotation = 0.5f;
    [SerializeField][Min(0.0f)] private float ballSwimRotation = 2.0f;

    private Rigidbody body;
    private Rigidbody connectedBody;
    private Rigidbody previousConnectedBody;

    private MeshRenderer meshRenderer;

    private Vector3 velocity;
    private Vector3 connectionVelocity;
    private Vector3 contactNormal;
    private Vector3 steepNormal;
    private Vector3 climbNormal;
    private Vector3 lastClimbNormal;
    private Vector3 upAxis;
    private Vector3 rightAxis;
    private Vector3 forwardAxis;
    private Vector3 connectionWorldPosition;
    private Vector3 connectionLocalPosition;
    private Vector3 playerInput;
    private Vector3 lastContactNormal;
    private Vector3 lastSteepNormal;
    private Vector3 lastConnectionVelocity;

    private float minGroundDotProduct;
    private float minStairsDotProduct;
    private float minClimbDotProduct;
    private float submergence;

    private int jumpPhase;
    private int groundContactCount;
    private int steepContactCount;
    private int climbContactCount;
    private int stepsSinceLastGrounded;
    private int stepsSinceLastJump;

    private bool desiredJump;
    private bool desiresClimbing;

    private bool OnGround => groundContactCount > 0;

    private bool OnSteep => steepContactCount > 0;

    private bool Climbing => climbContactCount > 0 && stepsSinceLastJump > 2;

    private bool InWater => submergence > 0.0f;

    private bool Swimming => submergence >= swimThreshold;

    private void OnValidate()
    {
        minGroundDotProduct = Mathf.Cos(maxGroundAngle * Mathf.Deg2Rad);
        minStairsDotProduct = Mathf.Cos(maxStairsAngle * Mathf.Deg2Rad);
        minClimbDotProduct = Mathf.Cos(maxClimbAngle * Mathf.Deg2Rad);
    }

    private void Awake()
    {
        body = GetComponent<Rigidbody>();

        body.useGravity = false;

        meshRenderer = ball.GetComponent<MeshRenderer>();

        OnValidate();
    }

    private void Update()
    {
        playerInput.x = Input.GetAxis("Horizontal");
        playerInput.z = Input.GetAxis("Vertical");
        playerInput.y = Swimming ? Input.GetAxis("UpDown") : 0.0f;
        playerInput = Vector3.ClampMagnitude(playerInput, 1.0f);

        if (playerInputSpace)
        {
            rightAxis = ProjectDirectionOnPlane(playerInputSpace.right, upAxis);
            forwardAxis = ProjectDirectionOnPlane(playerInputSpace.forward, upAxis);
        }
        else
        {
            rightAxis = ProjectDirectionOnPlane(Vector3.right, upAxis);
            forwardAxis = ProjectDirectionOnPlane(Vector3.forward, upAxis);
        }

        if (Swimming)
        {
            desiresClimbing = false;
        }
        else
        {
            desiredJump |= Input.GetButtonDown("Jump");
            desiresClimbing = Input.GetButton("Climb");
        }

        UpdateBall();
    }

    private void FixedUpdate()
    {
        Vector3 gravity = CustomGravity.GetGravity(body.position, out upAxis);
        
        UpdateState();

        if (InWater)
        {
            velocity *= 1.0f - waterDrag * submergence * Time.deltaTime;
        }

        AdjustVelocity();

        if (desiredJump)
        {
            desiredJump = false;

            Jump(gravity);
        }

        if (Climbing)
        {
            velocity -= contactNormal * (maxClimbAcceleration * 0.9f * Time.deltaTime);
        }
        else if (InWater)
        {
            velocity += gravity * ((1.0f - buoyancy * submergence) * Time.deltaTime);
        }
        else if (OnGround
            && velocity.sqrMagnitude < 0.01f)
        {
            velocity += contactNormal * (Vector3.Dot(gravity, contactNormal) * Time.deltaTime);
        }
        else if (desiresClimbing
            && OnGround)
        {
            velocity += (gravity - contactNormal * (maxClimbAcceleration * 0.9f)) * Time.deltaTime;
        }
        else
        {
            velocity += gravity * Time.deltaTime;
        }

        body.velocity = velocity;

        ClearState();
    }

    private void UpdateBall()
    {
        Material ballMaterial = normalMaterial;

        Vector3 rotationPlaneNormal = lastContactNormal;

        float rotationFactor = 1.0f;

        if (Climbing)
        {
            ballMaterial = climbingMaterial;
        }
        else if (Swimming)
        {
            ballMaterial = swimmingMaterial;

            rotationFactor = ballSwimRotation;
        }
        else if (!OnGround)
        {
            if (OnSteep)
            {
                lastContactNormal = lastSteepNormal;
            }
            else
            {
                rotationFactor = ballAirRotation;
            }
        }

        meshRenderer.material = ballMaterial;

        Vector3 movement = (body.velocity - lastConnectionVelocity) * Time.deltaTime;
        movement -= rotationPlaneNormal * Vector3.Dot(movement, rotationPlaneNormal);

        float distance = movement.magnitude;

        Quaternion rotation = ball.localRotation;

        if (connectedBody
            && connectedBody == previousConnectedBody)
        {
            rotation = Quaternion.Euler(connectedBody.angularVelocity * (Mathf.Rad2Deg * Time.deltaTime)) * rotation;

            if (distance < 0.001f)
            {
                ball.localRotation = rotation;

                return;
            }
        }
        else if (distance < 0.001f)
        {
            return;
        }

        float angle = distance * rotationFactor * (180.0f / Mathf.PI) / ballRadius;

        Vector3 rotationAxis = Vector3.Cross(rotationPlaneNormal, movement).normalized;

        rotation = Quaternion.Euler(rotationAxis * angle) * rotation;

        if (ballAlignSpeed > 0.0f)
        {
            rotation = AlignBallRotation(rotationAxis, rotation, distance);
        }

        ball.localRotation = rotation;
    }

    private Quaternion AlignBallRotation(Vector3 _rotationAxis, Quaternion _rotation, float _traveledDistance)
    {
        Vector3 ballAxis = ball.up;

        float dot = Mathf.Clamp(Vector3.Dot(ballAxis, _rotationAxis), -1.0f, 1.0f);
        float angle = Mathf.Acos(dot) * Mathf.Rad2Deg;
        float maxAngle = ballAlignSpeed * _traveledDistance;

        Quaternion newAlignment = Quaternion.FromToRotation(ballAxis, _rotationAxis) * _rotation;

        if (angle <= maxAngle)
        {
            return newAlignment;
        }
        else
        {
            return Quaternion.SlerpUnclamped(_rotation, newAlignment, maxAngle / angle);
        }
    }

    private void UpdateState()
    {
        stepsSinceLastGrounded += 1;
        stepsSinceLastJump += 1;

        velocity = body.velocity;

        if (CheckClimbing()
            || CheckSwimming()
            || OnGround
            || SnapToGround()
            || CheckSteepContacts())
        {
            stepsSinceLastGrounded = 0;

            if (stepsSinceLastJump > 1)
            {
                jumpPhase = 0;
            }

            if (groundContactCount > 1)
            {
                contactNormal.Normalize();
            }
        }
        else
        {
            contactNormal = upAxis;
        }

        if (connectedBody)
        {
            if (connectedBody.isKinematic
                || connectedBody.mass >= body.mass)
            {
                UpdateConnectionState();
            }
        }
    }

    private void ClearState()
    {
        lastContactNormal = contactNormal;

        lastSteepNormal = steepNormal;

        lastConnectionVelocity = connectionVelocity;
        
        groundContactCount = steepContactCount = climbContactCount = 0;

        contactNormal = steepNormal = connectionVelocity = climbNormal = Vector3.zero;

        previousConnectedBody = connectedBody;

        connectedBody = null;

        submergence = 0.0f;
    }

    private void UpdateConnectionState()
    {
        if (connectedBody == previousConnectedBody)
        {
            Vector3 connectionMovement = connectedBody.transform.TransformPoint(connectionLocalPosition) - connectionWorldPosition;

            connectionVelocity = connectionMovement / Time.deltaTime;
        }
        
        connectionWorldPosition = body.position;

        connectionLocalPosition = connectedBody.transform.InverseTransformPoint(connectionWorldPosition);
    }

    private bool SnapToGround()
    {
        if (stepsSinceLastGrounded > 1
            || stepsSinceLastJump <= 2)
        {
            return false;
        }

        float speed = velocity.magnitude;

        if (speed > maxSnapSpeed)
        {
            return false;
        }

        if (!Physics.Raycast(body.position, -upAxis, out RaycastHit hit, probeDistance, probeMask, QueryTriggerInteraction.Ignore))
        {
            return false;
        }

        float upDot = Vector3.Dot(upAxis, hit.normal);

        if (upDot < GetMinDot(hit.collider.gameObject.layer))
        {
            return false;
        }

        groundContactCount = 1;

        contactNormal = hit.normal;

        float dot = Vector3.Dot(velocity, hit.normal);

        if (dot > 0.0f)
        {
            velocity = (velocity - hit.normal * dot).normalized * speed;
        }

        connectedBody = hit.rigidbody;

        return true;
    }

    public void PreventSnapToGround()
    {
        stepsSinceLastJump = -1;
    }

    private float GetMinDot(int _layer)
    {
        return (stairsMask & (1 << _layer)) == 0 ? minGroundDotProduct : minStairsDotProduct;
    }

    private void Jump(Vector3 _gravity)
    {
        Vector3 jumpDirection;

        if (OnGround)
        {
            jumpDirection = contactNormal;
        }
        else if (OnSteep)
        {
            jumpDirection = steepNormal;

            jumpPhase = 0;
        }
        else if (maxAirJumps > 0
            && jumpPhase <= maxAirJumps)
        {
            if (jumpPhase == 0)
            {
                jumpPhase = 1;
            }

            jumpDirection = contactNormal;
        }
        else
        {
            return;
        }

        stepsSinceLastJump = 0;

        jumpPhase += 1;

        float jumpSpeed = Mathf.Sqrt(2.0f * _gravity.magnitude * jumpHeight);

        if (InWater)
        {
            jumpSpeed *= Mathf.Max(0.0f, 1.0f - submergence / swimThreshold);
        }

        jumpDirection = (jumpDirection + upAxis).normalized;

        float alignedSpeed = Vector3.Dot(velocity, jumpDirection);

        if (alignedSpeed > 0.0f)
        {
            jumpSpeed = Mathf.Max(jumpSpeed - alignedSpeed, 0.0f);
        }

        velocity += jumpDirection * jumpSpeed;
    }

    private Vector3 ProjectDirectionOnPlane(Vector3 _direction, Vector3 _normal)
    {
        return (_direction - _normal * Vector3.Dot(_direction, _normal)).normalized;
    }

    private void AdjustVelocity()
    {
        float acceleration;
        float speed;
        
        Vector3 xAxis;
        Vector3 zAxis;

        if (Climbing)
        {
            acceleration = maxClimbAcceleration;
            speed = maxClimbSpeed;
            
            xAxis = Vector3.Cross(contactNormal, upAxis);
            zAxis = upAxis;
        }
        else if (InWater)
        {
            float swimFactor = Mathf.Min(1.0f, submergence / swimThreshold);
            
            acceleration = Mathf.LerpUnclamped(OnGround ? maxAcceleration : maxAirAcceleration, maxSwimAcceleration, swimFactor);
            speed = Mathf.LerpUnclamped(maxSpeed, maxSwimSpeed, swimFactor);

            xAxis = rightAxis;
            zAxis = forwardAxis;
        }
        else
        {
            acceleration = OnGround ? maxAcceleration : maxAirAcceleration;
            speed = OnGround && desiresClimbing ? maxClimbSpeed : maxSpeed;

            xAxis = rightAxis;
            zAxis = forwardAxis;
        }
        
        xAxis = ProjectDirectionOnPlane(xAxis, contactNormal);
        zAxis = ProjectDirectionOnPlane(zAxis, contactNormal);

        Vector3 relativeVelocity = velocity - connectionVelocity;

        Vector3 adjustment;
        adjustment.x = playerInput.x * speed - Vector3.Dot(relativeVelocity, xAxis);
        adjustment.z = playerInput.z * speed - Vector3.Dot(relativeVelocity, zAxis);
        adjustment.y = Swimming ? playerInput.y * speed - Vector3.Dot(relativeVelocity, upAxis) : 0.0f;

        adjustment = Vector3.ClampMagnitude(adjustment, acceleration * Time.deltaTime);

        velocity += xAxis * adjustment.x + zAxis * adjustment.z;

        if (Swimming)
        {
            velocity += upAxis * adjustment.y;
        }
    }

    private bool CheckSteepContacts()
    {
        if (steepContactCount > 1)
        {
            steepNormal.Normalize();

            float upDot = Vector3.Dot(upAxis, steepNormal);

            if (upDot >= minGroundDotProduct)
            {
                groundContactCount = 1;

                contactNormal = steepNormal;

                return true;
            }
        }

        return false;
    }

    private bool CheckClimbing()
    {
        if (Climbing)
        {
            if (climbContactCount > 1)
            {
                climbNormal.Normalize();

                float upDot = Vector3.Dot(upAxis, climbNormal);

                if (upDot >= minGroundDotProduct)
                {
                    climbNormal = lastClimbNormal;
                }
            }
            
            groundContactCount = 1;

            contactNormal = climbNormal;

            return true;
        }

        return false;
    }

    private bool CheckSwimming()
    {
        if (Swimming)
        {
            groundContactCount = 0;

            contactNormal = upAxis;

            return true;
        }

        return false;
    }

    private void OnCollisionEnter(Collision _collision)
    {
        EvaluateCollision(_collision);
    }

    private void OnCollisionStay(Collision _collision)
    {
        EvaluateCollision(_collision);
    }

    private void OnTriggerEnter(Collider _other)
    {
        if ((waterMask & (1 << _other.gameObject.layer)) != 0)
        {
            EvaluateSubmergence(_other);
        }
    }

    private void OnTriggerStay(Collider _other)
    {
        if ((waterMask & (1 << _other.gameObject.layer)) != 0)
        {
            EvaluateSubmergence(_other);
        }
    }

    private void EvaluateSubmergence(Collider _collider)
    {
        if (Physics.Raycast(body.position + upAxis * submergenceOffset, -upAxis, out RaycastHit hit, submergenceRange + 1.0f, waterMask, QueryTriggerInteraction.Collide))
        {
            submergence = 1.0f - hit.distance / submergenceRange;
        }
        else
        {
            submergence = 1.0f;
        }

        if (Swimming)
        {
            connectedBody = _collider.attachedRigidbody;
        }
    }

    private void EvaluateCollision(Collision _collision)
    {
        if (Swimming)
        {
            return;
        }
        
        int layer = _collision.gameObject.layer;
        
        float minDot = GetMinDot(layer);

        for (int i = 0; i < _collision.contactCount; i++)
        {
            Vector3 normal = _collision.GetContact(i).normal;

            float upDot = Vector3.Dot(upAxis, normal);

            if (upDot >= minDot)
            {
                groundContactCount += 1;

                contactNormal += normal;

                connectedBody = _collision.rigidbody;
            }
            else
            {
                if (upDot > -0.01f)
                {
                    steepContactCount += 1;

                    steepNormal += normal;

                    if (groundContactCount == 0)
                    {
                        connectedBody = _collision.rigidbody;
                    }
                }

                if (desiresClimbing
                    && upDot >= minClimbDotProduct
                    && (climbMask & (1 << layer)) != 0)
                {
                    climbContactCount += 1;

                    climbNormal += normal;

                    lastClimbNormal = normal;

                    connectedBody = _collision.rigidbody;
                }
            }
        }
    }
}