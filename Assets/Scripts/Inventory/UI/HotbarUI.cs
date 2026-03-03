using UnityEngine;

public class HotbarUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private InventorySlotUI[] _slotUIs;
    [SerializeField] private Inventory _inventory;
    [SerializeField] private InventoryEvents _events;
    [SerializeField] private InventoryDragController _dragController;

    private void Start()
    {
        InitializeSlots();
    }

    private void InitializeSlots()
    {
        for (int i = 0; i < _slotUIs.Length; i++)
        {
            _slotUIs[i].Initialize(i, true, _inventory, _events);
            _slotUIs[i].SetDragController(_dragController);
        }
    }

    public void RefreshAllSlots()
    {
        foreach (var slotUI in _slotUIs)
        {
            slotUI.UpdateDisplay();
        }
    }
}
