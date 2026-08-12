using System.Collections.Generic;
using NUnit.Framework;

namespace Phasix.Tests.EditMode
{
    /// <summary>Covers FormationSystem.IsSlotOccupied — the exclusive-occupancy check shared by the Party menu's pre-battle picker and the in-battle Move skill (2026-08-12, see DECISIONS.md -> [Combat]).</summary>
    public class FormationSystemTests
    {
        [Test]
        public void IsSlotOccupied_EmptyOccupantList_ReturnsFalse()
        {
            Assert.IsFalse(FormationSystem.IsSlotOccupied(new List<(int, int)>(), 4, 3));
        }

        [Test]
        public void IsSlotOccupied_ExactMatchInList_ReturnsTrue()
        {
            var occupied = new List<(int, int)> { (2, 1), (4, 3), (6, 5) };

            Assert.IsTrue(FormationSystem.IsSlotOccupied(occupied, 4, 3));
        }

        [Test]
        public void IsSlotOccupied_SameLaneDifferentPosition_ReturnsFalse()
        {
            var occupied = new List<(int, int)> { (4, 3) };

            Assert.IsFalse(FormationSystem.IsSlotOccupied(occupied, 4, 1));
        }

        [Test]
        public void IsSlotOccupied_SamePositionDifferentLane_ReturnsFalse()
        {
            var occupied = new List<(int, int)> { (4, 3) };

            Assert.IsFalse(FormationSystem.IsSlotOccupied(occupied, 2, 3));
        }
    }
}
