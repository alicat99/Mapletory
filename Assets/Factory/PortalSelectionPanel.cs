using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Maptory.Factory
{
    public sealed class PortalSelectionPanel : MonoBehaviour
    {
        private static readonly Color PANEL_COLOR = new(0.14f, 0.16f, 0.23f, 1f);
        private static readonly Color ROW_COLOR = new(0.86f, 0.89f, 0.94f, 1f);
        private static readonly Color ROW_TEXT_COLOR = new(0.08f, 0.1f, 0.16f, 1f);
        private static readonly Color CLOSE_COLOR = new(0.57f, 0.18f, 0.2f, 1f);

        private FactoryTileCatalog catalog;
        private PortalState portal;
        private GameObject blocker;

        public bool IsOpen => blocker != null && blocker.activeSelf;

        public static PortalSelectionPanel Create(
            Transform parent,
            FactoryTileCatalog catalog)
        {
            var canvas_object = new GameObject(
                "Portal Selection UI",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            canvas_object.transform.SetParent(parent, false);

            var canvas = canvas_object.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 210;

            var scaler = canvas_object.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.matchWidthOrHeight = 1f;

            var panel = canvas_object.AddComponent<PortalSelectionPanel>();
            panel.catalog = catalog;
            panel.Build();
            return panel;
        }

        public void Show(PortalState target)
        {
            portal = target;
            blocker.SetActive(true);
        }

        private void Build()
        {
            blocker = CreateObject("Blocker", transform);
            Stretch(blocker.GetComponent<RectTransform>());
            blocker.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.58f);

            var panel = CreateObject("Panel", blocker.transform);
            var panel_rect = panel.GetComponent<RectTransform>();
            panel_rect.anchorMin = panel_rect.anchorMax = new Vector2(0.5f, 0.5f);
            panel_rect.pivot = new Vector2(0.5f, 0.5f);
            panel_rect.anchoredPosition = Vector2.zero;
            panel_rect.sizeDelta = new Vector2(
                504f,
                86f + PortalSupplyCatalog.Options.Count * 42f);
            panel.AddComponent<Image>().color = PANEL_COLOR;

            CreateText(
                "공급 사냥터 / 몬스터 선택",
                panel.transform,
                25f,
                TextAlignmentOptions.Center,
                new Vector2(72f, -18f),
                new Vector2(360f, 40f),
                Color.white);

            var close = CreateButton(
                "Close",
                panel.transform,
                "X",
                23f,
                CLOSE_COLOR,
                Color.white);
            SetRect(close.GetComponent<RectTransform>(), new Vector2(456f, -19f), new Vector2(36f, 36f));
            close.onClick.AddListener(Close);

            for (var index = 0; index < PortalSupplyCatalog.Options.Count; index++)
            {
                var option = PortalSupplyCatalog.Options[index];
                var row = CreateButton(
                    option.Material.ToString(),
                    panel.transform,
                    option.SelectionLabel,
                    18f,
                    ROW_COLOR,
                    ROW_TEXT_COLOR);
                SetRect(
                    row.GetComponent<RectTransform>(),
                    new Vector2(12f, -77f - index * 42f),
                    new Vector2(480f, 36f));
                row.onClick.AddListener(() => Select(option));
            }

            blocker.SetActive(false);
        }

        private void Select(PortalSupplyOption option)
        {
            portal.SelectMaterial(option.Material);
            Close();
        }

        private void Close()
        {
            blocker.SetActive(false);
        }

        private Button CreateButton(
            string name,
            Transform parent,
            string label,
            float font_size,
            Color background_color,
            Color text_color)
        {
            var game_object = CreateObject(name, parent);
            var image = game_object.AddComponent<Image>();
            image.color = background_color;
            var button = game_object.AddComponent<Button>();
            button.targetGraphic = image;
            var text = CreateText(
                label,
                game_object.transform,
                font_size,
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
            text.overflowMode = TextOverflowModes.Overflow;
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
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }
    }
}
