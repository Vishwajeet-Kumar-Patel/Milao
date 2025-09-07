#if USE_FIREBASE
using System;
using System.Collections.Generic;
using UnityEngine;
// ...existing code...
#else
using UnityEngine;
using System.Collections.Generic;
public class Inventory : MonoBehaviour
{
    public static Inventory Instance;
    public Dictionary<TypePowerUp, int> InventoryItems { get; set; }
    public List<GameObject> PowerUpsObject { get; set; }

    public void UpdateInventoryUI(GameObject inventoryPanel) {}
    public List<GameObject> GetAvailablePowerUps() { return new List<GameObject>(); }
    public void SetAvailablePowerUps(List<GameObject> powerUps, Transform parent, StatePowerUp statePowerUp) {}
    public void ResetParentPowerUps(bool changeCheck) {}
    public void OrderPowerUps(Transform parent) {}
    public void SaveDataBase(Dictionary<string, object> data) {}
    public void SetPowerUpGame() {}
}
#endif