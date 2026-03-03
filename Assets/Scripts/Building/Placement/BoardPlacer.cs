using UnityEngine;

public class BoardPlacer : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform _cameraTransform;
    [SerializeField] private GridManager _gridManager;
    [SerializeField] private BuildingEvents _events;
    [SerializeField] private BoardPreview _preview;

    [Header("Settings")]
    [SerializeField] private float _placeDistance = 10f;
    [SerializeField] private LayerMask _placeLayer;
    [SerializeField] private bool _enablePlacement = true;

    [Header("Inventory")]
    [SerializeField] private Inventory _inventory;
    [SerializeField] private ItemData _boardItem;

    private PlayerInput _input;
    private FaceHit _currentFaceHit;

    private void Awake()
    {
        _input = new PlayerInput();

        if (_gridManager == null)
            _gridManager = GridManager.Instance;
    }

    private void OnDestroy()
    {
        _input?.Dispose();
    }

    private void Update()
    {
        if (!_enablePlacement || _gridManager == null)
        {
            _preview?.Hide();
            return;
        }

        _input.Update();
        DetectTargetFace();
        UpdatePreview();
        HandleInput();
    }

    private void DetectTargetFace()
    {
        _currentFaceHit = FaceDetector.DetectFace(
            _cameraTransform.position,
            _cameraTransform.forward,
            _placeDistance,
            _placeLayer,
            _gridManager
        );
    }

    private void UpdatePreview()
    {
        if (_preview == null) return;

        if (_currentFaceHit.IsValid && !_gridManager.HasBoard(_currentFaceHit.Face))
        {
            Quaternion rotation = _currentFaceHit.Face.GetRotation();
            _preview.Show(
                _currentFaceHit.Face,
                _currentFaceHit.WorldPosition,
                rotation,
                _currentFaceHit.IsValid);
            _events?.RaisePreviewChanged(_currentFaceHit.Face);
        }
        else
        {
            _preview.Hide();
            _events?.RaisePreviewChanged(null);
        }
    }

    private void HandleInput()
    {
        if (_input.PlaceBoardPressed)
        {
            TryPlaceBoard();
        }

        if (_input.RemoveBoardPressed)
        {
            TryRemoveBoard();
        }
    }

    private void TryPlaceBoard()
    {
        if (!_currentFaceHit.IsValid)
            return;

        if (_gridManager.HasBoard(_currentFaceHit.Face))
            return;

        if (_inventory != null && _boardItem != null)
        {
            if (!_inventory.HasItem(_boardItem, 1))
                return;
        }

        BoardData data = BoardData.Default;
        if (_gridManager.TryPlaceBoard(_currentFaceHit.Face, data))
        {
            if (_inventory != null && _boardItem != null)
            {
                _inventory.TryRemoveItem(_boardItem, 1);
            }
        }
    }

    private void TryRemoveBoard()
    {
        GridFace? faceToRemove = FaceDetector.DetectFaceForRemoval(
            _cameraTransform.position,
            _cameraTransform.forward,
            _placeDistance,
            _placeLayer
        );

        if (faceToRemove.HasValue)
        {
            _gridManager.RemoveBoard(faceToRemove.Value);
        }
    }

    public void SetPlacementEnabled(bool enabled)
    {
        _enablePlacement = enabled;
        if (!enabled)
        {
            _preview?.Hide();
        }
    }
}
