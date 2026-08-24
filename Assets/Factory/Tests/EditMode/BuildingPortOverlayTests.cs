using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace Maptory.Factory.Tests
{
    public sealed class BuildingPortOverlayTests
    {
        [Test]
        public void PortSpriteSheetsMapEveryIsometricDirection()
        {
            AssertSheet("Factory/BuildingPorts/InputIcon", "InputIcon");
            AssertSheet("Factory/BuildingPorts/OutputIcon", "OutputIcon");
        }

        private static void AssertSheet(string path, string prefix)
        {
            var sprites = Resources.LoadAll<Sprite>(path);
            Assert.That(sprites, Has.Length.EqualTo(4));
            Assert.That(sprites.Single(sprite => sprite.name == prefix + "U").rect,
                Is.EqualTo(new Rect(0f, 0f, 16f, 16f)));
            Assert.That(sprites.Single(sprite => sprite.name == prefix + "R").rect,
                Is.EqualTo(new Rect(0f, 16f, 16f, 16f)));
            Assert.That(sprites.Single(sprite => sprite.name == prefix + "D").rect,
                Is.EqualTo(new Rect(16f, 16f, 16f, 16f)));
            Assert.That(sprites.Single(sprite => sprite.name == prefix + "L").rect,
                Is.EqualTo(new Rect(16f, 0f, 16f, 16f)));
        }
    }
}
