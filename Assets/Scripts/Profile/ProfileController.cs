using System;
using UnityEngine;

public class ProfileController : MonoBehaviour
{
    // Declaration of the 'inventoryPanel' variable of the 'GameObject' data type
    [SerializeField] GameObject inventoryPanel;
    [SerializeField] AvatarController avatar;

    void OnEnable()
    {
        UpdateProfileData();
    }

    void OnDisable()
    {
        if (Inventory.Instance == null)
        {
            Debug.LogWarning("Inventory.Instance is null in ProfileController.OnDisable");
            return;
        }
        Inventory.Instance.ResetParentPowerUps(false);
        if (Inventory.Instance.PowerUpsObject != null)
        {
            Inventory.Instance.PowerUpsObject.ForEach(powerUp =>
            {
                if (powerUp == null) return;
                PowerUp power = powerUp.GetComponent<PowerUp>();
                if (power != null && power.IsInTransitionDescription)
                    power.DisableDescription();
            });
        }
    }

    /// <summary>
    /// Updates the profile data, including power-up inventory and avatar.
    /// </summary>
    void UpdateProfileData()
    {
        // Null checks for Inventory, inventoryPanel, avatar, FirebaseApp, GameManager
        if (Inventory.Instance == null)
        {
            Debug.LogWarning("Inventory.Instance is null in ProfileController.UpdateProfileData");
            return;
        }
        if (inventoryPanel == null)
        {
            Debug.LogWarning("inventoryPanel is null in ProfileController.UpdateProfileData");
            return;
        }
        if (avatar == null)
        {
            Debug.LogWarning("avatar is null in ProfileController.UpdateProfileData");
            return;
        }
        // update the inventory UI with the updated power-ups
        if (Inventory.Instance.PowerUpsObject != null)
            Inventory.Instance.SetAvailablePowerUps(Inventory.Instance.PowerUpsObject, inventoryPanel.transform, StatePowerUp.Profile);
        Inventory.Instance.OrderPowerUps(inventoryPanel.transform);

        // Update inventory UI
        Inventory.Instance.UpdateInventoryUI(inventoryPanel);

        // Update avatar information based on user type
        if (FirebaseApp.Instance != null && FirebaseApp.Instance.User != null)
        {
            if (!FirebaseApp.Instance.User.IsAnonymous)
            {
                if (GameManager.Instance != null && GameManager.Instance.UserData != null && GameManager.Instance.UserPhoto != null)
                {
                    object genderObj;
                    if (GameManager.Instance.UserData.TryGetValue("gender", out genderObj))
                    {
                        GenderUser genderUser;
                        if (Enum.TryParse(typeof(GenderUser), genderObj.ToString(), out object genderParsed))
                        {
                            genderUser = (GenderUser)genderParsed;
                            Sprite userPhoto = GameManager.Instance.UserPhoto;
                            avatar.UpdateAvatar(genderUser, userPhoto);
                        }
                        else
                        {
                            Debug.LogWarning("Failed to parse gender in ProfileController.UpdateProfileData");
                            avatar.UpdateAvatarAnonymous();
                        }
                    }
                    else
                    {
                        Debug.LogWarning("Gender not found in UserData in ProfileController.UpdateProfileData");
                        avatar.UpdateAvatarAnonymous();
                    }
                }
                else
                {
                    Debug.LogWarning("GameManager.Instance or UserData/UserPhoto is null in ProfileController.UpdateProfileData");
                    avatar.UpdateAvatarAnonymous();
                }
            }
            else avatar.UpdateAvatarAnonymous();
        }
        else
        {
            Debug.LogWarning("FirebaseApp.Instance or User is null in ProfileController.UpdateProfileData");
            avatar.UpdateAvatarAnonymous();
        }
    }
}