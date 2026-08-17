using UnityEngine;

namespace Maptory.Factory
{
    public static class FactorySorting
    {
        public const int CONVEYOR_LAYER = 0;
        public const int RESOURCE_LAYER = 1;
        public const int EXTRACTOR_LAYER = 2;
        public const int ITEM_LAYER = 3;

        private const int LAYER_COUNT = 4;

        public static int GetOrder(Vector2 position, Vector2Int map_size, int layer)
        {
            var depth_stride = (map_size.x + 1) * LAYER_COUNT;
            var depth = map_size.x + map_size.y - position.x - position.y;
            return Mathf.RoundToInt(depth * depth_stride + position.x * LAYER_COUNT + layer + 10);
        }
    }
}
