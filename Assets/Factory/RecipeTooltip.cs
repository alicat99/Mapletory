using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Maptory.Factory
{
    public static class RecipeTooltip
    {
        public static GameObject Create(
            Transform parent,
            FactoryTileCatalog catalog,
            Vector2Int center,
            Vector2Int map_size)
        {
            var tooltip_object = new GameObject(
                "Recipe Tooltip",
                typeof(RectTransform),
                typeof(Canvas));
            tooltip_object.transform.SetParent(parent, false);
            tooltip_object.transform.localPosition = new Vector3(0f, 2.05f, -0.1f);
            tooltip_object.transform.localScale = Vector3.one * 0.015f;
            tooltip_object.GetComponent<RectTransform>().sizeDelta = new Vector2(240f, 42f);

            var canvas = tooltip_object.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.overrideSorting = true;
            canvas.sortingLayerName = FactorySorting.ITEM_SORTING_LAYER;
            canvas.sortingOrder = FactorySorting.GetOrder(
                center,
                map_size,
                FactorySorting.ITEM_LAYER) + 10;

            var background_object = new GameObject("Background", typeof(RectTransform), typeof(Image));
            background_object.transform.SetParent(tooltip_object.transform, false);
            Stretch(background_object.GetComponent<RectTransform>());
            var background = background_object.GetComponent<Image>();
            background.sprite = catalog.RoundedRectangle;
            background.type = Image.Type.Sliced;
            background.pixelsPerUnitMultiplier = 5f;
            background.color = new Color(0.02f, 0.025f, 0.02f, 0.94f);

            var text_object = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            text_object.transform.SetParent(tooltip_object.transform, false);
            Stretch(text_object.GetComponent<RectTransform>());
            var tooltip = text_object.GetComponent<TextMeshProUGUI>();
            tooltip.font = catalog.UiFont;
            tooltip.text = "(레시피 선택)";
            tooltip.fontSize = 24f;
            tooltip.alignment = TextAlignmentOptions.Center;
            tooltip.color = Color.white;
            tooltip.raycastTarget = false;
            tooltip.overflowMode = TextOverflowModes.Overflow;
            return tooltip_object;
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
