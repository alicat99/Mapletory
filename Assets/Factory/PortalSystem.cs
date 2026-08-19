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
        private const int BASE_VALUE_UNITS = 150;
        private const int MESO_BONUS_UNITS = 50;
        private const long MESO_UPGRADE_BASE_COST = 20L;
        private const long PRODUCTION_UPGRADE_BASE_COST = 20L;
        private const float PRODUCTION_MULTIPLIER = 1.25f;

        private readonly Dictionary<RawMaterialType, MonsterProgress> progress = new();
        private long meso_units;

        public long TotalMeso => meso_units / MESO_SCALE;

        public void RecordSupply(RawMaterialType material)
        {
            var monster = GetProgress(material);
            monster.LifetimeProduction++;
            monster.AvailableProduction++;
            meso_units += GetUnitValueUnits(monster);
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
                * MESO_BONUS_UNITS / (float)MESO_SCALE;
        }

        public float GetProductionMultiplier(RawMaterialType material)
        {
            return Mathf.Pow(PRODUCTION_MULTIPLIER, GetProgress(material).ProductionUpgradeLevel);
        }

        public float GetUnitValue(RawMaterialType material)
        {
            return GetUnitValueUnits(GetProgress(material)) / (float)MESO_SCALE;
        }

        public long GetMesoUpgradeCost(RawMaterialType material)
        {
            var level = GetProgress(material).MesoUpgradeLevel;
            return level >= MAX_UPGRADE_LEVEL
                ? 0L
                : MESO_UPGRADE_BASE_COST * (level + 1L);
        }

        public long GetProductionUpgradeCost(RawMaterialType material)
        {
            var level = GetProgress(material).ProductionUpgradeLevel;
            return level >= MAX_UPGRADE_LEVEL
                ? 0L
                : PRODUCTION_UPGRADE_BASE_COST << level;
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

        private static long GetUnitValueUnits(MonsterProgress monster)
        {
            var additive_value = BASE_VALUE_UNITS
                + monster.MesoUpgradeLevel * MESO_BONUS_UNITS;
            var multiplier = Mathf.Pow(
                PRODUCTION_MULTIPLIER,
                monster.ProductionUpgradeLevel);
            return (long)Math.Round(additive_value * multiplier);
        }

        private sealed class MonsterProgress
        {
            public long LifetimeProduction;
            public long AvailableProduction;
            public int MesoUpgradeLevel;
            public int ProductionUpgradeLevel;
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
