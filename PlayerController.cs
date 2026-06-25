using UnityEngine;
using UnityEngine.InputSystem;
using System;

public class PlayerController : MonoBehaviour
{
	[Header("References")]
	public Camera cam;
	
	[Header("Movement")]
	public float moveSpeed = 6f;
	public float slideMulti = 2f;
	public float crouchMulti = 0.5f;
	public float gravity = -20f;
	public float jumpHeight = 1.5f;
	public float slideTime = 1f;
	public float slideCooldown = 3f;
	public float crouchThreshold = 4f;
	[Range (0f, 1f)] public float slideMax = 0.3f;
	public float sidewaysDamping = 0.4f;
	public float standingHeight = 1.9f;
	public float crouchingHeight = 0.95f;
	public float slidingHeight = 0.5f;
	public Vector3 standingCamPos = new Vector3(0, 1.7f, 0); 
	public Vector3 crouchingCamPos = new Vector3(0, 0.85f, 0); 
	public Vector3 slidingCamPos = new Vector3(0, 0.34f, 0);
	public float forwardAlignmentThreshold = 30;
	
	[Header("Look")]
	public float mouseSensitivity = 0.5f;
	
	public event Action OnStartSlide;
	public event Action OnStopSlide;
	
	public event Action OnStartCrouch;
	public event Action OnStopCrouch;
	
	public event Action OnStartFalling;
	public event Action OnStopFalling;
	
	private CharacterController controller;
	
	private PlayerInputAction inputAction;
	
	private Vector2 moveInput;
	private Vector2 lookInput;
	private float verticalVelocity;
	private float lastVerticalVelocity;
	private float timer = 0f;
	private Vector3 slideDir;
	private Vector3 fallDir;
	private float pitch;
	private MovementState state = MovementState.Standing;
	
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
		
		inputAction.Player.Crouch.performed += OnCrouch;
		inputAction.Player.Crouch.canceled += OnCrouchCanceled;
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
		
		inputAction.Player.Crouch.performed -= OnCrouch;
		inputAction.Player.Crouch.canceled -= OnCrouchCanceled;
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

        verticalVelocity = (Mathf.Sqrt(jumpHeight * -2f * gravity));
    }
	
	private void OnCrouch(InputAction.CallbackContext ctx)
	{
		if (Vector3ToHorizontalVelocity(controller.velocity) < crouchThreshold)
			StartCrouch();
		else		
			StartSlide();
	}
	
	private void OnCrouchCanceled(InputAction.CallbackContext ctx)
	{
		StopSlide();
		StopCrouch();
	}
	
	private void Update()
	{
		UpdateAirborneState();
		UpdateSliding();
		Look();
		Move();
		
		lastVerticalVelocity = verticalVelocity
	}
	
	private void UpdateAirborneState()
	{		
		if (state == MovementState.Airborne && controller.isGrounded)
		{
			state = MovementState.Standing;
			OnStopFalling?.Invoke(lastVerticalVelocity);
		}
		
		if (state != MovementState.Airborne && !controller.isGrounded)
		{
			switch (state)
			{
				case MovementState.Crouching: StopCrouching(); break;
				case MovementState.Sliding: StopSliding(); break;
			}
			
			fallDir = controller.velocity;
			
			state = MovementState.Airborne;
			OnStartFalling?.Invoke();
		}
	}
	
	private void UpdateSliding()
	{
		if (state = MovementState.Sliding)
		{			
			timer -= Time.deltaTime;
			
			if (timer <= 0f)
				StopSlide();
		}
		else
			timer = timer > 0f ? timer - Time.deltaTime : 0f;
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
		Vector3 dir = state == MovementState.Sliding ? slideDir : ClampToUnit(transform.right * moveInput.x + transform.forward * moveInput.y);
	
		float speed = state == MovementState.Sliding ? GetSlidingVelocity(slideTimer, slideTime, moveSpeed, slideMulti, slideMax) : isCrouching ? moveSpeed * crouchMulti : moveSpeed;
		
		if (!controller.isGrounded)
			verticalVelocity += gravity * Time.deltaTime;
		else if (verticalVelocity < 0f)
			verticalVelocity = 0f;
		
		Vector3 horizontalVelocity = GetHorizontalVelocity(dir, speed, sidewaysDamping);
		
		Vector3 move = horizontalVelocity + Vector3.up * verticalVelocity;
		
		controller.Move(move * Time.deltaTime);
	}
	
	private Vector3 GetHorizontalVelocity(Vector3 direction, float speed, float damping)
	{
		Vector3 relForward = transform.forward;
		
		float alpha = Vector2.Angle( FlatVector3(relForward), FlatVector3(direction));
		
		return alpha < forwardAlignmentThreshold ? direction * speed : direction * speed * damping;
	}
	
	private Vector3 ClampToUnit(Vector3 v)
	{
		return Vector3.ClampMagnitude(v, 1f);
	}
	
	private Vector2 FlatVector3(Vector3 v)
	{
		return new Vector2(v.x, v.z);
	}
	
	private float Vector3ToHorizontalVelocity(Vector3 v)
	{
		v.y = 0;
		return v.magnitude;
	}
	
	private float GetSlidingVelocity(float t, float l, float vn, float m, float h)
    {
        float u = Mathf.Clamp01(t / l);

        if (h <= 0f || h >= 1f)
            return vn;

        // split curve at peak
        float rise;
        float fall;

        if (u < h)
        {
            // fast rise
            rise = Mathf.Pow(u / h, 2f);
            return vn + vn * (m - 1f) * rise;
        }
        else
        {
            // slow fall (delayed decay)
            float t2 = (u - h) / (1f - h);

            // ease-out cubic (slower drop)
            fall = 1f - Mathf.Pow(t2, 2.5f);

            return vn + vn * (m - 1f) * fall;
        }
    }
	
	private void StartCrouch()
	{
		if (state == MovementState.Sliding) return;
		
		state = MovementState.Crouching;
		
		controller.height = crouchingHeight;
		cam.transform.localPosition = crouchingCamPos;
		
		OnStartCrouch?.Invoke();
	}
	
	private void StopCrouch()
	{
		if (state != MovementState.Crouching) return;
		
		state = MovementState.Standing;
		
		controller.height = standingHeight;
		cam.transform.localPosition = standingCamPos;
		
		OnStopCrouch?.Invoke();
	}
	
	private void StartSlide()
	{
		if (state == MovementState.Crouching) return;
		
		if (!controller.isGrounded) return;
		
		if (slideCooldownTimer > 0f) return;
		
		state = MovementState.Sliding;
		
		timer = slideTime;
		
		slideDir = transform.forward; //Only slide in one direction no steering
		
		controller.height = slidingHeight;
		cam.transform.localPosition = slidingCamPos;
		
		OnStartSlide?.Invoke();
	}
	
	private void StopSlide()
	{
		if (state != MovementState.Sliding) return;
		
		state = MovementState.Standing;
		
		timer = slideCooldown;
		
		controller.height = standingHeight;
		cam.transform.localPosition = standingCamPos;
		
		OnStopSlide?.Invoke();
	}
}

enum MovementState
{
	Standing,
	Crouching,
	Sliding,
	Airborne
}