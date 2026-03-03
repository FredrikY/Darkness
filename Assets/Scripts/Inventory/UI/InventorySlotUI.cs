using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class InventorySlotUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("References")]
    [SerializeField] private Image _iconImage;
    [SerializeField] private TMP_Text _quantityText;
    [SerializeField] private Image _selectionHighlight;
    [SerializeField] private Button _slotButton;

    private int _slotIndex;
    private bool _isHotbar;
    private Inventory _inventory;
    private InventoryEvents _events;

    [Header("Drag & Drop")]
    [SerializeField] private Image _dropHighlight;
    [SerializeField] private Color _highlightColor = new Color(1f, 1f, 1f, 0.3f);
    private Color _originalHighlightColor;
    private InventoryDragController _dragController;

    public int SlotIndex => _slotIndex;
    public bool IsHotbar => _isHotbar;

    public void Initialize(int slotIndex, bool isHotbar, Inventory inventory, InventoryEvents events)
    {
        _slotIndex = slotIndex;
        _isHotbar = isHotbar;
        _inventory = inventory;
        _events = events;

        _events.OnSlotChanged += OnSlotChanged;
        _events.OnHotbarSelectionChanged += OnHotbarSelectionChanged;

        if (_slotButton != null)
            _slotButton.onClick.AddListener(OnSlotClicked);

        if (_dropHighlight != null)
            _originalHighlightColor = _dropHighlight.color;

        UpdateDisplay();
    }

    public void SetDragController(InventoryDragController controller)
    {
        _dragController = controller;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left || _dragController == null)
            return;

        var slot = _inventory.GetSlot(_slotIndex, _isHotbar);
        if (slot.IsEmpty)
            return;

        _dragController.BeginDrag(this, slot.Item, slot.Quantity);
        _dragController.ClearSourceSlot();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (_dragController != null && _dragController.IsDragging)
        {
            _dragController.UpdateDragPosition(eventData.position);
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (_dragController == null || !_dragController.IsDragging)
            return;

        if (eventData.pointerCurrentRaycast.gameObject == null || 
            eventData.pointerCurrentRaycast.gameObject.GetComponent<InventorySlotUI>() == null)
        {
            _dragController.CancelDrag();
        }
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (_dragController == null || !_dragController.IsDragging)
            return;

        _dragController.TryDropOnSlot(this);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_dragController == null || !_dragController.IsDragging || _dropHighlight == null)
            return;

        if (_dragController.GetSourceSlot() != this)
        {
            _dropHighlight.color = _highlightColor;
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (_dropHighlight != null)
        {
            _dropHighlight.color = _originalHighlightColor;
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Right || _dragController == null)
            return;

        _dragController.SplitFromSlot(this);
    }

    private void OnDestroy()
    {
        if (_events != null)
        {
            _events.OnSlotChanged -= OnSlotChanged;
            _events.OnHotbarSelectionChanged -= OnHotbarSelectionChanged;
        }
    }

    private void OnSlotChanged(int index, bool isHotbar)
    {
        if (index == _slotIndex && isHotbar == _isHotbar)
            UpdateDisplay();
    }

    private void OnHotbarSelectionChanged(int selectedIndex)
    {
        if (_isHotbar && _selectionHighlight != null)
            _selectionHighlight.enabled = (selectedIndex == _slotIndex);
    }

    public void UpdateDisplay()
    {
        if (_inventory == null) return;

        var slot = _isHotbar 
            ? _inventory.GetHotbarSlot(_slotIndex) 
            : _inventory.GetGridSlot(_slotIndex);

        if (slot.IsEmpty)
        {
            if (_iconImage != null) _iconImage.enabled = false;
            if (_quantityText != null) _quantityText.text = "";
        }
        else
        {
            if (_iconImage != null)
            {
                _iconImage.sprite = slot.Item.Icon;
                _iconImage.enabled = true;
            }
            if (_quantityText != null)
            {
                _quantityText.text = slot.Quantity > 1 ? slot.Quantity.ToString() : "";
            }
        }

        if (_selectionHighlight != null && _isHotbar)
            _selectionHighlight.enabled = (_inventory.SelectedHotbarIndex == _slotIndex);
    }

    private void OnSlotClicked()
    {
        // Handled by parent UI component for drag/drop
    }

    public void SetHighlight(bool highlighted)
    {
        if (_selectionHighlight != null)
            _selectionHighlight.enabled = highlighted;
    }
}
