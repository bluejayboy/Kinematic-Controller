using UnityEngine;

[RequireComponent(typeof(KinematicMotor))]
public class KinematicController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 10;
    [SerializeField] private float jumpSpeed = 15;
    [SerializeField] private float gravity = 35;
    [SerializeField] private float snapForce = 10;

    private KinematicMotor kinematicBody;

    private void Awake()
    {
        kinematicBody = GetComponent<KinematicMotor>();
    }

    private void FixedUpdate()
    {
        Vector3 moveDirection = kinematicBody.Velocity;

        if (kinematicBody.IsGrounded)
        {
            moveDirection = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
            moveDirection = moveDirection.normalized;
            moveDirection *= moveSpeed;
            moveDirection.y = -snapForce;

            if (Input.GetButton("Jump"))
            {
                moveDirection.y = jumpSpeed;
            }
        }

        moveDirection.y -= gravity * Time.fixedDeltaTime;
        kinematicBody.Move(moveDirection);
    }
}