using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Maptory.Factory
{
    public sealed class FactoryItemState
    {
        public int Id { get; }
        public RawMaterialType Material { get; }
        public Vector2Int Position { get; internal set; }
        public Vector2Int TargetPosition { get; internal set; }
        public bool IsSpawning { get; internal set; }

        public FactoryItemState(int id, RawMaterialType material, Vector2Int position)
        {
            Id = id;
            Material = material;
            Position = position;
            TargetPosition = position;
        }
    }

    public sealed class FairMergeSelector
    {
        private readonly Dictionary<Vector2Int, int> next_source_indices = new();

        public Vector2Int SelectSource(Vector2Int destination, IReadOnlyList<Vector2Int> sources)
        {
            var ordered_sources = sources.OrderBy(position => position.x)
                .ThenBy(position => position.y)
                .ToArray();
            next_source_indices.TryGetValue(destination, out var next_index);
            var selected_source = ordered_sources[next_index % ordered_sources.Length];
            next_source_indices[destination] = (next_index + 1) % ordered_sources.Length;
            return selected_source;
        }
    }

    public sealed class FactoryItemTransport
    {
        public const float STEP_DURATION = 0.45f;

        private const int PRODUCTION_STEP_INTERVAL = 3;

        private readonly ConveyorNetwork conveyor_network;
        private readonly ExtractionNetwork extraction_network;
        private readonly FairMergeSelector merge_selector = new();
        private readonly Dictionary<Vector2Int, int> production_steps = new();
        private readonly List<FactoryItemState> items = new();
        private float elapsed_step_time;
        private int next_item_id;

        public IReadOnlyList<FactoryItemState> Items => items;
        public float StepProgress => elapsed_step_time / STEP_DURATION;

        public FactoryItemTransport(
            ConveyorNetwork conveyor_network,
            ExtractionNetwork extraction_network)
        {
            this.conveyor_network = conveyor_network;
            this.extraction_network = extraction_network;
        }

        public void Update(float delta_time)
        {
            elapsed_step_time += delta_time;

            while (elapsed_step_time >= STEP_DURATION)
            {
                elapsed_step_time -= STEP_DURATION;
                Step();
            }
        }

        public void Step()
        {
            CommitMoves();
            PlanConveyorMoves();
            ProduceItems();
        }

        private void CommitMoves()
        {
            foreach (var item in items)
            {
                item.Position = item.TargetPosition;
                item.IsSpawning = false;
            }
        }

        private void PlanConveyorMoves()
        {
            var proposals = new List<MoveProposal>();

            foreach (var item in items)
            {
                item.TargetPosition = item.Position;

                if (conveyor_network.Conveyors.ContainsKey(item.Position)
                    && conveyor_network.TrySelectNextOutput(item.Position, out var output))
                {
                    proposals.Add(new MoveProposal(item, item.Position + output.ToOffset()));
                }
            }

            var accepted_moves = SelectDestinationWinners(proposals);
            RemoveBlockedMoves(accepted_moves);

            foreach (var pair in accepted_moves)
            {
                pair.Key.TargetPosition = pair.Value;
            }
        }

        private Dictionary<FactoryItemState, Vector2Int> SelectDestinationWinners(
            IReadOnlyList<MoveProposal> proposals)
        {
            var accepted_moves = new Dictionary<FactoryItemState, Vector2Int>();

            foreach (var destination_group in proposals.GroupBy(proposal => proposal.Destination))
            {
                var candidates = destination_group.ToArray();
                var sources = candidates.Select(candidate => candidate.Item.Position).ToArray();
                var selected_source = merge_selector.SelectSource(destination_group.Key, sources);
                var selected = candidates.First(candidate => candidate.Item.Position == selected_source);
                accepted_moves.Add(selected.Item, selected.Destination);
            }

            return accepted_moves;
        }

        private void RemoveBlockedMoves(Dictionary<FactoryItemState, Vector2Int> accepted_moves)
        {
            var occupants = items.ToDictionary(item => item.Position);
            var changed = true;

            while (changed)
            {
                changed = false;

                foreach (var pair in accepted_moves.ToArray())
                {
                    if (!occupants.TryGetValue(pair.Value, out var occupant)
                        || occupant == pair.Key
                        || accepted_moves.ContainsKey(occupant))
                    {
                        continue;
                    }

                    accepted_moves.Remove(pair.Key);
                    changed = true;
                }
            }
        }

        private void ProduceItems()
        {
            foreach (var extractor in extraction_network.Extractors.Values)
            {
                production_steps.TryGetValue(extractor.Center, out var steps);
                steps++;
                production_steps[extractor.Center] = steps;

                if (steps < PRODUCTION_STEP_INTERVAL
                    || !conveyor_network.Conveyors.ContainsKey(extractor.OutputPosition)
                    || items.Any(item => item.Position == extractor.OutputPosition
                        || item.TargetPosition == extractor.OutputPosition))
                {
                    continue;
                }

                var item = new FactoryItemState(
                    next_item_id++,
                    extractor.Material,
                    extractor.OutputPosition)
                {
                    IsSpawning = true
                };
                items.Add(item);
                production_steps[extractor.Center] = 0;
            }
        }

        private readonly struct MoveProposal
        {
            public FactoryItemState Item { get; }
            public Vector2Int Destination { get; }

            public MoveProposal(FactoryItemState item, Vector2Int destination)
            {
                Item = item;
                Destination = destination;
            }
        }
    }
}
