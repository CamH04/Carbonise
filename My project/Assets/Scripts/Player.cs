using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class Player : MonoBehaviour
{
    [Header("Movement Settings")]
    public float walkSpeed = 8f;
    public float runSpeed = 14f;
    public float acceleration = 80f;
    public float deceleration = 90f;
    public float airControl = 0.25f;
    public float jumpHeight = 12f;
    public float gravity = -200f;
    public float jumpCutMultiplier = 2.5f;

    [Header("Sliding Settings")]
    public float slideInitialSpeed = 10f;
    public float slideMaxSpeed = 16f;
    public float slideAcceleration = 20f;
    public float slideDeceleration = 8f;
    public float slideTransitionSpeed = 18f;
    public float slideDuration = 1.0f;
    public float slideMinSpeed = 3f;
    public float slideControllerHeight = 1f;
    public Vector3 slideCameraOffset = new Vector3(0, -0.8f, 0);
    public float slideMomentumRetention = 0.5f;
    public float slideJumpHeightMultiplier = 1.2f;
    public float slideJumpHorizontalBoost = 1.4f;
    public float slideEndGracePeriod = 0.15f;
    public float slideSteerStrength = 0.4f;
    public float slideGroundStick = 10f;

    [Header("Ground Settings")]
    public float groundStickForce = 0.5f;
    public float slopeLimit = 45f;

    [Header("Mouse Look Settings")]
    public float mouseSensitivity = 2f;
    public float maxLookAngle = 80f;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundDistance = 1f;
    public LayerMask groundMask;

    [Header("3D Game Features")]
    public bool enableHeadBob = true;
    public float bobFrequency = 2f;
    public float bobAmplitude = 0.1f;
    public bool enableStepSounds = false;
    public AudioClip[] footstepSounds;
    public float stepInterval = 0.5f;

    private CharacterController controller;
    private Camera playerCamera;
    private AudioSource audioSource;

    private PlayerInput playerInput;
    private InputAction moveAction;
    private InputAction lookAction;
    private InputAction jumpAction;
    private InputAction runAction;
    private InputAction slideAction;


    [Header("Jump Tuning")]
    public float jumpBufferTime = 0.1f;  
    private float jumpBufferCounter;
    private Vector3 velocity;
    private Vector3 currentMovement;
    private bool isGrounded;
    private bool wasGrounded;
    private bool isRunning;
    private bool jumpPressed;
    private bool jumpReleased;
    private bool slidePressed;
    private bool slideHeld;
    bool canJump;


    private bool isSliding;
    private float slideTimer;
    private Vector3 slideDirection;
    private float currentSlideSpeed;
    private float originalControllerHeight;
    private Vector3 originalControllerCenter;
    private float slideEndTimer;
    private Vector3 slideMomentum;

    private Vector2 mouseDelta;
    private float xRotation = 0f;

    private Vector3 originalCameraPos;
    private Vector3 slideCameraPos;
    private float bobTimer;
    private float stepTimer;
    private float coyoteTime = 0.15f;
    private float coyoteTimeCounter;
    private Vector3 cameraVelocity;
    private float cameraSmoothTime = 0.08f;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        playerCamera = GetComponentInChildren<Camera>();
        if (playerCamera == null) playerCamera = Camera.main;
        playerInput = GetComponent<PlayerInput>();
        originalControllerHeight = controller.height;
        originalControllerCenter = controller.center;

        if (playerInput != null && playerInput.actions != null)
        {
            moveAction = playerInput.actions["Move"];
            lookAction = playerInput.actions["Look"];
            jumpAction = playerInput.actions["Jump"];
            runAction = playerInput.actions["Run"];
            slideAction = playerInput.actions["Slide"];
        }
    }

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        originalCameraPos = playerCamera.transform.localPosition;
        slideCameraPos = originalCameraPos + slideCameraOffset;

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && enableStepSounds) audioSource = gameObject.AddComponent<AudioSource>();

        if (groundCheck == null)
        {
            GameObject groundCheckObj = new GameObject("GroundCheck");
            groundCheckObj.transform.SetParent(transform);
            float bottomOffset = -controller.height * 0.5f - 0.1f;
            groundCheckObj.transform.localPosition = new Vector3(0, bottomOffset, 0);
            groundCheck = groundCheckObj.transform;
        }
    }

    void Update()
    {
        HandleInput();
        HandleGroundCheck();
        HandleMouseLook();
        HandleSliding();
        HandleMovement();
        HandleJump();
        Handle3DFeatures();
        HandleStuckRecovery();
    }

    void HandleInput()
    {
        if (jumpAction != null)
        {
            if (jumpAction.WasPressedThisFrame())
            {
                jumpBufferCounter = jumpBufferTime;
            }

            if (jumpAction.WasReleasedThisFrame())
                jumpReleased = true;
        }

        if (slideAction != null)
        {
            if (slideAction.WasPressedThisFrame())
                slidePressed = true;
            slideHeld = slideAction.IsPressed();
        }
    }


    void HandleSliding()
    {
        if (slideEndTimer > 0f)
            slideEndTimer -= Time.deltaTime;

        if (slidePressed && isGrounded && !isSliding && slideEndTimer <= 0f && currentMovement.magnitude > slideMinSpeed)
            StartSlide();

        if (isSliding)
        {
            slideTimer -= Time.deltaTime;
            currentSlideSpeed = Mathf.MoveTowards(currentSlideSpeed, 0f, slideDeceleration * Time.deltaTime);

            Vector2 moveInput = moveAction != null ? moveAction.ReadValue<Vector2>() : Vector2.zero;
            Vector3 steerDirection = (transform.right * moveInput.x + transform.forward * moveInput.y).normalized;
            slideDirection = Vector3.Slerp(slideDirection, steerDirection == Vector3.zero ? slideDirection : steerDirection, slideSteerStrength * Time.deltaTime);
            currentMovement = slideDirection * currentSlideSpeed;

            if (isGrounded)
                velocity.y = -slideGroundStick;

            bool shouldEndSlide =
                slideTimer <= 0f ||
                !isGrounded ||
                (currentSlideSpeed < slideMinSpeed && slideTimer < slideDuration * 0.6f) ||
                (!slideHeld && slideTimer < slideDuration * 0.7f);

            if (shouldEndSlide)
                StopSlide();

            if (playerCamera != null)
                playerCamera.transform.localPosition = Vector3.SmoothDamp(playerCamera.transform.localPosition, slideCameraPos, ref cameraVelocity, cameraSmoothTime);
        }

        slidePressed = false;
    }

    void StartSlide()
    {
        isSliding = true;
        slideTimer = slideDuration;
        slideDirection = currentMovement.normalized;
        slideEndTimer = 0f;

        currentSlideSpeed = Mathf.Clamp(currentMovement.magnitude, slideInitialSpeed, slideMaxSpeed);
        slideMomentum = slideDirection * currentSlideSpeed * slideMomentumRetention;
        float heightDiff = originalControllerHeight - slideControllerHeight;
        Vector3 newCenter = new Vector3(originalControllerCenter.x, originalControllerCenter.y - (heightDiff * 0.5f), originalControllerCenter.z);
        controller.height = slideControllerHeight;
        controller.center = newCenter;
        controller.Move(Vector3.up * (heightDiff * 0.5f));
    }

    void StopSlide()
    {
        if (!isSliding) return;

        isSliding = false;
        slideEndTimer = slideEndGracePeriod;
        if (CanStandUp())
        {
            float heightDiff = originalControllerHeight - slideControllerHeight;
            controller.height = originalControllerHeight;
            controller.center = originalControllerCenter;
            controller.Move(Vector3.down * (heightDiff * 0.5f));
        }
        /*
        else
        {
            Debug.Log("Cannot stand up, staying crouched");
        }
        */
        if (playerCamera != null)
        {
            Vector3 targetCameraPos = (controller.height >= originalControllerHeight) ? originalCameraPos : slideCameraPos;
            playerCamera.transform.localPosition = Vector3.SmoothDamp(playerCamera.transform.localPosition, targetCameraPos, ref cameraVelocity, cameraSmoothTime);
        }
    }

    bool CanStandUp()
    {
        Vector3 currentBottom = transform.position + controller.center - Vector3.up * (controller.height * 0.5f);
        Vector3 futureTop = currentBottom + Vector3.up * originalControllerHeight;
        return !Physics.CheckCapsule(currentBottom, futureTop, controller.radius * 0.95f, groundMask);
    }

    void HandleGroundCheck()
    {
        wasGrounded = isGrounded;
        float rayDistance = 2.5f; // kill me
        isGrounded = Physics.Raycast(transform.position, Vector3.down, rayDistance, groundMask);

        //Debug.Log($"Player Y: {transform.position.y:F2}, Ground Hit: {isGrounded}, RayDistance: {rayDistance}");

        if (isGrounded)
            coyoteTimeCounter = coyoteTime;
        else
            coyoteTimeCounter = Mathf.Max(0f, coyoteTimeCounter - Time.deltaTime);

        if (isGrounded && velocity.y <= 0)
            velocity.y = -groundStickForce;
    }



    void HandleMouseLook()
    {
        if (lookAction != null)
        {
            mouseDelta = lookAction.ReadValue<Vector2>();
            float mouseX = mouseDelta.x * mouseSensitivity * Time.deltaTime;
            float mouseY = mouseDelta.y * mouseSensitivity * Time.deltaTime;
            transform.Rotate(Vector3.up * mouseX);
            xRotation -= mouseY;
            xRotation = Mathf.Clamp(xRotation, -maxLookAngle, maxLookAngle);
            if (playerCamera != null)
                playerCamera.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        }
    }

    void HandleMovement()
    {
        if (isSliding)
        {
            controller.Move(currentMovement * Time.deltaTime);
            return;
        }

        Vector2 moveInput = moveAction != null ? moveAction.ReadValue<Vector2>() : Vector2.zero;
        isRunning = runAction != null && runAction.IsPressed();
        Vector3 targetMovement = (transform.right * moveInput.x + transform.forward * moveInput.y).normalized;
        float currentSpeed = isRunning ? runSpeed : walkSpeed;
        targetMovement *= currentSpeed;

        float airControlMultiplier = (slideEndTimer > 0f && !isGrounded) ? 0.6f : 1f;
        float currentAcceleration = isGrounded ? (targetMovement.magnitude > 0 ? acceleration : deceleration) : acceleration * airControl * airControlMultiplier;

        float smoothTime = 1f / currentAcceleration;
        currentMovement = Vector3.Lerp(currentMovement, targetMovement, Time.deltaTime / smoothTime);
        controller.Move(currentMovement * Time.deltaTime);
    }

    void HandleJump()
    {
        canJump = isGrounded || coyoteTimeCounter > 0f;
        /*
        if (jumpBufferCounter > 0f)
        {
            Debug.Log($"JUMP ATTEMPT - Grounded: {isGrounded}, Coyote: {coyoteTimeCounter:F2}, CanJump: {canJump}, Buffer: {jumpBufferCounter:F2}");
        }
        */
        if (jumpBufferCounter > 0f && canJump)
        {
            if (isSliding && isGrounded)
                StopSlide();

            float jumpVelocityY = Mathf.Sqrt(jumpHeight * -2f * gravity);

            if (slideEndTimer > 0f && isGrounded)
            {
                jumpVelocityY *= slideJumpHeightMultiplier;
                Vector3 horizontalMomentum = new Vector3(slideMomentum.x, 0, slideMomentum.z) * slideJumpHorizontalBoost;
                currentMovement += horizontalMomentum;
                slideEndTimer = 0f;
                slideMomentum = Vector3.zero;
            }

            velocity.y = jumpVelocityY;

            coyoteTimeCounter = 0f;
            jumpBufferCounter = 0f;
        }

        if (jumpReleased && velocity.y > 0)
        {
            velocity.y *= 1f / jumpCutMultiplier;
        }

        velocity.y += gravity * Time.deltaTime;

        controller.Move(new Vector3(0, velocity.y * Time.deltaTime, 0));

        jumpReleased = false;

        if (jumpBufferCounter > 0f)
            jumpBufferCounter -= Time.deltaTime;
    }


    void Handle3DFeatures()
    {
        if (enableHeadBob && playerCamera != null && !isSliding)
        {
            bool isMoving = currentMovement.magnitude > 0.1f && isGrounded;
            if (isMoving)
            {
                float speedMultiplier = isRunning ? 1.3f : 1f;
                bobTimer += Time.deltaTime * bobFrequency * speedMultiplier;
                float bobOffset = Mathf.Sin(bobTimer) * bobAmplitude * (currentMovement.magnitude / walkSpeed);
                playerCamera.transform.localPosition = originalCameraPos + Vector3.up * bobOffset;
            }
            else
            {
                bobTimer = 0f;
                playerCamera.transform.localPosition = Vector3.Lerp(playerCamera.transform.localPosition, originalCameraPos, Time.deltaTime * 6f);
            }
        }

        if (enableStepSounds && audioSource != null && footstepSounds != null && footstepSounds.Length > 0 && !isSliding)
        {
            bool isMoving = currentMovement.magnitude > 0.1f && isGrounded;
            if (isMoving)
            {
                stepTimer -= Time.deltaTime;
                if (stepTimer <= 0f)
                {
                    AudioClip clip = footstepSounds[Random.Range(0, footstepSounds.Length)];
                    float volume = isRunning ? 0.8f : 0.6f;
                    audioSource.PlayOneShot(clip, volume);
                    float speedMultiplier = isRunning ? 0.7f : 1f;
                    stepTimer = stepInterval * speedMultiplier;
                }
            }
        }
    }
    void HandleStuckRecovery()
    {
        if (!isSliding && controller.height < originalControllerHeight && isGrounded)
        {
            if (CanStandUp())
            {
                float heightDiff = originalControllerHeight - controller.height;
                controller.height = originalControllerHeight;
                controller.center = originalControllerCenter;
                controller.Move(Vector3.down * (heightDiff * 0.5f));

                if (playerCamera != null)
                {
                    playerCamera.transform.localPosition = Vector3.SmoothDamp(playerCamera.transform.localPosition, originalCameraPos, ref cameraVelocity, cameraSmoothTime);
                }
            }
        }
    }
}