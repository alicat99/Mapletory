using System;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Maptory.Factory
{
    public sealed class FactoryTutorialSystem : MonoBehaviour
    {
        private static readonly string[] STEP_TITLES =
        {
            "카메라 이동",
            "화면 확대/축소",
            "컨베이어 선택",
            "건물 회전",
            "컨베이어 건설",
            "철거 모드",
            "실제 철거",
            "기본 건물 기능"
        };

        private static readonly string[] STEP_DETAILS =
        {
            "우클릭으로 드래그해 카메라를 이동하세요.",
            "마우스 휠을 굴려 Zoom In/Out 하세요.",
            "숫자 1키 또는 Hotbar 첫 칸으로 컨베이어를 선택하세요.",
            "2~6번의 회전 가능한 건물을 선택한 뒤 R키로 방향을 돌리세요.",
            "1키로 컨베이어를 선택하고 좌클릭 또는 드래그로 1개 이상 설치하세요.",
            "X키를 눌러 제거 모드에 들어가세요.",
            "방금 설치한 컨베이어 또는 다른 건물을 좌클릭해 실제로 제거하세요.",
            "공장의 기본 건물 역할을 확인하세요."
        };

        private FactoryProgression progression;
        private GameObject action_panel;
        private TMP_Text step_label;
        private TMP_Text instruction_label;
        private GameObject modal_blocker;
        private TMP_Text modal_title;
        private TMP_Text modal_body;
        private TMP_Text modal_confirm_label;
        private Button replay_button;
        private bool is_running;
        private FactoryTutorialTracker tracker;
        private Action modal_confirm;

        public bool IsModalOpen => modal_blocker != null && modal_blocker.activeSelf;

        public static FactoryTutorialSystem Create(
            Transform parent,
            FactoryTileCatalog catalog,
            FactoryProgression progression,
            FactoryCameraController camera_controller,
            FactoryBuildMode build_mode,
            ConveyorBuilder conveyor_builder,
            FactoryDemolitionController demolition,
            RecipeSelectionPanel recipe_panel,
            PortalSelectionPanel portal_panel,
            ItemUpgradePanel upgrade_panel,
            FactoryCodexPanel codex)
        {
            var canvas_object = new GameObject(
                "Tutorial UI",
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

            var tutorial = canvas_object.AddComponent<FactoryTutorialSystem>();
            tutorial.progression = progression;
            tutorial.tracker = new FactoryTutorialTracker(progression.Tutorial);
            tutorial.Build(catalog);
            tutorial.Bind(
                camera_controller,
                build_mode,
                conveyor_builder,
                demolition,
                recipe_panel,
                portal_panel,
                upgrade_panel,
                codex);
            tutorial.RefreshStartupState();
            return tutorial;
        }

        private void Bind(
            FactoryCameraController camera_controller,
            FactoryBuildMode build_mode,
            ConveyorBuilder conveyor_builder,
            FactoryDemolitionController demolition,
            RecipeSelectionPanel recipe_panel,
            PortalSelectionPanel portal_panel,
            ItemUpgradePanel upgrade_panel,
            FactoryCodexPanel codex)
        {
            camera_controller.Panned += distance =>
            {
                if (!IsStep(0)) return;
                Record(FactoryTutorialAction.Pan, distance);
            };
            camera_controller.Zoomed += amount =>
            {
                if (!IsStep(1)) return;
                Record(FactoryTutorialAction.Zoom, amount);
            };
            build_mode.Changed += tool =>
            {
                if (IsStep(2) && tool == FactoryBuildTool.Conveyor)
                    Record(FactoryTutorialAction.SelectConveyor);
            };
            build_mode.Rotated += (_, _) =>
            {
                if (IsStep(3)) Record(FactoryTutorialAction.RotateBuilding);
            };
            conveyor_builder.ConveyorsPlaced += count =>
            {
                if (IsStep(4) && count > 0) Record(FactoryTutorialAction.PlaceConveyor, count);
            };
            build_mode.DemolitionChanged += active =>
            {
                if (IsStep(5) && active) Record(FactoryTutorialAction.EnterDemolition);
            };
            demolition.Demolished += _ =>
            {
                if (IsStep(6)) Record(FactoryTutorialAction.Demolish);
            };

            recipe_panel.Opened += machine => ShowFeatureOnce(
                "recipe_selection",
                machine + " 레시피",
                "원하는 결과 아이콘을 고른 뒤 필요한 재료를 확인하고 ‘확인’을 누르세요. 염색기·조합기·가공시설은 같은 방식으로 레시피를 선택합니다.");
            portal_panel.Opened += () => ShowFeatureOnce(
                "portal_selection",
                "포탈과 사냥터",
                "공급할 몬스터와 사냥터를 선택합니다. 잠긴 사냥터는 메소 조건을 채운 뒤 ‘해금’할 수 있습니다.");
            upgrade_panel.Opened += () => ShowFeatureOnce(
                "item_upgrades",
                "아이템 업그레이드  [U]",
                "메소 탭은 메소를 소비해 가치를 합연산으로 높입니다. 누적 생산량 탭은 포탈에 보낸 생산량을 소비해 가치를 강하게 곱연산합니다.");
            codex.Opened += () => ShowFeatureOnce(
                "codex",
                "제작 도감  [E]",
                "몬스터와 전체 레시피를 확인할 수 있습니다. 제작 과정의 중간 아이템을 누르면 해당 제작법으로 이동하고, 이전 제작법으로 돌아갈 수 있습니다.");
        }

        private void RefreshStartupState()
        {
            if (progression.Tutorial.initial_completed)
            {
                is_running = false;
                action_panel.SetActive(false);
                replay_button.gameObject.SetActive(true);
                return;
            }

            is_running = true;
            action_panel.SetActive(true);
            replay_button.gameObject.SetActive(false);
            RefreshStep();
        }

        private bool IsStep(int step)
        {
            return is_running && progression.Tutorial.initial_step == step && !IsModalOpen;
        }

        private void Record(FactoryTutorialAction action, float amount = 1f)
        {
            if (!tracker.Record(action, amount)) return;

            progression.MarkChanged();
            progression.Save();
            RefreshStep();
        }

        private void RefreshStep()
        {
            var step = Mathf.Clamp(progression.Tutorial.initial_step, 0, STEP_TITLES.Length - 1);
            step_label.text = $"튜토리얼  {step + 1}/{STEP_TITLES.Length}  ·  {STEP_TITLES[step]}";
            instruction_label.text = STEP_DETAILS[step];
            if (step == STEP_TITLES.Length - 1) ShowBuildingExplanation();
        }

        private void ShowBuildingExplanation()
        {
            var body = new StringBuilder();
            var index = 1;
            foreach (var building in FactoryContentCatalog.Buildings)
            {
                body.Append(index++)
                    .Append("  ")
                    .Append(building.DisplayName)
                    .Append("  ·  ")
                    .AppendLine(building.Description);
            }
            ShowModal("기본 건물 기능", body.ToString(), "튜토리얼 완료", CompleteTutorial);
        }

        private void CompleteTutorial()
        {
            progression.Tutorial.initial_completed = true;
            progression.Tutorial.initial_step = STEP_TITLES.Length;
            progression.MarkChanged();
            progression.Save();
            is_running = false;
            action_panel.SetActive(false);
            replay_button.gameObject.SetActive(true);
        }

        private void SkipTutorial()
        {
            CompleteTutorial();
        }

        private void ReplayTutorial()
        {
            progression.Tutorial.initial_completed = false;
            progression.Tutorial.initial_step = 0;
            progression.MarkChanged();
            progression.Save();
            tracker.ResetAccumulation();
            is_running = true;
            action_panel.SetActive(true);
            replay_button.gameObject.SetActive(false);
            RefreshStep();
        }

        private void ShowFeatureOnce(string feature_id, string title, string body)
        {
            if (is_running || progression.Tutorial.HasSeen(feature_id)) return;

            progression.Tutorial.MarkSeen(feature_id);
            progression.MarkChanged();
            progression.Save();
            ShowModal(title, body, "확인", null);
        }

        private void ShowModal(string title, string body, string confirm, Action confirmed)
        {
            modal_title.text = title;
            modal_body.text = body;
            modal_confirm_label.text = confirm;
            modal_confirm = confirmed;
            modal_blocker.SetActive(true);
        }

        private void ConfirmModal()
        {
            modal_blocker.SetActive(false);
            var callback = modal_confirm;
            modal_confirm = null;
            callback?.Invoke();
        }

        private void Build(FactoryTileCatalog catalog)
        {
            BuildActionPanel(catalog);
            BuildReplayButton(catalog);
            BuildModal(catalog);
        }

        private void BuildActionPanel(FactoryTileCatalog catalog)
        {
            action_panel = CreateRounded("Action Tutorial", transform, catalog,
                new Color(0.035f, 0.04f, 0.06f, 0.96f), 4f);
            var rect = action_panel.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -20f);
            rect.sizeDelta = new Vector2(560f, 98f);
            step_label = CreateText(catalog, action_panel.transform, 21f,
                TextAlignmentOptions.MidlineLeft, new Vector2(18f, -8f), new Vector2(430f, 32f));
            step_label.color = new Color(1f, 0.84f, 0.2f, 1f);
            instruction_label = CreateText(catalog, action_panel.transform, 17f,
                TextAlignmentOptions.TopLeft, new Vector2(18f, -42f), new Vector2(430f, 46f));
            var skip = CreateButton(catalog, action_panel.transform, "건너뛰기", 16f);
            SetRect(skip.GetComponent<RectTransform>(), new Vector2(452f, -20f), new Vector2(90f, 52f));
            skip.onClick.AddListener(SkipTutorial);
        }

        private void BuildReplayButton(FactoryTileCatalog catalog)
        {
            replay_button = CreateButton(catalog, transform, "튜토리얼 다시보기", 16f);
            var rect = replay_button.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(24f, -154f);
            rect.sizeDelta = new Vector2(190f, 44f);
            replay_button.onClick.AddListener(ReplayTutorial);
        }

        private void BuildModal(FactoryTileCatalog catalog)
        {
            modal_blocker = new GameObject("Tutorial Modal Blocker", typeof(RectTransform), typeof(Image));
            modal_blocker.transform.SetParent(transform, false);
            Stretch(modal_blocker.GetComponent<RectTransform>());
            modal_blocker.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.62f);
            var panel = CreateRounded("Panel", modal_blocker.transform, catalog,
                new Color(0.035f, 0.045f, 0.035f, 0.99f), 2f);
            var rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(600f, 430f);
            modal_title = CreateText(catalog, panel.transform, 30f,
                TextAlignmentOptions.MidlineLeft, new Vector2(28f, -20f), new Vector2(544f, 46f));
            modal_body = CreateText(catalog, panel.transform, 20f,
                TextAlignmentOptions.TopLeft, new Vector2(28f, -86f), new Vector2(544f, 250f));
            modal_body.textWrappingMode = TextWrappingModes.Normal;
            var confirm = CreateButton(catalog, panel.transform, "", 20f);
            SetRect(confirm.GetComponent<RectTransform>(), new Vector2(378f, -356f), new Vector2(194f, 52f));
            modal_confirm_label = confirm.GetComponentInChildren<TMP_Text>();
            confirm.onClick.AddListener(ConfirmModal);
            modal_blocker.SetActive(false);
        }

        private static GameObject CreateRounded(
            string name,
            Transform parent,
            FactoryTileCatalog catalog,
            Color color,
            float multiplier)
        {
            var game_object = new GameObject(name, typeof(RectTransform), typeof(Image));
            game_object.transform.SetParent(parent, false);
            var image = game_object.GetComponent<Image>();
            image.sprite = catalog.RoundedRectangle;
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = multiplier;
            image.color = color;
            return game_object;
        }

        private static Button CreateButton(
            FactoryTileCatalog catalog,
            Transform parent,
            string label,
            float size)
        {
            var game_object = CreateRounded("Button", parent, catalog,
                new Color(0.16f, 0.18f, 0.16f, 1f), 4f);
            var button = game_object.AddComponent<Button>();
            button.targetGraphic = game_object.GetComponent<Image>();
            var text = CreateText(catalog, game_object.transform, size,
                TextAlignmentOptions.Center, Vector2.zero, Vector2.zero);
            text.text = label;
            Stretch(text.rectTransform);
            return button;
        }

        private static TMP_Text CreateText(
            FactoryTileCatalog catalog,
            Transform parent,
            float size,
            TextAlignmentOptions alignment,
            Vector2 position,
            Vector2 dimensions)
        {
            var text = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI))
                .GetComponent<TextMeshProUGUI>();
            text.transform.SetParent(parent, false);
            text.font = catalog.UiFont;
            text.fontSize = size;
            text.color = new Color(0.9f, 0.9f, 0.87f, 1f);
            text.alignment = alignment;
            text.raycastTarget = false;
            SetRect(text.rectTransform, position, dimensions);
            return text;
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
