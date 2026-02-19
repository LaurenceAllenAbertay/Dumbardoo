using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UnitWorldUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Unit unit;
    [SerializeField] private Slider healthSlider;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private Canvas targetCanvas;
    [SerializeField] private bool hideWhenCameraClose = true;
    [SerializeField] private float hideDistance = 1.5f;

    private int lastHealth = -1;
    private int lastMaxHealth = -1;

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
        UpdateHealth();
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
}
