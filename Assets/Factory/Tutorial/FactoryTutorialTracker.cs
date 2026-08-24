namespace Maptory.Factory
{
    public enum FactoryTutorialAction
    {
        Pan,
        Zoom,
        SelectConveyor,
        RotateBuilding,
        PlaceConveyor,
        EnterDemolition,
        Demolish
    }

    public sealed class FactoryTutorialTracker
    {
        private readonly FactoryTutorialProgressData progress;
        private float accumulated_pan;
        private float accumulated_zoom;

        public FactoryTutorialTracker(FactoryTutorialProgressData progress)
        {
            this.progress = progress;
        }

        public bool Record(FactoryTutorialAction action, float amount = 1f)
        {
            var completed = progress.initial_step switch
            {
                0 => action == FactoryTutorialAction.Pan
                    && (accumulated_pan += amount) >= 1.5f,
                1 => action == FactoryTutorialAction.Zoom
                    && (accumulated_zoom += amount) >= 0.5f,
                2 => action == FactoryTutorialAction.SelectConveyor,
                3 => action == FactoryTutorialAction.RotateBuilding,
                4 => action == FactoryTutorialAction.PlaceConveyor && amount >= 1f,
                5 => action == FactoryTutorialAction.EnterDemolition,
                6 => action == FactoryTutorialAction.Demolish,
                _ => false
            };
            if (!completed) return false;

            progress.initial_step++;
            ResetAccumulation();
            return true;
        }

        public void ResetAccumulation()
        {
            accumulated_pan = 0f;
            accumulated_zoom = 0f;
        }
    }
}
