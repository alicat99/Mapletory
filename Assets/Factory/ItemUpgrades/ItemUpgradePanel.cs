using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Maptory.Factory
{
    public sealed class ItemUpgradePanel : MonoBehaviour
    {
        private static readonly Color PANEL_COLOR = new(0.035f, 0.045f, 0.035f, 0.99f);
        private static readonly Color LIST_COLOR = new(0.018f, 0.024f, 0.018f, 0.99f);
        private static readonly Color CARD_COLOR = new(0.16f, 0.18f, 0.16f, 1f);
        private static readonly Color DISABLED_COLOR = new(0.09f, 0.1f, 0.09f, 1f);
        private static readonly Color ACCENT_COLOR = new(0.94f, 0.83f, 0.32f, 1f);

        private readonly List<ItemUpgradeRow> rows = new();

        private FactoryTileCatalog catalog;
        private PortalEconomy economy;
        private Func<bool> is_other_modal_open;
        private GameObject blocker;
        private TMP_Text resource_summary;
        private TabView meso_tab;
        private TabView production_tab;

        public bool IsOpen => blocker != null && blocker.activeSelf;
        public int RowCount => rows.Count;
        public ItemUpgradeCategory SelectedCategory { get; private set; }
        public event Action Opened;

        public static ItemUpgradePanel Create(
            Transform parent,
            FactoryTileCatalog catalog,
            PortalEconomy economy)
        {
            var canvas_object = new GameObject(
                "Item Upgrade UI",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            canvas_object.transform.SetParent(parent, false);

            var canvas = canvas_object.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 220;

            var scaler = canvas_object.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.matchWidthOrHeight = 1f;

            var panel = canvas_object.AddComponent<ItemUpgradePanel>();
            panel.catalog = catalog;
            panel.economy = economy;
            panel.Build();
            return panel;
        }

        public void SetOtherModalCheck(Func<bool> check)
        {
            is_other_modal_open = check;
        }

        public void Open()
        {
            if (is_other_modal_open != null && is_other_modal_open()) return;

            blocker.SetActive(true);
            Refresh();
            Opened?.Invoke();
        }

        public void Close()
        {
            blocker.SetActive(false);
        }

        public void Toggle()
        {
            if (IsOpen)
            {
                Close();
                return;
            }

            Open();
        }

        public void SelectCategory(ItemUpgradeCategory category)
        {
            SelectedCategory = category;
            RefreshTabs();
            Refresh();
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.uKey.wasPressedThisFrame)
            {
                Toggle();
            }

            if (!IsOpen) return;

            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                Close();
                return;
            }

            Refresh();
        }

        private void Build()
        {
            blocker = CreateObject("Blocker", transform);
            Stretch(blocker.GetComponent<RectTransform>());

            var panel = CreateRounded("Panel", blocker.transform, PANEL_COLOR, 2f);
            var panel_rect = panel.GetComponent<RectTransform>();
            panel_rect.anchorMin = panel_rect.anchorMax = new Vector2(1f, 0.5f);
            panel_rect.pivot = new Vector2(1f, 0.5f);
            panel_rect.anchoredPosition = new Vector2(-24f, 0f);
            panel_rect.sizeDelta = new Vector2(620f, 570f);

            CreateText(
                "아이템 업그레이드",
                panel.transform,
                30f,
                TextAlignmentOptions.MidlineLeft,
                new Vector2(22f, -10f),
                new Vector2(290f, 44f),
                new Color(0.92f, 0.92f, 0.89f, 1f));
            resource_summary = CreateText(
                "",
                panel.transform,
                19f,
                TextAlignmentOptions.MidlineRight,
                new Vector2(322f, -12f),
                new Vector2(210f, 40f),
                new Color(1f, 0.85f, 0.22f, 1f));

            var close = CreateButton(
                "Close",
                panel.transform,
                "X",
                24f,
                Color.clear,
                new Color(0.9f, 0.9f, 0.87f, 1f));
            SetRect(
                close.GetComponent<RectTransform>(),
                new Vector2(558f, -8f),
                new Vector2(42f, 42f));
            close.onClick.AddListener(Close);

            var list_frame = CreateRounded("List Frame", panel.transform, LIST_COLOR, 3f);
            SetRect(
                list_frame.GetComponent<RectTransform>(),
                new Vector2(20f, -96f),
                new Vector2(580f, 454f));
            BuildScrollView(list_frame.transform);

            meso_tab = CreateTab(panel.transform, "메소", 20f, ItemUpgradeCategory.Meso);
            production_tab = CreateTab(
                panel.transform,
                "누적 생산량",
                174f,
                ItemUpgradeCategory.Production);

            SelectedCategory = ItemUpgradeCategory.Meso;
            RefreshTabs();
            blocker.SetActive(false);
        }

        private void BuildScrollView(Transform parent)
        {
            var viewport = CreateObject("Viewport", parent);
            SetRect(
                viewport.GetComponent<RectTransform>(),
                new Vector2(12f, -12f),
                new Vector2(556f, 430f));
            viewport.AddComponent<RectMask2D>();

            var content = CreateObject("Content", viewport.transform);
            var content_rect = content.GetComponent<RectTransform>();
            content_rect.anchorMin = new Vector2(0f, 1f);
            content_rect.anchorMax = new Vector2(1f, 1f);
            content_rect.pivot = new Vector2(0.5f, 1f);
            content_rect.anchoredPosition = Vector2.zero;
            content_rect.sizeDelta = Vector2.zero;

            var layout = content.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 6f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var fitter = content.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scroll = parent.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport.GetComponent<RectTransform>();
            scroll.content = content_rect;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 28f;

            foreach (var option in PortalSupplyCatalog.Options)
            {
                var row = ItemUpgradeRow.Create(content.transform, catalog, Purchase);
                row.SetIcon(catalog.GetItemSprite(option.Material));
                rows.Add(row);
            }
        }

        private TabView CreateTab(
            Transform parent,
            string label,
            float x,
            ItemUpgradeCategory category)
        {
            var tab_object = CreateRounded("Tab " + label, parent, CARD_COLOR, 4f);
            SetRect(
                tab_object.GetComponent<RectTransform>(),
                new Vector2(x, -60f),
                new Vector2(150f, 46f));
            var button = tab_object.AddComponent<Button>();
            button.targetGraphic = tab_object.GetComponent<Image>();

            var inner = CreateRounded("Inner", tab_object.transform, DISABLED_COLOR, 4f);
            var inner_rect = inner.GetComponent<RectTransform>();
            Stretch(inner_rect);
            inner_rect.offsetMin = new Vector2(2f, 2f);
            inner_rect.offsetMax = new Vector2(-2f, -2f);

            var text = CreateText(
                label,
                inner.transform,
                20f,
                TextAlignmentOptions.Center,
                Vector2.zero,
                Vector2.zero,
                new Color(0.75f, 0.76f, 0.72f, 1f));
            Stretch(text.rectTransform);

            var bridge = CreateObject("Bridge", tab_object.transform);
            SetRect(
                bridge.GetComponent<RectTransform>(),
                new Vector2(2f, -38f),
                new Vector2(146f, 12f));
            bridge.AddComponent<Image>().color = LIST_COLOR;

            var view = new TabView(
                tab_object.GetComponent<RectTransform>(),
                tab_object.GetComponent<Image>(),
                inner.GetComponent<Image>(),
                text,
                bridge);
            button.onClick.AddListener(() => SelectCategory(category));
            return view;
        }

        private void Purchase(RawMaterialType material)
        {
            if (SelectedCategory == ItemUpgradeCategory.Meso)
            {
                economy.TryPurchaseMesoUpgrade(material);
            }
            else
            {
                economy.TryPurchaseProductionUpgrade(material);
            }

            Refresh();
        }

        private void Refresh()
        {
            for (var index = 0; index < rows.Count; index++)
            {
                var option = PortalSupplyCatalog.Options[index];
                rows[index].Bind(option, SelectedCategory, economy);
            }

            resource_summary.text = SelectedCategory == ItemUpgradeCategory.Meso
                ? $"보유 {economy.TotalMeso:N0} 메소"
                : $"강화 가능 {economy.CountAvailableProductionUpgrades()}종";
        }

        private void RefreshTabs()
        {
            RefreshTab(meso_tab, SelectedCategory == ItemUpgradeCategory.Meso);
            RefreshTab(
                production_tab,
                SelectedCategory == ItemUpgradeCategory.Production);
        }

        private static void RefreshTab(TabView tab, bool selected)
        {
            tab.Rect.anchoredPosition = new Vector2(
                tab.Rect.anchoredPosition.x,
                selected ? -60f : -66f);
            tab.Rect.sizeDelta = new Vector2(150f, selected ? 46f : 40f);
            tab.Border.color = selected ? ACCENT_COLOR : CARD_COLOR;
            tab.Background.color = selected ? LIST_COLOR : DISABLED_COLOR;
            tab.Label.color = selected
                ? new Color(0.95f, 0.95f, 0.91f, 1f)
                : new Color(0.58f, 0.6f, 0.56f, 1f);
            tab.Bridge.SetActive(selected);
            if (selected) tab.Rect.SetAsLastSibling();
        }

        private GameObject CreateRounded(
            string name,
            Transform parent,
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

        private Button CreateButton(
            string name,
            Transform parent,
            string label,
            float size,
            Color background_color,
            Color text_color)
        {
            var game_object = CreateRounded(name, parent, background_color, 4f);
            var button = game_object.AddComponent<Button>();
            button.targetGraphic = game_object.GetComponent<Image>();
            var text = CreateText(
                label,
                game_object.transform,
                size,
                TextAlignmentOptions.Center,
                Vector2.zero,
                Vector2.zero,
                text_color);
            Stretch(text.rectTransform);
            return button;
        }

        private TMP_Text CreateText(
            string value,
            Transform parent,
            float size,
            TextAlignmentOptions alignment,
            Vector2 position,
            Vector2 dimensions,
            Color color)
        {
            var text = CreateObject("Text", parent).AddComponent<TextMeshProUGUI>();
            text.font = catalog.UiFont;
            text.text = value;
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

        private sealed class TabView
        {
            public RectTransform Rect { get; }
            public Image Border { get; }
            public Image Background { get; }
            public TMP_Text Label { get; }
            public GameObject Bridge { get; }

            public TabView(
                RectTransform rect,
                Image border,
                Image background,
                TMP_Text label,
                GameObject bridge)
            {
                Rect = rect;
                Border = border;
                Background = background;
                Label = label;
                Bridge = bridge;
            }
        }
    }
}
