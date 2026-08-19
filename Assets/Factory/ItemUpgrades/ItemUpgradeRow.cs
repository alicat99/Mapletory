using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Maptory.Factory
{
    public enum ItemUpgradeCategory
    {
        Meso,
        Production
    }

    public sealed class ItemUpgradeRow : MonoBehaviour
    {
        public const float HEIGHT = 68f;

        private static readonly Color CARD_COLOR = new(0.16f, 0.18f, 0.16f, 1f);
        private static readonly Color AVAILABLE_COLOR = new(0.94f, 0.83f, 0.32f, 1f);
        private static readonly Color DISABLED_COLOR = new(0.12f, 0.13f, 0.12f, 1f);
        private static readonly Color MUTED_TEXT_COLOR = new(0.58f, 0.6f, 0.56f, 1f);

        private PortalSupplyOption option;
        private RawMaterialType material;
        private Action<RawMaterialType> upgrade_requested;
        private Image border;
        private Image icon;
        private TMP_Text name_label;
        private TMP_Text detail_label;
        private TMP_Text cost_label;
        private Button upgrade_button;
        private Image upgrade_background;
        private TMP_Text upgrade_label;

        public static ItemUpgradeRow Create(
            Transform parent,
            FactoryTileCatalog catalog,
            Action<RawMaterialType> on_upgrade)
        {
            var row_object = new GameObject(
                "Item Upgrade Row",
                typeof(RectTransform),
                typeof(LayoutElement),
                typeof(Image));
            row_object.transform.SetParent(parent, false);
            var layout = row_object.GetComponent<LayoutElement>();
            layout.minHeight = HEIGHT;
            layout.preferredHeight = HEIGHT;
            layout.flexibleHeight = 0f;

            var row = row_object.AddComponent<ItemUpgradeRow>();
            row.upgrade_requested = on_upgrade;
            row.border = row_object.GetComponent<Image>();
            row.border.sprite = catalog.RoundedRectangle;
            row.border.type = Image.Type.Sliced;
            row.border.pixelsPerUnitMultiplier = 5f;

            var inner = CreateRounded("Card", row_object.transform, catalog, CARD_COLOR, 5f);
            var inner_rect = inner.GetComponent<RectTransform>();
            Stretch(inner_rect);
            inner_rect.offsetMin = new Vector2(2f, 2f);
            inner_rect.offsetMax = new Vector2(-2f, -2f);

            row.icon = CreateObject("Icon", inner.transform).AddComponent<Image>();
            row.icon.preserveAspect = true;
            row.icon.raycastTarget = false;
            SetRect(row.icon.rectTransform, new Vector2(10f, -7f), new Vector2(54f, 54f));

            row.name_label = CreateText(
                "Name",
                inner.transform,
                catalog,
                21f,
                TextAlignmentOptions.MidlineLeft,
                new Vector2(76f, -7f),
                new Vector2(326f, 28f),
                new Color(0.94f, 0.94f, 0.91f, 1f));
            row.detail_label = CreateText(
                "Details",
                inner.transform,
                catalog,
                16f,
                TextAlignmentOptions.MidlineLeft,
                new Vector2(76f, -34f),
                new Vector2(326f, 24f),
                new Color(0.7f, 0.72f, 0.68f, 1f));
            row.cost_label = CreateText(
                "Cost",
                inner.transform,
                catalog,
                17f,
                TextAlignmentOptions.MidlineRight,
                new Vector2(408f, -8f),
                new Vector2(190f, 50f),
                new Color(0.9f, 0.9f, 0.86f, 1f));

            var button_object = CreateRounded(
                "Upgrade",
                inner.transform,
                catalog,
                DISABLED_COLOR,
                5f);
            SetRect(
                button_object.GetComponent<RectTransform>(),
                new Vector2(612f, -10f),
                new Vector2(120f, 44f));
            row.upgrade_background = button_object.GetComponent<Image>();
            row.upgrade_button = button_object.AddComponent<Button>();
            row.upgrade_button.targetGraphic = row.upgrade_background;
            row.upgrade_label = CreateText(
                "Label",
                button_object.transform,
                catalog,
                19f,
                TextAlignmentOptions.Center,
                Vector2.zero,
                Vector2.zero,
                MUTED_TEXT_COLOR);
            Stretch(row.upgrade_label.rectTransform);
            row.upgrade_button.onClick.AddListener(row.Purchase);
            return row;
        }

        public void Bind(
            PortalSupplyOption supply_option,
            ItemUpgradeCategory category,
            PortalEconomy economy)
        {
            option = supply_option;
            material = option.Material;
            name_label.text = option.ItemLabel;

            var level = category == ItemUpgradeCategory.Meso
                ? economy.GetMesoUpgradeLevel(material)
                : economy.GetProductionUpgradeLevel(material);
            var maximum = level >= PortalEconomy.MAX_UPGRADE_LEVEL;
            var available = category == ItemUpgradeCategory.Meso
                ? economy.CanPurchaseMesoUpgrade(material)
                : economy.CanPurchaseProductionUpgrade(material);

            if (category == ItemUpgradeCategory.Meso)
            {
                detail_label.text = $"Lv.{level} · 합연산 보너스 +{economy.GetMesoBonus(material):0.##}메소";
                cost_label.text = maximum
                    ? "최대 레벨"
                    : $"{economy.GetMesoUpgradeCost(material):N0} 메소";
            }
            else
            {
                detail_label.text = $"Lv.{level} · 개체 가치 ×{economy.GetProductionMultiplier(material):0.##}";
                cost_label.text = maximum
                    ? "최대 레벨"
                    : $"{economy.GetAvailableProduction(material):N0} / "
                        + $"{economy.GetProductionUpgradeCost(material):N0}";
            }

            border.color = available ? AVAILABLE_COLOR : CARD_COLOR;
            upgrade_button.interactable = available;
            upgrade_background.color = available ? AVAILABLE_COLOR : DISABLED_COLOR;
            upgrade_label.color = available
                ? new Color(0.1f, 0.11f, 0.09f, 1f)
                : MUTED_TEXT_COLOR;
            upgrade_label.text = maximum ? "완료" : available ? "강화" : "부족";
        }

        public void SetIcon(Sprite sprite)
        {
            icon.sprite = sprite;
        }

        private void Purchase()
        {
            upgrade_requested(material);
        }

        private static GameObject CreateRounded(
            string name,
            Transform parent,
            FactoryTileCatalog catalog,
            Color color,
            float multiplier)
        {
            var game_object = CreateObject(name, parent);
            var image = game_object.AddComponent<Image>();
            image.sprite = catalog.RoundedRectangle;
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = multiplier;
            image.color = color;
            return game_object;
        }

        private static TMP_Text CreateText(
            string name,
            Transform parent,
            FactoryTileCatalog catalog,
            float size,
            TextAlignmentOptions alignment,
            Vector2 position,
            Vector2 dimensions,
            Color color)
        {
            var text = CreateObject(name, parent).AddComponent<TextMeshProUGUI>();
            text.font = catalog.UiFont;
            text.fontSize = size;
            text.color = color;
            text.alignment = alignment;
            text.raycastTarget = false;
            text.overflowMode = TextOverflowModes.Ellipsis;
            SetRect(text.rectTransform, position, dimensions);
            return text;
        }

        private static GameObject CreateObject(string name, Transform parent)
        {
            var game_object = new GameObject(name, typeof(RectTransform));
            game_object.transform.SetParent(parent, false);
            return game_object;
        }

        private static void SetRect(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
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
