using UnityEngine;
using UnityEngine.Tilemaps;

namespace Maptory.Factory
{
    [DefaultExecutionOrder(-100)]
    public sealed class FactoryGame : MonoBehaviour
    {
        [SerializeField] private int map_width = 50;
        [SerializeField] private int map_height = 50;
        [SerializeField] private int grass_seed = 74021;

        private Grid grid;
        private Tilemap ground_tilemap;
        private Transform world_root;
        private Transform conveyor_root;
        private Transform item_root;
        private Tilemap preview_tilemap;
        private ConveyorNetwork conveyor_network;
        private ExtractionNetwork extraction_network;
        private FactoryTileCatalog tile_catalog;
        private ConveyorBuilder conveyor_builder;

        private void Awake()
        {
            tile_catalog = new FactoryTileCatalog();
            conveyor_network = new ConveyorNetwork();
            extraction_network = CreateExtractionNetwork();

            CreateMap();
            FillGround();
            CreateConstructionControls();
            ConfigureCamera();
        }

        private void CreateMap()
        {
            var grid_object = new GameObject("Isometric Grid");
            grid_object.transform.SetParent(transform, false);
            grid = grid_object.AddComponent<Grid>();
            grid.cellLayout = GridLayout.CellLayout.Isometric;
            grid.cellSize = new Vector3(1f, 0.5f, 1f);

            ground_tilemap = CreateTilemap("Grass", 0, TilemapRenderer.Mode.Chunk);
            var world_object = new GameObject("Factory Objects");
            world_object.transform.SetParent(grid.transform, false);
            world_root = world_object.transform;
            var conveyor_object = new GameObject("Conveyors");
            conveyor_object.transform.SetParent(world_root, false);
            conveyor_root = conveyor_object.transform;
            var item_object = new GameObject("Items");
            item_object.transform.SetParent(world_root, false);
            item_root = item_object.transform;
            preview_tilemap = CreateTilemap("Construction Preview", 30000, TilemapRenderer.Mode.Individual);
            preview_tilemap.color = new Color(1f, 1f, 1f, 0.65f);
            preview_tilemap.GetComponent<TilemapRenderer>().sortingLayerName =
                FactorySorting.ITEM_SORTING_LAYER;
        }

        private void FillGround()
        {
            var random = new System.Random(grass_seed);

            for (var y = 0; y < map_height; y++)
            {
                for (var x = 0; x < map_width; x++)
                {
                    var tile = random.Next(2) == 0 ? tile_catalog.Grass01 : tile_catalog.Grass02;
                    ground_tilemap.SetTile(new Vector3Int(x, y), tile);
                }
            }

            ground_tilemap.CompressBounds();
        }

        private void CreateConstructionControls()
        {
            var map_size = new Vector2Int(map_width, map_height);
            var build_mode = gameObject.AddComponent<FactoryBuildMode>();
            conveyor_builder = gameObject.AddComponent<ConveyorBuilder>();
            conveyor_builder.Initialize(
                Camera.main,
                grid,
                conveyor_root,
                preview_tilemap,
                build_mode,
                conveyor_network,
                extraction_network,
                tile_catalog,
                map_size);
            extraction_network.ExtractorPlaced += OnExtractorPlaced;
            extraction_network.DyeingMachinePlaced += OnDyeingMachinePlaced;

            var extractor_builder = gameObject.AddComponent<ExtractorBuilder>();
            extractor_builder.Initialize(
                Camera.main,
                grid,
                world_root,
                build_mode,
                extraction_network,
                tile_catalog,
                map_size);

            var recipe_panel = DyeingRecipePanel.Create(transform, tile_catalog);
            var dyeing_builder = gameObject.AddComponent<DyeingMachineBuilder>();
            dyeing_builder.Initialize(
                Camera.main,
                grid,
                world_root,
                build_mode,
                extraction_network,
                tile_catalog,
                recipe_panel,
                map_size);

            var item_transport = new FactoryItemTransport(conveyor_network, extraction_network);
            var item_view = gameObject.AddComponent<FactoryItemTransportView>();
            item_view.Initialize(item_transport, tile_catalog, grid, item_root, map_size);

            var hotbar = FactoryHotbar.Create(
                transform,
                tile_catalog.ConveyorIcon,
                tile_catalog.ExtractorIcon,
                tile_catalog.DyeingMachineIcon);
            hotbar.ToolClicked += build_mode.Toggle;
            build_mode.Changed += hotbar.SetSelectedTool;
        }

        private void OnExtractorPlaced(ExtractorState extractor)
        {
            conveyor_network.AddExternalInput(extractor.OutputPosition, extractor.Direction);
            conveyor_builder.RefreshConveyors();
        }

        private void OnDyeingMachinePlaced(DyeingMachineState machine)
        {
            var direction = GridDirectionExtensions.FromDelta(machine.Forward);
            conveyor_network.AddExternalInput(machine.OutputConveyorPosition, direction);
            conveyor_network.AddExternalOutput(machine.GetInputConveyorPosition(0), direction);
            conveyor_network.AddExternalOutput(machine.GetInputConveyorPosition(1), direction);
            conveyor_builder.RefreshConveyors();
        }

        private ExtractionNetwork CreateExtractionNetwork()
        {
            var deposits = new[]
            {
                new RawMaterialDeposit(RawMaterialType.DyeBlue, new Vector2Int(8, 8)),
                new RawMaterialDeposit(RawMaterialType.DyeRed, new Vector2Int(41, 8)),
                new RawMaterialDeposit(RawMaterialType.DyeYellow, new Vector2Int(8, 41)),
                new RawMaterialDeposit(RawMaterialType.Mushroom, new Vector2Int(41, 41)),
                new RawMaterialDeposit(RawMaterialType.Snail, new Vector2Int(25, 25))
            };
            return new ExtractionNetwork(deposits, conveyor_network);
        }

        private void ConfigureCamera()
        {
            var main_camera = Camera.main;
            main_camera.backgroundColor = new Color(0.075f, 0.12f, 0.08f);
            main_camera.transparencySortMode = TransparencySortMode.CustomAxis;
            main_camera.transparencySortAxis = FactorySorting.TRANSPARENCY_AXIS;

            var first_corner = ground_tilemap.GetCellCenterWorld(Vector3Int.zero);
            var opposite_corner = ground_tilemap.GetCellCenterWorld(
                new Vector3Int(map_width - 1, map_height - 1));
            var center = (first_corner + opposite_corner) * 0.5f;
            main_camera.transform.position = new Vector3(center.x, center.y, -10f);
            main_camera.orthographicSize = 7.5f;

            var controller = main_camera.gameObject.AddComponent<FactoryCameraController>();
            controller.Initialize(ground_tilemap.GetComponent<Renderer>());
        }

        private Tilemap CreateTilemap(string object_name, int sorting_order, TilemapRenderer.Mode mode)
        {
            var tilemap_object = new GameObject(object_name);
            tilemap_object.transform.SetParent(grid.transform, false);
            var tilemap = tilemap_object.AddComponent<Tilemap>();
            var renderer = tilemap_object.AddComponent<TilemapRenderer>();
            renderer.sortingOrder = sorting_order;
            renderer.mode = mode;
            renderer.sortOrder = TilemapRenderer.SortOrder.TopRight;
            return tilemap;
        }
    }
}
