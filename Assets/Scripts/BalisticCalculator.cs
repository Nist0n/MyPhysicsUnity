using UnityEngine;
using UnityEngine.InputSystem;

namespace Group_9
{
    [RequireComponent(typeof(TraectoryRenderer))]
    public class BalisticCalculator:MonoBehaviour
    {
        [Header("Header params")]
        [SerializeField] private float mass = 1f;
        [SerializeField] private float radius = 0.1f;
        [SerializeField] private float dragCoefficient = 0.47f;
        [SerializeField] private float airDensity = 1.225f;
        [SerializeField] private Vector3 wind = Vector3.zero;
        [SerializeField] private Transform zapustikPoint;
        [SerializeField] private Transform shootRound;
        [SerializeField] private float  muzzleVelocity = 20f;
        [SerializeField, Range(0, 85)] private float muzleAngle = 20f;

        [Header("Random ranges per shot")]
        [SerializeField] private Vector2 massRange = new Vector2(0.5f, 3f);
        [SerializeField] private Vector2 radiusRange = new Vector2(0.05f, 0.25f);

        [Header("Movement (WASD move, Q/E yaw)")]
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float yawSpeed = 90f;
        [SerializeField] private Transform cannonRoot;

        private TraectoryRenderer _traectoryRenderer;
        private float _currentMass;
        private float _currentRadius;

        private void Start()
        {
            _traectoryRenderer = GetComponent<TraectoryRenderer>();
            _currentMass = mass;
            _currentRadius = radius;
        }

        public void Update()
        {
            HandleMovement();

            if (!zapustikPoint) return;
            Vector3 v0 = CalculateVelocityVector(muzleAngle, muzzleVelocity);
            _traectoryRenderer.SetAirParams(_currentMass, _currentRadius, dragCoefficient, airDensity, wind);
            _traectoryRenderer.DrawWithAirEuler(zapustikPoint.position, v0);

            if (Input.GetKeyDown(KeyCode.Space))
            {
                _currentMass = Random.Range(massRange.x, massRange.y);
                _currentRadius = Random.Range(radiusRange.x, radiusRange.y);
                
                _traectoryRenderer.SetAirParams(_currentMass, _currentRadius, dragCoefficient, airDensity, wind);

                Fire(v0, _currentMass, _currentRadius);
            }
        }

        private void Fire(Vector3 initialVelocity, float mass, float radius)
        {
            if (!shootRound) return;
            GameObject newShootRound = Instantiate (shootRound.gameObject, zapustikPoint.position, Quaternion.identity);

            QuadraticDrag quadraticDrag = newShootRound.GetComponent<QuadraticDrag>();
            quadraticDrag.SetPhysicalParams(mass, radius, dragCoefficient, airDensity, wind, initialVelocity);
            
            newShootRound.transform.localScale = Vector3.one * (radius * 2f);

        }

        private Vector3 CalculateVelocityVector(float angleDeg, float muzzleSpeed)
        {
            float cos = Mathf.Cos(angleDeg * Mathf.Deg2Rad);
            float sin = Mathf.Sin(angleDeg * Mathf.Deg2Rad);
            float vx = muzzleSpeed * cos;
            float vy = muzzleSpeed * sin;

            return zapustikPoint.forward * vx + zapustikPoint.up * vy;
        }

        private void HandleMovement()
        {
            if (!cannonRoot) return;

            Vector3 move = Vector3.zero;
            if (Input.GetKey(KeyCode.W)) move += Vector3.forward;
            if (Input.GetKey(KeyCode.S)) move += Vector3.back;
            if (Input.GetKey(KeyCode.A)) move += Vector3.left;
            if (Input.GetKey(KeyCode.D)) move += Vector3.right;

            Vector3 worldMove = new Vector3(move.x, 0f, move.z) * (moveSpeed * Time.deltaTime);
            cannonRoot.position += worldMove;

            float yaw = 0f;
            if (Input.GetKey(KeyCode.Q)) yaw -= 1f;
            if (Input.GetKey(KeyCode.E)) yaw += 1f;
            cannonRoot.Rotate(Vector3.up, yaw * yawSpeed * Time.deltaTime, Space.World);
        }
    }
}

