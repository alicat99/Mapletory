using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Maptory.Factory
{
    public sealed class FactoryHotbar : MonoBehaviour
    {
        private static readonly Color SLOT_COLOR = new(0.16f, 0.17f, 0.18f, 0.96f);
        private static readonly Color SELECTED_COLOR = new(0.94f, 0.83f, 0.32f, 1f);
        private const float DEMOLITION_ALPHA = 0.35f;

        public event Action<FactoryBuildTool> ToolClicked;

        private readonly Dictionary<FactoryBuildTool, Image> tool_slots = new();
        private readonly List<FactoryBuildTool> slot_tools = new();
        private CanvasGroup canvas_group;

        public static FactoryHotbar Create(
            Transform parent,
            Sprite conveyor_icon,
            Sprite extractor_icon,
            Sprite erda_injector_icon,
            Sprite dyeing_machine_icon,
            Sprite combiner_icon,
            Sprite processing_machine_icon,
            Sprite portal_icon)
        {
            FactoryUiEventSystem.EnsureExists();

            var canvas_object = new GameObject(
                "Factory Hotbar",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            canvas_object.transform.SetParent(parent, false);

            var canvas = canvas_object.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            var scaler = canvas_object.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.matchWidthOrHeight = 1f;

            var hotbar = canvas_object.AddComponent<FactoryHotbar>();
            hotbar.canvas_group = canvas_object.AddComponent<CanvasGroup>();
            hotbar.Build(
                conveyor_icon,
                extractor_icon,
                erda_injector_icon,
                dyeing_machine_icon,
                combiner_icon,
                processing_machine_icon,
                portal_icon);
            return hotbar;
        }

        public void SetSelectedTool(FactoryBuildTool tool)
        {
            foreach (var pair in tool_slots)
            {
                pair.Value.color = tool == pair.Key ? SELECTED_COLOR : SLOT_COLOR;
            }
        }

        public void SetDemolitionMode(bool active)
        {
            canvas_group.alpha = active ? DEMOLITION_ALPHA : 1f;
            canvas_group.interactable = !active;
            canvas_group.blocksRaycasts = !active;
        }

        public void SelectSlot(int index)
        {
            if (index < 0 || index >= slot_tools.Count) return;

            var tool = slot_tools[index];
            if (tool != FactoryBuildTool.None) ToolClicked?.Invoke(tool);
        }

        private void Update()
        {
            if (Keyboard.current == null || IsEditingText()) return;

            var index = ReadNumberKey();
            if (index >= 0) SelectSlot(index);
        }

        private void Build(
            Sprite conveyor_icon,
            Sprite extractor_icon,
            Sprite erda_injector_icon,
            Sprite dyeing_machine_icon,
            Sprite combiner_icon,
            Sprite processing_machine_icon,
            Sprite portal_icon)
        {
            var panel = CreateUiObject("Slots", transform);
            var panel_rect = panel.GetComponent<RectTransform>();
            panel_rect.anchorMin = new Vector2(0.5f, 0f);
            panel_rect.anchorMax = new Vector2(0.5f, 0f);
            panel_rect.pivot = new Vector2(0.5f, 0f);
            panel_rect.anchoredPosition = new Vector2(0f, 18f);
            panel_rect.sizeDelta = new Vector2(656f, 68f);

            var background = panel.AddComponent<Image>();
            background.color = new Color(0.05f, 0.05f, 0.055f, 0.92f);

            var layout = panel.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(4, 4, 4, 4);
            layout.spacing = 4f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlHeight = false;
            layout.childControlWidth = false;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = false;

            var tools = FactoryContentCatalog.Buildings
                .Select(building => building.Tool)
                .ToArray();
            var icons = new[]
            {
                conveyor_icon,
                extractor_icon,
                erda_injector_icon,
                dyeing_machine_icon,
                combiner_icon,
                processing_machine_icon,
                portal_icon
            };

            for (var index = 0; index < 10; index++)
            {
                slot_tools.Add(index < tools.Length ? tools[index] : FactoryBuildTool.None);
                CreateSlot(panel.transform, index, tools, icons);
            }
        }

        private static int ReadNumberKey()
        {
            if (Keyboard.current.digit1Key.wasPressedThisFrame) return 0;
            if (Keyboard.current.digit2Key.wasPressedThisFrame) return 1;
            if (Keyboard.current.digit3Key.wasPressedThisFrame) return 2;
            if (Keyboard.current.digit4Key.wasPressedThisFrame) return 3;
            if (Keyboard.current.digit5Key.wasPressedThisFrame) return 4;
            if (Keyboard.current.digit6Key.wasPressedThisFrame) return 5;
            if (Keyboard.current.digit7Key.wasPressedThisFrame) return 6;
            if (Keyboard.current.digit8Key.wasPressedThisFrame) return 7;
            if (Keyboard.current.digit9Key.wasPressedThisFrame) return 8;
            if (Keyboard.current.digit0Key.wasPressedThisFrame) return 9;
            return -1;
        }

        private static bool IsEditingText()
        {
            var selected = EventSystem.current == null
                ? null
                : EventSystem.current.currentSelectedGameObject;
            return selected != null && selected.GetComponent<TMP_InputField>() != null;
        }

        private void CreateSlot(
            Transform parent,
            int index,
            IReadOnlyList<FactoryBuildTool> tools,
            IReadOnlyList<Sprite> icons)
        {
            var slot = CreateUiObject($"Slot {index + 1}", parent);
            var rect = slot.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(60f, 60f);

            var slot_image = slot.AddComponent<Image>();
            slot_image.color = SLOT_COLOR;

            var inner = CreateUiObject("Inner", slot.transform);
            var inner_rect = inner.GetComponent<RectTransform>();
            inner_rect.anchorMin = Vector2.zero;
            inner_rect.anchorMax = Vector2.one;
            inner_rect.offsetMin = new Vector2(4f, 4f);
            inner_rect.offsetMax = new Vector2(-4f, -4f);
            var inner_image = inner.AddComponent<Image>();
            inner_image.color = new Color(0.28f, 0.29f, 0.3f, 1f);
            inner_image.raycastTarget = false;

            if (index >= tools.Count)
            {
                return;
            }

            var tool = tools[index];
            var icon = CreateUiObject($"{tool} Icon", inner.transform);
            var icon_rect = icon.GetComponent<RectTransform>();
            icon_rect.anchorMin = new Vector2(0.5f, 0.5f);
            icon_rect.anchorMax = new Vector2(0.5f, 0.5f);
            icon_rect.sizeDelta = new Vector2(42f, 42f);
            var icon_image = icon.AddComponent<Image>();
            icon_image.sprite = icons[index];
            icon_image.preserveAspect = true;
            icon_image.raycastTarget = false;

            var button = slot.AddComponent<Button>();
            button.targetGraphic = slot_image;
            button.onClick.AddListener(() => ToolClicked?.Invoke(tool));

            tool_slots.Add(tool, slot_image);
        }

        private static GameObject CreateUiObject(string object_name, Transform parent)
        {
            var game_object = new GameObject(object_name, typeof(RectTransform));
            game_object.transform.SetParent(parent, false);
            return game_object;
        }
    }
}
