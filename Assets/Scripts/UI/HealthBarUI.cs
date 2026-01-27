using ArenaTactics.Data;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// World-space health bar that follows a gladiator.
/// </summary>
public class HealthBarUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private Gladiator gladiator;

    [SerializeField]
    private Canvas canvas;

    [SerializeField]
    private Image fillImage;

    [SerializeField]
    private Image backgroundImage;

    [Header("Settings")]
    [SerializeField]
    private Vector3 offset = new Vector3(0f, 2f, 0f);

    [SerializeField]
    private Color allyColor = Color.green;

    [SerializeField]
    private Color enemyColor = Color.red;

    private Camera mainCamera;

    private void Start()
    {
        mainCamera = Camera.main;

        if (canvas == null)
        {
            canvas = GetComponentInChildren<Canvas>();
        }

        if (fillImage == null)
        {
            fillImage = GetComponentInChildren<Image>();
        }

        if (backgroundImage == null && canvas != null)
        {
            Image[] images = canvas.GetComponentsInChildren<Image>();
            if (images.Length > 1)
            {
                backgroundImage = images[0];
                fillImage = images[1];
            }
        }

        UpdateTeamColor();
    }

    private void LateUpdate()
    {
        if (gladiator == null)
        {
            return;
        }

        UpdateHealthBar();

        if (mainCamera != null && canvas != null)
        {
            canvas.transform.rotation = mainCamera.transform.rotation;
        }

        transform.position = gladiator.transform.position + offset;
    }

    /// <summary>
    /// Assigns the gladiator this health bar should follow.
    /// </summary>
    public void Initialize(Gladiator target)
    {
        gladiator = target;
        UpdateTeamColor();
        UpdateHealthBar();
    }

    /// <summary>
    /// Assigns all runtime references for procedural setup.
    /// </summary>
    public void SetReferences(Gladiator target, Canvas canvasRef, Image fill, Image background)
    {
        gladiator = target;
        canvas = canvasRef;
        fillImage = fill;
        backgroundImage = background;
        UpdateTeamColor();
        UpdateHealthBar();
    }

    private void UpdateTeamColor()
    {
        if (gladiator == null || gladiator.Data == null || fillImage == null)
        {
            return;
        }

        fillImage.color = gladiator.Data.team == Team.Player ? allyColor : enemyColor;
    }

    private void UpdateHealthBar()
    {
        if (fillImage == null || gladiator == null || gladiator.MaxHP <= 0)
        {
            return;
        }

        float hpPercent = gladiator.CurrentHP / (float)gladiator.MaxHP;
        RectTransform fillRect = fillImage.GetComponent<RectTransform>();
        if (fillRect != null)
        {
            fillRect.sizeDelta = new Vector2(100f * Mathf.Clamp01(hpPercent), fillRect.sizeDelta.y);
        }
    }
}
