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
        public GridDirection EntryDirection { get; internal set; }
        public bool IsSpawning => ScaleAnimation == ItemScaleAnimation.Spawning;

        public FactoryItemState(
            int id,
            RawMaterialType material,
            Vector2Int position,
            GridDirection entry_direction)
        {
            Id = id;
            Material = material;
            Position = position;
            TargetPosition = position;
            EntryDirection = entry_direction;
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
        public const float EXTRACTOR_PRODUCTION_INTERVAL = 1f;

        private readonly ConveyorNetwork conveyor_network;
        private readonly ExtractionNetwork extraction_network;
        private readonly FairMergeSelector merge_selector = new();
        private readonly Dictionary<Vector2Int, float> production_elapsed = new();
        private readonly List<FactoryItemState> items = new();
        private float elapsed_step_time;
        private int next_item_id;

        public IReadOnlyList<FactoryItemState> Items => items;
        public float StepProgress => elapsed_step_time / STEP_DURATION;
        public float ScaleAnimationProgress => Mathf.Clamp01(elapsed_step_time / SCALE_ANIMATION_DURATION);

        public FactoryItemTransport(ConveyorNetwork conveyor_network, ExtractionNetwork extraction_network)
        {
            this.conveyor_network = conveyor_network;
            this.extraction_network = extraction_network;
            extraction_network.BuildingRemoved += OnBuildingRemoved;
        }

        public void RemoveItemsAt(Vector2Int position)
        {
            items.RemoveAll(item => item.Position == position || item.TargetPosition == position);
        }

        public void ClearItems()
        {
            items.Clear();
        }

        public FactoryItemState SpawnItem(
            RawMaterialType material,
            Vector2Int conveyor_position,
            GridDirection? entry_direction = null)
        {
            if (!conveyor_network.Conveyors.ContainsKey(conveyor_position))
            {
                throw new System.InvalidOperationException("Items can only spawn on conveyors.");
            }

            var item = new FactoryItemState(
                next_item_id++,
                material,
                conveyor_position,
                entry_direction ?? conveyor_network.Conveyors[conveyor_position].Direction)
            {
                ScaleAnimation = ItemScaleAnimation.Spawning
            };
            items.Add(item);
            return item;
        }

        public FactoryItemState RestoreItem(
            RawMaterialType material,
            Vector2Int position,
            Vector2Int target_position,
            ItemScaleAnimation scale_animation,
            GridDirection entry_direction)
        {
            var item = new FactoryItemState(
                next_item_id++,
                material,
                position,
                entry_direction)
            {
                TargetPosition = target_position,
                ScaleAnimation = scale_animation
            };
            if (scale_animation == ItemScaleAnimation.Despawning)
            {
                item.DestinationConsumer = FindRestoredConsumer(
                    material,
                    target_position);
            }
            items.Add(item);
            return item;
        }

        public void Update(float delta_time)
        {
            elapsed_step_time += delta_time;
            while (elapsed_step_time >= STEP_DURATION)
            {
                elapsed_step_time -= STEP_DURATION;
                StepTransport();
            }

            AdvanceExtractorProduction(delta_time);
        }

        public void Step()
        {
            StepTransport();
            AdvanceExtractorProduction(STEP_DURATION);
        }

        private void StepTransport()
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

                if (item.Position != item.TargetPosition)
                {
                    item.EntryDirection = GridDirectionExtensions.FromDelta(
                        item.TargetPosition - item.Position);
                }
                item.Position = item.TargetPosition;
                item.ScaleAnimation = ItemScaleAnimation.None;
            }
        }

        private void PlanConveyorMoves()
        {
            var proposals = new List<MoveProposal>();
            foreach (var item in items)
            {
                item.TargetPosition = item.Position;
            }

            var routed_items = RouteMachineInputs();

            foreach (var item in items)
            {
                if (routed_items.Contains(item)) continue;

                if (conveyor_network.Conveyors.ContainsKey(item.Position)
                    && conveyor_network.TrySelectNextOutput(
                        item.Position,
                        item.EntryDirection,
                        out var output))
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
            RouteRecipeMachineInputs(extraction_network.ProcessingMachines.Values, routed_items);
            RouteErdaInjectorInputs(routed_items);
            RoutePortalInputs(routed_items);
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

                for (var input = 0; input < machine.InputCount; input++)
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

        private void RoutePortalInputs(ISet<FactoryItemState> routed_items)
        {
            foreach (var portal in extraction_network.Portals.Values)
            {
                if (!portal.SelectedMaterial.HasValue) continue;

                foreach (var port in portal.InputPorts)
                {
                    var item = items.FirstOrDefault(candidate =>
                        candidate.Position == port.ConveyorPosition
                        && !routed_items.Contains(candidate));
                    if (item == null || !portal.CanAccept(item.Material)) continue;

                    if (!conveyor_network.Conveyors.TryGetValue(port.ConveyorPosition, out var conveyor)
                        || conveyor.Direction != port.Direction) continue;

                    item.TargetPosition = port.PortalPosition;
                    item.ScaleAnimation = ItemScaleAnimation.Despawning;
                    item.DestinationConsumer = portal;
                    routed_items.Add(item);
                }
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
            ProduceRecipeMachineItems(extraction_network.DyeingMachines.Values);
            ProduceRecipeMachineItems(extraction_network.Combiners.Values);
            ProduceRecipeMachineItems(extraction_network.ProcessingMachines.Values);
            ProduceErdaInjectorItems();
        }

        private void ProduceRecipeMachineItems(IEnumerable<IRecipeMachine> machines)
        {
            foreach (var machine in machines)
            {
                if (!machine.CanCraft) continue;

                var output_material = machine.SelectedRecipe.Result;
                if (TryTransferDirectly(
                    output_material,
                    machine.Center + machine.Forward,
                    machine.OutputConveyorPosition,
                    machine.Forward))
                {
                    machine.Craft();
                    continue;
                }

                if (!conveyor_network.Conveyors.ContainsKey(machine.OutputConveyorPosition)
                    || IsOccupied(machine.OutputConveyorPosition)) continue;

                SpawnItem(
                    machine.Craft(),
                    machine.OutputConveyorPosition,
                    GridDirectionExtensions.FromDelta(machine.Forward));
            }
        }

        private void ProduceErdaInjectorItems()
        {
            foreach (var injector in extraction_network.ErdaInjectors.Values)
            {
                if (!injector.CanProduce) continue;

                if (TryTransferDirectly(
                    injector.OutputMaterial,
                    injector.Center,
                    injector.OutputConveyorPosition,
                    injector.Forward))
                {
                    injector.Produce();
                    continue;
                }

                if (!conveyor_network.Conveyors.ContainsKey(injector.OutputConveyorPosition)
                    || IsOccupied(injector.OutputConveyorPosition)) continue;

                SpawnItem(
                    injector.Produce(),
                    injector.OutputConveyorPosition,
                    GridDirectionExtensions.FromDelta(injector.Forward));
            }
        }

        private void AdvanceExtractorProduction(float delta_time)
        {
            foreach (var extractor in extraction_network.Extractors.Values)
            {
                production_elapsed.TryGetValue(extractor.Center, out var elapsed);
                elapsed += delta_time;
                if (elapsed < EXTRACTOR_PRODUCTION_INTERVAL)
                {
                    production_elapsed[extractor.Center] = elapsed;
                    continue;
                }

                var forward = extractor.Direction.ToOffset();
                if (TryTransferDirectly(
                    extractor.Material,
                    extractor.Center + forward,
                    extractor.OutputPosition,
                    forward))
                {
                    production_elapsed[extractor.Center] = elapsed
                        - EXTRACTOR_PRODUCTION_INTERVAL;
                    continue;
                }

                if (!conveyor_network.Conveyors.ContainsKey(extractor.OutputPosition)
                    || IsOccupied(extractor.OutputPosition))
                {
                    production_elapsed[extractor.Center] = EXTRACTOR_PRODUCTION_INTERVAL;
                    continue;
                }

                SpawnItem(
                    extractor.Material,
                    extractor.OutputPosition,
                    extractor.Direction);
                production_elapsed[extractor.Center] = elapsed
                    - EXTRACTOR_PRODUCTION_INTERVAL;
            }
        }

        private bool TryTransferDirectly(
            RawMaterialType material,
            Vector2Int source_port,
            Vector2Int destination_port,
            Vector2Int forward)
        {
            if (IsOccupied(source_port) || IsOccupied(destination_port)) return false;

            var consumer = FindDirectConsumer(material, source_port, destination_port, forward);
            if (consumer == null) return false;

            var item = new FactoryItemState(
                next_item_id++,
                material,
                source_port,
                GridDirectionExtensions.FromDelta(forward))
            {
                TargetPosition = destination_port,
                ScaleAnimation = ItemScaleAnimation.Despawning,
                DestinationConsumer = consumer
            };
            items.Add(item);
            return true;
        }

        private IItemConsumer FindDirectConsumer(
            RawMaterialType material,
            Vector2Int source_port,
            Vector2Int destination_port,
            Vector2Int forward)
        {
            foreach (var machine in GetRecipeMachines())
            {
                if (machine.Forward != forward || !machine.CanAccept(material)) continue;

                for (var input = 0; input < machine.InputCount; input++)
                {
                    if (machine.GetInputPort(input) == destination_port) return machine;
                }
            }

            foreach (var injector in extraction_network.ErdaInjectors.Values)
            {
                if (injector.Forward == forward
                    && injector.Center == destination_port
                    && injector.CanAccept(material)) return injector;
            }

            foreach (var portal in extraction_network.Portals.Values)
            {
                if (!portal.CanAccept(material)) continue;

                foreach (var port in portal.InputPorts)
                {
                    if (port.ConveyorPosition == source_port
                        && port.PortalPosition == destination_port
                        && port.Direction.ToOffset() == forward) return portal;
                }
            }

            return null;
        }

        private IItemConsumer FindRestoredConsumer(
            RawMaterialType material,
            Vector2Int target_position)
        {
            foreach (var machine in GetRecipeMachines())
            {
                if (!machine.CanAccept(material)) continue;
                for (var input = 0; input < machine.InputCount; input++)
                {
                    if (machine.GetInputPort(input) == target_position) return machine;
                }
            }

            foreach (var injector in extraction_network.ErdaInjectors.Values)
            {
                if (injector.Center == target_position
                    && injector.CanAccept(material)) return injector;
            }

            foreach (var portal in extraction_network.Portals.Values)
            {
                if (portal.Contains(target_position)
                    && portal.CanAccept(material)) return portal;
            }

            throw new System.InvalidOperationException(
                "The saved moving item has no destination consumer.");
        }

        private IEnumerable<IRecipeMachine> GetRecipeMachines()
        {
            return extraction_network.DyeingMachines.Values
                .Cast<IRecipeMachine>()
                .Concat(extraction_network.Combiners.Values)
                .Concat(extraction_network.ProcessingMachines.Values);
        }

        private bool IsOccupied(Vector2Int position)
        {
            return items.Any(item => item.Position == position || item.TargetPosition == position);
        }

        private void OnBuildingRemoved(object building)
        {
            if (building is ExtractorState extractor)
            {
                production_elapsed.Remove(extractor.Center);
            }

            if (building is not IItemConsumer consumer) return;
            items.RemoveAll(item => ReferenceEquals(item.DestinationConsumer, consumer));
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
