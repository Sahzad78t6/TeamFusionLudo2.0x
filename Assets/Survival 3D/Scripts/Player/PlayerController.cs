using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;


public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float sprintMultiplier = 1.6f;
    public float crouchMultiplier = 0.6f;
    private Vector2 currentMovementInput;
    public float jumpForce = 5f;
    public LayerMask groundLayerMask;

    [Header("Look")]
    public Transform cameraContainer;
    public float minXLook = -80f;
    public float maxXLook = 80f;
    private float camCurXRot;
    public float lookSensitivity = 1f;

    [Header("First Person Immersion")]
    public bool enableHeadBob = true;
    public float bobFrequency = 10f;
    public float bobHorizontalAmplitude = 0.03f;
    public float bobVerticalAmplitude = 0.04f;
    public float sprintFovBoost = 5f;

    private Vector2 mouseDelta;
    private Camera mainCamera;
    private float defaultFov = 60f;
    private float headBobTimer;
    private Vector3 defaultCameraPos;
    private bool wasGrounded;

    [HideInInspector] public bool isSprinting;
    [HideInInspector] public bool isCrouching;
    [HideInInspector] public bool canLook = true;
    [HideInInspector] public bool isMobileLookActive;

    private Rigidbody rig;
    private CapsuleCollider capsuleCollider;
    public static PlayerController instance;

    private void Awake()
    {
        rig = GetComponent<Rigidbody>();
        capsuleCollider = GetComponent<CapsuleCollider>();
        instance = this;
    }

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        if (Camera.main != null)
        {
            mainCamera = Camera.main;
            defaultFov = mainCamera.fieldOfView;
        }
        if (cameraContainer != null)
        {
            defaultCameraPos = cameraContainer.localPosition;
        }
    }

    void Update()
    {
        // PC keyboard movement fallback (WASD / Arrow keys)
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        if (Mathf.Abs(h) > 0.01f || Mathf.Abs(v) > 0.01f)
        {
            currentMovementInput = new Vector2(h, v);
        }

        // PC keyboard fallbacks for sprint, crouch, and jump
        if (Input.GetKeyDown(KeyCode.LeftShift)) isSprinting = true;
        if (Input.GetKeyUp(KeyCode.LeftShift)) isSprinting = false;

        if (Input.GetKeyDown(KeyCode.Space)) TriggerJump();

        if (Input.GetKeyDown(KeyCode.C) || Input.GetKeyDown(KeyCode.LeftControl))
        {
            ToggleCrouch();
        }

        UpdateHeadBobAndFOV();
    }

    void FixedUpdate()
    {
        Move();
    }

    private void LateUpdate()
    {
        if (canLook == true)
        {
            CameraLook();
        }
    }

    private void Move()
    {
        float speed = moveSpeed;
        if (isSprinting && currentMovementInput.magnitude > 0.05f)
        {
            speed *= sprintMultiplier;
        }
        else if (isCrouching)
        {
            speed *= crouchMultiplier;
        }

        Vector3 dir = transform.forward * currentMovementInput.y + transform.right * currentMovementInput.x;
        dir *= speed;
        dir.y = rig.linearVelocity.y;

        rig.linearVelocity = dir;
    }

    private void CameraLook()
    {
        Vector2 delta = mouseDelta;
        if (delta.sqrMagnitude < 0.0001f && canLook)
        {
            float mx = Input.GetAxis("Mouse X");
            float my = Input.GetAxis("Mouse Y");
            delta = new Vector2(mx, my);
        }

        camCurXRot += delta.y * lookSensitivity;
        camCurXRot = Mathf.Clamp(camCurXRot, minXLook, maxXLook);

        if (cameraContainer != null)
        {
            Vector3 localRot = new Vector3(-camCurXRot, 0, 0);
            cameraContainer.localEulerAngles = localRot;
        }

        transform.eulerAngles += new Vector3(0, delta.x * lookSensitivity, 0);
        mouseDelta = Vector2.zero;
    }

    public void SetMobileMoveInput(Vector2 moveInput)
    {
        currentMovementInput = moveInput;
    }

    public void SetMobileLookInput(Vector2 lookInput)
    {
        mouseDelta = lookInput;
    }

    public void SetSprint(bool sprinting)
    {
        isSprinting = sprinting;
    }

    public void ToggleCrouch()
    {
        isCrouching = !isCrouching;
        if (capsuleCollider != null)
        {
            capsuleCollider.height = isCrouching ? 1.2f : 2.0f;
            capsuleCollider.center = isCrouching ? new Vector3(0, 0.6f, 0) : new Vector3(0, 1.0f, 0);
        }
    }

    public void TriggerJump()
    {
        if (isGrounded())
        {
            if (isCrouching) ToggleCrouch();
            rig.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }
    }

    public void OnLookInput(InputAction.CallbackContext context)
    {
        mouseDelta = context.ReadValue<Vector2>();
    }

    public void OnMoveInput(InputAction.CallbackContext context)
    {
        if (context.phase == InputActionPhase.Performed)
        {   
            currentMovementInput = context.ReadValue<Vector2>();
        }
        else if (context.phase == InputActionPhase.Canceled)
        {   
            currentMovementInput = Vector2.zero;
        }
    }

    public void OnJumpInput(InputAction.CallbackContext context)
    {
        if (context.phase == InputActionPhase.Started)
        {
            TriggerJump();
        }
    }

    public bool isGrounded()
    {
        LayerMask mask = groundLayerMask.value != 0 ? groundLayerMask : ~0;
        Ray[] rays = new Ray[4]
        {
            new Ray (transform.position + (transform.forward * 0.2f) + (Vector3.up * 0.05f), Vector3.down),
            new Ray(transform.position + (-transform.forward * 0.2f) + (Vector3.up * 0.05f), Vector3.down),
            new Ray(transform.position + (transform.right * 0.2f) + (Vector3.up * 0.05f), Vector3.down),
            new Ray(transform.position + (-transform.right * 0.2f) + (Vector3.up * 0.05f), Vector3.down)
        };
        for (int i = 0; i < rays.Length; i++)
        {
            if (Physics.Raycast(rays[i], 0.35f, mask))
            {
                return true;
            }
        }
        return false;
    }

    private void UpdateHeadBobAndFOV()
    {
        bool grounded = isGrounded();
        
        // Head bob logic when moving on ground
        if (enableHeadBob && cameraContainer != null)
        {
            float speedMag = new Vector3(rig.linearVelocity.x, 0, rig.linearVelocity.z).magnitude;
            if (grounded && speedMag > 0.2f)
            {
                float freq = isSprinting ? bobFrequency * 1.4f : bobFrequency;
                headBobTimer += Time.deltaTime * freq;
                float xOffset = Mathf.Cos(headBobTimer * 0.5f) * bobHorizontalAmplitude;
                float yOffset = Mathf.Sin(headBobTimer) * bobVerticalAmplitude;
                float crouchYOffset = isCrouching ? -0.4f : 0f;
                cameraContainer.localPosition = defaultCameraPos + new Vector3(xOffset, yOffset + crouchYOffset, 0);
            }
            else
            {
                headBobTimer = 0f;
                float crouchYOffset = isCrouching ? -0.4f : 0f;
                cameraContainer.localPosition = Vector3.Lerp(cameraContainer.localPosition, defaultCameraPos + new Vector3(0, crouchYOffset, 0), Time.deltaTime * 8f);
            }
        }

        // Camera FOV lerp on sprint
        if (mainCamera != null)
        {
            float targetFov = (isSprinting && currentMovementInput.y > 0.1f) ? defaultFov + sprintFovBoost : defaultFov;
            mainCamera.fieldOfView = Mathf.Lerp(mainCamera.fieldOfView, targetFov, Time.deltaTime * 6f);
        }

        wasGrounded = grounded;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position + (transform.forward * 0.2f), Vector3.down);
        Gizmos.DrawRay(transform.position + (-transform.forward * 0.2f),Vector3.down);
        Gizmos.DrawRay(transform.position + (transform.right * 0.2f),Vector3.down);
        Gizmos.DrawRay(transform.position + (-transform.right * 0.2f),Vector3.down);
    }

    public void ToggleCursor(bool toggle)
    {
        Cursor.lockState = toggle ? CursorLockMode.None : CursorLockMode.Locked;
        canLook = !toggle;
    }
}
