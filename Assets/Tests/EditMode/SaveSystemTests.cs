using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace Phasix.Tests.EditMode
{
    /// <summary>
    /// Covers the save/load round trip (2026-08 session, see DECISIONS.md -> [Save]):
    /// PhasixSaveData/PartySaveData <-> PhasixRuntimeData/PartySystem, SaveSystem's file I/O, and
    /// TryGetNewestSlot's auto-continue logic. Points SaveSystem.SaveDirectoryOverride at a fresh
    /// temp folder per test so nothing here ever touches a real player's save files —
    /// Application.persistentDataPath resolves to the same fixed folder across every Editor
    /// session for this project.
    /// </summary>
    public class SaveSystemTests
    {
        private string _tempDir;

        [SetUp]
        public void SetUp()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "PhasixSaveTests_" + Guid.NewGuid().ToString("N"));
            SaveSystem.SaveDirectoryOverride = _tempDir;
        }

        [TearDown]
        public void TearDown()
        {
            SaveSystem.SaveDirectoryOverride = null;
            if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, recursive: true);
        }

        private static PhasixData MakeSpecies(string name)
        {
            var species = ScriptableObject.CreateInstance<PhasixData>();
            SetPrivateField(species, "_speciesName", name);
            SetPrivateField(species, "_evolutionTier", 1);
            return species;
        }

        private static SpeciesDatabase MakeSpeciesDatabase(params (PhasixData species, string guid)[] entries)
        {
            var database = ScriptableObject.CreateInstance<SpeciesDatabase>();
            SetPrivateField(database, "_allSpecies", new List<PhasixData>(System.Array.ConvertAll(entries, e => e.species)));
            SetPrivateField(database, "_guids", new List<string>(System.Array.ConvertAll(entries, e => e.guid)));
            return database;
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(field, $"Expected private field {fieldName} on {target.GetType().Name}.");
            field.SetValue(target, value);
        }

        private static PhasixRuntimeData MakeRuntime(PhasixData species)
        {
            var runtime = new PhasixRuntimeData("node-guid-1")
            {
                speciesData = species,
                baseStats = new StatBlock(10, 9, 8, 7, 6, 5, 4, 3),
                unnamedPool = new StatBlock(1, 1, 1, 1, 1, 1, 1, 1),
                aptitude = 2,
                temper = Temper.Edge,
                personality = Personality.Brave,
                origin = OriginType.Wild,
                activeSignalType = SignalType.Pulse,
                bondPercent = 42f,
                bondFloor = 20f,
                sessionBondLoss = 1.5f,
                phaseSaturation = 33f,
                commonAura = 15,
                auraAllocatedPoints = 6,
                rareVariantAura = 2,
            };
            runtime.unlockedTreeTypes.Add(SkillTreeType.Mirror);
            runtime.unlockedTreeTypes.Add(SkillTreeType.Reaction);
            runtime.learnedSkillGuids.Add("skill-guid-a");
            runtime.learnedSkillGuids.Add("skill-guid-b");
            runtime.equippedSkillGuids.Add("skill-guid-a");
            runtime.specificAura["grief"] = 5;
            runtime.specificAura["joy"] = 3;
            runtime.discoveredNodeGuids.Add("node-guid-1");
            runtime.discoveredNodeGuids.Add("node-guid-2");
            return runtime;
        }

        [Test]
        public void PhasixSaveData_RoundTrip_PreservesAllFields()
        {
            PhasixData species = MakeSpecies("Round Trip Species");
            SpeciesDatabase database = MakeSpeciesDatabase((species, "species-guid-1"));
            PhasixRuntimeData original = MakeRuntime(species);

            PhasixSaveData dto = PhasixSaveData.FromRuntime(original, database);
            Assert.IsNotNull(dto);

            // Simulate the JSON round trip explicitly, not just object round trip.
            string json = JsonUtility.ToJson(dto);
            PhasixSaveData reparsed = JsonUtility.FromJson<PhasixSaveData>(json);

            PhasixRuntimeData restored = reparsed.ToRuntime(database);

            Assert.IsNotNull(restored);
            Assert.AreSame(species, restored.speciesData);
            Assert.AreEqual(original.instanceId, restored.instanceId);
            Assert.AreEqual(original.baseStats.Total, restored.baseStats.Total);
            Assert.AreEqual(original.aptitude, restored.aptitude);
            Assert.AreEqual(original.temper, restored.temper);
            Assert.AreEqual(original.origin, restored.origin);
            Assert.AreEqual(original.bondPercent, restored.bondPercent);
            Assert.AreEqual(original.commonAura, restored.commonAura);
            Assert.AreEqual(original.auraAllocatedPoints, restored.auraAllocatedPoints);
            CollectionAssert.AreEquivalent(original.unlockedTreeTypes, restored.unlockedTreeTypes);
            CollectionAssert.AreEquivalent(original.learnedSkillGuids, restored.learnedSkillGuids);
            CollectionAssert.AreEquivalent(original.equippedSkillGuids, restored.equippedSkillGuids);
            CollectionAssert.AreEquivalent(original.discoveredNodeGuids, restored.discoveredNodeGuids);
            Assert.AreEqual(2, restored.specificAura.Count, "Dictionary must survive the flatten-to-parallel-lists round trip.");
            Assert.AreEqual(5, restored.specificAura["grief"]);
            Assert.AreEqual(3, restored.specificAura["joy"]);

            UnityEngine.Object.DestroyImmediate(species);
            UnityEngine.Object.DestroyImmediate(database);
        }

        [Test]
        public void PhasixSaveData_FromRuntime_UnresolvableSpecies_ReturnsNull()
        {
            PhasixData species = MakeSpecies("Unregistered Species");
            SpeciesDatabase emptyDatabase = ScriptableObject.CreateInstance<SpeciesDatabase>();
            PhasixRuntimeData runtime = MakeRuntime(species);

            PhasixSaveData dto = PhasixSaveData.FromRuntime(runtime, emptyDatabase);

            Assert.IsNull(dto, "A species not registered in the SpeciesDatabase must not silently save a broken reference.");

            UnityEngine.Object.DestroyImmediate(species);
            UnityEngine.Object.DestroyImmediate(emptyDatabase);
        }

        [Test]
        public void SaveSystem_SaveThenLoad_RoundTripsThroughRealFile()
        {
            PhasixData species = MakeSpecies("File Round Trip");
            SpeciesDatabase database = MakeSpeciesDatabase((species, "species-guid-1"));

            var partyGo = new GameObject("TestPartySystem");
            var partySystem = partyGo.AddComponent<PartySystem>();
            partySystem.SetSlot(0, MakeRuntime(species));
            // activeSlotIndex deliberately left at -1 (never call SetActiveSlot in this test —
            // it needs _companionPrefab/_playerTransform, out of scope for a save/load test).

            SaveSystem.Save(1, partySystem, database);
            Assert.IsTrue(SaveSystem.SlotExists(1));

            bool loaded = SaveSystem.TryLoad(1, out SaveFile saveFile);
            Assert.IsTrue(loaded);
            Assert.IsNotNull(saveFile.party);
            Assert.AreEqual(PartySystem.MaxPartySize, saveFile.party.slots.Count);
            Assert.IsNotNull(saveFile.party.slots[0]);
            // Note: JsonUtility can't represent a null List<T> element for a class type — an
            // empty slot round-trips as a non-null PhasixSaveData with default/empty field values
            // (e.g. speciesGuid == ""), not literal null. The real contract this test cares about
            // is that it still loads back as an EMPTY party slot, asserted below via GetSlot(1) —
            // PhasixSaveData.ToRuntime already treats an unresolvable/empty speciesGuid as "no
            // Phasix here" and returns null, regardless of whether the DTO wrapper itself is null.

            var freshPartyGo = new GameObject("FreshPartySystem");
            var freshPartySystem = freshPartyGo.AddComponent<PartySystem>();
            SaveSystem.ApplyToPartySystem(saveFile, freshPartySystem, database);

            Assert.IsNotNull(freshPartySystem.GetSlot(0));
            Assert.AreEqual(species, freshPartySystem.GetSlot(0).speciesData);
            Assert.IsNull(freshPartySystem.GetSlot(1), "An empty save slot must load back as an empty party slot.");

            UnityEngine.Object.DestroyImmediate(partyGo);
            UnityEngine.Object.DestroyImmediate(freshPartyGo);
            UnityEngine.Object.DestroyImmediate(species);
            UnityEngine.Object.DestroyImmediate(database);
        }

        [Test]
        public void TryGetNewestSlot_NoSlotsSaved_ReturnsFalse()
        {
            bool found = SaveSystem.TryGetNewestSlot(out int slot);

            Assert.IsFalse(found);
            Assert.AreEqual(-1, slot);
        }

        [Test]
        public void TryGetNewestSlot_PicksMostRecentlyWrittenFile()
        {
            PhasixData species = MakeSpecies("Newest Slot Species");
            SpeciesDatabase database = MakeSpeciesDatabase((species, "species-guid-1"));
            var partyGo = new GameObject("TestPartySystem");
            var partySystem = partyGo.AddComponent<PartySystem>();
            partySystem.SetSlot(0, MakeRuntime(species));

            SaveSystem.Save(0, partySystem, database);
            System.Threading.Thread.Sleep(50); // ensure a measurably later file-write timestamp
            SaveSystem.Save(2, partySystem, database);

            bool found = SaveSystem.TryGetNewestSlot(out int newestSlot);

            Assert.IsTrue(found);
            Assert.AreEqual(2, newestSlot, "Slot 2 was saved after slot 0, so it must be picked as the newest.");

            UnityEngine.Object.DestroyImmediate(partyGo);
            UnityEngine.Object.DestroyImmediate(species);
            UnityEngine.Object.DestroyImmediate(database);
        }

        [Test]
        public void DeleteAllSlots_RemovesEveryFile()
        {
            PhasixData species = MakeSpecies("Delete Test Species");
            SpeciesDatabase database = MakeSpeciesDatabase((species, "species-guid-1"));
            var partyGo = new GameObject("TestPartySystem");
            var partySystem = partyGo.AddComponent<PartySystem>();
            partySystem.SetSlot(0, MakeRuntime(species));

            SaveSystem.Save(0, partySystem, database);
            SaveSystem.Save(1, partySystem, database);
            Assert.IsTrue(SaveSystem.SlotExists(0));
            Assert.IsTrue(SaveSystem.SlotExists(1));

            SaveSystem.DeleteAllSlots();

            Assert.IsFalse(SaveSystem.SlotExists(0));
            Assert.IsFalse(SaveSystem.SlotExists(1));
            Assert.IsFalse(SaveSystem.SlotExists(2));

            UnityEngine.Object.DestroyImmediate(partyGo);
            UnityEngine.Object.DestroyImmediate(species);
            UnityEngine.Object.DestroyImmediate(database);
        }
    }
}
