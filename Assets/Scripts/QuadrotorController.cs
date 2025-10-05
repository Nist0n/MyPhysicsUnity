using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class QuadrotorController : MonoBehaviour
{


    [Header("Physics Parametrs")]
    [SerializeField] private float mass = 1.5f;
    [SerializeField] private float maxThrottle = 30f;

    [SerializeField] private float maxPitchDeg = 20f;
    [SerializeField] private float maxRollDeg = 20f;

    private Rigidbody _rigidbody;
    private float _desiredYawDeg;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _rigidbody.mass = Mathf.Max(0.01f, mass);

        _desiredYawDeg = transform.eulerAngles.y;
    }

    private void Update()
    {
        
    }

    private void FixedUpdate()
    {
        float pitchInput = Mathf.Clamp(Input.GetAxis("Vertical"), min: -1f, max: 1f);
        float rollInput = Mathf.Clamp(Input.GetAxis("Horizontal"), min: -1f, max: 1f);

        float throttleInput = Keyboard.current.spaceKey.wasPressedThisFrame ? 1f : 0f;

        float targetPitch = pitchInput * maxPitchDeg;
        float targetRoll = -rollInput * maxRollDeg;

        float targetyawDeg = _desiredYawDeg;

        Quaternion qTarget = Quaternion.Euler(targetPitch, targetyawDeg, targetRoll);
        Quaternion qCurrent = _rigidbody.rotation;

        Quaternion qError = qTarget * Quaternion.Inverse(qCurrent);

        if (qError.w < 0)
            { 
            qError.x = -qError.x;
            qError.y = -qError.y;  
            qError.z = -qError.z;
            qError.w = -qError.w;
            }

        qError.ToAngleAxis(out float angleDeq, out Vector3 axis);

        float angleRed = Mathf.Deg2Rad * angleDeq;

        Vector3 omega = _rigidbody.angularVelocity;
        Vector3 torgue = 8 * angleRed * axis - 2.5f * omega;

        _rigidbody.AddTorque(torgue);

        float g = Physics.gravity.magnitude;
        float hover = g * _rigidbody.mass;

        float comander = Mathf.Lerp(hover - 0.5f * maxThrottle, hover + 0.5f * maxThrottle, throttleInput);
        float totalThrottle = Mathf.Clamp(comander, 0, maxThrottle);

        _rigidbody.AddForce(transform.up * totalThrottle, ForceMode.Force);

    }

}
