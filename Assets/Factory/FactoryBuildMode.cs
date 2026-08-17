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

        public FactoryBuildTool ActiveTool { get; private set; }

        public void Toggle(FactoryBuildTool tool)
        {
            SetActiveTool(ActiveTool == tool ? FactoryBuildTool.None : tool);
        }

        public void SetActiveTool(FactoryBuildTool tool)
        {
            if (ActiveTool == tool)
            {
                return;
            }

            ActiveTool = tool;
            Changed?.Invoke(tool);
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                SetActiveTool(FactoryBuildTool.None);
            }
        }
    }
}
