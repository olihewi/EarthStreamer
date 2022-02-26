using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimpleCamera : MonoBehaviour
{
    public float speed = 30.0F;
    public float cameraSensitivity = 90.0F;
    private Vector2 cameraRot;

    private void Start()
    {
        cameraRot = transform.rotation.eulerAngles;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.C)) Cursor.lockState = Cursor.lockState == CursorLockMode.Locked ? CursorLockMode.None : CursorLockMode.Locked;
        transform.position += (transform.forward * Input.GetAxis("Vertical") + transform.right * Input.GetAxis("Horizontal")) * (Time.deltaTime * speed * (Input.GetKey(KeyCode.LeftShift) ? 2.0F : 1.0F));
        cameraRot.x += Input.GetAxis("Mouse X") * cameraSensitivity * Time.deltaTime;
        cameraRot.y -= Input.GetAxis("Mouse Y") * cameraSensitivity * Time.deltaTime;
        cameraRot.y = Mathf.Clamp(cameraRot.y, -90.0F, 90.0F);
        transform.rotation = Quaternion.Euler(cameraRot.y, cameraRot.x,0.0F);
    }
}
