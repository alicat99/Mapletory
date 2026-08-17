using System.Collections.Generic;

namespace Maptory.Factory
{
    public static class ErdaInjectionRecipes
    {
        private static readonly Dictionary<RawMaterialType, RawMaterialType> RECIPES = new()
        {
            { RawMaterialType.MushroomBlue, RawMaterialType.MonsterMushroomBlue },
            { RawMaterialType.MushroomGreen, RawMaterialType.MonsterMushroomGreen },
            { RawMaterialType.MushroomOrange, RawMaterialType.MonsterMushroomOrange },
            { RawMaterialType.SnailBlue, RawMaterialType.MonsterSnailBlue },
            { RawMaterialType.Snail, RawMaterialType.MonsterSnailGreen },
            { RawMaterialType.SnailRed, RawMaterialType.MonsterSnailRed },
            { RawMaterialType.SpikeMushroomGray, RawMaterialType.MonsterSpikeMushroomGray }
        };

        public static IReadOnlyDictionary<RawMaterialType, RawMaterialType> All => RECIPES;

        public static bool Contains(RawMaterialType material)
        {
            return RECIPES.ContainsKey(material);
        }

        public static RawMaterialType GetResult(RawMaterialType material)
        {
            return RECIPES[material];
        }
    }
}
