using UnityEngine;

public class Target : MonoBehaviour
{
    [SerializeField] private float mass = 1f;
    [SerializeField] private float radius = 0.2f;
    [SerializeField] private float initialSpeed = 3f;
    [SerializeField] private Vector2 yawRangeDeg = new Vector2(-45f, 45f);

    private Rigidbody _rigidbody;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        if (_rigidbody == null)
        {
            _rigidbody = gameObject.AddComponent<Rigidbody>();
        }
        _rigidbody.useGravity = true;
        _rigidbody.linearDamping = 0f;
        _rigidbody.angularDamping = 0f;
    }

    public void Setup(float mass, float radius, float speed)
    {
        this.mass = Mathf.Max(0.0001f, mass);
        this.radius = Mathf.Max(0.0001f, radius);
        initialSpeed = Mathf.Max(0f, speed);

        _rigidbody.mass = this.mass;
        transform.localScale = Vector3.one * (this.radius * 2f);

        float yaw = Random.Range(yawRangeDeg.x, yawRangeDeg.y);
        Vector3 dir = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
        _rigidbody.linearVelocity = dir * initialSpeed;
    }
}


