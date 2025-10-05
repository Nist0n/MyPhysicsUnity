using UnityEngine;

public class QuadraticDrag : MonoBehaviour
{
    private float _radius;
    private float _dragCoefficient;
    private float _airDensity;
    private Vector3 _wind = Vector3.zero;

    private Rigidbody _rigidbody;
    private float _area;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        Vector3 relativeVelocity = _rigidbody.linearVelocity - _wind;
        float speed = relativeVelocity.magnitude;
        if (speed < 1e-6f) return;

        Vector3 drag = -0.5f * _airDensity * _dragCoefficient * _area * speed * relativeVelocity;
        _rigidbody.AddForce(drag, ForceMode.Force);
    }

    public void SetPhysicalParams(float mass, float radius, float dragCoefficent, float airDensty, Vector3 wind, Vector3 initialVelocity)
    {
        _radius = radius;
        _dragCoefficient = dragCoefficent;
        _airDensity = airDensty;
        _wind = wind;

        _rigidbody.mass = mass;
        _rigidbody.useGravity = true;
        _rigidbody.linearDamping = 0f;
        _rigidbody.angularDamping = 0f;
        _rigidbody.linearVelocity = initialVelocity;

        _area = _radius * _radius * Mathf.PI;
    }
}
