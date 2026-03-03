using System.Collections.Generic;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private int _gridSlotCount = 24;
    [SerializeField] private int _hotbarSlotCount = 8;

    [Header("References")]
    [SerializeField] private InventoryEvents _events;

    private InventorySlot[] _gridSlots;
    private InventorySlot[] _hotbarSlots;
    private int _selectedHotbarIndex;

    public int GridSlotCount => _gridSlotCount;
    public int HotbarSlotCount => _hotbarSlotCount;
    public int SelectedHotbarIndex => _selectedHotbarIndex;

    public event System.Action OnInventoryChanged;

    private void Awake()
    {
        _gridSlots = new InventorySlot[_gridSlotCount];
        _hotbarSlots = new InventorySlot[_hotbarSlotCount];
        
        for (int i = 0; i < _gridSlotCount; i++)
            _gridSlots[i] = InventorySlot.Empty;
        
        for (int i = 0; i < _hotbarSlotCount; i++)
            _hotbarSlots[i] = InventorySlot.Empty;
    }

    public InventorySlot GetGridSlot(int index)
    {
        if (index < 0 || index >= _gridSlotCount) return InventorySlot.Empty;
        return _gridSlots[index];
    }

    public InventorySlot GetHotbarSlot(int index)
    {
        if (index < 0 || index >= _hotbarSlotCount) return InventorySlot.Empty;
        return _hotbarSlots[index];
    }

    public ItemData GetSelectedItem()
    {
        return GetHotbarSlot(_selectedHotbarIndex).Item;
    }

    public int GetQuantity(ItemData item)
    {
        if (item == null) return 0;

        int total = 0;
        for (int i = 0; i < _gridSlotCount; i++)
        {
            if (_gridSlots[i].Item == item)
                total += _gridSlots[i].Quantity;
        }
        for (int i = 0; i < _hotbarSlotCount; i++)
        {
            if (_hotbarSlots[i].Item == item)
                total += _hotbarSlots[i].Quantity;
        }
        return total;
    }

    public bool HasItem(ItemData item, int quantity)
    {
        return GetQuantity(item) >= quantity;
    }

    public bool TryAddItem(ItemData item, int quantity)
    {
        if (item == null || quantity <= 0) return false;

        if (GetAvailableSpace(item) < quantity) return false;

        int remaining = quantity;

        // Fill existing stacks in hotbar first
        for (int i = 0; i < _hotbarSlotCount && remaining > 0; i++)
        {
            if (!_hotbarSlots[i].IsEmpty && _hotbarSlots[i].Item == item)
            {
                int canAdd = Mathf.Min(remaining, item.MaxStack - _hotbarSlots[i].Quantity);
                if (canAdd > 0)
                {
                    _hotbarSlots[i].Add(canAdd);
                    remaining -= canAdd;
                    _events?.RaiseSlotChanged(i, true);
                }
            }
        }

        // Fill existing stacks in grid
        for (int i = 0; i < _gridSlotCount && remaining > 0; i++)
        {
            if (!_gridSlots[i].IsEmpty && _gridSlots[i].Item == item)
            {
                int canAdd = Mathf.Min(remaining, item.MaxStack - _gridSlots[i].Quantity);
                if (canAdd > 0)
                {
                    _gridSlots[i].Add(canAdd);
                    remaining -= canAdd;
                    _events?.RaiseSlotChanged(i, false);
                }
            }
        }

        // Find empty slots in hotbar
        for (int i = 0; i < _hotbarSlotCount && remaining > 0; i++)
        {
            if (_hotbarSlots[i].IsEmpty)
            {
                int toAdd = Mathf.Min(remaining, item.MaxStack);
                _hotbarSlots[i] = new InventorySlot(item, toAdd);
                remaining -= toAdd;
                _events?.RaiseSlotChanged(i, true);
            }
        }

        // Find empty slots in grid
        for (int i = 0; i < _gridSlotCount && remaining > 0; i++)
        {
            if (_gridSlots[i].IsEmpty)
            {
                int toAdd = Mathf.Min(remaining, item.MaxStack);
                _gridSlots[i] = new InventorySlot(item, toAdd);
                remaining -= toAdd;
                _events?.RaiseSlotChanged(i, false);
            }
        }

        if (remaining == 0)
        {
            _events?.RaiseItemAdded(item.ItemId, quantity);
            OnInventoryChanged?.Invoke();
            return true;
        }

        return false;
    }

    public bool TryRemoveItem(ItemData item, int quantity)
    {
        if (!HasItem(item, quantity)) return false;

        int remaining = quantity;

        // Remove from hotbar (reverse order)
        for (int i = _hotbarSlotCount - 1; i >= 0 && remaining > 0; i--)
        {
            if (_hotbarSlots[i].Item == item)
            {
                int toRemove = Mathf.Min(remaining, _hotbarSlots[i].Quantity);
                _hotbarSlots[i].Remove(toRemove);
                remaining -= toRemove;
                _events?.RaiseSlotChanged(i, true);
            }
        }

        // Remove from grid (reverse order)
        for (int i = _gridSlotCount - 1; i >= 0 && remaining > 0; i--)
        {
            if (_gridSlots[i].Item == item)
            {
                int toRemove = Mathf.Min(remaining, _gridSlots[i].Quantity);
                _gridSlots[i].Remove(toRemove);
                remaining -= toRemove;
                _events?.RaiseSlotChanged(i, false);
            }
        }

        _events?.RaiseItemRemoved(item.ItemId, quantity);
        OnInventoryChanged?.Invoke();
        return true;
    }

    public void SelectHotbarSlot(int index)
    {
        if (index < 0 || index >= _hotbarSlotCount) return;
        _selectedHotbarIndex = index;
        _events?.RaiseHotbarSelectionChanged(index);
    }

    public void MoveGridSlot(int fromIndex, int toIndex)
    {
        if (fromIndex < 0 || fromIndex >= _gridSlotCount) return;
        if (toIndex < 0 || toIndex >= _gridSlotCount) return;

        var temp = _gridSlots[fromIndex];
        _gridSlots[fromIndex] = _gridSlots[toIndex];
        _gridSlots[toIndex] = temp;

        _events?.RaiseSlotChanged(fromIndex, false);
        _events?.RaiseSlotChanged(toIndex, false);
        OnInventoryChanged?.Invoke();
    }

    public void MoveHotbarSlot(int fromIndex, int toIndex)
    {
        if (fromIndex < 0 || fromIndex >= _hotbarSlotCount) return;
        if (toIndex < 0 || toIndex >= _hotbarSlotCount) return;

        var temp = _hotbarSlots[fromIndex];
        _hotbarSlots[fromIndex] = _hotbarSlots[toIndex];
        _hotbarSlots[toIndex] = temp;

        _events?.RaiseSlotChanged(fromIndex, true);
        _events?.RaiseSlotChanged(toIndex, true);
        OnInventoryChanged?.Invoke();
    }

    public void MoveBetweenAreas(int fromIndex, bool fromIsHotbar, int toIndex, bool toIsHotbar)
    {
        if (fromIsHotbar == toIsHotbar)
        {
            if (fromIsHotbar)
                MoveHotbarSlot(fromIndex, toIndex);
            else
                MoveGridSlot(fromIndex, toIndex);
            return;
        }

        ref InventorySlot fromSlot = ref fromIsHotbar ? ref _hotbarSlots[fromIndex] : ref _gridSlots[fromIndex];
        ref InventorySlot toSlot = ref toIsHotbar ? ref _hotbarSlots[toIndex] : ref _gridSlots[toIndex];

        (toSlot, fromSlot) = (fromSlot, toSlot);
        
        _events?.RaiseSlotChanged(fromIndex, fromIsHotbar);
        _events?.RaiseSlotChanged(toIndex, toIsHotbar);
        OnInventoryChanged?.Invoke();
    }

    public int StackItems(int fromIndex, bool fromIsHotbar, int toIndex, bool toIsHotbar)
    {
        ref InventorySlot fromSlot = ref fromIsHotbar ? ref _hotbarSlots[fromIndex] : ref _gridSlots[fromIndex];
        ref InventorySlot toSlot = ref toIsHotbar ? ref _hotbarSlots[toIndex] : ref _gridSlots[toIndex];

        if (fromSlot.IsEmpty || toSlot.IsEmpty || fromSlot.Item != toSlot.Item)
            return 0;

        int maxStack = toSlot.Item.MaxStack;
        int spaceAvailable = maxStack - toSlot.Quantity;
        int amountToTransfer = Mathf.Min(spaceAvailable, fromSlot.Quantity);

        if (amountToTransfer <= 0)
            return 0;

        toSlot = new InventorySlot(toSlot.Item, toSlot.Quantity + amountToTransfer);
        fromSlot = fromSlot.Quantity - amountToTransfer == 0 
            ? InventorySlot.Empty 
            : new InventorySlot(fromSlot.Item, fromSlot.Quantity - amountToTransfer);

        _events?.RaiseSlotChanged(fromIndex, fromIsHotbar);
        _events?.RaiseSlotChanged(toIndex, toIsHotbar);
        OnInventoryChanged?.Invoke();

        return amountToTransfer;
    }

    public (ItemData item, int splitCount) SplitStack(int index, bool isHotbar)
    {
        ref InventorySlot slot = ref isHotbar ? ref _hotbarSlots[index] : ref _gridSlots[index];

        if (slot.IsEmpty)
            return (null, 0);

        int splitCount = Mathf.CeilToInt(slot.Quantity / 2f);
        int remaining = slot.Quantity - splitCount;

        ItemData item = slot.Item;
        slot = remaining == 0 ? InventorySlot.Empty : new InventorySlot(slot.Item, remaining);

        _events?.RaiseSlotChanged(index, isHotbar);
        OnInventoryChanged?.Invoke();

        return (item, splitCount);
    }

    public bool PlaceOneItem(int toIndex, bool toIsHotbar, ItemData item, ref int heldQuantity)
    {
        if (item == null || heldQuantity <= 0)
            return false;

        ref InventorySlot toSlot = ref toIsHotbar ? ref _hotbarSlots[toIndex] : ref _gridSlots[toIndex];

        if (toSlot.IsEmpty)
        {
            toSlot = new InventorySlot(item, 1);
            heldQuantity--;
        }
        else if (toSlot.Item == item && toSlot.Quantity < item.MaxStack)
        {
            toSlot = new InventorySlot(item, toSlot.Quantity + 1);
            heldQuantity--;
        }
        else
        {
            return false;
        }

        _events?.RaiseSlotChanged(toIndex, toIsHotbar);
        OnInventoryChanged?.Invoke();
        return true;
    }

    public void SetSlot(int index, bool isHotbar, InventorySlot slot)
    {
        if (isHotbar)
            _hotbarSlots[index] = slot;
        else
            _gridSlots[index] = slot;

        _events?.RaiseSlotChanged(index, isHotbar);
        OnInventoryChanged?.Invoke();
    }

    public InventorySlot GetSlot(int index, bool isHotbar)
    {
        return isHotbar ? _hotbarSlots[index] : _gridSlots[index];
    }

    public void SplitGridStack(int index)
    {
        if (index < 0 || index >= _gridSlotCount) return;
        if (_gridSlots[index].IsEmpty) return;

        int half = _gridSlots[index].Quantity / 2;
        if (half == 0) return;

        int emptyIndex = FindEmptyGridSlot();
        if (emptyIndex == -1) return;

        var item = _gridSlots[index].Item;
        _gridSlots[index].Remove(half);
        _gridSlots[emptyIndex] = new InventorySlot(item, half);

        _events?.RaiseSlotChanged(index, false);
        _events?.RaiseSlotChanged(emptyIndex, false);
        OnInventoryChanged?.Invoke();
    }

    private int FindEmptyGridSlot()
    {
        for (int i = 0; i < _gridSlotCount; i++)
        {
            if (_gridSlots[i].IsEmpty) return i;
        }
        return -1;
    }

    private int GetAvailableSpace(ItemData item)
    {
        int space = 0;

        for (int i = 0; i < _hotbarSlotCount; i++)
        {
            if (_hotbarSlots[i].IsEmpty)
                space += item.MaxStack;
            else if (_hotbarSlots[i].Item == item)
                space += item.MaxStack - _hotbarSlots[i].Quantity;
        }

        for (int i = 0; i < _gridSlotCount; i++)
        {
            if (_gridSlots[i].IsEmpty)
                space += item.MaxStack;
            else if (_gridSlots[i].Item == item)
                space += item.MaxStack - _gridSlots[i].Quantity;
        }

        return space;
    }

    public void DebugAddItem(ItemData item, int quantity)
    {
        TryAddItem(item, quantity);
    }
}
