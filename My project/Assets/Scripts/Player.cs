using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class FirstPersonController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float walkSpeed = 8f;
    public float runSpeed = 14f;
    public float acceleration = 80f;
    public float deceleration = 90f;
    public float airControl = 0.25f;
    public float jumpHeight = 12f;
    public float gravity = -35f;
    public float jumpCutMultiplier = 2.5f;

    [Header("Sliding Settings")]
    public float slideInitialSpeed = 10f;
    public float slideMaxSpeed = 16f;
    public float slideAcceleration = 20f;
    public float slideDeceleration = 18f;
    public float slideTransitionSpeed = 18f;
    public float slideDuration = 1.0f;
    public float slideMinSpeed = 3f;
    public float slideControllerHeight = 1f;
    public Vector3 slideCameraOffset = new Vector3(0, -0.8f, 0);
    public float slideMomentumRetention = 0.3f;
    public float slideJumpHeightMultiplier = 1.2f;
    public float slideJumpHorizontalBoost = 1.4f;
    public float slideEndGracePeriod = 0.15f;
    public float slideSteerStrength = 0.4f;
    public float slideGroundStick = 10f;

    [Header("Ground Settings")]
    public float groundStickForce = 2f;
    public float slopeLimit = 45f;

    [Header("Mouse Look Settings")]
    public float mouseSensitivity = 2f;
    public float maxLookAngle = 80f;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundDistance = 0.4f;
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

    private Vector3 velocity;
    private Vector3 currentMovement;
    private bool isGrounded;
    private bool wasGrounded;
    private bool isRunning;
    private bool jumpPressed;
    private bool jumpReleased;
    private bool slidePressed;
    private bool slideHeld;

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
            try
            {
                moveAction = playerInput.actions["Move"];
                lookAction = playerInput.actions["Look"];
                jumpAction = playerInput.actions["Jump"];
                runAction = playerInput.actions["Run"];
                slideAction = playerInput.actions["Slide"];
            }
            catch (System.Exception e)
            {
                Debug.LogError("Input actions not found: " + e.Message);
            }
        }
        else
        {
            Debug.LogWarning("PlayerInput component missing or input actions not assigned");
        }
    }

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        if (playerCamera != null)
        {
            originalCameraPos = playerCamera.transform.localPosition;
            slideCameraPos = originalCameraPos + slideCameraOffset;
        }

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && enableStepSounds) audioSource = gameObject.AddComponent<AudioSource>();

        if (groundCheck == null)
        {
            GameObject groundCheckObj = new GameObject("GroundCheck");
            groundCheckObj.transform.SetParent(transform);
            groundCheckObj.transform.localPosition = new Vector3(0, -1f, 0);
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
    }

    void HandleInput()
    {
        if (jumpAction != null)
        {
            if (jumpAction.WasPressedThisFrame())
                jumpPressed = true;
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
        {
            slideEndTimer -= Time.deltaTime;
        }

        if (slidePressed && isGrounded && !isSliding && currentMovement.magnitude > slideMinSpeed)
        {
            StartSlide();
        }

        if (isSliding)
        {
            slideTimer -= Time.deltaTime;

            Vector2 moveInput = Vector2.zero;
            if (moveAction != null)
                moveInput = moveAction.ReadValue<Vector2>();
            float slideProgress = (slideDuration - slideTimer) / slideDuration;

            float speedCurve;
            if (slideProgress < 0.2f)
            {
                speedCurve = Mathf.Lerp(0.9f, 1.1f, slideProgress / 0.2f);
            }
            else if (slideProgress < 0.5f)
            {
                speedCurve = 1.1f;
            }
            else
            {
                float decelPhase = (slideProgress - 0.5f) / 0.5f;
                speedCurve = Mathf.Lerp(1.1f, 0.3f, decelPhase);
            }

            currentSlideSpeed = Mathf.Lerp(slideInitialSpeed, slideMaxSpeed, speedCurve);

            Vector3 steerDirection = (transform.right * moveInput.x).normalized;
            slideDirection = Vector3.Slerp(slideDirection,
                (slideDirection + steerDirection * slideSteerStrength).normalized,
                Time.deltaTime * 3.5f);

            Vector3 targetSlideMovement = slideDirection * currentSlideSpeed;
            currentMovement = Vector3.Lerp(currentMovement, targetSlideMovement,
                slideAcceleration * Time.deltaTime);

            slideMomentum = currentMovement * slideMomentumRetention;

            if (isGrounded && velocity.y <= 0)
            {
                velocity.y = -slideGroundStick;
            }

            bool shouldEndSlide = slideTimer <= 0f ||
                                 currentMovement.magnitude < slideMinSpeed * 0.5f ||
                                 !isGrounded ||
                                 (!slideHeld && slideTimer < slideDuration * 0.7f); 

            if (shouldEndSlide)
            {
                StopSlide();
            }

            if (playerCamera != null)
            {
                Vector3 targetCameraPos = isSliding ? slideCameraPos : originalCameraPos;
                playerCamera.transform.localPosition = Vector3.Lerp(
                    playerCamera.transform.localPosition,
                    targetCameraPos,
                    slideTransitionSpeed * Time.deltaTime
                );
            }
        }

        slidePressed = false;
    }

    void StartSlide()
    {
        isSliding = true;
        slideTimer = slideDuration;
        slideDirection = currentMovement.normalized;
        slideEndTimer = 0f;

        float initialSpeedMultiplier = Mathf.Clamp(currentMovement.magnitude / runSpeed, 0.8f, 1.2f);
        currentSlideSpeed = slideInitialSpeed * initialSpeedMultiplier;

        controller.height = slideControllerHeight;
        controller.center = new Vector3(originalControllerCenter.x,
            slideControllerHeight * 0.5f, originalControllerCenter.z);

        Debug.Log($"Started sliding with initial speed: {currentSlideSpeed}");
    }

    void StopSlide()
    {
        if (!isSliding) return;

        if (CanStandUp())
        {
            isSliding = false;
            slideEndTimer = slideEndGracePeriod;
            controller.height = originalControllerHeight;
            controller.center = originalControllerCenter;

            if (playerCamera != null)
            {
                playerCamera.transform.localPosition = Vector3.Lerp(
                    playerCamera.transform.localPosition,
                    originalCameraPos,
                    slideTransitionSpeed * Time.deltaTime
                );
            }

            Debug.Log("Stopped sliding - momentum jump available for " + slideEndGracePeriod + " seconds");
        }
        else
        {
            slideTimer = 0.1f;
        }
    }

    bool CanStandUp()
    {
        Vector3 bottom = transform.position + controller.center - Vector3.up * (originalControllerHeight * 0.5f);
        Vector3 top = transform.position + controller.center + Vector3.up * (originalControllerHeight * 0.5f);

        return !Physics.CheckCapsule(bottom, top, controller.radius * 0.9f, groundMask);
    }

    void HandleGroundCheck()
    {
        wasGrounded = isGrounded;
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);

        if (isGrounded)
        {
            coyoteTimeCounter = coyoteTime;
        }
        else
        {
            coyoteTimeCounter -= Time.deltaTime;
        }

        if (isGrounded && velocity.y <= 0)
        {
            velocity.y = -groundStickForce;
        }
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

        Vector2 moveInput = Vector2.zero;
        if (moveAction != null)
            moveInput = moveAction.ReadValue<Vector2>();

        isRunning = runAction != null && runAction.IsPressed();
        Vector3 targetMovement = transform.right * moveInput.x + transform.forward * moveInput.y;
        targetMovement = targetMovement.normalized;
        float currentSpeed = isRunning ? runSpeed : walkSpeed;
        targetMovement *= currentSpeed;

        float airControlMultiplier = (slideEndTimer > 0f && !isGrounded) ? 0.6f : 1f;
        float currentAcceleration = isGrounded ?
            (targetMovement.magnitude > 0 ? acceleration : deceleration) :
            acceleration * airControl * airControlMultiplier;

        float smoothTime = 1f / currentAcceleration;
        currentMovement = Vector3.Lerp(currentMovement, targetMovement, Time.deltaTime / smoothTime);
        controller.Move(currentMovement * Time.deltaTime);
    }

    void HandleJump()
    {
        if (jumpPressed && isSliding && isGrounded)
        {
            StopSlide();
        }

        if (jumpPressed && coyoteTimeCounter > 0f)
        {
            float jumpVelocityY = Mathf.Sqrt(jumpHeight * -2f * gravity);

            if (slideEndTimer > 0f && isGrounded)
            {
                jumpVelocityY *= slideJumpHeightMultiplier;
                Vector3 horizontalMomentum = new Vector3(slideMomentum.x, 0, slideMomentum.z) * slideJumpHorizontalBoost;
                currentMovement += horizontalMomentum;
                slideEndTimer = 0f; 
                slideMomentum = Vector3.zero; 

                Debug.Log($"Momentum jump! Height: {jumpVelocityY}, Horizontal boost: {horizontalMomentum.magnitude}");
            }

            velocity.y = jumpVelocityY;
            coyoteTimeCounter = 0f; 
        }

        if (jumpReleased && velocity.y > 0)
        {
            velocity.y *= 1f / jumpCutMultiplier;
        }

        velocity.y += gravity * Time.deltaTime;

        float maxFallSpeed = gravity * 1.5f;
        if (velocity.y < maxFallSpeed)
            velocity.y = maxFallSpeed;

        controller.Move(new Vector3(0, velocity.y * Time.deltaTime, 0));
        jumpPressed = false;
        jumpReleased = false;
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
                playerCamera.transform.localPosition = Vector3.Lerp(
                    playerCamera.transform.localPosition,
                    originalCameraPos,
                    Time.deltaTime * 6f
                );
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
                    PlayFootstep();
                    float speedMultiplier = isRunning ? 0.7f : 1f;
                    stepTimer = stepInterval * speedMultiplier;
                }
            }
        }
    }

    void PlayFootstep()
    {
        if (footstepSounds.Length > 0)
        {
            AudioClip clip = footstepSounds[Random.Range(0, footstepSounds.Length)];
            float volume = isRunning ? 0.8f : 0.6f;
            audioSource.PlayOneShot(clip, volume);
        }
    }

    void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = isGrounded ? Color.green : Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, groundDistance);
        }

        if (isSliding && Application.isPlaying)
        {
            Gizmos.color = Color.yellow;
            Vector3 bottom = transform.position + controller.center - Vector3.up * (originalControllerHeight * 0.5f);
            Vector3 top = transform.position + controller.center + Vector3.up * (originalControllerHeight * 0.5f);
            Gizmos.DrawWireSphere(bottom, controller.radius);
            Gizmos.DrawWireSphere(top, controller.radius);
            Gizmos.DrawLine(bottom, top);
        }
    }

    public void ToggleCursorLock()
    {
        if (Cursor.lockState == CursorLockMode.Locked)
        {
            Cursor.lockState = CursorLockMode.None;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
        }
    }

    public void SetCursorLock(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
    }

    public bool IsSliding => isSliding;
    public float SlideTimeRemaining => slideTimer;
    public float CurrentSlideSpeed => currentSlideSpeed;
}