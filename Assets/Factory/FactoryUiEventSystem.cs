using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace Maptory.Factory
{
    public static class FactoryUiEventSystem
    {
        public static void EnsureExists()
        {
            if (EventSystem.current != null) return;

            var event_system = new GameObject("EventSystem", typeof(EventSystem));
            var input_module = event_system.AddComponent<InputSystemUIInputModule>();
            input_module.AssignDefaultActions();
        }
    }
}
