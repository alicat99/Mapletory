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
            Assert.That(network.GetSpriteName(new Vector2Int(0, 0)), Is.EqualTo("ConveyorUU"));
            Assert.That(network.GetSpriteName(new Vector2Int(1, 0)), Is.EqualTo("ConveyorUU"));
            Assert.That(network.GetSpriteName(new Vector2Int(2, 0)), Is.EqualTo("ConveyorUX"));
        }

        [Test]
        public void DirectionsFollowIsometricScreenOrientation()
        {
            Assert.That(GridDirection.Up.ToOffset(), Is.EqualTo(Vector2Int.right));
            Assert.That(GridDirection.Right.ToOffset(), Is.EqualTo(Vector2Int.down));
            Assert.That(GridDirection.Down.ToOffset(), Is.EqualTo(Vector2Int.left));
            Assert.That(GridDirection.Left.ToOffset(), Is.EqualTo(Vector2Int.up));
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
            network.SetConveyor(Vector2Int.zero, GridDirection.Up);

            network.SetConveyor(Vector2Int.zero, GridDirection.Left);

            Assert.That(network.Conveyors[Vector2Int.zero].Direction, Is.EqualTo(GridDirection.Left));
            Assert.That(network.GetSpriteName(Vector2Int.zero), Is.EqualTo("ConveyorLX"));
        }

        [Test]
        public void OutwardNeighborsCreateAllDirectionOutput()
        {
            var network = new ConveyorNetwork();
            network.SetConveyor(Vector2Int.zero, GridDirection.Up);
            network.SetConveyor(Vector2Int.right, GridDirection.Up);
            network.SetConveyor(Vector2Int.up, GridDirection.Left);

            Assert.That(network.GetSpriteName(Vector2Int.zero), Is.EqualTo("ConveyorUA"));
        }

        [Test]
        public void SingleIncomingConveyorCreatesTurnSprite()
        {
            var network = new ConveyorNetwork();
            network.SetConveyor(Vector2Int.left, GridDirection.Up);
            network.SetConveyor(Vector2Int.zero, GridDirection.Right);
            network.SetConveyor(Vector2Int.down, GridDirection.Right);

            Assert.That(network.GetSpriteName(Vector2Int.zero), Is.EqualTo("ConveyorUR"));
        }

        [Test]
        public void ExtractorOutputCreatesTurnSpriteOnFirstConveyor()
        {
            var network = new ConveyorNetwork();
            var first_conveyor = new Vector2Int(2, 0);
            network.SetConveyor(first_conveyor, GridDirection.Right);
            network.SetConveyor(new Vector2Int(2, -1), GridDirection.Right);

            network.AddExternalInput(first_conveyor, GridDirection.Up);

            Assert.That(network.GetSpriteName(first_conveyor), Is.EqualTo("ConveyorUR"));
        }

        [Test]
        public void OutputToTurningConveyorUsesConnectedDirectionSprite()
        {
            var network = new ConveyorNetwork();
            network.SetConveyor(Vector2Int.zero, GridDirection.Up);
            network.SetConveyor(Vector2Int.right, GridDirection.Right);

            Assert.That(network.GetSpriteName(Vector2Int.zero), Is.EqualTo("ConveyorUU"));
        }

        [Test]
        public void MultipleIncomingConveyorsDoNotChangeDestinationSprite()
        {
            var network = new ConveyorNetwork();
            network.SetConveyor(Vector2Int.zero, GridDirection.Up);
            network.SetConveyor(Vector2Int.left, GridDirection.Up);
            network.SetConveyor(Vector2Int.down, GridDirection.Left);
            network.SetConveyor(Vector2Int.right, GridDirection.Up);

            Assert.That(network.GetSpriteName(Vector2Int.zero), Is.EqualTo("ConveyorUU"));
        }

        [Test]
        public void DistributorCyclesAcrossAvailableOutputs()
        {
            var network = new ConveyorNetwork();
            network.SetConveyor(Vector2Int.zero, GridDirection.Up);
            network.SetConveyor(Vector2Int.right, GridDirection.Up);
            network.SetConveyor(Vector2Int.up, GridDirection.Left);

            Assert.That(network.TrySelectNextOutput(Vector2Int.zero, out var first), Is.True);
            Assert.That(network.TrySelectNextOutput(Vector2Int.zero, out var second), Is.True);
            Assert.That(network.TrySelectNextOutput(Vector2Int.zero, out var third), Is.True);
            Assert.That(first, Is.EqualTo(GridDirection.Up));
            Assert.That(second, Is.EqualTo(GridDirection.Left));
            Assert.That(third, Is.EqualTo(GridDirection.Up));
        }

        [Test]
        public void TwoInputTwoOutputCrossingKeepsEachEntryDirectionStraight()
        {
            var network = new ConveyorNetwork();
            network.SetConveyor(Vector2Int.zero, GridDirection.Up);
            network.SetConveyor(Vector2Int.left, GridDirection.Up);
            network.SetConveyor(Vector2Int.down, GridDirection.Left);
            network.SetConveyor(Vector2Int.right, GridDirection.Up);
            network.SetConveyor(Vector2Int.up, GridDirection.Left);

            Assert.That(network.TrySelectNextOutput(
                Vector2Int.zero,
                GridDirection.Up,
                out var horizontal), Is.True);
            Assert.That(network.TrySelectNextOutput(
                Vector2Int.zero,
                GridDirection.Left,
                out var vertical), Is.True);
            Assert.That(horizontal, Is.EqualTo(GridDirection.Up));
            Assert.That(vertical, Is.EqualTo(GridDirection.Left));
        }

        [Test]
        public void ConveyorDoesNotOutputBackTowardItsInput()
        {
            var network = new ConveyorNetwork();
            network.SetConveyor(Vector2Int.zero, GridDirection.Up);
            network.SetConveyor(Vector2Int.left, GridDirection.Down);

            Assert.That(network.GetSpriteName(Vector2Int.zero), Is.EqualTo("ConveyorUX"));
        }
    }
}
