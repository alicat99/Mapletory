using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Maptory.Factory
{
    public sealed class FactoryObjectiveSystem : MonoBehaviour
    {
        private static readonly ObjectiveDefinition[] OBJECTIVES =
        {
            new("원재료 생산", "추출기에서 원재료를 1개 생산하세요.", RawMaterialType.Snail),
            new("첫 몬스터 제작", "에르다 주입기로 첫 몬스터를 만드세요.", RawMaterialType.MonsterSnailGreen),
            new("사냥터 공급", "완성된 몬스터를 포탈에 공급하세요.", RawMaterialType.MonsterSnailGreen),
            new("아이템 업그레이드", "U를 눌러 메소 또는 누적 생산량 업그레이드를 하세요.", RawMaterialType.MonsterSnailGreen),
            new("새 사냥터 해금", "포탈에서 다음 사냥터를 해금하세요.", RawMaterialType.MonsterSnailRed),
            new("다음 몬스터 제작", "새로 해금한 몬스터를 제작하세요.", RawMaterialType.MonsterSnailRed)
        };

        private FactoryProgression progression;
        private FactoryCodexPanel codex;
        private TMP_Text title;
        private TMP_Text detail;
        private Button recipe_button;

        public static FactoryObjectiveSystem Create(
            Transform parent,
            FactoryTileCatalog catalog,
            FactoryProgression progression,
            FactoryItemTransport transport,
            FactoryCodexPanel codex)
        {
            var canvas_object = new GameObject(
                "Current Objective UI",
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

            var system = canvas_object.AddComponent<FactoryObjectiveSystem>();
            system.progression = progression;
            system.codex = codex;
            system.Build(catalog);
            system.Bind(transport);
            system.Refresh();
            return system;
        }

        private void Bind(FactoryItemTransport transport)
        {
            transport.ItemSpawned += OnItemSpawned;
            progression.Economy.Supplied += OnSupplied;
            progression.Economy.UpgradePurchased += OnUpgradePurchased;
            progression.HuntingGroundUnlocked += OnHuntingGroundUnlocked;
        }

        private void OnItemSpawned(FactoryItemState item)
        {
            var step = progression.Objectives.current_step;
            if (step == 0 && FactoryContentCatalog.GetEntry(item.Material).Category == FactoryCodexCategory.RawMaterial)
            {
                Advance();
            }
            else if ((step == 1 || step == 5)
                     && FactoryContentCatalog.GetEntry(item.Material).Category == FactoryCodexCategory.Monster)
            {
                Advance();
            }
        }

        private void OnSupplied(RawMaterialType material)
        {
            if (progression.Objectives.current_step == 2) Advance();
        }

        private void OnUpgradePurchased(RawMaterialType material, ItemUpgradeCategory category)
        {
            if (progression.Objectives.current_step == 3) Advance();
        }

        private void OnHuntingGroundUnlocked(string hunting_ground_id)
        {
            if (progression.Objectives.current_step == 4) Advance();
        }

        private void Advance()
        {
            progression.Objectives.current_step++;
            progression.MarkChanged();
            Refresh();
        }

        private void Refresh()
        {
            if (progression.Objectives.current_step >= OBJECTIVES.Length)
            {
                title.text = "현재 목표  ·  완료";
                detail.text = "기본 생산 흐름을 모두 익혔습니다.";
                recipe_button.gameObject.SetActive(false);
                return;
            }

            var objective = OBJECTIVES[progression.Objectives.current_step];
            title.text = $"현재 목표  ·  {objective.Title}";
            detail.text = objective.Detail + "  (0/1)";
            recipe_button.gameObject.SetActive(true);
        }

        private void Build(FactoryTileCatalog catalog)
        {
            var panel = new GameObject("Objective", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(transform, false);
            var rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-22f, -84f);
            rect.sizeDelta = new Vector2(370f, 112f);
            var image = panel.GetComponent<Image>();
            image.sprite = catalog.RoundedRectangle;
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = 4f;
            image.color = new Color(0.035f, 0.04f, 0.06f, 0.94f);

            title = CreateText(catalog, panel.transform, 20f, TextAlignmentOptions.MidlineLeft,
                new Vector2(16f, -8f), new Vector2(338f, 30f));
            title.color = new Color(1f, 0.84f, 0.2f, 1f);
            detail = CreateText(catalog, panel.transform, 16f, TextAlignmentOptions.TopLeft,
                new Vector2(16f, -40f), new Vector2(238f, 58f));
            recipe_button = CreateButton(catalog, panel.transform, "레시피 보기");
            var button_rect = recipe_button.GetComponent<RectTransform>();
            button_rect.anchorMin = button_rect.anchorMax = new Vector2(0f, 1f);
            button_rect.pivot = new Vector2(0f, 1f);
            button_rect.anchoredPosition = new Vector2(260f, -52f);
            button_rect.sizeDelta = new Vector2(94f, 42f);
            recipe_button.onClick.AddListener(OpenRecipe);
        }

        private void OpenRecipe()
        {
            if (progression.Objectives.current_step >= OBJECTIVES.Length) return;
            codex.Open(OBJECTIVES[progression.Objectives.current_step].Target);
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
            var rect = text.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = dimensions;
            return text;
        }

        private static Button CreateButton(FactoryTileCatalog catalog, Transform parent, string label)
        {
            var button_object = new GameObject("Recipe", typeof(RectTransform), typeof(Image), typeof(Button));
            button_object.transform.SetParent(parent, false);
            var image = button_object.GetComponent<Image>();
            image.sprite = catalog.RoundedRectangle;
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = 4f;
            image.color = new Color(0.18f, 0.2f, 0.18f, 1f);
            var button = button_object.GetComponent<Button>();
            button.targetGraphic = image;
            var text = CreateText(catalog, button_object.transform, 15f, TextAlignmentOptions.Center,
                Vector2.zero, Vector2.zero);
            text.text = label;
            text.rectTransform.anchorMin = Vector2.zero;
            text.rectTransform.anchorMax = Vector2.one;
            text.rectTransform.offsetMin = text.rectTransform.offsetMax = Vector2.zero;
            return button;
        }

        private sealed class ObjectiveDefinition
        {
            public string Title { get; }
            public string Detail { get; }
            public RawMaterialType Target { get; }

            public ObjectiveDefinition(string title, string detail, RawMaterialType target)
            {
                Title = title;
                Detail = detail;
                Target = target;
            }
        }
    }
}
