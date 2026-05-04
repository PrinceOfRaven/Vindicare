using UnityEngine;
using UnityEngine.Tilemaps;

public class CaveGenerator : MonoBehaviour
{
    [Header("Tilemap Refs")]
    [SerializeField] private Tilemap _groundTilemap;
    [SerializeField] private Tilemap _wallTilemap;
    [SerializeField] private TileBase _groundTile;
    [SerializeField] private TileBase _wallTile;

    [Header("Generation")]
    [SerializeField] private int _width = 80;
    [SerializeField] private int _height = 80;
    [Range(0f, 1f)]
    [SerializeField] private float _fillProbability = 0.45f;
    [SerializeField] private int _smoothIterations = 5;

    [Header("Seed")]
    [SerializeField] private bool _useRandomSeed = true;
    [SerializeField] private int _seed = 0;

    [Header("Player")]
    [Tooltip("Если задан — будет телепортирован в свободную клетку после генерации")]
    [SerializeField] private Transform _player;

    private bool[,] _map;

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

        Render();

        PlacePlayer();

        int wallCount = 0;
        for (int x = 0; x < _width; x++)
            for (int y = 0; y < _height; y++)
                if (_map[x, y]) wallCount++;
        Debug.Log($"Wall cells in map: {wallCount} / {_width * _height}. WallTile is null: {_wallTile == null}. WallTilemap is null: {_wallTilemap == null}");
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

    private void Render()
    {
        _groundTilemap.ClearAllTiles();
        _wallTilemap.ClearAllTiles();
        for (int x = 0; x < _width; x++)
        {
            for (int y = 0; y < _height; y++)
            {
                Vector3Int pos = new Vector3Int(x - _width / 2, y - _height / 2, 0);
                _groundTilemap.SetTile(pos, _groundTile);
                if (_map[x, y]) _wallTilemap.SetTile(pos, _wallTile);
            }
        }
    }

    private void PlacePlayer()
    {
        if (_player == null) return;

        int cx = _width / 2;
        int cy = _height / 2;
        for (int radius = 0; radius < Mathf.Max(_width, _height); radius++)
        {
            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dy = -radius; dy <= radius; dy++)
                {
                    int x = cx + dx;
                    int y = cy + dy;
                    if (x < 0 || y < 0 || x >= _width || y >= _height) continue;
                    if (_map[x, y]) continue;

                    Vector3Int cellPos = new Vector3Int(x - _width / 2, y - _height / 2, 0);
                    Vector3 worldPos = _groundTilemap.GetCellCenterWorld(cellPos);
                    _player.position = new Vector3(worldPos.x, worldPos.y, _player.position.z);
                    return;
                }
            }
        }
    }
}
