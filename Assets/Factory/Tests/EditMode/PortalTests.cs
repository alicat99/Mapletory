using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace Maptory.Factory.Tests
{
    public sealed class PortalTests
    {
        [Test]
        public void SupplyCatalogUsesSevenProducedMonsterItems()
        {
            Assert.That(PortalSupplyCatalog.Options.Count, Is.EqualTo(7));
            Assert.That(
                PortalSupplyCatalog.Options[0].SelectionLabel,
                Is.EqualTo("[오솔길1 level1] 빨간 달팽이 · 공급 0/60"));
            Assert.That(
                PortalSupplyCatalog.Options[6].SelectionLabel,
                Is.EqualTo("[꿈꾸는 오솔길 level1] 파란 버섯 · 공급 0/60"));
            Assert.That(
                PortalSupplyCatalog.Options.Select(option => option.Material),
                Is.EquivalentTo(ErdaInjectionRecipes.All.Values));
        }

        [Test]
        public void PortalOccupiesTwoByTwoCellsAndExposesEightInputs()
        {
            var network = new ExtractionNetwork(
                new RawMaterialDeposit[0],
                new ConveyorNetwork());
            var anchor = new Vector2Int(10, 10);
            var portal = network.PlacePortal(anchor);

            Assert.That(portal.InputPorts.Count, Is.EqualTo(8));
            Assert.That(network.IsBuildingOccupied(anchor), Is.True);
            Assert.That(network.IsBuildingOccupied(anchor + Vector2Int.right), Is.True);
            Assert.That(network.IsBuildingOccupied(anchor + Vector2Int.up), Is.True);
            Assert.That(network.IsBuildingOccupied(anchor + Vector2Int.one), Is.True);
            Assert.That(network.IsBuildingOccupied(anchor + Vector2Int.left), Is.False);
            Assert.That(portal.InputPorts.Any(port =>
                port.ConveyorPosition == anchor + Vector2Int.left
                && port.PortalPosition == anchor
                && port.Direction == GridDirection.Up), Is.True);
            Assert.That(portal.InputPorts.Any(port =>
                port.ConveyorPosition == anchor + new Vector2Int(2, 1)
                && port.PortalPosition == anchor + Vector2Int.one
                && port.Direction == GridDirection.Down), Is.True);
        }

        [Test]
        public void PortalCannotOverlapDepositsBuildingsOrConveyors()
        {
            var conveyors = new ConveyorNetwork();
            conveyors.SetConveyor(new Vector2Int(10, 10), GridDirection.Up);
            var network = new ExtractionNetwork(new[]
            {
                new RawMaterialDeposit(RawMaterialType.Snail, Vector2Int.zero)
            }, conveyors);

            Assert.That(network.CanPlacePortal(new Vector2Int(10, 10)), Is.False);
            Assert.That(network.CanPlacePortal(Vector2Int.zero), Is.False);

            var portal = network.PlacePortal(new Vector2Int(20, 20));
            Assert.That(network.CanPlaceProcessingMachine(portal.Anchor + Vector2Int.one), Is.False);
        }

        [Test]
        public void PortalAcceptsOnlySelectedItemAndPaysThreeMesoPerPair()
        {
            var economy = new PortalEconomy();
            var portal = new PortalState(Vector2Int.zero, economy);

            Assert.That(portal.CanAccept(RawMaterialType.MonsterSnailRed), Is.False);
            portal.SelectMaterial(RawMaterialType.MonsterSnailRed);
            Assert.That(portal.CanAccept(RawMaterialType.MonsterSnailRed), Is.True);
            Assert.That(portal.CanAccept(RawMaterialType.SnailRed), Is.False);

            portal.AddInput(RawMaterialType.MonsterSnailRed);
            Assert.That(economy.TotalMeso, Is.EqualTo(1));
            portal.AddInput(RawMaterialType.MonsterSnailRed);
            Assert.That(economy.TotalMeso, Is.EqualTo(3));
            Assert.That(economy.GetTotalItems(RawMaterialType.MonsterSnailRed), Is.EqualTo(2));
        }

        [Test]
        public void EveryPortalSideConsumesMatchingConveyorItems()
        {
            foreach (var input_index in Enumerable.Range(0, 8))
            {
                var conveyors = new ConveyorNetwork();
                var network = new ExtractionNetwork(new RawMaterialDeposit[0], conveyors);
                var portal = network.PlacePortal(new Vector2Int(10, 10));
                portal.SelectMaterial(RawMaterialType.MonsterSnailRed);
                var input = portal.InputPorts[input_index];
                conveyors.SetConveyor(input.ConveyorPosition, input.Direction);
                var transport = new FactoryItemTransport(conveyors, network);
                transport.SpawnItem(RawMaterialType.MonsterSnailRed, input.ConveyorPosition);

                transport.Step();

                Assert.That(transport.Items[0].ScaleAnimation,
                    Is.EqualTo(ItemScaleAnimation.Despawning));
                Assert.That(transport.Items[0].TargetPosition,
                    Is.EqualTo(input.PortalPosition));

                transport.Step();

                Assert.That(transport.Items, Is.Empty);
                Assert.That(network.PortalEconomy.TotalMeso, Is.EqualTo(1));
            }
        }

        [Test]
        public void ErdaInjectorOutputsDirectlyIntoAdjacentPortal()
        {
            var conveyors = new ConveyorNetwork();
            var network = new ExtractionNetwork(new RawMaterialDeposit[0], conveyors);
            var injector = network.PlaceErdaInjector(new Vector2Int(10, 10), GridDirection.Up);
            var portal = network.PlacePortal(injector.OutputConveyorPosition);
            portal.SelectMaterial(RawMaterialType.MonsterSnailRed);
            injector.AddInput(RawMaterialType.SnailRed);
            var transport = new FactoryItemTransport(conveyors, network);

            transport.Step();

            Assert.That(transport.Items.Count, Is.EqualTo(1));
            Assert.That(transport.Items[0].Position, Is.EqualTo(injector.Center));
            Assert.That(transport.Items[0].TargetPosition, Is.EqualTo(portal.Anchor));
            Assert.That(transport.Items[0].ScaleAnimation,
                Is.EqualTo(ItemScaleAnimation.Despawning));

            transport.Step();

            Assert.That(transport.Items, Is.Empty);
            Assert.That(network.PortalEconomy.TotalMeso, Is.EqualTo(1));
        }

        [Test]
        public void PortalsShareSmoothedRateBySelectedMaterial()
        {
            var economy = new PortalEconomy();
            var first = new PortalState(Vector2Int.zero, economy);
            var second = new PortalState(new Vector2Int(4, 4), economy);
            first.SelectMaterial(RawMaterialType.MonsterMushroomGreen);
            second.SelectMaterial(RawMaterialType.MonsterMushroomGreen);
            first.AddInput(RawMaterialType.MonsterMushroomGreen);
            second.AddInput(RawMaterialType.MonsterMushroomGreen);

            economy.Update(1f);

            var first_sample = economy.GetItemsPerMinute(RawMaterialType.MonsterMushroomGreen);
            Assert.That(first_sample, Is.EqualTo(120f).Within(0.001f));
            Assert.That(first_sample * PortalEconomy.MESO_PER_ITEM,
                Is.EqualTo(180f).Within(0.001f));

            economy.Update(1f);

            var smoothed_sample = economy.GetItemsPerMinute(RawMaterialType.MonsterMushroomGreen);
            Assert.That(smoothed_sample, Is.GreaterThan(0f));
            Assert.That(smoothed_sample, Is.LessThan(first_sample));
        }

        [Test]
        public void PortalSpritesUseBuildingPivotAndDedicatedLayers()
        {
            var catalog = new FactoryTileCatalog();
            AssertPivot(catalog.GetPortalSprite());
            AssertPivot(catalog.GetPortalLowerSprite());
            AssertPivot(catalog.GetPortalUpperSprite());
        }

        private static void AssertPivot(Sprite sprite)
        {
            Assert.That(sprite, Is.Not.Null);
            Assert.That(sprite.pivot.x / sprite.rect.width, Is.EqualTo(0.5f).Within(0.001f));
            Assert.That(sprite.pivot.y / sprite.rect.height, Is.EqualTo(0.25f).Within(0.001f));
        }
    }
}
