using UnityEngine;

namespace Maptory.Factory
{
    public enum GridDirection
    {
        Up,
        Right,
        Down,
        Left
    }

    public static class GridDirectionExtensions
    {
        public static GridDirection FromDelta(Vector2Int delta)
        {
            if (delta.x > 0)
            {
                return GridDirection.Up;
            }

            if (delta.x < 0)
            {
                return GridDirection.Down;
            }

            if (delta.y > 0)
            {
                return GridDirection.Left;
            }

            if (delta.y < 0)
            {
                return GridDirection.Right;
            }

            return GridDirection.Up;
        }

        public static Vector2Int ToOffset(this GridDirection direction)
        {
            return direction switch
            {
                GridDirection.Up => Vector2Int.right,
                GridDirection.Right => Vector2Int.down,
                GridDirection.Down => Vector2Int.left,
                GridDirection.Left => Vector2Int.up,
                _ => throw new System.ArgumentOutOfRangeException(nameof(direction))
            };
        }

        public static char ToSpriteCode(this GridDirection direction)
        {
            return direction switch
            {
                GridDirection.Up => 'U',
                GridDirection.Right => 'R',
                GridDirection.Down => 'D',
                GridDirection.Left => 'L',
                _ => throw new System.ArgumentOutOfRangeException(nameof(direction))
            };
        }

        public static GridDirection Opposite(this GridDirection direction)
        {
            return direction switch
            {
                GridDirection.Up => GridDirection.Down,
                GridDirection.Right => GridDirection.Left,
                GridDirection.Down => GridDirection.Up,
                GridDirection.Left => GridDirection.Right,
                _ => throw new System.ArgumentOutOfRangeException(nameof(direction))
            };
        }
    }
}
