using UnityEngine;

public class TargetSpawner : MonoBehaviour
{
    [SerializeField] private Target targetPrefab;
    [SerializeField] private float spawnInterval = 2f;
    [SerializeField] private Vector2 massRange = new Vector2(0.5f, 5f);
    [SerializeField] private Vector2 radiusRange = new Vector2(0.1f, 0.5f);
    [SerializeField] private Vector2 speedRange = new Vector2(2f, 6f);
    [SerializeField] private Vector3 spawnBox = new Vector3(10f, 3f, 10f);

    private float _timer;

    private void Update()
    {
        _timer += Time.deltaTime;
        if (_timer >= spawnInterval)
        {
            _timer = 0f;
            Spawn();
        }
    }

    private void Spawn()
    {
        if (!targetPrefab) return;
        Vector3 offset = new Vector3(
            Random.Range(-spawnBox.x * 0.5f, spawnBox.x * 0.5f),
            Random.Range(0f, spawnBox.y),
            Random.Range(-spawnBox.z * 0.5f, spawnBox.z * 0.5f)
        );

        Target target = Instantiate(targetPrefab, transform.position + offset, Quaternion.identity);
        float m = Random.Range(massRange.x, massRange.y);
        float r = Random.Range(radiusRange.x, radiusRange.y);
        float v = Random.Range(speedRange.x, speedRange.y);
        target.Setup(m, r, v);
    }
}


