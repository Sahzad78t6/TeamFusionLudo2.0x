using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class InteractionManager : MonoBehaviour
{
    [Header("Interaction Settings")]
    public float maxCheckDistance = 4.0f;
    public float checkRate = 0.02f;
    private float lastCheckTime;
    public LayerMask layerMask;

    [Header("Targeting")]
    public GameObject currentInteractGameObject;
    public IInteractable currentInteractable;

    [Header("UI")]
    public TextMeshProUGUI prompttext;
    public Camera cam;

    private void Awake()
    {
        ResolveCamera();
    }

    private void Start()
    {
        ResolveCamera();
    }

    private void ResolveCamera()
    {
        if (cam == null) cam = GetComponentInChildren<Camera>();
        if (cam == null && PlayerController.instance != null) cam = PlayerController.instance.GetComponentInChildren<Camera>();
        if (cam == null) cam = Camera.main;
    }

    private void Update()
    {
        ResolveCamera();

        // PC keyboard interaction fallback (E key)
        if (Input.GetKeyDown(KeyCode.E))
        {
            TriggerInteract();
        }

        if (Time.time - lastCheckTime > checkRate)
        {
            lastCheckTime = Time.time;
            UpdateTargeting();
        }
    }

    public void UpdateTargeting()
    {
        if (cam == null) return;

        // Shoot ray strictly from center of screen forward
        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        RaycastHit hit;
        float dist = maxCheckDistance > 0 ? maxCheckDistance : 4.0f;

        // Ignore Player (Layer 8) and Ignore Raycast (Layer 2)
        int ignoreMask = (1 << 8) | (1 << 2);
        int mask = layerMask.value != 0 ? (layerMask.value & ~ignoreMask) : ~ignoreMask;

        if (Physics.Raycast(ray, out hit, dist, mask))
        {
            // Check for IInteractable on the hit object or its parents
            IInteractable interactable = hit.collider.GetComponent<IInteractable>() ?? hit.collider.GetComponentInParent<IInteractable>();

            if (interactable != null)
            {
                currentInteractGameObject = hit.collider.gameObject;
                currentInteractable = interactable;
                SetPromptText();
                return;
            }
        }

        // Ray hit a non-interactable blocker or nothing: clear target immediately
        ClearTarget();
    }

    private void ClearTarget()
    {
        currentInteractGameObject = null;
        currentInteractable = null;
        if (prompttext != null)
        {
            prompttext.gameObject.SetActive(false);
        }
    }

    void SetPromptText()
    {
        if (prompttext == null || currentInteractable == null) return;
        prompttext.gameObject.SetActive(true);

        bool isMobile = Application.isMobilePlatform || (PlayerController.instance != null && PlayerController.instance.isMobileLookActive);
        if (isMobile)
        {
            prompttext.text = string.Format("<b>[TAP INTERACT]</b> {0}", currentInteractable.GetInteractPrompt());
        }
        else
        {
            prompttext.text = string.Format("<b>[E]</b> {0}", currentInteractable.GetInteractPrompt());
        }
    }

    public void OnInteractInput(InputAction.CallbackContext context)
    {
        if (context.phase == InputActionPhase.Started)
        {
            TriggerInteract();
        }
    }

    // Called by Mobile Interact Button
    public void TriggerInteractFromMobile()
    {
        TriggerInteract();
    }

    public void TriggerInteract()
    {
        // Re-verify targeting at the moment of click
        UpdateTargeting();

        // Strict Target Validation
        if (currentInteractable != null && currentInteractGameObject != null && currentInteractGameObject.activeInHierarchy)
        {
            float dist = maxCheckDistance > 0 ? maxCheckDistance : 4.0f;
            if (cam != null && Vector3.Distance(cam.transform.position, currentInteractGameObject.transform.position) > (dist + 2.0f))
            {
                ClearTarget();
                return;
            }

            // Interact ONLY with the verified object under the crosshair
            currentInteractable.OnInteract();

            // Clear and re-evaluate
            ClearTarget();
            UpdateTargeting();
        }
    }
}

public interface IInteractable
{
    string GetInteractPrompt();
    void OnInteract();
}