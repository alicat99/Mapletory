using UnityEngine;

namespace Maptory.Factory
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public sealed class ConstructionGridOverlay : MonoBehaviour
    {
        private const string SHADER_RESOURCE_PATH =
            "Factory/Construction/ConstructionGridOverlay";

        private FactoryBuildMode build_mode;
        private Mesh grid_mesh;
        private Material grid_material;
        private MeshRenderer grid_renderer;

        public void Initialize(Grid map_grid, FactoryBuildMode mode, Vector2Int map_size)
        {
            build_mode = mode;
            grid_renderer = GetComponent<MeshRenderer>();
            grid_mesh = CreateMesh(map_grid, map_size);
            grid_material = new Material(Resources.Load<Shader>(SHADER_RESOURCE_PATH))
            {
                hideFlags = HideFlags.DontSave
            };

            GetComponent<MeshFilter>().sharedMesh = grid_mesh;
            grid_renderer.sharedMaterial = grid_material;
            grid_renderer.sortingLayerName = "Default";
            grid_renderer.sortingOrder = 1;
            grid_renderer.enabled = IsInteractionModeActive();
            build_mode.Changed += OnBuildToolChanged;
            build_mode.DemolitionChanged += OnDemolitionChanged;
        }

        private void OnDestroy()
        {
            if (build_mode != null)
            {
                build_mode.Changed -= OnBuildToolChanged;
                build_mode.DemolitionChanged -= OnDemolitionChanged;
            }

            if (Application.isPlaying)
            {
                Destroy(grid_mesh);
                Destroy(grid_material);
                return;
            }

            DestroyImmediate(grid_mesh);
            DestroyImmediate(grid_material);
        }

        private void OnBuildToolChanged(FactoryBuildTool tool)
        {
            grid_renderer.enabled = IsInteractionModeActive();
        }

        private void OnDemolitionChanged(bool active)
        {
            grid_renderer.enabled = IsInteractionModeActive();
        }

        private bool IsInteractionModeActive()
        {
            return build_mode.ActiveTool != FactoryBuildTool.None || build_mode.IsDemolitionMode;
        }

        private static Mesh CreateMesh(Grid map_grid, Vector2Int map_size)
        {
            var min_x = 0f;
            var min_y = 0f;
            var max_x = map_size.x;
            var max_y = map_size.y;
            var mesh = new Mesh
            {
                name = "Construction Grid Overlay",
                hideFlags = HideFlags.DontSave,
                vertices = new[]
                {
                    map_grid.CellToLocalInterpolated(new Vector3(min_x, min_y)),
                    map_grid.CellToLocalInterpolated(new Vector3(max_x, min_y)),
                    map_grid.CellToLocalInterpolated(new Vector3(max_x, max_y)),
                    map_grid.CellToLocalInterpolated(new Vector3(min_x, max_y))
                },
                uv = new[]
                {
                    Vector2.zero,
                    new Vector2(map_size.x, 0f),
                    new Vector2(map_size.x, map_size.y),
                    new Vector2(0f, map_size.y)
                },
                triangles = new[] { 0, 1, 2, 0, 2, 3 }
            };
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
