using NUnit.Framework;
using UnityEngine;

namespace Maptory.Factory.Tests
{
    public sealed class CombinerAndErdaInjectorTests
    {
        [Test]
        public void CombinerProvidesThreeSecondaryDyeRecipes()
        {
            Assert.That(CombiningRecipe.All.Count, Is.EqualTo(3));
            AssertRecipe(
                CombiningRecipe.All[CombiningRecipeId.DyeOrange],
                RawMaterialType.DyeRed,
                RawMaterialType.DyeYellow,
                RawMaterialType.DyeOrange);
            AssertRecipe(
                CombiningRecipe.All[CombiningRecipeId.DyePurple],
                RawMaterialType.DyeRed,
                RawMaterialType.DyeBlue,
                RawMaterialType.DyePurple);
            AssertRecipe(
                CombiningRecipe.All[CombiningRecipeId.DyeGreen],
                RawMaterialType.DyeBlue,
                RawMaterialType.DyeYellow,
                RawMaterialType.DyeGreen);
        }

        [Test]
        public void CombinerUsesDyeingMachinePortsAndThreeByThreeFootprint()
        {
            var network = new ExtractionNetwork(
                new RawMaterialDeposit[0],
                new ConveyorNetwork());
            var center = new Vector2Int(10, 10);
            var machine = network.PlaceCombiner(center, GridDirection.Up);

            Assert.That(machine.SelectedRecipe, Is.Null);
            Assert.That(machine.GetInputConveyorPosition(0),
                Is.EqualTo(center + new Vector2Int(-2, 1)));
            Assert.That(machine.GetInputConveyorPosition(1),
                Is.EqualTo(center + new Vector2Int(-2, -1)));
            Assert.That(machine.OutputConveyorPosition,
                Is.EqualTo(center + new Vector2Int(2, 0)));
            Assert.That(network.IsBuildingOccupied(center + Vector2Int.one), Is.True);
        }

        [Test]
        public void CombinerConsumesBothDyesAndSpawnsResultOnOutputConveyor()
        {
            var conveyors = new ConveyorNetwork();
            var network = new ExtractionNetwork(new RawMaterialDeposit[0], conveyors);
            var machine = network.PlaceCombiner(new Vector2Int(10, 10), GridDirection.Up);
            machine.SelectRecipe(CombiningRecipe.All[CombiningRecipeId.DyePurple]);
            var direction = GridDirectionExtensions.FromDelta(machine.Forward);
            conveyors.SetConveyor(machine.GetInputConveyorPosition(0), direction);
            conveyors.SetConveyor(machine.GetInputConveyorPosition(1), direction);
            conveyors.SetConveyor(machine.OutputConveyorPosition, GridDirection.Up);
            var transport = new FactoryItemTransport(conveyors, network);
            transport.SpawnItem(RawMaterialType.DyeRed, machine.GetInputConveyorPosition(0));
            transport.SpawnItem(RawMaterialType.DyeBlue, machine.GetInputConveyorPosition(1));

            transport.Step();
            transport.Step();

            Assert.That(transport.Items.Count, Is.EqualTo(1));
            Assert.That(transport.Items[0].Material, Is.EqualTo(RawMaterialType.DyePurple));
            Assert.That(transport.Items[0].Position, Is.EqualTo(machine.OutputConveyorPosition));
            Assert.That(transport.Items[0].ScaleAnimation, Is.EqualTo(ItemScaleAnimation.Spawning));
        }

        [Test]
        public void ErdaInjectorUsesOneCellAndProducesAllSevenTransportItems()
        {
            Assert.That(ErdaInjectionRecipes.All.Count, Is.EqualTo(7));

            foreach (var recipe in ErdaInjectionRecipes.All)
            {
                var conveyors = new ConveyorNetwork();
                var network = new ExtractionNetwork(new RawMaterialDeposit[0], conveyors);
                var injector = network.PlaceErdaInjector(
                    new Vector2Int(10, 10),
                    GridDirection.Up);
                var direction = GridDirectionExtensions.FromDelta(injector.Forward);
                conveyors.SetConveyor(injector.InputConveyorPosition, direction);
                conveyors.SetConveyor(injector.OutputConveyorPosition, direction);
                conveyors.SetConveyor(injector.OutputConveyorPosition + injector.Forward, direction);
                var transport = new FactoryItemTransport(conveyors, network);
                transport.SpawnItem(recipe.Key, injector.InputConveyorPosition);

                transport.Step();
                transport.Step();

                Assert.That(transport.Items.Count, Is.EqualTo(1));
                Assert.That(transport.Items[0].Material, Is.EqualTo(recipe.Value));
                Assert.That(transport.Items[0].Position, Is.EqualTo(injector.OutputConveyorPosition));
                Assert.That(transport.Items[0].ScaleAnimation, Is.EqualTo(ItemScaleAnimation.Spawning));

                transport.Step();

                Assert.That(transport.Items[0].TargetPosition,
                    Is.EqualTo(injector.OutputConveyorPosition + injector.Forward));
                Assert.That(network.IsBuildingOccupied(injector.Center), Is.True);
                Assert.That(network.IsBuildingOccupied(injector.Center + Vector2Int.up), Is.False);
            }
        }

        [Test]
        public void ErdaInjectorWaitsForClearOutputConveyor()
        {
            var conveyors = new ConveyorNetwork();
            var network = new ExtractionNetwork(new RawMaterialDeposit[0], conveyors);
            var injector = network.PlaceErdaInjector(new Vector2Int(10, 10), GridDirection.Up);
            var direction = GridDirectionExtensions.FromDelta(injector.Forward);
            conveyors.SetConveyor(injector.InputConveyorPosition, direction);
            var transport = new FactoryItemTransport(conveyors, network);
            transport.SpawnItem(RawMaterialType.SnailRed, injector.InputConveyorPosition);

            transport.Step();
            transport.Step();

            Assert.That(transport.Items, Is.Empty);
            Assert.That(injector.CanProduce, Is.True);

            conveyors.SetConveyor(injector.OutputConveyorPosition, direction);
            transport.Step();

            Assert.That(transport.Items.Count, Is.EqualTo(1));
            Assert.That(transport.Items[0].Material, Is.EqualTo(RawMaterialType.MonsterSnailRed));
            Assert.That(injector.CanProduce, Is.False);
        }

        [Test]
        public void ErdaInjectorDoesNotOverlapOccupiedOutput()
        {
            var conveyors = new ConveyorNetwork();
            var network = new ExtractionNetwork(new RawMaterialDeposit[0], conveyors);
            var injector = network.PlaceErdaInjector(new Vector2Int(10, 10), GridDirection.Up);
            var direction = GridDirectionExtensions.FromDelta(injector.Forward);
            conveyors.SetConveyor(injector.InputConveyorPosition, direction);
            conveyors.SetConveyor(injector.OutputConveyorPosition, direction);
            var transport = new FactoryItemTransport(conveyors, network);
            transport.SpawnItem(RawMaterialType.DyeRed, injector.OutputConveyorPosition);
            transport.SpawnItem(RawMaterialType.SnailRed, injector.InputConveyorPosition);

            transport.Step();
            transport.Step();

            Assert.That(transport.Items.Count, Is.EqualTo(1));
            Assert.That(transport.Items[0].Material, Is.EqualTo(RawMaterialType.DyeRed));
            Assert.That(transport.Items[0].Position, Is.EqualTo(injector.OutputConveyorPosition));
            Assert.That(injector.CanProduce, Is.True);
        }

        [Test]
        public void ErdaInjectorRejectsUnregisteredItemsAndResourceOverlap()
        {
            var conveyors = new ConveyorNetwork();
            var network = new ExtractionNetwork(new[]
            {
                new RawMaterialDeposit(RawMaterialType.DyeBlue, Vector2Int.zero)
            }, conveyors);
            Assert.That(network.CanPlaceErdaInjector(Vector2Int.zero), Is.False);
            Assert.That(network.CanPlaceErdaInjector(Vector2Int.one), Is.False);

            var injector = network.PlaceErdaInjector(new Vector2Int(10, 10), GridDirection.Up);
            var direction = GridDirectionExtensions.FromDelta(injector.Forward);
            conveyors.SetConveyor(injector.InputConveyorPosition, direction);
            var transport = new FactoryItemTransport(conveyors, network);
            transport.SpawnItem(RawMaterialType.DyeRed, injector.InputConveyorPosition);

            transport.Step();
            transport.Step();

            Assert.That(transport.Items.Count, Is.EqualTo(1));
            Assert.That(transport.Items[0].Position, Is.EqualTo(injector.InputConveyorPosition));
        }

        [Test]
        public void NewBuildingsAndErdaOutputsHaveRuntimeSprites()
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
                Assert.That(catalog.GetCombinerLowerSprite(direction), Is.Not.Null);
                Assert.That(catalog.GetCombinerUpperSprite(direction), Is.Not.Null);
                Assert.That(catalog.GetErdaInjectorLowerSprite(direction), Is.Not.Null);
                Assert.That(catalog.GetErdaInjectorUpperSprite(direction), Is.Not.Null);
            }

            foreach (var output in ErdaInjectionRecipes.All.Values)
            {
                var sprite = catalog.GetItemSprite(output);
                Assert.That(sprite, Is.Not.Null);
                Assert.That(sprite.pivot.x / sprite.rect.width, Is.EqualTo(0.5f).Within(0.001f));
                Assert.That(sprite.pivot.y / sprite.rect.height, Is.EqualTo(0.25f).Within(0.001f));
            }
        }

        private static void AssertRecipe(
            CombiningRecipe recipe,
            RawMaterialType first,
            RawMaterialType second,
            RawMaterialType result)
        {
            Assert.That(recipe.FirstMaterial, Is.EqualTo(first));
            Assert.That(recipe.SecondMaterial, Is.EqualTo(second));
            Assert.That(recipe.Result, Is.EqualTo(result));
        }
    }
}
