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
        private Tilemap conveyor_tilemap;
        private Tilemap preview_tilemap;
        private ConveyorNetwork conveyor_network;
        private FactoryTileCatalog tile_catalog;

        private void Awake()
        {
            tile_catalog = new FactoryTileCatalog();
            conveyor_network = new ConveyorNetwork();

            CreateMap();
            FillGround();
            var builder = CreateConstructionControls();
            ConfigureCamera(builder);
        }

        private void CreateMap()
        {
            var grid_object = new GameObject("Isometric Grid");
            grid_object.transform.SetParent(transform, false);
            grid = grid_object.AddComponent<Grid>();
            grid.cellLayout = GridLayout.CellLayout.Isometric;
            grid.cellSize = new Vector3(1f, 0.5f, 1f);

            ground_tilemap = CreateTilemap("Grass", 0, TilemapRenderer.Mode.Chunk);
            conveyor_tilemap = CreateTilemap("Conveyors", 10, TilemapRenderer.Mode.Individual);
            preview_tilemap = CreateTilemap("Construction Preview", 20, TilemapRenderer.Mode.Individual);
            preview_tilemap.color = new Color(1f, 1f, 1f, 0.65f);
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

        private ConveyorBuilder CreateConstructionControls()
        {
            var builder = gameObject.AddComponent<ConveyorBuilder>();
            builder.Initialize(
                Camera.main,
                grid,
                conveyor_tilemap,
                preview_tilemap,
                conveyor_network,
                tile_catalog,
                new Vector2Int(map_width, map_height));

            var hotbar = FactoryHotbar.Create(transform, tile_catalog.ConveyorIcon);
            hotbar.ConveyorClicked += builder.ToggleBuildMode;
            builder.BuildModeChanged += hotbar.SetConveyorSelected;
            return builder;
        }

        private void ConfigureCamera(ConveyorBuilder builder)
        {
            var main_camera = Camera.main;
            main_camera.backgroundColor = new Color(0.075f, 0.12f, 0.08f);
            main_camera.transparencySortMode = TransparencySortMode.CustomAxis;
            main_camera.transparencySortAxis = Vector3.up;

            var first_corner = ground_tilemap.GetCellCenterWorld(Vector3Int.zero);
            var opposite_corner = ground_tilemap.GetCellCenterWorld(
                new Vector3Int(map_width - 1, map_height - 1));
            var center = (first_corner + opposite_corner) * 0.5f;
            main_camera.transform.position = new Vector3(center.x, center.y, -10f);
            main_camera.orthographicSize = 7.5f;

            var controller = main_camera.gameObject.AddComponent<FactoryCameraController>();
            controller.Initialize(ground_tilemap.GetComponent<Renderer>(), builder);
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
