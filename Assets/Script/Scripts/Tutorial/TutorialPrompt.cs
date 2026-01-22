using UnityEngine;
using UnityEngine.UI;
using TMPro; // If you use TextMeshPro

public class TutorialPrompt : MonoBehaviour
{
    [Header("--- Components ---")]
    public Image iconImage;
    public TextMeshProUGUI textLabel; // Optional: If you want text like "HOLD"

    [Header("--- Icons ---")]
    public Sprite keyboardIcon; // e.g., Mouse Left Click
    public Sprite xboxIcon;     // e.g., RT
    public Sprite psIcon;       // e.g., R2

    [Header("--- Text (Optional) ---")]
    public string keyboardText = "Key";
    public string xboxText = "Button";
    public string psText = "Button";

    void Start()
    {
        // Update immediately
        if (InputDeviceManager.Instance != null)
        {
            UpdateVisuals(InputDeviceManager.Instance.GetCurrentDevice());
            InputDeviceManager.Instance.OnInputChanged += UpdateVisuals;
        }
    }

    void OnDestroy()
    {
        if (InputDeviceManager.Instance != null)
        {
            InputDeviceManager.Instance.OnInputChanged -= UpdateVisuals;
        }
    }

    public void UpdateVisuals(DeviceType type)
    {
        Sprite targetSprite = null;
        string targetText = "";

        switch (type)
        {
            case DeviceType.Keyboard:
                targetSprite = keyboardIcon;
                targetText = keyboardText;
                break;
            case DeviceType.Xbox:
                targetSprite = xboxIcon;
                targetText = xboxText;
                break;
            case DeviceType.PlayStation:
                targetSprite = psIcon;
                targetText = psText;
                break;
        }

        if (iconImage != null && targetSprite != null) iconImage.sprite = targetSprite;
        if (textLabel != null) textLabel.text = targetText;
    }
}