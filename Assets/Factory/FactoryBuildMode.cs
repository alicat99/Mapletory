using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Maptory.Factory
{
    public enum FactoryBuildTool
    {
        None,
        Conveyor,
        Extractor,
        DyeingMachine,
        Combiner,
        ErdaInjector,
        ProcessingMachine,
        Portal
    }

    public sealed class FactoryBuildMode : MonoBehaviour
    {
        public event Action<FactoryBuildTool> Changed;
        public event Action<bool> DemolitionChanged;
        public event Action<FactoryBuildTool, GridDirection> Rotated;

        public FactoryBuildTool ActiveTool { get; private set; }
        public bool IsDemolitionMode { get; private set; }

        public GridDirection GetDirection(FactoryBuildTool tool)
        {
            return tool switch
            {
                FactoryBuildTool.Extractor => extractor_direction,
                FactoryBuildTool.DyeingMachine => dyeing_direction,
                FactoryBuildTool.Combiner => combiner_direction,
                FactoryBuildTool.ErdaInjector => erda_injector_direction,
                FactoryBuildTool.ProcessingMachine => processing_direction,
                _ => GridDirection.Up
            };
        }

        private GridDirection extractor_direction = GridDirection.Up;
        private GridDirection dyeing_direction = GridDirection.Up;
        private GridDirection combiner_direction = GridDirection.Up;
        private GridDirection erda_injector_direction = GridDirection.Up;
        private GridDirection processing_direction = GridDirection.Up;

        public void Toggle(FactoryBuildTool tool)
        {
            SetDemolitionMode(false);
            SetActiveTool(ActiveTool == tool ? FactoryBuildTool.None : tool);
        }

        public void ToggleDemolitionMode()
        {
            SetDemolitionMode(!IsDemolitionMode);
        }

        public void SetDemolitionMode(bool active)
        {
            if (IsDemolitionMode == active) return;

            if (active) SetActiveTool(FactoryBuildTool.None);
            IsDemolitionMode = active;
            DemolitionChanged?.Invoke(active);
        }

        public void SetActiveTool(FactoryBuildTool tool)
        {
            if (tool != FactoryBuildTool.None) SetDemolitionMode(false);

            if (ActiveTool == tool)
            {
                return;
            }

            ActiveTool = tool;
            Changed?.Invoke(tool);
        }

        private void Update()
        {
            if (Keyboard.current == null) return;

            if (Keyboard.current.xKey.wasPressedThisFrame) ToggleDemolitionMode();
            if (Keyboard.current.rKey.wasPressedThisFrame) RotateActiveTool();
            if (!Keyboard.current.escapeKey.wasPressedThisFrame) return;

            SetDemolitionMode(false);
            SetActiveTool(FactoryBuildTool.None);
        }

        private void RotateActiveTool()
        {
            var direction = GetDirection(ActiveTool).RotateCounterClockwise();
            switch (ActiveTool)
            {
                case FactoryBuildTool.Extractor:
                    extractor_direction = direction;
                    break;
                case FactoryBuildTool.DyeingMachine:
                    dyeing_direction = direction;
                    break;
                case FactoryBuildTool.Combiner:
                    combiner_direction = direction;
                    break;
                case FactoryBuildTool.ErdaInjector:
                    erda_injector_direction = direction;
                    break;
                case FactoryBuildTool.ProcessingMachine:
                    processing_direction = direction;
                    break;
                default:
                    return;
            }

            Rotated?.Invoke(ActiveTool, direction);
        }
    }
}
