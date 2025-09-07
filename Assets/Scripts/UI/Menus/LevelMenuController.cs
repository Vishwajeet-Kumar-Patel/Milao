using System;
using System.Collections;
using UnityEngine;

public class LevelMenuController : MonoBehaviour
{
    public static LevelMenuController Instance;

    public GameObject LifeShop { get { return lifeShop; } }
    public GameObject CoinShop { get { return coinShop; } }
    public GameObject BoxLevelUI { get { return boxLevelUI; } }

    [SerializeField] GameObject overlay;
    [SerializeField] GameObject lifeShop;
    [SerializeField] GameObject coinShop;
    [SerializeField] GameObject boxLevelUI;
    [SerializeField] Reward rewardPopup; // Reference to the reward popup

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        // Automatically show reward popup when Level Menu loads
        // You can comment this out if you don't want it to auto-show
        ShowDailyReward();
    }

    /// <summary>
    /// Shows the daily reward popup
    /// </summary>
    public void ShowDailyReward()
    {
        if (rewardPopup != null)
        {
            rewardPopup.ShowReward();
        }
        else
        {
            Debug.LogWarning("LevelMenuController: rewardPopup not assigned!");
        }
    }

    /// <summary>
    /// Button method to manually trigger reward popup
    /// </summary>
    public void OnRewardButtonClicked()
    {
        ShowDailyReward();
    }

    public void OnScreen(GameObject screen)
    {
        overlay.SetActive(true);
        screen.SetActive(true);
    }

    public void OffScreen(GameObject screen)
    {
        screen.GetComponent<Animator>().SetTrigger("Transition");
        StartCoroutine(OffScreenRutiner(screen));
    }

    /// <summary>
    /// Moves the given screen off-screen with a callback action.
    /// </summary>
    public void OffScreen(GameObject screen, Action callback)
    {
        OffScreen(screen);
        StartCoroutine(OffScreenRutinerCallback(callback));
    }

    /// <summary>
    /// Coroutine to delay a callback action.
    /// </summary>
    IEnumerator OffScreenRutinerCallback(Action callback)
    {
        yield return new WaitForSeconds(1);
        callback.Invoke();
    }

    IEnumerator OffScreenRutiner(GameObject screen)
    {
        yield return new WaitForSeconds(1);
        overlay.SetActive(false);
        screen.SetActive(false);
    }

    public void OnScreenAdvance(GameObject screen)
    {
        screen.SetActive(true);
    }
}