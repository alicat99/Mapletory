using System;
using System.Collections.Generic;
using UnityEngine;

namespace Maptory.Factory
{
    public sealed class ConveyorTile
    {
        public GridDirection Direction { get; private set; }

        private int next_output_index;

        public ConveyorTile(GridDirection direction)
        {
            Direction = direction;
        }

        public void SetDirection(GridDirection direction)
        {
            Direction = direction;
            next_output_index = 0;
        }

        public GridDirection SelectNextOutput(IReadOnlyList<GridDirection> outputs)
        {
            var output = outputs[next_output_index % outputs.Count];
            next_output_index = (next_output_index + 1) % outputs.Count;
            return output;
        }
    }

    public sealed class ConveyorNetwork
    {
        private static readonly GridDirection[] DIRECTIONS =
        {
            GridDirection.Up,
            GridDirection.Right,
            GridDirection.Down,
            GridDirection.Left
        };

        private readonly Dictionary<Vector2Int, ConveyorTile> conveyors = new();

        public IReadOnlyDictionary<Vector2Int, ConveyorTile> Conveyors => conveyors;

        public void PlaceLine(Vector2Int start, Vector2Int end)
        {
            var delta = end - start;
            if (delta.x != 0 && delta.y != 0)
            {
                throw new ArgumentException("Conveyor lines must be horizontal or vertical.");
            }

            var direction = GridDirectionExtensions.FromDelta(delta);
            var length = Mathf.Abs(delta.x) + Mathf.Abs(delta.y);
            var offset = direction.ToOffset();

            for (var index = 0; index <= length; index++)
            {
                SetConveyor(start + offset * index, direction);
            }
        }

        public void SetConveyor(Vector2Int position, GridDirection direction)
        {
            if (conveyors.TryGetValue(position, out var conveyor))
            {
                conveyor.SetDirection(direction);
                return;
            }

            conveyors.Add(position, new ConveyorTile(direction));
        }

        public List<GridDirection> GetOutputDirections(Vector2Int position)
        {
            var conveyor = conveyors[position];
            var outputs = new List<GridDirection>(3);

            foreach (var direction in DIRECTIONS)
            {
                if (direction == conveyor.Direction.Opposite())
                {
                    continue;
                }

                var neighbor_position = position + direction.ToOffset();
                if (conveyors.TryGetValue(neighbor_position, out var neighbor)
                    && neighbor.Direction == direction)
                {
                    outputs.Add(direction);
                }
            }

            return outputs;
        }

        public string GetSpriteName(Vector2Int position)
        {
            var conveyor = conveyors[position];
            var outputs = GetOutputDirections(position);
            var output_code = outputs.Count switch
            {
                0 => 'X',
                1 => outputs[0].ToSpriteCode(),
                _ => 'A'
            };

            return $"Conveyor{conveyor.Direction.ToSpriteCode()}{output_code}";
        }

        public bool TrySelectNextOutput(Vector2Int position, out GridDirection output)
        {
            var outputs = GetOutputDirections(position);
            if (outputs.Count == 0)
            {
                output = default;
                return false;
            }

            output = conveyors[position].SelectNextOutput(outputs);
            return true;
        }

    }
}
