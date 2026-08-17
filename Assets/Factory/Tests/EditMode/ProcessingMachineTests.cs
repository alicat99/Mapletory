using NUnit.Framework;
using UnityEngine;

namespace Maptory.Factory.Tests
{
    public sealed class ProcessingMachineTests
    {
        [Test]
        public void HornRecipeUsesOneGreenSnailShell()
        {
            var recipe = ProcessingRecipe.All[ProcessingRecipeId.Horn];

            Assert.That(recipe.Ingredients.Count, Is.EqualTo(1));
            Assert.That(recipe.InputMaterial, Is.EqualTo(RawMaterialType.Snail));
            Assert.That(recipe.Result, Is.EqualTo(RawMaterialType.Horn));
            Assert.That(ProcessingRecipe.Categories[0].Name, Is.EqualTo("가공"));
        }

        [Test]
        public void ProcessingMachineUsesCenteredInputAndOutput()
        {
            var network = new ExtractionNetwork(
                new RawMaterialDeposit[0],
                new ConveyorNetwork());
            var center = new Vector2Int(10, 10);
            var machine = network.PlaceProcessingMachine(center, GridDirection.Up);

            Assert.That(machine.InputCount, Is.EqualTo(1));
            Assert.That(machine.GetInputPort(0), Is.EqualTo(center + Vector2Int.left));
            Assert.That(machine.GetInputConveyorPosition(0),
                Is.EqualTo(center + Vector2Int.left * 2));
            Assert.That(machine.OutputConveyorPosition,
                Is.EqualTo(center + Vector2Int.right * 2));
            Assert.That(network.IsBuildingOccupied(center + Vector2Int.one), Is.True);
        }

        [Test]
        public void ProcessingMachineConsumesSnailAndSpawnsHorn()
        {
            var conveyors = new ConveyorNetwork();
            var network = new ExtractionNetwork(new RawMaterialDeposit[0], conveyors);
            var machine = network.PlaceProcessingMachine(
                new Vector2Int(10, 10),
                GridDirection.Up);
            machine.SelectRecipe(ProcessingRecipe.All[ProcessingRecipeId.Horn]);
            var direction = GridDirectionExtensions.FromDelta(machine.Forward);
            conveyors.SetConveyor(machine.GetInputConveyorPosition(0), direction);
            conveyors.SetConveyor(machine.OutputConveyorPosition, direction);
            var transport = new FactoryItemTransport(conveyors, network);
            transport.SpawnItem(RawMaterialType.Snail, machine.GetInputConveyorPosition(0));

            transport.Step();
            transport.Step();

            Assert.That(transport.Items.Count, Is.EqualTo(1));
            Assert.That(transport.Items[0].Material, Is.EqualTo(RawMaterialType.Horn));
            Assert.That(transport.Items[0].Position, Is.EqualTo(machine.OutputConveyorPosition));
            Assert.That(transport.Items[0].ScaleAnimation, Is.EqualTo(ItemScaleAnimation.Spawning));
        }

        [Test]
        public void CombinerConsumesHornAndMushroomToMakeSpikeMushroom()
        {
            var conveyors = new ConveyorNetwork();
            var network = new ExtractionNetwork(new RawMaterialDeposit[0], conveyors);
            var machine = network.PlaceCombiner(new Vector2Int(10, 10), GridDirection.Up);
            machine.SelectRecipe(CombiningRecipe.All[CombiningRecipeId.SpikeMushroom]);
            var direction = GridDirectionExtensions.FromDelta(machine.Forward);
            conveyors.SetConveyor(machine.GetInputConveyorPosition(0), direction);
            conveyors.SetConveyor(machine.GetInputConveyorPosition(1), direction);
            conveyors.SetConveyor(machine.OutputConveyorPosition, direction);
            var transport = new FactoryItemTransport(conveyors, network);
            transport.SpawnItem(RawMaterialType.Horn, machine.GetInputConveyorPosition(0));
            transport.SpawnItem(RawMaterialType.Mushroom, machine.GetInputConveyorPosition(1));

            transport.Step();
            transport.Step();

            Assert.That(transport.Items.Count, Is.EqualTo(1));
            Assert.That(transport.Items[0].Material, Is.EqualTo(RawMaterialType.SpikeMushroom));
            Assert.That(transport.Items[0].Position, Is.EqualTo(machine.OutputConveyorPosition));
        }

        [Test]
        public void ProcessingMachineAndHornHaveRuntimeSprites()
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
                Assert.That(catalog.GetProcessingMachineLowerSprite(direction), Is.Not.Null);
                Assert.That(catalog.GetProcessingMachineUpperSprite(direction), Is.Not.Null);
            }

            Assert.That(catalog.GetItemSprite(RawMaterialType.Horn), Is.Not.Null);
        }
    }
}
