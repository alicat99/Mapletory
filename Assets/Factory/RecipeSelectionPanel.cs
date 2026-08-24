using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Maptory.Factory
{
    public sealed class RecipeSelectionPanel : MonoBehaviour
    {
        private static readonly Color PANEL_COLOR = new(0.035f, 0.045f, 0.035f, 0.98f);
        private static readonly Color DETAILS_COLOR = new(0.018f, 0.024f, 0.018f, 0.99f);
        private static readonly Color CARD_COLOR = new(0.16f, 0.18f, 0.16f, 1f);
        private static readonly Color SELECTION_COLOR = new(0.88f, 0.9f, 0.86f, 1f);
        private static readonly Color FOOTER_COLOR = new(0.34f, 0.36f, 0.33f, 1f);

        private readonly Dictionary<IRecipe, GameObject> recipe_selections = new();
        private readonly List<GameObject> ingredient_rows = new();
        private readonly List<Image> ingredient_icons = new();
        private readonly List<TMP_Text> ingredient_names = new();

        private FactoryTileCatalog catalog;
        private IRecipeMachine machine;
        private IRecipe pending_recipe;
        private GameObject blocker;
        private Transform recipe_list;
        private Image selected_result_icon;
        private TMP_Text selected_name;
        private TMP_Text title;

        public event Action<IRecipeMachine> RecipeSelected;
        public event Action<string> Opened;
        public bool IsOpen => blocker != null && blocker.activeSelf;

        public static RecipeSelectionPanel Create(Transform parent, FactoryTileCatalog catalog)
        {
            var canvas_object = new GameObject(
                "Recipe Selection UI",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            canvas_object.transform.SetParent(parent, false);
            var canvas = canvas_object.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 200;

            var scaler = canvas_object.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.matchWidthOrHeight = 1f;

            var panel = canvas_object.AddComponent<RecipeSelectionPanel>();
            panel.catalog = catalog;
            panel.Build();
            return panel;
        }

        public void Show(
            IRecipeMachine target,
            string panel_title,
            IReadOnlyList<RecipeCategory> categories)
        {
            machine = target;
            pending_recipe = target.SelectedRecipe;
            title.text = panel_title;
            PopulateRecipeList(categories);
            blocker.SetActive(true);
            RefreshSelection();
            Opened?.Invoke(panel_title);
        }

        private void Build()
        {
            blocker = CreateObject("Blocker", transform);
            Stretch(blocker.GetComponent<RectTransform>());
            blocker.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.58f);

            var panel = CreateRounded("Panel", blocker.transform, PANEL_COLOR, 2f);
            var panel_rect = panel.GetComponent<RectTransform>();
            panel_rect.anchorMin = panel_rect.anchorMax = new Vector2(0.5f, 0.5f);
            panel_rect.pivot = new Vector2(0.5f, 0.5f);
            panel_rect.anchoredPosition = Vector2.zero;
            panel_rect.sizeDelta = new Vector2(720f, 480f);

            title = CreateText("", panel.transform, 30f, TextAlignmentOptions.Left,
                new Vector2(22f, -14f), new Vector2(600f, 44f));
            var close = CreateButton("Close", panel.transform, "×", 38f);
            SetRect(close.GetComponent<RectTransform>(), new Vector2(662f, -12f), new Vector2(40f, 40f));
            close.GetComponent<Image>().color = Color.clear;
            close.onClick.AddListener(Close);
            CreateLine(panel.transform, new Vector2(0f, -62f), new Vector2(720f, 1f));

            BuildRecipeList(panel.transform);
            BuildDetails(panel.transform);
            BuildFooter(panel.transform);
            blocker.SetActive(false);
        }

        private void BuildRecipeList(Transform panel)
        {
            var recipes = CreateObject("Recipes", panel);
            SetRect(recipes.GetComponent<RectTransform>(), new Vector2(20f, -76f), new Vector2(390f, 294f));
            recipe_list = recipes.transform;
        }

        private void PopulateRecipeList(IReadOnlyList<RecipeCategory> categories)
        {
            recipe_selections.Clear();
            foreach (Transform child in recipe_list)
            {
                child.gameObject.SetActive(false);
                Destroy(child.gameObject);
            }

            for (var category_index = 0; category_index < categories.Count; category_index++)
            {
                var category = categories[category_index];
                var category_y = category_index * 94f;
                CreateRecipeCategory(recipe_list, category.Name, category_y);
                for (var recipe_index = 0; recipe_index < category.Recipes.Count; recipe_index++)
                {
                    CreateRecipeCard(
                        recipe_list,
                        category.Recipes[recipe_index],
                        recipe_index,
                        category_y + 28f);
                }
            }
        }

        private void BuildDetails(Transform panel)
        {
            var details = CreateRounded("Details", panel, DETAILS_COLOR, 3f);
            SetRect(details.GetComponent<RectTransform>(), new Vector2(426f, -76f), new Vector2(274f, 294f));
            CreateText("필요 재료", details.transform, 20f, TextAlignmentOptions.Left,
                new Vector2(16f, -14f), new Vector2(220f, 30f));

            ingredient_rows.Add(CreateItemRow(
                details.transform,
                50f,
                out var first_icon,
                out var first_name));
            ingredient_icons.Add(first_icon);
            ingredient_names.Add(first_name);
            ingredient_rows.Add(CreateItemRow(
                details.transform,
                126f,
                out var second_icon,
                out var second_name));
            ingredient_icons.Add(second_icon);
            ingredient_names.Add(second_name);
            CreateLine(details.transform, new Vector2(16f, -226f), new Vector2(242f, 1f));
            CreateText("소요 시간", details.transform, 18f, TextAlignmentOptions.Left,
                new Vector2(16f, -238f), new Vector2(140f, 32f));
            CreateText("1.0초", details.transform, 18f, TextAlignmentOptions.Right,
                new Vector2(164f, -238f), new Vector2(94f, 32f));
        }

        private void BuildFooter(Transform panel)
        {
            var footer = CreateRounded("Selected Recipe", panel, FOOTER_COLOR, 4f);
            SetRect(footer.GetComponent<RectTransform>(), new Vector2(20f, -392f), new Vector2(680f, 68f));

            selected_name = CreateText("(레시피 선택)", footer.transform, 22f,
                TextAlignmentOptions.MidlineLeft, new Vector2(18f, -8f), new Vector2(410f, 52f));
            selected_result_icon = CreateObject("Result Icon", footer.transform).AddComponent<Image>();
            selected_result_icon.preserveAspect = true;
            selected_result_icon.raycastTarget = false;
            SetRect(selected_result_icon.rectTransform, new Vector2(456f, -6f), new Vector2(56f, 56f));

            var confirm = CreateButton("Confirm", footer.transform, "확인", 23f);
            SetRect(confirm.GetComponent<RectTransform>(), new Vector2(530f, -8f), new Vector2(132f, 52f));
            confirm.onClick.AddListener(Confirm);
        }

        private void CreateRecipeCategory(Transform parent, string label, float y)
        {
            CreateText(label, parent, 19f, TextAlignmentOptions.Left,
                new Vector2(6f, -y), new Vector2(160f, 26f));
        }

        private void CreateRecipeCard(
            Transform parent,
            IRecipe recipe,
            int column,
            float y)
        {
            var card = CreateObject(recipe.DisplayName, parent);
            SetRect(card.GetComponent<RectTransform>(), new Vector2(6f + column * 82f, -y), new Vector2(70f, 62f));

            var selection = CreateRounded("Selection", card.transform, SELECTION_COLOR, 5f);
            Stretch(selection.GetComponent<RectTransform>());
            var selection_inner = CreateRounded("Inner", selection.transform, PANEL_COLOR, 5f);
            var inner_rect = selection_inner.GetComponent<RectTransform>();
            Stretch(inner_rect);
            inner_rect.offsetMin = new Vector2(2f, 2f);
            inner_rect.offsetMax = new Vector2(-2f, -2f);
            selection.SetActive(false);
            recipe_selections.Add(recipe, selection);

            var icon = CreateObject("Icon", card.transform).AddComponent<Image>();
            icon.sprite = catalog.GetItemSprite(recipe.Result);
            icon.preserveAspect = true;
            icon.raycastTarget = true;
            SetRect(icon.rectTransform, new Vector2(7f, -3f), new Vector2(56f, 56f));
            var button = card.AddComponent<Button>();
            button.targetGraphic = icon;
            button.onClick.AddListener(() => Select(recipe));
        }

        private GameObject CreateItemRow(
            Transform parent,
            float y,
            out Image icon,
            out TMP_Text label)
        {
            var row = CreateRounded("Ingredient", parent, CARD_COLOR, 4f);
            SetRect(row.GetComponent<RectTransform>(), new Vector2(16f, -y), new Vector2(242f, 66f));
            icon = CreateObject("Icon", row.transform).AddComponent<Image>();
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            SetRect(icon.rectTransform, new Vector2(10f, -8f), new Vector2(50f, 50f));
            label = CreateText("-", row.transform, 18f, TextAlignmentOptions.MidlineLeft,
                new Vector2(72f, -8f), new Vector2(156f, 50f));
            return row;
        }

        private void Select(IRecipe recipe)
        {
            pending_recipe = recipe;
            RefreshSelection();
        }

        private void RefreshSelection()
        {
            foreach (var pair in recipe_selections)
            {
                pair.Value.SetActive(pair.Key == pending_recipe);
            }

            if (pending_recipe == null)
            {
                ingredient_rows[0].SetActive(true);
                ingredient_rows[1].SetActive(false);
                ingredient_icons[0].enabled = false;
                ingredient_names[0].text = "레시피를 선택하세요";
                selected_name.text = "(레시피 선택)";
                selected_result_icon.enabled = false;
                return;
            }

            for (var index = 0; index < ingredient_rows.Count; index++)
            {
                var active = index < pending_recipe.Ingredients.Count;
                ingredient_rows[index].SetActive(active);
                if (active)
                {
                    SetItem(
                        ingredient_icons[index],
                        ingredient_names[index],
                        pending_recipe.Ingredients[index]);
                }
            }
            selected_name.text = pending_recipe.DisplayName;
            selected_result_icon.enabled = true;
            selected_result_icon.sprite = catalog.GetItemSprite(pending_recipe.Result);
        }

        private void SetItem(Image icon, TMP_Text label, RawMaterialType material)
        {
            icon.enabled = true;
            icon.sprite = catalog.GetItemSprite(material);
            label.text = material.ToKoreanName() + "  × 1";
        }

        private void Confirm()
        {
            if (pending_recipe == null)
            {
                return;
            }

            machine.SelectRecipe(pending_recipe);
            RecipeSelected?.Invoke(machine);
            Close();
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

        private Button CreateButton(string name, Transform parent, string label, float size)
        {
            var game_object = CreateRounded(name, parent, new Color(0.13f, 0.14f, 0.13f, 1f), 4f);
            var button = game_object.AddComponent<Button>();
            button.targetGraphic = game_object.GetComponent<Image>();
            var text = CreateText(label, game_object.transform, size, TextAlignmentOptions.Center,
                Vector2.zero, Vector2.zero);
            Stretch(text.rectTransform);
            return button;
        }

        private void CreateLine(Transform parent, Vector2 position, Vector2 size)
        {
            var line = CreateObject("Divider", parent);
            SetRect(line.GetComponent<RectTransform>(), position, size);
            line.AddComponent<Image>().color = new Color(0.55f, 0.57f, 0.53f, 0.28f);
        }

        private TMP_Text CreateText(
            string value,
            Transform parent,
            float size,
            TextAlignmentOptions alignment,
            Vector2 position,
            Vector2 dimensions)
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
