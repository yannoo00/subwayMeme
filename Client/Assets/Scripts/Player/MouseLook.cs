using UnityEngine;
using UnityEngine.InputSystem;

public class MouseLook : MonoBehaviour
{
    [SerializeField] private float _sensitivityX = 0.15f;
    [SerializeField] private float _sensitivityY = 0.12f;
    // player body 대신 카메라를 회전
    [SerializeField] private Transform _cameraPivot;

    private float _xRotation = 0f;


    void Update()
    {
        if (GameManager.Instance.CurrentState == GameState.UI) return;
        if (Mouse.current == null) return;

        float mouseX = Mouse.current.delta.x.ReadValue() * _sensitivityX;
        float mouseY = Mouse.current.delta.y.ReadValue() * _sensitivityY;

        // CameraHolder만 수평 회전
        _cameraPivot.Rotate(Vector3.up * mouseX);

        // Camera 자체는 수직 회전
        _xRotation -= mouseY;
        _xRotation = Mathf.Clamp(_xRotation, -90f, 90f);
        transform.localRotation = Quaternion.Euler(_xRotation, 0f, 0f);
    }
}
