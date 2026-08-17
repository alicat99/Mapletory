using System;
using System.Collections.Generic;
using UnityEngine;

namespace Maptory.Factory
{
    public enum RawMaterialType
    {
        DyeBlue, DyeRed, DyeYellow, DyeOrange, DyeGreen,
        Mushroom, Snail,
        SnailRed, SnailBlue,
        MushroomBlue, MushroomOrange, MushroomGreen,
        SpikeMushroom, SpikeMushroomBlue, SpikeMushroomOrange, SpikeMushroomGreen
    }

    public static class RawMaterialTypeExtensions
    {
        public static string ToResourceSpriteName(this RawMaterialType material)
        {
            return material == RawMaterialType.Snail ? "RawMaterialSnail" : $"RawMaterial{material}";
        }

        public static string ToItemSpriteName(this RawMaterialType material)
        {
            return material == RawMaterialType.Snail ? "SnailGreen" : material.ToString();
        }

        public static string ToKoreanName(this RawMaterialType material)
        {
            return material switch
            {
                RawMaterialType.DyeBlue => "파란 염료",
                RawMaterialType.DyeRed => "빨간 염료",
                RawMaterialType.DyeYellow => "노란 염료",
                RawMaterialType.DyeOrange => "주황 염료",
                RawMaterialType.DyeGreen => "초록 염료",
                RawMaterialType.Mushroom => "버섯 갓",
                RawMaterialType.Snail => "초록 달팽이 껍질",
                RawMaterialType.SnailRed => "빨간 달팽이 껍질",
                RawMaterialType.SnailBlue => "파란 달팽이 껍질",
                RawMaterialType.MushroomBlue => "파란 버섯 갓",
                RawMaterialType.MushroomOrange => "주황 버섯 갓",
                RawMaterialType.MushroomGreen => "초록 버섯 갓",
                RawMaterialType.SpikeMushroom => "뿔버섯 갓",
                RawMaterialType.SpikeMushroomBlue => "파란 뿔버섯 갓",
                RawMaterialType.SpikeMushroomOrange => "주황 뿔버섯 갓",
                RawMaterialType.SpikeMushroomGreen => "초록 뿔버섯 갓",
                _ => throw new ArgumentOutOfRangeException(nameof(material))
            };
        }
    }

    public enum DyeingRecipeId
    {
        SnailRed, SnailBlue,
        MushroomBlue, MushroomOrange, MushroomGreen,
        SpikeMushroomBlue, SpikeMushroomOrange, SpikeMushroomGreen
    }

    public sealed class DyeingRecipe
    {
        private static readonly Dictionary<DyeingRecipeId, DyeingRecipe> RECIPES = new()
        {
            { DyeingRecipeId.SnailRed, new(RawMaterialType.Snail, RawMaterialType.DyeRed, RawMaterialType.SnailRed) },
            { DyeingRecipeId.SnailBlue, new(RawMaterialType.Snail, RawMaterialType.DyeBlue, RawMaterialType.SnailBlue) },
            { DyeingRecipeId.MushroomBlue, new(RawMaterialType.Mushroom, RawMaterialType.DyeBlue, RawMaterialType.MushroomBlue) },
            { DyeingRecipeId.MushroomOrange, new(RawMaterialType.Mushroom, RawMaterialType.DyeOrange, RawMaterialType.MushroomOrange) },
            { DyeingRecipeId.MushroomGreen, new(RawMaterialType.Mushroom, RawMaterialType.DyeGreen, RawMaterialType.MushroomGreen) },
            { DyeingRecipeId.SpikeMushroomBlue, new(RawMaterialType.SpikeMushroom, RawMaterialType.DyeBlue, RawMaterialType.SpikeMushroomBlue) },
            { DyeingRecipeId.SpikeMushroomOrange, new(RawMaterialType.SpikeMushroom, RawMaterialType.DyeOrange, RawMaterialType.SpikeMushroomOrange) },
            { DyeingRecipeId.SpikeMushroomGreen, new(RawMaterialType.SpikeMushroom, RawMaterialType.DyeGreen, RawMaterialType.SpikeMushroomGreen) }
        };

        public static IReadOnlyDictionary<DyeingRecipeId, DyeingRecipe> All => RECIPES;
        public RawMaterialType BaseMaterial { get; }
        public RawMaterialType Dye { get; }
        public RawMaterialType Result { get; }
        public string DisplayName => Result.ToKoreanName();

        private DyeingRecipe(RawMaterialType base_material, RawMaterialType dye, RawMaterialType result)
        {
            BaseMaterial = base_material;
            Dye = dye;
            Result = result;
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

    public sealed class DyeingMachineState
    {
        private readonly HashSet<RawMaterialType> stored_materials = new();

        public Vector2Int Center { get; }
        public GridDirection Direction { get; }
        public DyeingRecipe SelectedRecipe { get; private set; }
        public Vector2Int Forward => Direction.ToOffset();
        public Vector2Int OutputPort => Center + Forward;
        public Vector2Int OutputConveyorPosition => Center + Forward * 2;
        public IReadOnlyCollection<RawMaterialType> StoredMaterials => stored_materials;
        public bool CanCraft => SelectedRecipe != null
            && stored_materials.Contains(SelectedRecipe.BaseMaterial)
            && stored_materials.Contains(SelectedRecipe.Dye);

        public DyeingMachineState(Vector2Int center, GridDirection direction)
        {
            Center = center;
            Direction = direction;
        }

        public Vector2Int GetInputPort(int index)
        {
            var offset = index == 0 ? new Vector2Int(-1, 1) : new Vector2Int(-1, -1);

            for (var rotation = 0; rotation < (int)Direction; rotation++)
            {
                offset = new Vector2Int(offset.y, -offset.x);
            }

            return Center + offset;
        }

        public Vector2Int GetInputConveyorPosition(int index)
        {
            return GetInputPort(index) - Forward;
        }

        public void SelectRecipe(DyeingRecipe recipe)
        {
            SelectedRecipe = recipe;
            stored_materials.Clear();
        }

        public bool CanAccept(RawMaterialType material)
        {
            return SelectedRecipe != null
                && (material == SelectedRecipe.BaseMaterial || material == SelectedRecipe.Dye)
                && !stored_materials.Contains(material);
        }

        public void AddInput(RawMaterialType material)
        {
            if (!CanAccept(material))
            {
                throw new InvalidOperationException("The dyeing machine cannot accept this item.");
            }

            stored_materials.Add(material);
        }

        public RawMaterialType Craft()
        {
            if (!CanCraft)
            {
                throw new InvalidOperationException("The dyeing recipe is incomplete.");
            }

            stored_materials.Clear();
            return SelectedRecipe.Result;
        }
    }

    public sealed class ExtractionNetwork
    {
        private readonly Dictionary<Vector2Int, RawMaterialDeposit> deposits = new();
        private readonly Dictionary<Vector2Int, ExtractorState> extractors = new();
        private readonly Dictionary<Vector2Int, DyeingMachineState> dyeing_machines = new();
        private readonly ConveyorNetwork conveyor_network;

        public IReadOnlyDictionary<Vector2Int, RawMaterialDeposit> Deposits => deposits;
        public IReadOnlyDictionary<Vector2Int, ExtractorState> Extractors => extractors;
        public IReadOnlyDictionary<Vector2Int, DyeingMachineState> DyeingMachines => dyeing_machines;
        public event Action<ExtractorState> ExtractorPlaced;
        public event Action<DyeingMachineState> DyeingMachinePlaced;

        public ExtractionNetwork(IEnumerable<RawMaterialDeposit> fixed_deposits, ConveyorNetwork conveyors)
        {
            conveyor_network = conveyors;
            foreach (var deposit in fixed_deposits)
            {
                deposits.Add(deposit.Center, deposit);
            }
        }

        public bool CanPlaceExtractor(Vector2Int center)
        {
            return deposits.ContainsKey(center) && IsFootprintClear(center, true);
        }

        public bool CanPlaceDyeingMachine(Vector2Int center)
        {
            return IsFootprintClear(center, false);
        }

        public bool IsBuildingOccupied(Vector2Int position)
        {
            foreach (var center in extractors.Keys)
            {
                if (IsInsideFootprint(position, center)) return true;
            }

            foreach (var center in dyeing_machines.Keys)
            {
                if (IsInsideFootprint(position, center)) return true;
            }

            return false;
        }

        public DyeingMachineState FindDyeingMachine(Vector2Int position)
        {
            foreach (var machine in dyeing_machines.Values)
            {
                if (IsInsideFootprint(position, machine.Center)) return machine;
            }

            return null;
        }

        public ExtractorState PlaceExtractor(Vector2Int center, GridDirection direction)
        {
            if (!CanPlaceExtractor(center)) throw new InvalidOperationException("Extractor footprint is occupied.");

            var extractor = new ExtractorState(deposits[center].Material, center, direction);
            extractors.Add(center, extractor);
            ExtractorPlaced?.Invoke(extractor);
            return extractor;
        }

        public DyeingMachineState PlaceDyeingMachine(Vector2Int center, GridDirection direction)
        {
            if (!CanPlaceDyeingMachine(center)) throw new InvalidOperationException("Dyeing machine footprint is occupied.");

            var machine = new DyeingMachineState(center, direction);
            dyeing_machines.Add(center, machine);
            DyeingMachinePlaced?.Invoke(machine);
            return machine;
        }

        private bool IsFootprintClear(Vector2Int center, bool allow_centered_deposit)
        {
            for (var y = -1; y <= 1; y++)
            {
                for (var x = -1; x <= 1; x++)
                {
                    var position = center + new Vector2Int(x, y);
                    if (IsBuildingOccupied(position) || conveyor_network.Conveyors.ContainsKey(position)) return false;
                }
            }

            foreach (var deposit in deposits.Values)
            {
                if (FootprintsOverlap(center, deposit.Center)
                    && (!allow_centered_deposit || deposit.Center != center)) return false;
            }

            return true;
        }

        private static bool IsInsideFootprint(Vector2Int position, Vector2Int center)
        {
            var offset = position - center;
            return Mathf.Abs(offset.x) <= 1 && Mathf.Abs(offset.y) <= 1;
        }

        private static bool FootprintsOverlap(Vector2Int first_center, Vector2Int second_center)
        {
            var offset = first_center - second_center;
            return Mathf.Abs(offset.x) <= 2 && Mathf.Abs(offset.y) <= 2;
        }
    }
}
