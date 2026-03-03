using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventoryDragController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Inventory _inventory;
    [SerializeField] private Canvas _canvas;
    [SerializeField] private GameObject _draggedIconPrefab;

    private GameObject _draggedIconInstance;
    private Image _draggedIconImage;
    private InventorySlotUI _sourceSlot;
    private ItemData _draggedItem;
    private int _draggedQuantity;
    private bool _isDragging;

    public bool IsDragging => _isDragging;
    public ItemData DraggedItem => _draggedItem;
    public int DraggedQuantity => _draggedQuantity;

    private void Awake()
    {
        _draggedIconInstance = Instantiate(_draggedIconPrefab, _canvas.transform);
        _draggedIconInstance.SetActive(false);
        _draggedIconImage = _draggedIconInstance.GetComponent<Image>();
    }

    public void BeginDrag(InventorySlotUI slot, ItemData item, int quantity)
    {
        _sourceSlot = slot;
        _draggedItem = item;
        _draggedQuantity = quantity;
        _isDragging = true;

        _draggedIconImage.sprite = item.Icon;
        _draggedIconImage.SetNativeSize();
        _draggedIconInstance.SetActive(true);
        UpdateDragPosition(Input.mousePosition);
    }

    public void EndDrag()
    {
        _isDragging = false;
        _sourceSlot = null;
        _draggedItem = null;
        _draggedQuantity = 0;
        _draggedIconInstance.SetActive(false);
    }

    public void CancelDrag()
    {
        if (_sourceSlot != null && _draggedItem != null)
        {
            _inventory.SetSlot(_sourceSlot.SlotIndex, _sourceSlot.IsHotbar, 
                new InventorySlot(_draggedItem, _draggedQuantity));
        }
        EndDrag();
    }

    public void UpdateDragPosition(Vector2 screenPosition)
    {
        if (_draggedIconInstance != null)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _canvas.transform as RectTransform,
                screenPosition,
                _canvas.worldCamera,
                out Vector2 localPoint);
            _draggedIconInstance.GetComponent<RectTransform>().anchoredPosition = localPoint;
        }
    }

    public void ClearSourceSlot()
    {
        if (_sourceSlot != null)
        {
            _inventory.SetSlot(_sourceSlot.SlotIndex, _sourceSlot.IsHotbar, InventorySlot.Empty);
        }
    }

    public bool TryDropOnSlot(InventorySlotUI targetSlot)
    {
        if (!_isDragging || _draggedItem == null)
            return false;

        var targetItem = _inventory.GetSlot(targetSlot.SlotIndex, targetSlot.IsHotbar);

        if (targetItem.IsEmpty)
        {
            _inventory.SetSlot(targetSlot.SlotIndex, targetSlot.IsHotbar, 
                new InventorySlot(_draggedItem, _draggedQuantity));
            EndDrag();
            return true;
        }

        if (targetItem.Item == _draggedItem)
        {
            int remaining = _draggedQuantity;
            int maxStack = _draggedItem.MaxStack;
            int space = maxStack - targetItem.Quantity;
            int toAdd = Mathf.Min(space, remaining);

            _inventory.SetSlot(targetSlot.SlotIndex, targetSlot.IsHotbar,
                new InventorySlot(_draggedItem, targetItem.Quantity + toAdd));

            _draggedQuantity -= toAdd;

            if (_draggedQuantity <= 0)
            {
                EndDrag();
            }
            else if (_sourceSlot != null)
            {
                _inventory.SetSlot(_sourceSlot.SlotIndex, _sourceSlot.IsHotbar,
                    new InventorySlot(_draggedItem, _draggedQuantity));
            }
            return true;
        }

        _inventory.SetSlot(targetSlot.SlotIndex, targetSlot.IsHotbar,
            new InventorySlot(_draggedItem, _draggedQuantity));
        _inventory.SetSlot(_sourceSlot.SlotIndex, _sourceSlot.IsHotbar, targetItem);
        EndDrag();
        return true;
    }

    public void SplitFromSlot(InventorySlotUI slot)
    {
        var slotData = _inventory.GetSlot(slot.SlotIndex, slot.IsHotbar);
        if (slotData.IsEmpty)
            return;

        if (!_isDragging)
        {
            int half = Mathf.CeilToInt(slotData.Quantity / 2f);
            _inventory.SetSlot(slot.SlotIndex, slot.IsHotbar,
                new InventorySlot(slotData.Item, slotData.Quantity - half));
            BeginDrag(slot, slotData.Item, half);
        }
        else if (_draggedItem == slotData.Item && slotData.Quantity < _draggedItem.MaxStack)
        {
            int held = _draggedQuantity;
            if (_inventory.PlaceOneItem(slot.SlotIndex, slot.IsHotbar, _draggedItem, ref held))
            {
                _draggedQuantity = held;
                if (_draggedQuantity <= 0)
                    EndDrag();
            }
        }
    }

    public InventorySlotUI GetSourceSlot() => _sourceSlot;
}
