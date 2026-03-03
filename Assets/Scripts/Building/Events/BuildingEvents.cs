using System;
using UnityEngine;

[CreateAssetMenu(fileName = "BuildingEvents", menuName = "Building/BuildingEvents")]
public class BuildingEvents : ScriptableObject
{
    public event Action<GridFace, BoardData> OnBoardPlaced;
    public event Action<GridFace> OnBoardRemoved;
    public event Action<GridFace?> OnPreviewChanged;

    public void RaiseBoardPlaced(GridFace face, BoardData data)
    {
        OnBoardPlaced?.Invoke(face, data);
    }

    public void RaiseBoardRemoved(GridFace face)
    {
        OnBoardRemoved?.Invoke(face);
    }

    public void RaisePreviewChanged(GridFace? face)
    {
        OnPreviewChanged?.Invoke(face);
    }
}
