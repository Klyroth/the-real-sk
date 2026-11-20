using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// ScopeSystem — The real SK (sniper scope, right-click to toggle)
// From tutorial video 40.
// CORRECTION vs tutorial (deliberate — the tutorial's own final code
// is inconsistent): the tutorial disables the main camera's whole
// GameObject on scope-in (`mainCamera.gameObject.SetActive(false)`),
// which would also disable the ScopeCamera child, and never visibly
// re-enables it. Here only the main Camera COMPONENT is disabled
// (children keep working), and it is re-enabled on scope-out.
// The ScopeCamera is a child of the main Camera and follows its
// rotation automatically; ScopeCameraController keeps its rotation
// in sync with the main CameraController.
public class ScopeSystem : MonoBehaviour
{
    public GameObject scopeImage;
    public float zoomSpeed = 2f;
    public Camera mainCamera;
    public Camera scopeCamera;
    private bool isScoped = false;
    private float originalFOV;

    void Update()
    {
        if (Input.GetMouseButtonDown(1))
        {
            isScoped = !isScoped;
            scopeImage.SetActive(isScoped);

            if (isScoped)
            {
                mainCamera.enabled = false;

                scopeCamera.enabled = true;
                originalFOV = scopeCamera.fieldOfView;
                scopeCamera.fieldOfView /= zoomSpeed;
            }
            else
            {
                scopeCamera.enabled = false;
                scopeCamera.fieldOfView = originalFOV;
                mainCamera.enabled = true;
            }
        }

        if (isScoped)
        {
            float zoom = Input.GetAxis("Mouse ScrollWheel");
            scopeCamera.fieldOfView -= zoom * zoomSpeed * 10f;
            scopeCamera.fieldOfView = Mathf.Clamp(scopeCamera.fieldOfView, 10f, 80f);
        }
    }
}
