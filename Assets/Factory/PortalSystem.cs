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
        public string SelectionLabel => $"[{SourceName} level1] {ItemLabel} · 공급 0/60";

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
            new(RawMaterialType.SnailRed, "오솔길1", "빨간 달팽이"),
            new(RawMaterialType.Snail, "리스항구 외곽", "달팽이"),
            new(RawMaterialType.MushroomGreen, "포자언덕", "초록 버섯"),
            new(RawMaterialType.SnailBlue, "오솔길2", "파란 달팽이"),
            new(RawMaterialType.SpikeMushroomOrange, "콧노래 오솔길", "주황 뿔버섯"),
            new(RawMaterialType.MushroomOrange, "헤네시스 북쪽언덕", "주황 버섯"),
            new(RawMaterialType.SpikeMushroomGreen, "바람꽃 수풀", "초록 뿔버섯"),
            new(RawMaterialType.MushroomBlue, "꿈꾸는 오솔길", "파란 버섯")
        };

        public static IReadOnlyList<PortalSupplyOption> Options => OPTIONS;
    }

    public sealed class PortalEconomy
    {
        public const float MESO_PER_ITEM = 1.5f;

        private const float SAMPLE_INTERVAL = 1f;
        private const float EMA_HALF_LIFE = 6f;

        private readonly Dictionary<RawMaterialType, SupplyMeter> meters = new();
        private float sample_elapsed;
        private int pending_meso_halves;

        public long TotalMeso { get; private set; }

        public void RecordSupply(RawMaterialType material)
        {
            if (!meters.TryGetValue(material, out var meter))
            {
                meter = new SupplyMeter();
                meters.Add(material, meter);
            }

            meter.WindowItems++;
            meter.TotalItems++;
            pending_meso_halves += 3;
            TotalMeso += pending_meso_halves / 2;
            pending_meso_halves %= 2;
        }

        public void Update(float delta_time)
        {
            sample_elapsed += delta_time;
            if (sample_elapsed < SAMPLE_INTERVAL) return;

            var alpha = 1f - Mathf.Exp(-Mathf.Log(2f) * sample_elapsed / EMA_HALF_LIFE);
            foreach (var meter in meters.Values)
            {
                var sample = meter.WindowItems * 60f / sample_elapsed;
                if (!meter.Initialized && sample > 0f)
                {
                    meter.ItemsPerMinute = sample;
                    meter.Initialized = true;
                }
                else if (meter.Initialized)
                {
                    meter.ItemsPerMinute = Mathf.Lerp(meter.ItemsPerMinute, sample, alpha);
                }

                meter.WindowItems = 0;
            }

            sample_elapsed = 0f;
        }

        public float GetItemsPerMinute(RawMaterialType material)
        {
            return meters.TryGetValue(material, out var meter) ? meter.ItemsPerMinute : 0f;
        }

        public long GetTotalItems(RawMaterialType material)
        {
            return meters.TryGetValue(material, out var meter) ? meter.TotalItems : 0L;
        }

        private sealed class SupplyMeter
        {
            public int WindowItems;
            public long TotalItems;
            public float ItemsPerMinute;
            public bool Initialized;
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
