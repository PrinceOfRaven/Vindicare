using UnityEngine;

public class SplitterEnemy : Enemy
{
    [Header("Разделение при смерти")]
    [SerializeField] private GameObject _splitPrefab;
    [SerializeField, Min(0)] private int _splitCount = 3;
    [SerializeField] private float _splitSpawnRadius = 0.4f;
    [SerializeField] private float _splitImpulse = 3f;

    protected override void onObjectDeath()
    {
        SpawnSplits();
        base.onObjectDeath();
    }

    private void SpawnSplits()
    {
        if (_splitPrefab == null || _splitCount <= 0) return;

        float angleStep = 360f / _splitCount;
        for (int i = 0; i < _splitCount; i++)
        {
            float angle = (angleStep * i + Random.Range(-15f, 15f)) * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            Vector3 spawnPos = transform.position + (Vector3)(dir * _splitSpawnRadius);

            GameObject child = Instantiate(_splitPrefab, spawnPos, Quaternion.identity);
            var childRb = child.GetComponent<Rigidbody2D>();
            if (childRb != null)
            {
                childRb.AddForce(dir * _splitImpulse, ForceMode2D.Impulse);
            }
        }
    }
}
