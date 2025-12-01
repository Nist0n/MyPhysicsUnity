using UnityEngine;
using UnityEngine.InputSystem;

namespace Car
{
    [RequireComponent(typeof(Rigidbody))]
    public class KartController : MonoBehaviour
    {
        [Header("Physics")]
        [SerializeField] private float _gravity = 9.81f;

        [Header("Wheel attachment points")]
        [SerializeField] private Transform _frontLeftWheel;
        [SerializeField] private Transform _frontRightWheel;
        [SerializeField] private Transform _rearLeftWheel;
        [SerializeField] private Transform _rearRightWheel;

        [Header("Weight distribution")]
        [Range(0f, 1f)]
        [SerializeField] private float _frontAxleShare = 0.5f;

        [Header("Steering")]
        [SerializeField] private float _maxSteerAngle = 30f;

        [Header("Input (New Input System)")]
        [SerializeField] private InputActionReference _moveActionRef;
        
        [SerializeField] private InputActionReference _handbrakeActionRef;

        [Header("Engine & drivetrain")]
        [SerializeField] private KartEngine _engine;
        [SerializeField] private float _gearRatio = 8f;
        [SerializeField] private float _drivetrainEfficiency = 0.9f;
        [SerializeField] private float _wheelRadius = 0.3f;

        [Header("Rolling resistance")]
        [SerializeField] private float _rollingResistance = 0.5f;
        [SerializeField] private float _handbrakeRollingMultiplier = 3f;

        [Header("Tyre friction")]
        [SerializeField] private float _frictionCoefficient = 1.0f;
        [SerializeField] private float _frontLateralStiffness = 80f;
        [SerializeField] private float _rearLateralStiffness = 100f;

        [Header("Handbrake tyre settings")]
        [SerializeField] private float _rearLateralStiffnessWithHandbrake = 0f;

        private Rigidbody _rb;

        private float _frontLeftNormalForce;
        private float _frontRightNormalForce;
        private float _rearLeftNormalForce;
        private float _rearRightNormalForce;

        private Quaternion _frontLeftInitialLocalRot;
        private Quaternion _frontRightInitialLocalRot;

        private float _throttleInput; 
        private float _steerInput;    
        private bool _handbrakePressed;
    
        public float SpeedMs
        {
            get
            {
                if (_rb) return _rb.linearVelocity.magnitude;
                else return 0f;
            }
        }

        public float SpeedKph => SpeedMs * 3.6f;

        public float FrontAxleFy { get; private set; }
        public float RearAxleFx { get; private set; }

        public float FrontLeftVLat { get; private set; }
        public float FrontRightVLat { get; private set; }
        public float RearLeftVLat { get; private set; }
        public float RearRightVLat { get; private set; }

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
        }

        private void Start()
        {
            if (_frontLeftWheel)
                _frontLeftInitialLocalRot = _frontLeftWheel.localRotation;

            if (_frontRightWheel)
                _frontRightInitialLocalRot = _frontRightWheel.localRotation;

            ComputeStaticWheelLoads();
        }

        private void OnEnable()
        {
            if (_moveActionRef && _moveActionRef.action != null)
                _moveActionRef.action.Enable();

            if (_handbrakeActionRef && _handbrakeActionRef.action != null)
                _handbrakeActionRef.action.Enable();
        }

        private void OnDisable()
        {
            if (_moveActionRef && _moveActionRef.action != null)
                _moveActionRef.action.Disable();

            if (_handbrakeActionRef && _handbrakeActionRef.action != null)
                _handbrakeActionRef.action.Disable();
        }

        private void Update()
        {
            ReadInput();
            RotateFrontWheels();
        }

        private void FixedUpdate()
        {
            if (!_rb)
                return;
        
            FrontAxleFy = 0f;
            RearAxleFx = 0f;

            float speedAlongForward = Vector3.Dot(_rb.linearVelocity, transform.forward);

            float throttleAbs = Mathf.Abs(_throttleInput);
            float engineTorque = 0f;
            if (_engine)
            {
                engineTorque = _engine.Simulate(
                    throttleAbs,
                    speedAlongForward,
                    Time.fixedDeltaTime
                );
            }

            float driveSign = Mathf.Sign(_throttleInput);

            float totalWheelTorque = engineTorque * _gearRatio * _drivetrainEfficiency;
            float wheelTorquePerRearWheel = totalWheelTorque * 0.5f;
            float driveForcePerRearWheel = _wheelRadius > 0.0001f
                ? driveSign * (wheelTorquePerRearWheel / _wheelRadius)
                : 0f;

            ApplyWheelForces(_frontLeftWheel, _frontLeftNormalForce, false, false, 0f, out float flFy, out float flVLat);
            ApplyWheelForces(_frontRightWheel, _frontRightNormalForce, false, false, 0f, out float frFy, out float frVLat);

            ApplyWheelForces(_rearLeftWheel, _rearLeftNormalForce, true, true, driveForcePerRearWheel, out float rlFy, out float rlVLat);
            ApplyWheelForces(_rearRightWheel, _rearRightNormalForce, true, true, driveForcePerRearWheel, out float rrFy, out float rrVLat);

            FrontAxleFy = flFy + frFy;

            FrontLeftVLat = flVLat;
            FrontRightVLat = frVLat;
            RearLeftVLat = rlVLat;
            RearRightVLat = rrVLat;
        }
    
        private void ComputeStaticWheelLoads()
        {
            if (!_rb)
                _rb = GetComponent<Rigidbody>();

            float mass = _rb.mass;
            float totalWeight = mass * _gravity;

            float frontWeight = totalWeight * _frontAxleShare;
            float rearWeight = totalWeight * (1f - _frontAxleShare);

            _frontLeftNormalForce = frontWeight * 0.5f;
            _frontRightNormalForce = frontWeight * 0.5f;

            _rearLeftNormalForce = rearWeight * 0.5f;
            _rearRightNormalForce = rearWeight * 0.5f;
        }

        private void ReadInput()
        {
            if (_moveActionRef && _moveActionRef.action != null)
            {
                Vector2 move = _moveActionRef.action.ReadValue<Vector2>();
                _steerInput = Mathf.Clamp(move.x, -1f, 1f);
                _throttleInput = Mathf.Clamp(move.y, -1f, 1f);
            }
            else
            {
                _steerInput = 0f;
                _throttleInput = 0f;
            }

            if (_handbrakeActionRef && _handbrakeActionRef.action != null)
            {
                float hb = _handbrakeActionRef.action.ReadValue<float>();
                _handbrakePressed = hb > 0.5f;
            }
            else
            {
                _handbrakePressed = false;
            }
        }

        private void RotateFrontWheels()
        {
            float steerAngle = _maxSteerAngle * _steerInput;
            Quaternion steerRotation = Quaternion.Euler(0f, steerAngle, 0f);

            if (_frontLeftWheel)
                _frontLeftWheel.localRotation = _frontLeftInitialLocalRot * steerRotation;

            if (_frontRightWheel)
                _frontRightWheel.localRotation = _frontRightInitialLocalRot * steerRotation;
        }
    
            private void ApplyWheelForces(
                Transform wheel,
                float normalForce,
                bool isDriven,
                bool isRear,
                float driveForceInput,
                out float fyOut,
                out float vLatOut)
            {
                fyOut = 0f;
                vLatOut = 0f;

                if (!wheel || !_rb)
                    return;

                Vector3 wheelPos = wheel.position;
                Vector3 wheelForward = wheel.forward;
                Vector3 wheelRight = wheel.right;

                Vector3 v = _rb.GetPointVelocity(wheelPos);
                float vLong = Vector3.Dot(v, wheelForward);
                float vLat = Vector3.Dot(v, wheelRight);

                vLatOut = vLat;

                float Fx = 0f;
                float Fy = 0f;

                if (isDriven)
                {
                    Fx += driveForceInput;
                }

                float rolling = _rollingResistance;
                if (isRear && _handbrakePressed)
                {
                    rolling *= _handbrakeRollingMultiplier;
                }

                Fx += -rolling * vLong;
            
                float lateralStiffness;
                if (isRear) lateralStiffness = _rearLateralStiffness;
                else lateralStiffness = _frontLateralStiffness;
                if (isRear && _handbrakePressed)
                {
                    lateralStiffness = _rearLateralStiffnessWithHandbrake;
                }

                Fy += -lateralStiffness * vLat;

                float frictionLimit = _frictionCoefficient * normalForce;
                float forceLength = Mathf.Sqrt(Fx * Fx + Fy * Fy);

                if (forceLength > frictionLimit && forceLength > 1e-6f)
                {
                    float scale = frictionLimit / forceLength;
                    Fx *= scale;
                    Fy *= scale;
                }

                fyOut = Fy;

                Vector3 force = wheelForward * Fx + wheelRight * Fy;
                _rb.AddForceAtPosition(force, wheelPos, ForceMode.Force);

                if (isRear)
                {
                    RearAxleFx += Vector3.Dot(force, transform.forward);
                }
            }

        private void OnGUI()
        {
            const int lineHeight = 18;
            int line = 0;

            void Label(string text)
            {
                GUI.Label(new Rect(10, 10 + line * lineHeight, 600, lineHeight), text);
                line++;
            }

            Label($"Speed: {SpeedMs:F2} m/s  ({SpeedKph:F1} km/h)");

            if (_engine)
            {
                Label($"Engine RPM: {_engine.CurrentRpm:F0}");
                Label($"Engine Torque: {_engine.CurrentTorque:F1} Nm");
                Label($"Throttle (smoothed): {_engine.SmoothedThrottle:F2}");
                Label($"RevLimiterFactor: {_engine.RevLimiterFactor:F2}");
            }

            Label($"Input throttle: {_throttleInput:F2}");
            Label($"Input steer:    {_steerInput:F2}");
            if (!_handbrakePressed) Label($"Handbrake:      {"OFF"}");
            else Label($"Handbrake:      {"ON"}");

            Label($"FrontAxleFy: {FrontAxleFy:F1} N");
            Label($"RearAxleFx:  {RearAxleFx:F1} N");

            Label($"v_lat FL: {FrontLeftVLat:F2}  FR: {FrontRightVLat:F2}");
            Label($"v_lat RL: {RearLeftVLat:F2}  RR: {RearRightVLat:F2}");
        }
    }
}


