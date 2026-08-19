using NUnit.Framework;
using UnityEngine;

namespace Maptory.Factory.Tests
{
    public sealed class FactoryStagePersistenceTests
    {
        [Test]
        public void FactoryLayoutRoundTripPreservesBuildingsAndSelections()
        {
            var deposits = new[]
            {
                new RawMaterialDeposit(RawMaterialType.Snail, new Vector2Int(5, 5))
            };
            var economy = new PortalEconomy();
            var conveyors = new ConveyorNetwork();
            conveyors.PlaceLine(new Vector2Int(1, 1), new Vector2Int(2, 1));
            var extraction = new ExtractionNetwork(deposits, conveyors, economy);
            extraction.PlaceExtractor(new Vector2Int(5, 5), GridDirection.Up);
            var dyeing = extraction.PlaceDyeingMachine(
                new Vector2Int(15, 15),
                GridDirection.Right);
            dyeing.SelectRecipe(DyeingRecipe.All[DyeingRecipeId.SnailRed]);
            var combiner = extraction.PlaceCombiner(
                new Vector2Int(20, 20),
                GridDirection.Down);
            combiner.SelectRecipe(CombiningRecipe.All[CombiningRecipeId.DyePurple]);
            var processing = extraction.PlaceProcessingMachine(
                new Vector2Int(25, 25),
                GridDirection.Left);
            processing.SelectRecipe(ProcessingRecipe.All[ProcessingRecipeId.Horn]);
            extraction.PlaceErdaInjector(new Vector2Int(7, 5), GridDirection.Up);
            var portal = extraction.PlacePortal(new Vector2Int(8, 5));
            portal.SelectMaterial(RawMaterialType.MonsterSnailGreen);

            var captured = FactoryStagePersistence.Capture(
                "stage_1",
                conveyors,
                extraction);
            var serialized = JsonUtility.ToJson(captured);
            var loaded = JsonUtility.FromJson<FactoryStageStateData>(serialized);
            var restored_conveyors = new ConveyorNetwork();
            var restored = new ExtractionNetwork(deposits, restored_conveyors, economy);
            FactoryStagePersistence.Restore(loaded, restored_conveyors, restored);

            Assert.That(restored_conveyors.Conveyors.Count, Is.EqualTo(2));
            Assert.That(restored.Extractors[new Vector2Int(5, 5)].Direction,
                Is.EqualTo(GridDirection.Up));
            Assert.That(restored.DyeingMachines[new Vector2Int(15, 15)].SelectedRecipe,
                Is.SameAs(DyeingRecipe.All[DyeingRecipeId.SnailRed]));
            Assert.That(restored.Combiners[new Vector2Int(20, 20)].SelectedRecipe,
                Is.SameAs(CombiningRecipe.All[CombiningRecipeId.DyePurple]));
            Assert.That(restored.ProcessingMachines[new Vector2Int(25, 25)].SelectedRecipe,
                Is.SameAs(ProcessingRecipe.All[ProcessingRecipeId.Horn]));
            Assert.That(restored.ErdaInjectors.ContainsKey(new Vector2Int(7, 5)), Is.True);
            Assert.That(restored.Portals[new Vector2Int(8, 5)].SelectedMaterial,
                Is.EqualTo(RawMaterialType.MonsterSnailGreen));
        }

        [Test]
        public void HeadlessFactoryContinuesProducingMeso()
        {
            var deposits = new[]
            {
                new RawMaterialDeposit(RawMaterialType.Snail, new Vector2Int(5, 5))
            };
            var economy = new PortalEconomy();
            var conveyors = new ConveyorNetwork();
            var extraction = new ExtractionNetwork(deposits, conveyors, economy);
            extraction.PlaceExtractor(new Vector2Int(5, 5), GridDirection.Up);
            extraction.PlaceErdaInjector(new Vector2Int(7, 5), GridDirection.Up);
            var portal = extraction.PlacePortal(new Vector2Int(8, 5));
            portal.SelectMaterial(RawMaterialType.MonsterSnailGreen);
            var state = FactoryStagePersistence.Capture(
                "stage_1",
                conveyors,
                extraction);
            var runtime = FactoryHeadlessRuntime.Create(
                state,
                deposits,
                economy,
                _ => true);

            for (var step = 0; step < 50; step++)
            {
                runtime.Update(0.1f);
            }

            Assert.That(economy.TotalMeso, Is.GreaterThanOrEqualTo(1));
            Assert.That(
                economy.GetTotalItems(RawMaterialType.MonsterSnailGreen),
                Is.GreaterThanOrEqualTo(1));
        }

        [Test]
        public void StageCollectionReplacesOnlyMatchingFactory()
        {
            var collection = new FactoryStageCollectionData();
            collection.SetStage(new FactoryStageStateData { stage_id = "stage_1" });
            collection.SetStage(new FactoryStageStateData { stage_id = "stage_2" });
            collection.SetStage(new FactoryStageStateData
            {
                stage_id = "stage_1",
                conveyors =
                {
                    new ConveyorStateData { x = 3, y = 4, direction = GridDirection.Left }
                }
            });

            Assert.That(collection.stages.Count, Is.EqualTo(2));
            Assert.That(collection.GetStage("stage_1").conveyors.Count, Is.EqualTo(1));
            Assert.That(collection.GetStage("stage_2"), Is.Not.Null);
        }
    }
}
