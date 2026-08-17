using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Maptory.Factory
{
    public enum ItemScaleAnimation
    {
        None,
        Spawning,
        Despawning
    }

    public sealed class FactoryItemState
    {
        public int Id { get; }
        public RawMaterialType Material { get; }
        public Vector2Int Position { get; internal set; }
        public Vector2Int TargetPosition { get; internal set; }
        public ItemScaleAnimation ScaleAnimation { get; internal set; }
        public IItemConsumer DestinationConsumer { get; internal set; }
        public bool IsSpawning => ScaleAnimation == ItemScaleAnimation.Spawning;

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
        public const float SCALE_ANIMATION_DURATION = 0.12f;

        private const int PRODUCTION_STEP_INTERVAL = 3;

        private readonly ConveyorNetwork conveyor_network;
        private readonly ExtractionNetwork extraction_network;
        private readonly FairMergeSelector merge_selector = new();
        private readonly Dictionary<Vector2Int, int> production_steps = new();
        private readonly List<FactoryItemState> items = new();
        private readonly List<FactoryMonsterState> monsters = new();
        private float elapsed_step_time;
        private int next_item_id;
        private int next_monster_id;

        public IReadOnlyList<FactoryItemState> Items => items;
        public IReadOnlyList<FactoryMonsterState> Monsters => monsters;
        public float StepProgress => elapsed_step_time / STEP_DURATION;
        public float ScaleAnimationProgress => Mathf.Clamp01(elapsed_step_time / SCALE_ANIMATION_DURATION);

        public FactoryItemTransport(ConveyorNetwork conveyor_network, ExtractionNetwork extraction_network)
        {
            this.conveyor_network = conveyor_network;
            this.extraction_network = extraction_network;
        }

        public FactoryItemState SpawnItem(RawMaterialType material, Vector2Int conveyor_position)
        {
            if (!conveyor_network.Conveyors.ContainsKey(conveyor_position))
            {
                throw new System.InvalidOperationException("Items can only spawn on conveyors.");
            }

            var item = new FactoryItemState(next_item_id++, material, conveyor_position)
            {
                ScaleAnimation = ItemScaleAnimation.Spawning
            };
            items.Add(item);
            return item;
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
            foreach (var item in items.ToArray())
            {
                if (item.ScaleAnimation == ItemScaleAnimation.Despawning)
                {
                    item.DestinationConsumer.AddInput(item.Material);
                    items.Remove(item);
                    continue;
                }

                item.Position = item.TargetPosition;
                item.ScaleAnimation = ItemScaleAnimation.None;
            }
        }

        private void PlanConveyorMoves()
        {
            var proposals = new List<MoveProposal>();
            var routed_items = RouteMachineInputs();

            foreach (var item in items)
            {
                item.TargetPosition = item.Position;
                if (routed_items.Contains(item)) continue;

                if (conveyor_network.Conveyors.ContainsKey(item.Position)
                    && conveyor_network.TrySelectNextOutput(item.Position, out var output))
                {
                    proposals.Add(new MoveProposal(item, item.Position + output.ToOffset()));
                }
            }

            var accepted_moves = SelectDestinationWinners(proposals);
            RemoveBlockedMoves(accepted_moves);
            foreach (var pair in accepted_moves) pair.Key.TargetPosition = pair.Value;
        }

        private HashSet<FactoryItemState> RouteMachineInputs()
        {
            var routed_items = new HashSet<FactoryItemState>();
            RouteRecipeMachineInputs(extraction_network.DyeingMachines.Values, routed_items);
            RouteRecipeMachineInputs(extraction_network.Combiners.Values, routed_items);
            RouteErdaInjectorInputs(routed_items);
            return routed_items;
        }

        private void RouteRecipeMachineInputs(
            IEnumerable<IRecipeMachine> machines,
            ISet<FactoryItemState> routed_items)
        {
            foreach (var machine in machines)
            {
                if (machine.SelectedRecipe == null) continue;
                var reserved = new HashSet<RawMaterialType>();

                for (var input = 0; input < 2; input++)
                {
                    var conveyor_position = machine.GetInputConveyorPosition(input);
                    var item = items.FirstOrDefault(candidate =>
                        candidate.Position == conveyor_position
                        && !routed_items.Contains(candidate));
                    if (item == null || reserved.Contains(item.Material) || !machine.CanAccept(item.Material)) continue;

                    var required_direction = GridDirectionExtensions.FromDelta(machine.Forward);
                    if (!conveyor_network.Conveyors.TryGetValue(conveyor_position, out var conveyor)
                        || conveyor.Direction != required_direction) continue;

                    item.TargetPosition = machine.GetInputPort(input);
                    item.ScaleAnimation = ItemScaleAnimation.Despawning;
                    item.DestinationConsumer = machine;
                    routed_items.Add(item);
                    reserved.Add(item.Material);
                }
            }
        }

        private void RouteErdaInjectorInputs(ISet<FactoryItemState> routed_items)
        {
            foreach (var injector in extraction_network.ErdaInjectors.Values)
            {
                var conveyor_position = injector.InputConveyorPosition;
                var item = items.FirstOrDefault(candidate =>
                    candidate.Position == conveyor_position
                    && !routed_items.Contains(candidate));
                if (item == null || !injector.CanAccept(item.Material)) continue;

                var required_direction = GridDirectionExtensions.FromDelta(injector.Forward);
                if (!conveyor_network.Conveyors.TryGetValue(conveyor_position, out var conveyor)
                    || conveyor.Direction != required_direction) continue;

                item.TargetPosition = injector.Center;
                item.ScaleAnimation = ItemScaleAnimation.Despawning;
                item.DestinationConsumer = injector;
                routed_items.Add(item);
            }
        }

        private Dictionary<FactoryItemState, Vector2Int> SelectDestinationWinners(IReadOnlyList<MoveProposal> proposals)
        {
            var accepted_moves = new Dictionary<FactoryItemState, Vector2Int>();
            foreach (var group in proposals.GroupBy(proposal => proposal.Destination))
            {
                var candidates = group.ToArray();
                var source = merge_selector.SelectSource(group.Key, candidates.Select(candidate => candidate.Item.Position).ToArray());
                var selected = candidates.First(candidate => candidate.Item.Position == source);
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
                        || accepted_moves.ContainsKey(occupant)) continue;
                    accepted_moves.Remove(pair.Key);
                    changed = true;
                }
            }
        }

        private void ProduceItems()
        {
            ProduceExtractorItems();
            ProduceRecipeMachineItems(extraction_network.DyeingMachines.Values);
            ProduceRecipeMachineItems(extraction_network.Combiners.Values);
            ProduceMonsters();
        }

        private void ProduceRecipeMachineItems(IEnumerable<IRecipeMachine> machines)
        {
            foreach (var machine in machines)
            {
                if (!machine.CanCraft
                    || !conveyor_network.Conveyors.ContainsKey(machine.OutputConveyorPosition)
                    || IsOccupied(machine.OutputConveyorPosition)) continue;

                SpawnItem(machine.Craft(), machine.OutputConveyorPosition);
            }
        }

        private void ProduceMonsters()
        {
            foreach (var injector in extraction_network.ErdaInjectors.Values)
            {
                if (!injector.CanProduce) continue;
                monsters.Add(new FactoryMonsterState(
                    next_monster_id++,
                    injector.Produce(),
                    injector.OutputPosition));
            }
        }

        private void ProduceExtractorItems()
        {
            foreach (var extractor in extraction_network.Extractors.Values)
            {
                production_steps.TryGetValue(extractor.Center, out var steps);
                steps++;
                production_steps[extractor.Center] = steps;
                if (steps < PRODUCTION_STEP_INTERVAL
                    || !conveyor_network.Conveyors.ContainsKey(extractor.OutputPosition)
                    || IsOccupied(extractor.OutputPosition)) continue;

                SpawnItem(extractor.Material, extractor.OutputPosition);
                production_steps[extractor.Center] = 0;
            }
        }

        private bool IsOccupied(Vector2Int position)
        {
            return items.Any(item => item.Position == position || item.TargetPosition == position);
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
