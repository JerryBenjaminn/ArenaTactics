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
    [SerializeField] private TextMeshProUGUI classText;
    [SerializeField] private TextMeshProUGUI raceText;
    [SerializeField] private TextMeshProUGUI specialsText;

    [Header("Resources")]
    [SerializeField] private Image hpBarFill;
    [SerializeField] private TextMeshProUGUI hpText;
    [SerializeField] private Image mpBarFill;
    [SerializeField] private TextMeshProUGUI mpText;
    [SerializeField] private TextMeshProUGUI apText;
    [SerializeField] private Image xpBarFill;
    [SerializeField] private TextMeshProUGUI xpText;
    [SerializeField] private TextMeshProUGUI levelText;

    [Header("Stats")]
    [SerializeField] private TextMeshProUGUI attackText;
    [SerializeField] private TextMeshProUGUI strengthText;
    [SerializeField] private TextMeshProUGUI dexterityText;
    [SerializeField] private TextMeshProUGUI intelligenceText;
    [SerializeField] private TextMeshProUGUI defenseText;
    [SerializeField] private TextMeshProUGUI speedText;
    [SerializeField] private TextMeshProUGUI movementText;
    [SerializeField] private TextMeshProUGUI accuracyText;
    [SerializeField] private TextMeshProUGUI dodgeText;
    [SerializeField] private TextMeshProUGUI critText;
    [SerializeField] private TextMeshProUGUI spellCritText;
    [SerializeField] private TextMeshProUGUI initiativeText;
    [SerializeField] private TextMeshProUGUI magicResistText;
    [SerializeField] private TextMeshProUGUI spellSlotsText;

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

        if (raceText != null)
        {
            raceText.text = data.race != null ? $"Race: {data.race.raceName}" : "Race: None";
        }

        if (classText != null)
        {
            classText.text = data.gladiatorClass != null
                ? $"Class: {data.gladiatorClass.className}"
                : "Class: None";
        }

        UpdateResourceBar(hpBarFill, hpText, currentGladiator.CurrentHP, currentGladiator.MaxHP, "HP");
        UpdateResourceBar(mpBarFill, mpText, currentGladiator.RemainingMP, currentGladiator.MaxMP, "MP");

        if (apText != null)
        {
            apText.text = $"AP: {currentGladiator.RemainingAP}/{currentGladiator.MaxAP}";
        }

        if (levelText != null)
        {
            levelText.text = $"Level: {currentGladiator.CurrentLevel}";
        }

        UpdateXPBar();

        if (attackText != null)
        {
            int totalAttack = currentGladiator.GetTotalAttack();
            string scaling = "STR";
            if (currentGladiator.EquippedWeapon != null)
            {
                switch (currentGladiator.EquippedWeapon.scalingStat)
                {
                    case ScalingStat.Dexterity:
                        scaling = "DEX";
                        break;
                    case ScalingStat.Intelligence:
                        scaling = "INT";
                        break;
                }
            }
            attackText.text = $"Attack: {totalAttack} ({scaling})";
        }

        if (strengthText != null)
        {
            strengthText.text = $"STR: {currentGladiator.GetTotalStrength()}";
        }

        if (dexterityText != null)
        {
            dexterityText.text = $"DEX: {currentGladiator.GetTotalDexterity()}";
        }

        if (intelligenceText != null)
        {
            intelligenceText.text = $"INT: {currentGladiator.GetTotalIntelligence()}";
        }

        if (defenseText != null)
        {
            int baseDefense = data.Defense;
            int totalDefense = currentGladiator.GetTotalDefense();
            int bonusDefense = totalDefense - baseDefense;
            defenseText.text = bonusDefense > 0
                ? $"Defense: {totalDefense} ({baseDefense} +{bonusDefense})"
                : $"Defense: {baseDefense}";
        }

        if (speedText != null)
        {
            speedText.text = $"Speed: {data.Speed}";
        }

        if (movementText != null)
        {
            movementText.text = $"Movement: {data.MovementPoints}";
        }

        if (accuracyText != null)
        {
            accuracyText.text = $"Accuracy: {currentGladiator.GetAccuracy():P0}";
        }

        if (dodgeText != null)
        {
            dodgeText.text = $"Dodge: {currentGladiator.GetDodgeChance():P0}";
        }

        if (critText != null)
        {
            critText.text = $"Crit: {currentGladiator.GetCritChance():P0}";
        }

        if (spellCritText != null)
        {
            spellCritText.text = $"Spell Crit: {currentGladiator.GetSpellCritChance():P0}";
        }

        if (initiativeText != null)
        {
            initiativeText.text = $"Initiative: {currentGladiator.GetInitiative()}";
        }

        if (magicResistText != null)
        {
            magicResistText.text = $"Magic Res: {currentGladiator.GetMagicResistance()}";
        }

        if (spellSlotsText != null)
        {
            spellSlotsText.text = $"Spell Slots: {currentGladiator.CurrentSpellSlots}/{currentGladiator.GetSpellSlots()}";
        }

        UpdateEquipment();
        UpdateRaceSpecials();
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

    private void UpdateXPBar()
    {
        if (xpBarFill != null)
        {
            int requiredXP = currentGladiator.XPToNextLevel;
            float percent = requiredXP > 0 ? (float)currentGladiator.CurrentXP / requiredXP : 1f;

            RectTransform fillRect = xpBarFill.GetComponent<RectTransform>();
            RectTransform bgRect = xpBarFill.transform.parent.GetComponent<RectTransform>();

            if (fillRect != null && bgRect != null)
            {
                float maxWidth = bgRect.rect.width;
                fillRect.sizeDelta = new Vector2(maxWidth * Mathf.Clamp01(percent), fillRect.sizeDelta.y);
            }
        }

        if (xpText != null)
        {
            if (currentGladiator.XPToNextLevel <= 0)
            {
                xpText.text = "XP: MAX";
            }
            else
            {
                xpText.text = $"XP: {currentGladiator.CurrentXP}/{currentGladiator.XPToNextLevel}";
            }
        }
    }

    private void UpdateEquipment()
    {
        if (weaponText != null)
        {
            WeaponData weapon = currentGladiator.EquippedWeapon;
            if (weapon != null)
            {
                int range = currentGladiator.GetAttackRange();
                if (weapon.weaponType == WeaponType.Magic)
                {
                    weaponText.text = $"Weapon: {weapon.weaponName} (Magic, 0 damage - spellcasting focus, +{weapon.spellPowerBonus:P0} spell power)";
                }
                else
                {
                    weaponText.text = $"Weapon: {weapon.weaponName} ({weapon.weaponType}, +{weapon.baseDamage} Dmg, Range {range})";
                }
            }
            else
            {
                weaponText.text = "Weapon: None (Unarmed)";
            }
        }

        if (armorText != null)
        {
            ArmorData armor = currentGladiator.EquippedArmor;
            if (armor != null)
            {
                string dodgeText = armor.dodgeBonus != 0f ? $" Dodge {armor.dodgeBonus:P0}," : string.Empty;
                string moveText = armor.movementPenalty != 0 ? $" MP {armor.movementPenalty}," : string.Empty;
                string spellText = armor.spellPowerBonus > 0f ? $" Spell {armor.spellPowerBonus:P0}," : string.Empty;
                string slotsText = armor.spellSlotBonus > 0 ? $" Slots +{armor.spellSlotBonus}," : string.Empty;

                armorText.text =
                    $"Armor: {armor.armorName} (HP +{armor.hpBonus}, DEF +{armor.defenseBonus}," +
                    $" STR +{armor.strengthBonus}, DEX +{armor.dexterityBonus}, INT +{armor.intelligenceBonus}," +
                    $"{dodgeText}{moveText}{spellText}{slotsText} Tier {armor.tier})";
            }
            else
            {
                armorText.text = "Armor: None";
            }
        }

        if (trinketText != null)
        {
            trinketText.text = "Trinket: None";
        }
    }

    private void UpdateRaceSpecials()
    {
        if (specialsText == null || currentGladiator == null || currentGladiator.Data == null)
        {
            return;
        }

        if (currentGladiator.Data.race == null)
        {
            specialsText.text = "No racial bonuses";
            return;
        }

        RaceData race = currentGladiator.Data.race;
        System.Text.StringBuilder builder = new System.Text.StringBuilder();

        if (race.xpBonusMultiplier > 1f)
        {
            builder.AppendLine($"+{(race.xpBonusMultiplier - 1f) * 100f:0}% XP");
        }
        if (race.meleeDamageBonus > 0f)
        {
            builder.AppendLine($"+{race.meleeDamageBonus * 100f:0}% Melee Damage");
        }
        if (race.dexWeaponDamageBonus > 0f)
        {
            builder.AppendLine($"+{race.dexWeaponDamageBonus * 100f:0}% DEX Weapon Damage");
        }
        if (race.magicResistBonus > 0f)
        {
            builder.AppendLine($"+{race.magicResistBonus * 100f:0}% Magic Resist");
        }
        if (race.physicalDamageReduction > 0f)
        {
            builder.AppendLine($"-{race.physicalDamageReduction * 100f:0}% Physical Damage");
        }
        if (race.dodgeBonus > 0f)
        {
            builder.AppendLine($"+{race.dodgeBonus * 100f:0}% Dodge");
        }
        if (race.spellPowerBonus > 0f)
        {
            builder.AppendLine($"+{race.spellPowerBonus * 100f:0}% Spell Power");
        }
        if (race.spellSlotBonus > 0)
        {
            builder.AppendLine($"+{race.spellSlotBonus} Spell Slots");
        }
        if (race.immuneToStunParalysis)
        {
            builder.AppendLine("Immune to Stun");
        }
        if (race.hasPoisonOnHit)
        {
            builder.AppendLine($"Poison on hit ({race.poisonChance * 100f:0}% for {race.poisonDamagePerTurn} dmg)");
        }
        if (race.hpRegenPerTurn > 0f)
        {
            builder.AppendLine($"Regenerate {race.hpRegenPerTurn * 100f:0}% HP/turn");
        }

        specialsText.text = builder.Length > 0 ? builder.ToString().TrimEnd() : "No racial bonuses";
    }
}
