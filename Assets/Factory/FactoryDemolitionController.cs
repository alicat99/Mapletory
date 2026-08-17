using System;
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
                || !Mouse.current.leftButton.wasPressedThisFrame
                || (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())) return;

            var world = main_camera.ScreenToWorldPoint(Mouse.current.position.value);
            var cell = grid.WorldToCell(world);
            Demolish(new Vector2Int(cell.x, cell.y));
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
