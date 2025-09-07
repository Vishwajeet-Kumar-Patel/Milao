using System.Collections;
using UnityEngine;
using DG.Tweening;
using TMPro;

public class MainMenuController : MonoBehaviour
{
    [SerializeField] LoginController LoginController;
    [SerializeField] AudioSource audioSourceLoginUI;
    [SerializeField] AudioSource audioEnvironment;
    [SerializeField] CanvasGroup loading;
    [SerializeField] CanvasGroup alreadyLoading;
    [Header("Main Menu Buttons")]
    [SerializeField] GameObject playButton;
    [SerializeField] GameObject loginButton;

    public void LinkAssets()
    {
        Application.OpenURL("https://assetstore.unity.com/packages/2d/gui/hungry-bat-match-3-ui-free-229197#publisher");
    }

    /// <summary>
    /// Initiates the screen change transition and loads the level menu scene.
    /// </summary>
    public void Play()
    {
        // If transition and Firebase are available, use them. Otherwise, load LevelMenu directly.
        if (ScreenChangeTransition.Instance != null)
        {
            if (FirebaseApp.Instance != null && FirebaseApp.Instance.User == null)
            {
                FirebaseApp.Instance.LoginAnonymous();
                StartCoroutine(ScreenChangeTransition.Instance.FadeOut("LevelMenu", true));
            }
            else
            {
                StartCoroutine(ScreenChangeTransition.Instance.FadeOut("LevelMenu"));
            }
        }
        else
        {
            // Fallback: load LevelMenu scene directly
            UnityEngine.SceneManagement.SceneManager.LoadScene("LevelMenu");
        }
    }

    /// <summary>
    /// Logs in by activating the specified screen and fading it in.
    /// </summary>
    /// <param name="screen">The screen GameObject to be logged in.</param>
    public void Login(GameObject screen)
    {
        screen.SetActive(true);
        CanvasGroup canvasGroup = screen.GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0;
            canvasGroup.DOFade(1, 0.5f);
        }
    }

    /// <summary>
    /// Closes the UI login by playing the login UI audio, fading out the screen, and deactivating it when complete.
    /// </summary>
    /// <param name="screen">The screen GameObject to be closed.</param>
    public void CloseUILogin(GameObject screen)
    {
        audioSourceLoginUI.Play();

        screen.SetActive(true);
        CanvasGroup canvasGroup = screen.GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1;
            canvasGroup.DOFade(0, 0.5f).OnComplete(() => {
                if (canvasGroup != null)
                    screen.SetActive(false);
            });
        }
    }

    /// <summary>
    /// Checks if the game is still loading and waits until it's ready.
    /// </summary>
    public void CheckLoadingGame()
    {
        StartCoroutine(CheckLoadingGameRutiner());
    }

    /// <summary>
    /// Coroutine for checking if the game is still loading.
    /// </summary>
    /// <returns>IEnumerator for coroutine execution.</returns>
    IEnumerator CheckLoadingGameRutiner()
    {
        yield return StartCoroutine(GameManager.Instance.LoadingGameRutiner());

        GameReadyToPlay();
    }

    /// <summary>
    /// Performs necessary actions when the game is ready to play.
    /// </summary>
    void GameReadyToPlay()
    {
        LoginController.UserAlreadyAuthenticated();
        ShowUIGameReadyToPlay();
    }

    /// <summary>
    /// Displays the UI for game readiness.
    /// </summary>
    public void ShowUIGameReadyToPlay()
    {
        audioEnvironment.Play();

        if (loading != null)
        {
            loading.DOFade(0, .8f).OnComplete(() =>
            {
                if (loading != null)
                    loading.gameObject.SetActive(false);

                // Show Play and Login buttons directly
                if (playButton != null)
                {
                    playButton.SetActive(true);
                    // Also ensure parent is active and visible
                    if (playButton.transform.parent != null)
                        playButton.transform.parent.gameObject.SetActive(true);
                }
                else
                    Debug.LogWarning("MainMenuController: playButton not assigned");

                if (loginButton != null)
                {
                    loginButton.SetActive(true);
                    if (loginButton.transform.parent != null)
                        loginButton.transform.parent.gameObject.SetActive(true);
                }
                else
                    Debug.LogWarning("MainMenuController: loginButton not assigned");

                // Optionally hide alreadyLoading panel if not needed
                if (alreadyLoading != null)
                    alreadyLoading.gameObject.SetActive(false);
            });
        }
    }
}