using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaveSpawner : MonoBehaviour
{
    [Serializable]
    public class EnemyGroup
    {
        [Tooltip("Префаб с EnemyBase-компонентом.")]
        public GameObject enemyPrefab;

        [Min(1)] public int count = 5;

        [Tooltip("Секунд между спавнами внутри этой группы.")]
        [Min(0.05f)] public float spawnInterval = 1.0f;

        [Tooltip("Сколько врагов спавнить за один тик (роем).")]
        [Min(1)] public int burstSize = 1;
    }

    [Serializable]
    public class Wave
    {
        public string name = "Wave";
        public List<EnemyGroup> groups = new List<EnemyGroup>();

        [Tooltip("Если true — следующая волна стартует только после смерти всех врагов этой волны (для босса/мини-босса).")]
        public bool waitForClear = false;
    }

    [Header("Волны")]
    [SerializeField] private List<Wave> _waves = new List<Wave>();
    [SerializeField, Min(0f)] private float _initialDelay = 3f;
    [SerializeField, Min(0f)] private float _delayBetweenWaves = 5f;

    [Tooltip("Зациклить ли волны после прохождения последней (для бесконечного режима).")]
    [SerializeField] private bool _loopWaves = false;

    [Header("Спавн")]
    [Tooltip("Камера, относительно которой спавним за экраном. Если пусто — Camera.main.")]
    [SerializeField] private Camera _camera;

    [Tooltip("Минимальное расстояние от игрока для спавна (мировые единицы).")]
    [SerializeField, Min(1f)] private float _minSpawnDistance = 12f;

    [Tooltip("Максимальное расстояние от игрока для спавна.")]
    [SerializeField, Min(2f)] private float _maxSpawnDistance = 20f;

    [Tooltip("Сколько раз пытаемся найти проходимую клетку для одного врага.")]
    [SerializeField, Min(1)] private int _spawnAttempts = 16;

    [Header("Цель")]
    [SerializeField] private string _playerTag = "Player";

    [Header("Дебаг")]
    [SerializeField] private bool _verboseLogs = true;

    private readonly HashSet<UnitsBase> _aliveFromCurrentWave = new HashSet<UnitsBase>();
    private int _currentWaveIndex = -1;
    public int CurrentWaveIndex => _currentWaveIndex;

    // Всегда берём актуального игрока через свойство.
    // Если игрок пересоздался — мы автоматически получим нового.
    private Transform Player =>
        PlayerMovement.Instance != null ? PlayerMovement.Instance.transform : null;

    private void Start()
    {
        if (_camera == null) _camera = Camera.main;
        StartCoroutine(RunSpawner());
    }

    private IEnumerator RunSpawner()
    {
        while (CaveGenerator.Instance == null || !CaveGenerator.Instance.IsGenerated)
            yield return null;

        while (Player == null) yield return null;

        if (_initialDelay > 0f) yield return new WaitForSeconds(_initialDelay);

        int safety = 0;
        while (true)
        {
            for (int i = 0; i < _waves.Count; i++)
            {
                _currentWaveIndex = i;
                yield return RunWave(_waves[i]);

                if (i < _waves.Count - 1 || _loopWaves)
                    yield return new WaitForSeconds(_delayBetweenWaves);
            }

            if (!_loopWaves) break;

            if (++safety > 10_000) yield break;
        }

        if (_verboseLogs) Debug.Log("[WaveSpawner] Все волны пройдены.");
    }

    private IEnumerator RunWave(Wave wave)
    {
        if (_verboseLogs) Debug.Log($"[WaveSpawner] Волна '{wave.name}' стартовала.");
        _aliveFromCurrentWave.Clear();

        int active = wave.groups.Count;
        foreach (var group in wave.groups)
        {
            StartCoroutine(SpawnGroup(group, () => active--));
        }

        while (active > 0) yield return null;

        if (wave.waitForClear)
        {
            while (HasAliveFromCurrentWave()) yield return null;
            if (_verboseLogs) Debug.Log($"[WaveSpawner] Волна '{wave.name}' зачищена.");
        }
    }

    private IEnumerator SpawnGroup(EnemyGroup group, Action onDone)
    {
        if (group.enemyPrefab == null)
        {
            Debug.LogWarning("[WaveSpawner] У группы пуст префаб врага.");
            onDone?.Invoke();
            yield break;
        }

        int spawned = 0;
        while (spawned < group.count)
        {
            int batch = Mathf.Min(group.burstSize, group.count - spawned);
            for (int i = 0; i < batch; i++) SpawnOne(group.enemyPrefab);
            spawned += batch;

            if (spawned < group.count)
                yield return new WaitForSeconds(group.spawnInterval);
        }

        onDone?.Invoke();
    }

    private void SpawnOne(GameObject prefab)
    {
        // Если игрока вообще нет (например, помер и сцена пересоздаётся) —
        // просто пропускаем спавн, ничего страшного.
        if (Player == null) return;

        if (!TryFindSpawnPoint(out Vector3 pos))
        {
            pos = Player.position;
            if (_verboseLogs) Debug.LogWarning("[WaveSpawner] Не нашёл проходимую точку, фоллбэк на игрока.");
        }

        GameObject instance = Instantiate(prefab, pos, Quaternion.identity);
        if (instance.TryGetComponent(out UnitsBase unit))
        {
            _aliveFromCurrentWave.Add(unit);
            unit.OnDeath += OnEnemyDied;
        }
    }

    private bool TryFindSpawnPoint(out Vector3 pos)
    {
        pos = Vector3.zero;
        var player = Player;
        if (player == null || CaveGenerator.Instance == null) return false;

        for (int i = 0; i < _spawnAttempts; i++)
        {
            if (!CaveGenerator.Instance.TryGetRandomWalkableAround(
                    player.position, _minSpawnDistance, _maxSpawnDistance, 4, out Vector3 candidate))
                continue;

            if (_camera != null && IsOnScreen(candidate)) continue;

            pos = candidate;
            return true;
        }

        if (CaveGenerator.Instance.TryGetRandomWalkableAround(
                player.position, _minSpawnDistance, _maxSpawnDistance, _spawnAttempts, out Vector3 fallback))
        {
            pos = fallback;
            return true;
        }

        return false;
    }

    private bool IsOnScreen(Vector3 worldPos)
    {
        Vector3 vp = _camera.WorldToViewportPoint(worldPos);
        const float pad = 0.05f;
        return vp.z > 0 && vp.x > -pad && vp.x < 1f + pad && vp.y > -pad && vp.y < 1f + pad;
    }

    private void OnEnemyDied(UnitsBase unit)
    {
        unit.OnDeath -= OnEnemyDied;
        _aliveFromCurrentWave.Remove(unit);
    }

    private bool HasAliveFromCurrentWave()
    {
        _aliveFromCurrentWave.RemoveWhere(u => u == null || !u.IsAlive);
        return _aliveFromCurrentWave.Count > 0;
    }
}