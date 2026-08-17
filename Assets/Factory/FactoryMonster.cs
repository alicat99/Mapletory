using System.Collections.Generic;
using UnityEngine;

namespace Maptory.Factory
{
    public enum FactoryMonsterType
    {
        MushroomBlue,
        MushroomGreen,
        MushroomOrange,
        SnailBlue,
        SnailGreen,
        SnailRed,
        SpikeMushroomGray
    }

    public static class FactoryMonsterRecipes
    {
        private static readonly Dictionary<RawMaterialType, FactoryMonsterType> RECIPES = new()
        {
            { RawMaterialType.MushroomBlue, FactoryMonsterType.MushroomBlue },
            { RawMaterialType.MushroomGreen, FactoryMonsterType.MushroomGreen },
            { RawMaterialType.MushroomOrange, FactoryMonsterType.MushroomOrange },
            { RawMaterialType.SnailBlue, FactoryMonsterType.SnailBlue },
            { RawMaterialType.Snail, FactoryMonsterType.SnailGreen },
            { RawMaterialType.SnailRed, FactoryMonsterType.SnailRed },
            { RawMaterialType.SpikeMushroomGray, FactoryMonsterType.SpikeMushroomGray }
        };

        public static IReadOnlyDictionary<RawMaterialType, FactoryMonsterType> All => RECIPES;

        public static bool Contains(RawMaterialType material)
        {
            return RECIPES.ContainsKey(material);
        }

        public static FactoryMonsterType GetMonster(RawMaterialType material)
        {
            return RECIPES[material];
        }

        public static string ToSpriteName(this FactoryMonsterType monster)
        {
            return $"Monster{monster}";
        }
    }

    public sealed class FactoryMonsterState
    {
        public int Id { get; }
        public FactoryMonsterType Type { get; }
        public Vector2Int Position { get; }

        public FactoryMonsterState(int id, FactoryMonsterType type, Vector2Int position)
        {
            Id = id;
            Type = type;
            Position = position;
        }
    }
}
