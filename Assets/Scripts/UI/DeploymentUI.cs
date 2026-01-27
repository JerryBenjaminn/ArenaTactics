using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI panel for the deployment phase.
/// </summary>
public class DeploymentUI : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private GameObject deploymentPanel;
    [SerializeField] private TextMeshProUGUI instructionsText;
    [SerializeField] private Button readyButton;
    [SerializeField] private CanvasGroup canvasGroup;

    private void Awake()
    {
        if (deploymentPanel == null)
        {
            Debug.LogError("DeploymentUI: deploymentPanel is not assigned in Inspector!");
        }

        if (canvasGroup == null && deploymentPanel != null)
        {
            canvasGroup = deploymentPanel.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = deploymentPanel.AddComponent<CanvasGroup>();
            }
        }

        if (readyButton != null)
        {
            readyButton.onClick.AddListener(OnReadyClicked);
        }

        Hide();
    }

    public void Show()
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }
        else
        {
            Debug.LogError("DeploymentUI.Show() - CanvasGroup is NULL!");
        }

        if (instructionsText != null)
        {
            instructionsText.text = "Position your gladiators in the highlighted zone.\nClick a gladiator, then click a tile to move it.";
        }
    }

    public void Hide()
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
    }

    private void OnReadyClicked()
    {
        BattleManager battleManager = FindAnyObjectByType<BattleManager>();
        if (battleManager != null)
        {
            battleManager.CompleteDeployment();
        }

        Hide();
    }
}
