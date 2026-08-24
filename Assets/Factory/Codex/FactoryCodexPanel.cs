using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Maptory.Factory
{
    public sealed class FactoryCodexPanel : MonoBehaviour
    {
        private static readonly Color PANEL_COLOR = new(0.035f, 0.045f, 0.035f, 0.99f);
        private static readonly Color LIST_COLOR = new(0.018f, 0.024f, 0.018f, 0.99f);
        private static readonly Color CARD_COLOR = new(0.16f, 0.18f, 0.16f, 1f);
        private static readonly Color ACCENT_COLOR = new(0.94f, 0.83f, 0.32f, 1f);

        private readonly Stack<RawMaterialType> history = new();
        private readonly List<GameObject> generated_items = new();
        private readonly Dictionary<FactoryCodexCategory, Image> tab_images = new();
        private FactoryTileCatalog catalog;
        private FactoryProgression progression;
        private Func<bool> is_other_modal_open;
        private GameObject blocker;
        private Transform item_list;
        private Transform ingredient_links;
        private Image selected_icon;
        private TMP_Text selected_name;
        private TMP_Text selected_process;
        private Button back_button;
        private FactoryCodexCategory selected_category;
        private RawMaterialType? selected_material;

        public event Action Opened;
        public bool IsOpen => blocker != null && blocker.activeSelf;

        public static FactoryCodexPanel Create(
            Transform parent,
            FactoryTileCatalog catalog,
            FactoryProgression progression)
        {
            var canvas_object = new GameObject(
                "Recipe Codex UI",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            canvas_object.transform.SetParent(parent, false);
            var canvas = canvas_object.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 215;
            var scaler = canvas_object.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.matchWidthOrHeight = 1f;

            var panel = canvas_object.AddComponent<FactoryCodexPanel>();
            panel.catalog = catalog;
            panel.progression = progression;
            panel.Build();
            return panel;
        }

        public void SetOtherModalCheck(Func<bool> check)
        {
            is_other_modal_open = check;
        }

        public void Open(RawMaterialType? material = null)
        {
            if (is_other_modal_open != null && is_other_modal_open()) return;

            blocker.SetActive(true);
            history.Clear();
            if (material.HasValue)
            {
                var entry = FactoryContentCatalog.GetEntry(material.Value);
                SelectCategory(entry.Category);
                SelectMaterial(material.Value, false);
            }
            else
            {
                SelectCategory(selected_category);
            }
            Opened?.Invoke();
        }

        public void Toggle()
        {
            if (IsOpen) Close();
            else Open();
        }

        private void Update()
        {
            if (Keyboard.current == null) return;
            if (Keyboard.current.eKey.wasPressedThisFrame) Toggle();
            if (IsOpen && Keyboard.current.escapeKey.wasPressedThisFrame) Close();
        }

        private void Build()
        {
            blocker = CreateObject("Blocker", transform);
            Stretch(blocker.GetComponent<RectTransform>());
            blocker.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.45f);

            var panel = CreateRounded("Panel", blocker.transform, PANEL_COLOR, 2f);
            var panel_rect = panel.GetComponent<RectTransform>();
            panel_rect.anchorMin = panel_rect.anchorMax = new Vector2(0.5f, 0.5f);
            panel_rect.pivot = new Vector2(0.5f, 0.5f);
            panel_rect.sizeDelta = new Vector2(860f, 560f);

            CreateText("제작 도감  [E]", panel.transform, 30f, TextAlignmentOptions.MidlineLeft,
                new Vector2(22f, -12f), new Vector2(300f, 44f));
            var close = CreateButton("Close", panel.transform, "X", 23f, Color.clear);
            SetRect(close.GetComponent<RectTransform>(), new Vector2(800f, -10f), new Vector2(42f, 42f));
            close.onClick.AddListener(Close);

            var category_values = (FactoryCodexCategory[])Enum.GetValues(typeof(FactoryCodexCategory));
            for (var index = 0; index < category_values.Length; index++)
            {
                CreateTab(panel.transform, category_values[index], 20f + index * 154f);
            }

            var list = CreateRounded("Item List", panel.transform, LIST_COLOR, 3f);
            SetRect(list.GetComponent<RectTransform>(), new Vector2(20f, -108f), new Vector2(300f, 430f));
            item_list = list.transform;

            var details = CreateRounded("Details", panel.transform, LIST_COLOR, 3f);
            SetRect(details.GetComponent<RectTransform>(), new Vector2(334f, -108f), new Vector2(506f, 430f));
            selected_icon = CreateObject("Selected Icon", details.transform).AddComponent<Image>();
            selected_icon.preserveAspect = true;
            selected_icon.raycastTarget = false;
            SetRect(selected_icon.rectTransform, new Vector2(18f, -18f), new Vector2(72f, 72f));
            selected_name = CreateText("항목을 선택하세요", details.transform, 27f,
                TextAlignmentOptions.MidlineLeft, new Vector2(108f, -18f), new Vector2(330f, 40f));
            selected_process = CreateText("", details.transform, 19f,
                TextAlignmentOptions.TopLeft, new Vector2(20f, -112f), new Vector2(466f, 210f));
            selected_process.textWrappingMode = TextWrappingModes.Normal;
            selected_process.overflowMode = TextOverflowModes.Ellipsis;

            ingredient_links = CreateObject("Ingredient Links", details.transform).transform;
            SetRect(ingredient_links.GetComponent<RectTransform>(), new Vector2(20f, -330f), new Vector2(466f, 54f));
            back_button = CreateButton("Back", details.transform, "← 이전 제작법", 18f, CARD_COLOR);
            SetRect(back_button.GetComponent<RectTransform>(), new Vector2(326f, -386f), new Vector2(160f, 34f));
            back_button.onClick.AddListener(Back);

            selected_category = FactoryCodexCategory.Monster;
            SelectCategory(selected_category);
            blocker.SetActive(false);
        }

        private void CreateTab(Transform parent, FactoryCodexCategory category, float x)
        {
            var tab = CreateRounded(
                "Tab " + FactoryContentCatalog.GetCategoryName(category),
                parent,
                CARD_COLOR,
                4f);
            SetRect(tab.GetComponent<RectTransform>(), new Vector2(x, -64f), new Vector2(144f, 54f));
            var button = tab.AddComponent<Button>();
            button.targetGraphic = tab.GetComponent<Image>();
            var label = CreateText(FactoryContentCatalog.GetCategoryName(category), tab.transform, 19f,
                TextAlignmentOptions.Center, Vector2.zero, Vector2.zero);
            Stretch(label.rectTransform);
            button.onClick.AddListener(() => SelectCategory(category));
            tab_images.Add(category, tab.GetComponent<Image>());
        }

        private void SelectCategory(FactoryCodexCategory category)
        {
            selected_category = category;
            foreach (var tab in tab_images)
            {
                tab.Value.color = tab.Key == category ? ACCENT_COLOR : CARD_COLOR;
            }

            ClearGenerated();
            var row = 0;
            foreach (var entry in FactoryContentCatalog.Entries)
            {
                if (entry.Category != category) continue;
                CreateEntryButton(entry, row++);
            }
        }

        private void CreateEntryButton(FactoryCodexEntry entry, int row)
        {
            var unlocked = IsUnlocked(entry);
            var button = CreateButton(
                entry.Material.ToString(),
                item_list,
                unlocked ? entry.Material.ToKoreanName() : "[잠금] " + entry.Material.ToKoreanName(),
                18f,
                unlocked ? CARD_COLOR : new Color(0.08f, 0.09f, 0.08f, 1f));
            SetRect(button.GetComponent<RectTransform>(), new Vector2(10f, -10f - row * 44f), new Vector2(280f, 38f));
            button.onClick.AddListener(() => SelectMaterial(entry.Material, true));
            generated_items.Add(button.gameObject);
        }

        private void SelectMaterial(RawMaterialType material, bool remember)
        {
            if (remember && selected_material.HasValue) history.Push(selected_material.Value);
            selected_material = material;
            var entry = FactoryContentCatalog.GetEntry(material);
            selected_icon.sprite = catalog.GetItemSprite(material);
            selected_name.text = IsUnlocked(entry)
                ? material.ToKoreanName()
                : "[잠금] " + material.ToKoreanName();
            selected_process.text = BuildProcessText(entry);
            BuildIngredientLinks(entry);
            back_button.interactable = history.Count > 0;
        }

        private string BuildProcessText(FactoryCodexEntry entry)
        {
            var builder = new StringBuilder();
            AppendProcess(builder, entry, 0, new HashSet<RawMaterialType>());
            return builder.ToString();
        }

        private void AppendProcess(
            StringBuilder builder,
            FactoryCodexEntry entry,
            int depth,
            ISet<RawMaterialType> visited)
        {
            builder.Append(' ', depth * 2)
                .Append(entry.ProducerName)
                .Append(" → ")
                .AppendLine(entry.Material.ToKoreanName());
            if (!visited.Add(entry.Material)) return;

            foreach (var ingredient in entry.Ingredients)
            {
                AppendProcess(builder, FactoryContentCatalog.GetEntry(ingredient), depth + 1, visited);
            }
            visited.Remove(entry.Material);
        }

        private void BuildIngredientLinks(FactoryCodexEntry entry)
        {
            foreach (Transform child in ingredient_links) Destroy(child.gameObject);
            for (var index = 0; index < entry.Ingredients.Count; index++)
            {
                var ingredient = entry.Ingredients[index];
                var button = CreateButton(
                    "Ingredient " + ingredient,
                    ingredient_links,
                    ingredient.ToKoreanName() + "  ›",
                    17f,
                    CARD_COLOR);
                SetRect(button.GetComponent<RectTransform>(), new Vector2(index * 224f, 0f), new Vector2(214f, 42f));
                button.onClick.AddListener(() =>
                {
                    var target = FactoryContentCatalog.GetEntry(ingredient);
                    SelectCategory(target.Category);
                    SelectMaterial(ingredient, true);
                });
            }
        }

        private bool IsUnlocked(FactoryCodexEntry entry)
        {
            return entry.Category != FactoryCodexCategory.Monster
                || progression.IsMonsterUnlocked(entry.Material);
        }

        private void Back()
        {
            if (history.Count == 0) return;
            var material = history.Pop();
            var entry = FactoryContentCatalog.GetEntry(material);
            SelectCategory(entry.Category);
            SelectMaterial(material, false);
        }

        private void ClearGenerated()
        {
            foreach (var item in generated_items) Destroy(item);
            generated_items.Clear();
        }

        private void Close()
        {
            blocker.SetActive(false);
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

        private Button CreateButton(string name, Transform parent, string label, float size, Color color)
        {
            var game_object = CreateRounded(name, parent, color, 4f);
            var button = game_object.AddComponent<Button>();
            button.targetGraphic = game_object.GetComponent<Image>();
            var text = CreateText(label, game_object.transform, size,
                TextAlignmentOptions.Center, Vector2.zero, Vector2.zero);
            Stretch(text.rectTransform);
            return button;
        }

        private TMP_Text CreateText(string value, Transform parent, float size,
            TextAlignmentOptions alignment, Vector2 position, Vector2 dimensions)
        {
            var text = CreateObject("Text", parent).AddComponent<TextMeshProUGUI>();
            text.font = catalog.UiFont;
            text.text = value;
            text.fontSize = size;
            text.color = new Color(0.9f, 0.9f, 0.87f, 1f);
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
    }
}
