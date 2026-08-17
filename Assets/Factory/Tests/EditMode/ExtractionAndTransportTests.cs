using NUnit.Framework;
using UnityEngine;

namespace Maptory.Factory.Tests
{
    public sealed class ExtractionAndTransportTests
    {
        [Test]
        public void ExtractorCanOnlyCoverDepositCenterOnce()
        {
            var network = CreateExtractionNetwork();

            Assert.That(network.CanPlaceExtractor(Vector2Int.zero), Is.True);
            Assert.That(network.CanPlaceExtractor(Vector2Int.right), Is.False);

            var extractor = network.PlaceExtractor(Vector2Int.zero, GridDirection.Up);

            Assert.That(network.CanPlaceExtractor(Vector2Int.zero), Is.False);
            Assert.That(extractor.OutputPosition, Is.EqualTo(new Vector2Int(2, 0)));
            Assert.That(extractor.Material, Is.EqualTo(RawMaterialType.DyeBlue));
        }

        [Test]
        public void ExtractorFootprintOccupiesAllNineCells()
        {
            var network = CreateExtractionNetwork();
            network.PlaceExtractor(Vector2Int.zero, GridDirection.Up);

            Assert.That(network.IsBuildingOccupied(new Vector2Int(-1, -1)), Is.True);
            Assert.That(network.IsBuildingOccupied(new Vector2Int(1, 1)), Is.True);
            Assert.That(network.IsBuildingOccupied(new Vector2Int(2, 0)), Is.False);
        }

        [Test]
        public void ExtractorCannotCoverExistingConveyor()
        {
            var conveyors = new ConveyorNetwork();
            conveyors.SetConveyor(Vector2Int.right, GridDirection.Up);
            var network = CreateExtractionNetwork(conveyors);

            Assert.That(network.CanPlaceExtractor(Vector2Int.zero), Is.False);
        }

        [Test]
        public void DyeingMachineCannotOverlapExtractorFootprint()
        {
            var network = new ExtractionNetwork(new[]
            {
                new RawMaterialDeposit(RawMaterialType.DyeBlue, Vector2Int.zero)
            }, new ConveyorNetwork());
            network.PlaceExtractor(Vector2Int.zero, GridDirection.Up);

            Assert.That(network.CanPlaceDyeingMachine(new Vector2Int(2, 0)), Is.False);
        }

        [Test]
        public void CounterClockwiseRotationFollowsScreenDirections()
        {
            Assert.That(GridDirection.Up.RotateCounterClockwise(), Is.EqualTo(GridDirection.Left));
            Assert.That(GridDirection.Left.RotateCounterClockwise(), Is.EqualTo(GridDirection.Down));
            Assert.That(GridDirection.Down.RotateCounterClockwise(), Is.EqualTo(GridDirection.Right));
            Assert.That(GridDirection.Right.RotateCounterClockwise(), Is.EqualTo(GridDirection.Up));
        }

        [Test]
        public void MaterialTypesMapToExpectedSprites()
        {
            Assert.That(RawMaterialType.DyeBlue.ToResourceSpriteName(),
                Is.EqualTo("RawMaterialDyeBlue"));
            Assert.That(RawMaterialType.Snail.ToItemSpriteName(), Is.EqualTo("SnailGreen"));
            Assert.That(RawMaterialType.Mushroom.ToItemSpriteName(), Is.EqualTo("Mushroom"));
        }

        [Test]
        public void ExtractedItemAppearsOnFirstConveyorThenMovesAlongLine()
        {
            var conveyors = new ConveyorNetwork();
            conveyors.PlaceLine(new Vector2Int(2, 0), new Vector2Int(4, 0));
            var extraction = CreateExtractionNetwork();
            extraction.PlaceExtractor(Vector2Int.zero, GridDirection.Up);
            var transport = new FactoryItemTransport(conveyors, extraction);

            transport.Step();
            transport.Step();
            transport.Step();

            Assert.That(transport.Items.Count, Is.EqualTo(1));
            Assert.That(transport.Items[0].Material, Is.EqualTo(RawMaterialType.DyeBlue));
            Assert.That(transport.Items[0].Position, Is.EqualTo(new Vector2Int(2, 0)));
            Assert.That(transport.Items[0].TargetPosition, Is.EqualTo(new Vector2Int(2, 0)));
            Assert.That(transport.Items[0].IsSpawning, Is.True);

            transport.Step();

            Assert.That(transport.Items[0].Position, Is.EqualTo(new Vector2Int(2, 0)));
            Assert.That(transport.Items[0].TargetPosition, Is.EqualTo(new Vector2Int(3, 0)));
            Assert.That(transport.Items[0].IsSpawning, Is.False);
        }

        [Test]
        public void StepProgressAdvancesLinearlyWithElapsedTime()
        {
            var transport = new FactoryItemTransport(
                new ConveyorNetwork(),
                CreateExtractionNetwork());

            transport.Update(FactoryItemTransport.STEP_DURATION * 0.25f);

            Assert.That(transport.StepProgress, Is.EqualTo(0.25f).Within(0.0001f));
        }

        [Test]
        public void ExtractorWaitsUntilOutputConveyorExists()
        {
            var extraction = CreateExtractionNetwork();
            extraction.PlaceExtractor(Vector2Int.zero, GridDirection.Up);
            var transport = new FactoryItemTransport(new ConveyorNetwork(), extraction);

            for (var step = 0; step < 10; step++)
            {
                transport.Step();
            }

            Assert.That(transport.Items, Is.Empty);
        }

        [Test]
        public void MergeSelectorAlternatesCompetingInputs()
        {
            var selector = new FairMergeSelector();
            var destination = Vector2Int.zero;
            var left = Vector2Int.left;
            var right = Vector2Int.right;
            var sources = new[] { right, left };

            Assert.That(selector.SelectSource(destination, sources), Is.EqualTo(left));
            Assert.That(selector.SelectSource(destination, sources), Is.EqualTo(right));
            Assert.That(selector.SelectSource(destination, sources), Is.EqualTo(left));
            Assert.That(selector.SelectSource(destination, sources), Is.EqualTo(right));
        }

        [Test]
        public void LowerYAlwaysHasHigherSortingPriority()
        {
            var map_size = new Vector2Int(50, 50);
            var lower_y = FactorySorting.GetOrder(
                new Vector2(0, 10),
                map_size,
                FactorySorting.CONVEYOR_LAYER);
            var higher_y = FactorySorting.GetOrder(
                new Vector2(11, 0),
                map_size,
                FactorySorting.ITEM_LAYER);

            Assert.That(lower_y, Is.GreaterThan(higher_y));
        }

        [Test]
        public void TransparencyAxisIncludesItemHeight()
        {
            Assert.That(FactorySorting.TRANSPARENCY_AXIS.y, Is.GreaterThan(0f));
            Assert.That(FactorySorting.TRANSPARENCY_AXIS.z, Is.LessThan(0f));
            Assert.That(FactorySorting.TRANSPARENCY_AXIS.magnitude,
                Is.EqualTo(1f).Within(0.0001f));
            Assert.That(FactoryItemTransportView.ITEM_HEIGHT, Is.EqualTo(0.3f));
        }

        [Test]
        public void ItemSortingLevelIsAboveConveyorSortingLevel()
        {
            var conveyor_level = SortingLayer.GetLayerValueFromName(
                FactorySorting.CONVEYOR_SORTING_LAYER);
            var item_level = SortingLayer.GetLayerValueFromName(
                FactorySorting.ITEM_SORTING_LAYER);

            Assert.That(item_level, Is.GreaterThan(conveyor_level));
        }

        [Test]
        public void ExtractorHasGeneratedLowerAndUpperSprites()
        {
            var catalog = new FactoryTileCatalog();

            foreach (var direction in new[]
            {
                GridDirection.Up,
                GridDirection.Right,
                GridDirection.Down,
                GridDirection.Left
            })
            {
                var lower = catalog.GetExtractorLowerSprite(direction);
                var upper = catalog.GetExtractorUpperSprite(direction);
                Assert.That(lower, Is.Not.Null);
                Assert.That(upper, Is.Not.Null);
                Assert.That(lower.rect.size, Is.EqualTo(upper.rect.size));
                Assert.That(lower.pivot, Is.EqualTo(upper.pivot));
            }
        }

        [Test]
        public void UpDyeingMachineUsesSpecifiedPorts()
        {
            var machine = new DyeingMachineState(Vector2Int.zero, GridDirection.Up);

            Assert.That(machine.GetInputPort(0), Is.EqualTo(new Vector2Int(-1, -1)));
            Assert.That(machine.GetInputPort(1), Is.EqualTo(new Vector2Int(1, -1)));
            Assert.That(machine.OutputPort, Is.EqualTo(new Vector2Int(0, 1)));
            Assert.That(machine.GetInputConveyorPosition(0), Is.EqualTo(new Vector2Int(-1, -2)));
            Assert.That(machine.OutputConveyorPosition, Is.EqualTo(new Vector2Int(0, 2)));
        }

        [Test]
        public void DyeingMachinePortsRotateWithOutputDirection()
        {
            var machine = new DyeingMachineState(Vector2Int.zero, GridDirection.Right);

            Assert.That(machine.GetInputPort(0), Is.EqualTo(new Vector2Int(-1, 1)));
            Assert.That(machine.GetInputPort(1), Is.EqualTo(new Vector2Int(-1, -1)));
            Assert.That(machine.OutputPort, Is.EqualTo(Vector2Int.right));
            Assert.That(machine.OutputConveyorPosition, Is.EqualTo(Vector2Int.right * 2));
        }

        [Test]
        public void DyeingMachineStartsWithoutRecipeAndCannotOverlapResources()
        {
            var network = CreateExtractionNetwork();

            Assert.That(network.CanPlaceDyeingMachine(Vector2Int.zero), Is.False);
            Assert.That(network.CanPlaceDyeingMachine(new Vector2Int(2, 0)), Is.False);
            Assert.That(network.CanPlaceDyeingMachine(new Vector2Int(4, 4)), Is.True);
            var machine = network.PlaceDyeingMachine(new Vector2Int(4, 4), GridDirection.Up);
            Assert.That(machine.SelectedRecipe, Is.Null);
            Assert.That(network.IsBuildingOccupied(new Vector2Int(5, 5)), Is.True);
        }

        [Test]
        public void AllEightDyeingRecipesAreAvailableIncludingSpikeMushrooms()
        {
            Assert.That(DyeingRecipe.All.Count, Is.EqualTo(8));
            Assert.That(DyeingRecipe.All[DyeingRecipeId.SnailRed].Result,
                Is.EqualTo(RawMaterialType.SnailRed));
            Assert.That(DyeingRecipe.All[DyeingRecipeId.SpikeMushroomGreen].BaseMaterial,
                Is.EqualTo(RawMaterialType.SpikeMushroom));
            Assert.That(DyeingRecipe.All[DyeingRecipeId.SpikeMushroomGreen].Result,
                Is.EqualTo(RawMaterialType.SpikeMushroomGreen));

            var catalog = new FactoryTileCatalog();
            foreach (var recipe in DyeingRecipe.All.Values)
            {
                Assert.That(catalog.GetItemSprite(recipe.BaseMaterial), Is.Not.Null);
                Assert.That(catalog.GetItemSprite(recipe.Dye), Is.Not.Null);
                Assert.That(catalog.GetItemSprite(recipe.Result), Is.Not.Null);
            }
        }

        [Test]
        public void DyeingInputsDisappearAndProduceSelectedResult()
        {
            var conveyors = new ConveyorNetwork();
            var center = new Vector2Int(10, 10);
            var network = new ExtractionNetwork(new RawMaterialDeposit[0], conveyors);
            var machine = network.PlaceDyeingMachine(center, GridDirection.Up);
            machine.SelectRecipe(DyeingRecipe.All[DyeingRecipeId.SnailRed]);
            var input_direction = GridDirectionExtensions.FromDelta(machine.Forward);
            conveyors.SetConveyor(machine.GetInputConveyorPosition(0), input_direction);
            conveyors.SetConveyor(machine.GetInputConveyorPosition(1), input_direction);
            conveyors.SetConveyor(machine.OutputConveyorPosition, GridDirection.Up);
            var transport = new FactoryItemTransport(conveyors, network);
            transport.SpawnItem(RawMaterialType.Snail, machine.GetInputConveyorPosition(0));
            transport.SpawnItem(RawMaterialType.DyeRed, machine.GetInputConveyorPosition(1));

            transport.Step();

            Assert.That(transport.Items, Has.All.Matches<FactoryItemState>(
                item => item.ScaleAnimation == ItemScaleAnimation.Despawning));

            transport.Step();

            Assert.That(transport.Items.Count, Is.EqualTo(1));
            Assert.That(transport.Items[0].Material, Is.EqualTo(RawMaterialType.SnailRed));
            Assert.That(transport.Items[0].Position, Is.EqualTo(machine.OutputConveyorPosition));
            Assert.That(transport.Items[0].ScaleAnimation, Is.EqualTo(ItemScaleAnimation.Spawning));
        }

        [Test]
        public void DyeingMachineDoesNotConsumeUnselectedMaterial()
        {
            var conveyors = new ConveyorNetwork();
            var network = new ExtractionNetwork(new RawMaterialDeposit[0], conveyors);
            var machine = network.PlaceDyeingMachine(new Vector2Int(10, 10), GridDirection.Up);
            machine.SelectRecipe(DyeingRecipe.All[DyeingRecipeId.SnailRed]);
            var input = machine.GetInputConveyorPosition(0);
            conveyors.SetConveyor(input, GridDirectionExtensions.FromDelta(machine.Forward));
            var transport = new FactoryItemTransport(conveyors, network);
            transport.SpawnItem(RawMaterialType.DyeYellow, input);

            transport.Step();

            Assert.That(transport.Items.Count, Is.EqualTo(1));
            Assert.That(transport.Items[0].Position, Is.EqualTo(input));
            Assert.That(transport.Items[0].TargetPosition, Is.EqualTo(input));
            Assert.That(transport.Items[0].ScaleAnimation, Is.EqualTo(ItemScaleAnimation.None));
        }

        [Test]
        public void DyeingMachineAndExtractorUseSplitBuildingSprites()
        {
            var catalog = new FactoryTileCatalog();

            foreach (var direction in new[]
            {
                GridDirection.Up,
                GridDirection.Right,
                GridDirection.Down,
                GridDirection.Left
            })
            {
                Assert.That(catalog.GetDyeingMachineLowerSprite(direction), Is.Not.Null);
                Assert.That(catalog.GetDyeingMachineUpperSprite(direction), Is.Not.Null);
            }

            Assert.That(FactoryItemTransport.SCALE_ANIMATION_DURATION,
                Is.LessThan(FactoryItemTransport.STEP_DURATION));
        }

        private static ExtractionNetwork CreateExtractionNetwork(
            ConveyorNetwork conveyors = null)
        {
            return new ExtractionNetwork(new[]
            {
                new RawMaterialDeposit(RawMaterialType.DyeBlue, Vector2Int.zero)
            }, conveyors ?? new ConveyorNetwork());
        }
    }
}
