using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// ScopeCameraController — The real SK
// From tutorial video 40. The tutorial's code body was never shown;
// its Inspector fields are identical to CameraController, so this is
// a copy of CameraController that stays in sync with it.
// The ScopeCamera is a child of the main Camera, so it follows the
// main camera's rotation anyway; syncing rotationX/rotationY from the
// main CameraController keeps it correct even after the scope camera
// was disabled (unscoped) while the player kept looking around.
public class ScopeCameraController : MonoBehaviour
{
    public Transform playerTransform;
    public float sensitivity = 2f;
    // Kept in sync with CameraController (widened 2026-11-20, see there).
    public float minXAngle = -85f;
    public float maxXAngle = 85f;
    public float minYAngle = -360f;
    public float maxYAngle = 360f;
    public float smoothSpeed = 10f;

    public CameraController mainCameraController;

    private float rotationX = 0f;
    private float rotationY = 0f;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        if (mainCameraController != null)
        {
            rotationX = mainCameraController.GetRotationX();
            rotationY = mainCameraController.GetRotationY();
        }

        Quaternion targetRotation = Quaternion.Euler(rotationX, rotationY, 0);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, smoothSpeed * Time.deltaTime);
    }
}
