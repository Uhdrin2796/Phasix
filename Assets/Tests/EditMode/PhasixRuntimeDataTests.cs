using NUnit.Framework;

namespace Phasix.Tests.EditMode
{
    /// <summary>Covers PhasixRuntimeData.preferredLaneIndex/preferredPositionIndex (2026-08-12 formation grid system, see DECISIONS.md -> [Combat]) — the rest of this class has no existing coverage, out of scope here.</summary>
    public class PhasixRuntimeDataTests
    {
        [Test]
        public void PreferredLaneIndex_DefaultsToLaneMovementSystemDefaultStartingLane()
        {
            var runtime = new PhasixRuntimeData("test-node-guid");

            Assert.AreEqual(LaneMovementSystem.DefaultStartingLane, runtime.preferredLaneIndex);
        }

        [Test]
        public void PreferredPositionIndex_DefaultsToLaneMovementSystemDefaultStartingPosition()
        {
            var runtime = new PhasixRuntimeData("test-node-guid");

            Assert.AreEqual(LaneMovementSystem.DefaultStartingPosition, runtime.preferredPositionIndex);
        }
    }
}
