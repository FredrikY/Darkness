using UnityEngine;
using UnityEngine.InputSystem;

public class InventoryDebug : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Inventory _inventory;
    
    [Header("Test Items")]
    [SerializeField] private ItemData _testBoard;
    [SerializeField] private ItemData _testReinforcer;
    [SerializeField] private int _testQuantity = 5;

    private void Start(){
        if (_testBoard != null)
            {
                _inventory.DebugAddItem(_testBoard, _testQuantity);
                Debug.Log($"[InventoryDebug] Added {_testQuantity} boards");
            }
        if (_testReinforcer != null)
            {
                _inventory.DebugAddItem(_testReinforcer, _testQuantity);
                Debug.Log($"[InventoryDebug] Added {_testQuantity} reinforcers");
            }
    }

    private void Update()
    {
        if (_inventory == null) return;

        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        if (keyboard.numpad1Key.wasPressedThisFrame)
        {
            if (_testBoard != null)
            {
                _inventory.DebugAddItem(_testBoard, _testQuantity);
                Debug.Log($"[InventoryDebug] Added {_testQuantity} boards");
            }
        }
        
        if (keyboard.numpad2Key.wasPressedThisFrame)
        {
            if (_testReinforcer != null)
            {
                _inventory.DebugAddItem(_testReinforcer, _testQuantity);
                Debug.Log($"[InventoryDebug] Added {_testQuantity} reinforcers");
            }
        }
    }
}
