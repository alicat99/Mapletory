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

        public IReadOnlyDictionary<Vector2Int, RawMaterialDeposit> Deposits => deposits;
        public IReadOnlyDictionary<Vector2Int, ExtractorState> Extractors => extractors;
        public event Action<ExtractorState> ExtractorPlaced;

        public ExtractionNetwork(IEnumerable<RawMaterialDeposit> fixed_deposits)
        {
            foreach (var deposit in fixed_deposits)
            {
                deposits.Add(deposit.Center, deposit);
            }
        }

        public bool CanPlaceExtractor(Vector2Int center)
        {
            return deposits.ContainsKey(center) && !extractors.ContainsKey(center);
        }

        public ExtractorState PlaceExtractor(Vector2Int center, GridDirection direction)
        {
            var deposit = deposits[center];
            var extractor = new ExtractorState(deposit.Material, center, direction);
            extractors.Add(center, extractor);
            ExtractorPlaced?.Invoke(extractor);
            return extractor;
        }
    }
}
