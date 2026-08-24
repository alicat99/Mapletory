using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Maptory.Factory
{
    public sealed class FactoryBuildingPortOverlay : MonoBehaviour
    {
        private readonly List<SpriteRenderer> markers = new();
        private Camera main_camera;
        private Grid grid;
        private Transform marker_root;
        private FactoryBuildMode build_mode;
        private ExtractionNetwork extraction_network;
        private Sprite input_icon;
        private Sprite output_icon;
        private Vector2Int last_preview_cell = new(int.MinValue, int.MinValue);
        private FactoryBuildTool last_preview_tool;
        private GridDirection last_preview_direction;

        public void Initialize(
            Camera camera,
            Grid map_grid,
            Transform world_root,
            FactoryBuildMode mode,
            ExtractionNetwork extraction)
        {
            main_camera = camera;
            grid = map_grid;
            build_mode = mode;
            extraction_network = extraction;
            marker_root = new GameObject("Building Port Markers").transform;
            marker_root.SetParent(world_root, false);
            input_icon = Resources.Load<Sprite>("Factory/BuildingPorts/InputIcon");
            output_icon = Resources.Load<Sprite>("Factory/BuildingPorts/OutputIcon");
            build_mode.Changed += _ => RefreshPreview(true);
            build_mode.Rotated += (_, _) => RefreshPreview(true);
        }

        private void Update()
        {
            if (Mouse.current == null) return;

            if (build_mode.ActiveTool == FactoryBuildTool.None)
            {
                Clear();
                return;
            }

            RefreshPreview(false);
        }

        private void RefreshPreview(bool force)
        {
            var tool = build_mode.ActiveTool;
            if (tool == FactoryBuildTool.None || tool == FactoryBuildTool.Conveyor)
            {
                Clear();
                return;
            }

            var cell = GetPointerCell();
            var direction = build_mode.GetDirection(tool);
            if (!force
                && cell == last_preview_cell
                && tool == last_preview_tool
                && direction == last_preview_direction)
            {
                return;
            }

            last_preview_cell = cell;
            last_preview_tool = tool;
            last_preview_direction = direction;
            Clear();
            switch (tool)
            {
                case FactoryBuildTool.Extractor:
                    ShowOutput(cell + direction.ToOffset() * 2, direction);
                    break;
                case FactoryBuildTool.DyeingMachine:
                    ShowRecipeMachine(new DyeingMachineState(cell, direction));
                    break;
                case FactoryBuildTool.Combiner:
                    ShowRecipeMachine(new CombinerState(cell, direction));
                    break;
                case FactoryBuildTool.ProcessingMachine:
                    ShowRecipeMachine(new ProcessingMachineState(cell, direction));
                    break;
                case FactoryBuildTool.ErdaInjector:
                    ShowInjector(new ErdaInjectorState(cell, direction));
                    break;
                case FactoryBuildTool.Portal:
                    ShowPortal(new PortalState(cell, extraction_network.PortalEconomy));
                    break;
            }
        }

        private void ShowRecipeMachine(IRecipeMachine machine)
        {
            var direction = GridDirectionExtensions.FromDelta(machine.Forward);
            for (var index = 0; index < machine.InputCount; index++)
            {
                ShowInput(machine.GetInputConveyorPosition(index), direction);
            }
            ShowOutput(machine.OutputConveyorPosition, direction);
        }

        private void ShowInjector(ErdaInjectorState injector)
        {
            ShowInput(injector.InputConveyorPosition, injector.Direction);
            ShowOutput(injector.OutputConveyorPosition, injector.Direction);
        }

        private void ShowPortal(PortalState portal)
        {
            foreach (var port in portal.InputPorts)
            {
                ShowInput(port.ConveyorPosition, port.Direction);
            }
        }

        private void ShowInput(Vector2Int position, GridDirection direction)
        {
            ShowMarker(position, input_icon, direction, 0f);
        }

        private void ShowOutput(Vector2Int position, GridDirection direction)
        {
            ShowMarker(position, output_icon, direction, 0f);
        }

        private void ShowMarker(
            Vector2Int position,
            Sprite sprite,
            GridDirection direction,
            float flow_offset)
        {
            var marker = new GameObject(sprite.name).AddComponent<SpriteRenderer>();
            marker.transform.SetParent(marker_root, false);
            var flow = GetWorldDirection(direction);
            marker.transform.localPosition = grid.GetCellCenterLocal((Vector3Int)position)
                + flow * flow_offset;
            marker.transform.localRotation = Quaternion.Euler(
                0f,
                0f,
                Mathf.Atan2(flow.y, flow.x) * Mathf.Rad2Deg - 26.565f);
            marker.transform.localScale = Vector3.one * 0.7f;
            marker.sprite = sprite;
            marker.sortingLayerName = FactorySorting.ITEM_SORTING_LAYER;
            marker.sortingOrder = 31000;
            markers.Add(marker);
        }

        private Vector3 GetWorldDirection(GridDirection direction)
        {
            var origin = grid.GetCellCenterLocal(Vector3Int.zero);
            var target = grid.GetCellCenterLocal((Vector3Int)direction.ToOffset());
            return (target - origin).normalized;
        }

        private Vector2Int GetPointerCell()
        {
            var world = main_camera.ScreenToWorldPoint(Mouse.current.position.value);
            var cell = grid.WorldToCell(world);
            return new Vector2Int(cell.x, cell.y);
        }

        private void Clear()
        {
            foreach (var marker in markers) Destroy(marker.gameObject);
            markers.Clear();
        }
    }
}
