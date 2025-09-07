using System.Collections;
using UnityEngine;
using TMPro;

public class TimerGame : MonoBehaviour
{
    public float TimeRemaining { get { return timeRemaining; } set { timeRemaining = value; } }
    public bool RestartTimer { get { return restartTimer; } set { restartTimer = value; } }

    [SerializeField] TMP_Text timerText;

    float timeRemaining;

    int minutes;
    int seconds;

    bool restartTimer = true;
    Coroutine updateTimerCoroutine;

    /// <summary>
    /// Starts the timer coroutine.
    /// </summary>
    public void StartTimer()
    {
        // Stop any existing timer coroutine to prevent overlaps
        StopTimer();
        
        timeRemaining = GameManager.Instance.TotalSeconds;
        restartTimer = true;
        updateTimerCoroutine = StartCoroutine(UpdateTimer());
    }

    /// <summary>
    /// Coroutine for updating the timer UI.
    /// </summary>
    /// <returns>An IEnumerator for coroutine execution.</returns>
    public IEnumerator UpdateTimer()
    {
        while (timeRemaining > 0 && restartTimer)
        {
            timeRemaining -= Time.deltaTime;

            minutes = Mathf.FloorToInt(timeRemaining / 60);
            seconds = Mathf.FloorToInt(timeRemaining % 60);

            if (minutes > 0) timerText.text = string.Format("{0}:{1:00}", minutes, seconds);
            else timerText.text = string.Format("{0:00}", seconds);

            yield return null;
        }

        if (restartTimer)
        {
            timerText.text = "00";

            yield return null;

            StartCoroutine(GUIManager.Instance.CheckGameStatus());
        }
    }

    public void StopTimer()
    {
        restartTimer = false;
        
        // Stop the specific timer coroutine if it exists
        if (updateTimerCoroutine != null)
        {
            StopCoroutine(updateTimerCoroutine);
            updateTimerCoroutine = null;
        }
    }
    
    /// <summary>
    /// Safely restart the timer with new time remaining.
    /// </summary>
    public void RestartTimerWithTime(float newTimeRemaining)
    {
        StopTimer();
        timeRemaining = newTimeRemaining;
        restartTimer = true;
        updateTimerCoroutine = StartCoroutine(UpdateTimer());
    }
    
    void OnDisable()
    {
        // Ensure timer is stopped when object is disabled
        StopTimer();
    }
}