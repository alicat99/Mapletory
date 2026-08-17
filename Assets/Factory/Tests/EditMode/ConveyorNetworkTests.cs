using System;
using NUnit.Framework;
using UnityEngine;

namespace Maptory.Factory.Tests
{
    public sealed class ConveyorNetworkTests
    {
        [Test]
        public void PlaceLineCreatesStraightConveyorsWithTerminalEnd()
        {
            var network = new ConveyorNetwork();

            network.PlaceLine(Vector2Int.zero, new Vector2Int(2, 0));

            Assert.That(network.Conveyors.Count, Is.EqualTo(3));
            Assert.That(network.GetSpriteName(new Vector2Int(0, 0)), Is.EqualTo("ConveyorRR"));
            Assert.That(network.GetSpriteName(new Vector2Int(1, 0)), Is.EqualTo("ConveyorRR"));
            Assert.That(network.GetSpriteName(new Vector2Int(2, 0)), Is.EqualTo("ConveyorRX"));
        }

        [Test]
        public void PlaceLineRejectsDiagonalPlacement()
        {
            var network = new ConveyorNetwork();

            Assert.Throws<ArgumentException>(() =>
                network.PlaceLine(Vector2Int.zero, new Vector2Int(1, 1)));
        }

        [Test]
        public void SetConveyorOverwritesExistingDirection()
        {
            var network = new ConveyorNetwork();
            network.SetConveyor(Vector2Int.zero, GridDirection.Right);

            network.SetConveyor(Vector2Int.zero, GridDirection.Up);

            Assert.That(network.Conveyors[Vector2Int.zero].Direction, Is.EqualTo(GridDirection.Up));
            Assert.That(network.GetSpriteName(Vector2Int.zero), Is.EqualTo("ConveyorUX"));
        }

        [Test]
        public void OutwardNeighborsCreateAllDirectionOutput()
        {
            var network = new ConveyorNetwork();
            network.SetConveyor(Vector2Int.zero, GridDirection.Right);
            network.SetConveyor(Vector2Int.right, GridDirection.Right);
            network.SetConveyor(Vector2Int.up, GridDirection.Up);

            Assert.That(network.GetSpriteName(Vector2Int.zero), Is.EqualTo("ConveyorRA"));
        }

        [Test]
        public void MultipleIncomingConveyorsDoNotChangeDestinationSprite()
        {
            var network = new ConveyorNetwork();
            network.SetConveyor(Vector2Int.zero, GridDirection.Right);
            network.SetConveyor(Vector2Int.left, GridDirection.Right);
            network.SetConveyor(Vector2Int.up, GridDirection.Down);
            network.SetConveyor(Vector2Int.right, GridDirection.Right);

            Assert.That(network.GetSpriteName(Vector2Int.zero), Is.EqualTo("ConveyorRR"));
        }

        [Test]
        public void DistributorCyclesAcrossAvailableOutputs()
        {
            var network = new ConveyorNetwork();
            network.SetConveyor(Vector2Int.zero, GridDirection.Right);
            network.SetConveyor(Vector2Int.right, GridDirection.Right);
            network.SetConveyor(Vector2Int.up, GridDirection.Up);

            Assert.That(network.TrySelectNextOutput(Vector2Int.zero, out var first), Is.True);
            Assert.That(network.TrySelectNextOutput(Vector2Int.zero, out var second), Is.True);
            Assert.That(network.TrySelectNextOutput(Vector2Int.zero, out var third), Is.True);
            Assert.That(first, Is.EqualTo(GridDirection.Up));
            Assert.That(second, Is.EqualTo(GridDirection.Right));
            Assert.That(third, Is.EqualTo(GridDirection.Up));
        }

        [Test]
        public void ConveyorDoesNotOutputBackTowardItsInput()
        {
            var network = new ConveyorNetwork();
            network.SetConveyor(Vector2Int.zero, GridDirection.Right);
            network.SetConveyor(Vector2Int.left, GridDirection.Left);

            Assert.That(network.GetSpriteName(Vector2Int.zero), Is.EqualTo("ConveyorRX"));
        }
    }
}
