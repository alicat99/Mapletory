using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace Maptory.Factory.Tests
{
    public sealed class PortalTests
    {
        [Test]
        public void SupplyCatalogUsesEightStageMonsterItems()
        {
            Assert.That(PortalSupplyCatalog.Options.Count, Is.EqualTo(8));
            Assert.That(
                PortalSupplyCatalog.Options[0].SelectionLabel,
                Is.EqualTo("[리스항구 외곽 level1] 달팽이"));
            Assert.That(
                PortalSupplyCatalog.Options[7].SelectionLabel,
                Is.EqualTo("[초록 뿔버섯 숲 level1] 초록 뿔버섯"));
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
        public void PortalsShareMonsterProgressAndApplyBothUpgradeTypes()
        {
            var economy = new PortalEconomy();
            var first = new PortalState(Vector2Int.zero, economy);
            var second = new PortalState(new Vector2Int(4, 4), economy);
            first.SelectMaterial(RawMaterialType.MonsterMushroomGreen);
            second.SelectMaterial(RawMaterialType.MonsterMushroomGreen);
            for (var index = 0; index < 20; index++)
            {
                var portal = index % 2 == 0 ? first : second;
                portal.AddInput(RawMaterialType.MonsterMushroomGreen);
            }

            Assert.That(economy.TotalMeso, Is.EqualTo(30));
            Assert.That(economy.GetTotalItems(RawMaterialType.MonsterMushroomGreen),
                Is.EqualTo(20));
            Assert.That(economy.GetAvailableProduction(RawMaterialType.MonsterMushroomGreen),
                Is.EqualTo(20));
            Assert.That(economy.TryPurchaseProductionUpgrade(
                RawMaterialType.MonsterMushroomGreen), Is.True);
            Assert.That(economy.GetProductionUpgradeLevel(
                RawMaterialType.MonsterMushroomGreen), Is.EqualTo(1));
            Assert.That(economy.GetAvailableProduction(RawMaterialType.MonsterMushroomGreen),
                Is.Zero);
            Assert.That(economy.GetUnitValue(RawMaterialType.MonsterMushroomGreen),
                Is.EqualTo(1.88f).Within(0.001f));

            Assert.That(economy.TryPurchaseMesoUpgrade(
                RawMaterialType.MonsterMushroomGreen), Is.True);
            Assert.That(economy.TotalMeso, Is.EqualTo(10));
            Assert.That(economy.GetMesoUpgradeLevel(
                RawMaterialType.MonsterMushroomGreen), Is.EqualTo(1));
            Assert.That(economy.GetUnitValue(RawMaterialType.MonsterMushroomGreen),
                Is.EqualTo(2.5f).Within(0.001f));

            first.AddInput(RawMaterialType.MonsterMushroomGreen);
            Assert.That(economy.TotalMeso, Is.EqualTo(12));
            Assert.That(economy.GetTotalItems(RawMaterialType.MonsterMushroomGreen),
                Is.EqualTo(21));
            Assert.That(economy.GetUnitValue(RawMaterialType.MonsterSnailRed),
                Is.EqualTo(1.5f).Within(0.001f));
        }

        [Test]
        public void DebugBalanceValuesRecalculateFutureMonsterValue()
        {
            var economy = new PortalEconomy();
            var material = RawMaterialType.MonsterSnailRed;
            economy.SetBaseValue(material, 4f);
            economy.SetMesoBonusPerLevel(material, 2f);
            economy.SetProductionMultiplierPerLevel(material, 1.5f);
            economy.SetMesoUpgradeLevel(material, 2);
            economy.SetProductionUpgradeLevel(material, 1);
            economy.SetAvailableProduction(material, 123);
            economy.SetUpgradeBaseCosts(material, 7, 11);
            economy.SetUpgradeCostCoefficients(3f, 2f);

            Assert.That(economy.GetUnitValue(material), Is.EqualTo(12f).Within(0.001f));
            Assert.That(economy.GetAvailableProduction(material), Is.EqualTo(123));
            Assert.That(economy.GetMesoUpgradeCost(material), Is.EqualTo(63));
            Assert.That(economy.GetProductionUpgradeCost(material), Is.EqualTo(22));

            economy.RecordSupply(material);
            Assert.That(economy.TotalMeso, Is.EqualTo(12));

            economy.SetMesoUpgradeLevel(material, 100);
            Assert.That(economy.GetMesoUpgradeLevel(material), Is.EqualTo(100));
        }

        [Test]
        public void RuntimeDepositsUsePlacementAndRemovalEvents()
        {
            var network = new ExtractionNetwork(
                new RawMaterialDeposit[0],
                new ConveyorNetwork());
            RawMaterialDeposit placed = null;
            RawMaterialDeposit removed = null;
            network.DepositPlaced += deposit => placed = deposit;
            network.DepositRemoved += deposit => removed = deposit;

            var deposit_state = network.PlaceDeposit(
                RawMaterialType.Mushroom,
                new Vector2Int(8, 8));

            Assert.That(placed, Is.SameAs(deposit_state));
            Assert.That(network.CanPlaceDeposit(new Vector2Int(9, 8)), Is.False);

            network.RemoveDeposit(new Vector2Int(9, 8));

            Assert.That(removed, Is.SameAs(deposit_state));
            Assert.That(network.Deposits, Is.Empty);
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
