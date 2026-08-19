using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Maptory.Factory
{
    public sealed class ItemUpgradeShortcut : MonoBehaviour
    {
        private PortalEconomy economy;
        private ItemUpgradePanel panel;
        private GameObject badge;
        private TMP_Text badge_text;

        public static ItemUpgradeShortcut Create(
            Transform parent,
            FactoryTileCatalog catalog,
            PortalEconomy economy,
            ItemUpgradePanel panel)
        {
            var canvas_object = new GameObject(
                "Item Upgrade Shortcut",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            canvas_object.transform.SetParent(parent, false);

            var canvas = canvas_object.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 125;

            var scaler = canvas_object.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.matchWidthOrHeight = 1f;

            var shortcut = canvas_object.AddComponent<ItemUpgradeShortcut>();
            shortcut.economy = economy;
            shortcut.panel = panel;
            shortcut.Build(catalog);
            shortcut.Refresh();
            return shortcut;
        }

        private void Update()
        {
            Refresh();
        }

        private void Build(FactoryTileCatalog catalog)
        {
            var button_object = new GameObject(
                "Open Item Upgrades",
                typeof(RectTransform),
                typeof(Image),
                typeof(Button));
            button_object.transform.SetParent(transform, false);
            var rect = button_object.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(24f, -90f);
            rect.sizeDelta = new Vector2(190f, 54f);

            var image = button_object.GetComponent<Image>();
            image.sprite = catalog.RoundedRectangle;
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = 4f;
            image.color = new Color(0.035f, 0.04f, 0.06f, 0.94f);

            var button = button_object.GetComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(panel.Toggle);

            var label_object = new GameObject(
                "Label",
                typeof(RectTransform),
                typeof(TextMeshProUGUI));
            label_object.transform.SetParent(button_object.transform, false);
            var label_rect = label_object.GetComponent<RectTransform>();
            label_rect.anchorMin = Vector2.zero;
            label_rect.anchorMax = Vector2.one;
            label_rect.offsetMin = new Vector2(14f, 6f);
            label_rect.offsetMax = new Vector2(-14f, -6f);
            var label = label_object.GetComponent<TextMeshProUGUI>();
            label.font = catalog.UiFont;
            label.text = "아이템 업그레이드  [U]";
            label.fontSize = 18f;
            label.alignment = TextAlignmentOptions.Center;
            label.color = new Color(0.9f, 0.9f, 0.87f, 1f);
            label.raycastTarget = false;

            badge = new GameObject(
                "Available Badge",
                typeof(RectTransform),
                typeof(Image));
            badge.transform.SetParent(button_object.transform, false);
            var badge_rect = badge.GetComponent<RectTransform>();
            badge_rect.anchorMin = badge_rect.anchorMax = new Vector2(1f, 1f);
            badge_rect.pivot = new Vector2(0.5f, 0.5f);
            badge_rect.anchoredPosition = new Vector2(-2f, -2f);
            badge_rect.sizeDelta = new Vector2(30f, 30f);
            var badge_image = badge.GetComponent<Image>();
            badge_image.sprite = catalog.RoundedRectangle;
            badge_image.type = Image.Type.Sliced;
            badge_image.pixelsPerUnitMultiplier = 6f;
            badge_image.color = new Color(0.94f, 0.83f, 0.32f, 1f);

            var badge_label = new GameObject(
                "Count",
                typeof(RectTransform),
                typeof(TextMeshProUGUI));
            badge_label.transform.SetParent(badge.transform, false);
            var badge_label_rect = badge_label.GetComponent<RectTransform>();
            badge_label_rect.anchorMin = Vector2.zero;
            badge_label_rect.anchorMax = Vector2.one;
            badge_label_rect.offsetMin = Vector2.zero;
            badge_label_rect.offsetMax = Vector2.zero;
            badge_text = badge_label.GetComponent<TextMeshProUGUI>();
            badge_text.font = catalog.UiFont;
            badge_text.fontSize = 17f;
            badge_text.alignment = TextAlignmentOptions.Center;
            badge_text.color = new Color(0.1f, 0.11f, 0.09f, 1f);
            badge_text.raycastTarget = false;
        }

        private void Refresh()
        {
            var count = economy.CountAvailableProductionUpgrades();
            badge.SetActive(count > 0);
            badge_text.text = count.ToString();
        }
    }
}
