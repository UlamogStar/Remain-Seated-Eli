using System;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class WheelchairController : MonoBehaviour
{
    [Header("Input (drag these in)")]
    public InputActionReference leftWheelAction;
    public InputActionReference rightWheelAction;
    public InputActionReference scrollAction;

    // camera test variables
    public InputActionReference cameraAction;
    
    [Header("Movement Settings")]
    public float wheelForce = 100f;    // forward/back force when both wheels engaged
    public float turnForce = 50f;      // torque when one wheel is engaged
    public float maxSpeed = 5f;        // cap linear speed
    public float maxTurnSpeed = 2f;    // cap angular Y speed

    [Header("Camera Settings")]
    public Transform playerCamera;     // assign your Main Camera (child of this object)
    public float cameraFollowSmooth = 5f;

    private Rigidbody rb;
    private bool leftWheelActive;
    private bool rightWheelActive;
    
    // camera test variables   ---------------------------------------------------
    [Header("Camera Rotation Limits")]
    [Tooltip("Look sensitivity for camera rotation")]
    public float mouseSensitivity = 10;
    private float xRotation = 0f; // vertical rotation
    private float yRotation = 0f; // horizontal rotation
   
    [Tooltip("Limits the vertical rotation of the camera for looking down")]
    [SerializeField] private float minVertical = -30f; // to limit vertical rotation
    [Tooltip("Limits the vertical rotation of the camera for looking up")]
    [SerializeField] private float maxVertical = 30f; // to limit vertical rotation
    [Tooltip("Limits the horizontal rotation of the camera for looking left and right")]
    [SerializeField] private float maxHorizontal = 60f; // to limit horizontal rotation
    private float baseYaw = 0f; // initial yaw of the chair
    private float basePitch = 0f; // initial pitch of the chair

    private void Start()
    {
        // gets Scene position of the chair camera and sets it as the base rotation to solve camera jumps on start
        // could put in awake but have not tested it yet there
        baseYaw = transform.eulerAngles.y;
        basePitch = transform.eulerAngles.x;
    }


    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void OnEnable()
    {
        // Enable input actions
        if (leftWheelAction)  leftWheelAction.action.Enable();
        if (rightWheelAction) rightWheelAction.action.Enable();
        if (scrollAction)     scrollAction.action.Enable();

        // Subscribe to button press/release
        if (leftWheelAction)
        {
            leftWheelAction.action.performed += OnLeftPressed;
            leftWheelAction.action.canceled  += OnLeftReleased;
        }
        if (rightWheelAction)
        {
            rightWheelAction.action.performed += OnRightPressed;
            rightWheelAction.action.canceled  += OnRightReleased;
        }

        // Scroll is read on performed to create a push
        if (scrollAction)
        {
            scrollAction.action.performed += OnScroll;
        }

        // RB default tuning
        rb.linearDamping = 1.5f;
        rb.angularDamping = 2f;
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        
        // Subscribe to camera action -----------------------------------------
        if (cameraAction)
        {
            cameraAction.action.Enable();
        }
        
    }

    private void OnDisable()
    {
        if (leftWheelAction)
        {
            leftWheelAction.action.performed -= OnLeftPressed;
            leftWheelAction.action.canceled  -= OnLeftReleased;
            leftWheelAction.action.Disable();
        }
        if (rightWheelAction)
        {
            rightWheelAction.action.performed -= OnRightPressed;
            rightWheelAction.action.canceled  -= OnRightReleased;
            rightWheelAction.action.Disable();
        }
        if (scrollAction)
        {
            scrollAction.action.performed -= OnScroll;
            scrollAction.action.Disable();
        }
        
        // Camera Action Unsubscribe ----------------------------------
        if (cameraAction)
        {
            cameraAction.action.Disable();
        }
        
    }

    private void Update()
    {
        // Smoothly align the camera yaw to the chair
        if (playerCamera)
        {
            Quaternion targetRot = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
            playerCamera.rotation = Quaternion.Slerp(
                playerCamera.rotation, targetRot, Time.deltaTime * cameraFollowSmooth
            );
        }
    }

    private void OnLeftPressed(InputAction.CallbackContext ctx)  => leftWheelActive  = true;
    private void OnLeftReleased(InputAction.CallbackContext ctx) => leftWheelActive  = false;
    private void OnRightPressed(InputAction.CallbackContext ctx) => rightWheelActive = true;
    private void OnRightReleased(InputAction.CallbackContext ctx)=> rightWheelActive = false;

    private void OnScroll(InputAction.CallbackContext ctx)
    {
        // Scroll is Vector2 (x,y). We want vertical wheel.
        Vector2 delta = ctx.ReadValue<Vector2>();
        float scrollY = delta.y;
        if (Mathf.Abs(scrollY) < 0.01f) return;

        ApplyWheelPush(scrollY);
    }

    private void ApplyWheelPush(float scroll)
    {
        // Forward cap
        if (rb.linearVelocity.magnitude > maxSpeed && leftWheelActive && rightWheelActive)
            return;

        Vector3 fwd = transform.forward;

        if (leftWheelActive && rightWheelActive)
        {
            // Move forward/back
            rb.AddForce(fwd * scroll * wheelForce, ForceMode.Force);
        }
        else if (leftWheelActive)
        {
            // Turn right (positive scroll => clockwise yaw)
            if (Mathf.Abs(rb.angularVelocity.y) < maxTurnSpeed)
                rb.AddTorque(Vector3.up * scroll * turnForce, ForceMode.Force);
        }
        else if (rightWheelActive)
        {
            // Turn left (positive scroll => counter-clockwise yaw)
            if (Mathf.Abs(rb.angularVelocity.y) < maxTurnSpeed)
                rb.AddTorque(Vector3.up * -scroll * turnForce, ForceMode.Force);
        }
    }
    // ---------------------- camera detection testing section -----------------------

    private void FixedUpdate()
    {
        CameraDetection(); // start Raycast detection for playerlook
        
        // get input action for mouse and stick movement
        Vector2 lookInput = cameraAction.action.ReadValue<Vector2>();
        
        // scale input by sensitivity and time for smoothing and consistency across frame rates
        // takes the input from the input action and makes sure that it works on different machines
        float mouseX = lookInput.x * mouseSensitivity * Time.deltaTime;
        float mouseY = lookInput.y * mouseSensitivity * Time.deltaTime;

        // --- Vertical rotation ---
        xRotation -= mouseY; // invert mouseY for natural feel 
        xRotation = Mathf.Clamp(xRotation, minVertical, maxVertical);// clamp vertical rotation

        // --- Horizontal rotation ---
        yRotation += mouseX;// add mouseX to horizontal rotation
        yRotation = Mathf.Clamp(yRotation, -maxHorizontal, maxHorizontal); // clamp horizontal rotation

        // Combine both rotations in one quaternion
        // this changes the rotation of the chair  base on the input from the mouse after they have been clamped
        Quaternion combinedRotation = Quaternion.Euler(basePitch + xRotation, baseYaw + yRotation, 0f);
        transform.rotation = combinedRotation; // apply rotation to the chair
    }

    private void CameraDetection() // raycast to detect objects in front of camera
    {
        Vector3 fwd = transform.TransformDirection(Vector3.forward); // get forward direction of chair
        
        if (Physics.Raycast(transform.position, fwd, out RaycastHit hit, 100f)) // raycast from chair position forward
        {
         
            Debug.Log("Hit" + hit.collider.name); // log the name of the object hit by the raycast for testing
            
            Debug.DrawRay(playerCamera.position,fwd * hit.distance, Color.red); // draw red ray to show hit to show in editor what was hit
        }
        else
        {
            Debug.DrawRay(playerCamera.position, fwd * 10f, Color.green); // if nothing is hit, draw green ray to show in editor
        }
    }
}
