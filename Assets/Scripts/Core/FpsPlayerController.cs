using Unity.Netcode;
using UnityEngine;

public class FpsPlayerController : NetworkBehaviour
{
    [Header("Movement")]
    public float walkSpeed = 5f;
    public float sprintSpeed = 8f;
    public float acceleration = 12f;
    public float deceleration = 16f;
    [Range(0f, 1f)]
    public float airControl = 0.4f;

    public float gravity = -9.81f;
    public float jumpHeight = 1.5f;

    [Header("Mouse Look")]
    public float mouseSensitivity = 1000f;
    public Transform cameraTransform;

    [Header("ADS (Aim Down Sights)")]
    public bool enableADS = true;
    public float adsFov = 40f;
    public float adsLerpSpeed = 10f;
    [Range(0.1f, 1f)]
    public float adsSensitivityMultiplier = 0.5f;

    [Header("ADS Movement")]
    [Range(0.1f, 1f)]
    public float adsMoveSpeedMultiplier = 0.6f;

    [Header("Sprint")]
    public KeyCode sprintKey = KeyCode.LeftShift;

    [Header("Slide")]
    public float slideSpeed = 12f;
    public float slideDuration = 0.6f;
    public float slideCooldown = 0.8f;
    public float slideGravityMultiplier = 1.5f;
    [Range(0.3f, 1f)]
    public float slideHeightMultiplier = 0.6f;
    public float slideJumpMultiplier = 1.2f;

    [Header("Slide Audio")]
    public AudioSource movementAudioSource;
    public AudioClip slideStartClip;
    public AudioClip slideLoopClip;
    public AudioClip slideEndClip;
    [Range(0f, 1f)]
    public float slideLoopVolume = 0.7f;

    private CharacterController controller;
    private float verticalVelocity;
    private float xRotation = 0f;
    private Vector3 horizontalVelocity = Vector3.zero;

    // ADS camera
    private float baseFov = 60f;
    private bool isAiming = false;
    private Camera playerCamera;
    private AudioListener cameraAudioListener;

    // Slide state
    private bool isSliding = false;
    private float slideTimer = 0f;
    private float slideCooldownTimer = 0f;
    private Vector3 slideDirection;

    // Sprint toggle
    private bool isSprinting = false;

    // Controller original sizes
    private float originalControllerHeight;
    private Vector3 originalControllerCenter;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        originalControllerHeight = controller.height;
        originalControllerCenter = controller.center;
    }

    public override void OnNetworkSpawn()
    {
        if (cameraTransform != null)
        {
            playerCamera = cameraTransform.GetComponent<Camera>();
            cameraAudioListener = cameraTransform.GetComponent<AudioListener>();
        }

        if (IsOwner)
        {
            if (cameraTransform != null) cameraTransform.gameObject.SetActive(true);
            if (playerCamera != null)
            {
                baseFov = playerCamera.fieldOfView;
                playerCamera.enabled = true;
            }
            if (cameraAudioListener != null) cameraAudioListener.enabled = true;

            // Locked at match start, but we will unlock it in Update() if in lobby
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            if (cameraTransform != null)
                cameraTransform.gameObject.SetActive(true);

            if (playerCamera != null)
                playerCamera.enabled = false;

            if (cameraAudioListener != null)
                cameraAudioListener.enabled = false;
        }
    }

    void Update()
    {
        if (!IsOwner) return;

        // 🔥 NEW: Disable player control + unlock cursor when NOT in a round
        bool canControlPlayer = true;

        if (RoundManager.Instance != null)
            canControlPlayer = RoundManager.Instance.roundInProgress.Value;

        if (!canControlPlayer)
        {
            // LOBBY MODE
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            return; // do not process any movement or camera look
        }
        else
        {
            // MATCH MODE
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        HandleMouseLook();
        HandleMovement();
        HandleADS();
    }

    // ================= MOUSE LOOK =================

    void HandleMouseLook()
    {
        float currentSensitivity = mouseSensitivity;

        if (enableADS && isAiming)
            currentSensitivity *= adsSensitivityMultiplier;

        float mouseX = Input.GetAxis("Mouse X") * currentSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * currentSensitivity * Time.deltaTime;

        transform.Rotate(Vector3.up * mouseX);

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -80f, 80f);

        if (cameraTransform != null)
            cameraTransform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
    }

    // ================= ADS =================

    void HandleADS()
    {
        if (!enableADS || playerCamera == null)
            return;

        isAiming = Input.GetMouseButton(1);

        float targetFov = isAiming ? adsFov : baseFov;

        playerCamera.fieldOfView = Mathf.Lerp(
            playerCamera.fieldOfView,
            targetFov,
            Time.deltaTime * adsLerpSpeed
        );
    }

    // ================= MOVEMENT + SLIDE =================

    void HandleMovement()
    {
        bool isGrounded = controller.isGrounded;

        bool isAimingLocal = Input.GetMouseButton(1);

        if (slideCooldownTimer > 0f)
            slideCooldownTimer -= Time.deltaTime;

        float x = Input.GetAxisRaw("Horizontal");
        float z = Input.GetAxisRaw("Vertical");
        Vector3 inputDir = (transform.right * x + transform.forward * z).normalized;

        bool hasMoveInput = inputDir.sqrMagnitude > 0.01f;

        if (Input.GetKeyDown(sprintKey) && isGrounded && hasMoveInput)
            isSprinting = !isSprinting;

        if (!hasMoveInput || !isGrounded)
            isSprinting = false;

        if (!isSliding &&
            isGrounded &&
            isSprinting &&
            slideCooldownTimer <= 0f &&
            Input.GetKeyDown(KeyCode.LeftControl))
        {
            StartSlide(inputDir);
        }

        if (isSliding)
        {
            UpdateSlide(isGrounded);
            return;
        }

        float targetSpeed = isSprinting ? sprintSpeed : walkSpeed;

        if (isAimingLocal && enableADS)
            targetSpeed *= adsMoveSpeedMultiplier;

        Vector3 targetHorizontalVelocity = inputDir * targetSpeed;

        float lerpRate = hasMoveInput ? acceleration * Time.deltaTime : deceleration * Time.deltaTime;

        if (!isGrounded)
            lerpRate *= airControl;

        horizontalVelocity = Vector3.Lerp(horizontalVelocity, targetHorizontalVelocity, lerpRate);

        if (isGrounded && verticalVelocity < 0)
            verticalVelocity = -2f;

        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);

        verticalVelocity += gravity * Time.deltaTime;

        Vector3 finalVelocity = horizontalVelocity;
        finalVelocity.y = verticalVelocity;

        controller.Move(finalVelocity * Time.deltaTime);
    }

    // ================= SLIDE HELPERS =================

    void StartSlide(Vector3 inputDir)
    {
        isSliding = true;
        slideTimer = slideDuration;
        slideCooldownTimer = slideDuration + slideCooldown;

        Vector3 horiz = horizontalVelocity;
        horiz.y = 0f;
        if (horiz.magnitude < 0.1f)
            horiz = inputDir * sprintSpeed;

        slideDirection = horiz.normalized;

        controller.height = originalControllerHeight * slideHeightMultiplier;
        controller.center = originalControllerCenter * slideHeightMultiplier;

        if (movementAudioSource != null)
        {
            if (slideStartClip != null)
                movementAudioSource.PlayOneShot(slideStartClip);

            if (slideLoopClip != null)
            {
                movementAudioSource.clip = slideLoopClip;
                movementAudioSource.loop = true;
                movementAudioSource.volume = slideLoopVolume;
                movementAudioSource.Play();
            }
        }
    }

    void UpdateSlide(bool isGrounded)
    {
        slideTimer -= Time.deltaTime;

        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            horizontalVelocity = slideDirection * slideSpeed;
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity) * slideJumpMultiplier;

            StopSlide();
            return;
        }

        if (!isGrounded)
        {
            StopSlide();
            return;
        }

        float t = Mathf.Clamp01(slideTimer / slideDuration);
        float currentSlideSpeed = slideSpeed * (0.5f + 0.5f * t);

        Vector3 horiz = slideDirection * currentSlideSpeed;

        if (isGrounded && verticalVelocity < 0)
            verticalVelocity = -2f;

        verticalVelocity += gravity * slideGravityMultiplier * Time.deltaTime;

        Vector3 vel = horiz;
        vel.y = verticalVelocity;

        controller.Move(vel * Time.deltaTime);

        if (slideTimer <= 0f)
            StopSlide();
    }

    void StopSlide()
    {
        isSliding = false;
        controller.height = originalControllerHeight;
        controller.center = originalControllerCenter;

        if (movementAudioSource != null)
        {
            if (movementAudioSource.loop)
                movementAudioSource.Stop();

            if (slideEndClip != null)
                movementAudioSource.PlayOneShot(slideEndClip);
        }
    }
}
