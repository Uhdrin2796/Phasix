using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace Phasix.Tests.EditMode
{
    /// <summary>Covers AudioCueCatalog.GetHitImpactClip's base/duo type resolution and unauthored-slot fallback (2026-08-10 — Phase 3 close-out pass).</summary>
    public class AudioCueCatalogTests
    {
        private static AudioClip MakeClip(string name)
        {
            return AudioClip.Create(name, 1, 1, 44100, false);
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(field, $"Expected private field {fieldName} on {target.GetType().Name}.");
            field.SetValue(target, value);
        }

        [Test]
        public void GetHitImpactClip_BaseType_ReturnsItsOwnIndexedClip()
        {
            var catalog = ScriptableObject.CreateInstance<AudioCueCatalog>();
            AudioClip fireClip = MakeClip("fire-hit");
            var clips = new AudioClip[8];
            clips[(int)PrimalType.Fire] = fireClip;
            SetPrivateField(catalog, "_hitImpactClipsByBaseType", clips);

            AudioClip result = catalog.GetHitImpactClip(PrimalType.Fire);

            Assert.AreSame(fireClip, result);

            Object.DestroyImmediate(catalog);
        }

        [Test]
        public void GetHitImpactClip_DuoType_ResolvesToFirstBaseParentsClip()
        {
            var catalog = ScriptableObject.CreateInstance<AudioCueCatalog>();
            AudioClip fireClip = MakeClip("fire-hit");
            var clips = new AudioClip[8];
            clips[(int)PrimalType.Fire] = fireClip; // Steam's parents are (Fire, Water) — Fire is .a
            SetPrivateField(catalog, "_hitImpactClipsByBaseType", clips);

            AudioClip result = catalog.GetHitImpactClip(PrimalType.Steam);

            Assert.AreSame(fireClip, result);

            Object.DestroyImmediate(catalog);
        }

        [Test]
        public void GetHitImpactClip_UnauthoredSlot_FallsBackToFallbackClip()
        {
            var catalog = ScriptableObject.CreateInstance<AudioCueCatalog>();
            AudioClip fallback = MakeClip("fallback-hit");
            SetPrivateField(catalog, "_hitImpactClipsByBaseType", new AudioClip[8]);
            SetPrivateField(catalog, "_hitImpactFallbackClip", fallback);

            AudioClip result = catalog.GetHitImpactClip(PrimalType.Water);

            Assert.AreSame(fallback, result);

            Object.DestroyImmediate(catalog);
        }

        [Test]
        public void GetHitImpactClip_FullyEmptyCatalog_ReturnsNullWithoutThrowing()
        {
            var catalog = ScriptableObject.CreateInstance<AudioCueCatalog>();
            SetPrivateField(catalog, "_hitImpactClipsByBaseType", new AudioClip[8]);

            AudioClip result = null;
            Assert.DoesNotThrow(() => result = catalog.GetHitImpactClip(PrimalType.Lightning));
            Assert.IsNull(result);

            Object.DestroyImmediate(catalog);
        }
    }
}
