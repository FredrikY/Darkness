# Inventory Drag & Drop Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Implement Minecraft-style drag & drop for inventory with cursor-following icon, stack splitting, and cross-area movement.

**Architecture:** Centralized InventoryDragController manages all drag state. InventorySlotUI implements Unity drag interfaces and delegates to controller. Inventory.cs gains cross-area movement methods.

**Tech Stack:** Unity 2022+, Unity UI (UnityEngine.UI), C# event system

---

## Task 1: Add Cross-Area Movement to Inventory.cs

**Files:**
- Modify: `Assets/Scripts/Inventory/Core/Inventory.cs`

**Step 1: Add MoveBetweenAreas method**

Add after existing `MoveGridSlot` method:

```csharp
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
    
    _events?.RaiseOnSlotChanged(fromIndex, fromIsHotbar);
    _events?.RaiseOnSlotChanged(toIndex, toIsHotbar);
    OnInventoryChanged?.Invoke();
}
```

**Step 2: Add StackItems method**

```csharp
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

    _events?.RaiseOnSlotChanged(fromIndex, fromIsHotbar);
    _events?.RaiseOnSlotChanged(toIndex, toIsHotbar);
    OnInventoryChanged?.Invoke();

    return amountToTransfer;
}
```

**Step 3: Add SplitStack method**

```csharp
public (ItemData item, int splitCount) SplitStack(int index, bool isHotbar)
{
    ref InventorySlot slot = ref isHotbar ? ref _hotbarSlots[index] : ref _gridSlots[index];

    if (slot.IsEmpty)
        return (null, 0);

    int splitCount = Mathf.CeilToInt(slot.Quantity / 2f);
    int remaining = slot.Quantity - splitCount;

    slot = remaining == 0 ? InventorySlot.Empty : new InventorySlot(slot.Item, remaining);

    _events?.RaiseOnSlotChanged(index, isHotbar);
    OnInventoryChanged?.Invoke();

    return (slot.Item, splitCount);
}
```

**Step 4: Add PlaceOneItem method (for right-click placing)**

```csharp
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

    _events?.RaiseOnSlotChanged(toIndex, toIsHotbar);
    OnInventoryChanged?.Invoke();
    return true;
}
```

**Step 5: Add SetSlot method (for restoring items)**

```csharp
public void SetSlot(int index, bool isHotbar, InventorySlot slot)
{
    if (isHotbar)
        _hotbarSlots[index] = slot;
    else
        _gridSlots[index] = slot;

    _events?.RaiseOnSlotChanged(index, isHotbar);
    OnInventoryChanged?.Invoke();
}
```

**Step 6: Commit**

```bash
git add Assets/Scripts/Inventory/Core/Inventory.cs
git commit -m "feat(inventory): add cross-area movement and stack manipulation methods"
```

---

## Task 2: Create InventoryDragController

**Files:**
- Create: `Assets/Scripts/Inventory/UI/InventoryDragController.cs`

**Step 1: Create the controller script**

```csharp
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
```

**Step 2: Add GetSlot method to Inventory.cs**

Add to Inventory.cs:

```csharp
public InventorySlot GetSlot(int index, bool isHotbar)
{
    return isHotbar ? _hotbarSlots[index] : _gridSlots[index];
}
```

**Step 3: Commit**

```bash
git add Assets/Scripts/Inventory/UI/InventoryDragController.cs Assets/Scripts/Inventory/Core/Inventory.cs
git commit -m "feat(inventory): add InventoryDragController for drag state management"
```

---

## Task 3: Modify InventorySlotUI for Drag Interfaces

**Files:**
- Modify: `Assets/Scripts/Inventory/UI/InventorySlotUI.cs`

**Step 1: Add fields and drag controller reference**

Add to existing fields:

```csharp
[Header("Drag & Drop")]
[SerializeField] private Image _dropHighlight;
[SerializeField] private Color _highlightColor = new Color(1f, 1f, 1f, 0.3f);
private Color _originalColor;
private InventoryDragController _dragController;
private bool _isHovered;
```

**Step 2: Add properties and Initialize modification**

Add after existing properties:

```csharp
public int SlotIndex { get; private set; }
public bool IsHotbar { get; private set; }

public void SetDragController(InventoryDragController controller)
{
    _dragController = controller;
}
```

Modify `Initialize` method to store drag controller and slot info:

```csharp
public void Initialize(int slotIndex, bool isHotbar, Inventory inventory, InventoryEvents events)
{
    _slotIndex = slotIndex;
    _isHotbar = isHotbar;
    _inventory = inventory;
    _events = events;
    SlotIndex = slotIndex;
    IsHotbar = isHotbar;

    if (_dropHighlight != null)
        _originalColor = _dropHighlight.color;

    UpdateDisplay();
    SubscribeToEvents();
}
```

**Step 3: Implement drag interfaces**

Add to class declaration:

```csharp
public class InventorySlotUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
```

**Step 4: Implement interface methods**

```csharp
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
        _isHovered = true;
    }
}

public void OnPointerExit(PointerEventData eventData)
{
    if (_dropHighlight != null)
    {
        _dropHighlight.color = _originalColor;
        _isHovered = false;
    }
}

public void OnPointerClick(PointerEventData eventData)
{
    if (eventData.button != PointerEventData.InputButton.Right || _dragController == null)
        return;

    _dragController.SplitFromSlot(this);
}
```

**Step 5: Update OnDestroy to reset highlight**

```csharp
private void OnDestroy()
{
    UnsubscribeFromEvents();
}
```

**Step 6: Commit**

```bash
git add Assets/Scripts/Inventory/UI/InventorySlotUI.cs
git commit -m "feat(inventory): add drag interfaces to InventorySlotUI"
```

---

## Task 4: Create Dragged Icon Prefab

**Files:**
- Create prefab in Editor (documented in this task)

**Step 1: Create the dragged icon GameObject**

In Unity Editor:
1. Right-click in `Assets/Prefabs` folder → Create → UI → Image
2. Name it `DraggedIcon`
3. Set RectTransform:
   - Width: 64
   - Height: 64
4. Set Image component:
   - Raycast Target: unchecked
5. Add Canvas Group component:
   - Alpha: 1
   - Interactable: unchecked
   - Block Raycasts: unchecked

**Step 2: Create prefab**

Drag the GameObject from Hierarchy to `Assets/Prefabs/Inventory/` folder as a prefab.

**Step 3: Delete from scene**

Remove the GameObject from the Hierarchy (prefab is saved).

**Step 4: No commit needed (prefab is binary)**

---

## Task 5: Wire Up Drag Controller in Scene

**Files:**
- Scene modification (no code)

**Step 1: Add DragController to Inventory UI**

In Unity Editor:
1. Select the `InventoryGridUI` GameObject (or create empty GameObject under Canvas)
2. Add Component → `InventoryDragController`
3. Assign fields:
   - Inventory: drag the player's Inventory component
   - Canvas: drag the main Canvas
   - Dragged Icon Prefab: drag the `DraggedIcon` prefab

**Step 2: Wire slot UIs**

Ensure all `InventorySlotUI` components have:
- `_dropHighlight` assigned (can use the slot background Image)
- Call `SetDragController()` from parent UI script

**Step 3: Modify InventoryGridUI to pass drag controller**

Add to `Assets/Scripts/Inventory/UI/InventoryGridUI.cs`:

```csharp
[SerializeField] private InventoryDragController _dragController;

private void Start()
{
    InitializeSlots();
}

private void InitializeSlots()
{
    for (int i = 0; i < _slots.Length; i++)
    {
        _slots[i].Initialize(i, false, _inventory, _events);
        _slots[i].SetDragController(_dragController);
    }
}
```

**Step 4: Modify HotbarUI similarly**

Add to `Assets/Scripts/Inventory/UI/HotbarUI.cs`:

```csharp
[SerializeField] private InventoryDragController _dragController;

public void Initialize(Inventory inventory, InventoryEvents events)
{
    for (int i = 0; i < _slots.Length; i++)
    {
        _slots[i].Initialize(i, true, inventory, events);
        _slots[i].SetDragController(_dragController);
    }
}
```

**Step 5: Commit**

```bash
git add Assets/Scripts/Inventory/UI/InventoryGridUI.cs Assets/Scripts/Inventory/UI/HotbarUI.cs
git commit -m "feat(inventory): wire drag controller into UI panels"
```

---

## Task 6: Manual Testing

**Files:**
- None (testing only)

**Step 1: Test basic drag**

1. Open inventory
2. Left-click and drag an item
3. Verify icon follows cursor
4. Drop on empty slot
5. Verify item moves

**Step 2: Test stacking**

1. Drag item onto same item type
2. Verify items stack
3. Test stack overflow returns excess

**Step 3: Test swapping**

1. Drag item onto different item type
2. Verify items swap

**Step 4: Test cross-area**

1. Drag from grid to hotbar
2. Drag from hotbar to grid
3. Verify both work

**Step 5: Test right-click split**

1. Right-click a stack
2. Verify half picked up
3. Right-click empty slot
4. Verify one item placed

**Step 6: Test cancel**

1. Start drag
2. Drop outside inventory
3. Verify item returns to source

---

## Summary

| Task | Files | Commits |
|------|-------|---------|
| 1. Inventory methods | Inventory.cs | 1 |
| 2. DragController | InventoryDragController.cs, Inventory.cs | 1 |
| 3. SlotUI interfaces | InventorySlotUI.cs | 1 |
| 4. Prefab | DraggedIcon prefab | 0 (binary) |
| 5. Wire up | InventoryGridUI.cs, HotbarUI.cs | 1 |
| 6. Testing | Manual | 0 |

**Total: 4 commits, 1 prefab**
