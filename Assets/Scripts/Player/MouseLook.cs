using Unity.VisualScripting.Antlr3.Runtime;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

public class MouseLook : MonoBehaviour
{
    [SerializeField] private float _mouseSensitivity = 100f;
    [SerializeField] private Transform _playerBody;

    private float _xRotation = 0f; 

    void Start()
    {
        Cursor.lockState    = CursorLockMode.Locked;
        Cursor.visible      = false;
    }

    void Update()
    {
        if (Mouse.current == null) return; 
        
        float mouseX = Mouse.current.delta.x.ReadValue() * _mouseSensitivity * Time.deltaTime;
        float mouseY = Mouse.current.delta.y.ReadValue() * _mouseSensitivity * Time.deltaTime;        

        // 플레이어 몸체 회전
        _playerBody.Rotate(Vector3.up * mouseX); 

        // x축 회전이 음수면 위를 봄 (카메라 회전)
        _xRotation -= mouseY;
        _xRotation = Mathf.Clamp(_xRotation, -90f, 90f);
        transform.localRotation = Quaternion.Euler(_xRotation, 0f, 0f);
    }
}
