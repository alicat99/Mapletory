using NUnit.Framework;
using UnityEngine;

namespace Maptory.Factory.Tests
{
    public sealed class DemolitionTests
    {
        [Test]
        public void DemolitionModeClearsConstructionAndToolSelectionExitsDemolition()
        {
            var game_object = new GameObject("Build Mode Test");
            var mode = game_object.AddComponent<FactoryBuildMode>();
            mode.SetActiveTool(FactoryBuildTool.Conveyor);

            mode.ToggleDemolitionMode();

            Assert.That(mode.IsDemolitionMode, Is.True);
            Assert.That(mode.ActiveTool, Is.EqualTo(FactoryBuildTool.None));

            mode.Toggle(FactoryBuildTool.Extractor);

            Assert.That(mode.IsDemolitionMode, Is.False);
            Assert.That(mode.ActiveTool, Is.EqualTo(FactoryBuildTool.Extractor));
            Object.DestroyImmediate(game_object);
        }

        [Test]
        public void RemovingAnyFootprintCellRemovesWholeBuilding()
        {
            var network = new ExtractionNetwork(
                new RawMaterialDeposit[0],
                new ConveyorNetwork());
            var machine = network.PlaceDyeingMachine(new Vector2Int(10, 10), GridDirection.Up);
            object removed = null;
            network.BuildingRemoved += building => removed = building;

            var result = network.RemoveBuilding(new Vector2Int(11, 9));

            Assert.That(result, Is.SameAs(machine));
            Assert.That(removed, Is.SameAs(machine));
            Assert.That(network.DyeingMachines, Is.Empty);
            Assert.That(network.IsBuildingOccupied(machine.Center), Is.False);
        }

        [Test]
        public void ConveyorAndExternalConnectionsCanBeRemoved()
        {
            var network = new ConveyorNetwork();
            network.SetConveyor(Vector2Int.zero, GridDirection.Up);
            network.AddExternalOutput(Vector2Int.zero, GridDirection.Up);

            Assert.That(network.GetOutputDirections(Vector2Int.zero),
                Does.Contain(GridDirection.Up));

            network.RemoveExternalOutput(Vector2Int.zero, GridDirection.Up);

            Assert.That(network.GetOutputDirections(Vector2Int.zero), Is.Empty);
            Assert.That(network.RemoveConveyor(Vector2Int.zero), Is.True);
            Assert.That(network.RemoveConveyor(Vector2Int.zero), Is.False);
        }

        [Test]
        public void RemovingDestinationBuildingCancelsPendingDelivery()
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

            network.RemoveBuilding(portal.Anchor);

            Assert.That(transport.Items, Is.Empty);
        }

        [Test]
        public void DragDemolitionFillsEveryCellBetweenPointerFrames()
        {
            var cells = FactoryDemolitionController.GetLineCells(
                new Vector2Int(2, 3),
                new Vector2Int(7, 5));

            Assert.That(cells[0], Is.EqualTo(new Vector2Int(2, 3)));
            Assert.That(cells[^1], Is.EqualTo(new Vector2Int(7, 5)));
            Assert.That(cells.Count, Is.EqualTo(6));
            for (var index = 1; index < cells.Count; index++)
            {
                var delta = cells[index] - cells[index - 1];
                Assert.That(Mathf.Abs(delta.x), Is.LessThanOrEqualTo(1));
                Assert.That(Mathf.Abs(delta.y), Is.LessThanOrEqualTo(1));
            }
        }
    }
}
