using UnityEngine;

public class BoardPreview : MonoBehaviour
{
    [SerializeField] private GameObject _previewPrefab;
    [SerializeField] private Material _validMaterial;
    [SerializeField] private Material _invalidMaterial;

    private GameObject _previewObject;
    private MeshRenderer[] _previewRenderers;
    private bool _isVisible;

    public bool IsVisible => _isVisible;
    public GridFace? CurrentFace { get; private set; }

    private void Awake()
    {
        if (_previewPrefab != null)
        {
            _previewObject = Instantiate(_previewPrefab, transform);
            _previewObject.name = "BoardPreview";
            _previewRenderers = _previewObject.GetComponentsInChildren<MeshRenderer>();
            SetVisible(false);
        }
    }

    public void Show(GridFace face, Vector3 worldPosition, Quaternion rotation, bool isValid)
    {
        if (_previewObject == null) return;

        CurrentFace = face;
        _previewObject.transform.position = worldPosition;
        _previewObject.transform.rotation = rotation;

        ApplyMaterial(isValid ? _validMaterial : _invalidMaterial);
        SetVisible(true);
    }

    public void Hide()
    {
        CurrentFace = null;
        SetVisible(false);
    }

    private void SetVisible(bool visible)
    {
        _isVisible = visible;
        if (_previewObject != null)
        {
            _previewObject.SetActive(visible);
        }
    }

    private void ApplyMaterial(Material material)
    {
        if (_previewRenderers == null || material == null) return;

        foreach (var renderer in _previewRenderers)
        {
            renderer.material = material;
        }
    }

    private void OnDestroy()
    {
        if (_previewObject != null)
        {
            Destroy(_previewObject);
        }
    }
}
