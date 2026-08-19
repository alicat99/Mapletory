using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Maptory.Factory
{
    public sealed class StageSelectionPanel : MonoBehaviour
    {
        private static readonly Color BACKGROUND_COLOR = new(0.035f, 0.055f, 0.04f, 1f);
        private static readonly Color PANEL_COLOR = new(0.1f, 0.13f, 0.1f, 1f);
        private static readonly Color CARD_COLOR = new(0.18f, 0.21f, 0.18f, 1f);
        private static readonly Color LOCKED_COLOR = new(0.09f, 0.1f, 0.09f, 1f);
        private static readonly Color ACCENT_COLOR = new(0.94f, 0.83f, 0.32f, 1f);
        private static readonly Color MUTED_COLOR = new(0.62f, 0.65f, 0.61f, 1f);

        private readonly List<StageRow> rows = new();
        private FactoryTileCatalog catalog;
        private FactoryProgression progression;
        private Action<string> enter_stage;
        private GameObject purchase_popup;
        private TMP_Text purchase_title;
        private TMP_Text purchase_cost;
        private TMP_Text purchase_status;
        private Button purchase_button;
        private FactoryStageDefinition pending_stage;
        private TMP_Text meso_text;

        public static StageSelectionPanel Create(
            Transform parent,
            FactoryTileCatalog catalog,
            FactoryProgression progression,
            Action<string> enter_stage)
        {
            FactoryUiEventSystem.EnsureExists();

            var canvas_object = new GameObject(
                "Stage Selection UI",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            canvas_object.transform.SetParent(parent, false);
            var canvas = canvas_object.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 300;

            var scaler = canvas_object.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.matchWidthOrHeight = 1f;

            var panel = canvas_object.AddComponent<StageSelectionPanel>();
            panel.catalog = catalog;
            panel.progression = progression;
            panel.enter_stage = enter_stage;
            panel.Build();
            return panel;
        }

        private void Update()
        {
            meso_text.text = $"{progression.Economy.TotalMeso:N0} 메소";
            if (purchase_popup.activeSelf) RefreshPurchasePopup();
        }

        private void Build()
        {
            var background = CreateObject("Background", transform);
            Stretch(background.GetComponent<RectTransform>());
            background.AddComponent<Image>().color = BACKGROUND_COLOR;

            CreateText(
                "스테이지 선택",
                background.transform,
                38f,
                TextAlignmentOptions.Center,
                new Vector2(440f, -58f),
                new Vector2(400f, 58f),
                Color.white);
            meso_text = CreateText(
                "",
                background.transform,
                24f,
                TextAlignmentOptions.MidlineRight,
                new Vector2(940f, -28f),
                new Vector2(280f, 44f),
                ACCENT_COLOR);

            var list = CreateRounded("Stage List", background.transform, PANEL_COLOR, 2f);
            SetRect(list.GetComponent<RectTransform>(), new Vector2(270f, -140f), new Vector2(740f, 470f));

            for (var index = 0; index < progression.Config.Stages.Count; index++)
            {
                var stage = progression.Config.Stages[index];
                var card = CreateRounded("Stage " + stage.Id, list.transform, CARD_COLOR, 3f);
                SetRect(card.GetComponent<RectTransform>(), new Vector2(22f, -24f - index * 132f), new Vector2(696f, 108f));
                var title = CreateText(
                    stage.DisplayName,
                    card.transform,
                    27f,
                    TextAlignmentOptions.MidlineLeft,
                    new Vector2(22f, -12f),
                    new Vector2(330f, 38f),
                    Color.white);
                var detail = CreateText(
                    "",
                    card.transform,
                    17f,
                    TextAlignmentOptions.MidlineLeft,
                    new Vector2(22f, -55f),
                    new Vector2(420f, 30f),
                    MUTED_COLOR);
                var button = CreateButton(card.transform, "입장", new Vector2(530f, -27f), new Vector2(142f, 56f));
                button.onClick.AddListener(() => SelectStage(stage));
                rows.Add(new StageRow(stage, card.GetComponent<Image>(), title, detail, button));
            }

            BuildPurchasePopup(background.transform);
            progression.Changed += RefreshRows;
            RefreshRows();
        }

        private void SelectStage(FactoryStageDefinition stage)
        {
            if (progression.IsStageUnlocked(stage.Id))
            {
                enter_stage(stage.Id);
                return;
            }

            pending_stage = stage;
            purchase_popup.SetActive(true);
            RefreshPurchasePopup();
        }

        private void BuildPurchasePopup(Transform parent)
        {
            purchase_popup = CreateObject("Stage Purchase Blocker", parent);
            Stretch(purchase_popup.GetComponent<RectTransform>());
            purchase_popup.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.62f);

            var popup = CreateRounded("Purchase Popup", purchase_popup.transform, PANEL_COLOR, 2f);
            var popup_rect = popup.GetComponent<RectTransform>();
            popup_rect.anchorMin = popup_rect.anchorMax = new Vector2(0.5f, 0.5f);
            popup_rect.pivot = new Vector2(0.5f, 0.5f);
            popup_rect.sizeDelta = new Vector2(470f, 280f);

            purchase_title = CreateText("", popup.transform, 29f, TextAlignmentOptions.Center,
                new Vector2(35f, -28f), new Vector2(400f, 46f), Color.white);
            purchase_cost = CreateText("", popup.transform, 22f, TextAlignmentOptions.Center,
                new Vector2(35f, -91f), new Vector2(400f, 42f), ACCENT_COLOR);
            purchase_status = CreateText("", popup.transform, 18f, TextAlignmentOptions.Center,
                new Vector2(35f, -139f), new Vector2(400f, 34f), MUTED_COLOR);

            var cancel = CreateButton(popup.transform, "취소", new Vector2(48f, -203f), new Vector2(170f, 52f));
            cancel.onClick.AddListener(() => purchase_popup.SetActive(false));
            purchase_button = CreateButton(popup.transform, "해금 후 입장", new Vector2(252f, -203f), new Vector2(170f, 52f));
            purchase_button.onClick.AddListener(PurchaseStage);
            purchase_popup.SetActive(false);
        }

        private void PurchaseStage()
        {
            if (!progression.TryUnlockStage(pending_stage.Id)) return;

            purchase_popup.SetActive(false);
            enter_stage(pending_stage.Id);
        }

        private void RefreshRows()
        {
            foreach (var row in rows)
            {
                var unlocked = progression.IsStageUnlocked(row.Stage.Id);
                row.Background.color = unlocked ? CARD_COLOR : LOCKED_COLOR;
                row.Title.color = unlocked ? Color.white : MUTED_COLOR;
                row.Detail.text = unlocked
                    ? $"사냥터 {row.Stage.HuntingGrounds.Count}개 · 해금됨"
                    : $"[잠금] 해금 비용 {row.Stage.UnlockMesoCost:N0} 메소";
                row.Button.GetComponentInChildren<TMP_Text>().text = unlocked ? "입장" : "해금";
            }
        }

        private void RefreshPurchasePopup()
        {
            if (pending_stage == null) return;

            var available = progression.Economy.CanSpendMeso(pending_stage.UnlockMesoCost);
            purchase_title.text = pending_stage.DisplayName + " 해금";
            purchase_cost.text = $"{pending_stage.UnlockMesoCost:N0} 메소 필요";
            purchase_cost.color = available ? ACCENT_COLOR : new Color(1f, 0.38f, 0.34f, 1f);
            purchase_status.text = available
                ? "구매하면 영구 해금되고 바로 입장합니다."
                : $"메소가 {pending_stage.UnlockMesoCost - progression.Economy.TotalMeso:N0} 부족합니다.";
            purchase_button.interactable = available;
        }

        private Button CreateButton(Transform parent, string label, Vector2 position, Vector2 size)
        {
            var game_object = CreateRounded("Button " + label, parent, CARD_COLOR, 4f);
            SetRect(game_object.GetComponent<RectTransform>(), position, size);
            var button = game_object.AddComponent<Button>();
            button.targetGraphic = game_object.GetComponent<Image>();
            var text = CreateText(label, game_object.transform, 20f, TextAlignmentOptions.Center,
                Vector2.zero, Vector2.zero, Color.white);
            Stretch(text.rectTransform);
            return button;
        }

        private GameObject CreateRounded(string name, Transform parent, Color color, float multiplier)
        {
            var game_object = CreateObject(name, parent);
            var image = game_object.AddComponent<Image>();
            image.sprite = catalog.RoundedRectangle;
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = multiplier;
            image.color = color;
            return game_object;
        }

        private TMP_Text CreateText(string value, Transform parent, float size,
            TextAlignmentOptions alignment, Vector2 position, Vector2 dimensions, Color color)
        {
            var text = CreateObject("Text", parent).AddComponent<TextMeshProUGUI>();
            text.font = catalog.UiFont;
            text.text = value;
            text.fontSize = size;
            text.color = color;
            text.alignment = alignment;
            text.raycastTarget = false;
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

        private sealed class StageRow
        {
            public FactoryStageDefinition Stage { get; }
            public Image Background { get; }
            public TMP_Text Title { get; }
            public TMP_Text Detail { get; }
            public Button Button { get; }

            public StageRow(FactoryStageDefinition stage, Image background, TMP_Text title,
                TMP_Text detail, Button button)
            {
                Stage = stage;
                Background = background;
                Title = title;
                Detail = detail;
                Button = button;
            }
        }
    }

    public static class StageReturnButton
    {
        public static void Create(
            Transform parent,
            FactoryTileCatalog catalog,
            string stage_name,
            Action return_to_selection)
        {
            var canvas_object = new GameObject(
                "Stage Navigation UI",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            canvas_object.transform.SetParent(parent, false);
            var canvas = canvas_object.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 190;
            var scaler = canvas_object.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.matchWidthOrHeight = 1f;

            var button_object = new GameObject("Return Button", typeof(RectTransform), typeof(Image));
            button_object.transform.SetParent(canvas_object.transform, false);
            var rect = button_object.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-22f, -22f);
            rect.sizeDelta = new Vector2(178f, 48f);
            var image = button_object.GetComponent<Image>();
            image.sprite = catalog.RoundedRectangle;
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = 4f;
            image.color = new Color(0.12f, 0.14f, 0.12f, 0.96f);
            var button = button_object.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(() => return_to_selection());

            var text = new GameObject("Text", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            text.transform.SetParent(button_object.transform, false);
            text.font = catalog.UiFont;
            text.text = $"{stage_name}  돌아가기";
            text.fontSize = 18f;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
            Stretch(text.rectTransform);
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }
    }
}
