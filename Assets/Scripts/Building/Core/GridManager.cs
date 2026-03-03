using System.Collections.Generic;
using UnityEngine;

public class GridManager : MonoBehaviour
{
    public static GridManager Instance { get; private set; }

    [Header("Grid Settings")]
    [SerializeField] private float _cellSize = 4f;
    [SerializeField] private BuildingEvents _events;
    [SerializeField] private GameObject _boardPrefab;

    private readonly Dictionary<GridFace, BoardData> _boards = new();
    private readonly Dictionary<GridFace, GameObject> _boardObjects = new();

    // Reusable buffer for adjacency queries (max 12 neighbors per face: 4 edges × 3 per edge)
    private readonly GridFace[] _adjacencyBuffer = new GridFace[12];

    public float CellSize => _cellSize;
    public BuildingEvents Events => _events;
    public int BoardCount => _boards.Count;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    // ─── Coordinate Conversion ───────────────────────────────────────

    public Vector3Int WorldToCell(Vector3 worldPosition)
    {
        return new Vector3Int(
            Mathf.RoundToInt(worldPosition.x / _cellSize),
            Mathf.RoundToInt(worldPosition.y / _cellSize),
            Mathf.RoundToInt(worldPosition.z / _cellSize)
        );
    }

    public Vector3 CellToWorld(Vector3Int cell)
    {
        return new Vector3(cell.x, cell.y, cell.z) * _cellSize;
    }

    /// <summary>
    /// Determines which face of the grid a world position is nearest to.
    /// The position is snapped to the nearest cell, then the axis component
    /// furthest from the cell center determines which face.
    /// </summary>
    public GridFace WorldToFace(Vector3 worldPosition)
    {
        Vector3Int cell = WorldToCell(worldPosition);
        Vector3 cellCenter = CellToWorld(cell);
        Vector3 local = worldPosition - cellCenter;

        float ax = Mathf.Abs(local.x);
        float ay = Mathf.Abs(local.y);
        float az = Mathf.Abs(local.z);

        if (ay >= ax && ay >= az)
        {
            // Nearest to Y face
            Vector3Int dir = local.y >= 0 ? Vector3Int.up : Vector3Int.down;
            return GridFace.FromCellAndDirection(cell, dir);
        }
        if (ax >= az)
        {
            // Nearest to X face
            Vector3Int dir = local.x >= 0 ? Vector3Int.right : Vector3Int.left;
            return GridFace.FromCellAndDirection(cell, dir);
        }
        // Nearest to Z face
        Vector3Int zDir = local.z >= 0 ? new Vector3Int(0, 0, 1) : new Vector3Int(0, 0, -1);
        return GridFace.FromCellAndDirection(cell, zDir);
    }

    // ─── Queries ─────────────────────────────────────────────────────

    public bool HasBoard(GridFace face)
    {
        return _boards.ContainsKey(face);
    }

    public BoardData? GetBoardData(GridFace face)
    {
        return _boards.TryGetValue(face, out BoardData data) ? data : null;
    }

    public IEnumerable<GridFace> GetAllFaces()
    {
        return _boards.Keys;
    }

    /// <summary>
    /// Returns true if at least one adjacent face (sharing a physical edge)
    /// already has a board placed. Returns true if the grid is empty
    /// (first board can go anywhere).
    /// </summary>
    public bool HasAdjacentBoard(GridFace face)
    {
        if (_boards.Count == 0)
            return true;

        face.GetAdjacentFaces(_adjacencyBuffer, out int count);
        for (int i = 0; i < count; i++)
        {
            if (_boards.ContainsKey(_adjacencyBuffer[i]))
                return true;
        }
        return false;
    }

    /// <summary>
    /// Gets all faces adjacent to the given face that do NOT have a board.
    /// Used for generating snap zones.
    /// </summary>
    public void GetEmptyAdjacentFaces(GridFace face, List<GridFace> result)
    {
        result.Clear();
        face.GetAdjacentFaces(_adjacencyBuffer, out int count);
        for (int i = 0; i < count; i++)
        {
            if (!_boards.ContainsKey(_adjacencyBuffer[i]))
                result.Add(_adjacencyBuffer[i]);
        }
    }

    public bool HasAnyBoardInCell(Vector3Int cell)
    {
        // Check all 6 faces of a cell
        // +X face: GridFace(cell, X)
        // -X face: GridFace(cell + left, X) = GridFace(cell - right, X)
        // +Y face: GridFace(cell, Y)
        // -Y face: GridFace(cell + down, Y) = GridFace(cell - up, Y)
        // +Z face: GridFace(cell, Z)
        // -Z face: GridFace(cell + back, Z) = GridFace(cell - forward, Z)

        if (_boards.ContainsKey(new GridFace(cell, Axis.X))) return true;
        if (_boards.ContainsKey(new GridFace(cell + Vector3Int.left, Axis.X))) return true;
        if (_boards.ContainsKey(new GridFace(cell, Axis.Y))) return true;
        if (_boards.ContainsKey(new GridFace(cell + Vector3Int.down, Axis.Y))) return true;
        if (_boards.ContainsKey(new GridFace(cell, Axis.Z))) return true;
        if (_boards.ContainsKey(new GridFace(cell + new Vector3Int(0, 0, -1), Axis.Z))) return true;

        return false;
    }

    // ─── Placement ───────────────────────────────────────────────────

    public bool TryPlaceBoard(GridFace face, BoardData data)
    {
        if (_boards.ContainsKey(face))
            return false;

        _boards[face] = data;
        SpawnBoardVisual(face, data);
        _events?.RaiseBoardPlaced(face, data);
        return true;
    }

    public void RemoveBoard(GridFace face)
    {
        if (!_boards.ContainsKey(face))
            return;

        _boards.Remove(face);
        DestroyBoardVisual(face);
        _events?.RaiseBoardRemoved(face);
    }

    /// <summary>
    /// Replaces a board's visual with a reinforced version while keeping its position.
    /// </summary>
    public bool ReinforceBoard(GridFace face, GameObject reinforcedPrefab)
    {
        if (!_boards.TryGetValue(face, out BoardData data))
            return false;

        DestroyBoardVisual(face);

        float placedTime = data.PlacedTime;

        if (reinforcedPrefab != null)
        {
            Vector3 worldPos = face.GetWorldPosition(_cellSize);
            Quaternion rotation = face.GetRotation();

            GameObject board = Instantiate(reinforcedPrefab, worldPos, rotation, transform);
            board.name = $"Board_{face}_Reinforced";

            BoardVisual visual = board.GetComponent<BoardVisual>();
            if (visual != null)
                visual.Initialize(face);

            _boardObjects[face] = board;
        }

        _boards[face] = new BoardData("reinforced", placedTime, data.CustomData);
        return true;
    }

    // ─── Visual Spawning ─────────────────────────────────────────────

    private void SpawnBoardVisual(GridFace face, BoardData data)
    {
        if (_boardPrefab == null) return;

        Vector3 worldPos = face.GetWorldPosition(_cellSize);
        Quaternion rotation = face.GetRotation();

        GameObject board = Instantiate(_boardPrefab, worldPos, rotation, transform);
        board.name = $"Board_{face}";

        BoardVisual visual = board.GetComponent<BoardVisual>();
        if (visual != null)
            visual.Initialize(face);

        _boardObjects[face] = board;
    }

    private void DestroyBoardVisual(GridFace face)
    {
        if (_boardObjects.TryGetValue(face, out GameObject board))
        {
            Destroy(board);
            _boardObjects.Remove(face);
        }
    }

    // ─── Utility ─────────────────────────────────────────────────────

    public void ClearAll()
    {
        foreach (var face in new List<GridFace>(_boards.Keys))
        {
            RemoveBoard(face);
        }
    }
}
