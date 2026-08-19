using UnityEngine;

namespace Maptory.Factory
{
    public sealed class FactoryProgressAutosave : MonoBehaviour
    {
        private const float SAVE_INTERVAL = 2f;

        private FactoryProgression progression;
        private float next_save_time;

        public void Initialize(FactoryProgression factory_progression)
        {
            progression = factory_progression;
            next_save_time = Time.unscaledTime + SAVE_INTERVAL;
        }

        private void Update()
        {
            if (Time.unscaledTime < next_save_time) return;

            next_save_time = Time.unscaledTime + SAVE_INTERVAL;
            if (progression.NeedsSave) progression.Save();
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused && progression.NeedsSave) progression.Save();
        }

        private void OnApplicationFocus(bool focused)
        {
            if (!focused && progression.NeedsSave) progression.Save();
        }

        private void OnApplicationQuit()
        {
            if (progression.NeedsSave) progression.Save();
        }
    }
}
