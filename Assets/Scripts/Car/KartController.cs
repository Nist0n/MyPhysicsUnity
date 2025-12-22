using UnityEngine;
using UnityEngine.InputSystem;

namespace Car
{
    [RequireComponent(typeof(Rigidbody))]
    public class KartController : MonoBehaviour
    {
        [Header("Assignment 4 Settings")]
        [Tooltip("Включить четвертое задание")]
        [SerializeField] private bool _enableAssignment4 = false;

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

        [Header("Body Roll")]
        [SerializeField] private bool _enableBodyRoll = true;
        [SerializeField] private Transform _bodyTransform;
        [SerializeField] private float _bodyRollStrength = 5000f;
        [SerializeField] private float _bodyRollDamping = 2000f;
        [SerializeField] private float _bodyRollSpeed = 90f;
        [SerializeField] private float _minSpeedForRoll = 2f;

        [Header("Steering")]
        [SerializeField] private float _maxSteerAngle = 45f;
        [SerializeField] private float _speedSensitiveSteeringThreshold = 15f;
        [Range(0f, 1f)]
        [SerializeField] private float _minSteerMultiplierAtSpeed = 0.4f;

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
        [SerializeField] private float _frontLateralStiffness = 60f;
        [SerializeField] private float _rearLateralStiffness = 100f;

        [Header("Handbrake tyre settings")]
        [SerializeField] private float _rearLateralStiffnessWithHandbrake = 0f;

        [Header("Suspension")]
        [SerializeField] private CarSuspension _suspension;

        [Header("Aerodynamics - Drag")]
        [SerializeField] private float _dragCoefficient = 0.3f;
        [SerializeField] private float _frontalArea = 1.5f;
        [SerializeField] private float _airDensity = 1.225f;

        [Header("Aerodynamics - Downforce (Wing)")]
        [SerializeField] private bool _enableDownforce = true;
        [SerializeField] private float _wingAngleDeg = 15f;
        [SerializeField] private float _wingArea = 0.8f;
        [SerializeField] private float _liftCoefficientSlope = 0.1f;
        [SerializeField] private Transform _wingPosition;

        [Header("Aerodynamics - Ground Effect")]
        [SerializeField] private bool _enableGroundEffect = true;
        [SerializeField] private float _groundEffectForce = 5000f;
        [SerializeField] private float _groundEffectRaycastDistance = 1f;
        [SerializeField] private Transform _groundEffectPosition;
        [SerializeField] private LayerMask _groundLayer = -1;

        [Header("Flight Stability")]
        [SerializeField] private bool _enableFlightStabilization = true;
        [SerializeField] private float _flightStabilizationStrength = 50f;
        [SerializeField] private float _maxFlightAngularVelocity = 2f;

        [Header("Debug Visualization")]
        [SerializeField] private bool showAerodynamicsGizmos = true;
        [SerializeField] private float forceVectorScale = 0.001f;

        private Rigidbody _rb;
        private bool _isInFlight = false;

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
        public float DragForce { get; private set; }
        public float Downforce { get; private set; }
        public float GroundEffectForce { get; private set; }
        public float CenterOfMassHeight { get; private set; }
        public float BodyRollAngle { get; private set; }
        public float RollTorque { get; private set; }

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

            if (!_suspension)
                _suspension = GetComponent<CarSuspension>();
            
            // Отключаем подвеску если четвертое задание выключено
            if (_suspension)
            {
                _suspension.enabled = _enableAssignment4;
            }

            ComputeStaticWheelLoads();

            EnsureStableCenterOfMass();
        }
        
        private void EnsureStableCenterOfMass()
        {
            if (!_rb) return;
            
            Vector3 currentCOM = _rb.centerOfMass;
            
            Vector3 targetCOM = new Vector3(0f, -0.2f, 0f);
            
            if (Mathf.Abs(currentCOM.x) > 0.05f)
            {
                Debug.LogWarning($"Center of mass is offset horizontally: {currentCOM.x}. This may cause instability in flight.");
            }

            if (currentCOM == Vector3.zero)
            {
                _rb.centerOfMass = targetCOM;
            }
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
            
            // Используем подвеску только если включено четвертое задание
            if (_enableAssignment4 && _suspension)
            {
                float minNormalForce = _rb.mass * _gravity * 0.1f;
                
                if (_suspension.FrontLeftData.isGrounded)
                    _frontLeftNormalForce = Mathf.Max(minNormalForce, _suspension.FrontLeftData.totalForce);
                else
                    _frontLeftNormalForce = minNormalForce;
                    
                if (_suspension.FrontRightData.isGrounded)
                    _frontRightNormalForce = Mathf.Max(minNormalForce, _suspension.FrontRightData.totalForce);
                else
                    _frontRightNormalForce = minNormalForce;
                    
                if (_suspension.RearLeftData.isGrounded)
                    _rearLeftNormalForce = Mathf.Max(minNormalForce, _suspension.RearLeftData.totalForce);
                else
                    _rearLeftNormalForce = minNormalForce;
                    
                if (_suspension.RearRightData.isGrounded)
                    _rearRightNormalForce = Mathf.Max(minNormalForce, _suspension.RearRightData.totalForce);
                else
                    _rearRightNormalForce = minNormalForce;
            }
            else
            {
                ComputeStaticWheelLoads();
            }

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
            
            // Функции четвертого задания применяются только если включено
            if (_enableAssignment4)
            {
                ApplyAerodynamics();
                
                UpdateCenterOfMassHeight();
                
                if (_enableBodyRoll && !_isInFlight)
                {
                    ApplyBodyRoll();
                }

                CheckFlightStatus();
                if (_enableFlightStabilization && _isInFlight)
                {
                    StabilizeFlight();
                }
            }
        }
        
        private void ApplyBodyRoll()
        {
            if (!_rb || !_bodyTransform) return;
            
            float speed = SpeedMs;
            
            float lateralForce = FrontAxleFy;
            
            float angularVelocityY = _rb.angularVelocity.y;
            float turnRadius = speed / (Mathf.Abs(angularVelocityY) + 0.01f);
            float centrifugalForce = _rb.mass * speed * speed / Mathf.Max(turnRadius, 1f);
            
            float totalLateralForce = Mathf.Max(Mathf.Abs(lateralForce), centrifugalForce * 0.3f);
            
            float rollDirection = -Mathf.Sign(_steerInput);
            if (Mathf.Abs(_steerInput) < 0.01f)
            {
                rollDirection = -Mathf.Sign(lateralForce);
            }
            
            if (speed < _minSpeedForRoll)
            {
                if (_bodyTransform.localRotation != Quaternion.identity)
                {
                    _bodyTransform.localRotation = Quaternion.RotateTowards(
                        _bodyTransform.localRotation,
                        Quaternion.identity,
                        _bodyRollSpeed * Time.fixedDeltaTime
                    );
                }
                BodyRollAngle = 0f;
                RollTorque = 0f;
                return;
            }
            
            float speedFactor = Mathf.Clamp01((speed - _minSpeedForRoll) / 20f);
            float targetRollAngle = totalLateralForce * _bodyRollStrength * speedFactor * rollDirection * 0.0001f;
            targetRollAngle = Mathf.Clamp(targetRollAngle, -45f, 45f);
            
            RollTorque = totalLateralForce * _bodyRollStrength * speedFactor;
            
            Quaternion targetRollRotation = Quaternion.Euler(targetRollAngle, 0f, 0f);
            _bodyTransform.localRotation = Quaternion.RotateTowards(
                _bodyTransform.localRotation,
                targetRollRotation,
                _bodyRollSpeed * Time.fixedDeltaTime
            );

            Vector3 right = _bodyTransform.right;
            Vector3 forward = _bodyTransform.forward;
            Vector3 rightHorizontal = Vector3.ProjectOnPlane(right, Vector3.up);
            Vector3 horizontalPerp = Vector3.Cross(Vector3.up, forward).normalized;
            
            if (rightHorizontal.magnitude > 0.01f)
            {
                float rollAngle = Vector3.SignedAngle(horizontalPerp, rightHorizontal.normalized, forward);
                BodyRollAngle = rollAngle;
            }
            else
            {
                BodyRollAngle = 0f;
            }
        }
        
        private void CheckFlightStatus()
        {
            if (_suspension)
            {
                _isInFlight = !_suspension.FrontLeftData.isGrounded &&
                             !_suspension.FrontRightData.isGrounded &&
                             !_suspension.RearLeftData.isGrounded &&
                             !_suspension.RearRightData.isGrounded;
            }
            else
            {
                bool anyGrounded = false;
                if (_frontLeftWheel)
                {
                    RaycastHit hit;
                    if (Physics.Raycast(_frontLeftWheel.position, Vector3.down, out hit, 1f, _groundLayer))
                        anyGrounded = true;
                }
                _isInFlight = !anyGrounded;
            }
        }
        
        private void StabilizeFlight()
        {
            if (!_rb) return;
            
            Vector3 angularVelocity = _rb.angularVelocity;
            
            float pitchDamping = -angularVelocity.x * _flightStabilizationStrength;
            float rollDamping = -angularVelocity.z * _flightStabilizationStrength;
            
            if (Mathf.Abs(angularVelocity.x) > _maxFlightAngularVelocity)
            {
                float excess = angularVelocity.x - Mathf.Sign(angularVelocity.x) * _maxFlightAngularVelocity;
                angularVelocity.x -= excess * 0.5f;
            }
            if (Mathf.Abs(angularVelocity.z) > _maxFlightAngularVelocity)
            {
                float excess = angularVelocity.z - Mathf.Sign(angularVelocity.z) * _maxFlightAngularVelocity;
                angularVelocity.z -= excess * 0.5f;
            }
            
            _rb.angularVelocity = angularVelocity;
            
            _rb.AddTorque(transform.right * pitchDamping, ForceMode.Force);
            _rb.AddTorque(transform.forward * rollDamping, ForceMode.Force);
            
            Vector3 currentUp = transform.up;
            Vector3 targetUp = Vector3.up;
            
            Vector3 rotationAxis = Vector3.Cross(currentUp, targetUp);
            float rotationAngle = Vector3.Angle(currentUp, targetUp);
            
            if (rotationAngle > 5f && rotationAxis.magnitude > 0.01f)
            {
                float correctionTorque = rotationAngle * _flightStabilizationStrength * 0.15f;
                _rb.AddTorque(rotationAxis.normalized * correctionTorque, ForceMode.Force);
            }
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
            float speed = SpeedMs;
            float steerMultiplier = 1f;
            
            if (speed > _speedSensitiveSteeringThreshold)
            {
                float speedFactor = Mathf.Clamp01((speed - _speedSensitiveSteeringThreshold) / _speedSensitiveSteeringThreshold);
                steerMultiplier = Mathf.Lerp(1f, _minSteerMultiplierAtSpeed, speedFactor);
            }
            
            float steerAngle = _maxSteerAngle * _steerInput * steerMultiplier;
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

        private void ApplyAerodynamics()
        {
            if (!_rb) return;

            Vector3 velocity = _rb.linearVelocity;
            float speed = velocity.magnitude;
            float speedSquared = speed * speed;
            
            float dragCoeff = _dragCoefficient;

            DragForce = 0.5f * _airDensity * dragCoeff * _frontalArea * speedSquared;
            Vector3 dragDirection = speed > 0.01f ? -velocity.normalized : -transform.forward;
            Vector3 dragForceVector = dragDirection * DragForce;
            _rb.AddForce(dragForceVector, ForceMode.Force);
            
            if (_enableDownforce && speed > 0.01f)
            {
                float liftCoefficient = _liftCoefficientSlope * (_wingAngleDeg * Mathf.Deg2Rad);
                Downforce = 0.5f * _airDensity * liftCoefficient * _wingArea * speedSquared;
                
                Vector3 downforcePosition;
                if (_wingPosition)
                {
                    downforcePosition = _wingPosition.position;
                    float lateralOffset = Vector3.Dot(downforcePosition - transform.position, transform.right);
                    if (Mathf.Abs(lateralOffset) > 0.1f)
                    {
                        downforcePosition = transform.position + transform.forward * Vector3.Dot(downforcePosition - transform.position, transform.forward);
                    }
                }
                else
                {
                    downforcePosition = _rb.worldCenterOfMass;
                }
                
                Vector3 downforceDirection = -transform.up;
                Vector3 downforceVector = downforceDirection * Downforce;
                _rb.AddForceAtPosition(downforceVector, downforcePosition, ForceMode.Force);
            }
            else
            {
                Downforce = 0f;
            }
            
            if (_enableGroundEffect)
            {
                RaycastHit hit;
                Vector3 rayOrigin = _groundEffectPosition ? _groundEffectPosition.position : transform.position;
                Vector3 rayDirection = -transform.up;
                
                if (Physics.Raycast(rayOrigin, rayDirection, out hit, _groundEffectRaycastDistance, _groundLayer))
                {
                    float height = hit.distance;
                    if (height > 0.01f)
                    {
                        GroundEffectForce = _groundEffectForce / height;
                        Vector3 groundEffectDirection = -transform.up;
                        Vector3 groundEffectVector = groundEffectDirection * GroundEffectForce;
                        Vector3 forcePosition = _groundEffectPosition ? _groundEffectPosition.position : transform.position;
                        _rb.AddForceAtPosition(groundEffectVector, forcePosition, ForceMode.Force);
                    }
                    else
                    {
                        GroundEffectForce = 0f;
                    }
                }
                else
                {
                    GroundEffectForce = 0f;
                }
            }
            else
            {
                GroundEffectForce = 0f;
            }
        }

        private void UpdateCenterOfMassHeight()
        {
            if (!_rb) return;

            Vector3 comWorld = _rb.worldCenterOfMass;
            CenterOfMassHeight = comWorld.y - transform.position.y;
            
            RaycastHit hit;
            if (Physics.Raycast(comWorld, Vector3.down, out hit, 10f, _groundLayer))
            {
                CenterOfMassHeight = hit.distance;
            }
        }

        private void OnDrawGizmosSelected()
        {
            // Визуализация работает только если включено четвертое задание
            if (!_enableAssignment4 || !showAerodynamicsGizmos) return;
            
            if (Application.isPlaying && _rb)
            {
                Vector3 velocity = _rb.linearVelocity;
                float speed = velocity.magnitude;
                
                if (DragForce > 0.1f)
                {
                    Gizmos.color = Color.red;
                    Vector3 dragDirection = speed > 0.01f ? -velocity.normalized : -transform.forward;
                    Vector3 dragVector = dragDirection * DragForce * forceVectorScale;
                    Gizmos.DrawRay(transform.position, dragVector);
                }
                
                if (Downforce > 0.1f && _wingPosition)
                {
                    Gizmos.color = Color.blue;
                    Vector3 downforceVector = -transform.up * Downforce * forceVectorScale;
                    Gizmos.DrawRay(_wingPosition.position, downforceVector);
                }
                
                if (GroundEffectForce > 0.1f)
                {
                    Gizmos.color = Color.yellow;
                    Vector3 gePosition = _groundEffectPosition ? _groundEffectPosition.position : transform.position;
                    Vector3 geVector = -transform.up * GroundEffectForce * forceVectorScale;
                    Gizmos.DrawRay(gePosition, geVector);
                    
                    RaycastHit hit;
                    Vector3 rayOrigin = gePosition;
                    Vector3 rayDirection = -transform.up;
                    if (Physics.Raycast(rayOrigin, rayDirection, out hit, _groundEffectRaycastDistance, _groundLayer))
                    {
                        Gizmos.color = Color.magenta;
                        Gizmos.DrawLine(rayOrigin, hit.point);
                        Gizmos.DrawSphere(hit.point, 0.05f);
                    }
                }
                
                Gizmos.color = Color.green;
                Vector3 comWorld = _rb.worldCenterOfMass;
                Gizmos.DrawSphere(comWorld, 0.1f);
                
                RaycastHit comHit;
                if (Physics.Raycast(comWorld, Vector3.down, out comHit, 10f, _groundLayer))
                {
                    Gizmos.color = Color.cyan;
                    Gizmos.DrawLine(comWorld, comHit.point);
                }
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

            // Скорость автомобиля
            Label($"=== SPEED ===");
            Label($"Speed: {SpeedMs:F2} m/s  ({SpeedKph:F1} km/h)");

            // Обороты двигателя
            if (_engine)
            {
                Label($"=== ENGINE ===");
                Label($"Engine RPM: {_engine.CurrentRpm:F0}");
                Label($"Engine Torque: {_engine.CurrentTorque:F1} Nm");
            }

            // Телеметрия четвертого задания
            if (_enableAssignment4)
            {
                //  Сила аэродинамического сопротивления
                Label($"=== AERODYNAMICS ===");
                Label($"Drag Force: {DragForce:F1} N");
                
                // Сила прижима крыла
                Label($"Downforce: {Downforce:F1} N");
                Label($"Ground Effect Force: {GroundEffectForce:F1} N");

                // Сила подвески на каждом колесе
                if (_suspension)
                {
                    Label($"=== SUSPENSION FORCES ===");
                    var fl = _suspension.FrontLeftData;
                    var fr = _suspension.FrontRightData;
                    var rl = _suspension.RearLeftData;
                    var rr = _suspension.RearRightData;

                    Label($"FL: Spring={fl.springForce:F0}N Damper={fl.damperForce:F0}N Total={fl.totalForce:F0}N");
                    Label($"FR: Spring={fr.springForce:F0}N Damper={fr.damperForce:F0}N Total={fr.totalForce:F0}N");
                    Label($"RL: Spring={rl.springForce:F0}N Damper={rl.damperForce:F0}N Total={rl.totalForce:F0}N");
                    Label($"RR: Spring={rr.springForce:F0}N Damper={rr.damperForce:F0}N Total={rr.totalForce:F0}N");
                }

                // Расстояние от каждого колеса до земли
                if (_suspension)
                {
                    Label($"=== WHEEL DISTANCES ===");
                    Label($"FL Distance: {_suspension.FrontLeftData.hitDistance:F3}m");
                    Label($"FR Distance: {_suspension.FrontRightData.hitDistance:F3}m");
                    Label($"RL Distance: {_suspension.RearLeftData.hitDistance:F3}m");
                    Label($"RR Distance: {_suspension.RearRightData.hitDistance:F3}m");
                }

                // Степень сжатия подвески каждого колеса
                if (_suspension)
                {
                    Label($"=== SUSPENSION COMPRESSION ===");
                    Label($"FL Compression: {_suspension.FrontLeftData.compression:F4}m");
                    Label($"FR Compression: {_suspension.FrontRightData.compression:F4}m");
                    Label($"RL Compression: {_suspension.RearLeftData.compression:F4}m");
                    Label($"RR Compression: {_suspension.RearRightData.compression:F4}m");
                }

                // Высота центра масс автомобиля
                Label($"=== CENTER OF MASS ===");
                Label($"CoM Height: {CenterOfMassHeight:F3}m");
                
                // Крен кузова
                if (_enableBodyRoll)
                {
                    Label($"=== BODY ROLL ===");
                    Label($"Roll Angle: {BodyRollAngle:F2}°");
                    Label($"Roll Torque: {RollTorque:F1} N·m");
                }
                
                // Статус полета
                Label($"=== FLIGHT STATUS ===");
                Label($"In Flight: {(_isInFlight ? "YES" : "NO")}");
                if (_isInFlight && _rb)
                {
                    Vector3 angVel = _rb.angularVelocity;
                    Label($"Angular Vel: Pitch={angVel.x:F2} Roll={angVel.z:F2} Yaw={angVel.y:F2} rad/s");
                }
            }

            // Базовая телеметрия (всегда показывается)
            Label($"=== INPUT ===");
            Label($"Input throttle: {_throttleInput:F2}");
            Label($"Input steer:    {_steerInput:F2}");
            
            float speed = SpeedMs;
            float steerMultiplier = 1f;
            if (speed > _speedSensitiveSteeringThreshold)
            {
                float speedFactor = Mathf.Clamp01((speed - _speedSensitiveSteeringThreshold) / _speedSensitiveSteeringThreshold);
                steerMultiplier = Mathf.Lerp(1f, _minSteerMultiplierAtSpeed, speedFactor);
            }
            float currentSteerAngle = _maxSteerAngle * _steerInput * steerMultiplier;
            Label($"Steer angle: {currentSteerAngle:F1}° (multiplier: {steerMultiplier:F2})");
            
            if (!_handbrakePressed) Label($"Handbrake:      {"OFF"}");
            else Label($"Handbrake:      {"ON"}");

            Label($"=== TYRE FORCES ===");
            Label($"FrontAxleFy: {FrontAxleFy:F1} N");
            Label($"RearAxleFx:  {RearAxleFx:F1} N");
            Label($"v_lat FL: {FrontLeftVLat:F2}  FR: {FrontRightVLat:F2}");
            Label($"v_lat RL: {RearLeftVLat:F2}  RR: {RearRightVLat:F2}");
        }
    }
}


