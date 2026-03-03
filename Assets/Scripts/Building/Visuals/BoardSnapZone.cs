using UnityEngine;

public class BoardSnapZone : MonoBehaviour
{
    [SerializeField] private GridFace _targetFace;

    public GridFace TargetFace => _targetFace;

    public void Initialize(GridFace targetFace, float cellSize)
    {
        _targetFace = targetFace;
        name = $"SnapZone_{targetFace}";

        Vector3 worldPos = targetFace.GetWorldPosition(cellSize);
        transform.position = worldPos;
        transform.rotation = targetFace.GetRotation();
    }

    public static BoardSnapZone Create(
        GameObject parent, GridFace targetFace, float cellSize, float triggerSize)
    {
        GameObject zoneObj = new GameObject("SnapZone");
        zoneObj.transform.SetParent(parent.transform);
        zoneObj.layer = parent.layer;

        BoxCollider trigger = zoneObj.AddComponent<BoxCollider>();
        trigger.isTrigger = true;
        trigger.size = new Vector3(cellSize, triggerSize, cellSize);

        BoardSnapZone zone = zoneObj.AddComponent<BoardSnapZone>();
        zone.Initialize(targetFace, cellSize);

        return zone;
    }
}
