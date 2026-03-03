using UnityEngine;

public readonly struct FaceHit
{
    public readonly GridFace Face;
    public readonly bool IsValid;
    public readonly Vector3 WorldPosition;

    public FaceHit(GridFace face, bool isValid, Vector3 worldPosition)
    {
        Face = face;
        IsValid = isValid;
        WorldPosition = worldPosition;
    }

    public static FaceHit Invalid => new FaceHit(default, false, Vector3.zero);
}

public static class FaceDetector
{
    /// <summary>
    /// Detects which grid face the player is aiming at.
    /// Priority: SnapZone hit > surface hit > free placement.
    /// </summary>
    public static FaceHit DetectFace(
        Vector3 origin,
        Vector3 direction,
        float maxDistance,
        LayerMask layerMask,
        GridManager gridManager)
    {
        if (gridManager == null)
            return FaceHit.Invalid;

        if (Physics.Raycast(origin, direction, out RaycastHit hit, maxDistance, layerMask))
        {
            // Priority 1: Snap zone — provides exact target face
            BoardSnapZone snapZone = hit.collider.GetComponent<BoardSnapZone>();
            if (snapZone != null)
            {
                GridFace face = snapZone.TargetFace;
                Vector3 worldPos = face.GetWorldPosition(gridManager.CellSize);
                bool isValid = !gridManager.HasBoard(face) && gridManager.HasAdjacentBoard(face);
                return new FaceHit(face, isValid, worldPos);
            }

            // Priority 2: Hit an existing surface — determine which face from hit geometry
            return DetectFromHit(hit, gridManager);
        }

        // Priority 3: No hit — free placement only for the first board
        if (gridManager.BoardCount == 0)
        {
            return GetFreePlacementFace(origin, direction, maxDistance, gridManager);
        }

        return FaceHit.Invalid;
    }

    /// <summary>
    /// Detects a placed board for removal by raycasting and finding a BoardVisual.
    /// </summary>
    public static GridFace? DetectFaceForRemoval(
        Vector3 origin,
        Vector3 direction,
        float maxDistance,
        LayerMask layerMask)
    {
        if (!Physics.Raycast(origin, direction, out RaycastHit hit, maxDistance, layerMask))
            return null;

        BoardVisual boardVisual = hit.collider.GetComponentInParent<BoardVisual>();
        if (boardVisual != null)
            return boardVisual.Face;

        return null;
    }

    private static FaceHit GetFreePlacementFace(
        Vector3 origin, Vector3 direction, float distance, GridManager gridManager)
    {
        Vector3 placePos = origin + direction * (distance * 0.5f);
        GridFace face = DetermineFreePlacementFace(placePos, direction, gridManager);
        Vector3 worldPos = face.GetWorldPosition(gridManager.CellSize);
        return new FaceHit(face, true, worldPos);
    }

    /// <summary>
    /// For free placement (no surface hit), picks the face most perpendicular
    /// to the camera direction.
    /// </summary>
    private static GridFace DetermineFreePlacementFace(
        Vector3 worldPos, Vector3 lookDirection, GridManager gridManager)
    {
        Vector3Int cell = gridManager.WorldToCell(worldPos);

        float ax = Mathf.Abs(lookDirection.x);
        float ay = Mathf.Abs(lookDirection.y);
        float az = Mathf.Abs(lookDirection.z);

        // Pick the axis most aligned with look direction, place face perpendicular
        // The face the player "sees" faces them, so we use the axis most aligned with look.
        if (ay >= ax && ay >= az)
        {
            // Looking mostly up/down → place a Y-face
            Vector3Int dir = lookDirection.y > 0 ? Vector3Int.up : Vector3Int.down;
            return GridFace.FromCellAndDirection(cell, dir);
        }
        if (ax >= az)
        {
            // Looking mostly left/right → place an X-face
            Vector3Int dir = lookDirection.x > 0 ? Vector3Int.right : Vector3Int.left;
            return GridFace.FromCellAndDirection(cell, dir);
        }
        // Looking mostly forward/back → place a Z-face
        Vector3Int zDir = lookDirection.z > 0 ? new Vector3Int(0, 0, 1) : new Vector3Int(0, 0, -1);
        return GridFace.FromCellAndDirection(cell, zDir);
    }

    /// <summary>
    /// Determines the grid face from a physics raycast hit on an existing surface.
    /// Uses the hit position relative to the nearest cell center to determine
    /// which face boundary the hit is closest to.
    /// </summary>
    private static FaceHit DetectFromHit(RaycastHit hit, GridManager gridManager)
    {
        Vector3 hitPoint = hit.point;
        Vector3Int hitCell = gridManager.WorldToCell(hitPoint);
        Vector3 cellCenter = gridManager.CellToWorld(hitCell);
        Vector3 local = hitPoint - cellCenter;
        float halfSize = gridManager.CellSize * 0.5f;

        // Determine which face the hit is nearest to using local position within cell
        GridFace face = DetermineFaceFromLocalPosition(hitCell, local, halfSize);
        Vector3 worldPos = face.GetWorldPosition(gridManager.CellSize);
        bool isValid = !gridManager.HasBoard(face) && gridManager.HasAdjacentBoard(face);

        return new FaceHit(face, isValid, worldPos);
    }

    /// <summary>
    /// Given a local position within a cell (relative to cell center),
    /// returns the grid face nearest to that position.
    /// </summary>
    private static GridFace DetermineFaceFromLocalPosition(
        Vector3Int cell, Vector3 localPos, float halfSize)
    {
        float ax = Mathf.Abs(localPos.x);
        float ay = Mathf.Abs(localPos.y);
        float az = Mathf.Abs(localPos.z);

        // The component furthest from center (closest to a face boundary) determines the face
        if (ax >= ay && ax >= az)
        {
            Vector3Int dir = localPos.x > 0 ? Vector3Int.right : Vector3Int.left;
            return GridFace.FromCellAndDirection(cell, dir);
        }
        if (ay >= az)
        {
            Vector3Int dir = localPos.y > 0 ? Vector3Int.up : Vector3Int.down;
            return GridFace.FromCellAndDirection(cell, dir);
        }

        Vector3Int zDir = localPos.z > 0 ? new Vector3Int(0, 0, 1) : new Vector3Int(0, 0, -1);
        return GridFace.FromCellAndDirection(cell, zDir);
    }
}
