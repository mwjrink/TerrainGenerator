using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public new CameraController camera;

    public float gravity = -19.6f;//-9.8f;
    public float speed = 5.0f;
    public float jumpForce = 5.0f;

    public bool freeFlyingMode = false;

    public Rigidbody rBody;
    public CharacterController controller;

    public Vector3 position { get => transform.position; private set => transform.position = value; }
    public Vector3 velocity;// { get => rBody.velocity; private set => rBody.velocity = value; }
    public Vector3 acceleration;
    public Vector3 gravDir = Vector3.down;

    private void Awake()
    {
        rBody = GetComponent<Rigidbody>();
        controller = GetComponent<CharacterController>();

        controller.slopeLimit = 45.0f;
        controller.stepOffset = 0.5f;
    }

    // Start is called before the first frame update
    void Start()
    {
        acceleration = new Vector3(0.0f, gravity, 0.0f);
    }

    // Update is called once per frame
    void Update()
    {
        // updateUsingPosition();
        // updateUsingRigidBody();
        updateUsingCharController();
    }

    private void updateUsingCharController()
    {
        if (freeFlyingMode)
        {
            var forwards = camera.direction * Input.GetAxis("Vertical");
            var sideways = new Vector3(camera.direction.z, 0, -camera.direction.x) * Input.GetAxis("Horizontal");

            var currentSpeed = speed;// Input.GetButton("Sprint") ? speed * 5 : speed;
            velocity = (forwards + sideways).normalized * currentSpeed;

            controller.Move(velocity * Time.deltaTime);
        }
        else
        {

            if (controller.isGrounded)
            {
                // acceleration.y = 0.0f;

                var forwards = new Vector3(camera.direction.x, 0, camera.direction.z) * Input.GetAxis("Vertical");
                var sideways = new Vector3(camera.direction.z, 0, -camera.direction.x) * Input.GetAxis("Horizontal");

                var currentSpeed = speed;// Input.GetButton("Sprint") ? speed * 5 : speed;
                velocity = (forwards + sideways).normalized * currentSpeed;

                if (Input.GetButton("Jump"))
                {
                    velocity.y = 7.5f;
                }
            }
            else
            {
                // acceleration.y = gravity;
            }

            velocity += acceleration * Time.deltaTime * 0.5f;
            controller.Move(velocity * Time.deltaTime);
        }
    }

    private void updateUsingRigidBody()
    {
        var forwards = new Vector3(camera.direction.x, 0, camera.direction.z) * Input.GetAxis("Vertical");
        var sideways = new Vector3(camera.direction.z, 0, -camera.direction.x) * Input.GetAxis("Horizontal");

        var currentSpeed = speed; // Input.GetButton("Sprint") ? speed * 5.0f : speed

        // var vertVel = acceleration * 0.5f * Time.deltaTime + new Vector3(0, rBody.velocity.y, 0);
        // rBody.velocity = (forwards + sideways).normalized * currentSpeed + vertVel;

        rBody.MovePosition(position + (forwards + sideways).normalized * Time.deltaTime * currentSpeed);

        var playerLayer = LayerMask.NameToLayer("Player");

        var hit = Physics.Raycast(position - 0.05f * gravDir, gravDir, out var hitInfo);
        var grounded = hit ? hitInfo.distance < 0.1f : false;

        // use the Input System Package instead
        if (grounded)//position.y == 0)
        {
            Debug.DrawRay(position - 0.05f * gravDir, gravDir * hitInfo.distance, Color.green);

            if (Input.GetButton("Jump"))
            {
                // rBody.velocity.Set(rBody.velocity.x, 7.5f, rBody.velocity.z);
                rBody.AddRelativeForce(Vector3.up * jumpForce, ForceMode.VelocityChange);
            }
        }
        else
        {
            Debug.DrawRay(position - 0.05f * gravDir, gravDir * (hit ? hitInfo.distance : 5f), Color.red);
        }
    }

    private void updateUsingPosition()
    {
        var playerLayer = LayerMask.NameToLayer("Player");

        var hit = Physics.Raycast(position, gravDir, out var hitInfo);
        var grounded = hit ? hitInfo.distance < 0.05f : false;

        // use the Input System Package instead
        if (grounded)//position.y == 0)
        {
            Debug.DrawRay(position, gravDir * hitInfo.distance, Color.green);
            acceleration.y = 0.0f;

            var forwards = new Vector3(camera.direction.x, 0, camera.direction.z) * Input.GetAxis("Vertical");
            var sideways = new Vector3(camera.direction.z, 0, -camera.direction.x) * Input.GetAxis("Horizontal");

            var currentSpeed = speed;// Input.GetButton("Sprint") ? speed * 5 : speed;
            velocity = (forwards + sideways).normalized * currentSpeed;

            if (Input.GetButton("Jump"))
            {
                velocity.Set(velocity.x, 7.5f, velocity.z);
            }
        }
        else
        {
            Debug.DrawRay(position, gravDir * (hit ? hitInfo.distance : 5f), Color.red);
            acceleration.y = gravity;
        }

        velocity += acceleration * Time.deltaTime * 0.5f;
        position += velocity * Time.deltaTime;

        var clamped = position;
        clamped.y = Mathf.Clamp(position.y, 0.0f, 1000.0f);
        position = clamped;
    }
}
