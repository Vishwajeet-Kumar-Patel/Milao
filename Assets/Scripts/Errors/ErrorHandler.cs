using UnityEngine;
using TMPro;
using System.ComponentModel;
using System;

public class ErrorHandler : MonoBehaviour
{
    [SerializeField] TMP_Text errorMessage;
    [SerializeField] TMP_Text errorReference;
    [SerializeField] string emailAddress;
    [SerializeField] string subject;
    [SerializeField, TextArea] string body;

    Errors error;

    /// <summary>
    /// Displays the error message for a specific error.
    /// </summary>
    /// <param name="error">The error to display.</param>
    public void ShowErrorMessage(Errors error)
    {
        this.error = error;
        var fieldInfo = error.GetType().GetField(error.ToString());
        var attributes = fieldInfo.GetCustomAttributes(typeof(DescriptionAttribute), false) as DescriptionAttribute[];

        this.errorMessage.text = attributes[0].Description;
        this.errorReference.text = error.ToString();
    }

    /// <summary>
    /// Retry the action associated with the error.
    /// </summary>
    public void Retry()
    {
        GameManager.Instance.OnErrorRetry?.Invoke(error);
    }

    /// <summary>
    /// Close the error and hide the display.
    /// </summary>
    public void Close()
    {
        HideDisplayError();
        GameManager.Instance.OnErrorClose?.Invoke(error);
    }

    /// <summary>
    /// Close the error and hide the display.
    /// </summary>
    public void HideDisplayError()
    {
        this.gameObject.SetActive(false);
    }

    public void Contact()
    {
        string encodedSubject = Uri.EscapeDataString(subject);
        string encodedBody = Uri.EscapeDataString(body);

        string emailUrl = "mailto:" + emailAddress + "?subject=" + encodedSubject + "&body=" + encodedBody;
        Application.OpenURL(emailUrl);
    }
}