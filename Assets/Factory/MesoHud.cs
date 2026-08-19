using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Maptory.Factory
{
    public sealed class MesoHud : MonoBehaviour
    {
        private PortalEconomy economy;
        private TMP_Text meso_text;

        public static MesoHud Create(
            Transform parent,
            FactoryTileCatalog catalog,
            PortalEconomy economy)
        {
            var canvas_object = new GameObject(
                "Meso HUD",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            canvas_object.transform.SetParent(parent, false);

            var canvas = canvas_object.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 120;

            var scaler = canvas_object.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.matchWidthOrHeight = 1f;

            var panel = new GameObject("Meso", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(canvas_object.transform, false);
            var panel_rect = panel.GetComponent<RectTransform>();
            panel_rect.anchorMin = panel_rect.anchorMax = new Vector2(0f, 1f);
            panel_rect.pivot = new Vector2(0f, 1f);
            panel_rect.anchoredPosition = new Vector2(24f, -20f);
            panel_rect.sizeDelta = new Vector2(190f, 58f);
            var panel_image = panel.GetComponent<Image>();
            panel_image.sprite = catalog.RoundedRectangle;
            panel_image.type = Image.Type.Sliced;
            panel_image.pixelsPerUnitMultiplier = 4f;
            panel_image.color = new Color(0.035f, 0.04f, 0.06f, 0.94f);

            var text_object = new GameObject(
                "Value",
                typeof(RectTransform),
                typeof(TextMeshProUGUI));
            text_object.transform.SetParent(panel.transform, false);
            var text_rect = text_object.GetComponent<RectTransform>();
            text_rect.anchorMin = Vector2.zero;
            text_rect.anchorMax = Vector2.one;
            text_rect.offsetMin = new Vector2(18f, 6f);
            text_rect.offsetMax = new Vector2(-18f, -6f);
            var text = text_object.GetComponent<TextMeshProUGUI>();
            text.font = catalog.UiFont;
            text.fontSize = 26f;
            text.alignment = TextAlignmentOptions.Center;
            text.color = new Color(1f, 0.85f, 0.22f, 1f);
            text.raycastTarget = false;

            var hud = canvas_object.AddComponent<MesoHud>();
            hud.economy = economy;
            hud.meso_text = text;
            hud.Refresh();
            return hud;
        }

        private void Update()
        {
            Refresh();
        }

        private void Refresh()
        {
            meso_text.text = $"{economy.TotalMeso:N0} 메소";
        }
    }
}
