using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(UpdateScoreUI))]
public class CompleteGameController : MonoBehaviour
{
    [SerializeField] GameObject overlay;
    [SerializeField] GameObject boxCompleteGame;
    [SerializeField] GameObject particleSystemEnergy;
    [SerializeField] GameObject[] boxStars;
    [SerializeField] UpdateScoreUI updateScoreUI;
    [SerializeField] AudioSource audioSource;
    [SerializeField] AudioClip popComplete;
    [SerializeField] GameObject boxBonus;
    [SerializeField] TMP_Text textBonus;

    int stars;
    int score;
    int bonus = 0;

    public void OnCompleteGame()
    {
        stars = ProgressBar.Instance.GetActiveStars();
        score = GUIManager.Instance.Score;
        overlay.SetActive(true);
        boxCompleteGame.SetActive(true);
        particleSystemEnergy.SetActive(true);
        StartCoroutine(ActiveStars(stars));
        StartCoroutine(updateScoreUI.UpdateScoreRutiner());

        if (GUIManager.Instance.MoveCounter > 0 && GUIManager.Instance.GamePlayMode == GamePlayMode.MovesLimited && GameManager.Instance.IsTheCurrentLevel())
            bonus = GUIManager.Instance.MoveCounter;

        if (GameManager.Instance.GameMode == GameMode.TimeObjective)
        {
            TimerGame timerGame = FindFirstObjectByType<TimerGame>();
            bonus = (int)timerGame.TimeRemaining / 10;
        }

        if (bonus > 0)
        {
            boxBonus.SetActive(true);
            textBonus.text = $"+{bonus}";
        }
    }

    IEnumerator ActiveStars(int activeStars)
    {
        yield return new WaitForSeconds(1.2f);
        for (int i = 0; i < activeStars; i++)
        {
            yield return new WaitForSeconds(.07f);
            boxStars[i].SetActive(true);
            boxStars[i].GetComponentInParent<Animator>().enabled = true;
        }
    }

    public void Replay()
    {
        audioSource.PlayOneShot(popComplete);
        StartCoroutine(ScreenChangeTransition.Instance.FadeOut(SceneManager.GetActiveScene().name));
        Inventory.Instance.ResetParentPowerUps(false);
    }

    public void NextLevel()
    {
        if (stars >= 3)
        {
            audioSource.PlayOneShot(popComplete);
            StartCoroutine(ScreenChangeTransition.Instance.FadeOut("LevelMenu"));
            GameManager.Instance.NextLevel(stars, score, bonus);
            GameManager.Instance.WinGame();
            Inventory.Instance.ResetParentPowerUps(true);

            if (LevelManager.Instance != null)
                LevelManager.Instance.NextLevel();
        }
        else
        {
            Debug.Log("You need 3 stars to proceed to the next level!");
            Replay();
        }
    }
}
