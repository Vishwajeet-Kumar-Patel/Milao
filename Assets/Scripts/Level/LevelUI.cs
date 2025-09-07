using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;

public class LevelUI : MonoBehaviour
{
    [SerializeField] GameObject container;
    [SerializeField] GameObject overlay;

    [SerializeField] TMP_Text nameLevelText;
    [SerializeField] Image[] stars;
    [SerializeField] TMP_Text goal;
    [SerializeField] GameObject powerUpPanel;

    [SerializeField] GameObject boxSelectPower;
    [SerializeField] TMP_Text hightScoreText;

    GameMode gameMode;

    // Method to activate the level UI by setting the 'container' and 'overlay' game objects to active
    public void ActiveLevelUI()
    {
        container.SetActive(true);
        overlay.SetActive(true);
    }

    /// <summary>
    /// Sets the name, number of stars, and goal of the level in the UI.
    /// </summary>
    /// <param name="nameText">The level name.</param>
    /// <param name="amountStars">The number of stars collected.</param>
    /// <param name="goalText">The goal text.</param>
    /// <param name="gameMode">The game mode.</param>
    public void SetValueLevel(string nameText, int amountStars, int amountScore, string goalText, GameMode gameMode)
    {
        if (nameLevelText != null)
            nameLevelText.text = nameText;

        if (stars != null)
        {
            for (int i = 0; i < amountStars && i < stars.Length; i++)
            {
                if (stars[i] != null)
                    stars[i].enabled = true;
            }
        }

        if (goal != null)
            goal.text = goalText;

        if (amountScore != 0 && hightScoreText != null)
            hightScoreText.text = amountScore.ToString();

        this.gameMode = gameMode;

        if (Inventory.Instance != null)
        {
            List<GameObject> powerUps = Inventory.Instance.GetAvailablePowerUps();
            if (powerUps != null && powerUps.Count > 0 && boxSelectPower != null && powerUpPanel != null)
            {
                Inventory.Instance.SetAvailablePowerUps(powerUps, boxSelectPower.transform, StatePowerUp.LevelUI);
                Inventory.Instance.UpdateInventoryUI(powerUpPanel);
            }
        }

    }

    /// <summary>
    /// Starts playing the specified level.
    /// </summary>
    /// <param name="nameScene">The name of the scene to load.</param>
    public void PlayLevel(string nameScene)
    {
        if (LifeController.Instance != null && (LifeController.Instance.HasLives || LifeController.Instance.IsInfinite))
        {
            if (GetAdd() && AdsManager.Instance != null)
                AdsManager.Instance.ShowAndLoadInterstitialAd();

            if (GameManager.Instance != null)
            {
                GameManager.Instance.GameMode = gameMode;
                GameManager.Instance.ObjectiveComplete = false;
                if (nameLevelText != null && !string.IsNullOrEmpty(nameLevelText.text))
                    GameManager.Instance.CurrentLevel = Convert.ToInt32(nameLevelText.text.Split(' ')[1]) - 1;

                if (GameManager.Instance.Difficulty == 0)
                    GameManager.Instance.GetDifficulty();

                DifficultManager.SetInitialStateData(DifficultManager.GetLevelData());
                GameManager.Instance.OnDifficult.Invoke(GameManager.Instance.GameMode);
            }

            if (ScreenChangeTransition.Instance != null)
                StartCoroutine(ScreenChangeTransition.Instance.FadeOut(nameScene));

            if (Inventory.Instance != null)
                StartCoroutine(ResetParentPowerUps());
        }
        else
        {
            if (LevelMenuController.Instance != null)
            {
                LevelMenuController.Instance.OffScreen(LevelMenuController.Instance.BoxLevelUI, () =>
                {
                    LevelMenuController.Instance.OnScreen(LevelMenuController.Instance.LifeShop);
                });
            }
        }
    }

    bool GetAdd() => GameManager.Instance.Level % 3 == 0;

    /// <summary>
    /// Coroutine to delay resetting the parent of power-ups.
    /// </summary>
    /// <returns>An IEnumerator for use in StartCoroutine.</returns>
    IEnumerator ResetParentPowerUps()
    {
        yield return new WaitForSecondsRealtime(.8f);
        Inventory.Instance.ResetParentPowerUps(false);
    }
    /// <summary>
    /// Resets the information of a level by disabling stars.
    /// </summary>
    public void ResetInformationLevel()
    {
        for (int i = 0; i < 3; i++)
        {
            stars[i].enabled = false;
        }

        hightScoreText.text = "";
    }
}