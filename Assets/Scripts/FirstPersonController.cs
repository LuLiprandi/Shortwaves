using UnityEngine;
using UnityEngine.InputSystem;

public class FirstPersonController : MonoBehaviour
{
    [Header("Paramètres de mouvement")]
    [SerializeField] private float moveSpeed = 5f;

    [Header("Paramètres de caméra")]
    [SerializeField] private float mouseSensitivity = 2f;
    [SerializeField] private Transform playerCamera;
    [SerializeField] private float maxLookAngle = 90f;

    private CharacterController characterController;
    private PlayerInputActions inputActions;
    private Vector2 moveInput;
    private Vector2 lookInput;
    private float verticalRotation;
    private Vector3 moveDirection;

    private const float GRAVITY = -9.81f;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        inputActions = new PlayerInputActions();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (playerCamera == null)
        {
            playerCamera = GetComponentInChildren<Camera>().transform;
        }
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

    private void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    private void OnLook(InputAction.CallbackContext context)
    {
        lookInput = context.ReadValue<Vector2>();
    }

    private void Update()
    {
        HandleMovement();
        HandleRotation();
    }

    private void HandleMovement()
    {
        float deltaTime = Time.deltaTime;

        moveDirection.x = moveInput.x;
        moveDirection.y = GRAVITY;
        moveDirection.z = moveInput.y;

        Vector3 worldMove = transform.TransformDirection(moveDirection);

        characterController.Move(worldMove * moveSpeed * deltaTime);
    }

    private void HandleRotation()
    {
        float horizontalRotation = lookInput.x * mouseSensitivity;
        transform.Rotate(0f, horizontalRotation, 0f);

        verticalRotation -= lookInput.y * mouseSensitivity;
        verticalRotation = Mathf.Clamp(verticalRotation, -maxLookAngle, maxLookAngle);

        playerCamera.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);
    }

    private void OnDestroy()
    {
        inputActions?.Dispose();
    }
}
