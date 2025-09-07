using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Events;
using System.ComponentModel;
using System.Net.NetworkInformation;
using UnityEngine.Networking;
using System.Threading.Tasks;
using GoogleMobileAds.Api;

// Define a set of game modes for different game objectives
public enum GameMode
{
    [Description("Feed the bat!")]
    FeedingObjective,
    [Description("Beat the score!")]
    ScoringObjective,
    [Description("Pick the fruits before the time runs out!")]
    TimeObjective,
    [Description("Collect all orders!")]
    CollectionObjective
}

public enum GamePlayMode { MovesLimited, TimedMatch }
public enum TypeRewardedAd { Life, Coin }

/// <summary>
/// Enum representing different error types.
/// </summary>
public enum Errors
{
    [Description("We're sorry, but you don't seem to have an internet connection right now. To access the online features, please check your Wi-Fi or mobile data connection and try again. Thank you!. If the problem persists, contact the game developer")]
    NNA_GM_THIS,
    [Description("Something went wrong with the database dependencies. Please try again. If the problem persists, contact the game developer")]
    F_FA_68,
    [Description("Something went wrong with authentication. If the problem persists, contact the game developer.")]
    AUGGC_FA_122,
    [Description("Something went wrong with authentication. If the problem persists, contact the game developer.")]
    AUGGF_FA_127,
    [Description("Something went wrong when downloading the avatar image. If the problem persists, contact the game developer.")]
    UP_FA_199,
    [Description("Something went wrong with authentication. If the problem persists, contact the game developer.")]
    AUGG_GA_71,
    [Description("Something went wrong while synchronizing with the database. If the problem persists, contact the game developer.")]
    CNU_CF_80,
    [Description("Something went wrong while synchronizing with the database. If the problem persists, contact the game developer.")]
    GUD_CF_118,
    [Description("Something went wrong while synchronizing with the database. If the problem persists, contact the game developer.")]
    GUL_CF_153,
    [Description("Something went wrong while synchronizing with the database. If the problem persists, contact the game developer.")]
    SUL_CF_188,
    [Description("Something went wrong while synchronizing with the database. If the problem persists, contact the game developer.")]
    GUC_CF_211,
    [Description("Something went wrong while synchronizing with the database. If the problem persists, contact the game developer.")]
    SUC_CF_223,
}

public enum GameState { LevelMenu, InGame }

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;  // Static reference to the GameManager instance

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Debug.Log($"GameManager Awake: AvailableFruits count = {(AvailableFruits != null ? AvailableFruits.Count : 0)}");
    }

    public GameState currentGameState;

    /// Observer Pattern
    [HideInInspector] public UnityEvent<GameMode> OnGameMode;
    [HideInInspector] public UnityEvent OnUniqueMatches;
    [HideInInspector] public UnityEvent<Errors> OnErrorRetry;
    [HideInInspector] public UnityEvent<Errors> OnErrorClose;
    [HideInInspector] public UnityEvent LevelUp;
    [HideInInspector] public UnityEvent<GameMode> OnDifficult;

    Dictionary<Errors, Action> errorsRetryHandler;
    Dictionary<Errors, Action> errorsCloseHandler;

    public int Level { get { return level; } set { level = value; } }  // Public getter for the current level
    // Gets or sets the objective game mode.
    public GameMode GameMode { get { return gameMode; } set { gameMode = value; } }
    // Gets the list of available fruits.
    public List<GameObject> AvailableFruits { get { return availableFruits; } set { availableFruits = value; } }
    public List<GameObject> UpcomingFruits { get { return upcomingFruits; } set { upcomingFruits = value; } }

    // Gets the maximum feeding objective.
    public int MaxFeedingObjective { get { return maxFeedingObjective; } set { maxFeedingObjective = value; } }
    // Gets the maximum score objective.
    public int MaxScoreObjective { get { return maxScoreObjective; } set { maxScoreObjective = value; } }
    public float ProbabilityMultiplicationFactor { get { return probabilityMultiplicationFactor; } set { probabilityMultiplicationFactor = value; } }

    // Gets the current move counter.
    public int MoveCounter { get { return moveCounter; } set { moveCounter = value; } }
    // Gets the time to match.
    public float TimeToMatch { get { return timeToMatch; } set { timeToMatch = value; } }
    // Gets the minimum allowed 'timeToMatch'.
    public float MinTimeToMatch { get { return minTimeToMatch; } }
    // Gets the 'timeToMatchPenalty' value.
    public float TimeToMatchPenalty { get { return timeToMatchPenalty; } set { timeToMatchPenalty = value; } }
    // Gets or sets the 'timeToMatchPenaltyTimes' value.
    public float TimeToMatchPenaltyTimes { get { return timeToMatchPenaltyTimes; } set { timeToMatchPenaltyTimes = value; } }
    public int ScoreBar { get { return scoreBar; } set { scoreBar = value; } }
    // Gets the maximum allowed 'maxTimeToMatchPenaltyTimes'.
    public float MaxTimeToMatchPenaltyTimes { get { return maxTimeToMatchPenaltyTimes; } }
    // Gets or sets a value indicating whether there is a time-to-match penalty.
    public bool IsTimeToMatchPenalty { get { return isTimeToMatchPenalty; } set { isTimeToMatchPenalty = value; } }

    // Gets or sets whether the game objective is complete.
    public bool ObjectiveComplete { get { return objectiveComplete; } set { objectiveComplete = value; } }

    // Gets the total remaining seconds for the timer.
    public float TotalSeconds { get { return totalSeconds; } set { totalSeconds = value; } }
    // Gets the match objective amount.
    public int MatchObjectiveAmount { get { return matchObjectiveAmount; } set { matchObjectiveAmount = value; } }
    public int FruitCollectionAmount { get { return fruitCollectionAmount; } set { fruitCollectionAmount = value; } }
    public float FruitCollectionProbability { get { return fruitCollectionProbability; } set { fruitCollectionProbability = value; } }

    // Gets or sets a value indicating whether unique matches are required.
    public bool UniqueMatches { get { return uniqueMatches; } set { uniqueMatches = value; } }

    public Dictionary<string, object> UserData { get { return userData; } set { userData = value; } }
    public Sprite UserPhoto { get { return userPhoto; } set { userPhoto = value; } }

    public List<Dictionary<string, object>> LevelsData { get { return levelsData; } set { levelsData = value; } }
    public Dictionary<string, object> CollectiblesData { get { return collectiblesData; } set { collectiblesData = value; } }

    public int CurrentLevel { get { return currentLevel; } set { currentLevel = value; } }  // Public getter for the current level
    public int Difficulty { get { return difficulty; } }  // Public getter for the current level
    public DifficultData DifficultData { get { return difficultData; } }  // Public getter for the current level
    public bool PowerUpActivate { get { return powerUpActivate; } set { powerUpActivate = value; } }
    public TypePowerUp CurrentPowerUp { get { return currentPowerUp; } set { currentPowerUp = value; } }
    public GameObject CurrentGameObjectPowerUp { get { return currentGameObjectPowerUp; } set { currentGameObjectPowerUp = value; } }

    public bool CheckUnlockFruitsToCreateLevels { get { return checkUnlockFruitsToCreateLevels; } set { checkUnlockFruitsToCreateLevels = value; } }

    public bool UserAlready;
    public bool UserIsAnonymous;
    public bool UserIsLinker;

    // Serialized game mode field
    [SerializeField] GameMode gameMode;

    // Private field for the current level
    [SerializeField] int level = 0;

    // List of available fruits
    [SerializeField] List<GameObject> availableFruits;
    // List of upcoming fruits
    [SerializeField] List<GameObject> upcomingFruits;

    [Header("Game Mode")]
    // Move counter for the game mode
    [SerializeField] int moveCounter;
    // Time to match for the game mode
    [SerializeField] float timeToMatch;
    [SerializeField] float minTimeToMatch;
    [SerializeField] float timeToMatchPenalty;
    [SerializeField] int scoreBar;

    [Header("Feeding Objective")]
    // Maximum feeding objective
    [SerializeField] int maxFeedingObjective;

    [Header("Scoring Objective")]
    // Maximum score objective
    [SerializeField] int maxScoreObjective;
    [SerializeField] float probabilityMultiplicationFactor;

    [Header("Time Objective")]
    [SerializeField] float totalSeconds;
    [SerializeField] int matchObjectiveAmount;

    [Header("Collection Objective")]
    [SerializeField] int fruitCollectionAmount;
    [SerializeField, Range(0f, 1f)] float fruitCollectionProbability;

    [Space(10)]
    [SerializeField] int rewardLevelPass;

    Dictionary<GameMode, Action> onDifficultHandler;
    [SerializeField] DifficultData difficultData;
    const string KEY_DIFFICULTY = "difficulty";
    const string KEY_CONSECUTIVE_VICTORY = "victory";
    const string KEY_CONSECUTIVE_DEFEAT = "defeat";
    // Current game difficulty level
    int difficulty;
    // Maximum consecutive wins/losses before difficulty adjustment
    const int MAX_CONSECUTIVE_GAME = 1;
    // Number of consecutive victories
    int consecutiveVictory;
    // Number of consecutive defeats
    int consecutiveDefeat;

    // Indicates whether the game objective is complete
    bool objectiveComplete;

    bool uniqueMatches;

    // Dictionary containing user data.
    Dictionary<string, object> userData;
    // Sprite representing the user's photo.
    Sprite userPhoto;

    List<Dictionary<string, object>> levelsData = new List<Dictionary<string, object>>();
    Dictionary<string, object> collectiblesData;

    ErrorHandler errorHandler;

    int currentLevel;

    bool powerUpActivate;
    TypePowerUp currentPowerUp;
    GameObject currentGameObjectPowerUp;

    // A flag to determine whether to check and unlock fruits for creating levels.
    bool checkUnlockFruitsToCreateLevels = true;

    float timeToMatchPenaltyTimes = -1;
    float maxTimeToMatchPenaltyTimes = 3;
    bool isTimeToMatchPenalty;

    /// <summary>
    /// Subscribes to the SceneManager's sceneLoaded event when the script is enabled.
    /// </summary>
    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        OnErrorRetry.AddListener(ErrorRetry);
        OnErrorClose.AddListener(ErrorClose);
        OnDifficult.AddListener(Difficult);
    }

    /// <summary>
    /// Unsubscribes from the SceneManager's sceneLoaded event when the script is disabled.
    /// </summary>
    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        OnErrorRetry.RemoveListener(ErrorRetry);
        OnErrorClose.RemoveListener(ErrorClose);
        OnDifficult.RemoveListener(Difficult);
    }

    

    /// <summary>
    /// Clears all user-related data and resets the user's state to default values.
    /// </summary>
    public void ClearAllDataUser()
    {
        // Clear user data containers.
        userData.Clear();
        levelsData.Clear();
        collectiblesData.Clear();
        // Reset user-related variables.
        UserAlready = false;
        UserIsAnonymous = false;
        UserIsLinker = false;
        level = 0;
        difficulty = 1;
        consecutiveVictory = 0;
        consecutiveDefeat = 0;
        // Save the difficulty level.
        SaveDifficulty();
        // Reset objective completion status and user photo.
        objectiveComplete = false;
        userPhoto = null;
    }

    /// <summary>
    /// Save the current difficulty level to PlayerPrefs.
    /// </summary>
    void SaveDifficulty()
    {
        PlayerPrefs.SetInt(KEY_DIFFICULTY, difficulty);
        PlayerPrefs.SetInt(KEY_CONSECUTIVE_VICTORY, consecutiveVictory);
        PlayerPrefs.SetInt(KEY_CONSECUTIVE_DEFEAT, consecutiveDefeat);
    }

    /// <summary>
    /// Get the current difficulty level. If not found in PlayerPrefs, return a default value.
    /// </summary>
    /// <returns>The current difficulty level.</returns>
    public void GetDifficulty()
    {
        if (PlayerPrefs.HasKey(KEY_DIFFICULTY))
        {
            difficulty = PlayerPrefs.GetInt(KEY_DIFFICULTY);
            consecutiveVictory = PlayerPrefs.GetInt(KEY_CONSECUTIVE_VICTORY);
            consecutiveDefeat = PlayerPrefs.GetInt(KEY_CONSECUTIVE_DEFEAT);
            return;
        }

        // If difficulty is not set, return a default value based on the level (assuming 'level' variable is defined elsewhere).
        difficulty = level == 0 ? 1 : 5;
    }

    /// <summary>
    /// Change the game difficulty by the specified amount, clamped between 1 and 10.
    /// </summary>
    /// <param name="newDifficulty">The amount by which to change the difficulty.</param>
    public void ChangeDifficulty(int newDifficulty)
    {
        difficulty = Math.Clamp(difficulty + newDifficulty, 1, 10);

        SaveDifficulty();
    }

    /// <summary>
    /// Handles changes in game difficulty.
    /// </summary>
    /// <param name="gamePlayMode">The game mode to change difficulty for.</param>
    void Difficult(GameMode gamePlayMode)
    {
        if (onDifficultHandler == null)
        {
            Debug.LogWarning("GameManager.Difficult: onDifficultHandler is null");
            return;
        }
        if (onDifficultHandler.ContainsKey(gamePlayMode))
            onDifficultHandler[gamePlayMode]();
        else
            Debug.LogWarning($"GameManager.Difficult: No handler for game mode {gamePlayMode}");
    }

    /// <summary>
    /// Handle a victory in the game.
    /// </summary>
    public void WinGame()
    {
        consecutiveVictory++;

        if (consecutiveVictory >= MAX_CONSECUTIVE_GAME && difficulty < 10)
        {
            ChangeDifficulty(1);

            consecutiveVictory = 0;
        }

        consecutiveDefeat = 0;
    }

    /// <summary>
    /// Handle a defeat in the game.
    /// </summary>
    public void LoseGame()
    {
        consecutiveDefeat++;

        if (consecutiveDefeat >= MAX_CONSECUTIVE_GAME && difficulty > 1)
        {
            ChangeDifficulty(-1);

            consecutiveDefeat = 0;
        }

        consecutiveVictory = 0;
    }

    /// <summary>
    /// Handles the retry action for a specific error.
    /// </summary>
    /// <param name="error">The error to retry.</param>
    void ErrorRetry(Errors error)
    {
        if (errorsRetryHandler.ContainsKey(error)) errorsRetryHandler[error]();
    }

    /// <summary>
    /// Handles the close action for a specific error.
    /// </summary>
    /// <param name="error">The error to close.</param>
    void ErrorClose(Errors error)
    {
        if (errorsCloseHandler.ContainsKey(error)) errorsCloseHandler[error]();
    }

    /// <summary>
    /// Initiates the process after a successful anonymous login.
    /// </summary>
    public void LoginSuccessAnonymous()
    {
        StartCoroutine(LoginSuccessAnonymousRutiner());
    }

    /// <summary>
    /// Coroutine to handle anonymous login success operations.
    /// </summary>
    IEnumerator LoginSuccessAnonymousRutiner()
    {
        // Check if the user already exists in the Cloud Firestore database
        Task<bool> checkUserTask = CloudFirestore.Instance.CheckUserExists(userData["id"].ToString());

        yield return new WaitUntil(() => checkUserTask.IsCompleted);

        bool userExists = checkUserTask.Result;

        if (userExists)
        {
            // If user exists, retrieve user-related data and yield break
            CloudFirestore.Instance.GetUserData(userData["id"].ToString());
            var _ = CloudFirestore.Instance.UserLevels;
            var __ = CloudFirestore.Instance.UserCollectibles;
            StartCoroutine(WaitUserAlready());
            yield break;
        }

        // If user does not exist, create a new user
        CloudFirestore.Instance.CreateNewUser(userData);
    }

    IEnumerator WaitUserAlready()
    {
        yield return new WaitForSecondsRealtime(1);
        UserAlready = true;
    }

    /// <summary>
    /// Checks the internet connection and initiates the connection check routine if available.
    /// </summary>
    void CheckInternetConnection(Scene scene)
    {
        if (NetworkInterface.GetIsNetworkAvailable())
        {
            // Initiate the internet connection check routine.
            StartCoroutine(CheckInternetConnectionRutiner(scene));
        }
        else HasFoundError(Errors.NNA_GM_THIS); // Handle error when no network connection is available.
    }

    /// <summary>
    /// Coroutine to perform the internet connection check.
    /// </summary>
    IEnumerator CheckInternetConnectionRutiner(Scene scene)
    {
        Task<bool> result = IsConnectionNetwork();

        yield return new WaitUntil(() => result.IsCompleted);

        if (result.Result)
        {
            if (scene.name == "MainMenu")
            {
                // Start the Firebase service when network connection is available.
                FirebaseApp firebaseApp = FindFirstObjectByType<FirebaseApp>();
                if (firebaseApp != null)
                {
                    firebaseApp.StartFirebaseService();
                }
                else
                {
                    Debug.LogWarning("CheckInternetConnectionRutiner: FirebaseApp not found in scene");
                }
            }
        }
        else HasFoundError(Errors.NNA_GM_THIS);  // Handle error when network connection check fails.
    }

    /// <summary>
    /// Checks if a network connection to a specific URL is available.
    /// </summary>
    /// <returns>True if network connection is available, otherwise false.</returns>
    async Task<bool> IsConnectionNetwork()
    {
        UnityWebRequest www = UnityWebRequest.Get("https://www.google.com");
        var asyncOperation = www.SendWebRequest();

        while (!asyncOperation.isDone)
        {
            await Task.Yield();
        }

        if (www.result == UnityWebRequest.Result.Success)
        {
            // Network connection is available.
            return true;
        }

        // Network connection is not available.
        return false;
    }

    /// <summary>
    /// Increases the level count and starts the next level routine.
    /// </summary>
    public void NextLevel(int stars, int score, int bonus)
    {
        Dictionary<string, object> currentLevel = levelsData[this.currentLevel];

        if (IsTheCurrentLevel())
        {
            UpdateLevelComplete(currentLevel, stars, score, true, bonus);
            // Starts the next level routine
            StartCoroutine(NextLevelRoutine());
        }
        else
        {
            if (stars > Convert.ToInt32(currentLevel["Stars"]))
            {
                UpdateLevelComplete(currentLevel, stars, null, false, bonus);
            }

            if (score > Convert.ToInt32(currentLevel["Score"]))
            {
                UpdateLevelComplete(currentLevel, null, score, false, bonus);
            }
        }
    }

    public bool IsTheCurrentLevel() => currentLevel == level;

    /// <summary>
    /// Updates the completion status of the current level.
    /// </summary>
    /// <param name="currentLevel">The dictionary representing the current level.</param>
    /// <param name="stars">The number of stars obtained by the player.</param>
    /// <param name="updateLevelDataBase">Indicates whether to update the level data in the database.</param>
    void UpdateLevelComplete(Dictionary<string, object> currentLevel, int? stars, int? score, bool updateLevelDataBase, int bonus)
    {
        if (stars != null)
            currentLevel["Stars"] = stars;

        if (score != null)
            currentLevel["Score"] = score;

        CloudFirestore.Instance.UpdateDocumentLevel($"level {this.currentLevel + 1}", currentLevel);

        if (updateLevelDataBase)
        {
            CloudFirestore.Instance.UpdateLevelUser(new Dictionary<string, object> { { "level", this.currentLevel + 1 } });
            CoinController.Instance.ChangeCoins(rewardLevelPass + bonus);
        }
    }

    /// <summary>
    /// Waits for 2 seconds and finds the LevelManager object.
    /// </summary>
    IEnumerator NextLevelRoutine()
    {
        yield return new WaitForSeconds(2.5f);

        // Finds the LevelManager object
    LevelManager levelManager = FindFirstObjectByType<LevelManager>();
        levelManager.NextLevel();
    }

    /// <summary>
    /// Called when a scene is loaded, checks if the scene is "Game" and sets the objective game mode.
    /// </summary>
    /// <param name="scene">The scene that was loaded</param>
    /// <param name="mode">The mode used to load the scene</param>
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        errorHandler = GameObject.FindFirstObjectByType<ErrorHandler>(FindObjectsInactive.Include);

        if (scene.name == "Game")
        {
            if (AdsManager.Instance != null)
                AdsManager.Instance.DestroyBannerView();
            currentGameState = GameState.InGame;
            OnGameMode?.Invoke(gameMode);
            if (Inventory.Instance != null)
                Inventory.Instance.SetPowerUpGame();
        }
        else if (scene.name == "LevelMenu")
        {
            if (AdsManager.Instance != null)
            {
                AdsManager.Instance.DestroyBannerView();
                AdsManager.Instance.LoadAd(AdPosition.Bottom);
            }
            currentGameState = GameState.LevelMenu;
            Time.timeScale = 1;
            UpdateAvatars();
            if (CoinController.Instance != null)
                CoinController.Instance.UpdateCoinsUI();
            if (LifeController.Instance != null)
                LifeController.Instance.UpdateLivesUI();
        }
        else if (scene.name == "MainMenu")
        {
            if (AdsManager.Instance != null)
                AdsManager.Instance.LoadAd(AdPosition.Top);
        }

        CheckInternetConnection(scene);
    }

    /// <summary>
    /// Gets a random game play mode from the GamePlayMode enum.
    /// </summary>
    /// <returns>A random game play mode</returns>
    public GamePlayMode GetRandomGamePlayMode()
    {
        int numberOfGamePlayMode = Enum.GetNames(typeof(GamePlayMode)).Length;

        return (GamePlayMode)UnityEngine.Random.Range(0, numberOfGamePlayMode);
    }

    /// <summary>
    /// Coroutine for loading the game.
    /// </summary>
    /// <returns>IEnumerator for coroutine execution.</returns>
    public IEnumerator LoadingGameRutiner()
    {
        while ((FirebaseApp.Instance == null || FirebaseApp.Instance.User == null || userPhoto == null && (FirebaseApp.Instance.User != null && !FirebaseApp.Instance.User.IsAnonymous)) || userData == null)
        {
            yield return null;
        }
    }

    /// <summary>
    /// Updates the avatars for all players.
    /// </summary>
    public void UpdateAvatars()
    {
        StartCoroutine(UpdateAvatarsRutiner());
    }

    /// <summary>
    /// Coroutine for updating avatars.
    /// </summary>
    /// <returns>IEnumerator object.</returns>
    IEnumerator UpdateAvatarsRutiner()
    {
        yield return StartCoroutine(LoadingGameRutiner());

        // Find all AvatarController objects in the scene
        AvatarController[] avatars = Resources.FindObjectsOfTypeAll<AvatarController>();

        // Update the avatars with the user's gender and photo
        for (int i = 0; i < avatars.Length; i++)
        {
            if (FirebaseApp.Instance != null && FirebaseApp.Instance.User != null && userData != null && userPhoto != null && userData.ContainsKey("gender") && !FirebaseApp.Instance.User.IsAnonymous)
                avatars[i].UpdateAvatar((GenderUser)Enum.Parse(typeof(GenderUser), userData["gender"].ToString()), userPhoto);
            else
                avatars[i].UpdateAvatarAnonymous();
        }
    }

    /// <summary>
    /// Start the coroutine for displaying the error UI.
    /// </summary>
    /// <param name="error">The error to display.</param>
    public void HasFoundError(Errors error)
    {
        StartCoroutine(HasFoundErrorRutiner(error));
    }

    /// <summary>
    /// Coroutine for handling the error UI display and interaction.
    /// </summary>
    /// <param name="error">The error to handle.</param>
    /// <returns>An enumerator.</returns>
    IEnumerator HasFoundErrorRutiner(Errors error)
    {
        while (errorHandler == null)
        {
            yield return null;
        }

        errorHandler.gameObject.SetActive(true);
        errorHandler.ShowErrorMessage(error);
    }

    /// <summary>
    /// Start the coroutine for displaying the error UI.
    /// </summary>
    /// <param name="error">The error to display.</param>
    public void HideDisplayError()
    {
        errorHandler.HideDisplayError();
    }

    /// <summary>
    /// Closes the error handling process for retrieving user data.
    /// </summary>
    public void ResetCurrentSceneAndSignOut()
    {
        FirebaseApp.Instance.SignOut();
        SceneManager.LoadScene("MainMenu");
    }

    /// <summary>
    /// Retrieves the current user's level from the userData dictionary.
    /// If the level data is not available, it updates the user's level to 0.
    /// </summary>
    public void GetCurrentLevelUser()
    {
        if (userData.ContainsKey("level"))
            level = Convert.ToInt32(userData["level"]);
        else
        {
            CloudFirestore.Instance.UpdateLevelUser(new Dictionary<string, object> { { "level", 0 } });
            level = 0;
        }
    }

    /// <summary>
    /// Retry network error handling by rechecking the internet connection.
    /// </summary>
    void RetryErrorNetworkAvailable()
    {
        HideDisplayError();
        // Retry by initiating internet connection check.
        StartCoroutine(RetryErrorGetUserCollectiblesRutiner());
    }

    /// <summary>
    /// Coroutine to retry handling network error by rechecking the internet connection.
    /// </summary>
    IEnumerator RetryErrorGetUserCollectiblesRutiner()
    {
        // Retry the internet connection check after a short delay.
        yield return new WaitForSeconds(.1f);
        CheckInternetConnection(SceneManager.GetActiveScene());
    }

    /// <summary>
    /// Close the error handling due to a network error and reset the current scene while signing out.
    /// </summary>
    void CloseErrorNetworkAvailable()
    {
        // Close the error UI, reset the scene, and sign out the user.
        ResetCurrentSceneAndSignOut();
    }
}