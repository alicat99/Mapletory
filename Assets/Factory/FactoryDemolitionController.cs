using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Maptory.Factory
{
    public sealed class FactoryBuildingView : MonoBehaviour
    {
        public object State { get; private set; }

        public static void Attach(GameObject building_object, object state)
        {
            building_object.AddComponent<FactoryBuildingView>().State = state;
        }
    }

    public sealed class FactoryDemolitionController : MonoBehaviour
    {
        private Camera main_camera;
        private Grid grid;
        private Transform world_root;
        private FactoryBuildMode build_mode;
        private ConveyorNetwork conveyor_network;
        private ExtractionNetwork extraction_network;
        private ConveyorBuilder conveyor_builder;
        private FactoryItemTransport item_transport;
        private Vector2Int last_drag_cell;
        private bool has_last_drag_cell;

        public void Initialize(
            Camera camera,
            Grid map_grid,
            Transform root,
            FactoryBuildMode mode,
            ConveyorNetwork conveyors,
            ExtractionNetwork extraction,
            ConveyorBuilder builder,
            FactoryItemTransport transport)
        {
            main_camera = camera;
            grid = map_grid;
            world_root = root;
            build_mode = mode;
            conveyor_network = conveyors;
            extraction_network = extraction;
            conveyor_builder = builder;
            item_transport = transport;
        }

        private void Update()
        {
            if (!build_mode.IsDemolitionMode
                || Mouse.current == null
                || !Mouse.current.leftButton.isPressed)
            {
                has_last_drag_cell = false;
                return;
            }

            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                has_last_drag_cell = false;
                return;
            }

            var world = main_camera.ScreenToWorldPoint(Mouse.current.position.value);
            var cell = grid.WorldToCell(world);
            var current_cell = new Vector2Int(cell.x, cell.y);
            if (has_last_drag_cell)
            {
                DemolishLine(last_drag_cell, current_cell);
            }
            else
            {
                Demolish(current_cell);
            }

            last_drag_cell = current_cell;
            has_last_drag_cell = true;
        }

        public void DemolishLine(Vector2Int start, Vector2Int end)
        {
            foreach (var position in GetLineCells(start, end)) Demolish(position);
        }

        public static IReadOnlyList<Vector2Int> GetLineCells(Vector2Int start, Vector2Int end)
        {
            var cells = new List<Vector2Int>();
            var x = start.x;
            var y = start.y;
            var delta_x = Mathf.Abs(end.x - start.x);
            var delta_y = Mathf.Abs(end.y - start.y);
            var step_x = start.x < end.x ? 1 : -1;
            var step_y = start.y < end.y ? 1 : -1;
            var error = delta_x - delta_y;

            while (true)
            {
                cells.Add(new Vector2Int(x, y));
                if (x == end.x && y == end.y) return cells;

                var doubled_error = error * 2;
                if (doubled_error > -delta_y)
                {
                    error -= delta_y;
                    x += step_x;
                }

                if (doubled_error < delta_x)
                {
                    error += delta_x;
                    y += step_y;
                }
            }
        }

        public void Demolish(Vector2Int position)
        {
            if (conveyor_network.RemoveConveyor(position))
            {
                item_transport.RemoveItemsAt(position);
                conveyor_builder.RefreshConveyors();
                return;
            }

            var building = extraction_network.RemoveBuilding(position);
            if (building == null) return;

            foreach (var view in world_root.GetComponentsInChildren<FactoryBuildingView>(true))
            {
                if (!ReferenceEquals(view.State, building)) continue;

                Destroy(view.gameObject);
                return;
            }
        }
    }
}
