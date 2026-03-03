using UnityEngine;

public class InventoryGridUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private InventorySlotUI[] _slotUIs;
    [SerializeField] private Inventory _inventory;
    [SerializeField] private InventoryEvents _events;
    [SerializeField] private InventoryDragController _dragController;
    [SerializeField] private GameObject _panel;

    private bool _isOpen;

    public bool IsOpen => _isOpen;

    private void Start()
    {
        InitializeSlots();
        Close();
    }

    private void InitializeSlots()
    {
        for (int i = 0; i < _slotUIs.Length; i++)
        {
            _slotUIs[i].Initialize(i, false, _inventory, _events);
            _slotUIs[i].SetDragController(_dragController);
        }
    }

    public void Toggle()
    {
        if (_isOpen)
            Close();
        else
            Open();
    }

    public void Open()
    {
        _isOpen = true;
        if (_panel != null)
            _panel.SetActive(true);
        _events?.RaiseInventoryOpened();
    }

    public void Close()
    {
        _isOpen = false;
        if (_panel != null)
            _panel.SetActive(false);
        _events?.RaiseInventoryClosed();
    }

    public void RefreshAllSlots()
    {
        foreach (var slotUI in _slotUIs)
        {
            slotUI.UpdateDisplay();
        }
    }
}
