using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;

namespace Maptory.Factory
{
    public sealed class ConveyorBuilder : MonoBehaviour
    {
        private Camera main_camera;
        private Grid grid;
        private Transform conveyor_root;
        private Tilemap preview_tilemap;
        private FactoryBuildMode build_mode;
        private ConveyorNetwork conveyor_network;
        private FactoryTileCatalog tile_catalog;
        private readonly Dictionary<Vector2Int, SpriteRenderer> conveyor_renderers = new();
        private Vector2Int map_size;
        private Vector2Int drag_start;
        private Vector2Int drag_end;
        private bool is_dragging;

        public void Initialize(
            Camera camera,
            Grid map_grid,
            Transform conveyors,
            Tilemap preview,
            FactoryBuildMode mode,
            ConveyorNetwork network,
            FactoryTileCatalog catalog,
            Vector2Int size)
        {
            main_camera = camera;
            grid = map_grid;
            conveyor_root = conveyors;
            preview_tilemap = preview;
            build_mode = mode;
            conveyor_network = network;
            tile_catalog = catalog;
            map_size = size;
            build_mode.Changed += OnBuildToolChanged;
        }

        private void Update()
        {
            if (build_mode.ActiveTool != FactoryBuildTool.Conveyor || Mouse.current == null)
            {
                return;
            }

            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                BeginDrag();
            }

            if (is_dragging && Mouse.current.leftButton.isPressed)
            {
                UpdateDrag();
            }

            if (is_dragging && Mouse.current.leftButton.wasReleasedThisFrame)
            {
                CompleteDrag();
            }
        }

        private void BeginDrag()
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            var cell = GetPointerCell();
            if (!Contains(cell))
            {
                return;
            }

            drag_start = cell;
            drag_end = cell;
            is_dragging = true;
            DrawPreview();
        }

        private void UpdateDrag()
        {
            var pointer_cell = ClampToMap(GetPointerCell());
            var delta = pointer_cell - drag_start;
            drag_end = Mathf.Abs(delta.x) >= Mathf.Abs(delta.y)
                ? new Vector2Int(pointer_cell.x, drag_start.y)
                : new Vector2Int(drag_start.x, pointer_cell.y);
            DrawPreview();
        }

        private void CompleteDrag()
        {
            conveyor_network.PlaceLine(drag_start, drag_end);
            is_dragging = false;
            preview_tilemap.ClearAllTiles();
            DrawConveyors();
        }

        private void DrawPreview()
        {
            preview_tilemap.ClearAllTiles();
            var direction = GridDirectionExtensions.FromDelta(drag_end - drag_start);
            var offset = direction.ToOffset();
            var length = Mathf.Abs(drag_end.x - drag_start.x) + Mathf.Abs(drag_end.y - drag_start.y);

            for (var index = 0; index <= length; index++)
            {
                var output_code = index == length ? 'X' : direction.ToSpriteCode();
                var sprite_name = $"Conveyor{direction.ToSpriteCode()}{output_code}";
                var position = drag_start + offset * index;
                preview_tilemap.SetTile((Vector3Int)position, tile_catalog.GetConveyorTile(sprite_name));
            }
        }

        private void DrawConveyors()
        {
            foreach (var pair in conveyor_network.Conveyors)
            {
                if (!conveyor_renderers.TryGetValue(pair.Key, out var renderer))
                {
                    renderer = CreateConveyorRenderer(pair.Key);
                    conveyor_renderers.Add(pair.Key, renderer);
                }

                var sprite_name = conveyor_network.GetSpriteName(pair.Key);
                renderer.sprite = tile_catalog.GetConveyorSprite(sprite_name);
            }
        }

        private SpriteRenderer CreateConveyorRenderer(Vector2Int position)
        {
            var conveyor_object = new GameObject($"Conveyor ({position.x}, {position.y})");
            conveyor_object.transform.SetParent(conveyor_root, false);
            conveyor_object.transform.localPosition = grid.GetCellCenterLocal((Vector3Int)position);

            var renderer = conveyor_object.AddComponent<SpriteRenderer>();
            renderer.spriteSortPoint = SpriteSortPoint.Pivot;
            renderer.sortingOrder = FactorySorting.GetOrder(
                position,
                map_size,
                FactorySorting.CONVEYOR_LAYER);
            return renderer;
        }

        private Vector2Int GetPointerCell()
        {
            var screen_position = Mouse.current.position.value;
            var world_position = main_camera.ScreenToWorldPoint(screen_position);
            var cell = grid.WorldToCell(world_position);
            return new Vector2Int(cell.x, cell.y);
        }

        private Vector2Int ClampToMap(Vector2Int cell)
        {
            return new Vector2Int(
                Mathf.Clamp(cell.x, 0, map_size.x - 1),
                Mathf.Clamp(cell.y, 0, map_size.y - 1));
        }

        private bool Contains(Vector2Int cell)
        {
            return cell.x >= 0 && cell.x < map_size.x && cell.y >= 0 && cell.y < map_size.y;
        }

        private void OnBuildToolChanged(FactoryBuildTool tool)
        {
            if (tool == FactoryBuildTool.Conveyor)
            {
                return;
            }

            is_dragging = false;
            preview_tilemap.ClearAllTiles();
        }
    }
}
