using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Type of power up that this object represents
public enum TypePowerUp
{
    [Description("Remove fruits in a radius of 0.6, a real explosion of fun!")]
    Bomb,
    [Description("Eliminate rows and columns of fruit, unleash the storm of destruction on the board!")]
    Lightning,
    [Description("Revolutionize fruits within a radius of 1, prepare the chaos of combinations!")]
    Potion
}

public enum StatePowerUp { Profile, LevelUI, Game }

// Timer that counts down the time for the power up
public class PowerUp : Timer
{
    // Public API
    public TypePowerUp TypePowerUp => typePowerUp;
    public StatePowerUp StatePowerUp { get => statePowerUp; set => statePowerUp = value; }
    public TMP_Text TextAmount { get => textAmount; set => textAmount = value; }
    public bool IsChecked { get => isChecked; set => isChecked = value; }
    public bool IsInTransitionDescription => isInTransitionDescription;
    public bool IsCooldown => isCooldown;
    public bool IsActive { get; set; }

    [Header("Config")]
    [SerializeField] private TypePowerUp typePowerUp;
    [SerializeField] private StatePowerUp statePowerUp = StatePowerUp.Profile;

    [Header("UI References")]
    [SerializeField] private TMP_Text textAmount;
    [SerializeField] private GameObject labelTimer;
    [SerializeField] private GameObject imageInfinite;
    [SerializeField] private GameObject imageCheck;
    [SerializeField] private GameObject imgLock;
    [SerializeField] private Image spritePowerUp;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private GameObject descriptionObject;
    [SerializeField] private Image imgCooldown;
    [SerializeField] private GameObject objectLock;
    [SerializeField] private int availableInLevel = 1;
    [SerializeField] private GameObject textInfoLevel;

    // Runtime flags/state
    private bool isChecked;
    private bool isInTransitionDescription;
    private bool isCooldown;

    // Components
    private AudioSource audioSource;
    private Animator animator;

    // Behaviour timing
    private float timeDescription = 3f;
    private readonly int timeCooldown = 20;
    private float currentTimeCooldown = 0f;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        animator = GetComponent<Animator>();
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        if (descriptionText != null)
            descriptionText.text = GetPowerUpDescription(typePowerUp);

        // Listen for level-ups to unlock when appropriate
        if (GameManager.Instance != null && GameManager.Instance.LevelUp != null)
            GameManager.Instance.LevelUp.AddListener(CheckUnlock);

        CheckUnlock();
        ResetCoolDownUI();
    }

    /// <summary>
    /// Saves power-up data to the inventory database.
    /// </summary>
    private void SavePowerDataBase()
    {
        // assumes Timer provides timeRemainingInSeconds
        var subData = new Dictionary<string, object> { { "time", timeRemainingInSeconds } };
        var data = new Dictionary<string, object> { { typePowerUp.ToString(), subData } };
        Inventory.Instance.SaveDataBase(data);
    }

    /// <summary>
    /// Make this power-up infinite for a duration (minutes). Updates UI state if needed.
    /// </summary>
    public void MakeInfinitePowerUp(float minutes, DateTime currentTime)
    {
        // assumes Timer provides timeRemainingInSeconds & previouslyAllottedTime
        timeRemainingInSeconds += minutes * 60f;
        previouslyAllottedTime = currentTime;

        if (!IsInfinite)
            ChangeStatePowerUp();
    }

    /// <summary>
    /// Toggle UI/flags when infinite state changes.
    /// </summary>
    private void ChangeStatePowerUp()
    {
        IsInfinite = !IsInfinite;                  // assumes Timer provides IsInfinite
        if (labelTimer) labelTimer.SetActive(!labelTimer.activeSelf);
        if (textAmount) textAmount.enabled = !textAmount.enabled;
        if (imageInfinite) imageInfinite.SetActive(!imageInfinite.activeSelf);
    }

    /// <summary>
    /// Toggle the "selected" checkmark in LevelUI state.
    /// </summary>
    private void ToggleChangeCheckPowerUp()
    {
        if (imageCheck) imageCheck.SetActive(!imageCheck.activeSelf);
        isChecked = !isChecked;
    }

    /// <summary>
    /// Directly show/hide the checkmark image.
    /// </summary>
    public void ChangeCheckPowerUpUI(bool stateToChange)
    {
        if (imageCheck) imageCheck.SetActive(stateToChange);
    }

    /// <summary>
    /// Button handler.
    /// </summary>
    public void OnClickPowerUp()
    {
        if (animator) animator.enabled = true;
        if (audioSource) audioSource.Play();

        switch (statePowerUp)
        {
            case StatePowerUp.Profile:
                if (isInTransitionDescription) return;

                isInTransitionDescription = true;
                if (descriptionObject) descriptionObject.SetActive(true);
                if (animator)
                {
                    animator.enabled = true;
                    animator.Play("DescriptionIn");
                }

                if (!IsActive)
                {
                    if (textInfoLevel) textInfoLevel.SetActive(true);
                    if (descriptionText) descriptionText.gameObject.SetActive(false);
                }
                else
                {
                    if (descriptionText) descriptionText.gameObject.SetActive(true);
                    if (textInfoLevel) textInfoLevel.SetActive(false);
                }

                StartCoroutine(TimeToDescriptionRutiner());
                break;

            case StatePowerUp.LevelUI:
                ToggleChangeCheckPowerUp();
                break;

            case StatePowerUp.Game:
                if (BoardManager.Instance.IsShifting) return;
                if ((Inventory.Instance.InventoryItems[typePowerUp] <= 0 && !IsInfinite) || isCooldown) return;

                var overlay = GameObject.FindFirstObjectByType<OverlayDisplayPowerUp>();
                var canvas = GetComponent<Canvas>();
                if (canvas && overlay) canvas.sortingOrder = overlay.OverlayEnable ? 1 : 5;
                if (overlay && spritePowerUp) overlay.SwitchState(spritePowerUp.sprite);

                GameManager.Instance.CurrentPowerUp = typePowerUp;
                GameManager.Instance.CurrentGameObjectPowerUp = gameObject;
                break;
        }
    }

    /// <summary>Disable this object's animator.</summary>
    public void DisableAnimator()
    {
        if (animator) animator.enabled = false;
    }

    /// <summary>Return the Description attribute text for a TypePowerUp.</summary>
    private string GetPowerUpDescription(TypePowerUp pType)
    {
        var fieldInfo = pType.GetType().GetField(pType.ToString());
        if (fieldInfo == null) return pType.ToString();

        var attributes = fieldInfo.GetCustomAttributes(typeof(DescriptionAttribute), false) as DescriptionAttribute[];
        if (attributes != null && attributes.Length > 0)
            return attributes[0].Description;

        return pType.ToString();
    }

    /// <summary>
    /// Waits a bit, plays exit animation, and hides description.
    /// </summary>
    private IEnumerator TimeToDescriptionRutiner()
    {
        yield return new WaitForSeconds(timeDescription);

        if (animator)
        {
            if (!animator.enabled) animator.enabled = true;
            animator.Play("DescriptionOut");
        }

        yield return new WaitForSeconds(0.6f);

        if (IsActive)
        {
            if (textInfoLevel) textInfoLevel.SetActive(false);
        }
        else
        {
            if (descriptionText) descriptionText.gameObject.SetActive(false);
        }

        DisableDescription();
    }

    /// <summary>Hide description panel and reset transition flag.</summary>
    public void DisableDescription()
    {
        isInTransitionDescription = false;
        if (descriptionObject) descriptionObject.SetActive(false);
        DisableAnimator();
    }

    /// <summary>
    /// Sets the timer to infinite until a given local time and compensates elapsed time.
    /// </summary>
    public void SetTimerInfinite(DateTime currentTime, DateTime time, int timeToInfinite)
    {
        // assumes Timer provides previouslyAllottedTime & timeRemainingInSeconds
        previouslyAllottedTime = time.ToLocalTime();

        float timeDiff = (float)(currentTime.Subtract(previouslyAllottedTime)).TotalSeconds;

        MakeInfinitePowerUp(timeToInfinite, currentTime);
        timeRemainingInSeconds -= timeDiff;
    }

    /// <summary>Begins cooldown visuals & state.</summary>
    public void StartCooldown()
    {
        isCooldown = true;
        if (imgCooldown)
        {
            imgCooldown.gameObject.SetActive(true);
            imgCooldown.fillAmount = 1f;
        }
        currentTimeCooldown = 0f;

        StartCoroutine(CooldownRutiner());
    }

    private IEnumerator CooldownRutiner()
    {
        while (currentTimeCooldown < timeCooldown)
        {
            currentTimeCooldown += Time.deltaTime;

            if (imgCooldown)
                imgCooldown.fillAmount = 1f - (currentTimeCooldown / timeCooldown);

            if (currentTimeCooldown >= timeCooldown)
            {
                isCooldown = false;
                if (imgCooldown) imgCooldown.gameObject.SetActive(false);
            }

            yield return null;
        }
    }

    /// <summary>Resets cooldown UI to hidden/idle.</summary>
    public void ResetCoolDownUI()
    {
        isCooldown = false;
        if (imgCooldown) imgCooldown.gameObject.SetActive(false);
    }

    /// <summary>Unlocks when level is high enough.</summary>
    private void CheckUnlock()
    {
        if (!IsActive && availableInLevel <= GameManager.Instance.Level + 1)
            Unlock();
    }

    /// <summary>Apply unlock visuals.</summary>
    private void Unlock()
    {
        IsActive = true;
        if (objectLock) objectLock.SetActive(false);
        if (imgLock) imgLock.SetActive(false);
        if (textAmount) textAmount.enabled = true;
    }
}
