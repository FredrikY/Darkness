using System;
using UnityEngine;

/// <summary>
/// Uniquely identifies a face on the 3D grid. A face is the planar boundary
/// between two adjacent cells along one axis.
///
/// Canonical form: Cell is always the cell with the LESSER coordinate along
/// the face's axis. For example, the face between (0,0,0) and (0,1,0) is
/// stored as GridFace((0,0,0), Y) — never as GridFace((0,1,0), Y).
///
/// This makes duplicates impossible by construction:
/// - "Top of cell (0,0,0)" = GridFace((0,0,0), Y)
/// - "Bottom of cell (0,1,0)" = GridFace((0,0,0), Y) — same face.
///
/// Face counts:
/// - 1 cell = 6 faces
/// - 2 adjacent cells = 11 faces (shared face counted once)
/// - N cells in a line = 5N + 1 faces
/// </summary>
[System.Serializable]
public readonly struct GridFace : IEquatable<GridFace>
{
    /// <summary>
    /// The cell with the lesser coordinate along the face axis.
    /// For an X-face at x=3, this cell has x=3 and the other cell has x=4.
    /// For a Y-face at y=5, this cell has y=5 and the other cell has y=6.
    /// </summary>
    public readonly Vector3Int Cell;

    /// <summary>
    /// The axis this face is perpendicular to.
    /// </summary>
    public readonly Axis Axis;

    public GridFace(Vector3Int cell, Axis axis)
    {
        Cell = cell;
        Axis = axis;
    }

    public GridFace(int x, int y, int z, Axis axis)
        : this(new Vector3Int(x, y, z), axis) { }

    /// <summary>
    /// Creates a GridFace from a cell coordinate and a signed direction (e.g. "the Up face of cell C").
    /// Automatically canonicalizes: if the direction is positive along the axis, the cell is used as-is.
    /// If negative (Down, Left, Back), the neighboring cell in that direction becomes the canonical cell.
    /// </summary>
    public static GridFace FromCellAndDirection(Vector3Int cell, Vector3Int direction)
    {
        // direction must be a unit vector along one axis
        if (direction == Vector3Int.up)
            return new GridFace(cell, Axis.Y);
        if (direction == Vector3Int.down)
            return new GridFace(cell + Vector3Int.down, Axis.Y);
        if (direction == Vector3Int.right)
            return new GridFace(cell, Axis.X);
        if (direction == Vector3Int.left)
            return new GridFace(cell + Vector3Int.left, Axis.X);
        if (direction == new Vector3Int(0, 0, 1)) // forward
            return new GridFace(cell, Axis.Z);
        if (direction == new Vector3Int(0, 0, -1)) // back
            return new GridFace(cell + new Vector3Int(0, 0, -1), Axis.Z);

        throw new ArgumentException($"Invalid direction: {direction}. Must be a cardinal unit vector.");
    }

    /// <summary>
    /// The normal vector of this face (points in the positive axis direction).
    /// </summary>
    public Vector3 Normal => Axis switch
    {
        Axis.X => Vector3.right,
        Axis.Y => Vector3.up,
        Axis.Z => Vector3.forward,
        _ => Vector3.zero
    };

    /// <summary>
    /// The two cells this face separates. CellA = Cell (lesser), CellB = Cell + axis offset (greater).
    /// </summary>
    public Vector3Int CellA => Cell;
    public Vector3Int CellB => Cell + AxisOffset;

    /// <summary>
    /// Unit offset along this face's axis.
    /// </summary>
    public Vector3Int AxisOffset => Axis switch
    {
        Axis.X => Vector3Int.right,
        Axis.Y => Vector3Int.up,
        Axis.Z => new Vector3Int(0, 0, 1),
        _ => Vector3Int.zero
    };

    /// <summary>
    /// World position of the center of this face.
    /// </summary>
    public Vector3 GetWorldPosition(float cellSize)
    {
        // Face center is halfway between the two cell centers
        Vector3 cellACenter = new Vector3(Cell.x, Cell.y, Cell.z) * cellSize;
        Vector3 offset = (Vector3)AxisOffset * (cellSize * 0.5f);
        return cellACenter + offset;
    }

    /// <summary>
    /// Rotation for a board placed on this face (board lies in the face plane,
    /// with its flat side facing along the normal).
    /// </summary>
    public Quaternion GetRotation()
    {
        return Axis switch
        {
            Axis.X => Quaternion.Euler(0, 0, -90),
            Axis.Y => Quaternion.identity,
            Axis.Z => Quaternion.Euler(90, 0, 0),
            _ => Quaternion.identity
        };
    }

    /// <summary>
    /// Returns all faces that share a physical edge with this face.
    ///
    /// Each face has 4 edges. Each edge is shared by 3 other grid faces:
    ///   1. Coplanar neighbor (same axis, shifted along the tangent)
    ///   2. Perpendicular face hinging on CellA side
    ///   3. Perpendicular face hinging on CellB side
    ///
    /// Total: 4 edges × 3 = 12 adjacent faces.
    /// Buffer must be at least 12 elements.
    ///
    /// Example: Y-face at (0,0,0), +X edge:
    ///   1. Y-face at (1,0,0) — coplanar, extends right
    ///   2. X-face at (0,0,0) — perpendicular, below the edge
    ///   3. X-face at (0,1,0) — perpendicular, above the edge
    /// </summary>
    public void GetAdjacentFaces(GridFace[] result, out int count)
    {
        count = 0;
        // We derive the two tangent axes from the face axis
        GetTangentAxes(out Axis tan1, out Axis tan2);

        // For each tangent axis, there are 2 edges (positive and negative side).
        // Each edge is shared by 3 other faces:
        //   1. Coplanar face (same axis, shifted along tangent)
        //   2. Perpendicular face on CellA side (axis = tangent, at current cell or offset)
        //   3. Perpendicular face on CellB side (axis = tangent, at current cell or offset)

        // Tangent 1 positive side
        AddEdgeNeighbors(result, ref count, tan1, +1);
        // Tangent 1 negative side
        AddEdgeNeighbors(result, ref count, tan1, -1);
        // Tangent 2 positive side
        AddEdgeNeighbors(result, ref count, tan2, +1);
        // Tangent 2 negative side
        AddEdgeNeighbors(result, ref count, tan2, -1);
    }

    private void AddEdgeNeighbors(GridFace[] result, ref int count, Axis tangentAxis, int sign)
    {
        Vector3Int tangentOffset = AxisToOffset(tangentAxis) * sign;

        // 1. Coplanar neighbor: same axis, shifted along tangent
        result[count++] = new GridFace(Cell + tangentOffset, Axis);

        // 2. Perpendicular face hinging on CellA side of the edge
        // This face has axis = tangentAxis. Its canonical cell depends on sign.
        if (sign > 0)
        {
            // The edge is at the +tangent boundary of our face.
            // The perpendicular face connects CellA to CellA+tangentOffset.
            // Canonical form: lesser cell along tangentAxis = Cell (if sign > 0, Cell is lesser)
            result[count++] = new GridFace(Cell, tangentAxis);
        }
        else
        {
            // The edge is at the -tangent boundary.
            // The perpendicular face connects CellA to CellA+tangentOffset (= CellA - tangent).
            // Canonical form: lesser cell = CellA + tangentOffset (since sign < 0, that's lesser)
            result[count++] = new GridFace(Cell + tangentOffset, tangentAxis);
        }

        // 3. Perpendicular face hinging on CellB side of the edge
        // Same tangent axis, but starting from CellB = Cell + AxisOffset
        Vector3Int cellB = Cell + AxisOffset;
        if (sign > 0)
        {
            result[count++] = new GridFace(cellB, tangentAxis);
        }
        else
        {
            result[count++] = new GridFace(cellB + tangentOffset, tangentAxis);
        }
    }

    /// <summary>
    /// Gets the two tangent axes for this face (the axes the face extends along).
    /// </summary>
    public void GetTangentAxes(out Axis tan1, out Axis tan2)
    {
        switch (Axis)
        {
            case Axis.X:
                tan1 = Axis.Y;
                tan2 = Axis.Z;
                break;
            case Axis.Y:
                tan1 = Axis.X;
                tan2 = Axis.Z;
                break;
            case Axis.Z:
                tan1 = Axis.X;
                tan2 = Axis.Y;
                break;
            default:
                tan1 = Axis.X;
                tan2 = Axis.Y;
                break;
        }
    }

    public static Vector3Int AxisToOffset(Axis axis)
    {
        return axis switch
        {
            Axis.X => Vector3Int.right,
            Axis.Y => Vector3Int.up,
            Axis.Z => new Vector3Int(0, 0, 1),
            _ => Vector3Int.zero
        };
    }

    // Equality
    public bool Equals(GridFace other) => Cell.Equals(other.Cell) && Axis == other.Axis;
    public override bool Equals(object obj) => obj is GridFace other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(Cell, Axis);
    public override string ToString() => $"GridFace({Cell}, {Axis})";

    public static bool operator ==(GridFace left, GridFace right) => left.Equals(right);
    public static bool operator !=(GridFace left, GridFace right) => !left.Equals(right);
}
