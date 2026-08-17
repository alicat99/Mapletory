using System;
using System.Collections.Generic;
using UnityEngine;

namespace Maptory.Factory
{
    public enum RawMaterialType
    {
        DyeBlue,
        DyeRed,
        DyeYellow,
        Mushroom,
        Snail
    }

    public static class RawMaterialTypeExtensions
    {
        public static string ToResourceSpriteName(this RawMaterialType material)
        {
            return $"RawMaterial{material}";
        }

        public static string ToItemSpriteName(this RawMaterialType material)
        {
            return material == RawMaterialType.Snail ? "SnailGreen" : material.ToString();
        }
    }

    public sealed class RawMaterialDeposit
    {
        public RawMaterialType Material { get; }
        public Vector2Int Center { get; }

        public RawMaterialDeposit(RawMaterialType material, Vector2Int center)
        {
            Material = material;
            Center = center;
        }
    }

    public sealed class ExtractorState
    {
        public RawMaterialType Material { get; }
        public Vector2Int Center { get; }
        public GridDirection Direction { get; }

        public Vector2Int OutputPosition => Center + Direction.ToOffset() * 2;

        public ExtractorState(RawMaterialType material, Vector2Int center, GridDirection direction)
        {
            Material = material;
            Center = center;
            Direction = direction;
        }
    }

    public sealed class ExtractionNetwork
    {
        private readonly Dictionary<Vector2Int, RawMaterialDeposit> deposits = new();
        private readonly Dictionary<Vector2Int, ExtractorState> extractors = new();
        private readonly ConveyorNetwork conveyor_network;

        public IReadOnlyDictionary<Vector2Int, RawMaterialDeposit> Deposits => deposits;
        public IReadOnlyDictionary<Vector2Int, ExtractorState> Extractors => extractors;
        public event Action<ExtractorState> ExtractorPlaced;

        public ExtractionNetwork(
            IEnumerable<RawMaterialDeposit> fixed_deposits,
            ConveyorNetwork conveyors)
        {
            conveyor_network = conveyors;

            foreach (var deposit in fixed_deposits)
            {
                deposits.Add(deposit.Center, deposit);
            }
        }

        public bool CanPlaceExtractor(Vector2Int center)
        {
            return deposits.ContainsKey(center) && IsFootprintClear(center);
        }

        public bool IsBuildingOccupied(Vector2Int position)
        {
            foreach (var extractor in extractors.Values)
            {
                var offset = position - extractor.Center;
                if (Mathf.Abs(offset.x) <= 1 && Mathf.Abs(offset.y) <= 1)
                {
                    return true;
                }
            }

            return false;
        }

        public ExtractorState PlaceExtractor(Vector2Int center, GridDirection direction)
        {
            if (!CanPlaceExtractor(center))
            {
                throw new InvalidOperationException("Extractor footprint is occupied.");
            }

            var deposit = deposits[center];
            var extractor = new ExtractorState(deposit.Material, center, direction);
            extractors.Add(center, extractor);
            ExtractorPlaced?.Invoke(extractor);
            return extractor;
        }

        private bool IsFootprintClear(Vector2Int center)
        {
            for (var y = -1; y <= 1; y++)
            {
                for (var x = -1; x <= 1; x++)
                {
                    var position = center + new Vector2Int(x, y);
                    if (IsBuildingOccupied(position)
                        || conveyor_network.Conveyors.ContainsKey(position))
                    {
                        return false;
                    }
                }
            }

            return true;
        }
    }
}
