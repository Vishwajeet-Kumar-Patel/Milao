using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;

public class GUIManager : MonoBehaviour
{
    public static GUIManager Instance;

    public GamePlayMode GamePlayMode { get { return gamePlayMode; } set { gamePlayMode = value; } }
    public float CurrentTime { get { return currentTime; } set { currentTime = value; } }
    public TimerGame TimerGame { get { return bannerTime.GetComponent<TimerGame>(); } }

    public int Score
    {
        get { return score; }
        set
        {
            score = value;
            
            // Stop existing score update coroutine to prevent overlaps
            if (updateScoreCoroutine != null)
                StopCoroutine(updateScoreCoroutine);
            updateScoreCoroutine = StartCoroutine(UpdateScore());
            
            ProgressBar.Instance.ChangeBarScore(score);
            if (GameManager.Instance.GameMode == GameMode.ScoringObjective)
            {
                StopCoroutine(characterBatUI.RemainingScore());
                StartCoroutine(characterBatUI.RemainingScore());
            }
        }
    }

    public int MoveCounter
    {
        get { return moveCounter; }
        set
        {
            moveCounter = value;
            movesText.text = moveCounter.ToString();
            if (moveCounter <= 0)
            {
                moveCounter = 0;
                StartCoroutine(CheckGameStatus());
            }
        }
    }

    public int MultiplicationFactor { set { multiplicationFactorText.text = value.ToString(); } }

    [SerializeField] GamePlayMode gamePlayMode;

    [SerializeField] GameObject bannerMove;
    [SerializeField] GameObject bannerTime;

    [SerializeField] TMP_Text movesText, scoreText, multiplicationFactorText;
    [SerializeField] GameObject imageInfiniteMoves;
    [SerializeField] TimerGame timerGame;

    [Header("Screens")]
    [SerializeField] GameOverController menuGameOver;
    [SerializeField] CompleteGameController menuCompleteGame;
    [Header("UI")]
    // Serialized time bar UI element
    [SerializeField] GameObject timeBarUI;
    [SerializeField] GameObject multiplicationFactor;
    [SerializeField] CharacterBatUI characterBatUI;

    int moveCounter, score;
    float timeToMatch, currentTime;
    float lerpDurationScore = 1;

    // Dictionary to map game modes to corresponding objective setting methods.
    Dictionary<GameMode, Action> gameModeHandlers;
    
    // Coroutine management to prevent overlaps
    Coroutine timeToMatchCoroutine;
    Coroutine updateScoreCoroutine;

    public bool AlreadyLoseGame { get; set; }

    void OnEnable()
    {
        if (GameManager.Instance != null && GameManager.Instance.OnGameMode != null)
            GameManager.Instance.OnGameMode.AddListener(OnGameMode);
        else
            Debug.LogWarning("GUIManager.OnEnable: GameManager.Instance or OnGameMode is null");
    }

    void OnDisable()
    {
        if (GameManager.Instance != null && GameManager.Instance.OnGameMode != null)
            GameManager.Instance.OnGameMode.RemoveListener(OnGameMode);
            
        // Stop all managed coroutines to prevent overlaps
        if (timeToMatchCoroutine != null)
        {
            StopCoroutine(timeToMatchCoroutine);
            timeToMatchCoroutine = null;
        }
        
        if (updateScoreCoroutine != null)
        {
            StopCoroutine(updateScoreCoroutine);
            updateScoreCoroutine = null;
        }
    }

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(this.gameObject);

        moveCounter = GameManager.Instance.MoveCounter;
        timeToMatch = GameManager.Instance.TimeToMatch;

        gameModeHandlers = new Dictionary<GameMode, Action>()
        {
            { GameMode.FeedingObjective, SetFeedingObjective },
            { GameMode.ScoringObjective, SetScoringObjective },
            { GameMode.TimeObjective, SetTimeObjective },
            { GameMode.CollectionObjective, SetCollectionObjective }
        };

        AlreadyLoseGame = false;
    }

    void Start()
    {
        scoreText.text = score.ToString();
        movesText.text = moveCounter.ToString();
    }

    void OnGameMode(GameMode gameMode)
    {
        if (gameModeHandlers.ContainsKey(gameMode)) gameModeHandlers[gameMode]();
    }

    /// <summary>
    /// Sets the feeding objective for the game mode.
    /// </summary>
    void SetFeedingObjective()
    {
        // Ensure clean UI state for moves-limited mode
        bannerTime.SetActive(false);
        timeBarUI.SetActive(false);
        imageInfiniteMoves.SetActive(false);
        
        // Enable move-related UI
        bannerMove.SetActive(true);
        movesText.enabled = true;
        gamePlayMode = GamePlayMode.MovesLimited;
    }

    /// <summary>
    /// Sets the scoring objective for the game mode.
    /// </summary>
    void SetScoringObjective()
    {
        // Ensure clean UI state for moves-limited mode
        bannerTime.SetActive(false);
        timeBarUI.SetActive(false);
        imageInfiniteMoves.SetActive(false);
        
        // Enable move-related UI
        bannerMove.SetActive(true);
        movesText.enabled = true;
        gamePlayMode = GamePlayMode.MovesLimited;
        multiplicationFactor.SetActive(true);
    }

    /// <summary>
    /// Sets the game's play mode to Timed Match and activates the necessary UI elements.
    /// </summary>
    void SetTimeObjective()
    {
        gamePlayMode = GamePlayMode.TimedMatch;
        
        // Hide move-related UI elements
        bannerMove.SetActive(false);
        movesText.enabled = false;
        imageInfiniteMoves.SetActive(false);
        
        // Show time-related UI elements
        bannerTime.SetActive(true);
        timeBarUI.SetActive(true);
    }

    void SetCollectionObjective()
    {
        // Ensure clean UI state for moves-limited mode
        bannerTime.SetActive(false);
        timeBarUI.SetActive(false);
        imageInfiniteMoves.SetActive(false);
        
        // Enable move-related UI
        bannerMove.SetActive(true);
        movesText.enabled = true;
        gamePlayMode = GamePlayMode.MovesLimited;
    }

    /// <summary>
    /// Helper method to ensure clean UI state transitions
    /// </summary>
    void ResetAllUIElements()
    {
        // Hide all UI elements first
        bannerTime.SetActive(false);
        bannerMove.SetActive(false);
        timeBarUI.SetActive(false);
        imageInfiniteMoves.SetActive(false);
        movesText.enabled = false;
        multiplicationFactor.SetActive(false);
    }

    /// <summary>
    /// Updates the game play mode and UI based on the randomly generated game play mode.
    /// </summary>
    public void UpdateStateGamePlayMode()
    {
        bannerMove.SetActive(true);

        gamePlayMode = GameManager.Instance.GetRandomGamePlayMode();

        if (gamePlayMode == GamePlayMode.TimedMatch)
        {
            timeBarUI.SetActive(true);
            imageInfiniteMoves.SetActive(true);
            movesText.enabled = false;
        }
        else if (gamePlayMode == GamePlayMode.MovesLimited)
        {
            MovesLimited();
        }
    }

    /// <summary>
    /// Disables unnecessary UI elements and enables the move-related UI elements.
    /// </summary>
    void MovesLimited()
    {
        bannerTime.SetActive(false);
        timeBarUI.SetActive(false);
        imageInfiniteMoves.SetActive(false);
        bannerMove.SetActive(true);
        movesText.enabled = true;
    }

    /// <summary>
    /// Wait until the board has finished changing before displaying the finished game or game completed menu.
    /// </summary>
    public IEnumerator CheckGameStatus()
    {
        yield return new WaitForEndOfFrame();
        yield return new WaitUntil(() => !BoardManager.Instance.IsShifting);
        yield return new WaitForSeconds(0.3f);

        if (ProgressBar.Instance.GetActiveStars() >= 1 && GameManager.Instance.ObjectiveComplete)
            menuCompleteGame.OnCompleteGame();
        else menuGameOver.OnGameOver();
    }

    /// <summary>
    /// Updates the score display with a smooth animation.
    /// </summary>
    IEnumerator UpdateScore()
    {
        if (gamePlayMode == GamePlayMode.TimedMatch) currentTime = 0;

        int scoreDisplay = Convert.ToInt32(scoreText.text);

        float elapsedTime = 0;

        while (scoreDisplay < score)
        {
            scoreDisplay = Mathf.RoundToInt(Mathf.Lerp(scoreDisplay, score, elapsedTime / lerpDurationScore));
            scoreText.text = scoreDisplay.ToString();

            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        // Clear coroutine reference when done
        updateScoreCoroutine = null;
        yield return null;
    }

    /// <summary>
    /// Coroutine to update the time to match UI.
    /// </summary>
    public IEnumerator TimeToMatchCoroutine()
    {
        float factor;

        while (currentTime < timeToMatch && !AlreadyLoseGame)
        {
            currentTime += Time.deltaTime;
            factor = Mathf.Clamp(currentTime / timeToMatch, 0, 1);
            UITimeBar.Instance.ChangeTimeBar(factor);

            if (currentTime > timeToMatch)
            {
                yield return new WaitUntil(() => !BoardManager.Instance.IsShifting);

                if (currentTime >= timeToMatch)
                {
                    AlreadyLoseGame = true;
                    StartCoroutine(CheckGameStatus());
                }
            }

            yield return null;
        }

        if (!AlreadyLoseGame && currentTime > timeToMatch)
        {
            yield return new WaitUntil(() => !BoardManager.Instance.IsShifting);

            if (currentTime >= timeToMatch) StartCoroutine(CheckGameStatus());
        }

        // Clear coroutine reference when done
        timeToMatchCoroutine = null;
        yield return null;
    }
    
    /// <summary>
    /// Safely start TimeToMatchCoroutine, stopping any existing instance.
    /// </summary>
    public void StartTimeToMatchCoroutine()
    {
        if (timeToMatchCoroutine != null)
        {
            StopCoroutine(timeToMatchCoroutine);
        }
        timeToMatchCoroutine = StartCoroutine(TimeToMatchCoroutine());
    }

    /// <summary>
    /// Changes the 'timeToMatch' value, clamping it between 3 and the GameManager's 'TimeToMatch' property.
    /// </summary>
    /// <param name="amount">The amount by which to adjust the 'timeToMatch' value.</param>
    public void ChangeTimeToMatch(float amount)
    {
        // Calculate the new 'timeToMatch' value and clamp it to the desired range.
        timeToMatch = Math.Clamp(timeToMatch += amount, 3, GameManager.Instance.TimeToMatch);

        // Check if 'timeToMatch' is equal to the GameManager's 'TimeToMatch' value and update 'IsTimeToMatchPenalty' accordingly.
        if (timeToMatch == GameManager.Instance.TimeToMatch)
            GameManager.Instance.IsTimeToMatchPenalty = false;
    }

    /// <summary>
    /// Checks whether the game objective is complete and whether the player earned at least three stars, then starts the CheckGameStatus coroutine.
    /// </summary>
    public void CompleteTimeToMatchObjective()
    {
        // Check whether the game objective is complete, the player earned at least three stars, and the game mode is TimedMatch.
        if (GameManager.Instance.ObjectiveComplete && ProgressBar.Instance.GetActiveStars() >= 3)
            StartCoroutine(CheckGameStatus());
    }

    public void ContinueGameReward()
    {
        GameOverController.Instance.HideScreen();
        StartCoroutine(ContinueGameRewardRutiner());
    }

    IEnumerator ContinueGameRewardRutiner()
    {
        yield return new WaitForSeconds(1.1f);

        if (gamePlayMode == GamePlayMode.MovesLimited)
        {
            moveCounter += 3;
            movesText.text = moveCounter.ToString();
        }
        else
        {
            if (GameManager.Instance.GameMode == GameMode.TimeObjective)
            {
                // Use the new safe restart method to prevent timer overlaps
                float newTimeRemaining = timerGame.TimeRemaining + GameManager.Instance.TimeToMatch;
                timerGame.RestartTimerWithTime(newTimeRemaining);
            }

            currentTime = 0;
            StartTimeToMatchCoroutine();
        }
    }
}