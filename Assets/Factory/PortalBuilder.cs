using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Maptory.Factory
{
    public sealed class PortalBuilder : MonoBehaviour
    {
        private static readonly Color VALID_GHOST_COLOR = new(1f, 1f, 1f, 0.6f);
        private static readonly Color INVALID_GHOST_COLOR = new(1f, 0.35f, 0.35f, 0.45f);

        private Camera main_camera;
        private Grid grid;
        private Transform world_root;
        private FactoryBuildMode build_mode;
        private ExtractionNetwork extraction_network;
        private FactoryTileCatalog tile_catalog;
        private PortalSelectionPanel selection_panel;
        private Vector2Int map_size;
        private SpriteRenderer ghost_renderer;

        public void Initialize(
            Camera camera,
            Grid map_grid,
            Transform root,
            FactoryBuildMode mode,
            ExtractionNetwork network,
            FactoryTileCatalog catalog,
            PortalSelectionPanel panel,
            Vector2Int size)
        {
            main_camera = camera;
            grid = map_grid;
            world_root = root;
            build_mode = mode;
            extraction_network = network;
            tile_catalog = catalog;
            selection_panel = panel;
            map_size = size;
            CreateGhost();
            build_mode.Changed += OnBuildToolChanged;
        }

        private void Update()
        {
            if (Mouse.current == null) return;

            if (build_mode.ActiveTool == FactoryBuildTool.Portal)
            {
                UpdateConstruction();
                return;
            }

            ghost_renderer.enabled = false;
            if (build_mode.ActiveTool == FactoryBuildTool.None
                && !build_mode.IsDemolitionMode
                && Mouse.current.leftButton.wasPressedThisFrame
                && (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject()))
            {
                var portal = extraction_network.FindPortal(GetPointerCell());
                if (portal != null)
                {
                    selection_panel.Show(portal);
                }
            }
        }

        private void UpdateConstruction()
        {
            var anchor = GetPointerCell();
            ghost_renderer.enabled = ContainsFootprintAndInputs(anchor);
            if (!ghost_renderer.enabled) return;

            ghost_renderer.transform.localPosition = GetVisualCenter(anchor);
            ghost_renderer.color = extraction_network.CanPlacePortal(anchor)
                ? VALID_GHOST_COLOR
                : INVALID_GHOST_COLOR;
            ghost_renderer.sortingOrder = FactorySorting.GetOrder(
                anchor + new Vector2(0.5f, 0.5f),
                map_size,
                FactorySorting.ITEM_LAYER);

            if (Mouse.current.leftButton.wasPressedThisFrame
                && (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject())
                && extraction_network.CanPlacePortal(anchor))
            {
                Place(anchor);
            }
        }

        private void Place(Vector2Int anchor)
        {
            var portal = extraction_network.PlacePortal(anchor);
            var portal_object = new GameObject($"Portal ({anchor.x}, {anchor.y})");
            portal_object.transform.SetParent(world_root, false);
            portal_object.transform.localPosition = GetVisualCenter(anchor);
            FactoryBuildingView.Attach(portal_object, portal);
            CreatePart(
                portal_object.transform,
                "Lower",
                tile_catalog.GetPortalLowerSprite(),
                FactorySorting.CONVEYOR_SORTING_LAYER,
                portal.VisualCenter);
            CreatePart(
                portal_object.transform,
                "Upper",
                tile_catalog.GetPortalUpperSprite(),
                FactorySorting.ITEM_SORTING_LAYER,
                portal.VisualCenter);
            PortalTooltip.Create(
                portal_object.transform,
                tile_catalog,
                portal,
                extraction_network.PortalEconomy,
                map_size);
            ghost_renderer.enabled = false;
        }

        private void CreateGhost()
        {
            var ghost_object = new GameObject("Portal Ghost");
            ghost_object.transform.SetParent(world_root, false);
            ghost_renderer = ghost_object.AddComponent<SpriteRenderer>();
            ghost_renderer.sprite = tile_catalog.GetPortalSprite();
            ghost_renderer.spriteSortPoint = SpriteSortPoint.Pivot;
            ghost_renderer.sortingLayerName = FactorySorting.ITEM_SORTING_LAYER;
            ghost_renderer.enabled = false;
        }

        private void CreatePart(
            Transform parent,
            string name,
            Sprite sprite,
            string sorting_layer,
            Vector2 center)
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

        private Vector3 GetVisualCenter(Vector2Int anchor)
        {
            var first = grid.GetCellCenterLocal((Vector3Int)anchor);
            var opposite = grid.GetCellCenterLocal((Vector3Int)(anchor + Vector2Int.one));
            return (first + opposite) * 0.5f;
        }

        private bool ContainsFootprintAndInputs(Vector2Int anchor)
        {
            return anchor.x >= 1 && anchor.x < map_size.x - 2
                && anchor.y >= 1 && anchor.y < map_size.y - 2;
        }

        private void OnBuildToolChanged(FactoryBuildTool tool)
        {
            if (tool != FactoryBuildTool.Portal) ghost_renderer.enabled = false;
        }
    }
}
