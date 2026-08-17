using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Maptory.Factory
{
    public sealed class PortalTooltip : MonoBehaviour
    {
        private PortalState portal;
        private PortalEconomy economy;
        private RectTransform tooltip_rect;
        private TMP_Text label;

        public static PortalTooltip Create(
            Transform parent,
            FactoryTileCatalog catalog,
            PortalState portal,
            PortalEconomy economy,
            Vector2Int map_size)
        {
            var tooltip_object = new GameObject(
                "Portal Tooltip",
                typeof(RectTransform),
                typeof(Canvas));
            tooltip_object.transform.SetParent(parent, false);
            tooltip_object.transform.localPosition = new Vector3(0f, 1.75f, -0.1f);
            tooltip_object.transform.localScale = Vector3.one * 0.015f;
            tooltip_object.GetComponent<RectTransform>().sizeDelta = new Vector2(380f, 44f);

            var canvas = tooltip_object.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.overrideSorting = true;
            canvas.sortingLayerName = FactorySorting.ITEM_SORTING_LAYER;
            canvas.sortingOrder = FactorySorting.GetOrder(
                portal.VisualCenter,
                map_size,
                FactorySorting.ITEM_LAYER) + 10;

            var background_object = new GameObject(
                "Background",
                typeof(RectTransform),
                typeof(Image));
            background_object.transform.SetParent(tooltip_object.transform, false);
            Stretch(background_object.GetComponent<RectTransform>());
            var background = background_object.GetComponent<Image>();
            background.sprite = catalog.RoundedRectangle;
            background.type = Image.Type.Sliced;
            background.pixelsPerUnitMultiplier = 5f;
            background.color = new Color(0.02f, 0.025f, 0.02f, 0.94f);

            var text_object = new GameObject(
                "Text",
                typeof(RectTransform),
                typeof(TextMeshProUGUI));
            text_object.transform.SetParent(tooltip_object.transform, false);
            Stretch(text_object.GetComponent<RectTransform>());
            var text = text_object.GetComponent<TextMeshProUGUI>();
            text.font = catalog.UiFont;
            text.fontSize = 22f;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
            text.raycastTarget = false;
            text.overflowMode = TextOverflowModes.Overflow;

            var tooltip = tooltip_object.AddComponent<PortalTooltip>();
            tooltip.portal = portal;
            tooltip.economy = economy;
            tooltip.tooltip_rect = tooltip_object.GetComponent<RectTransform>();
            tooltip.label = text;
            tooltip.Refresh();
            return tooltip;
        }

        private void Update()
        {
            Refresh();
        }

        private void Refresh()
        {
            if (!portal.SelectedMaterial.HasValue)
            {
                label.text = "(아이템 선택)";
                FitWidth();
                return;
            }

            var material = portal.SelectedMaterial.Value;
            var items_per_minute = economy.GetItemsPerMinute(material);
            var meso_per_minute = items_per_minute * PortalEconomy.MESO_PER_ITEM;
            label.text = $"{material.ToKoreanName()} {items_per_minute:0.0}/분 | "
                + $"{meso_per_minute:0.0}메소/분";
            FitWidth();
        }

        private void FitWidth()
        {
            var width = Mathf.Clamp(label.preferredWidth + 32f, 140f, 340f);
            tooltip_rect.sizeDelta = new Vector2(width, tooltip_rect.sizeDelta.y);
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
