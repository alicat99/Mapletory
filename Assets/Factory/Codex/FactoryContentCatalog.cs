using System;
using System.Collections.Generic;
using System.Linq;

namespace Maptory.Factory
{
    public enum FactoryCodexCategory
    {
        Monster,
        Dyeing,
        Processing,
        Combining,
        RawMaterial
    }

    public sealed class FactoryCodexEntry
    {
        public RawMaterialType Material { get; }
        public FactoryCodexCategory Category { get; }
        public string ProducerName { get; }
        public IReadOnlyList<RawMaterialType> Ingredients { get; }

        public FactoryCodexEntry(
            RawMaterialType material,
            FactoryCodexCategory category,
            string producer_name,
            IReadOnlyList<RawMaterialType> ingredients)
        {
            Material = material;
            Category = category;
            ProducerName = producer_name;
            Ingredients = ingredients;
        }
    }

    public sealed class FactoryBuildingInfo
    {
        public FactoryBuildTool Tool { get; }
        public string DisplayName { get; }
        public string Description { get; }

        public FactoryBuildingInfo(FactoryBuildTool tool, string display_name, string description)
        {
            Tool = tool;
            DisplayName = display_name;
            Description = description;
        }
    }

    public static class FactoryContentCatalog
    {
        private static readonly FactoryBuildingInfo[] BUILDINGS =
        {
            new(FactoryBuildTool.Conveyor, "컨베이어", "아이템 운송"),
            new(FactoryBuildTool.Extractor, "추출기", "원재료 추출"),
            new(FactoryBuildTool.ErdaInjector, "에르다 주입기", "아이템을 몬스터로 변환"),
            new(FactoryBuildTool.DyeingMachine, "염색기", "아이템 염색"),
            new(FactoryBuildTool.Combiner, "조합기", "아이템 합성"),
            new(FactoryBuildTool.ProcessingMachine, "가공시설", "다른 아이템으로 가공"),
            new(FactoryBuildTool.Portal, "포탈", "완성 몬스터를 사냥터에 공급")
        };

        private static readonly FactoryCodexEntry[] ENTRIES = CreateEntries();

        public static IReadOnlyList<FactoryBuildingInfo> Buildings => BUILDINGS;
        public static IReadOnlyList<FactoryCodexEntry> Entries => ENTRIES;

        public static FactoryCodexEntry GetEntry(RawMaterialType material)
        {
            return ENTRIES.First(entry => entry.Material == material);
        }

        public static string GetCategoryName(FactoryCodexCategory category)
        {
            return category switch
            {
                FactoryCodexCategory.Monster => "몬스터",
                FactoryCodexCategory.Dyeing => "염색기",
                FactoryCodexCategory.Processing => "가공시설",
                FactoryCodexCategory.Combining => "조합기",
                FactoryCodexCategory.RawMaterial => "원재료",
                _ => throw new ArgumentOutOfRangeException(nameof(category))
            };
        }

        private static FactoryCodexEntry[] CreateEntries()
        {
            var entries = new List<FactoryCodexEntry>();
            foreach (var material in new[]
                     {
                         RawMaterialType.DyeBlue,
                         RawMaterialType.DyeRed,
                         RawMaterialType.DyeYellow,
                         RawMaterialType.Mushroom,
                         RawMaterialType.Snail
                     })
            {
                entries.Add(new FactoryCodexEntry(
                    material,
                    FactoryCodexCategory.RawMaterial,
                    "추출기",
                    Array.Empty<RawMaterialType>()));
            }

            AddRecipes(entries, DyeingRecipe.All.Values, FactoryCodexCategory.Dyeing, "염색기");
            AddRecipes(entries, ProcessingRecipe.All.Values, FactoryCodexCategory.Processing, "가공시설");
            AddRecipes(entries, CombiningRecipe.All.Values, FactoryCodexCategory.Combining, "조합기");
            foreach (var recipe in ErdaInjectionRecipes.All)
            {
                entries.Add(new FactoryCodexEntry(
                    recipe.Value,
                    FactoryCodexCategory.Monster,
                    "에르다 주입기",
                    new[] { recipe.Key }));
            }

            return entries.ToArray();
        }

        private static void AddRecipes(
            ICollection<FactoryCodexEntry> entries,
            IEnumerable<IRecipe> recipes,
            FactoryCodexCategory category,
            string producer)
        {
            foreach (var recipe in recipes)
            {
                entries.Add(new FactoryCodexEntry(
                    recipe.Result,
                    category,
                    producer,
                    recipe.Ingredients));
            }
        }
    }
}
