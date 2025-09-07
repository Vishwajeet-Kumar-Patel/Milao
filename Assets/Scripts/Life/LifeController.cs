#if USE_FIREBASE
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;
using UnityEngine.UI;
// ...existing code...
#else
using UnityEngine;
using TMPro;
using System;
using UnityEngine.UI;
public class LifeController : Timer
{
    public static LifeController Instance;
    public static Action OnInfiniteLife;

    private int lives = 5;
    private float lifeRecoveryTimer = 0f;
    private const float recoveryInterval = 1800f; // 30 minutes in seconds

    public int Lives { get { return lives; } }
    public int MaxLives { get { return 5; } }
    public TMP_Text WaitTime { get; set; }
    public bool HasLives => lives > 0;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Update()
    {
        if (lives < MaxLives)
        {
            lifeRecoveryTimer += Time.deltaTime;
            if (lifeRecoveryTimer >= recoveryInterval)
            {
                lives++;
                lifeRecoveryTimer = 0f;
                UpdateLivesUI();
            }
        }
    }

    public void ChangeLives(int amount, bool isBuy = false)
    {
        lives = Mathf.Clamp(lives + amount, 0, MaxLives);
        UpdateLivesUI();
    }

    public void UpdateLivesUI()
    {
        // Update UI here
        if (ProgressBar.Instance != null)
        {
            ProgressBar.Instance.SetLivesText(lives);
        }
    }

    public void SetTimer(float time, float timeInSecond = 0) {}
}
#endif