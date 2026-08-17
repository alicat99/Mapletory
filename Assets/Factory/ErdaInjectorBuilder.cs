using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Maptory.Factory
{
    public sealed class ErdaInjectorBuilder : MonoBehaviour
    {
        private static readonly Color VALID_GHOST_COLOR = new(1f, 1f, 1f, 0.6f);
        private static readonly Color INVALID_GHOST_COLOR = new(1f, 0.35f, 0.35f, 0.45f);

        private Camera main_camera;
        private Grid grid;
        private Transform world_root;
        private FactoryBuildMode build_mode;
        private ExtractionNetwork extraction_network;
        private FactoryTileCatalog tile_catalog;
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
            Vector2Int size)
        {
            main_camera = camera;
            grid = map_grid;
            world_root = root;
            build_mode = mode;
            extraction_network = network;
            tile_catalog = catalog;
            map_size = size;
            CreateGhost();
            build_mode.Changed += OnBuildToolChanged;
        }

        private void Update()
        {
            if (build_mode.ActiveTool != FactoryBuildTool.ErdaInjector || Mouse.current == null)
            {
                ghost_renderer.enabled = false;
                return;
            }

            if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
            {
                direction = direction.RotateCounterClockwise();
            }

            var center = GetPointerCell();
            ghost_renderer.enabled = ContainsPorts(center);
            if (!ghost_renderer.enabled) return;

            ghost_renderer.transform.localPosition = grid.GetCellCenterLocal((Vector3Int)center);
            ghost_renderer.sprite = tile_catalog.GetErdaInjectorSprite(direction);
            ghost_renderer.color = extraction_network.CanPlaceErdaInjector(center)
                ? VALID_GHOST_COLOR
                : INVALID_GHOST_COLOR;
            ghost_renderer.sortingOrder = FactorySorting.GetOrder(
                center,
                map_size,
                FactorySorting.ITEM_LAYER);

            if (Mouse.current.leftButton.wasPressedThisFrame
                && (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject())
                && extraction_network.CanPlaceErdaInjector(center))
            {
                Place(center);
            }
        }

        private void Place(Vector2Int center)
        {
            var injector = extraction_network.PlaceErdaInjector(center, direction);
            var injector_object = new GameObject($"Erda Injector ({center.x}, {center.y})");
            injector_object.transform.SetParent(world_root, false);
            injector_object.transform.localPosition = grid.GetCellCenterLocal((Vector3Int)center);
            FactoryBuildingView.Attach(injector_object, injector);
            CreatePart(injector_object.transform, "Lower",
                tile_catalog.GetErdaInjectorLowerSprite(direction),
                FactorySorting.CONVEYOR_SORTING_LAYER, center);
            CreatePart(injector_object.transform, "Upper",
                tile_catalog.GetErdaInjectorUpperSprite(direction),
                FactorySorting.ITEM_SORTING_LAYER, center);
            ghost_renderer.enabled = false;
        }

        private void CreateGhost()
        {
            var ghost_object = new GameObject("Erda Injector Ghost");
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
            renderer.sortingOrder = FactorySorting.GetOrder(
                center,
                map_size,
                FactorySorting.EXTRACTOR_LAYER);
        }

        private Vector2Int GetPointerCell()
        {
            var world = main_camera.ScreenToWorldPoint(Mouse.current.position.value);
            var cell = grid.WorldToCell(world);
            return new Vector2Int(cell.x, cell.y);
        }

        private bool ContainsPorts(Vector2Int center)
        {
            var forward = direction.ToOffset();
            return IsInsideMap(center - forward) && IsInsideMap(center + forward);
        }

        private bool IsInsideMap(Vector2Int position)
        {
            return position.x >= 0 && position.x < map_size.x
                && position.y >= 0 && position.y < map_size.y;
        }

        private void OnBuildToolChanged(FactoryBuildTool tool)
        {
            if (tool != FactoryBuildTool.ErdaInjector) ghost_renderer.enabled = false;
        }
    }
}
