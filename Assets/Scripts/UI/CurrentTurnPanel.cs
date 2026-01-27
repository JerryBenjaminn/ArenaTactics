using ArenaTactics.Data;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UI panel showing current turn gladiator info.
/// </summary>
public class CurrentTurnPanel : MonoBehaviour
{
    [Header("Gladiator Info")]
    [SerializeField] private Image portraitImage;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private Image hpBarFill;
    [SerializeField] private TextMeshProUGUI hpText;
    [SerializeField] private TextMeshProUGUI mpText;
    [SerializeField] private TextMeshProUGUI apText;

    [Header("Action Buttons")]
    [SerializeField] private Button undoButton;
    [SerializeField] private Button endTurnButton;

    [Header("Colors")]
    [SerializeField] private Color playerColor = new Color(0.2f, 0.4f, 1f);
    [SerializeField] private Color enemyColor = new Color(1f, 0.3f, 0.3f);
    [SerializeField] private Color hpHighColor = Color.green;
    [SerializeField] private Color hpMidColor = Color.yellow;
    [SerializeField] private Color hpLowColor = Color.red;

    private void Start()
    {
        if (undoButton != null)
        {
            undoButton.onClick.AddListener(OnUndoClicked);
        }

        if (endTurnButton != null)
        {
            endTurnButton.onClick.AddListener(OnEndTurnClicked);
        }

        gameObject.SetActive(false);
    }

    public void UpdatePanel(Gladiator gladiator)
    {
        if (gladiator == null || gladiator.Data == null)
        {
            gameObject.SetActive(false);
            return;
        }

        gameObject.SetActive(true);

        if (portraitImage != null)
        {
            portraitImage.color = gladiator.Data.team == Team.Player ? playerColor : enemyColor;
        }

        if (nameText != null)
        {
            nameText.text = gladiator.Data.gladiatorName;
        }

        float hpPercent = gladiator.MaxHP > 0
            ? gladiator.CurrentHP / (float)gladiator.MaxHP
            : 0f;

        if (hpBarFill != null)
        {
            RectTransform fillRect = hpBarFill.GetComponent<RectTransform>();
            RectTransform backgroundRect = hpBarFill.transform.parent.GetComponent<RectTransform>();

            if (fillRect != null && backgroundRect != null)
            {
                float maxWidth = backgroundRect.rect.width;
                fillRect.sizeDelta = new Vector2(maxWidth * Mathf.Clamp01(hpPercent), fillRect.sizeDelta.y);
            }

            if (hpPercent > 0.5f)
            {
                hpBarFill.color = hpHighColor;
            }
            else if (hpPercent > 0.3f)
            {
                hpBarFill.color = hpMidColor;
            }
            else
            {
                hpBarFill.color = hpLowColor;
            }
        }

        if (hpText != null)
        {
            hpText.text = $"HP: {gladiator.CurrentHP}/{gladiator.MaxHP}";
        }

        if (mpText != null)
        {
            mpText.text = $"MP: {gladiator.RemainingMP}/{gladiator.MaxMP}";
        }

        if (apText != null)
        {
            apText.text = $"AP: {gladiator.RemainingAP}/{gladiator.MaxAP}";
        }

        UpdateButtonStates(gladiator);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    private void UpdateButtonStates(Gladiator gladiator)
    {
        if (undoButton != null)
        {
            bool canUndo = gladiator.IsPlayerControlled &&
                           PlayerInputController.Instance != null &&
                           PlayerInputController.Instance.CanUndo();
            undoButton.interactable = canUndo;
        }

        if (endTurnButton != null)
        {
            endTurnButton.interactable = gladiator.IsPlayerControlled;
        }
    }

    private void OnUndoClicked()
    {
        if (PlayerInputController.Instance != null)
        {
            PlayerInputController.Instance.UndoMove();
        }
    }

    private void OnEndTurnClicked()
    {
        if (PlayerInputController.Instance != null)
        {
            PlayerInputController.Instance.EndTurn();
        }
    }
}
