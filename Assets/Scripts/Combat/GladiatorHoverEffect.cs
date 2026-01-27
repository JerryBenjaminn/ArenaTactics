using UnityEngine;

/// <summary>
/// Provides simple hover feedback for gladiators.
/// </summary>
public class GladiatorHoverEffect : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Gladiator gladiator;
    [SerializeField] private Renderer gladiatorRenderer;

    [Header("Hover Settings")]
    [SerializeField] private Color hoverTintColor = new Color(1.2f, 1.2f, 1.2f, 1f);
    [SerializeField] private float hoverScaleMultiplier = 1.05f;

    private Color originalColor = Color.white;
    private Vector3 originalScale;
    private bool isHovering;

    private void Start()
    {
        if (gladiator == null)
        {
            gladiator = GetComponent<Gladiator>();
        }

        if (gladiatorRenderer == null)
        {
            gladiatorRenderer = GetComponentInChildren<Renderer>();
        }

        if (gladiatorRenderer != null)
        {
            originalColor = gladiatorRenderer.material.color;
        }

        originalScale = transform.localScale;
    }

    private void OnMouseEnter()
    {
        if (!isHovering)
        {
            ApplyHoverEffect();
        }
    }

    private void OnMouseExit()
    {
        if (isHovering)
        {
            RemoveHoverEffect();
        }
    }

    private void ApplyHoverEffect()
    {
        isHovering = true;

        if (gladiatorRenderer != null)
        {
            gladiatorRenderer.material.color = originalColor * hoverTintColor;
        }

        transform.localScale = originalScale * hoverScaleMultiplier;
    }

    private void RemoveHoverEffect()
    {
        isHovering = false;

        if (gladiatorRenderer != null)
        {
            gladiatorRenderer.material.color = originalColor;
        }

        transform.localScale = originalScale;
    }

    private void OnDisable()
    {
        if (isHovering)
        {
            RemoveHoverEffect();
        }
    }
}
