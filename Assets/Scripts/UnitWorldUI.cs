using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Displays world-space UI for a unit, including health and name.
/// </summary>
public class UnitWorldUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Unit unit;
    [SerializeField] private Slider healthSlider;
    [SerializeField] private Image healthFillImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private Canvas targetCanvas;
    [SerializeField] private bool hideWhenCameraClose = true;
    [SerializeField] private float hideDistance = 1.5f;

    [Header("Team Colors")]
    [SerializeField] private Color teamOneFillColor = new Color(0.2f, 0.55f, 1f, 1f);
    [SerializeField] private Color teamTwoFillColor = new Color(1f, 0.3f, 0.3f, 1f);

    private int lastHealth = -1;
    private int lastMaxHealth = -1;
    private int lastTeamId = int.MinValue;
    private Color defaultFillColor = Color.white;

    private void Awake()
    {
        if (unit == null)
        {
            unit = GetComponentInParent<Unit>();
        }

        if (healthSlider == null)
        {
            healthSlider = GetComponentInChildren<Slider>(true);
        }
        if (healthFillImage == null && healthSlider != null && healthSlider.fillRect != null)
        {
            healthFillImage = healthSlider.fillRect.GetComponent<Image>();
        }
        if (healthFillImage != null)
        {
            defaultFillColor = healthFillImage.color;
        }

        if (nameText == null)
        {
            nameText = GetComponentInChildren<TMP_Text>(true);
        }

        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }

        if (targetCanvas == null)
        {
            targetCanvas = GetComponentInChildren<Canvas>(true);
        }

        RefreshAll();
    }

    private void OnEnable()
    {
        RefreshAll();
    }

    private void LateUpdate()
    {
        UpdateBillboard();
        UpdateHealth();
        UpdateTeamColor();
        UpdateVisibility();
    }

    private void UpdateBillboard()
    {
        Transform cam = cameraTransform != null ? cameraTransform : Camera.main?.transform;
        if (cam == null)
        {
            return;
        }

        Vector3 toCamera = transform.position - cam.position;
        if (toCamera.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        transform.rotation = Quaternion.LookRotation(toCamera, Vector3.up);
    }

    private void UpdateHealth()
    {
        if (unit == null || healthSlider == null)
        {
            return;
        }

        if (unit.CurrentHealth == lastHealth && unit.MaxHealth == lastMaxHealth)
        {
            return;
        }

        lastHealth = unit.CurrentHealth;
        lastMaxHealth = unit.MaxHealth;
        healthSlider.maxValue = lastMaxHealth;
        healthSlider.value = Mathf.Clamp(lastHealth, 0, lastMaxHealth);
    }

    private void RefreshAll()
    {
        if (unit != null && nameText != null)
        {
            nameText.text = unit.name;
        }

        lastHealth = -1;
        lastMaxHealth = -1;
        lastTeamId = int.MinValue;
        UpdateHealth();
        UpdateTeamColor();
    }

    private void UpdateVisibility()
    {
        if (!hideWhenCameraClose || targetCanvas == null)
        {
            return;
        }

        Transform cam = cameraTransform != null ? cameraTransform : Camera.main?.transform;
        if (cam == null)
        {
            return;
        }

        Transform anchor = unit != null ? unit.transform : transform;
        float sqrDistance = (cam.position - anchor.position).sqrMagnitude;
        bool shouldShow = sqrDistance > hideDistance * hideDistance;
        if (targetCanvas.enabled != shouldShow)
        {
            targetCanvas.enabled = shouldShow;
        }
    }

    public void RefreshName()
    {
        if (unit == null)
        {
            unit = GetComponentInParent<Unit>();
        }

        if (unit != null && nameText != null)
        {
            nameText.text = unit.name;
        }
    }

    private void UpdateTeamColor()
    {
        if (unit == null || healthFillImage == null)
        {
            return;
        }

        int teamId = unit.TeamId;
        if (teamId == lastTeamId)
        {
            return;
        }

        lastTeamId = teamId;
        if (teamId == 0)
        {
            healthFillImage.color = teamOneFillColor;
        }
        else if (teamId == 1)
        {
            healthFillImage.color = teamTwoFillColor;
        }
        else
        {
            healthFillImage.color = defaultFillColor;
        }
    }
}
