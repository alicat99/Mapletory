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
        public void ExtractorCannotOverlapAnotherBuildingFootprint()
        {
            var network = new ExtractionNetwork(new[]
            {
                new RawMaterialDeposit(RawMaterialType.DyeBlue, Vector2Int.zero),
                new RawMaterialDeposit(RawMaterialType.DyeRed, new Vector2Int(2, 0))
            }, new ConveyorNetwork());
            network.PlaceExtractor(Vector2Int.zero, GridDirection.Up);

            Assert.That(network.CanPlaceExtractor(new Vector2Int(2, 0)), Is.False);
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
