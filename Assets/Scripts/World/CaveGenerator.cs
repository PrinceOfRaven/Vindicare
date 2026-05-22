using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(Transform))]
public class CaveGenerator : MonoBehaviour
{
    public static CaveGenerator Instance { get; private set; }

    [Header("Tilemap Refs")]
    [SerializeField] private Tilemap _groundTilemap;
    [SerializeField] private Tilemap _wallTilemap;
    [SerializeField] private TileBase _groundTile;
    [SerializeField] private TileBase _wallTile;

    [Tooltip("Композитный коллайдер на объекте Walls. Перегенерирует геометрию после изменения тайлов.")]
    [SerializeField] private CompositeCollider2D _wallsComposite;

    [Header("Generation")]
    [SerializeField, Min(8)] private int _width = 80;
    [SerializeField, Min(8)] private int _height = 80;
    [Range(0f, 1f)]
    [SerializeField] private float _fillProbability = 0.45f;
    [SerializeField, Min(0)] private int _smoothIterations = 5;

    [Tooltip("Минимальный размер пещеры в клетках. Пещеры меньше будут заполнены стеной.")]
    [SerializeField, Min(1)] private int _minCaveSize = 50;

    [Tooltip("Толщина невидимой стены-рамки за границей карты (в клетках). " +
             "Закрывает чёрную пустоту от камеры.")]
    [SerializeField, Min(0)] private int _borderThickness = 16;

    [Tooltip("Гарантировать, что игрок появится в самой большой пещере.")]
    [SerializeField] private bool _keepOnlyLargestCave = true;

    [Header("Seed")]
    [SerializeField] private bool _useRandomSeed = true;
    [SerializeField] private int _seed = 0;

    [Header("Player")]
    [Tooltip("Если задан — будет телепортирован в свободную клетку после генерации")]
    [SerializeField] private Transform _player;

    [Tooltip("Радиус свободного места вокруг точки спавна (в клетках)")]
    [SerializeField, Min(0)] private int _playerSpawnSafeRadius = 1;

    private bool[,] _map;

    public bool IsGenerated { get; private set; }
    public int Width => _width;
    public int Height => _height;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        Generate();
    }

    [ContextMenu("Regenerate")]
    public void RegenerateFromMenu()
    {
        Generate();
    }

    public void Generate()
    {
        if (_useRandomSeed) _seed = System.Environment.TickCount;
        System.Random rng = new System.Random(_seed);

        _map = new bool[_width, _height];

        for (int x = 0; x < _width; x++)
        {
            for (int y = 0; y < _height; y++)
            {
                if (x == 0 || y == 0 || x == _width - 1 || y == _height - 1)
                    _map[x, y] = true;
                else
                    _map[x, y] = rng.NextDouble() < _fillProbability;
            }
        }

        for (int i = 0; i < _smoothIterations; i++) SmoothStep();

        CleanupCaves();

        Render();
        RebuildColliders();
        PlacePlayer();

        IsGenerated = true;

        int wallCount = 0;
        for (int x = 0; x < _width; x++)
            for (int y = 0; y < _height; y++)
                if (_map[x, y]) wallCount++;
        Debug.Log($"[CaveGenerator] seed={_seed} walls={wallCount}/{_width * _height}");
    }

    public bool IsWalkable(Vector3 worldPos)
    {
        if (!IsGenerated || _groundTilemap == null) return false;
        Vector3Int cell = _groundTilemap.WorldToCell(worldPos);
        int gx = cell.x + _width / 2;
        int gy = cell.y + _height / 2;
        if (gx < 0 || gy < 0 || gx >= _width || gy >= _height) return false;
        return !_map[gx, gy];
    }

    public bool TryGetRandomWalkableAround(Vector3 origin, float minRadius, float maxRadius, int attempts, out Vector3 result)
    {
        result = origin;
        if (!IsGenerated || _groundTilemap == null) return false;

        for (int i = 0; i < attempts; i++)
        {
            float angle = Random.value * Mathf.PI * 2f;
            float r = Mathf.Lerp(minRadius, maxRadius, Random.value);
            Vector3 candidate = origin + new Vector3(Mathf.Cos(angle) * r, Mathf.Sin(angle) * r, 0f);
            if (IsWalkable(candidate))
            {
                Vector3Int cell = _groundTilemap.WorldToCell(candidate);
                result = _groundTilemap.GetCellCenterWorld(cell);
                return true;
            }
        }
        return false;
    }

    private void SmoothStep()
    {
        bool[,] next = new bool[_width, _height];
        for (int x = 0; x < _width; x++)
        {
            for (int y = 0; y < _height; y++)
            {
                int walls = CountWallNeighbors(x, y);
                if (walls > 4) next[x, y] = true;
                else if (walls < 4) next[x, y] = false;
                else next[x, y] = _map[x, y];
            }
        }
        _map = next;
    }

    private int CountWallNeighbors(int cx, int cy)
    {
        int count = 0;
        for (int x = cx - 1; x <= cx + 1; x++)
        {
            for (int y = cy - 1; y <= cy + 1; y++)
            {
                if (x == cx && y == cy) continue;
                if (x < 0 || y < 0 || x >= _width || y >= _height) count++;
                else if (_map[x, y]) count++;
            }
        }
        return count;
    }

    private void CleanupCaves()
    {
        bool[,] visited = new bool[_width, _height];
        List<List<Vector2Int>> caves = new List<List<Vector2Int>>();

        for (int x = 0; x < _width; x++)
        {
            for (int y = 0; y < _height; y++)
            {
                if (visited[x, y] || _map[x, y]) continue;
                caves.Add(FloodFill(x, y, visited));
            }
        }

        foreach (var cave in caves)
        {
            if (cave.Count < _minCaveSize)
            {
                foreach (var c in cave) _map[c.x, c.y] = true;
            }
        }

        if (_keepOnlyLargestCave && caves.Count > 0)
        {
            int largestIdx = 0;
            for (int i = 1; i < caves.Count; i++)
                if (caves[i].Count > caves[largestIdx].Count) largestIdx = i;

            for (int i = 0; i < caves.Count; i++)
            {
                if (i == largestIdx) continue;
                foreach (var c in caves[i]) _map[c.x, c.y] = true;
            }
        }
    }

    private List<Vector2Int> FloodFill(int sx, int sy, bool[,] visited)
    {
        var result = new List<Vector2Int>();
        var queue = new Queue<Vector2Int>();
        queue.Enqueue(new Vector2Int(sx, sy));
        visited[sx, sy] = true;

        while (queue.Count > 0)
        {
            var p = queue.Dequeue();
            result.Add(p);

            TryEnqueue(p.x + 1, p.y, visited, queue);
            TryEnqueue(p.x - 1, p.y, visited, queue);
            TryEnqueue(p.x, p.y + 1, visited, queue);
            TryEnqueue(p.x, p.y - 1, visited, queue);
        }
        return result;
    }

    private void TryEnqueue(int x, int y, bool[,] visited, Queue<Vector2Int> queue)
    {
        if (x < 0 || y < 0 || x >= _width || y >= _height) return;
        if (visited[x, y] || _map[x, y]) return;
        visited[x, y] = true;
        queue.Enqueue(new Vector2Int(x, y));
    }

    private void Render()
    {
        _groundTilemap.ClearAllTiles();
        _wallTilemap.ClearAllTiles();

        // Рисуем с запасом за границей сетки: рамка из wall-тайлов закрывает
        // чёрную пустоту, которую иначе видела бы камера у края карты.
        for (int x = -_borderThickness; x < _width + _borderThickness; x++)
        {
            for (int y = -_borderThickness; y < _height + _borderThickness; y++)
            {
                Vector3Int pos = new Vector3Int(x - _width / 2, y - _height / 2, 0);
                bool inside = x >= 0 && y >= 0 && x < _width && y < _height;
                if (inside)
                {
                    _groundTilemap.SetTile(pos, _groundTile);
                    if (_map[x, y]) _wallTilemap.SetTile(pos, _wallTile);
                }
                else
                {
                    _wallTilemap.SetTile(pos, _wallTile);
                }
            }
        }
    }

    private void RebuildColliders()
    {
        if (_wallsComposite != null)
        {
            _wallsComposite.GenerateGeometry();
        }
        else
        {
            var composite = _wallTilemap != null ? _wallTilemap.GetComponent<CompositeCollider2D>() : null;
            if (composite != null) composite.GenerateGeometry();
        }
    }

    private void PlacePlayer()
    {
        if (_player == null) return;

        int cx = _width / 2;
        int cy = _height / 2;

        int maxRadius = Mathf.Max(_width, _height);
        for (int radius = 0; radius < maxRadius; radius++)
        {
            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dy = -radius; dy <= radius; dy++)
                {
                    int x = cx + dx;
                    int y = cy + dy;
                    if (!IsSafeSpawn(x, y)) continue;

                    Vector3Int cellPos = new Vector3Int(x - _width / 2, y - _height / 2, 0);
                    Vector3 worldPos = _groundTilemap.GetCellCenterWorld(cellPos);

                    var rb = _player.GetComponent<Rigidbody2D>();
                    if (rb != null)
                    {
                        rb.position = new Vector2(worldPos.x, worldPos.y);
                        rb.linearVelocity = Vector2.zero;
                    }
                    _player.position = new Vector3(worldPos.x, worldPos.y, _player.position.z);
                    return;
                }
            }
        }

        Debug.LogWarning("[CaveGenerator] Не нашёл безопасной клетки для спавна игрока.");
    }

    private bool IsSafeSpawn(int x, int y)
    {
        for (int ix = x - _playerSpawnSafeRadius; ix <= x + _playerSpawnSafeRadius; ix++)
        {
            for (int iy = y - _playerSpawnSafeRadius; iy <= y + _playerSpawnSafeRadius; iy++)
            {
                if (ix < 0 || iy < 0 || ix >= _width || iy >= _height) return false;
                if (_map[ix, iy]) return false;
            }
        }
        return true;
    }
}