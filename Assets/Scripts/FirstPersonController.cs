using UnityEngine;
using UnityEngine.InputSystem;

public class FirstPersonController : MonoBehaviour
{
    [Header("Param�tres de mouvement")]
    [SerializeField] private float moveSpeed = 5f;

    [Header("Param�tres de cam�ra")]
    [SerializeField] private float mouseSensitivity = 2f;
    [SerializeField] private Transform playerCamera;
    [SerializeField] private float maxLookAngle = 90f;

    [Header("Rotation initiale")]
    [SerializeField] private float initialYRotation = 0f;
    [SerializeField] private float initialVerticalAngle = 0f;

    [Header("Param�tres assis")]
    [SerializeField] private float seatedHorizontalLimit = 60f;
    [SerializeField] private float seatedVerticalMin = 5f;
    [SerializeField] private float seatedVerticalMax = 85f;
    [SerializeField] private float seatedInitialAngle = 30f;
    [SerializeField] private float seatedFOV = 45f;
    [SerializeField] private float fovLerpSpeed = 3f;

    private CharacterController characterController;
    private PlayerInputActions inputActions;
    private Vector2 moveInput;
    private Vector2 lookInput;
    private float verticalRotation;
    private Vector3 moveDirection;

    private bool isSeated = false;
    private float seatedCenterYRotation;

    private Camera playerCameraComponent;
    private float defaultFOV;
    private float targetFOV;

    private const float GRAVITY = -9.81f;
    private const string KeySensitivity = "opt_sensitivity";

    public bool CanMove { get; set; } = true;
    public bool CanLook { get; set; } = true;
    public bool IsSeated => isSeated;

    private void Start()
    {
        // Écrase la valeur Inspector si l'utilisateur a sauvegardé une sensibilité dans les options
        if (PlayerPrefs.HasKey(KeySensitivity))
            mouseSensitivity = PlayerPrefs.GetFloat(KeySensitivity);
    }

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        inputActions = new PlayerInputActions();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (playerCamera == null)
            playerCamera = GetComponentInChildren<Camera>().transform;

        playerCameraComponent = playerCamera.GetComponent<Camera>();
        defaultFOV = playerCameraComponent.fieldOfView;
        targetFOV = defaultFOV;

        transform.rotation = Quaternion.Euler(0f, initialYRotation, 0f);
        verticalRotation = initialVerticalAngle;
        playerCamera.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);
    }

    private void OnEnable()
    {
        var playerActionMap = inputActions.Player;
        playerActionMap.Enable();

        playerActionMap.Move.performed += OnMove;
        playerActionMap.Move.canceled += OnMove;
        playerActionMap.Look.performed += OnLook;
        playerActionMap.Look.canceled += OnLook;
    }

    private void OnDisable()
    {
        var playerActionMap = inputActions.Player;

        playerActionMap.Move.performed -= OnMove;
        playerActionMap.Move.canceled -= OnMove;
        playerActionMap.Look.performed -= OnLook;
        playerActionMap.Look.canceled -= OnLook;

        playerActionMap.Disable();
    }

    private void OnMove(InputAction.CallbackContext context) =>
        moveInput = context.ReadValue<Vector2>();

    private void OnLook(InputAction.CallbackContext context) =>
        lookInput = context.ReadValue<Vector2>();

    private void Update()
    {
        HandleMovement();
        HandleRotation();
        HandleFOV();
    }

    private void HandleMovement()
    {
        if (!characterController.enabled) return;

        if (!CanMove)
        {
            moveDirection.y = GRAVITY;
            characterController.Move(new Vector3(0, moveDirection.y, 0) * Time.deltaTime);
            return;
        }

        moveDirection.x = moveInput.x;
        moveDirection.y = GRAVITY;
        moveDirection.z = moveInput.y;

        characterController.Move(transform.TransformDirection(moveDirection) * moveSpeed * Time.deltaTime);
    }

    private void HandleRotation()
    {
        if (!CanLook) return;

        if (isSeated)
            HandleSeatedRotation();
        else
            HandleFreeRotation();
    }

    private void HandleFreeRotation()
    {
        transform.Rotate(0f, lookInput.x * mouseSensitivity, 0f);

        verticalRotation -= lookInput.y * mouseSensitivity;
        verticalRotation = Mathf.Clamp(verticalRotation, -maxLookAngle, maxLookAngle);

        playerCamera.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);
    }

    private void HandleSeatedRotation()
    {
        float newY = transform.eulerAngles.y + lookInput.x * mouseSensitivity;
        float deltaY = Mathf.DeltaAngle(seatedCenterYRotation, newY);
        deltaY = Mathf.Clamp(deltaY, -seatedHorizontalLimit, seatedHorizontalLimit);
        transform.rotation = Quaternion.Euler(0f, seatedCenterYRotation + deltaY, 0f);

        verticalRotation -= lookInput.y * mouseSensitivity;
        verticalRotation = Mathf.Clamp(verticalRotation, seatedVerticalMin, seatedVerticalMax);
        playerCamera.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);
    }

    private void HandleFOV()
    {
        playerCameraComponent.fieldOfView = Mathf.Lerp(
            playerCameraComponent.fieldOfView,
            targetFOV,
            Time.deltaTime * fovLerpSpeed
        );
    }

    /// <summary>Activates or deactivates seated camera constraints and FOV zoom.</summary>
    public void SetSeatedMode(bool seated)
    {
        isSeated = seated;

        if (seated)
        {
            seatedCenterYRotation = transform.eulerAngles.y;
            verticalRotation = seatedInitialAngle;
            playerCamera.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);
            targetFOV = seatedFOV;
        }
        else
        {
            verticalRotation = 0f;
            playerCamera.localRotation = Quaternion.identity;
            targetFOV = defaultFOV;
        }
    }

    private void OnDestroy() => inputActions?.Dispose();
}
