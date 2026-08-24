using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Maptory.Factory
{
    public sealed class ExtractorBuilder : MonoBehaviour
    {
        private static readonly Color VALID_GHOST_COLOR = new(1f, 1f, 1f, 0.6f);
        private static readonly Color INVALID_GHOST_COLOR = new(1f, 0.35f, 0.35f, 0.45f);
        private const int DEPOSIT_VISUAL_SPACING = 3;

        private readonly Dictionary<RawMaterialDeposit, GameObject> deposit_views = new();

        private Camera main_camera;
        private Grid grid;
        private Transform world_root;
        private FactoryBuildMode build_mode;
        private ExtractionNetwork extraction_network;
        private FactoryTileCatalog tile_catalog;
        private Vector2Int map_size;
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

            DrawDeposits();
            DrawExtractors();
            CreateGhost();
            build_mode.Changed += OnBuildToolChanged;
            extraction_network.DepositPlaced += DrawDeposit;
            extraction_network.DepositRemoved += RemoveDepositView;
        }

        private void Update()
        {
            if (build_mode.ActiveTool != FactoryBuildTool.Extractor || Mouse.current == null)
            {
                ghost_renderer.enabled = false;
                return;
            }

            var center = GetPointerCell();
            DrawGhost(center);

            if (Mouse.current.leftButton.wasPressedThisFrame
                && (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject())
                && extraction_network.CanPlaceExtractor(center))
            {
                PlaceExtractor(center);
            }
        }

        private void DrawDeposits()
        {
            foreach (var deposit in extraction_network.Deposits.Values)
            {
                DrawDeposit(deposit);
            }
        }

        private void DrawDeposit(RawMaterialDeposit deposit)
        {
            var deposit_object = new GameObject($"Raw Material {deposit.Material}");
            deposit_object.transform.SetParent(world_root, false);
            deposit_object.transform.localPosition = grid.GetCellCenterLocal((Vector3Int)deposit.Center);
            for (var y = -1; y <= 1; y++)
            {
                for (var x = -1; x <= 1; x++)
                {
                    var cell = deposit.Center + new Vector2Int(
                        x * DEPOSIT_VISUAL_SPACING,
                        y * DEPOSIT_VISUAL_SPACING);
                    CreateDepositPart(
                        deposit_object.transform,
                        $"Lower ({x}, {y})",
                        tile_catalog.GetRawMaterialLowerSprite(deposit.Material),
                        FactorySorting.CONVEYOR_SORTING_LAYER,
                        deposit.Center,
                        cell);
                    CreateDepositPart(
                        deposit_object.transform,
                        $"Upper ({x}, {y})",
                        tile_catalog.GetRawMaterialUpperSprite(deposit.Material),
                        FactorySorting.ITEM_SORTING_LAYER,
                        deposit.Center,
                        cell);
                }
            }
            deposit_views.Add(deposit, deposit_object);
        }

        private void DrawExtractors()
        {
            foreach (var extractor in extraction_network.Extractors.Values)
            {
                DrawExtractor(extractor);
            }
        }

        private void CreateDepositPart(
            Transform deposit,
            string part_name,
            Sprite sprite,
            string sorting_layer,
            Vector2Int center,
            Vector2Int cell)
        {
            var part_object = new GameObject(part_name);
            part_object.transform.SetParent(deposit, false);
            part_object.transform.localPosition = grid.GetCellCenterLocal((Vector3Int)cell)
                - grid.GetCellCenterLocal((Vector3Int)center);
            var renderer = part_object.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.spriteSortPoint = SpriteSortPoint.Pivot;
            renderer.sortingLayerName = sorting_layer;
            renderer.sortingOrder = FactorySorting.GetOrder(
                cell,
                map_size,
                FactorySorting.RESOURCE_LAYER);
        }

        private void RemoveDepositView(RawMaterialDeposit deposit)
        {
            if (!deposit_views.Remove(deposit, out var view)) return;

            Destroy(view);
        }

        private void CreateGhost()
        {
            var ghost_object = new GameObject("Extractor Ghost");
            ghost_object.transform.SetParent(world_root, false);
            ghost_renderer = ghost_object.AddComponent<SpriteRenderer>();
            ghost_renderer.spriteSortPoint = SpriteSortPoint.Pivot;
            ghost_renderer.sortingLayerName = FactorySorting.ITEM_SORTING_LAYER;
            ghost_renderer.enabled = false;
        }

        private void DrawGhost(Vector2Int center)
        {
            ghost_renderer.enabled = ContainsFootprint(center);
            if (!ghost_renderer.enabled)
            {
                return;
            }

            ghost_renderer.transform.localPosition = grid.GetCellCenterLocal((Vector3Int)center);
            ghost_renderer.sprite = tile_catalog.GetExtractorSprite(
                build_mode.GetDirection(FactoryBuildTool.Extractor));
            ghost_renderer.color = extraction_network.CanPlaceExtractor(center)
                ? VALID_GHOST_COLOR
                : INVALID_GHOST_COLOR;
            ghost_renderer.sortingOrder = FactorySorting.GetOrder(
                center,
                map_size,
                FactorySorting.ITEM_LAYER);
        }

        private void PlaceExtractor(Vector2Int center)
        {
            var extractor = extraction_network.PlaceExtractor(
                center,
                build_mode.GetDirection(FactoryBuildTool.Extractor));
            DrawExtractor(extractor);
            ghost_renderer.enabled = false;
        }

        private void DrawExtractor(ExtractorState extractor)
        {
            var center = extractor.Center;
            var extractor_object = new GameObject($"Extractor ({center.x}, {center.y})");
            extractor_object.transform.SetParent(world_root, false);
            extractor_object.transform.localPosition = grid.GetCellCenterLocal((Vector3Int)center);
            FactoryBuildingView.Attach(extractor_object, extractor);
            CreateExtractorPart(
                extractor_object.transform,
                "Lower",
                tile_catalog.GetExtractorLowerSprite(extractor.Direction),
                FactorySorting.CONVEYOR_SORTING_LAYER,
                center);
            CreateExtractorPart(
                extractor_object.transform,
                "Upper",
                tile_catalog.GetExtractorUpperSprite(extractor.Direction),
                FactorySorting.ITEM_SORTING_LAYER,
                center);
        }

        private void CreateExtractorPart(
            Transform extractor,
            string part_name,
            Sprite sprite,
            string sorting_layer,
            Vector2Int center)
        {
            var part_object = new GameObject(part_name);
            part_object.transform.SetParent(extractor, false);
            var renderer = part_object.AddComponent<SpriteRenderer>();
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
            var world_position = main_camera.ScreenToWorldPoint(Mouse.current.position.value);
            var cell = grid.WorldToCell(world_position);
            return new Vector2Int(cell.x, cell.y);
        }

        private bool ContainsFootprint(Vector2Int center)
        {
            return center.x >= 1 && center.x < map_size.x - 1
                && center.y >= 1 && center.y < map_size.y - 1;
        }

        private void OnBuildToolChanged(FactoryBuildTool tool)
        {
            if (tool != FactoryBuildTool.Extractor)
            {
                ghost_renderer.enabled = false;
            }
        }
    }
}
