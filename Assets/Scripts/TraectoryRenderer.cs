using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class TraectoryRenderer : MonoBehaviour
{
    [Header("Trajectory paramets")]
    [SerializeField] private float lineWidth = 0.15f;
    [SerializeField] private int pointCount = 30;
    [SerializeField] private float timeStep = 0.1f;
    [Space]
    [SerializeField] private QuadraticDrag shootRound;
    [Header("Air physics preview")]
    [SerializeField] private float mass = 1f;
    [SerializeField] private float radius = 0.1f;
    [SerializeField] private float dragCoefficient = 0.47f;
    [SerializeField] private float airDensity = 1.225f;
    [SerializeField] private Vector3 wind = Vector3.zero;
    [SerializeField] private LayerMask collisionMask = ~0;
    private float area;
    private LineRenderer lineRenderer;

    private void Awake() => InitializeLineRenderer();

    private void InitializeLineRenderer()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.startWidth = lineWidth;
        lineRenderer.useWorldSpace = true;

        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
    }

    public void DrawWithAirEuler(Vector3 startPosition, Vector3 startVelocity)
    {
        if (pointCount < 2) pointCount = 2;
        area = Mathf.PI * radius * radius;

        Vector3 position = startPosition;
        Vector3 velocity = startVelocity;

        lineRenderer.positionCount = pointCount;

        for (int i = 0; i < pointCount; i++)
        {
            lineRenderer.SetPosition(i, position);

            Vector3 relativeVelocity = velocity - wind;
            float speed = relativeVelocity.magnitude;
            Vector3 drag = speed > 1e-6f ? (-0.5f * airDensity * dragCoefficient * area * speed) * relativeVelocity : Vector3.zero;
            Vector3 acceleration = Physics.gravity + drag / Mathf.Max(0.0001f, mass);
            
            velocity += acceleration * timeStep;
            Vector3 nextPosition = position + velocity * timeStep;
            
            float distance = Vector3.Distance(position, nextPosition);
            if (distance > 0f)
            {
                if (Physics.SphereCast(position, radius, (nextPosition - position).normalized, out RaycastHit hit, distance, collisionMask, QueryTriggerInteraction.Ignore))
                {
                    lineRenderer.positionCount = i + 1;
                    lineRenderer.SetPosition(i, hit.point);
                    return;
                }
            }

            position = nextPosition;
        }
    }

    public void SetAirParams(float mass, float radius, float dragCoefficient, float airDensity, Vector3 wind)
    {
        this.mass = Mathf.Max(0.0001f, mass);
        this.radius = Mathf.Max(0.0001f, radius);
        this.dragCoefficient = Mathf.Max(0f, dragCoefficient);
        this.airDensity = Mathf.Max(0f, airDensity);
        this.wind = wind;
    }
}
