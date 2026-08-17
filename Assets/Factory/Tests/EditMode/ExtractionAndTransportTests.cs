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
        public void ExtractedItemMovesOntoAndAlongConveyors()
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
            Assert.That(transport.Items[0].Position, Is.EqualTo(Vector2Int.zero));
            Assert.That(transport.Items[0].TargetPosition, Is.EqualTo(Vector2Int.right));

            transport.Step();
            transport.Step();

            Assert.That(transport.Items[0].Position, Is.EqualTo(new Vector2Int(2, 0)));
            Assert.That(transport.Items[0].TargetPosition, Is.EqualTo(new Vector2Int(3, 0)));
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

        private static ExtractionNetwork CreateExtractionNetwork()
        {
            return new ExtractionNetwork(new[]
            {
                new RawMaterialDeposit(RawMaterialType.DyeBlue, Vector2Int.zero)
            });
        }
    }
}
