using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class BoardVisual : MonoBehaviour
{
    [SerializeField] private MeshRenderer _meshRenderer;
    [SerializeField] private float _snapTriggerThickness = 0.1f;

    private GridFace _face;
    private bool _isPreview;
    private readonly List<BoardSnapZone> _snapZones = new();

    public GridFace Face => _face;
    public bool IsPreview => _isPreview;

    public void Initialize(GridFace face)
    {
        _face = face;
        _isPreview = false;
        name = $"Board_{face}";

        CreateSnapZones();
    }

    public void CleanupSnapZones()
    {
        foreach (var zone in _snapZones)
        {
            if (zone != null)
                Destroy(zone.gameObject);
        }
        _snapZones.Clear();
    }

    private void OnDestroy()
    {
        CleanupSnapZones();
    }

    private void CreateSnapZones()
    {
        CleanupSnapZones();

        if (GridManager.Instance == null) return;

        float cellSize = GridManager.Instance.CellSize;

        // Get all empty adjacent faces from the GridManager
        var emptyFaces = new List<GridFace>();
        GridManager.Instance.GetEmptyAdjacentFaces(_face, emptyFaces);

        foreach (GridFace adjacentFace in emptyFaces)
        {
            BoardSnapZone zone = BoardSnapZone.Create(
                gameObject, adjacentFace, cellSize, _snapTriggerThickness);
            _snapZones.Add(zone);
        }
    }

    public void SetPreviewMode(bool isPreview)
    {
        _isPreview = isPreview;

        if (_meshRenderer != null)
        {
            foreach (var mat in _meshRenderer.materials)
            {
                if (isPreview)
                {
                    mat.SetFloat("_Mode", 3);
                    mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    mat.SetInt("_ZWrite", 0);
                    mat.DisableKeyword("_ALPHATEST_ON");
                    mat.EnableKeyword("_ALPHABLEND_ON");
                    mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                    mat.renderQueue = 3000;
                    Color c = mat.color;
                    c.a = 0.5f;
                    mat.color = c;
                }
            }
        }
    }

    public void SetValidHighlight(bool isValid)
    {
        if (_meshRenderer == null) return;

        foreach (var mat in _meshRenderer.materials)
        {
            mat.color = isValid
                ? new Color(0.2f, 1f, 0.2f, _isPreview ? 0.5f : 1f)
                : new Color(1f, 0.2f, 0.2f, _isPreview ? 0.5f : 1f);
        }
    }
}
