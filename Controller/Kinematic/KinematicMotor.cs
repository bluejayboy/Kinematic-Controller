using UnityEngine;
using System.Collections.Generic;

public class KinematicMotor : MonoBehaviour
{
    public Vector3 Up { get; set; }
    public Vector3 Position { get; set; }
    public Vector3 Velocity { get; private set; }
    public bool IsGrounded { get; private set; }

    [SerializeField] private float slopeLimit = 45f;
    [SerializeField] private float stepOffset = 0.3f;
    [SerializeField] private float skinWidth = 0.08f;

    [SerializeField] private int MaxSweepSteps = 5;
    [SerializeField] private float MinMoveDistance = 0.01f;
    [SerializeField] private float MinCeilingAngle = 145;

    private readonly Collider[] overlaps = new Collider[5];
    private readonly List<RaycastHit> contacts = new List<RaycastHit>();

    private CapsuleCollider capsule;
    private Rigidbody rb;

    private void Awake()
    {
        capsule = GetComponent<CapsuleCollider>();
        rb = GetComponent<Rigidbody>();
    }

    public void Move(Vector3 velocity)
    {
        Up = transform.up;
        Position = rb.position;
        Velocity = velocity;
        IsGrounded = false;

        contacts.Clear();

        HandleCollision();
        HandleContacts();
        Depenetrate();

        rb.MovePosition(Position);
    }

    public void Rotate(Quaternion rotation)
    {
        rb.MoveRotation(rotation);
    }

    private void HandleCollision()
    {
        if (Velocity.sqrMagnitude < MinMoveDistance)
        {
            return;
        }

        Vector3 localVelocity = transform.InverseTransformDirection(Velocity) * Time.deltaTime;
        Vector3 lateralVelocity = new Vector3(localVelocity.x, 0, localVelocity.z);
        Vector3 verticalVelocity = new Vector3(0, localVelocity.y, 0);

        lateralVelocity = transform.TransformDirection(lateralVelocity);
        verticalVelocity = transform.TransformDirection(verticalVelocity);

        CapsuleSweep(lateralVelocity.normalized, lateralVelocity.magnitude, stepOffset, MinCeilingAngle);
        CapsuleSweep(verticalVelocity.normalized, verticalVelocity.magnitude, 0, 0, slopeLimit);
    }

    private void HandleContacts()
    {
        if (contacts.Count > 0)
        {
            float angle;

            foreach (RaycastHit contact in contacts)
            {
                angle = Vector3.Angle(Up, contact.normal);

                if (angle <= slopeLimit)
                {
                    IsGrounded = true;
                }

                Velocity -= Vector3.Project(Velocity, contact.normal);
            }
        }
    }

    private void CapsuleSweep(Vector3 direction, float distance, float stepOffset, float minSlideAngle = 0, float maxSlideAngle = 360)
    {
        Vector3 origin, top, bottom;
        RaycastHit hitInfo;
        float safeDistance;
        float slideAngle;

        float capsuleOffset = capsule.height * 0.5f - capsule.radius;

        for (int i = 0; i < MaxSweepSteps; i++)
        {
            origin = Position + capsule.center - direction * capsule.radius;
            bottom = origin - Up * (capsuleOffset - stepOffset);
            top = origin + Up * capsuleOffset;

            if (Physics.CapsuleCast(top, bottom, capsule.radius, direction, out hitInfo, distance + capsule.radius))
            {
                slideAngle = Vector3.Angle(Up, hitInfo.normal);
                safeDistance = hitInfo.distance - capsule.radius - skinWidth;
                Position += direction * safeDistance;
                contacts.Add(hitInfo);

                if ((slideAngle >= minSlideAngle) && (slideAngle <= maxSlideAngle))
                {
                    break;
                }

                direction = Vector3.ProjectOnPlane(direction, hitInfo.normal);
                distance -= safeDistance;
            }
            else
            {
                Position += direction * distance;
                break;
            }
        }
    }

    private void Depenetrate()
    {
        float capsuleOffset = capsule.height * 0.5f - capsule.radius;
        Vector3 top = Position + Up * capsuleOffset;
        Vector3 bottom = Position - Up * capsuleOffset;
        int overlapCount = Physics.OverlapCapsuleNonAlloc(top, bottom, capsule.radius, overlaps);

        if (overlapCount > 0)
        {
            for (int i = 0; i < overlapCount; i++)
            {
                if ((overlaps[i].transform != transform) && Physics.ComputePenetration(capsule, Position, transform.rotation,
                    overlaps[i], overlaps[i].transform.position, overlaps[i].transform.rotation, out Vector3 direction, out float distance))
                {
                    Position += direction * (distance + skinWidth);
                    Velocity -= Vector3.Project(Velocity, -direction);
                }
            }
        }
    }
}