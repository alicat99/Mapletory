using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;

namespace Maptory.Factory
{
    public enum FactoryDebugBrush
    {
        None,
        Grass01,
        Grass02,
        DepositDyeBlue,
        DepositDyeRed,
        DepositDyeYellow,
        DepositMushroom,
        DepositSnail,
        Erase,
        ClearItems
    }

    public sealed class FactoryDebugMapEditor : MonoBehaviour
    {
        private Camera main_camera;
        private Grid grid;
        private Tilemap ground_tilemap;
        private FactoryTileCatalog catalog;
        private FactoryBuildMode build_mode;
        private ExtractionNetwork extraction_network;
        private FactoryDemolitionController demolition;
        private FactoryItemTransport item_transport;
        private Vector2Int map_size;
        private string stage_id;
        private Vector2Int last_cell;
        private bool input_enabled;
        private bool has_last_cell;

        public FactoryDebugBrush ActiveBrush { get; private set; }
        public event Action<FactoryDebugBrush> BrushChanged;

        public void Initialize(
            Camera camera,
            Grid map_grid,
            Tilemap ground,
            FactoryTileCatalog tile_catalog,
            FactoryBuildMode mode,
            ExtractionNetwork extraction,
            FactoryDemolitionController demolition_controller,
            FactoryItemTransport transport,
            Vector2Int size,
            string current_stage_id)
        {
            main_camera = camera;
            grid = map_grid;
            ground_tilemap = ground;
            catalog = tile_catalog;
            build_mode = mode;
            extraction_network = extraction;
            demolition = demolition_controller;
            item_transport = transport;
            map_size = size;
            stage_id = current_stage_id;
            build_mode.Changed += OnBuildToolChanged;
            build_mode.DemolitionChanged += OnDemolitionChanged;
        }

        public void SetInputEnabled(bool enabled)
        {
            input_enabled = enabled;
            if (!enabled) has_last_cell = false;
        }

        public void SetBrush(FactoryDebugBrush brush)
        {
            build_mode.SetDemolitionMode(false);
            build_mode.SetActiveTool(FactoryBuildTool.None);
            ActiveBrush = brush;
            has_last_cell = false;
            BrushChanged?.Invoke(brush);
        }

        public void ClearAllItems()
        {
            item_transport.ClearItems();
        }

        public FactoryMapSettingsData CaptureSettings()
        {
            var settings = new FactoryMapSettingsData
            {
                stage_id = stage_id,
                width = map_size.x,
                height = map_size.y
            };
            for (var y = 0; y < map_size.y; y++)
            {
                for (var x = 0; x < map_size.x; x++)
                {
                    var tile = ground_tilemap.GetTile(new Vector3Int(x, y));
                    settings.grass_tiles.Add(tile == catalog.Grass02 ? 1 : 0);
                }
            }

            foreach (var deposit in extraction_network.Deposits)
            {
                var state = deposit.Value;
                settings.deposits.Add(new DepositSettingsData
                {
                    material = state.Material,
                    x = state.Center.x,
                    y = state.Center.y
                });
            }
            return settings;
        }

        private void Update()
        {
            if (!input_enabled
                || ActiveBrush == FactoryDebugBrush.None
                || Mouse.current == null
                || !Mouse.current.leftButton.isPressed)
            {
                has_last_cell = false;
                return;
            }

            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                has_last_cell = false;
                return;
            }

            var world = main_camera.ScreenToWorldPoint(Mouse.current.position.value);
            var cell = grid.WorldToCell(world);
            var position = new Vector2Int(cell.x, cell.y);
            if (!Contains(position) || (has_last_cell && position == last_cell)) return;

            ApplyBrush(position);
            last_cell = position;
            has_last_cell = true;
        }

        private void ApplyBrush(Vector2Int position)
        {
            switch (ActiveBrush)
            {
                case FactoryDebugBrush.Grass01:
                    ground_tilemap.SetTile((Vector3Int)position, catalog.Grass01);
                    break;
                case FactoryDebugBrush.Grass02:
                    ground_tilemap.SetTile((Vector3Int)position, catalog.Grass02);
                    break;
                case FactoryDebugBrush.DepositDyeBlue:
                    PlaceDeposit(RawMaterialType.DyeBlue, position);
                    break;
                case FactoryDebugBrush.DepositDyeRed:
                    PlaceDeposit(RawMaterialType.DyeRed, position);
                    break;
                case FactoryDebugBrush.DepositDyeYellow:
                    PlaceDeposit(RawMaterialType.DyeYellow, position);
                    break;
                case FactoryDebugBrush.DepositMushroom:
                    PlaceDeposit(RawMaterialType.Mushroom, position);
                    break;
                case FactoryDebugBrush.DepositSnail:
                    PlaceDeposit(RawMaterialType.Snail, position);
                    break;
                case FactoryDebugBrush.Erase:
                    demolition.Demolish(position);
                    extraction_network.RemoveDeposit(position);
                    break;
                case FactoryDebugBrush.ClearItems:
                    item_transport.RemoveItemsAt(position);
                    break;
            }
        }

        private void PlaceDeposit(RawMaterialType material, Vector2Int center)
        {
            if (!ContainsDeposit(center) || !extraction_network.CanPlaceDeposit(center)) return;

            extraction_network.PlaceDeposit(material, center);
        }

        private bool Contains(Vector2Int position)
        {
            return position.x >= 0 && position.x < map_size.x
                && position.y >= 0 && position.y < map_size.y;
        }

        private bool ContainsDeposit(Vector2Int center)
        {
            return center.x >= 1 && center.x < map_size.x - 1
                && center.y >= 1 && center.y < map_size.y - 1;
        }

        private void OnBuildToolChanged(FactoryBuildTool tool)
        {
            if (tool == FactoryBuildTool.None || ActiveBrush == FactoryDebugBrush.None) return;

            ActiveBrush = FactoryDebugBrush.None;
            BrushChanged?.Invoke(ActiveBrush);
        }

        private void OnDemolitionChanged(bool active)
        {
            if (!active || ActiveBrush == FactoryDebugBrush.None) return;

            ActiveBrush = FactoryDebugBrush.None;
            BrushChanged?.Invoke(ActiveBrush);
        }
    }
}
