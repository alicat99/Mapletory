using NUnit.Framework;
using UnityEngine;

namespace Maptory.Factory.Tests
{
    public sealed class ConstructionGridOverlayTests
    {
        [Test]
        public void UsesConstantSizeMeshAndOnlyShowsDuringConstruction()
        {
            var root = new GameObject("Construction Grid Test");
            var grid = root.AddComponent<Grid>();
            grid.cellLayout = GridLayout.CellLayout.Isometric;
            grid.cellSize = new Vector3(1f, 0.5f, 1f);
            var build_mode = root.AddComponent<FactoryBuildMode>();
            var overlay = root.AddComponent<ConstructionGridOverlay>();

            overlay.Initialize(grid, build_mode, new Vector2Int(100000, 100000));

            var mesh = root.GetComponent<MeshFilter>().sharedMesh;
            var renderer = root.GetComponent<MeshRenderer>();
            Assert.That(mesh.vertexCount, Is.EqualTo(4));
            Assert.That(renderer.enabled, Is.False);

            build_mode.SetActiveTool(FactoryBuildTool.Conveyor);
            Assert.That(renderer.enabled, Is.True);

            build_mode.SetActiveTool(FactoryBuildTool.None);
            Assert.That(renderer.enabled, Is.False);
            Object.DestroyImmediate(root);
        }
    }
}
