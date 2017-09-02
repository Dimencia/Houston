using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityStandardAssets.CrossPlatformInput;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
public class PlayerControls : MonoBehaviour
{

    [Serializable]
    public class MovementSettings
    {
        public float ForwardSpeed = 8.0f;   // Speed when walking forward
        public float BackwardSpeed = 4.0f;  // Speed when walking backwards
        public float StrafeSpeed = 4.0f;    // Speed when walking sideways
        public float RunMultiplier = 2.0f;   // Speed when sprinting
        public float ForwardSpeedOriginal;
        public float BackwardSpeedOriginal;
        public float StrafeSpeedOriginal;
        public KeyCode RunKey = KeyCode.LeftShift;
        public float JumpForce = 30f;
        public AnimationCurve SlopeCurveModifier = new AnimationCurve(new Keyframe(-90.0f, 1.0f), new Keyframe(0.0f, 1.0f), new Keyframe(90.0f, 0.0f));
        [HideInInspector] public float CurrentTargetSpeed = 8f;

#if !MOBILE_INPUT
        private bool m_Running;
#endif


        public void triggerSpeedReduction()
        {
            ForwardSpeed = ForwardSpeedOriginal / 4;
            BackwardSpeed = BackwardSpeedOriginal / 4;
            StrafeSpeed = StrafeSpeedOriginal / 4;
        }

        public void triggerSpeedNormal()
        {
            // Man I'm so bad at function names right now
            ForwardSpeed = ForwardSpeedOriginal;
            BackwardSpeed = BackwardSpeedOriginal;
            StrafeSpeed = StrafeSpeedOriginal;
        }

        public void UpdateDesiredTargetSpeed(Vector2 input)
        {
            if (input == Vector2.zero) return;
            if (input.x > 0 || input.x < 0)
            {
                //strafe
                CurrentTargetSpeed = StrafeSpeed;
            }
            if (input.y < 0)
            {
                //backwards
                CurrentTargetSpeed = BackwardSpeed;
            }
            if (input.y > 0)
            {
                //forwards
                //handled last as if strafing and moving forward at the same time forwards speed should take precedence
                CurrentTargetSpeed = ForwardSpeed;
            }
#if !MOBILE_INPUT
            if (Input.GetKey(RunKey))
            {
                CurrentTargetSpeed *= RunMultiplier;
                m_Running = true;
            }
            else
            {
                m_Running = false;
            }
#endif
        }

#if !MOBILE_INPUT
        public bool Running
        {
            get { return m_Running; }
        }
#endif
    }
    [Serializable]
    public class AdvancedSettings
    {
        public float collisionFixModifier = 0.01f;
        public float groundCheckDistance = 0.01f; // distance for checking if the controller is grounded ( 0.01f seems to work best for this )
        public float stickToGroundHelperDistance = 0.5f; // stops the character
        public float slowDownRate = 20f; // rate at which the controller comes to a stop when there is no input
        public bool airControl; // can the user control the direction that is being moved in the air
        [Tooltip("set it to 0.1 or more if you get stuck in wall")]
        public float shellOffset; //reduce the radius by that ratio to avoid getting stuck in wall (a value of 0.1f is nice)
    }

    public Rigidbody rocket;
    public float torque = 10000000.0f;
    public float horizontalSpeed = 4.0f;
    public float verticalSpeed = 4.0f;
    private Rocket_Control controller;

    private Vector3 lastGoodPosition;

    public Camera cam;
    public MovementSettings movementSettings = new MovementSettings();
    //public MouseLook mouseLook = new MouseLook();
    public AdvancedSettings advancedSettings = new AdvancedSettings();


    private Rigidbody m_RigidBody;
    private CapsuleCollider m_Capsule;
    private BoxCollider m_Box;
    private Vector3 m_GroundContactNormal;
    private bool m_Jump, m_PreviouslyGrounded, m_Jumping, m_IsGrounded;

    // Use this for initialization
    void Start()
    {

        movementSettings.ForwardSpeedOriginal = movementSettings.ForwardSpeed;
        movementSettings.BackwardSpeedOriginal = movementSettings.BackwardSpeed;
        movementSettings.StrafeSpeedOriginal = movementSettings.StrafeSpeed;

        torque = 50000000.0f;
        controller = rocket.GetComponent<Rocket_Control>();
        m_RigidBody = GetComponent<Rigidbody>();
        m_Capsule = GetComponent<CapsuleCollider>();
        m_Box = GetComponent<BoxCollider>();
        //mouseLook.Init (transform, cam.transform);

        Rigidbody rb = GetComponent<Rigidbody>();

        //rb.inertiaTensor = rb.inertiaTensor + new Vector3(0, 0, GetComponent<Rigidbody>().inertiaTensor.z * 100);
        // Should prevent 'bouncing'

        
}

    public Vector3 Velocity
    {
        get { return m_RigidBody.velocity; }
    }

    public bool Grounded
    {
        get { return m_IsGrounded; }
    }

    public bool Jumping
    {
        get { return m_Jumping; }
    }

    public bool Running
    {
        get
        {
#if !MOBILE_INPUT
            return movementSettings.Running;
#else
	            return false;
#endif
        }
    }

    private void FixedUpdate()
    {
        if (!controller.controllingRocket)
        {
            lastGoodPosition = transform.localPosition;
            GroundCheck();
            Vector2 input = GetInput();

            if ((Mathf.Abs(input.x) > 0 || Mathf.Abs(input.y) > 0) && (advancedSettings.airControl || m_IsGrounded))
                // They used to both be > float.Epsilon for cross platform
            {
                // always move along the camera forward as it is the direction that it being aimed at
                Vector3 desiredMove = cam.transform.forward * input.y + cam.transform.right * input.x;
                desiredMove = Vector3.ProjectOnPlane(desiredMove, m_GroundContactNormal).normalized;

                desiredMove.x = desiredMove.x * movementSettings.CurrentTargetSpeed;
                desiredMove.z = desiredMove.z * movementSettings.CurrentTargetSpeed;
                desiredMove.y = desiredMove.y * movementSettings.CurrentTargetSpeed;
                if (m_RigidBody.velocity.sqrMagnitude <
                    (movementSettings.CurrentTargetSpeed * movementSettings.CurrentTargetSpeed))
                {

                    m_RigidBody.AddForce(desiredMove * SlopeMultiplier(), ForceMode.Impulse);
                    //Debug.Log("Added force to rigidbody..." + desiredMove);
                }
                // Check if this will place us inside a collider next tick
                // If it would, set velocity to 0 and directly set the position?

                /* None of this worked.

                RaycastHit ray;
                float velocityMagnitude = m_RigidBody.velocity.magnitude;
                if (Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out ray, velocityMagnitude))
                {
                    // We gonna run into something
                    // Reduce velocity to less than the distance between these two - take the normal of velocity and multiply it by raycast distance
                    // And when we detect preventingCollision we'll zero out velocity
                    preventingCollision = true;
                    m_RigidBody.velocity = m_RigidBody.velocity.normalized * (ray.distance - advancedSettings.collisionCheckDistance);
                }

                // This didn't seem to do anything, so we're then gonna detect if we're already in an object and try to move out
                if(GetComponent<CapsuleCollider>().bounds.Intersects())*/

                // New idea, every frame there isn't a collision, we store the position
                // Then on collision start, we set the position back, and then raycast to the collision point and move that distance
                
            }

            if (m_IsGrounded)
            {
                m_RigidBody.drag = 5f;

                if (m_Jump)
                {
                    m_RigidBody.drag = 0f;
                    m_RigidBody.velocity = new Vector3(m_RigidBody.velocity.x, 0f, m_RigidBody.velocity.z);
                    m_RigidBody.AddForce(new Vector3(0f, movementSettings.JumpForce, 0f), ForceMode.Impulse);
                    m_Jumping = true;
                }

                if (!m_Jumping && Mathf.Abs(input.x) == 0 && Mathf.Abs(input.y) == 0 && m_RigidBody.velocity.magnitude < 1f)
                {
                    m_RigidBody.Sleep();
                }
            }
            else
            {
                m_RigidBody.drag = 0f;
                if (m_PreviouslyGrounded && !m_Jumping)
                {
                    StickToGroundHelper();
                }
            }
            m_Jump = false;
        }
    }

    

    // Update is called once per frame
    void Update()
    {

        if (controller.controllingRocket)
        {

            if (Input.GetKey(KeyCode.W))
            {
                rocket.AddTorque(rocket.transform.forward * torque);
            }
            if (Input.GetKey(KeyCode.S))
            {
                rocket.AddTorque(-rocket.transform.forward * torque);
            }
            if (Input.GetKey(KeyCode.A))
            {
                rocket.AddTorque(-rocket.transform.right * torque);
            }
            if (Input.GetKey(KeyCode.D))
            {
                rocket.AddTorque(rocket.transform.right * torque);
            }
            if (Input.GetKey(KeyCode.Q)) // Q and E take a lot less torque
            {
                rocket.AddTorque(-rocket.transform.up * torque/10);
            }
            if (Input.GetKey(KeyCode.E))
            {
                rocket.AddTorque(rocket.transform.up * torque/10);
            }

            // F will be the key to exit things, clicking the normal interact key
            if (Input.GetKey(KeyCode.F))
            {
                controller.releaseRocketControl();
            }
        }
        else
        {

            //RotateView();
            // Handled in custom function 

            if (CrossPlatformInputManager.GetButtonDown("Jump") && !m_Jump)
            {
                m_Jump = true;
            }
            /*Rigidbody rb = Camera.main.GetComponent<Rigidbody>();
            if (Input.GetKey(KeyCode.W))
            {
                rb.MovePosition(rb.position + Camera.main.transform.forward.normalized*movementAmount);
            }
            if (Input.GetKey(KeyCode.S))
            {
                rb.MovePosition(rb.position - Camera.main.transform.forward.normalized * movementAmount);
            }
            if (Input.GetKey(KeyCode.A))
            {
                rb.MovePosition(rb.position + Camera.main.transform.right.normalized * movementAmount);
            }
            if (Input.GetKey(KeyCode.D))
            {
                rb.MovePosition(rb.position - Camera.main.transform.right.normalized * movementAmount);
            }*/
            // Handle this with the new stuff

        }

        float yaw = horizontalSpeed * Input.GetAxis("Mouse X");
        float pitch = -verticalSpeed * Input.GetAxis("Mouse Y");

        if (yaw == 0 && pitch == 0)
        {
            Camera.main.SendMessage("UpdateCameraShake"); // Only trigger this if no mouse movement, so it is less intrusive
            return;
        }
        else
        {

            // Lock roll to the same as the rocket
            //Debug.Log("Setting Z to " + rocket.transform.eulerAngles.z);
            //transform.eulerAngles = new Vector3(pitch, yaw, rocket.transform.eulerAngles.z);
            Camera.main.transform.localEulerAngles = new Vector3(Camera.main.transform.localEulerAngles.x + pitch,
                Camera.main.transform.localEulerAngles.y + yaw,
                Camera.main.transform.localEulerAngles.z);

        }


    }

    private void OnCollisionEnter(Collision collision)
    {
        // IDC I'm gonna stop the collision before it becomes a problem, I hope this happens before the game does any reaction
        //if(GetComponent<CapsuleCollider>().bounds.Intersects(collision.collider.bounds))
        //{
        // Get that shit out
        // We need to push them along the vector from the collision point to the center of their character
        // We'll reverse total velocity then push along this vector after modifying from advancedSettings

        // Just the first contact point is probably fine
        //Vector3 pushBackVector = -collision.contacts[0].point.normalized * advancedSettings.collisionFixModifier;
        //m_RigidBody.AddForce(pushBackVector,ForceMode.Impulse);
        //Debug.Log("Pushing back from collision...");

        //}

        //Debug.Log("Collided with " + collision.collider.name + " with tag " + collision.collider.tag);

        // Do not run this if it's just the floor

        /*if (!collision.collider.tag.Equals("Floor"))
        {

            //transform.localPosition = lastGoodPosition;
            //RaycastHit ray;
            //Vector3 contactPoint = collision.contacts[0].point;
            //Physics.Raycast(transform.localPosition, contactPoint, out ray);
            //transform.localPosition = transform.localPosition - directionTo(contactPoint) * (ray.distance - advancedSettings.collisionFixModifier);
            //m_RigidBody.velocity = Vector3.zero;
            //lastGoodPosition = transform.localPosition;
            //Debug.Log("Manually moved out of collision");
        }*/

    }

    private void OnCollisionStay(Collision collision)
    {
        
    }

    private void OnCollisionExit(Collision collision)
    {
        
    }

    private Vector3 directionTo(Vector3 to)
    {
       return (to-transform.position).normalized;
    }


private float SlopeMultiplier()
    {
        float angle = Vector3.Angle(m_GroundContactNormal, Vector3.up);
        return movementSettings.SlopeCurveModifier.Evaluate(angle);
    }


    private void StickToGroundHelper()
    {
        RaycastHit hitInfo;
        if (Physics.Raycast(GetComponent<CapsuleCollider>().bounds.center, -GetComponent<CapsuleCollider>().transform.up, out hitInfo, advancedSettings.stickToGroundHelperDistance + (m_Capsule.height/2)))
        {
            if (Mathf.Abs(Vector3.Angle(hitInfo.normal, Vector3.up)) < 85f)
            {
                m_RigidBody.velocity = Vector3.ProjectOnPlane(m_RigidBody.velocity, hitInfo.normal);
            }
        }
    }


    private Vector2 GetInput()
    {

        /*Vector2 input = new Vector2
        {
            x = CrossPlatformInputManager.GetAxis("Horizontal"),
            y = CrossPlatformInputManager.GetAxis("Vertical")
        };*/
        // This was supposed to be cross platform but didn't work right, probably because of some project settings
        // So I'll use this.
        float x = 0;
        float y = 0;
        if (Input.GetKey(KeyCode.W))
        {
            y = 1;
        }
        if (Input.GetKey(KeyCode.S))
        {
            y = -1;
        }
        if (Input.GetKey(KeyCode.A))
        {
            x = -1;
        }
        if (Input.GetKey(KeyCode.D))
        {
            x = 1;
        }
        Vector2 input = new Vector2(x, y);
        //if (input != Vector2.zero)
        //    Debug.Log("Input is " + input);
        movementSettings.UpdateDesiredTargetSpeed(input);
        return input;
    }


    /*private void RotateView()
    {
        //avoids the mouse looking if the game is effectively paused
        if (Mathf.Abs(Time.timeScale) < float.Epsilon) return;

        // get the rotation before it's changed
        float oldYRotation = transform.eulerAngles.y;

        mouseLook.LookRotation (transform, cam.transform);

        if (m_IsGrounded || advancedSettings.airControl)
        {
            // Rotate the rigidbody velocity to match the new direction that the character is looking
            Quaternion velRotation = Quaternion.AngleAxis(transform.eulerAngles.y - oldYRotation, Vector3.up);
            m_RigidBody.velocity = velRotation*m_RigidBody.velocity;
        }
    }*/

    /// sphere cast down just beyond the bottom of the capsule to see if the capsule is colliding round the bottom
    private void GroundCheck()
    {
        if (GetComponent<CapsuleCollider>().isTrigger)
        {
            m_IsGrounded = false;
            return; // If we're jettisoned or otherwise not colliding don't detect these
        }
        m_PreviouslyGrounded = m_IsGrounded;
        RaycastHit hitInfo;
        if(Physics.Raycast(GetComponent<CapsuleCollider>().bounds.center, -GetComponent<CapsuleCollider>().transform.up, out hitInfo, advancedSettings.groundCheckDistance + (m_Capsule.height/2)*m_Capsule.transform.localScale.y))
        {
            m_IsGrounded = true;
            m_GroundContactNormal = hitInfo.normal;
            movementSettings.triggerSpeedNormal();
        }
        else
        {
            m_IsGrounded = false;
            m_GroundContactNormal = Vector3.up;
            movementSettings.triggerSpeedReduction(); // If we do this here, it'll only slow controls in the air, not slow them down for no reason if they suddenly go airborne
        }
        if (!m_PreviouslyGrounded && m_IsGrounded && m_Jumping)
        {
            m_Jumping = false;
        }
        Physics.Raycast(GetComponent<CapsuleCollider>().bounds.center, -GetComponent<CapsuleCollider>().transform.up, out hitInfo);
        Debug.Log("Grounded = " + m_IsGrounded + "; nearest object is " + hitInfo.collider.gameObject.name + " which is " + hitInfo.distance + " away");
    }
    

}