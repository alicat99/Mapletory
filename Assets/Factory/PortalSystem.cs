using System;
using System.Collections.Generic;
using UnityEngine;

namespace Maptory.Factory
{
    public sealed class PortalSupplyOption
    {
        public RawMaterialType Material { get; }
        public string SourceName { get; }
        public string ItemLabel { get; }
        public string SelectionLabel => $"[{SourceName} level1] {ItemLabel}";

        public PortalSupplyOption(
            RawMaterialType material,
            string source_name,
            string item_label)
        {
            Material = material;
            SourceName = source_name;
            ItemLabel = item_label;
        }
    }

    public static class PortalSupplyCatalog
    {
        private static readonly PortalSupplyOption[] OPTIONS =
        {
            new(RawMaterialType.MonsterSnailRed, "오솔길1", "빨간 달팽이"),
            new(RawMaterialType.MonsterSnailGreen, "리스항구 외곽", "달팽이"),
            new(RawMaterialType.MonsterMushroomGreen, "포자언덕", "초록 버섯"),
            new(RawMaterialType.MonsterSnailBlue, "오솔길2", "파란 달팽이"),
            new(RawMaterialType.MonsterMushroomOrange, "헤네시스 북쪽언덕", "주황 버섯"),
            new(RawMaterialType.MonsterSpikeMushroomBlue, "뿔버섯 숲", "파란 뿔버섯"),
            new(RawMaterialType.MonsterMushroomBlue, "꿈꾸는 오솔길", "파란 버섯")
        };

        public static IReadOnlyList<PortalSupplyOption> Options => OPTIONS;
    }

    public sealed class PortalEconomy
    {
        public const int MAX_UPGRADE_LEVEL = 20;

        private const int MESO_SCALE = 100;
        private const float DEFAULT_BASE_VALUE = 1.5f;
        private const float DEFAULT_MESO_BONUS = 0.5f;
        private const float DEFAULT_PRODUCTION_MULTIPLIER = 1.25f;

        private readonly Dictionary<RawMaterialType, MonsterProgress> progress = new();
        private readonly Dictionary<RawMaterialType, MonsterBalance> balances = new();
        private long meso_units;

        public long TotalMeso => meso_units / MESO_SCALE;
        public long MesoUpgradeBaseCost { get; private set; } = 20L;
        public long ProductionUpgradeBaseCost { get; private set; } = 20L;
        public int MaximumUpgradeLevel { get; private set; } = MAX_UPGRADE_LEVEL;

        public void RecordSupply(RawMaterialType material)
        {
            var monster = GetProgress(material);
            monster.LifetimeProduction++;
            monster.AvailableProduction++;
            meso_units += GetUnitValueUnits(monster, GetBalance(material));
        }

        public long GetTotalItems(RawMaterialType material)
        {
            return GetProgress(material).LifetimeProduction;
        }

        public long GetAvailableProduction(RawMaterialType material)
        {
            return GetProgress(material).AvailableProduction;
        }

        public int GetMesoUpgradeLevel(RawMaterialType material)
        {
            return GetProgress(material).MesoUpgradeLevel;
        }

        public int GetProductionUpgradeLevel(RawMaterialType material)
        {
            return GetProgress(material).ProductionUpgradeLevel;
        }

        public float GetMesoBonus(RawMaterialType material)
        {
            return GetProgress(material).MesoUpgradeLevel
                * GetBalance(material).MesoBonusPerLevel;
        }

        public float GetProductionMultiplier(RawMaterialType material)
        {
            return Mathf.Pow(
                GetBalance(material).ProductionMultiplierPerLevel,
                GetProgress(material).ProductionUpgradeLevel);
        }

        public float GetUnitValue(RawMaterialType material)
        {
            return GetUnitValueUnits(GetProgress(material), GetBalance(material))
                / (float)MESO_SCALE;
        }

        public float GetBaseValue(RawMaterialType material)
        {
            return GetBalance(material).BaseValue;
        }

        public float GetMesoBonusPerLevel(RawMaterialType material)
        {
            return GetBalance(material).MesoBonusPerLevel;
        }

        public float GetProductionMultiplierPerLevel(RawMaterialType material)
        {
            return GetBalance(material).ProductionMultiplierPerLevel;
        }

        public void SetBaseValue(RawMaterialType material, float value)
        {
            GetBalance(material).BaseValue = Mathf.Max(0f, value);
        }

        public void SetMesoBonusPerLevel(RawMaterialType material, float value)
        {
            GetBalance(material).MesoBonusPerLevel = Mathf.Max(0f, value);
        }

        public void SetProductionMultiplierPerLevel(RawMaterialType material, float value)
        {
            GetBalance(material).ProductionMultiplierPerLevel = Mathf.Max(0.01f, value);
        }

        public void SetMesoUpgradeLevel(RawMaterialType material, int level)
        {
            GetProgress(material).MesoUpgradeLevel = Mathf.Clamp(level, 0, MaximumUpgradeLevel);
        }

        public void SetProductionUpgradeLevel(RawMaterialType material, int level)
        {
            GetProgress(material).ProductionUpgradeLevel = Mathf.Clamp(level, 0, MaximumUpgradeLevel);
        }

        public void SetAvailableProduction(RawMaterialType material, long amount)
        {
            GetProgress(material).AvailableProduction = System.Math.Max(0L, amount);
        }

        public void SetUpgradeCosts(long meso_base_cost, long production_base_cost)
        {
            MesoUpgradeBaseCost = System.Math.Max(1L, meso_base_cost);
            ProductionUpgradeBaseCost = System.Math.Max(1L, production_base_cost);
        }

        public void SetMaximumUpgradeLevel(int level)
        {
            MaximumUpgradeLevel = Mathf.Clamp(level, 1, MAX_UPGRADE_LEVEL);
            foreach (var monster in progress.Values)
            {
                monster.MesoUpgradeLevel = Mathf.Min(monster.MesoUpgradeLevel, MaximumUpgradeLevel);
                monster.ProductionUpgradeLevel = Mathf.Min(
                    monster.ProductionUpgradeLevel,
                    MaximumUpgradeLevel);
            }
        }

        public long GetMesoUpgradeCost(RawMaterialType material)
        {
            var level = GetProgress(material).MesoUpgradeLevel;
            return level >= MaximumUpgradeLevel
                ? 0L
                : MesoUpgradeBaseCost * (level + 1L);
        }

        public long GetProductionUpgradeCost(RawMaterialType material)
        {
            var level = GetProgress(material).ProductionUpgradeLevel;
            return level >= MaximumUpgradeLevel
                ? 0L
                : ProductionUpgradeBaseCost << level;
        }

        public bool CanPurchaseMesoUpgrade(RawMaterialType material)
        {
            var cost = GetMesoUpgradeCost(material);
            return cost > 0L && meso_units >= cost * MESO_SCALE;
        }

        public bool CanPurchaseProductionUpgrade(RawMaterialType material)
        {
            var cost = GetProductionUpgradeCost(material);
            return cost > 0L && GetProgress(material).AvailableProduction >= cost;
        }

        public bool TryPurchaseMesoUpgrade(RawMaterialType material)
        {
            if (!CanPurchaseMesoUpgrade(material)) return false;

            var monster = GetProgress(material);
            meso_units -= GetMesoUpgradeCost(material) * MESO_SCALE;
            monster.MesoUpgradeLevel++;
            return true;
        }

        public bool TryPurchaseProductionUpgrade(RawMaterialType material)
        {
            if (!CanPurchaseProductionUpgrade(material)) return false;

            var monster = GetProgress(material);
            monster.AvailableProduction -= GetProductionUpgradeCost(material);
            monster.ProductionUpgradeLevel++;
            return true;
        }

        public int CountAvailableProductionUpgrades()
        {
            var count = 0;
            foreach (var option in PortalSupplyCatalog.Options)
            {
                if (CanPurchaseProductionUpgrade(option.Material)) count++;
            }

            return count;
        }

        private MonsterProgress GetProgress(RawMaterialType material)
        {
            if (!progress.TryGetValue(material, out var monster))
            {
                monster = new MonsterProgress();
                progress.Add(material, monster);
            }

            return monster;
        }

        private MonsterBalance GetBalance(RawMaterialType material)
        {
            if (!balances.TryGetValue(material, out var balance))
            {
                balance = new MonsterBalance();
                balances.Add(material, balance);
            }

            return balance;
        }

        private static long GetUnitValueUnits(MonsterProgress monster, MonsterBalance balance)
        {
            var additive_value = balance.BaseValue
                + monster.MesoUpgradeLevel * balance.MesoBonusPerLevel;
            var multiplier = Mathf.Pow(
                balance.ProductionMultiplierPerLevel,
                monster.ProductionUpgradeLevel);
            return (long)Math.Round(additive_value * multiplier * MESO_SCALE);
        }

        private sealed class MonsterProgress
        {
            public long LifetimeProduction;
            public long AvailableProduction;
            public int MesoUpgradeLevel;
            public int ProductionUpgradeLevel;
        }

        private sealed class MonsterBalance
        {
            public float BaseValue = DEFAULT_BASE_VALUE;
            public float MesoBonusPerLevel = DEFAULT_MESO_BONUS;
            public float ProductionMultiplierPerLevel = DEFAULT_PRODUCTION_MULTIPLIER;
        }
    }

    public readonly struct PortalInputPort
    {
        public Vector2Int ConveyorPosition { get; }
        public Vector2Int PortalPosition { get; }
        public GridDirection Direction { get; }

        public PortalInputPort(
            Vector2Int conveyor_position,
            Vector2Int portal_position,
            GridDirection direction)
        {
            ConveyorPosition = conveyor_position;
            PortalPosition = portal_position;
            Direction = direction;
        }
    }

    public sealed class PortalState : IItemConsumer
    {
        private readonly PortalEconomy economy;
        private readonly PortalInputPort[] input_ports;

        public Vector2Int Anchor { get; }
        public Vector2 VisualCenter => Anchor + new Vector2(0.5f, 0.5f);
        public RawMaterialType? SelectedMaterial { get; private set; }
        public IReadOnlyList<PortalInputPort> InputPorts => input_ports;

        public PortalState(Vector2Int anchor, PortalEconomy portal_economy)
        {
            Anchor = anchor;
            economy = portal_economy;
            input_ports = CreateInputPorts(anchor);
        }

        public void SelectMaterial(RawMaterialType material)
        {
            SelectedMaterial = material;
        }

        public bool CanAccept(RawMaterialType material)
        {
            return SelectedMaterial == material;
        }

        public void AddInput(RawMaterialType material)
        {
            if (!CanAccept(material))
            {
                throw new InvalidOperationException("The portal cannot accept this item.");
            }

            economy.RecordSupply(material);
        }

        public bool Contains(Vector2Int position)
        {
            var offset = position - Anchor;
            return offset.x >= 0 && offset.x <= 1
                && offset.y >= 0 && offset.y <= 1;
        }

        private static PortalInputPort[] CreateInputPorts(Vector2Int anchor)
        {
            var ports = new List<PortalInputPort>(8);
            var footprint = new[]
            {
                anchor,
                anchor + Vector2Int.right,
                anchor + Vector2Int.up,
                anchor + Vector2Int.one
            };

            foreach (var portal_position in footprint)
            {
                foreach (GridDirection direction in Enum.GetValues(typeof(GridDirection)))
                {
                    var conveyor_position = portal_position - direction.ToOffset();
                    var offset = conveyor_position - anchor;
                    var inside = offset.x >= 0 && offset.x <= 1
                        && offset.y >= 0 && offset.y <= 1;
                    if (!inside)
                    {
                        ports.Add(new PortalInputPort(
                            conveyor_position,
                            portal_position,
                            direction));
                    }
                }
            }

            return ports.ToArray();
        }
    }
}
