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

        public FactoryBuildTool ActiveTool { get; private set; }
        public bool IsDemolitionMode { get; private set; }

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
            if (!Keyboard.current.escapeKey.wasPressedThisFrame) return;

            SetDemolitionMode(false);
            SetActiveTool(FactoryBuildTool.None);
        }
    }
}
