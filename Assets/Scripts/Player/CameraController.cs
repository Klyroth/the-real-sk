using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// CameraController — The real SK
// From tutorial videos 09 (fields + cursor lock), 10 (mouse-look Update),
// 11 (player rotates with camera — one Slerp line).
// Adaptations vs tutorial:
//  - touchLookDelta: Android right-half drag writes here, consumed each
//    frame (zero on PC, so PC behaviour is unchanged). Phase 11.
//  - LoadSettings() reads saved SmoothSpeed/Sensitivity from PlayerPrefs;
//    the tutorial calls it from Update() every frame (video 49) — here it
//    is called once from Start() (same result, no per-frame cost).
public class CameraController : MonoBehaviour
{
    public Transform playerTransform;
    public float sensitivity = 2f;
    public float minXAngle = -30f;
    public float maxXAngle = 30f;
    public float minYAngle = -360f;
    public float maxYAngle = 360f;
    public float smoothSpeed = 10f;

    private float rotationX = 0f;
    private float rotationY = 0f;

    // Android touch look (written by TouchControls, consumed each frame)
    public static Vector2 touchLookDelta;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        LoadSettings();
    }

    void Update()
    {
        float mouseX = Input.GetAxis("Mouse X") * sensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * sensitivity;

        mouseX += touchLookDelta.x * sensitivity;
        mouseY += touchLookDelta.y * sensitivity;
        touchLookDelta = Vector2.zero;

        rotationX -= mouseY;
        rotationY += mouseX;

        // Clamp rotation values
        rotationX = Mathf.Clamp(rotationX, minXAngle, maxXAngle);
        rotationY = Mathf.Clamp(rotationY, minYAngle, maxYAngle);

        Quaternion targetRotation = Quaternion.Euler(rotationX, rotationY, 0);

        if (playerTransform != null)
        {
            playerTransform.rotation = Quaternion.Slerp(playerTransform.rotation, targetRotation, smoothSpeed * Time.deltaTime);
        }
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, smoothSpeed * Time.deltaTime);
    }

    public void LoadSettings()
    {
        if (PlayerPrefs.HasKey("SmoothSpeed"))
        {
            smoothSpeed = PlayerPrefs.GetFloat("SmoothSpeed");
        }
        if (PlayerPrefs.HasKey("Sensitivity"))
        {
            sensitivity = PlayerPrefs.GetFloat("Sensitivity");
        }
    }

    public float GetRotationX()
    {
        return rotationX;
    }

    public float GetRotationY()
    {
        return rotationY;
    }
}
