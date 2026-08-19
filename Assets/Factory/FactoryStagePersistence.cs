using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Maptory.Factory
{
    public enum FactoryBuildingKind
    {
        Extractor,
        DyeingMachine,
        Combiner,
        ProcessingMachine,
        ErdaInjector,
        Portal
    }

    [Serializable]
    public sealed class FactoryStageCollectionData
    {
        public List<FactoryStageStateData> stages = new();

        public FactoryStageStateData GetStage(string stage_id)
        {
            return stages.Find(stage => stage.stage_id == stage_id);
        }

        public void SetStage(FactoryStageStateData stage)
        {
            stages.RemoveAll(saved => saved.stage_id == stage.stage_id);
            stages.Add(stage);
        }
    }

    [Serializable]
    public sealed class FactoryStageStateData
    {
        public string stage_id;
        public List<ConveyorStateData> conveyors = new();
        public List<FactoryBuildingStateData> buildings = new();
    }

    [Serializable]
    public sealed class ConveyorStateData
    {
        public int x;
        public int y;
        public GridDirection direction;
    }

    [Serializable]
    public sealed class FactoryBuildingStateData
    {
        public FactoryBuildingKind kind;
        public int x;
        public int y;
        public GridDirection direction;
        public bool has_recipe;
        public RawMaterialType recipe_result;
        public bool has_portal_material;
        public RawMaterialType portal_material;
    }

    public static class FactoryStagePersistence
    {
        public static FactoryStageStateData Capture(
            string stage_id,
            ConveyorNetwork conveyors,
            ExtractionNetwork extraction)
        {
            var data = new FactoryStageStateData { stage_id = stage_id };
            foreach (var pair in conveyors.Conveyors)
            {
                data.conveyors.Add(new ConveyorStateData
                {
                    x = pair.Key.x,
                    y = pair.Key.y,
                    direction = pair.Value.Direction
                });
            }

            foreach (var extractor in extraction.Extractors.Values)
            {
                data.buildings.Add(CreateBuilding(
                    FactoryBuildingKind.Extractor,
                    extractor.Center,
                    extractor.Direction));
            }

            AddRecipeMachines(data, FactoryBuildingKind.DyeingMachine,
                extraction.DyeingMachines.Values);
            AddRecipeMachines(data, FactoryBuildingKind.Combiner,
                extraction.Combiners.Values);
            AddRecipeMachines(data, FactoryBuildingKind.ProcessingMachine,
                extraction.ProcessingMachines.Values);

            foreach (var injector in extraction.ErdaInjectors.Values)
            {
                data.buildings.Add(CreateBuilding(
                    FactoryBuildingKind.ErdaInjector,
                    injector.Center,
                    injector.Direction));
            }

            foreach (var portal in extraction.Portals.Values)
            {
                var building = CreateBuilding(
                    FactoryBuildingKind.Portal,
                    portal.Anchor,
                    GridDirection.Up);
                building.has_portal_material = portal.SelectedMaterial.HasValue;
                if (portal.SelectedMaterial.HasValue)
                {
                    building.portal_material = portal.SelectedMaterial.Value;
                }
                data.buildings.Add(building);
            }

            return data;
        }

        public static void Restore(
            FactoryStageStateData data,
            ConveyorNetwork conveyors,
            ExtractionNetwork extraction)
        {
            foreach (var conveyor in data.conveyors)
            {
                conveyors.SetConveyor(
                    new Vector2Int(conveyor.x, conveyor.y),
                    conveyor.direction);
            }

            foreach (var building in data.buildings)
            {
                RestoreBuilding(building, extraction);
            }

            ConnectExisting(conveyors, extraction);
        }

        public static void ConnectExisting(
            ConveyorNetwork conveyors,
            ExtractionNetwork extraction)
        {
            foreach (var extractor in extraction.Extractors.Values)
            {
                conveyors.AddExternalInput(extractor.OutputPosition, extractor.Direction);
            }

            ConnectRecipeMachines(conveyors, extraction.DyeingMachines.Values);
            ConnectRecipeMachines(conveyors, extraction.Combiners.Values);
            ConnectRecipeMachines(conveyors, extraction.ProcessingMachines.Values);

            foreach (var injector in extraction.ErdaInjectors.Values)
            {
                var direction = GridDirectionExtensions.FromDelta(injector.Forward);
                conveyors.AddExternalInput(injector.OutputConveyorPosition, direction);
                conveyors.AddExternalOutput(injector.InputConveyorPosition, direction);
            }

            foreach (var portal in extraction.Portals.Values)
            {
                foreach (var port in portal.InputPorts)
                {
                    conveyors.AddExternalOutput(port.ConveyorPosition, port.Direction);
                }
            }
        }

        private static FactoryBuildingStateData CreateBuilding(
            FactoryBuildingKind kind,
            Vector2Int position,
            GridDirection direction)
        {
            return new FactoryBuildingStateData
            {
                kind = kind,
                x = position.x,
                y = position.y,
                direction = direction
            };
        }

        private static void AddRecipeMachines<T>(
            FactoryStageStateData data,
            FactoryBuildingKind kind,
            IEnumerable<T> machines) where T : IRecipeMachine
        {
            foreach (var machine in machines)
            {
                var direction = GridDirectionExtensions.FromDelta(machine.Forward);
                var building = CreateBuilding(kind, machine.Center, direction);
                building.has_recipe = machine.SelectedRecipe != null;
                if (machine.SelectedRecipe != null)
                {
                    building.recipe_result = machine.SelectedRecipe.Result;
                }
                data.buildings.Add(building);
            }
        }

        private static void RestoreBuilding(
            FactoryBuildingStateData data,
            ExtractionNetwork extraction)
        {
            var position = new Vector2Int(data.x, data.y);
            switch (data.kind)
            {
                case FactoryBuildingKind.Extractor:
                    extraction.PlaceExtractor(position, data.direction);
                    break;
                case FactoryBuildingKind.DyeingMachine:
                    var dyeing_machine = extraction.PlaceDyeingMachine(position, data.direction);
                    if (data.has_recipe)
                    {
                        dyeing_machine.SelectRecipe(DyeingRecipe.All.Values.First(
                            recipe => recipe.Result == data.recipe_result));
                    }
                    break;
                case FactoryBuildingKind.Combiner:
                    var combiner = extraction.PlaceCombiner(position, data.direction);
                    if (data.has_recipe)
                    {
                        combiner.SelectRecipe(CombiningRecipe.All.Values.First(
                            recipe => recipe.Result == data.recipe_result));
                    }
                    break;
                case FactoryBuildingKind.ProcessingMachine:
                    var processing_machine = extraction.PlaceProcessingMachine(
                        position,
                        data.direction);
                    if (data.has_recipe)
                    {
                        processing_machine.SelectRecipe(ProcessingRecipe.All.Values.First(
                            recipe => recipe.Result == data.recipe_result));
                    }
                    break;
                case FactoryBuildingKind.ErdaInjector:
                    extraction.PlaceErdaInjector(position, data.direction);
                    break;
                case FactoryBuildingKind.Portal:
                    var portal = extraction.PlacePortal(position);
                    if (data.has_portal_material)
                    {
                        portal.SelectMaterial(data.portal_material);
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private static void ConnectRecipeMachines<T>(
            ConveyorNetwork conveyors,
            IEnumerable<T> machines) where T : IRecipeMachine
        {
            foreach (var machine in machines)
            {
                var direction = GridDirectionExtensions.FromDelta(machine.Forward);
                conveyors.AddExternalInput(machine.OutputConveyorPosition, direction);
                for (var input = 0; input < machine.InputCount; input++)
                {
                    conveyors.AddExternalOutput(
                        machine.GetInputConveyorPosition(input),
                        direction);
                }
            }
        }
    }

    public sealed class FactoryHeadlessRuntime
    {
        private readonly string stage_id;
        private readonly ConveyorNetwork conveyors;
        private readonly ExtractionNetwork extraction;
        private readonly FactoryItemTransport transport;

        private FactoryHeadlessRuntime(
            string id,
            ConveyorNetwork conveyor_network,
            ExtractionNetwork extraction_network)
        {
            stage_id = id;
            conveyors = conveyor_network;
            extraction = extraction_network;
            transport = new FactoryItemTransport(conveyors, extraction);
        }

        public static FactoryHeadlessRuntime Create(
            FactoryStageStateData state,
            IEnumerable<RawMaterialDeposit> deposits,
            PortalEconomy economy,
            Func<RawMaterialType, bool> is_portal_material_allowed)
        {
            var conveyors = new ConveyorNetwork();
            var extraction = new ExtractionNetwork(
                deposits,
                conveyors,
                economy,
                is_portal_material_allowed);
            FactoryStagePersistence.Restore(state, conveyors, extraction);
            return new FactoryHeadlessRuntime(state.stage_id, conveyors, extraction);
        }

        public void Update(float delta_time)
        {
            transport.Update(delta_time);
        }

        public FactoryStageStateData Capture()
        {
            return FactoryStagePersistence.Capture(stage_id, conveyors, extraction);
        }
    }
}
