using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Maptory.Factory
{
    public sealed class FactoryHotbar : MonoBehaviour
    {
        private static readonly Color SLOT_COLOR = new(0.16f, 0.17f, 0.18f, 0.96f);
        private static readonly Color SELECTED_COLOR = new(0.94f, 0.83f, 0.32f, 1f);

        public event Action<FactoryBuildTool> ToolClicked;

        private readonly Dictionary<FactoryBuildTool, Image> tool_slots = new();

        public static FactoryHotbar Create(
            Transform parent,
            Sprite conveyor_icon,
            Sprite extractor_icon,
            Sprite dyeing_machine_icon,
            Sprite combiner_icon,
            Sprite erda_injector_icon,
            Sprite processing_machine_icon)
        {
            EnsureEventSystem();

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
            hotbar.Build(
                conveyor_icon,
                extractor_icon,
                dyeing_machine_icon,
                combiner_icon,
                erda_injector_icon,
                processing_machine_icon);
            return hotbar;
        }

        public void SetSelectedTool(FactoryBuildTool tool)
        {
            foreach (var pair in tool_slots)
            {
                pair.Value.color = tool == pair.Key ? SELECTED_COLOR : SLOT_COLOR;
            }
        }

        private void Build(
            Sprite conveyor_icon,
            Sprite extractor_icon,
            Sprite dyeing_machine_icon,
            Sprite combiner_icon,
            Sprite erda_injector_icon,
            Sprite processing_machine_icon)
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

            var tools = new[]
            {
                FactoryBuildTool.Conveyor,
                FactoryBuildTool.Extractor,
                FactoryBuildTool.DyeingMachine,
                FactoryBuildTool.Combiner,
                FactoryBuildTool.ErdaInjector,
                FactoryBuildTool.ProcessingMachine
            };
            var icons = new[]
            {
                conveyor_icon,
                extractor_icon,
                dyeing_machine_icon,
                combiner_icon,
                erda_injector_icon,
                processing_machine_icon
            };

            for (var index = 0; index < 10; index++)
            {
                CreateSlot(panel.transform, index, tools, icons);
            }
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

        private static void EnsureEventSystem()
        {
            if (EventSystem.current != null)
            {
                return;
            }

            var event_system = new GameObject("EventSystem", typeof(EventSystem));
            var input_module = event_system.AddComponent<InputSystemUIInputModule>();
            input_module.AssignDefaultActions();
        }

        private static GameObject CreateUiObject(string object_name, Transform parent)
        {
            var game_object = new GameObject(object_name, typeof(RectTransform));
            game_object.transform.SetParent(parent, false);
            return game_object;
        }
    }
}
