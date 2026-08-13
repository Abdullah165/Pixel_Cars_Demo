using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>Creates the compact in-game fare display and brake button.</summary>
public class TaxiGameUI : MonoBehaviour
{
    private PlayerTaxiController playerTaxi;
    private FareManager fareManager;
    private RectTransform brakeButtonRect;
    private Text brakeLabel;
    private Text fareLabel;
    private bool brakeHeld;

    private void Start()
    {
        CreateUI();
    }

    private void Update()
    {
        if (playerTaxi == null)
        {
            playerTaxi = FindFirstObjectByType<PlayerTaxiController>();
        }

        if (fareManager == null)
        {
            fareManager = FindFirstObjectByType<FareManager>();
        }

        CheckBrakeButtonHold();

        if (fareManager != null && fareLabel != null)
        {
            fareLabel.text = $"{fareManager.StatusText}\nFARES: {fareManager.FaresCompleted}\nSPEED: {Mathf.RoundToInt(GameSpeedController.NormalizedTaxiSpeed * 100f)}%";
        }
    }

    private void CheckBrakeButtonHold()
    {
        if (Touchscreen.current != null)
        {
            var touch = Touchscreen.current.primaryTouch;
            if (touch.press.wasPressedThisFrame && RectTransformUtility.RectangleContainsScreenPoint(brakeButtonRect, touch.position.ReadValue()))
            {
                SetBrakeHeld(true);
            }

            if (brakeHeld && touch.press.wasReleasedThisFrame)
            {
                SetBrakeHeld(false);
            }

            return;
        }

        if (Mouse.current == null)
        {
            return;
        }

        if (Mouse.current.leftButton.wasPressedThisFrame && RectTransformUtility.RectangleContainsScreenPoint(brakeButtonRect, Mouse.current.position.ReadValue()))
        {
            SetBrakeHeld(true);
        }

        if (brakeHeld && Mouse.current.leftButton.wasReleasedThisFrame)
        {
            SetBrakeHeld(false);
        }
    }

    private void SetBrakeHeld(bool value)
    {
        brakeHeld = value;
        if (playerTaxi != null)
        {
            playerTaxi.SetBrakeInput(brakeHeld);
        }

        brakeLabel.text = brakeHeld ? "BRAKE\nHOLD" : "BRAKE";
    }

    private void CreateUI()
    {
        GameObject canvasObject = new GameObject("Taxi HUD", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);

        fareLabel = CreateText(canvasObject.transform, "Fare Status", new Vector2(24f, -24f), new Vector2(520f, 150f), TextAnchor.UpperLeft, 28);
        RectTransform fareRect = fareLabel.GetComponent<RectTransform>();
        fareRect.anchorMin = new Vector2(0f, 1f);
        fareRect.anchorMax = new Vector2(0f, 1f);
        fareRect.pivot = new Vector2(0f, 1f);

        GameObject brakeButton = new GameObject("Brake Button", typeof(RectTransform), typeof(Image), typeof(Button));
        brakeButton.transform.SetParent(canvasObject.transform, false);
        brakeButtonRect = brakeButton.GetComponent<RectTransform>();
        brakeButtonRect.anchorMin = new Vector2(1f, 0f);
        brakeButtonRect.anchorMax = new Vector2(1f, 0f);
        brakeButtonRect.pivot = new Vector2(1f, 0f);
        brakeButtonRect.anchoredPosition = new Vector2(-34f, 34f);
        brakeButtonRect.sizeDelta = new Vector2(210f, 130f);
        brakeButton.GetComponent<Image>().color = new Color(0.9f, 0.2f, 0.12f, 0.92f);
        brakeLabel = CreateText(brakeButton.transform, "Label", Vector2.zero, brakeButtonRect.sizeDelta, TextAnchor.MiddleCenter, 32);
        RectTransform labelRect = brakeLabel.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.pivot = new Vector2(0.5f, 0.5f);
        labelRect.anchoredPosition = Vector2.zero;
        labelRect.sizeDelta = Vector2.zero;
        brakeLabel.text = "BRAKE";
    }

    private Text CreateText(Transform parent, string objectName, Vector2 anchoredPosition, Vector2 size, TextAnchor alignment, int fontSize)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(parent, false);
        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.zero;
        rect.pivot = Vector2.zero;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        Text text = textObject.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = Color.white;
        return text;
    }
}
