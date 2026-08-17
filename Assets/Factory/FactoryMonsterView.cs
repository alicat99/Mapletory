using System.Collections.Generic;
using UnityEngine;

namespace Maptory.Factory
{
    public sealed class FactoryMonsterView : MonoBehaviour
    {
        private readonly Dictionary<int, SpriteRenderer> renderers = new();

        private FactoryItemTransport transport;
        private FactoryTileCatalog tile_catalog;
        private Grid grid;
        private Transform monster_root;
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
            monster_root = root;
            map_size = size;
        }

        private void Update()
        {
            foreach (var monster in transport.Monsters)
            {
                if (renderers.ContainsKey(monster.Id)) continue;
                renderers.Add(monster.Id, CreateRenderer(monster));
            }
        }

        private SpriteRenderer CreateRenderer(FactoryMonsterState monster)
        {
            var monster_object = new GameObject($"Monster {monster.Id} {monster.Type}");
            monster_object.transform.SetParent(monster_root, false);
            monster_object.transform.localPosition = grid.GetCellCenterLocal((Vector3Int)monster.Position);
            var renderer = monster_object.AddComponent<SpriteRenderer>();
            renderer.sprite = tile_catalog.GetMonsterSprite(monster.Type);
            renderer.spriteSortPoint = SpriteSortPoint.Pivot;
            renderer.sortingLayerName = FactorySorting.ITEM_SORTING_LAYER;
            renderer.sortingOrder = FactorySorting.GetOrder(
                monster.Position,
                map_size,
                FactorySorting.ITEM_LAYER);
            return renderer;
        }
    }
}
