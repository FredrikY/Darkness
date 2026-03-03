# Inventory Drag & Drop Design

**Date:** 2026-03-03  
**Goal:** Implement Minecraft-style drag & drop for inventory system

## Overview

Add drag & drop functionality allowing players to move items between inventory slots using mouse interactions. Supports cross-area movement (grid ↔ hotbar), stack splitting via right-click, and visual feedback.

## Architecture

### New Component: InventoryDragController

Centralized drag state manager. Slots delegate drag operations to this controller.

**Responsibilities:**
- Track current drag state (source slot, item, quantity)
- Handle cursor-following dragged item icon
- Coordinate between slots for drop operations
- Manage drop target highlighting

### Modified Classes

| Class | Changes |
|-------|---------|
| `InventorySlotUI` | Implement Unity drag interfaces, add drop highlight |
| `Inventory.cs` | Add cross-area movement and stacking methods |
| `InventoryEvents.cs` | Optional drag events for UI sync |

### Unchanged Classes

- `InventorySlot` (struct)
- `ItemData` (ScriptableObject)
- `HotbarUI`
- `InventoryGridUI`

## Data Structures

### InventoryDragController Fields

```
_draggedIcon: Image          // Cursor-following icon
_sourceSlotUI: InventorySlotUI
_sourceItem: ItemData
_sourceQuantity: int
_isDragging: bool
_inventory: Inventory
```

### Inventory.cs New Methods

```
MoveBetweenAreas(fromIndex, isFromHotbar, toIndex, isToHotbar)
StackItems(fromIndex, isFromHotbar, toIndex, isToHotbar)
SplitStack(index, isHotbar) -> (remainingCount, splitItem, splitCount)
```

### InventorySlotUI Additions

```
_dropHighlight: Image
_dragController: InventoryDragController
```

**Interfaces:** `IBeginDragHandler`, `IDragHandler`, `IEndDragHandler`, `IDropHandler`, `IPointerEnterHandler`, `IPointerExitHandler`

## Data Flow

### Left-Click Drag (Full Stack)

1. **OnBeginDrag** → Controller stores source, shows cursor icon, clears source visual
2. **OnDrag** → Controller updates icon position to cursor
3. **OnPointerEnter/Exit** → Controller highlights/unhighlights hovered slot
4. **OnDrop** → Controller checks target:
   - Empty slot → move item
   - Same item type → stack (up to max)
   - Different item → swap
5. **OnEndDrag** → Controller clears state, hides cursor icon

### Right-Click (Split Stack)

1. **OnPointerClick** (right button) → Controller checks slot
2. If dragging nothing → pick up half (rounded up), leave rest in source
3. If dragging item → place one item from dragged stack into slot
4. Continue until exhausted or slot full

### Cross-Area Movement

`MoveBetweenAreas()` handles grid↔hotbar swaps transparently.

## Error Handling

| Edge Case | Behavior |
|-----------|----------|
| Drop outside slot | Return item to source |
| Drop on same slot | Restore (no-op) |
| Stack overflow | Leave excess in source |
| Empty slot right-click | Ignore |
| Drag during inventory closed | Cancel drag |
| Rapid clicking | `_isDragging` flag prevents double-drags |

No exceptions thrown - all operations gracefully restore source state.

## Test Cases

### Manual Tests

1. Drag item to empty slot → item moves
2. Drag item onto same item type → stacks correctly
3. Drag item onto different item → swaps
4. Drag between grid and hotbar → works bidirectionally
5. Right-click full stack → picks up half
6. Right-click while dragging → places one item
7. Drop outside inventory → returns to source
8. Stack at max capacity → prevents overflow
9. Drag with inventory closed → ignored

### Optional Automated Tests

- `Inventory.MoveBetweenAreas()` unit tests
- `Inventory.StackItems()` boundary tests
- Stack splitting math verification

## File Structure

```
Assets/Scripts/Inventory/
├── Core/
│   ├── Inventory.cs (modified)
│   └── InventoryEvents.cs (modified - optional)
├── UI/
│   ├── InventorySlotUI.cs (modified)
│   └── InventoryDragController.cs (new)
```
