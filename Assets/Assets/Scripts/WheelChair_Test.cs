using UnityEngine;
using UnityEngine.InputSystem;
public class WheelChair_Test : MonoBehaviour
{
    [Header("Input (drag these in)")]
    public InputActionReference leftWheelAction;
    public InputActionReference rightWheelAction;
    public InputActionReference scrollAction;
    public InputActionReference cameraAction;
    public InputActionReference interactAction;

    [Header("Movement Settings")]
    public float wheelForce = 200f;
    public float turnForce = 5f;
    public float maxSpeed = 5f;
    public float maxTurnSpeed = 2f;

    [Header("Camera Settings")]
    public Transform playerCamera;       // your camera transform (child or separate)
    public float cameraFollowSmooth = 5f;
    public Vector3 cameraOffset = new Vector3(0f, 1.5f, 0f); // camera height offset above chair

    private Rigidbody rb;
    private bool leftWheelActive;
    private bool rightWheelActive;

    [Header("Camera Rotation Settings")]
    public float mouseSensitivity = 10f;
    private float xRotation;
    private float yRotation;
    [SerializeField] private float minVertical = -30f;
    [SerializeField] private float maxVertical = 30f;
    [SerializeField] private float maxHorizontal = 60f;

    private float baseYaw; // used for initial camera alignment
    private float basePitch; // used for initial camera alignment
    public Camera mainCamera;
    
    [Header("Highlight Settings")]
    public Material highlightMaterial;   // assign in Inspector
    private MeshRenderer lastHighlightedRenderer;
    private Material[] originalMaterials;
    Canvas lastCanvas;
    public Canvas interactionCanvas;
    

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void Start()
    {
        // not used yet but will be when camera and player interaction is fully implemented so camera does not jump on start
        baseYaw = transform.eulerAngles.y;
        basePitch = transform.eulerAngles.x;
        
        interactionCanvas.gameObject.SetActive(false); // set interaction canvas to inactive at start
    }

    private void OnEnable()
    {
        if (leftWheelAction) leftWheelAction.action.Enable();
        if (rightWheelAction) rightWheelAction.action.Enable();
        if (scrollAction) scrollAction.action.Enable();
        if (cameraAction) cameraAction.action.Enable();
        
        if(interactAction) interactAction.action.Enable();

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
        if (scrollAction)
        {
            scrollAction.action.performed += OnScroll;
        }
        

        rb.linearDamping = 1.5f;
        rb.angularDamping = 2f;
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
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
        if (cameraAction)
        {
            cameraAction.action.Disable();
        }
        
        if(interactAction) {interactAction.action.Disable(); }
        
    }

    private void Update()
    {
        HandleCameraLook();
        FollowCameraToChair();
        CameraDetection(); // would like to handel in fixed update but player interactions will not work correctly then
    }

    // ---------------- Movement Logic ----------------
    private void OnLeftPressed(InputAction.CallbackContext ctx)  => leftWheelActive  = true;
    private void OnLeftReleased(InputAction.CallbackContext ctx) => leftWheelActive  = false;
    private void OnRightPressed(InputAction.CallbackContext ctx) => rightWheelActive = true;
    private void OnRightReleased(InputAction.CallbackContext ctx)=> rightWheelActive = false;

    private void OnScroll(InputAction.CallbackContext ctx)
    {
        Vector2 delta = ctx.ReadValue<Vector2>();
        float scrollY = delta.y;
        if (Mathf.Abs(scrollY) < 0.01f) return;
        ApplyWheelPush(scrollY);
    }

    private void ApplyWheelPush(float scroll)
    {
        if (rb.linearVelocity.magnitude > maxSpeed && leftWheelActive && rightWheelActive)
            return;

        Vector3 fwd = transform.forward;

        if (leftWheelActive && rightWheelActive)
        {
            rb.AddForce(fwd * scroll * wheelForce, ForceMode.Force);
        }
        else if (leftWheelActive)
        {
            if (Mathf.Abs(rb.angularVelocity.y) < maxTurnSpeed)
                rb.AddTorque(Vector3.up * scroll * turnForce, ForceMode.Force);
        }
        else if (rightWheelActive)
        {
            if (Mathf.Abs(rb.angularVelocity.y) < maxTurnSpeed)
                rb.AddTorque(Vector3.up * -scroll * turnForce, ForceMode.Force);
        }
    }

    // ---------------- Camera Control ----------------
    private void HandleCameraLook()
    {
        Vector2 lookInput = cameraAction.action.ReadValue<Vector2>();
        float mouseX = lookInput.x * mouseSensitivity * Time.deltaTime;
        float mouseY = lookInput.y * mouseSensitivity * Time.deltaTime;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, minVertical, maxVertical);
        yRotation += mouseX;
        yRotation = Mathf.Clamp(yRotation, -maxHorizontal, maxHorizontal);

        // camera rotation (independent of chair)
        Quaternion camRot = Quaternion.Euler(xRotation, transform.eulerAngles.y + yRotation, 0f);
        mainCamera.transform.rotation = camRot;
    }

    private void FollowCameraToChair()
    {
        // follow position smoothly
        Vector3 targetPos = transform.position + cameraOffset;
        playerCamera.position = Vector3.Lerp(
            playerCamera.position, targetPos, Time.deltaTime * cameraFollowSmooth
        );
    }

    // ---------------- Detection (Ray cast) ----------------
    private void CameraDetection()
    {
        if (!mainCamera) return;
        Vector3 fwd = mainCamera.transform.forward;

        if (Physics.Raycast(mainCamera.transform.position, fwd, out RaycastHit hit, 100f))
        {
            Debug.DrawRay(mainCamera.transform.position, fwd * hit.distance, Color.red);

            Interactable interactable = hit.collider.GetComponent<Interactable>();
            MeshRenderer meshRenderer = hit.collider.GetComponent<MeshRenderer>();
            Canvas canvas = hit.collider.GetComponent<Canvas>();

            if (meshRenderer && interactable)
            {
                if (meshRenderer != lastHighlightedRenderer)
                {
                    // Remove outline from previous
                    if (lastHighlightedRenderer != null)
                    {
                        RemoveOutline(lastHighlightedRenderer);
                    }

                    // Add outline to new
                    AddOutline(meshRenderer);
                    lastHighlightedRenderer = meshRenderer;
                    
                    // Enable the new interactable's canvas
                    if (interactable)
                    {
                        interactionCanvas.gameObject.SetActive(true);
                    }
                }
            }

            if (interactable && interactAction.action.WasPressedThisFrame())
            {
                Debug.DrawRay(mainCamera.transform.position, fwd * 10f, Color.blue);
                Debug.Log("Interacting with: " + hit.collider.name);
                interactable.Interact();
            }
        }
        else
        {
            Debug.DrawRay(mainCamera.transform.position, fwd * 10f, Color.green);
            if (lastHighlightedRenderer != null)
            {
                RemoveOutline(lastHighlightedRenderer);
                lastHighlightedRenderer = null;
                
            }
            
            interactionCanvas.gameObject.SetActive(false);
            
        }
    }

    // Add outline material temporarily
    private void AddOutline(MeshRenderer renderer)
    {
        var mats = renderer.sharedMaterials;
        var newMats = new Material[mats.Length + 1];
        mats.CopyTo(newMats, 0);
        newMats[mats.Length] = highlightMaterial;
        renderer.materials = newMats;
    }

    // Remove outline material
    private void RemoveOutline(MeshRenderer renderer)
    {
        var mats = renderer.sharedMaterials;
        if (mats.Length > 1 && mats[mats.Length - 1] == highlightMaterial)
        {
            var newMats = new Material[mats.Length - 1];
            for (int i = 0; i < newMats.Length; i++)
                newMats[i] = mats[i];
            renderer.materials = newMats;
        }
    }
}