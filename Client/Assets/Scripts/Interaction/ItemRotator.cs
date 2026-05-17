using UnityEngine;

public class ItemRotator : MonoBehaviour
{
    [SerializeField] private Vector3 _rotationAxis = Vector3.up;
    [SerializeField] private float _speed = 90f;

    private void Update()
    {
        transform.Rotate(_rotationAxis * _speed * Time.deltaTime);
    }
}
