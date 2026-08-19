using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
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
        private RecipeSelectionPanel recipe_panel;
        private PortalSelectionPanel portal_panel;
        private ItemUpgradePanel item_upgrade_panel;
        private FactoryContentConfig content_config;
        private FactorySaveService save_service;
        private FactorySettingsData factory_settings;
        private FactoryStageCollectionData factory_stages;
        private FactoryProgression progression;
        private PortalEconomy economy;
        private FactoryStageDefinition current_stage;
        private FactoryDebugMapEditor debug_map_editor;
        private FactoryItemTransport item_transport;
        private readonly List<FactoryHeadlessRuntime> background_factories = new();
        private float next_factory_save_time;

        private void Awake()
        {
            tile_catalog = new FactoryTileCatalog();
            InitializeProgression();

            if (string.IsNullOrEmpty(FactoryStageSession.SelectedStageId)
                || !progression.IsStageUnlocked(FactoryStageSession.SelectedStageId))
            {
                FactoryStageSession.Clear();
                InitializeBackgroundFactories(null);
                Camera.main.backgroundColor = new Color(0.035f, 0.055f, 0.04f);
                StageSelectionPanel.Create(transform, tile_catalog, progression, EnterStage);
                return;
            }

            current_stage = content_config.GetStage(FactoryStageSession.SelectedStageId);
            grass_seed = current_stage.GrassSeed;
            conveyor_network = new ConveyorNetwork();
            extraction_network = CreateExtractionNetwork();
            RestoreCurrentFactory();

            CreateMap();
            FillGround();
            CreateConstructionControls();
            InitializeBackgroundFactories(current_stage.Id);
            ConfigureCamera();
            StageReturnButton.Create(transform, tile_catalog, current_stage.DisplayName, ReturnToStages);
        }

        private void Update()
        {
            foreach (var factory in background_factories)
            {
                factory.Update(Time.deltaTime);
            }

            if (Time.unscaledTime < next_factory_save_time) return;

            next_factory_save_time = Time.unscaledTime + 2f;
            SaveFactoryStates();
        }

        private void InitializeProgression()
        {
            var config_asset = Resources.Load<FactoryContentConfig>(
                "Factory/Progression/FactoryContentConfig");
            content_config = config_asset.CreateRuntimeCopy();
            save_service = new FactorySaveService();
            economy = new PortalEconomy();
            factory_settings = save_service.LoadSettings();
            factory_stages = save_service.LoadFactories();
            factory_settings.Apply(content_config, economy);
            progression = new FactoryProgression(
                content_config,
                economy,
                save_service,
                save_service.LoadProgress());
            gameObject.AddComponent<FactoryProgressAutosave>().Initialize(progression);
        }

        private void EnterStage(string stage_id)
        {
            if (!progression.IsStageUnlocked(stage_id)) return;

            SaveFactoryStates();
            progression.Save();
            FactoryStageSession.Select(stage_id);
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        private void ReturnToStages()
        {
            SaveRuntimeSettings();
            SaveFactoryStates();
            progression.Save();
            FactoryStageSession.Clear();
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
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
            var saved_map = factory_settings.GetMap(current_stage.Id);
            if (saved_map != null
                && saved_map.width == map_width
                && saved_map.height == map_height
                && saved_map.grass_tiles.Count == map_width * map_height)
            {
                for (var y = 0; y < map_height; y++)
                {
                    for (var x = 0; x < map_width; x++)
                    {
                        var tile_index = saved_map.grass_tiles[y * map_width + x];
                        ground_tilemap.SetTile(
                            new Vector3Int(x, y),
                            tile_index == 1 ? tile_catalog.Grass02 : tile_catalog.Grass01);
                    }
                }
                ground_tilemap.CompressBounds();
                return;
            }

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
            var grid_overlay_object = new GameObject("Construction Grid Overlay");
            grid_overlay_object.transform.SetParent(grid.transform, false);
            var grid_overlay = grid_overlay_object.AddComponent<ConstructionGridOverlay>();
            grid_overlay.Initialize(grid, build_mode, map_size);

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
            extraction_network.CombinerPlaced += OnCombinerPlaced;
            extraction_network.ProcessingMachinePlaced += OnProcessingMachinePlaced;
            extraction_network.ErdaInjectorPlaced += OnErdaInjectorPlaced;
            extraction_network.PortalPlaced += OnPortalPlaced;
            extraction_network.BuildingRemoved += OnBuildingRemoved;

            var extractor_builder = gameObject.AddComponent<ExtractorBuilder>();
            extractor_builder.Initialize(
                Camera.main,
                grid,
                world_root,
                build_mode,
                extraction_network,
                tile_catalog,
                map_size);

            recipe_panel = RecipeSelectionPanel.Create(transform, tile_catalog);
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

            var combiner_builder = gameObject.AddComponent<CombinerBuilder>();
            combiner_builder.Initialize(
                Camera.main,
                grid,
                world_root,
                build_mode,
                extraction_network,
                tile_catalog,
                recipe_panel,
                map_size);

            var erda_injector_builder = gameObject.AddComponent<ErdaInjectorBuilder>();
            erda_injector_builder.Initialize(
                Camera.main,
                grid,
                world_root,
                build_mode,
                extraction_network,
                tile_catalog,
                map_size);

            var processing_machine_builder = gameObject.AddComponent<ProcessingMachineBuilder>();
            processing_machine_builder.Initialize(
                Camera.main,
                grid,
                world_root,
                build_mode,
                extraction_network,
                tile_catalog,
                recipe_panel,
                map_size);

            portal_panel = PortalSelectionPanel.Create(
                transform,
                tile_catalog,
                progression,
                current_stage);
            var portal_builder = gameObject.AddComponent<PortalBuilder>();
            portal_builder.Initialize(
                Camera.main,
                grid,
                world_root,
                build_mode,
                extraction_network,
                tile_catalog,
                portal_panel,
                map_size);

            item_transport = new FactoryItemTransport(conveyor_network, extraction_network);
            var item_view = gameObject.AddComponent<FactoryItemTransportView>();
            item_view.Initialize(item_transport, tile_catalog, grid, item_root, map_size);

            var demolition = gameObject.AddComponent<FactoryDemolitionController>();
            demolition.Initialize(
                Camera.main,
                grid,
                world_root,
                build_mode,
                conveyor_network,
                extraction_network,
                conveyor_builder,
                item_transport);

            var hotbar = FactoryHotbar.Create(
                transform,
                tile_catalog.ConveyorIcon,
                tile_catalog.ExtractorIcon,
                tile_catalog.DyeingMachineIcon,
                tile_catalog.CombinerIcon,
                tile_catalog.ErdaInjectorIcon,
                tile_catalog.ProcessingMachineIcon,
                tile_catalog.PortalIcon);
            hotbar.ToolClicked += build_mode.Toggle;
            build_mode.Changed += hotbar.SetSelectedTool;
            build_mode.DemolitionChanged += hotbar.SetDemolitionMode;
            MesoHud.Create(transform, tile_catalog, extraction_network.PortalEconomy);
            item_upgrade_panel = ItemUpgradePanel.Create(
                transform,
                tile_catalog,
                extraction_network.PortalEconomy);
            item_upgrade_panel.SetOtherModalCheck(
                () => (recipe_panel != null && recipe_panel.IsOpen)
                    || (portal_panel != null && portal_panel.IsOpen));
            ItemUpgradeShortcut.Create(
                transform,
                tile_catalog,
                extraction_network.PortalEconomy,
                item_upgrade_panel);

            debug_map_editor = gameObject.AddComponent<FactoryDebugMapEditor>();
            debug_map_editor.Initialize(
                Camera.main,
                grid,
                ground_tilemap,
                tile_catalog,
                build_mode,
                extraction_network,
                demolition,
                item_transport,
                map_size,
                current_stage.Id);
            FactoryDebugPanel.Create(
                transform,
                tile_catalog,
                extraction_network.PortalEconomy,
                debug_map_editor,
                progression,
                save_service);
        }

        private void SaveRuntimeSettings()
        {
            if (current_stage == null || debug_map_editor == null) return;

            factory_settings.Capture(content_config, economy);
            factory_settings.SetMap(debug_map_editor.CaptureSettings());
            save_service.SaveSettings(factory_settings);
        }

        private void RestoreCurrentFactory()
        {
            var saved_factory = factory_stages.GetStage(current_stage.Id);
            if (saved_factory == null) return;

            FactoryStagePersistence.Restore(
                saved_factory,
                conveyor_network,
                extraction_network);
        }

        private void InitializeBackgroundFactories(string excluded_stage_id)
        {
            background_factories.Clear();
            foreach (var saved_factory in factory_stages.stages)
            {
                if (saved_factory.stage_id == excluded_stage_id) continue;

                var stage_id = saved_factory.stage_id;
                var runtime = FactoryHeadlessRuntime.Create(
                    saved_factory,
                    CreateDeposits(stage_id),
                    economy,
                    material => progression.IsMonsterUnlocked(stage_id, material));
                background_factories.Add(runtime);
            }

            next_factory_save_time = Time.unscaledTime + 2f;
        }

        private void SaveFactoryStates()
        {
            if (current_stage != null && conveyor_network != null && extraction_network != null)
            {
                factory_stages.SetStage(FactoryStagePersistence.Capture(
                    current_stage.Id,
                    conveyor_network,
                    extraction_network));
            }

            foreach (var factory in background_factories)
            {
                factory_stages.SetStage(factory.Capture());
            }

            save_service.SaveFactories(factory_stages);
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused) SaveFactoryStates();
        }

        private void OnApplicationFocus(bool focused)
        {
            if (!focused) SaveFactoryStates();
        }

        private void OnApplicationQuit()
        {
            SaveFactoryStates();
        }

        private void OnExtractorPlaced(ExtractorState extractor)
        {
            conveyor_network.AddExternalInput(extractor.OutputPosition, extractor.Direction);
            conveyor_builder.RefreshConveyors();
        }

        private void OnDyeingMachinePlaced(DyeingMachineState machine)
        {
            ConnectRecipeMachine(machine);
        }

        private void OnCombinerPlaced(CombinerState machine)
        {
            ConnectRecipeMachine(machine);
        }

        private void OnProcessingMachinePlaced(ProcessingMachineState machine)
        {
            ConnectRecipeMachine(machine);
        }

        private void ConnectRecipeMachine(IRecipeMachine machine)
        {
            var direction = GridDirectionExtensions.FromDelta(machine.Forward);
            conveyor_network.AddExternalInput(machine.OutputConveyorPosition, direction);
            for (var input = 0; input < machine.InputCount; input++)
            {
                conveyor_network.AddExternalOutput(machine.GetInputConveyorPosition(input), direction);
            }
            conveyor_builder.RefreshConveyors();
        }

        private void OnErdaInjectorPlaced(ErdaInjectorState injector)
        {
            var direction = GridDirectionExtensions.FromDelta(injector.Forward);
            conveyor_network.AddExternalInput(injector.OutputConveyorPosition, direction);
            conveyor_network.AddExternalOutput(injector.InputConveyorPosition, direction);
            conveyor_builder.RefreshConveyors();
        }

        private void OnPortalPlaced(PortalState portal)
        {
            foreach (var port in portal.InputPorts)
            {
                conveyor_network.AddExternalOutput(port.ConveyorPosition, port.Direction);
            }
            conveyor_builder.RefreshConveyors();
        }

        private void OnBuildingRemoved(object building)
        {
            switch (building)
            {
                case ExtractorState extractor:
                    conveyor_network.RemoveExternalInput(extractor.OutputPosition, extractor.Direction);
                    break;
                case IRecipeMachine machine:
                    DisconnectRecipeMachine(machine);
                    break;
                case ErdaInjectorState injector:
                    var injector_direction = GridDirectionExtensions.FromDelta(injector.Forward);
                    conveyor_network.RemoveExternalInput(
                        injector.OutputConveyorPosition,
                        injector_direction);
                    conveyor_network.RemoveExternalOutput(
                        injector.InputConveyorPosition,
                        injector_direction);
                    break;
                case PortalState portal:
                    foreach (var port in portal.InputPorts)
                    {
                        conveyor_network.RemoveExternalOutput(port.ConveyorPosition, port.Direction);
                    }
                    break;
            }

            conveyor_builder.RefreshConveyors();
        }

        private void DisconnectRecipeMachine(IRecipeMachine machine)
        {
            var direction = GridDirectionExtensions.FromDelta(machine.Forward);
            conveyor_network.RemoveExternalInput(machine.OutputConveyorPosition, direction);
            for (var input = 0; input < machine.InputCount; input++)
            {
                conveyor_network.RemoveExternalOutput(machine.GetInputConveyorPosition(input), direction);
            }
        }

        private ExtractionNetwork CreateExtractionNetwork()
        {
            return new ExtractionNetwork(
                CreateDeposits(current_stage.Id),
                conveyor_network,
                economy,
                material => progression.IsMonsterUnlocked(current_stage.Id, material));
        }

        private RawMaterialDeposit[] CreateDeposits(string stage_id)
        {
            var saved_map = factory_settings.GetMap(stage_id);
            var deposits = saved_map == null
                ? new[]
            {
                new RawMaterialDeposit(RawMaterialType.DyeBlue, new Vector2Int(8, 8)),
                new RawMaterialDeposit(RawMaterialType.DyeRed, new Vector2Int(41, 8)),
                new RawMaterialDeposit(RawMaterialType.DyeYellow, new Vector2Int(8, 41)),
                new RawMaterialDeposit(RawMaterialType.Mushroom, new Vector2Int(41, 41)),
                new RawMaterialDeposit(RawMaterialType.Snail, new Vector2Int(25, 25))
            }
                : saved_map.deposits.ConvertAll(deposit => new RawMaterialDeposit(
                    deposit.material,
                    new Vector2Int(deposit.x, deposit.y))).ToArray();
            return deposits;
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
            controller.Initialize(
                ground_tilemap.GetComponent<Renderer>(),
                () => (recipe_panel != null && recipe_panel.IsOpen)
                    || (portal_panel != null && portal_panel.IsOpen));
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
