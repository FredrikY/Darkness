using UnityEngine;
using UnityEngine.InputSystem;

public class ReinforcerUseHandler : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Inventory _inventory;
    [SerializeField] private GridManager _gridManager;
    [SerializeField] private Transform _cameraTransform;
    [SerializeField] private ItemData _reinforcerItem;
    [SerializeField] private GameObject _reinforcedBoardPrefab;

    [Header("Settings")]
    [SerializeField] private float _useDistance = 10f;
    [SerializeField] private LayerMask _boardLayer;

    private InputSystem_Actions _input;
    private bool _attackPressed;

    private void Awake()
    {
        _input = new InputSystem_Actions();
    }

    private void OnEnable()
    {
        _input.Enable();
        _input.Player.Attack.performed += OnAttack;
    }

    private void OnDisable()
    {
        _input.Player.Attack.performed -= OnAttack;
        _input.Disable();
    }

    private void OnDestroy()
    {
        _input?.Dispose();
    }

    private void OnAttack(InputAction.CallbackContext context)
    {
        _attackPressed = true;
    }

    private void Update()
    {
        HandleReinforcerUse();
    }

    private void HandleReinforcerUse()
    {
        if (_inventory == null || _reinforcerItem == null) return;
        if (_gridManager == null || _cameraTransform == null) return;

        var selectedItem = _inventory.GetSelectedItem();
        if (selectedItem != _reinforcerItem) return;

        if (!_attackPressed) return;
        _attackPressed = false;

        if (Physics.Raycast(_cameraTransform.position, _cameraTransform.forward,
            out RaycastHit hit, _useDistance, _boardLayer))
        {
            var face = GetFaceFromHit(hit);
            if (face.HasValue)
            {
                TryReinforceBoard(face.Value);
            }
        }
    }

    private GridFace? GetFaceFromHit(RaycastHit hit)
    {
        var boardVisual = hit.collider.GetComponentInParent<BoardVisual>();
        if (boardVisual != null)
        {
            return boardVisual.Face;
        }
        return null;
    }

    private void TryReinforceBoard(GridFace face)
    {
        if (!_inventory.HasItem(_reinforcerItem, 1)) return;
        if (_reinforcedBoardPrefab == null) return;

        if (_gridManager.ReinforceBoard(face, _reinforcedBoardPrefab))
        {
            _inventory.TryRemoveItem(_reinforcerItem, 1);
        }
    }
}
