using ArenaTactics.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Popup window showing detailed gladiator info.
/// </summary>
public class GladiatorInfoWindow : MonoBehaviour
{
    [Header("Window")]
    [SerializeField] private GameObject windowPanel;

    [Header("Gladiator Info")]
    [SerializeField] private Image portraitImage;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI teamText;

    [Header("Resources")]
    [SerializeField] private Image hpBarFill;
    [SerializeField] private TextMeshProUGUI hpText;
    [SerializeField] private Image mpBarFill;
    [SerializeField] private TextMeshProUGUI mpText;
    [SerializeField] private TextMeshProUGUI apText;

    [Header("Stats")]
    [SerializeField] private TextMeshProUGUI attackText;
    [SerializeField] private TextMeshProUGUI defenseText;
    [SerializeField] private TextMeshProUGUI speedText;
    [SerializeField] private TextMeshProUGUI movementText;

    [Header("Equipment")]
    [SerializeField] private TextMeshProUGUI weaponText;
    [SerializeField] private TextMeshProUGUI armorText;
    [SerializeField] private TextMeshProUGUI trinketText;

    [Header("Buttons")]
    [SerializeField] private Button closeButton;

    [Header("Colors")]
    [SerializeField] private Color playerColor = new Color(0.2f, 0.4f, 1f);
    [SerializeField] private Color enemyColor = new Color(1f, 0.3f, 0.3f);

    [Header("Update Settings")]
    [SerializeField] private bool autoUpdate = true;
    [SerializeField] private float updateInterval = 0.2f;

    private Gladiator currentGladiator;
    private float lastUpdateTime;

    private void Start()
    {
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(Close);
        }

        Hide();
    }

    private void Update()
    {
        if (autoUpdate && windowPanel != null && windowPanel.activeSelf && currentGladiator != null)
        {
            if (Time.time - lastUpdateTime >= updateInterval)
            {
                UpdateDisplay();
                lastUpdateTime = Time.time;
            }
        }
    }

    public void Show(Gladiator gladiator)
    {
        if (gladiator == null || gladiator.Data == null)
        {
            Hide();
            return;
        }

        currentGladiator = gladiator;
        lastUpdateTime = Time.time;
        UpdateDisplay();

        if (windowPanel != null)
        {
            windowPanel.SetActive(true);
        }
    }

    public void Hide()
    {
        if (windowPanel != null)
        {
            windowPanel.SetActive(false);
        }

        currentGladiator = null;
    }

    public void Close()
    {
        Hide();
    }

    public bool IsShowing()
    {
        return windowPanel != null && windowPanel.activeSelf;
    }

    public Gladiator GetCurrentGladiator()
    {
        return currentGladiator;
    }

    private void UpdateDisplay()
    {
        if (currentGladiator == null || currentGladiator.Data == null)
        {
            return;
        }

        GladiatorData data = currentGladiator.Data;

        if (portraitImage != null)
        {
            portraitImage.color = data.team == Team.Player ? playerColor : enemyColor;
        }

        if (nameText != null)
        {
            nameText.text = data.gladiatorName;
        }

        if (teamText != null)
        {
            teamText.text = data.team == Team.Player ? "Player" : "Enemy";
            teamText.color = data.team == Team.Player ? playerColor : enemyColor;
        }

        UpdateResourceBar(hpBarFill, hpText, currentGladiator.CurrentHP, currentGladiator.MaxHP, "HP");
        UpdateResourceBar(mpBarFill, mpText, currentGladiator.RemainingMP, currentGladiator.MaxMP, "MP");

        if (apText != null)
        {
            apText.text = $"AP: {currentGladiator.RemainingAP}/{currentGladiator.MaxAP}";
        }

        if (attackText != null)
        {
            int baseAttack = data.attack;
            int totalAttack = currentGladiator.GetTotalAttack();
            int weaponBonus = totalAttack - baseAttack;

            attackText.text = weaponBonus > 0
                ? $"Attack: {totalAttack} ({baseAttack} +{weaponBonus})"
                : $"Attack: {baseAttack}";
        }

        if (defenseText != null)
        {
            defenseText.text = $"Defense: {data.defense}";
        }

        if (speedText != null)
        {
            speedText.text = $"Speed: {data.speed}";
        }

        if (movementText != null)
        {
            movementText.text = $"Movement: {data.movementPoints}";
        }

        UpdateEquipment();
    }

    private void UpdateResourceBar(Image barFill, TextMeshProUGUI text, int current, int max, string label)
    {
        if (barFill != null)
        {
            float percent = max > 0 ? (float)current / max : 0f;

            RectTransform fillRect = barFill.GetComponent<RectTransform>();
            RectTransform bgRect = barFill.transform.parent.GetComponent<RectTransform>();

            if (fillRect != null && bgRect != null)
            {
                float maxWidth = bgRect.rect.width;
                fillRect.sizeDelta = new Vector2(maxWidth * Mathf.Clamp01(percent), fillRect.sizeDelta.y);
            }
        }

        if (text != null)
        {
            text.text = $"{label}: {current}/{max}";
        }
    }

    private void UpdateEquipment()
    {
        if (weaponText != null)
        {
            WeaponData weapon = currentGladiator.EquippedWeapon;
            if (weapon != null)
            {
                weaponText.text = $"Weapon: {weapon.weaponName} (+{weapon.baseDamage} Dmg, Range {weapon.attackRange})";
            }
            else
            {
                weaponText.text = "Weapon: None (Unarmed)";
            }
        }

        if (armorText != null)
        {
            armorText.text = "Armor: None";
        }

        if (trinketText != null)
        {
            trinketText.text = "Trinket: None";
        }
    }
}
