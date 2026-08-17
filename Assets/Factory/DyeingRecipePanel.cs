using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Maptory.Factory
{
    public sealed class DyeingRecipePanel : MonoBehaviour
    {
        private static readonly Color PANEL_COLOR = new(0.07f, 0.09f, 0.07f, 0.97f);
        private static readonly Color CARD_COLOR = new(0.16f, 0.18f, 0.16f, 1f);
        private static readonly Color SELECTED_COLOR = new(0.42f, 0.46f, 0.40f, 1f);

        private readonly Dictionary<DyeingRecipe, Image> recipe_cards = new();

        private FactoryTileCatalog catalog;
        private DyeingMachineState machine;
        private DyeingRecipe pending_recipe;
        private GameObject blocker;
        private Transform recipe_grid;
        private Image base_icon;
        private Image dye_icon;
        private Image result_icon;
        private TMP_Text base_name;
        private TMP_Text dye_name;
        private TMP_Text result_name;

        public event Action<DyeingMachineState> RecipeSelected;

        public static DyeingRecipePanel Create(Transform parent, FactoryTileCatalog catalog)
        {
            var canvas_object = new GameObject(
                "Dyeing Recipe UI",
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

            var panel = canvas_object.AddComponent<DyeingRecipePanel>();
            panel.catalog = catalog;
            panel.Build();
            return panel;
        }

        public void Show(DyeingMachineState target)
        {
            machine = target;
            pending_recipe = target.SelectedRecipe;
            blocker.SetActive(true);
            RefreshSelection();
        }

        private void Build()
        {
            blocker = CreateObject("Blocker", transform);
            Stretch(blocker.GetComponent<RectTransform>());
            blocker.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.5f);

            var panel = CreateRounded("Panel", blocker.transform, PANEL_COLOR, 2f);
            var panel_rect = panel.GetComponent<RectTransform>();
            panel_rect.anchorMin = panel_rect.anchorMax = new Vector2(0.5f, 0.5f);
            panel_rect.sizeDelta = new Vector2(900f, 560f);

            CreateText("염색기", panel.transform, 34f, TextAlignmentOptions.Left,
                new Vector2(28f, -22f), new Vector2(760f, 52f));
            var close = CreateButton("닫기", panel.transform, "×", 40f);
            SetRect(close.GetComponent<RectTransform>(), new Vector2(842f, -18f), new Vector2(40f, 40f));
            close.GetComponent<Button>().onClick.AddListener(Close);

            var left = CreateRounded("Recipes", panel.transform, new Color(0.09f, 0.11f, 0.09f, 1f), 2.5f);
            SetRect(left.GetComponent<RectTransform>(), new Vector2(24f, -88f), new Vector2(548f, 380f));
            recipe_grid = left.transform;

            CreateRecipeCategory("달팽이", -10f);
            CreateRecipeCard(DyeingRecipe.All[DyeingRecipeId.SnailRed], 0, -46f);
            CreateRecipeCard(DyeingRecipe.All[DyeingRecipeId.SnailBlue], 1, -46f);

            CreateRecipeCategory("버섯", -126f);
            CreateRecipeCard(DyeingRecipe.All[DyeingRecipeId.MushroomBlue], 0, -162f);
            CreateRecipeCard(DyeingRecipe.All[DyeingRecipeId.MushroomOrange], 1, -162f);
            CreateRecipeCard(DyeingRecipe.All[DyeingRecipeId.MushroomGreen], 2, -162f);

            CreateRecipeCategory("뿔버섯", -242f);
            CreateRecipeCard(DyeingRecipe.All[DyeingRecipeId.SpikeMushroomBlue], 0, -278f);
            CreateRecipeCard(DyeingRecipe.All[DyeingRecipeId.SpikeMushroomOrange], 1, -278f);
            CreateRecipeCard(DyeingRecipe.All[DyeingRecipeId.SpikeMushroomGreen], 2, -278f);

            var details = CreateRounded("Details", panel.transform, new Color(0.09f, 0.11f, 0.09f, 1f), 2.5f);
            SetRect(details.GetComponent<RectTransform>(), new Vector2(592f, -88f), new Vector2(284f, 380f));
            CreateText("필요 재료", details.transform, 22f, TextAlignmentOptions.Left,
                new Vector2(18f, -16f), new Vector2(200f, 34f));
            base_icon = CreateItemRow(details.transform, 64f, out base_name);
            dye_icon = CreateItemRow(details.transform, 152f, out dye_name);
            result_icon = CreateItemRow(details.transform, 258f, out result_name);

            var confirm = CreateButton("Confirm", panel.transform, "확인", 25f);
            SetRect(confirm.GetComponent<RectTransform>(), new Vector2(592f, -488f), new Vector2(284f, 48f));
            confirm.GetComponent<Button>().onClick.AddListener(Confirm);
            blocker.SetActive(false);
        }

        private void CreateRecipeCategory(string label, float y)
        {
            CreateText(label, recipe_grid, 20f, TextAlignmentOptions.Left,
                new Vector2(14f, y), new Vector2(220f, 30f));
        }

        private void CreateRecipeCard(DyeingRecipe recipe, int column, float y)
        {
            var card = CreateRounded(recipe.DisplayName, recipe_grid, CARD_COLOR, 4f);
            var x = 14f + column * 174f;
            SetRect(card.GetComponent<RectTransform>(), new Vector2(x, y), new Vector2(164f, 64f));
            recipe_cards.Add(recipe, card.GetComponent<Image>());

            var icon = CreateObject("Icon", card.transform).AddComponent<Image>();
            icon.sprite = catalog.GetItemSprite(recipe.Result);
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            SetRect(icon.rectTransform, new Vector2(8f, -8f), new Vector2(48f, 48f));
            CreateText(recipe.DisplayName, card.transform, 15f, TextAlignmentOptions.MidlineLeft,
                new Vector2(62f, -8f), new Vector2(94f, 48f));
            card.AddComponent<Button>().onClick.AddListener(() => Select(recipe));
        }

        private Image CreateItemRow(Transform parent, float y, out TMP_Text label)
        {
            var row = CreateRounded("Item", parent, CARD_COLOR, 4f);
            SetRect(row.GetComponent<RectTransform>(), new Vector2(14f, -y), new Vector2(256f, 78f));
            var icon = CreateObject("Icon", row.transform).AddComponent<Image>();
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            SetRect(icon.rectTransform, new Vector2(10f, -10f), new Vector2(58f, 58f));
            label = CreateText("-", row.transform, 18f, TextAlignmentOptions.MidlineLeft,
                new Vector2(78f, -10f), new Vector2(168f, 58f));
            return icon;
        }

        private void Select(DyeingRecipe recipe)
        {
            pending_recipe = recipe;
            RefreshSelection();
        }

        private void RefreshSelection()
        {
            foreach (var pair in recipe_cards)
            {
                pair.Value.color = pair.Key == pending_recipe ? SELECTED_COLOR : CARD_COLOR;
            }

            if (pending_recipe == null)
            {
                base_icon.enabled = dye_icon.enabled = result_icon.enabled = false;
                base_name.text = dye_name.text = result_name.text = "레시피를 선택하세요";
                return;
            }

            SetItem(base_icon, base_name, pending_recipe.BaseMaterial);
            SetItem(dye_icon, dye_name, pending_recipe.Dye);
            SetItem(result_icon, result_name, pending_recipe.Result);
        }

        private void SetItem(Image icon, TMP_Text label, RawMaterialType material)
        {
            icon.enabled = true;
            icon.sprite = catalog.GetItemSprite(material);
            label.text = material.ToKoreanName() + "  × 1";
        }

        private void Confirm()
        {
            if (pending_recipe == null) return;
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
            var game_object = CreateRounded(name, parent, new Color(0.13f, 0.14f, 0.13f, 1f), 3.5f);
            var button = game_object.AddComponent<Button>();
            button.targetGraphic = game_object.GetComponent<Image>();
            var text = CreateText(label, game_object.transform, size, TextAlignmentOptions.Center,
                Vector2.zero, Vector2.zero);
            Stretch(text.rectTransform);
            return button;
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
