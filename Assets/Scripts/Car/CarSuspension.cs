using UnityEngine;

namespace Car
{
    public class CarSuspension : MonoBehaviour
    {
        [Header("Suspension Settings")]
        [SerializeField] private float restLength = 0.5f;
        [SerializeField] private float springTravel = 0.2f;
        [SerializeField] private float springStiffness = 35000f;
        [SerializeField] private float damperStiffness = 4500f;
        [Tooltip("Радиус колеса")]
        [SerializeField] private float wheelRadius = 0.3f;
        [SerializeField] private float maxSuspensionForce = 50000f;

        [Header("Wheel Positions")]
        [SerializeField] private Transform frontLeftWheel;
        [SerializeField] private Transform frontRightWheel;
        [SerializeField] private Transform rearLeftWheel;
        [SerializeField] private Transform rearRightWheel;

        [Header("Raycast Settings")]
        [SerializeField] private LayerMask groundLayer = -1;
        [SerializeField] private float raycastDistance = 2f;

        [Header("Debug Visualization")]
        [SerializeField] private bool showGizmos = true;
        [SerializeField] private bool showForceVectors = true;
        [SerializeField] private float forceVectorScale = 0.001f;

        private Rigidbody _rb;
        private float[] _lastCompression = new float[4];
        
        public struct WheelSuspensionData
        {
            public float springForce;
            public float damperForce;
            public float totalForce;
            public float hitDistance;
            public float compression;
            public bool isGrounded;
        }

        public WheelSuspensionData FrontLeftData { get; private set; }
        public WheelSuspensionData FrontRightData { get; private set; }
        public WheelSuspensionData RearLeftData { get; private set; }
        public WheelSuspensionData RearRightData { get; private set; }

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            if (!_rb)
                _rb = GetComponentInParent<Rigidbody>();
        }

        private void FixedUpdate()
        {
            if (!_rb) return;
            
            // Подвеска работает только если включено через KartController
            // Проверяем, активен ли компонент
            if (!enabled) return;

            FrontLeftData = CalculateSuspension(frontLeftWheel, 0);
            FrontRightData = CalculateSuspension(frontRightWheel, 1);
            RearLeftData = CalculateSuspension(rearLeftWheel, 2);
            RearRightData = CalculateSuspension(rearRightWheel, 3);
        }

        private WheelSuspensionData CalculateSuspension(Transform wheelTransform, int wheelIndex)
        {
            WheelSuspensionData data = new WheelSuspensionData();

            if (!wheelTransform)
                return data;

            Vector3 wheelPosition = wheelTransform.position;
            Vector3 suspensionDirection = -transform.up;
            
            RaycastHit hit;
            bool isGrounded = Physics.Raycast(
                wheelPosition,
                suspensionDirection,
                out hit,
                raycastDistance,
                groundLayer
            );

            data.isGrounded = isGrounded;
            
            if (!isGrounded)
            {
                data.hitDistance = raycastDistance;
                data.compression = 0f;
                if (wheelIndex >= 0 && wheelIndex < 4)
                    _lastCompression[wheelIndex] = 0f;
                return data;
            }

            float hitDistance = hit.distance;
            data.hitDistance = hitDistance;

            // Вычисляем сжатие подвески
            float currentLength = hitDistance - wheelRadius;
            float compression = restLength - currentLength;
            compression = Mathf.Clamp(compression, -springTravel, springTravel);
            
            // Плавное нарастание сжатия для предотвращения резких скачков
            if (wheelIndex >= 0 && wheelIndex < 4)
            {
                float lastComp = _lastCompression[wheelIndex];
                float compressionChange = compression - lastComp;
                
                float maxChangePerFrame = springTravel * 0.4f;
                if (Mathf.Abs(compressionChange) > maxChangePerFrame)
                {
                    compressionChange = Mathf.Sign(compressionChange) * maxChangePerFrame;
                    compression = lastComp + compressionChange;
                }
                
                _lastCompression[wheelIndex] = compression;
            }
            
            data.compression = compression;

            // Сила пружины по закону Гука: F = k * x
            
            // Целевое сжатие для состояния покоя (компенсирует вес автомобиля)
            float weightPerWheel = _rb.mass * Physics.gravity.magnitude * 0.25f;
            float targetCompression = weightPerWheel / springStiffness;
            targetCompression = Mathf.Clamp(targetCompression, 0f, springTravel * 0.5f);
            
            float compressionOffset = compression - targetCompression;
            
            float springForce = springStiffness * compressionOffset + weightPerWheel;
            
            springForce = Mathf.Max(weightPerWheel * 0.1f, springForce);
            
            data.springForce = springForce;
            
            Vector3 wheelVelocity = _rb.GetPointVelocity(wheelPosition);
            float compressionVelocity = Vector3.Dot(wheelVelocity, suspensionDirection);

            // Сила амортизатора: F = -c * v (замедляет движение, всегда против скорости)
            float damperForce = -damperStiffness * compressionVelocity;
            data.damperForce = damperForce;
            
            float totalForce = springForce + damperForce;
            
            totalForce = Mathf.Clamp(totalForce, -maxSuspensionForce, maxSuspensionForce);
            
            if (compression < -springTravel * 0.8f)
            {
                totalForce = 0f;
            }
            
            data.totalForce = totalForce;
            
            Vector3 upDirection = -suspensionDirection;
            Vector3 suspensionForce = upDirection * totalForce;
            _rb.AddForceAtPosition(suspensionForce, wheelPosition, ForceMode.Force);

            return data;
        }

        private void OnDrawGizmosSelected()
        {
            if (!showGizmos || !frontLeftWheel) return;
            
            Gizmos.color = Color.green;
            DrawWheelRaycast(frontLeftWheel);
            DrawWheelRaycast(frontRightWheel);
            DrawWheelRaycast(rearLeftWheel);
            DrawWheelRaycast(rearRightWheel);
            
            if (showForceVectors && Application.isPlaying)
            {
                DrawSuspensionForce(frontLeftWheel, FrontLeftData);
                DrawSuspensionForce(frontRightWheel, FrontRightData);
                DrawSuspensionForce(rearLeftWheel, RearLeftData);
                DrawSuspensionForce(rearRightWheel, RearRightData);
            }
        }

        private void DrawWheelRaycast(Transform wheel)
        {
            if (!wheel) return;
            
            Vector3 pos = wheel.position;
            Vector3 direction = -transform.up;
            
            Gizmos.color = Color.green;
            Gizmos.DrawRay(pos, direction * raycastDistance);
            
            if (Application.isPlaying)
            {
                RaycastHit hit;
                if (Physics.Raycast(pos, direction, out hit, raycastDistance, groundLayer))
                {
                    Gizmos.color = Color.red;
                    Gizmos.DrawSphere(hit.point, 0.05f);
                    
                    Gizmos.color = Color.yellow;
                    Vector3 wheelContactPoint = pos + direction * hit.distance;
                    Gizmos.DrawSphere(wheelContactPoint, 0.03f);
                }
            }
        }

        private void DrawSuspensionForce(Transform wheel, WheelSuspensionData data)
        {
            if (!wheel || !data.isGrounded) return;
            
            Vector3 pos = wheel.position;
            Vector3 upDirection = transform.up;
            Vector3 forceVector = upDirection * data.totalForce * forceVectorScale;
            
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(pos, forceVector);

            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(pos, upDirection * data.springForce * forceVectorScale);
        }
    }
}
