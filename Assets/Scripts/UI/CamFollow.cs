using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Obsolete("已被弃用， 新版采用遮罩实现效果")]
public class CamFollow : MonoBehaviour
{
    private Camera assignedCamera; // Assign the camera in the Inspector
    private Camera mainCamera;

    void Update()
    {
        FollowMouse();
    }

    private void Start()
    {
        assignedCamera = GetComponent<Camera>();
        mainCamera = Camera.main;
    }

    private void FollowMouse()
    {
        if (assignedCamera == null)
        {
            Debug.LogWarning("Assigned camera is not set.");
            return;
        }

        // Get the mouse position in world space
        Vector3 mousePosition = mainCamera.ScreenToWorldPoint(new Vector3(Input.mousePosition.x,
            Input.mousePosition.y, assignedCamera.nearClipPlane));

        // Smoothly move the camera towards the mouse position
        Vector3 targetPosition = new Vector3(mousePosition.x, mousePosition.y, transform.position.z);
        transform.position = targetPosition;
    }
}
