Manual Steps Required
Task 4: Create Prefab
1. In Unity, create UI Image in Assets/Prefabs/Inventory/
2. Name: DraggedIcon, Size: 64x64
3. Disable Raycast Target, add Canvas Group (no raycast/interact)
Task 5 Scene Wiring:
1. Add InventoryDragController component to Inventory UI GameObject
2. Assign: Inventory, Canvas, DraggedIcon prefab
3. On InventoryGridUI & HotbarUI: assign Drag Controller field
4. On each InventorySlotUI: assign Drop Highlight (use slot background Image)
Task 6: Test in Play Mode
- Drag items, stack, swap, right-click split, cross-area movement
