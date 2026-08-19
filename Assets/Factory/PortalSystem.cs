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
            new(RawMaterialType.MonsterSnailGreen, "리스항구 외곽", "달팽이"),
            new(RawMaterialType.MonsterSnailRed, "오솔길1", "빨간 달팽이"),
            new(RawMaterialType.MonsterSnailBlue, "오솔길2", "파란 달팽이"),
            new(RawMaterialType.MonsterMushroomBlue, "꿈꾸는 오솔길", "파란 버섯"),
            new(RawMaterialType.MonsterMushroomOrange, "헤네시스 북쪽언덕", "주황 버섯"),
            new(RawMaterialType.MonsterMushroomGreen, "포자언덕", "초록 버섯"),
            new(RawMaterialType.MonsterSpikeMushroomOrange, "주황 뿔버섯 숲", "주황 뿔버섯"),
            new(RawMaterialType.MonsterSpikeMushroomGreen, "초록 뿔버섯 숲", "초록 뿔버섯")
        };

        public static IReadOnlyList<PortalSupplyOption> Options => OPTIONS;

        public static PortalSupplyOption Get(RawMaterialType material)
        {
            foreach (var option in OPTIONS)
            {
                if (option.Material == material) return option;
            }

            throw new ArgumentException($"Unknown portal supply material: {material}");
        }
    }

    public sealed class PortalEconomy
    {
        private const int MESO_SCALE = 100;
        private const float DEFAULT_MESO_COST_COEFFICIENT = 1.25f;
        private const float DEFAULT_PRODUCTION_COST_COEFFICIENT = 5f;

        private readonly Dictionary<RawMaterialType, MonsterProgress> progress = new();
        private readonly Dictionary<RawMaterialType, MonsterBalance> balances = new();
        private long meso_units;

        public long TotalMeso => meso_units / MESO_SCALE;
        public float MesoUpgradeCostCoefficient { get; private set; } =
            DEFAULT_MESO_COST_COEFFICIENT;
        public float ProductionUpgradeCostCoefficient { get; private set; } =
            DEFAULT_PRODUCTION_COST_COEFFICIENT;
        public event Action Changed;

        public void RecordSupply(RawMaterialType material)
        {
            var monster = GetProgress(material);
            monster.LifetimeProduction++;
            monster.AvailableProduction++;
            meso_units += GetUnitValueUnits(monster, GetBalance(material));
            Changed?.Invoke();
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

        public long GetMesoUpgradeBaseCost(RawMaterialType material)
        {
            return GetBalance(material).MesoUpgradeBaseCost;
        }

        public long GetProductionUpgradeBaseCost(RawMaterialType material)
        {
            return GetBalance(material).ProductionUpgradeBaseCost;
        }

        public void SetBaseValue(RawMaterialType material, float value)
        {
            GetBalance(material).BaseValue = Mathf.Max(0f, value);
            Changed?.Invoke();
        }

        public void SetMesoBonusPerLevel(RawMaterialType material, float value)
        {
            GetBalance(material).MesoBonusPerLevel = Mathf.Max(0f, value);
            Changed?.Invoke();
        }

        public void SetProductionMultiplierPerLevel(RawMaterialType material, float value)
        {
            GetBalance(material).ProductionMultiplierPerLevel = Mathf.Max(0.01f, value);
            Changed?.Invoke();
        }

        public void SetMesoUpgradeLevel(RawMaterialType material, int level)
        {
            GetProgress(material).MesoUpgradeLevel = Mathf.Max(0, level);
            Changed?.Invoke();
        }

        public void SetProductionUpgradeLevel(RawMaterialType material, int level)
        {
            GetProgress(material).ProductionUpgradeLevel = Mathf.Max(0, level);
            Changed?.Invoke();
        }

        public void SetAvailableProduction(RawMaterialType material, long amount)
        {
            GetProgress(material).AvailableProduction = System.Math.Max(0L, amount);
            Changed?.Invoke();
        }

        public void SetUpgradeBaseCosts(
            RawMaterialType material,
            long meso_base_cost,
            long production_base_cost)
        {
            var balance = GetBalance(material);
            balance.MesoUpgradeBaseCost = System.Math.Max(1L, meso_base_cost);
            balance.ProductionUpgradeBaseCost = System.Math.Max(1L, production_base_cost);
            Changed?.Invoke();
        }

        public void SetUpgradeCostCoefficients(float meso, float production)
        {
            MesoUpgradeCostCoefficient = Mathf.Max(1f, meso);
            ProductionUpgradeCostCoefficient = Mathf.Max(1f, production);
            Changed?.Invoke();
        }

        public bool CanSpendMeso(long amount)
        {
            return amount >= 0L && amount <= meso_units / MESO_SCALE;
        }

        public bool TrySpendMeso(long amount)
        {
            if (!CanSpendMeso(amount)) return false;

            meso_units -= amount * MESO_SCALE;
            Changed?.Invoke();
            return true;
        }

        public bool CanSpend(long meso, RawMaterialType material, long amount)
        {
            return CanSpendMeso(meso)
                && amount >= 0L
                && GetAvailableProduction(material) >= amount;
        }

        public bool TrySpend(long meso, RawMaterialType material, long amount)
        {
            if (!CanSpend(meso, material, amount)) return false;

            meso_units -= meso * MESO_SCALE;
            GetProgress(material).AvailableProduction -= amount;
            Changed?.Invoke();
            return true;
        }

        public long GetMesoUpgradeCost(RawMaterialType material)
        {
            var level = GetProgress(material).MesoUpgradeLevel;
            return CalculateUpgradeCost(
                GetBalance(material).MesoUpgradeBaseCost,
                MesoUpgradeCostCoefficient,
                level);
        }

        public long GetProductionUpgradeCost(RawMaterialType material)
        {
            var level = GetProgress(material).ProductionUpgradeLevel;
            return CalculateUpgradeCost(
                GetBalance(material).ProductionUpgradeBaseCost,
                ProductionUpgradeCostCoefficient,
                level);
        }

        public bool CanPurchaseMesoUpgrade(RawMaterialType material)
        {
            return CanSpendMeso(GetMesoUpgradeCost(material));
        }

        public bool CanPurchaseProductionUpgrade(RawMaterialType material)
        {
            var cost = GetProductionUpgradeCost(material);
            return GetProgress(material).AvailableProduction >= cost;
        }

        public bool TryPurchaseMesoUpgrade(RawMaterialType material)
        {
            if (!CanPurchaseMesoUpgrade(material)) return false;

            var monster = GetProgress(material);
            meso_units -= GetMesoUpgradeCost(material) * MESO_SCALE;
            monster.MesoUpgradeLevel++;
            Changed?.Invoke();
            return true;
        }

        public bool TryPurchaseProductionUpgrade(RawMaterialType material)
        {
            if (!CanPurchaseProductionUpgrade(material)) return false;

            var monster = GetProgress(material);
            monster.AvailableProduction -= GetProductionUpgradeCost(material);
            monster.ProductionUpgradeLevel++;
            Changed?.Invoke();
            return true;
        }

        public PortalEconomyProgressData ExportProgress()
        {
            var data = new PortalEconomyProgressData { meso_units = meso_units };
            foreach (var option in PortalSupplyCatalog.Options)
            {
                var monster = GetProgress(option.Material);
                data.monsters.Add(new MonsterProgressData
                {
                    material = option.Material,
                    lifetime_production = monster.LifetimeProduction,
                    available_production = monster.AvailableProduction,
                    meso_upgrade_level = monster.MesoUpgradeLevel,
                    production_upgrade_level = monster.ProductionUpgradeLevel
                });
            }
            return data;
        }

        public void ImportProgress(PortalEconomyProgressData data)
        {
            progress.Clear();
            meso_units = data.meso_units;
            foreach (var saved in data.monsters)
            {
                progress.Add(saved.material, new MonsterProgress
                {
                    LifetimeProduction = saved.lifetime_production,
                    AvailableProduction = saved.available_production,
                    MesoUpgradeLevel = Mathf.Max(0, saved.meso_upgrade_level),
                    ProductionUpgradeLevel = Mathf.Max(0, saved.production_upgrade_level)
                });
            }
        }

        public PortalEconomySettingsData ExportSettings()
        {
            var data = new PortalEconomySettingsData
            {
                configured = true,
                meso_cost_coefficient = MesoUpgradeCostCoefficient,
                production_cost_coefficient = ProductionUpgradeCostCoefficient
            };
            foreach (var option in PortalSupplyCatalog.Options)
            {
                var balance = GetBalance(option.Material);
                data.monsters.Add(new MonsterBalanceData
                {
                    material = option.Material,
                    base_value = balance.BaseValue,
                    meso_bonus_per_level = balance.MesoBonusPerLevel,
                    production_multiplier_per_level = balance.ProductionMultiplierPerLevel,
                    meso_upgrade_base_cost = balance.MesoUpgradeBaseCost,
                    production_upgrade_base_cost = balance.ProductionUpgradeBaseCost
                });
            }
            return data;
        }

        public void ImportSettings(PortalEconomySettingsData data)
        {
            if (!data.configured) return;

            MesoUpgradeCostCoefficient = Mathf.Max(1f, data.meso_cost_coefficient);
            ProductionUpgradeCostCoefficient = Mathf.Max(1f, data.production_cost_coefficient);
            balances.Clear();
            foreach (var saved in data.monsters)
            {
                balances.Add(saved.material, new MonsterBalance
                {
                    BaseValue = Mathf.Max(0f, saved.base_value),
                    MesoBonusPerLevel = Mathf.Max(0f, saved.meso_bonus_per_level),
                    ProductionMultiplierPerLevel = Mathf.Max(
                        0.01f,
                        saved.production_multiplier_per_level),
                    MesoUpgradeBaseCost = System.Math.Max(1L, saved.meso_upgrade_base_cost),
                    ProductionUpgradeBaseCost = System.Math.Max(
                        1L,
                        saved.production_upgrade_base_cost)
                });
            }
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
                balance = CreateDefaultBalance(material);
                balances.Add(material, balance);
            }

            return balance;
        }

        private static MonsterBalance CreateDefaultBalance(RawMaterialType material)
        {
            return material switch
            {
                RawMaterialType.MonsterSnailGreen => new MonsterBalance(1f, 0.1f, 50L),
                RawMaterialType.MonsterSnailRed => new MonsterBalance(2f, 0.2f, 100L),
                RawMaterialType.MonsterSnailBlue => new MonsterBalance(3f, 0.3f, 400L),
                RawMaterialType.MonsterMushroomBlue => new MonsterBalance(5f, 0.5f, 500L),
                RawMaterialType.MonsterMushroomOrange => new MonsterBalance(7f, 0.7f, 1500L),
                RawMaterialType.MonsterMushroomGreen => new MonsterBalance(10f, 1f, 4500L),
                RawMaterialType.MonsterSpikeMushroomOrange => new MonsterBalance(20f, 2f, 7500L),
                RawMaterialType.MonsterSpikeMushroomGreen => new MonsterBalance(30f, 3f, 15000L),
                _ => new MonsterBalance(1f, 0.1f, 50L)
            };
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

        private static long CalculateUpgradeCost(long base_cost, float coefficient, int level)
        {
            var cost = base_cost * Math.Pow(coefficient, level);
            return cost >= long.MaxValue ? long.MaxValue : (long)Math.Ceiling(cost);
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
            public float BaseValue;
            public float MesoBonusPerLevel;
            public float ProductionMultiplierPerLevel;
            public long MesoUpgradeBaseCost;
            public long ProductionUpgradeBaseCost;

            public MonsterBalance()
            {
            }

            public MonsterBalance(float base_value, float meso_bonus, long meso_cost)
            {
                BaseValue = base_value;
                MesoBonusPerLevel = meso_bonus;
                ProductionMultiplierPerLevel = 1.5f;
                MesoUpgradeBaseCost = meso_cost;
                ProductionUpgradeBaseCost = 100L;
            }
        }
    }

    [Serializable]
    public sealed class PortalEconomyProgressData
    {
        public long meso_units;
        public List<MonsterProgressData> monsters = new();
    }

    [Serializable]
    public sealed class MonsterProgressData
    {
        public RawMaterialType material;
        public long lifetime_production;
        public long available_production;
        public int meso_upgrade_level;
        public int production_upgrade_level;
    }

    [Serializable]
    public sealed class PortalEconomySettingsData
    {
        public bool configured;
        public float meso_cost_coefficient;
        public float production_cost_coefficient;
        public List<MonsterBalanceData> monsters = new();
    }

    [Serializable]
    public sealed class MonsterBalanceData
    {
        public RawMaterialType material;
        public float base_value;
        public float meso_bonus_per_level;
        public float production_multiplier_per_level;
        public long meso_upgrade_base_cost;
        public long production_upgrade_base_cost;
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
        private readonly Func<RawMaterialType, bool> material_allowed;

        public Vector2Int Anchor { get; }
        public Vector2 VisualCenter => Anchor + new Vector2(0.5f, 0.5f);
        public RawMaterialType? SelectedMaterial { get; private set; }
        public IReadOnlyList<PortalInputPort> InputPorts => input_ports;

        public PortalState(
            Vector2Int anchor,
            PortalEconomy portal_economy,
            Func<RawMaterialType, bool> is_material_allowed = null)
        {
            Anchor = anchor;
            economy = portal_economy;
            material_allowed = is_material_allowed ?? (_ => true);
            input_ports = CreateInputPorts(anchor);
        }

        public void SelectMaterial(RawMaterialType material)
        {
            if (!material_allowed(material))
            {
                throw new InvalidOperationException("The hunting ground is locked.");
            }

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
