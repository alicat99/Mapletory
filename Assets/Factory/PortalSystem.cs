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
        public const float MESO_PER_ITEM = 1.5f;

        private const float MEASUREMENT_WINDOW = 5f;
        private const float EMA_HALF_LIFE = 6f;

        private readonly Dictionary<RawMaterialType, SupplyMeter> meters = new();
        private float elapsed_time;
        private int pending_meso_halves;

        public long TotalMeso { get; private set; }

        public void RecordSupply(RawMaterialType material)
        {
            if (!meters.TryGetValue(material, out var meter))
            {
                meter = new SupplyMeter();
                meters.Add(material, meter);
            }

            meter.InputTimes.Enqueue(elapsed_time);
            meter.TotalItems++;
            pending_meso_halves += 3;
            TotalMeso += pending_meso_halves / 2;
            pending_meso_halves %= 2;

            RefreshMeter(meter, 0f);
        }

        public void Update(float delta_time)
        {
            elapsed_time += delta_time;
            foreach (var meter in meters.Values)
            {
                RefreshMeter(meter, delta_time);
            }
        }

        public float GetItemsPerMinute(RawMaterialType material)
        {
            return meters.TryGetValue(material, out var meter) ? meter.ItemsPerMinute : 0f;
        }

        public long GetTotalItems(RawMaterialType material)
        {
            return meters.TryGetValue(material, out var meter) ? meter.TotalItems : 0L;
        }

        private void RefreshMeter(SupplyMeter meter, float delta_time)
        {
            while (meter.InputTimes.Count > 0
                && elapsed_time - meter.InputTimes.Peek() > MEASUREMENT_WINDOW)
            {
                meter.InputTimes.Dequeue();
            }

            var raw_rate = CalculateRate(meter.InputTimes);
            if (meter.InputTimes.Count >= 4)
            {
                meter.ItemsPerMinute = raw_rate;
                return;
            }

            var alpha = 1f - Mathf.Exp(-Mathf.Log(2f) * delta_time / EMA_HALF_LIFE);
            meter.ItemsPerMinute = Mathf.Lerp(meter.ItemsPerMinute, raw_rate, alpha);
        }

        private static float CalculateRate(Queue<float> input_times)
        {
            if (input_times.Count < 2) return 0f;

            var times = input_times.ToArray();
            var mean_interval = (times[^1] - times[0]) / (times.Length - 1);
            if (mean_interval <= 0f) return 0f;

            return 60f / mean_interval;
        }

        private sealed class SupplyMeter
        {
            public readonly Queue<float> InputTimes = new();
            public long TotalItems;
            public float ItemsPerMinute;
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
