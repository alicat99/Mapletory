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
        private readonly Dictionary<Vector2Int, List<GridDirection>> external_inputs = new();
        private readonly Dictionary<Vector2Int, List<GridDirection>> external_outputs = new();

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

        public bool RemoveConveyor(Vector2Int position)
        {
            return conveyors.Remove(position);
        }

        public void AddExternalInput(Vector2Int position, GridDirection direction)
        {
            if (!external_inputs.TryGetValue(position, out var inputs))
            {
                inputs = new List<GridDirection>();
                external_inputs.Add(position, inputs);
            }

            inputs.Add(direction);
        }

        public void AddExternalOutput(Vector2Int position, GridDirection direction)
        {
            if (!external_outputs.TryGetValue(position, out var outputs))
            {
                outputs = new List<GridDirection>();
                external_outputs.Add(position, outputs);
            }

            outputs.Add(direction);
        }

        public void RemoveExternalInput(Vector2Int position, GridDirection direction)
        {
            RemoveExternalConnection(external_inputs, position, direction);
        }

        public void RemoveExternalOutput(Vector2Int position, GridDirection direction)
        {
            RemoveExternalConnection(external_outputs, position, direction);
        }

        public List<GridDirection> GetOutputDirections(Vector2Int position)
        {
            var outputs = GetConveyorOutputDirections(position);

            if (external_outputs.TryGetValue(position, out var registered_outputs))
            {
                outputs.AddRange(registered_outputs);
            }

            return outputs;
        }

        private List<GridDirection> GetConveyorOutputDirections(Vector2Int position)
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
                    && (direction == conveyor.Direction || neighbor.Direction == direction))
                {
                    outputs.Add(direction);
                }
            }

            return outputs;
        }

        public string GetSpriteName(Vector2Int position)
        {
            var conveyor = conveyors[position];
            var input_direction = GetInputDirection(position, conveyor);
            var outputs = GetOutputDirections(position);
            var output_code = outputs.Count switch
            {
                0 => 'X',
                1 => outputs[0].ToSpriteCode(),
                _ => 'A'
            };

            return $"Conveyor{input_direction.ToSpriteCode()}{output_code}";
        }

        public bool TrySelectNextOutput(Vector2Int position, out GridDirection output)
        {
            var outputs = GetConveyorOutputDirections(position);
            if (outputs.Count == 0)
            {
                output = default;
                return false;
            }

            output = conveyors[position].SelectNextOutput(outputs);
            return true;
        }

        private GridDirection GetInputDirection(Vector2Int position, ConveyorTile conveyor)
        {
            var input_direction = conveyor.Direction;
            var input_count = 0;

            if (external_inputs.TryGetValue(position, out var inputs))
            {
                foreach (var direction in inputs)
                {
                    input_direction = direction;
                    input_count++;
                }
            }

            foreach (var direction in DIRECTIONS)
            {
                var neighbor_position = position - direction.ToOffset();
                if (!conveyors.TryGetValue(neighbor_position, out var neighbor)
                    || neighbor.Direction != direction)
                {
                    continue;
                }

                input_direction = direction;
                input_count++;
            }

            return input_count == 1 ? input_direction : conveyor.Direction;
        }

        private static void RemoveExternalConnection(
            IDictionary<Vector2Int, List<GridDirection>> connections,
            Vector2Int position,
            GridDirection direction)
        {
            if (!connections.TryGetValue(position, out var directions)) return;

            directions.Remove(direction);
            if (directions.Count == 0) connections.Remove(position);
        }
    }
}
