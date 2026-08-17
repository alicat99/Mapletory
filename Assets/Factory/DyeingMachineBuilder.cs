using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Maptory.Factory
{
    public sealed class DyeingMachineBuilder : MonoBehaviour
    {
        private static readonly Color VALID_GHOST_COLOR = new(1f, 1f, 1f, 0.6f);
        private static readonly Color INVALID_GHOST_COLOR = new(1f, 0.35f, 0.35f, 0.45f);

        private readonly Dictionary<DyeingMachineState, TextMeshPro> tooltips = new();

        private Camera main_camera;
        private Grid grid;
        private Transform world_root;
        private FactoryBuildMode build_mode;
        private ExtractionNetwork extraction_network;
        private FactoryTileCatalog tile_catalog;
        private DyeingRecipePanel recipe_panel;
        private Vector2Int map_size;
        private GridDirection direction = GridDirection.Up;
        private SpriteRenderer ghost_renderer;

        public void Initialize(
            Camera camera,
            Grid map_grid,
            Transform root,
            FactoryBuildMode mode,
            ExtractionNetwork network,
            FactoryTileCatalog catalog,
            DyeingRecipePanel panel,
            Vector2Int size)
        {
            main_camera = camera;
            grid = map_grid;
            world_root = root;
            build_mode = mode;
            extraction_network = network;
            tile_catalog = catalog;
            recipe_panel = panel;
            map_size = size;
            CreateGhost();
            build_mode.Changed += OnBuildToolChanged;
            recipe_panel.RecipeSelected += OnRecipeSelected;
        }

        private void Update()
        {
            if (Mouse.current == null) return;

            if (build_mode.ActiveTool == FactoryBuildTool.DyeingMachine)
            {
                UpdateConstruction();
                return;
            }

            ghost_renderer.enabled = false;
            if (build_mode.ActiveTool == FactoryBuildTool.None
                && Mouse.current.leftButton.wasPressedThisFrame
                && (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject()))
            {
                var machine = extraction_network.FindDyeingMachine(GetPointerCell());
                if (machine != null) recipe_panel.Show(machine);
            }
        }

        private void UpdateConstruction()
        {
            if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
            {
                direction = direction.RotateCounterClockwise();
            }

            var center = GetPointerCell();
            ghost_renderer.enabled = ContainsFootprint(center);
            if (!ghost_renderer.enabled) return;

            ghost_renderer.transform.localPosition = grid.GetCellCenterLocal((Vector3Int)center);
            ghost_renderer.sprite = tile_catalog.GetDyeingMachineSprite(direction);
            ghost_renderer.color = extraction_network.CanPlaceDyeingMachine(center)
                ? VALID_GHOST_COLOR
                : INVALID_GHOST_COLOR;
            ghost_renderer.sortingOrder = FactorySorting.GetOrder(center, map_size, FactorySorting.ITEM_LAYER);

            if (Mouse.current.leftButton.wasPressedThisFrame
                && (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject())
                && extraction_network.CanPlaceDyeingMachine(center))
            {
                Place(center);
            }
        }

        private void Place(Vector2Int center)
        {
            var machine = extraction_network.PlaceDyeingMachine(center, direction);
            var machine_object = new GameObject($"Dyeing Machine ({center.x}, {center.y})");
            machine_object.transform.SetParent(world_root, false);
            machine_object.transform.localPosition = grid.GetCellCenterLocal((Vector3Int)center);
            CreatePart(machine_object.transform, "Lower",
                tile_catalog.GetDyeingMachineLowerSprite(direction),
                FactorySorting.CONVEYOR_SORTING_LAYER, center);
            CreatePart(machine_object.transform, "Upper",
                tile_catalog.GetDyeingMachineUpperSprite(direction),
                FactorySorting.ITEM_SORTING_LAYER, center);
            CreateTooltip(machine, machine_object.transform, center);
            ghost_renderer.enabled = false;
        }

        private void CreateTooltip(DyeingMachineState machine, Transform parent, Vector2Int center)
        {
            var tooltip_object = new GameObject("Recipe Tooltip");
            tooltip_object.transform.SetParent(parent, false);
            tooltip_object.transform.localPosition = new Vector3(0f, 0.85f, -0.1f);
            tooltip_object.transform.localScale = Vector3.one * 0.08f;
            var tooltip = tooltip_object.AddComponent<TextMeshPro>();
            tooltip.font = tile_catalog.UiFont;
            tooltip.text = "(레시피 선택)";
            tooltip.fontSize = 4f;
            tooltip.alignment = TextAlignmentOptions.Center;
            tooltip.color = Color.white;
            tooltip.rectTransform.sizeDelta = new Vector2(12f, 2f);
            tooltip.renderer.sortingLayerName = FactorySorting.ITEM_SORTING_LAYER;
            tooltip.renderer.sortingOrder = FactorySorting.GetOrder(center, map_size, FactorySorting.ITEM_LAYER) + 1;
            tooltips.Add(machine, tooltip);
        }

        private void CreateGhost()
        {
            var ghost_object = new GameObject("Dyeing Machine Ghost");
            ghost_object.transform.SetParent(world_root, false);
            ghost_renderer = ghost_object.AddComponent<SpriteRenderer>();
            ghost_renderer.spriteSortPoint = SpriteSortPoint.Pivot;
            ghost_renderer.sortingLayerName = FactorySorting.ITEM_SORTING_LAYER;
            ghost_renderer.enabled = false;
        }

        private void CreatePart(
            Transform parent,
            string name,
            Sprite sprite,
            string sorting_layer,
            Vector2Int center)
        {
            var part = new GameObject(name);
            part.transform.SetParent(parent, false);
            var renderer = part.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.spriteSortPoint = SpriteSortPoint.Pivot;
            renderer.sortingLayerName = sorting_layer;
            renderer.sortingOrder = FactorySorting.GetOrder(center, map_size, FactorySorting.EXTRACTOR_LAYER);
        }

        private Vector2Int GetPointerCell()
        {
            var world = main_camera.ScreenToWorldPoint(Mouse.current.position.value);
            var cell = grid.WorldToCell(world);
            return new Vector2Int(cell.x, cell.y);
        }

        private bool ContainsFootprint(Vector2Int center)
        {
            return center.x >= 1 && center.x < map_size.x - 1
                && center.y >= 1 && center.y < map_size.y - 1;
        }

        private void OnBuildToolChanged(FactoryBuildTool tool)
        {
            if (tool != FactoryBuildTool.DyeingMachine) ghost_renderer.enabled = false;
        }

        private void OnRecipeSelected(DyeingMachineState machine)
        {
            tooltips[machine].gameObject.SetActive(false);
        }
    }
}
