using System;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Maptory.Factory
{
    public sealed class FactoryDebugPanel : MonoBehaviour
    {
        private static readonly Color PANEL_COLOR = new(0.035f, 0.045f, 0.035f, 0.98f);
        private static readonly Color CARD_COLOR = new(0.15f, 0.17f, 0.15f, 1f);
        private static readonly Color FIELD_COLOR = new(0.08f, 0.095f, 0.08f, 1f);
        private static readonly Color ACCENT_COLOR = new(0.94f, 0.83f, 0.32f, 1f);
        private static readonly Color TEXT_COLOR = new(0.92f, 0.92f, 0.89f, 1f);
        private static readonly Color MUTED_COLOR = new(0.66f, 0.68f, 0.64f, 1f);

        private readonly List<GameObject> pages = new();
        private readonly List<Image> tab_backgrounds = new();
        private readonly List<TMP_Text> tab_labels = new();

        private FactoryTileCatalog catalog;
        private PortalEconomy economy;
        private FactoryDebugMapEditor map_editor;
        private FactoryProgression progression;
        private FactorySaveService save_service;
        private GameObject panel_root;
        private TMP_Text brush_label;
        private TMP_Text monster_name;
        private TMP_Text monster_summary;
        private NumericField base_value_field;
        private NumericField meso_bonus_field;
        private NumericField multiplier_field;
        private NumericField meso_level_field;
        private NumericField production_level_field;
        private NumericField available_production_field;
        private NumericField meso_cost_field;
        private NumericField production_cost_field;
        private NumericField meso_coefficient_field;
        private NumericField production_coefficient_field;
        private TMP_Text upgrade_monster_name;
        private TMP_Text stage_name;
        private TMP_Text hunting_ground_name;
        private TMP_Text unlock_summary;
        private NumericField stage_unlock_cost_field;
        private NumericField hunting_unlock_cost_field;
        private int selected_page;
        private int selected_monster;
        private int selected_stage;
        private int selected_hunting_ground;

        public bool IsOpen => panel_root != null && panel_root.activeSelf;

        public static FactoryDebugPanel Create(
            Transform parent,
            FactoryTileCatalog catalog,
            PortalEconomy economy,
            FactoryDebugMapEditor map_editor,
            FactoryProgression progression,
            FactorySaveService save_service)
        {
            var canvas_object = new GameObject(
                "Factory Debug UI",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            canvas_object.transform.SetParent(parent, false);

            var canvas = canvas_object.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 240;

            var scaler = canvas_object.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.matchWidthOrHeight = 1f;

            var panel = canvas_object.AddComponent<FactoryDebugPanel>();
            panel.catalog = catalog;
            panel.economy = economy;
            panel.map_editor = map_editor;
            panel.progression = progression;
            panel.save_service = save_service;
            panel.Build();
            return panel;
        }

        public void Open()
        {
            panel_root.SetActive(true);
            SetPage(selected_page);
        }

        public void Close()
        {
            panel_root.SetActive(false);
            map_editor.SetInputEnabled(false);
        }

        public void Toggle()
        {
            if (IsOpen) Close();
            else Open();
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.f3Key.wasPressedThisFrame)
            {
                SaveSettingsAndRestart();
                return;
            }

            if (Keyboard.current != null && Keyboard.current.f2Key.wasPressedThisFrame)
            {
                Toggle();
            }

            if (!IsOpen) return;

            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                Close();
                return;
            }

            if (selected_page == 1) RefreshMonsterSummary();
        }

        private void Build()
        {
            panel_root = CreateRounded("Panel", transform, PANEL_COLOR, 2f);
            var panel_rect = panel_root.GetComponent<RectTransform>();
            panel_rect.anchorMin = panel_rect.anchorMax = new Vector2(0f, 0.5f);
            panel_rect.pivot = new Vector2(0f, 0.5f);
            panel_rect.anchoredPosition = new Vector2(24f, 0f);
            panel_rect.sizeDelta = new Vector2(390f, 670f);

            CreateText(
                "런타임 디버그 [F2]",
                panel_root.transform,
                26f,
                TextAlignmentOptions.MidlineLeft,
                new Vector2(18f, -10f),
                new Vector2(300f, 42f),
                TEXT_COLOR);

            var close = CreateButton(panel_root.transform, "X", new Vector2(340f, -10f), new Vector2(34f, 34f));
            close.onClick.AddListener(Close);

            CreateTab("맵", 0, 12f);
            CreateTab("몬스터", 1, 104f);
            CreateTab("업글", 2, 196f);
            CreateTab("해금", 3, 288f);

            BuildMapPage();
            BuildMonsterPage();
            BuildUpgradePage();
            BuildUnlockPage();
            map_editor.BrushChanged += OnBrushChanged;

            selected_page = 0;
            SetPage(0);
            panel_root.SetActive(false);
            map_editor.SetInputEnabled(false);
        }

        private void CreateTab(string label, int index, float x)
        {
            var button = CreateButton(
                panel_root.transform,
                label,
                new Vector2(x, -58f),
                new Vector2(84f, 42f));
            button.onClick.AddListener(() => SetPage(index));
            tab_backgrounds.Add(button.GetComponent<Image>());
            tab_labels.Add(button.GetComponentInChildren<TMP_Text>());
        }

        private void BuildMapPage()
        {
            var page = CreatePage("Map Page");
            CreateText(
                "월드에서 좌클릭·드래그하여 적용",
                page.transform,
                17f,
                TextAlignmentOptions.MidlineLeft,
                new Vector2(0f, 0f),
                new Vector2(350f, 30f),
                MUTED_COLOR);

            var brushes = new[]
            {
                ("잔디 A", FactoryDebugBrush.Grass01),
                ("잔디 B", FactoryDebugBrush.Grass02),
                ("셀 지우기", FactoryDebugBrush.Erase),
                ("파랑 염료", FactoryDebugBrush.DepositDyeBlue),
                ("빨강 염료", FactoryDebugBrush.DepositDyeRed),
                ("노랑 염료", FactoryDebugBrush.DepositDyeYellow),
                ("버섯", FactoryDebugBrush.DepositMushroom),
                ("달팽이", FactoryDebugBrush.DepositSnail),
                ("아이템 지우기", FactoryDebugBrush.ClearItems)
            };

            for (var index = 0; index < brushes.Length; index++)
            {
                var column = index % 3;
                var row = index / 3;
                var brush = brushes[index].Item2;
                var button = CreateButton(
                    page.transform,
                    brushes[index].Item1,
                    new Vector2(column * 116f, -42f - row * 54f),
                    new Vector2(108f, 46f));
                button.onClick.AddListener(() => map_editor.SetBrush(brush));
            }

            brush_label = CreateText(
                "선택 도구: 없음",
                page.transform,
                18f,
                TextAlignmentOptions.MidlineLeft,
                new Vector2(0f, -214f),
                new Vector2(350f, 32f),
                ACCENT_COLOR);

            var clear_all = CreateButton(
                page.transform,
                "이동 아이템 전체 정리",
                new Vector2(0f, -260f),
                new Vector2(224f, 46f));
            clear_all.onClick.AddListener(map_editor.ClearAllItems);

            CreateText(
                "건물·컨베이어 배치는 하단 핫바 또는 1~7 키를 사용합니다.\n"
                + "X 철거 모드는 드래그 제거를 지원합니다.",
                page.transform,
                16f,
                TextAlignmentOptions.TopLeft,
                new Vector2(0f, -326f),
                new Vector2(350f, 80f),
                MUTED_COLOR);
        }

        private void BuildMonsterPage()
        {
            var page = CreatePage("Monster Page");
            var previous = CreateButton(page.transform, "<", new Vector2(0f, 0f), new Vector2(42f, 38f));
            previous.onClick.AddListener(() => SelectMonster(-1));
            monster_name = CreateText(
                "",
                page.transform,
                21f,
                TextAlignmentOptions.Center,
                new Vector2(50f, 0f),
                new Vector2(248f, 38f),
                TEXT_COLOR);
            var next = CreateButton(page.transform, ">", new Vector2(306f, 0f), new Vector2(42f, 38f));
            next.onClick.AddListener(() => SelectMonster(1));

            base_value_field = CreateFloatField(page.transform, "기본 가치", -50f, value =>
                economy.SetBaseValue(SelectedMaterial, value));
            meso_bonus_field = CreateFloatField(page.transform, "합연산 / Lv", -104f, value =>
                economy.SetMesoBonusPerLevel(SelectedMaterial, value));
            multiplier_field = CreateFloatField(page.transform, "곱연산 / Lv", -158f, value =>
                economy.SetProductionMultiplierPerLevel(SelectedMaterial, value));
            meso_level_field = CreateIntegerField(page.transform, "메소 강화 Lv", -212f, value =>
                economy.SetMesoUpgradeLevel(SelectedMaterial, (int)value));
            production_level_field = CreateIntegerField(page.transform, "생산 강화 Lv", -266f, value =>
                economy.SetProductionUpgradeLevel(SelectedMaterial, (int)value));
            available_production_field = CreateIntegerField(page.transform, "사용 가능 생산량", -320f, value =>
                economy.SetAvailableProduction(SelectedMaterial, value));

            monster_summary = CreateText(
                "",
                page.transform,
                17f,
                TextAlignmentOptions.TopLeft,
                new Vector2(0f, -388f),
                new Vector2(350f, 90f),
                ACCENT_COLOR);
            RefreshMonsterFields();
        }

        private void BuildUpgradePage()
        {
            var page = CreatePage("Upgrade Page");
            var previous = CreateButton(page.transform, "<", new Vector2(0f, 0f), new Vector2(42f, 38f));
            previous.onClick.AddListener(() => SelectMonster(-1));
            upgrade_monster_name = CreateText(
                "",
                page.transform,
                20f,
                TextAlignmentOptions.Center,
                new Vector2(50f, 0f),
                new Vector2(248f, 38f),
                TEXT_COLOR);
            var next = CreateButton(page.transform, ">", new Vector2(306f, 0f), new Vector2(42f, 38f));
            next.onClick.AddListener(() => SelectMonster(1));

            meso_cost_field = CreateIntegerField(page.transform, "메소 기본 비용", -54f, value =>
            {
                economy.SetUpgradeBaseCosts(
                    SelectedMaterial,
                    value,
                    economy.GetProductionUpgradeBaseCost(SelectedMaterial));
                RefreshUpgradeFields();
            });
            production_cost_field = CreateIntegerField(page.transform, "생산량 기본 비용", -108f, value =>
            {
                economy.SetUpgradeBaseCosts(
                    SelectedMaterial,
                    economy.GetMesoUpgradeBaseCost(SelectedMaterial),
                    value);
                RefreshUpgradeFields();
            });
            meso_coefficient_field = CreateFloatField(page.transform, "메소 비용 계수", -162f, value =>
            {
                economy.SetUpgradeCostCoefficients(
                    value,
                    economy.ProductionUpgradeCostCoefficient);
                RefreshUpgradeFields();
            });
            production_coefficient_field = CreateFloatField(page.transform, "생산량 비용 계수", -216f, value =>
            {
                economy.SetUpgradeCostCoefficients(
                    economy.MesoUpgradeCostCoefficient,
                    value);
                RefreshUpgradeFields();
            });

            CreateText(
                "다음 강화 비용 = 기본 비용 × 계수^현재 레벨\n"
                + "기본 비용은 몬스터마다 다르게 설정됩니다.\n최대 레벨 제한은 없습니다.",
                page.transform,
                17f,
                TextAlignmentOptions.TopLeft,
                new Vector2(0f, -286f),
                new Vector2(350f, 126f),
                MUTED_COLOR);
            RefreshUpgradeFields();
        }

        private void BuildUnlockPage()
        {
            var page = CreatePage("Unlock Page");
            CreateText("스테이지", page.transform, 17f, TextAlignmentOptions.MidlineLeft,
                Vector2.zero, new Vector2(100f, 36f), MUTED_COLOR);
            var previous_stage = CreateButton(page.transform, "<", new Vector2(94f, 0f), new Vector2(38f, 36f));
            previous_stage.onClick.AddListener(() => SelectStage(-1));
            stage_name = CreateText("", page.transform, 18f, TextAlignmentOptions.Center,
                new Vector2(136f, 0f), new Vector2(166f, 36f), TEXT_COLOR);
            var next_stage = CreateButton(page.transform, ">", new Vector2(306f, 0f), new Vector2(38f, 36f));
            next_stage.onClick.AddListener(() => SelectStage(1));

            stage_unlock_cost_field = CreateIntegerField(page.transform, "스테이지 메소", -52f, value =>
            {
                SelectedStage.SetUnlockMesoCost(value);
                RefreshUnlockFields();
            });

            CreateText("사냥터", page.transform, 17f, TextAlignmentOptions.MidlineLeft,
                new Vector2(0f, -116f), new Vector2(100f, 36f), MUTED_COLOR);
            var previous_ground = CreateButton(page.transform, "<", new Vector2(94f, -116f), new Vector2(38f, 36f));
            previous_ground.onClick.AddListener(() => SelectHuntingGround(-1));
            hunting_ground_name = CreateText("", page.transform, 17f, TextAlignmentOptions.Center,
                new Vector2(136f, -116f), new Vector2(166f, 36f), TEXT_COLOR);
            var next_ground = CreateButton(page.transform, ">", new Vector2(306f, -116f), new Vector2(38f, 36f));
            next_ground.onClick.AddListener(() => SelectHuntingGround(1));

            hunting_unlock_cost_field = CreateIntegerField(page.transform, "사냥터 메소", -168f, value =>
            {
                SelectedHuntingGround.SetUnlockMesoCost(value);
                RefreshUnlockFields();
            });

            unlock_summary = CreateText("", page.transform, 16f, TextAlignmentOptions.TopLeft,
                new Vector2(0f, -232f), new Vector2(350f, 70f), MUTED_COLOR);

            var restart = CreateButton(
                page.transform,
                "변경사항 저장 후 처음부터 실행",
                new Vector2(0f, -330f),
                new Vector2(344f, 54f));
            restart.onClick.AddListener(SaveSettingsAndRestart);
            RefreshUnlockFields();
        }

        private GameObject CreatePage(string name)
        {
            var page = CreateObject(name, panel_root.transform);
            var rect = page.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(20f, -116f);
            rect.sizeDelta = new Vector2(350f, 530f);
            pages.Add(page);
            return page;
        }

        private NumericField CreateFloatField(
            Transform parent,
            string label,
            float y,
            Action<float> apply)
        {
            return CreateNumericField(parent, label, y, TMP_InputField.ContentType.DecimalNumber, value =>
            {
                if (!float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed)) return;
                apply(parsed);
                RefreshMonsterFields();
            });
        }

        private NumericField CreateIntegerField(
            Transform parent,
            string label,
            float y,
            Action<long> apply)
        {
            return CreateNumericField(parent, label, y, TMP_InputField.ContentType.IntegerNumber, value =>
            {
                if (!long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)) return;
                apply(parsed);
                if (selected_page == 1) RefreshMonsterFields();
            });
        }

        private NumericField CreateNumericField(
            Transform parent,
            string label,
            float y,
            TMP_InputField.ContentType content_type,
            Action<string> apply)
        {
            CreateText(
                label,
                parent,
                17f,
                TextAlignmentOptions.MidlineLeft,
                new Vector2(0f, y),
                new Vector2(190f, 42f),
                MUTED_COLOR);

            var field_object = CreateRounded("Field " + label, parent, FIELD_COLOR, 4f);
            SetRect(
                field_object.GetComponent<RectTransform>(),
                new Vector2(196f, y),
                new Vector2(152f, 42f));
            var input = field_object.AddComponent<TMP_InputField>();
            input.contentType = content_type;
            input.lineType = TMP_InputField.LineType.SingleLine;

            var viewport = CreateObject("Viewport", field_object.transform);
            var viewport_rect = viewport.GetComponent<RectTransform>();
            Stretch(viewport_rect);
            viewport_rect.offsetMin = new Vector2(10f, 3f);
            viewport_rect.offsetMax = new Vector2(-10f, -3f);
            viewport.AddComponent<RectMask2D>();

            var text = CreateText(
                "",
                viewport.transform,
                18f,
                TextAlignmentOptions.MidlineRight,
                Vector2.zero,
                Vector2.zero,
                TEXT_COLOR);
            Stretch(text.rectTransform);
            input.textViewport = viewport_rect;
            input.textComponent = text;
            input.onEndEdit.AddListener(value => apply(value));
            return new NumericField(input);
        }

        private void SetPage(int index)
        {
            selected_page = index;
            for (var page = 0; page < pages.Count; page++)
            {
                var selected = page == index;
                pages[page].SetActive(selected);
                tab_backgrounds[page].color = selected ? ACCENT_COLOR : CARD_COLOR;
                tab_labels[page].color = selected
                    ? new Color(0.1f, 0.11f, 0.09f, 1f)
                    : TEXT_COLOR;
            }

            map_editor.SetInputEnabled(IsOpen && index == 0);
            if (index == 1) RefreshMonsterFields();
            if (index == 2) RefreshUpgradeFields();
            if (index == 3) RefreshUnlockFields();
        }

        private FactoryStageDefinition SelectedStage => progression.Config.Stages[selected_stage];
        private HuntingGroundDefinition SelectedHuntingGround =>
            SelectedStage.HuntingGrounds[selected_hunting_ground];

        private void SelectStage(int offset)
        {
            var count = progression.Config.Stages.Count;
            selected_stage = (selected_stage + offset + count) % count;
            selected_hunting_ground = 0;
            RefreshUnlockFields();
        }

        private void SelectHuntingGround(int offset)
        {
            var count = SelectedStage.HuntingGrounds.Count;
            selected_hunting_ground = (selected_hunting_ground + offset + count) % count;
            RefreshUnlockFields();
        }

        private void RefreshUnlockFields()
        {
            if (stage_name == null) return;

            var stage = SelectedStage;
            var ground = SelectedHuntingGround;
            stage_name.text = stage.DisplayName;
            hunting_ground_name.text = ground.SupplyOption.SourceName;
            stage_unlock_cost_field.Set(stage.UnlockMesoCost);
            hunting_unlock_cost_field.Set(ground.UnlockMesoCost);
            unlock_summary.text = $"몬스터: {ground.SupplyOption.ItemLabel}\n"
                + $"초기 해금: {(ground.InitiallyUnlocked ? "예" : "아니오")}\n"
                + "해금 조건: 메소만 사용";
        }

        private void SaveSettingsAndRestart()
        {
            var settings = save_service.LoadSettings();
            settings.Capture(progression.Config, economy);
            settings.SetMap(map_editor.CaptureSettings());
            save_service.SaveSettings(settings);
            save_service.ResetProgress();
            FactoryStageSession.Clear();
            SceneManager.LoadScene(0);
        }

        private void SelectMonster(int offset)
        {
            var count = PortalSupplyCatalog.Options.Count;
            selected_monster = (selected_monster + offset + count) % count;
            RefreshMonsterFields();
        }

        private RawMaterialType SelectedMaterial => PortalSupplyCatalog.Options[selected_monster].Material;

        private void RefreshMonsterFields()
        {
            if (monster_name == null) return;

            var option = PortalSupplyCatalog.Options[selected_monster];
            monster_name.text = option.ItemLabel;
            base_value_field.Set(economy.GetBaseValue(option.Material));
            meso_bonus_field.Set(economy.GetMesoBonusPerLevel(option.Material));
            multiplier_field.Set(economy.GetProductionMultiplierPerLevel(option.Material));
            meso_level_field.Set(economy.GetMesoUpgradeLevel(option.Material));
            production_level_field.Set(economy.GetProductionUpgradeLevel(option.Material));
            available_production_field.Set(economy.GetAvailableProduction(option.Material));
            RefreshMonsterSummary();
            RefreshUpgradeFields();
        }

        private void RefreshMonsterSummary()
        {
            if (monster_summary == null) return;

            monster_summary.text = $"최종 가치  {economy.GetUnitValue(SelectedMaterial):0.##} 메소/개\n"
                + $"누적 생산량  {economy.GetTotalItems(SelectedMaterial):N0}\n"
                + $"사용 가능  {economy.GetAvailableProduction(SelectedMaterial):N0}";
        }

        private void RefreshUpgradeFields()
        {
            if (meso_cost_field == null) return;

            var option = PortalSupplyCatalog.Options[selected_monster];
            upgrade_monster_name.text = option.ItemLabel;
            meso_cost_field.Set(economy.GetMesoUpgradeBaseCost(option.Material));
            production_cost_field.Set(economy.GetProductionUpgradeBaseCost(option.Material));
            meso_coefficient_field.Set(economy.MesoUpgradeCostCoefficient);
            production_coefficient_field.Set(economy.ProductionUpgradeCostCoefficient);
        }

        private void OnBrushChanged(FactoryDebugBrush brush)
        {
            if (brush_label != null) brush_label.text = "선택 도구: " + GetBrushLabel(brush);
        }

        private static string GetBrushLabel(FactoryDebugBrush brush)
        {
            return brush switch
            {
                FactoryDebugBrush.None => "없음",
                FactoryDebugBrush.Grass01 => "잔디 A",
                FactoryDebugBrush.Grass02 => "잔디 B",
                FactoryDebugBrush.DepositDyeBlue => "파랑 염료",
                FactoryDebugBrush.DepositDyeRed => "빨강 염료",
                FactoryDebugBrush.DepositDyeYellow => "노랑 염료",
                FactoryDebugBrush.DepositMushroom => "버섯",
                FactoryDebugBrush.DepositSnail => "달팽이",
                FactoryDebugBrush.Erase => "셀 지우기",
                FactoryDebugBrush.ClearItems => "아이템 지우기",
                _ => brush.ToString()
            };
        }

        private Button CreateButton(Transform parent, string label, Vector2 position, Vector2 size)
        {
            var game_object = CreateRounded("Button " + label, parent, CARD_COLOR, 4f);
            SetRect(game_object.GetComponent<RectTransform>(), position, size);
            var button = game_object.AddComponent<Button>();
            button.targetGraphic = game_object.GetComponent<Image>();
            var text = CreateText(
                label,
                game_object.transform,
                17f,
                TextAlignmentOptions.Center,
                Vector2.zero,
                Vector2.zero,
                TEXT_COLOR);
            Stretch(text.rectTransform);
            return button;
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

        private sealed class NumericField
        {
            private readonly TMP_InputField input;

            public NumericField(TMP_InputField input)
            {
                this.input = input;
            }

            public void Set(float value)
            {
                input.SetTextWithoutNotify(value.ToString("0.###", CultureInfo.InvariantCulture));
            }

            public void Set(long value)
            {
                input.SetTextWithoutNotify(value.ToString(CultureInfo.InvariantCulture));
            }
        }
    }
}
