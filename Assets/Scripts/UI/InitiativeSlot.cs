using ArenaTactics.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI slot for a single initiative entry.
/// </summary>
public class InitiativeSlot : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private Image portraitImage;
    [SerializeField] private Image borderImage;
    [SerializeField] private TextMeshProUGUI nameText;

    [Header("Colors")]
    [SerializeField] private Color playerColor = new Color(0.2f, 0.4f, 1f);
    [SerializeField] private Color enemyColor = new Color(1f, 0.3f, 0.3f);
    [SerializeField] private Color currentBorderColor = Color.yellow;
    [SerializeField] private Color normalBorderColor = new Color(0.5f, 0.5f, 0.5f);

    [Header("Scaling")]
    [SerializeField] private float normalScale = 1f;
    [SerializeField] private float currentScale = 1.3f;

    public void Setup(Gladiator gladiator, bool isCurrent)
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
            string displayName = gladiator.Data.gladiatorName;
            if (!string.IsNullOrEmpty(displayName) && displayName.Length > 8)
            {
                displayName = displayName.Substring(0, 7) + ".";
            }
            nameText.text = displayName;
        }

        if (borderImage != null)
        {
            borderImage.color = isCurrent ? currentBorderColor : normalBorderColor;
        }

        transform.localScale = Vector3.one * (isCurrent ? currentScale : normalScale);
    }
}
