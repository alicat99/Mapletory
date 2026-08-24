using System.Linq;
using NUnit.Framework;

namespace Maptory.Factory.Tests
{
    public sealed class TutorialAndCodexTests
    {
        [Test]
        public void TutorialOnlyAdvancesForTheExpectedRealAction()
        {
            var progress = new FactoryTutorialProgressData();
            var tracker = new FactoryTutorialTracker(progress);

            Assert.That(tracker.Record(FactoryTutorialAction.Zoom, 1f), Is.False);
            Assert.That(progress.initial_step, Is.Zero);
            Assert.That(tracker.Record(FactoryTutorialAction.Pan, 0.7f), Is.False);
            Assert.That(tracker.Record(FactoryTutorialAction.Pan, 0.8f), Is.True);
            Assert.That(tracker.Record(FactoryTutorialAction.Zoom, 0.5f), Is.True);
            Assert.That(tracker.Record(FactoryTutorialAction.SelectConveyor), Is.True);
            Assert.That(tracker.Record(FactoryTutorialAction.RotateBuilding), Is.True);
            Assert.That(tracker.Record(FactoryTutorialAction.PlaceConveyor, 1f), Is.True);
            Assert.That(tracker.Record(FactoryTutorialAction.EnterDemolition), Is.True);
            Assert.That(tracker.Record(FactoryTutorialAction.Demolish), Is.True);
            Assert.That(progress.initial_step, Is.EqualTo(7));
        }

        [Test]
        public void CodexRecursivelyCoversEveryRecipeIngredient()
        {
            foreach (var entry in FactoryContentCatalog.Entries)
            {
                foreach (var ingredient in entry.Ingredients)
                {
                    Assert.That(
                        FactoryContentCatalog.Entries.Any(candidate => candidate.Material == ingredient),
                        Is.True,
                        $"Missing codex entry for {ingredient}");
                }
            }
        }

        [Test]
        public void SharedBuildingOrderMatchesHotbarKeys()
        {
            Assert.That(
                FactoryContentCatalog.Buildings.Select(building => building.Tool),
                Is.EqualTo(new[]
                {
                    FactoryBuildTool.Conveyor,
                    FactoryBuildTool.Extractor,
                    FactoryBuildTool.ErdaInjector,
                    FactoryBuildTool.DyeingMachine,
                    FactoryBuildTool.Combiner,
                    FactoryBuildTool.ProcessingMachine,
                    FactoryBuildTool.Portal
                }));
        }

        [Test]
        public void FeatureHintsAreStoredOnlyOnce()
        {
            var progress = new FactoryTutorialProgressData();

            progress.MarkSeen("codex");
            progress.MarkSeen("codex");

            Assert.That(progress.HasSeen("codex"), Is.True);
            Assert.That(progress.explained_features.Count, Is.EqualTo(1));
        }
    }
}
