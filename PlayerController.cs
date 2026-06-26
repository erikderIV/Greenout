using UnityEngine;
using UnityEngine.InputSystem;
using System;
using Unity.VisualScripting;

public class PlayerController : MonoBehaviour
{
    [Header("Debug")]
    [SerializeField] private Vector3 velocity;
    [SerializeField] private float speed;
    [SerializeField] private float timer = 0f;

    [Header("References")]
    public Camera cam;

    [Header("Movement")]
    public float moveSpeed = 6f;
    public float maxStandingSpeed = 10f;
    public float maxCrouchingSpeed = 5f;
    public float slideStrength = 10f;
    public float gravity = -20f;
    public float jumpHeight = 1.5f;
    public float speedToSlide = 4f;
    public float speedToKeepSliding = 1f;
    public float slideCooldown = 1f;
    public float groundFriction = 0.9f;
    public float slideFriction = 0.8f;
    public float airFriction = 0.98f;
    public bool toggleCrouchSlide = false;
    public float standingHeight = 1.9f;
    public float crouchingHeight = 0.95f;
    public float slidingHeight = 0.475f;
    public Vector3 standingCamPos = new Vector3(0, 0.6f, 0);
    public Vector3 crouchingCamPos = new Vector3(0, 0, 0);
    public Vector3 slidingCamPos = new Vector3(0, -0.4f, 0);

    [Header("Look")]
    public float mouseSensitivity = 0.3f;

    public event Action OnStartSlide;
    public event Action OnStopSlide;

    public event Action OnStartCrouch;
    public event Action OnStopCrouch;

    public event Action OnStartFalling;
    public event Action<float> OnStopFalling;

    private CharacterController controller;

    private PlayerInputAction inputAction;

    private Vector2 moveInput;
    private Vector2 lookInput;

    private float currentHeight = 1.9f;
    private Vector3 currentCamPos = new Vector3(0, 1.7f, 0);
    private float pitch;
    private bool wasGrounded;
    private MovementState state = MovementState.Standing;
    private MovementState Laststate = MovementState.Standing;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        inputAction = new PlayerInputAction();
    }

    private void OnEnable()
    {
        inputAction.Enable();

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        inputAction.Player.Move.performed += OnMove;
        inputAction.Player.Move.canceled += OnMoveCanceled;

        inputAction.Player.Look.performed += OnLook;
        inputAction.Player.Look.canceled += OnLookCanceled;

        inputAction.Player.Jump.performed += OnJump;

        inputAction.Player.Crouch.performed += OnCrouchSlide;
        inputAction.Player.Crouch.canceled += OnCrouchSlideCanceled;
    }

    private void OnDisable()
    {
        inputAction.Disable();

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        inputAction.Player.Move.performed -= OnMove;
        inputAction.Player.Move.canceled -= OnMoveCanceled;

        inputAction.Player.Look.performed -= OnLook;
        inputAction.Player.Look.canceled -= OnLookCanceled;

        inputAction.Player.Jump.performed -= OnJump;

        inputAction.Player.Crouch.performed -= OnCrouchSlide;
        inputAction.Player.Crouch.canceled -= OnCrouchSlideCanceled;
    }

    private void OnMove(InputAction.CallbackContext ctx)
    {
        moveInput = ctx.ReadValue<Vector2>();
    }

    private void OnMoveCanceled(InputAction.CallbackContext ctx)
    {
        moveInput = Vector2.zero;
    }

    private void OnLook(InputAction.CallbackContext ctx)
    {
        lookInput = ctx.ReadValue<Vector2>();
    }

    private void OnLookCanceled(InputAction.CallbackContext ctx)
    {
        lookInput = Vector2.zero;
    }

    private void OnJump(InputAction.CallbackContext ctx)
    {
        if (!controller.isGrounded) return;

        AddImpulse(Mathf.Sqrt(jumpHeight * -2f * gravity) * Vector3.up);
    }

    private void OnCrouchSlide(InputAction.CallbackContext ctx)
    {
        if (toggleCrouchSlide)
        {
            if (state == MovementState.Crouching || state == MovementState.Sliding)
                StopCrouchSlide();
            else
                StartCrouchSlide();
        }
        else
        {
            StartCrouchSlide();
        }
    }

    private void OnCrouchSlideCanceled(InputAction.CallbackContext ctx)
    {
        if (toggleCrouchSlide) return;

        StopCrouchSlide();
    }

    private void Update()
    {
        UpdateAirborneState();
        UpdateSliding();
        UpdateVelocity();
        Look();
        Move();
        //UpdateControllerAndCam();

        speed = HorizontalVelocity(velocity);

        if (Laststate != state)
        {
            Debug.Log($"State changed from {Laststate} to {state}");
        }

        Laststate = state;
    }

    private void UpdateAirborneState()
    {
        bool grounded = controller.isGrounded;

        if (!wasGrounded && grounded)
        {
            // landed
            if (state == MovementState.Airborne)
            {
                state = MovementState.Standing;
                OnStopFalling?.Invoke(velocity.y);
            }
        }

        if (wasGrounded && !grounded && velocity.y < 0f)
        {
            if (state != MovementState.Airborne)
            {
                state = MovementState.Airborne;
                OnStartFalling?.Invoke();
            }
        }

        if (!wasGrounded && !grounded && state != MovementState.Airborne)
        {
            state = MovementState.Airborne;
            OnStartFalling?.Invoke();
        }

        wasGrounded = grounded;
    }

    private void UpdateSliding()
    {
        timer = timer > 0f ? timer - Time.deltaTime : 0f;

        if (state == MovementState.Sliding && HorizontalVelocity(velocity) < speedToKeepSliding)
        {
            SlidingToCrouching();
        }
    }

    private void UpdateVelocity()
    {
        Vector3 input = transform.right * moveInput.x + transform.forward * moveInput.y;

        input = Vector3.ClampMagnitude(input, 1f);

        Vector3 horizontal = new Vector3(velocity.x, 0, velocity.z);

        Vector3 accel = input;

        switch (state)
        {
            case MovementState.Standing:
                accel *= moveSpeed;
                horizontal += accel * Time.deltaTime;

                horizontal *= Mathf.Pow(groundFriction, Time.deltaTime * 60f);

                horizontal = Vector3.ClampMagnitude(horizontal, maxStandingSpeed);

                velocity.x = horizontal.x;
                velocity.z = horizontal.z;
                break;
            case MovementState.Airborne:
                accel *= moveSpeed * 0.3f;

                horizontal += accel * Time.deltaTime;

                velocity *= Mathf.Pow(airFriction, Time.deltaTime * 60f);

                velocity.y += gravity * Time.deltaTime;
                velocity.x = horizontal.x;
                velocity.z = horizontal.z;
                break;
            case MovementState.Crouching:
                accel *= moveSpeed;
                horizontal += accel * Time.deltaTime;

                horizontal *= Mathf.Pow(groundFriction, Time.deltaTime * 60f);

                horizontal = Vector3.ClampMagnitude(horizontal, maxCrouchingSpeed);

                velocity.x = horizontal.x;
                velocity.z = horizontal.z;
                break;
            case MovementState.Sliding:
                accel *= moveSpeed * 0.2f;
                horizontal += accel * Time.deltaTime;

                horizontal *= Mathf.Pow(slideFriction, Time.deltaTime * 60f);

                velocity.x = horizontal.x;
                velocity.z = horizontal.z;
                break;
        }

        if (HorizontalVelocity(velocity) < 0.01f)
        {
            velocity.x = 0f;
            velocity.z = 0f;
        }
    }

    private void Look()
    {
        float mouseX = lookInput.x * mouseSensitivity;
        float mouseY = lookInput.y * mouseSensitivity;

        transform.Rotate(Vector3.up * mouseX);

        pitch -= mouseY;
        pitch = Mathf.Clamp(pitch, -90f, 90f);

        cam.transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    private void Move()
    {
        if (controller.isGrounded && velocity.y < 0)
            velocity.y = -2f;

        controller.Move(velocity * Time.deltaTime);
    }

    private void UpdateControllerAndCam()
    {
        Vector3 desiredCamPos;
        float desiredHeight;

        switch (state)
        {
            case MovementState.Standing: desiredCamPos = standingCamPos; desiredHeight = standingHeight; break;
            case MovementState.Crouching: desiredCamPos = crouchingCamPos; desiredHeight = crouchingHeight; break;
            case MovementState.Sliding: desiredCamPos = slidingCamPos; desiredHeight = slidingHeight; break;
            default: desiredCamPos = standingCamPos; desiredHeight = standingHeight; break;
        }

        currentCamPos = new Vector3(0, Mathf.Lerp(currentCamPos.y, desiredCamPos.y, 10f * Time.deltaTime), 0);
        currentHeight = Mathf.Lerp(currentHeight, desiredHeight, 10f * Time.deltaTime);

        cam.transform.localPosition = currentCamPos;
        controller.height = currentHeight;
        controller.center = new Vector3(0, -0.5f * (currentHeight - 1.9f), 0);
    }

    private float HorizontalVelocity(Vector3 v)
    {
        v.y = 0;
        return v.magnitude;
    }

    public void AddImpulse(Vector3 impulse)
    {
        velocity += impulse;
    }

    bool CanStartCrouchSlide() => state == MovementState.Standing && controller.isGrounded;

    private void StartCrouchSlide()
    {
        if (!CanStartCrouchSlide()) return;


        if (HorizontalVelocity(velocity) >= speedToSlide && timer <= 0)
        {
            Debug.Log("Start Slide");

            state = MovementState.Sliding;

            Vector3 horizontal = new Vector3(velocity.x, 0, velocity.z);

            AddImpulse(horizontal.normalized * slideStrength);

            OnStartSlide?.Invoke();
        }
        else
        {
            Debug.Log("Start Crouch");

            state = MovementState.Crouching;

            OnStartCrouch?.Invoke();
        }
    }

    private void StopCrouchSlide()
    {
        if (state == MovementState.Sliding)
        {
            Debug.Log("Stop Slide");

            timer = slideCooldown;

            state = MovementState.Standing;

            OnStopSlide?.Invoke();
        }
        else if (state == MovementState.Crouching)
        {
            Debug.Log("Stop Crouch");

            state = MovementState.Standing;

            OnStopCrouch?.Invoke();
        }
    }

    private void SlidingToCrouching()
    {
        if (state != MovementState.Sliding) return;

        state = MovementState.Crouching;
        timer = slideCooldown;
        OnStopSlide?.Invoke();
        OnStartCrouch?.Invoke();

    }
}

enum MovementState
{
    Standing,
    Crouching,
    Sliding,
    Airborne
}
