using UnityEngine;
using TMPro;
using DG.Tweening;

public class Reward : MonoBehaviour
{
    [SerializeField] TMP_Text textHeadMessage;
    [SerializeField] AudioClip audioPop;
    [SerializeField] AudioClip audioGetCoins;
    [SerializeField] GameObject coinObject;
    [SerializeField] GameObject rewardCoinObject;
    [SerializeField] GameObject overlay; // Background overlay
    [SerializeField] GameObject rewardPanel; // Main reward panel

    float time = .6f;
    int rewardCoins = 100;
    string nameUser;
    AudioSource audioSource;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        
        // Ensure the reward popup is hidden initially
        if (gameObject.activeSelf)
            gameObject.SetActive(false);
    }

    void Start()
    {
        // Set up user name for personalized greeting
        if (GameManager.Instance != null && GameManager.Instance.UserData != null && GameManager.Instance.UserData.ContainsKey("name"))
        {
            nameUser = (string)GameManager.Instance.UserData["name"];
            textHeadMessage.text = string.Format($"Hello, {nameUser.Split(' ')[0]}!");
        }
        else
        {
            nameUser = "Player";
            textHeadMessage.text = "Hello!";
        }
    }

    /// <summary>
    /// Shows the reward popup on screen
    /// </summary>
    public void ShowReward()
    {
        // Activate the reward popup
        gameObject.SetActive(true);
        
        // Show overlay if available
        if (overlay != null)
            overlay.SetActive(true);
            
        // Animate the reward panel appearing
        if (rewardPanel != null)
        {
            rewardPanel.transform.localScale = Vector3.zero;
            rewardPanel.transform.DOScale(Vector3.one, 0.5f).SetEase(Ease.OutBack);
        }
        
        // Play popup sound
        if (audioSource != null && audioPop != null)
            audioSource.PlayOneShot(audioPop);
    }

    /// <summary>
    /// Legacy method for backward compatibility
    /// </summary>
    public void ActivateReward()
    {
        ShowReward();
    }

    /// <summary>
    /// Handles claiming the reward and closing the popup
    /// </summary>
    public void ClaimReward()
    {
        if (rewardCoinObject != null && coinObject != null && audioSource != null && audioPop != null && audioGetCoins != null)
        {
            // Show coin animation
            rewardCoinObject.SetActive(true);
            audioSource.PlayOneShot(audioPop);
            
            // Animate coin moving to coin counter
            rewardCoinObject.transform.DOMove(coinObject.transform.position, time).OnComplete(() =>
            {
                audioSource.PlayOneShot(audioGetCoins);

                var canvasGroup = rewardCoinObject.GetComponent<CanvasGroup>();
                if (canvasGroup != null)
                {
                    canvasGroup.DOFade(0, time).OnComplete(() =>
                    {
                        // Add coins to player's account
                        if (CoinController.Instance != null)
                            CoinController.Instance.ChangeCoins(rewardCoins);
                        
                        // Close the reward popup
                        CloseReward();
                    });
                }
                else
                {
                    // Fallback if no CanvasGroup
                    if (CoinController.Instance != null)
                        CoinController.Instance.ChangeCoins(rewardCoins);
                    CloseReward();
                }
            });
        }
        else
        {
            // Fallback if components are missing
            if (CoinController.Instance != null)
                CoinController.Instance.ChangeCoins(rewardCoins);
            CloseReward();
        }
    }

    /// <summary>
    /// Closes the reward popup with animation
    /// </summary>
    public void CloseReward()
    {
        // Animate the panel closing
        if (rewardPanel != null)
        {
            rewardPanel.transform.DOScale(Vector3.zero, 0.3f).SetEase(Ease.InBack).OnComplete(() =>
            {
                // Hide overlay
                if (overlay != null)
                    overlay.SetActive(false);
                    
                // Deactivate the entire popup
                gameObject.SetActive(false);
            });
        }
        else
        {
            // Fallback without animation
            if (overlay != null)
                overlay.SetActive(false);
            gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Allows closing the popup without claiming (e.g., close button)
    /// </summary>
    public void DismissReward()
    {
        CloseReward();
    }
}
