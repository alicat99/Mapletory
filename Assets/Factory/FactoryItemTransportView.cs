using System.Collections.Generic;
using UnityEngine;

namespace Maptory.Factory
{
    public sealed class FactoryItemTransportView : MonoBehaviour
    {
        private static readonly Vector3 ITEM_SURFACE_OFFSET = new(0f, 0.5f, 0f);

        private readonly Dictionary<int, SpriteRenderer> item_renderers = new();

        private FactoryItemTransport transport;
        private FactoryTileCatalog tile_catalog;
        private Grid grid;
        private Transform item_root;
        private Vector2Int map_size;

        public void Initialize(
            FactoryItemTransport item_transport,
            FactoryTileCatalog catalog,
            Grid map_grid,
            Transform root,
            Vector2Int size)
        {
            transport = item_transport;
            tile_catalog = catalog;
            grid = map_grid;
            item_root = root;
            map_size = size;
        }

        private void Update()
        {
            transport.Update(Time.deltaTime);
            DrawItems();
        }

        private void DrawItems()
        {
            var progress = transport.StepProgress;

            foreach (var item in transport.Items)
            {
                if (!item_renderers.TryGetValue(item.Id, out var renderer))
                {
                    renderer = CreateRenderer(item);
                    item_renderers.Add(item.Id, renderer);
                }

                var from = grid.GetCellCenterLocal((Vector3Int)item.Position);
                var target = grid.GetCellCenterLocal((Vector3Int)item.TargetPosition);
                renderer.transform.localPosition = Vector3.Lerp(from, target, progress)
                    + ITEM_SURFACE_OFFSET;
                renderer.transform.localScale = item.IsSpawning
                    ? Vector3.one * progress
                    : Vector3.one;

                var grid_position = Vector2.Lerp(item.Position, item.TargetPosition, progress);
                renderer.sortingOrder = FactorySorting.GetOrder(
                    grid_position,
                    map_size,
                    FactorySorting.ITEM_LAYER);
            }
        }

        private SpriteRenderer CreateRenderer(FactoryItemState item)
        {
            var item_object = new GameObject($"Item {item.Id} {item.Material}");
            item_object.transform.SetParent(item_root, false);
            var renderer = item_object.AddComponent<SpriteRenderer>();
            renderer.sprite = tile_catalog.GetItemSprite(item.Material);
            renderer.spriteSortPoint = SpriteSortPoint.Pivot;
            return renderer;
        }
    }
}
