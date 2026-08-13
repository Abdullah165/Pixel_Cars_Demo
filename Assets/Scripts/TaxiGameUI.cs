using UnityEngine;
using UnityEngine.UI;

/// <summary>Creates the compact in-game fare display.</summary>
public class TaxiGameUI : MonoBehaviour
{
    private FareManager fareManager;
    private Text fareLabel;

    private void Start()
    {
        CreateUI();
    }

    private void Update()
    {

        if (fareManager == null)
        {
            fareManager = FindFirstObjectByType<FareManager>();
        }


        if (fareManager != null && fareLabel != null)
        {
            fareLabel.text = $"{fareManager.StatusText}\nFARES: {fareManager.FaresCompleted}\nSPEED: {Mathf.RoundToInt(GameSpeedController.NormalizedTaxiSpeed * 100f)}%";
        }
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

        fareLabel = CreateText(canvasObject.transform, "Fare Status", new Vector2(24f, -24f), new Vector2(420f, 100f), TextAnchor.UpperLeft, 18);
        RectTransform fareRect = fareLabel.GetComponent<RectTransform>();
        fareRect.anchorMin = new Vector2(0f, 1f);
        fareRect.anchorMax = new Vector2(0f, 1f);
        fareRect.pivot = new Vector2(0f, 1f);

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
