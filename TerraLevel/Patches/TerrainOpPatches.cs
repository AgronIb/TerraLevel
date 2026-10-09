using HarmonyLib;
using UnityEngine;

namespace TerraLevel.Patches
{
    /// <summary>
    /// Carries the chosen radius from the placing client to the zone owner.
    ///
    /// Valheim 1.0 serializes a TerrainOp as its prefab hash only; the owner resolves the shared prefab
    /// settings from ObjectDB. We append (magic, radius) after the hash. Vanilla readers stop after the hash,
    /// our Deserialize postfix reads the tail and returns a clone of the shared settings with the radius applied.
    /// </summary>
    [HarmonyPatch] // required: Harmony.PatchAll ignores classes without a class-level HarmonyPatch attribute
    internal static class TerrainOpPatches
    {
        /// <summary>
        /// Placer side. Runs before TerrainOp.Awake finds heightmaps and calls ApplyOperation,
        /// so the instance copy of the settings already carries the chosen radius.
        /// </summary>
        [HarmonyPatch(typeof(TerrainOp), "Awake")]
        [HarmonyPrefix]
        private static void Awake_Prefix(TerrainOp __instance)
        {
            if (TerrainOp.m_forceDisableTerrainOps)
            {
                // Placement ghost being instantiated; Awake returns early anyway.
                return;
            }
            if (!OpContext.IsOurPrefab(__instance.gameObject))
            {
                return;
            }

            OpContext.ApplyRadius(__instance.m_settings, RadiusState.Current);
        }

        [HarmonyPatch(typeof(TerrainOp.Settings), nameof(TerrainOp.Settings.Serialize))]
        [HarmonyPostfix]
        private static void Serialize_Postfix(TerrainOp.Settings __instance, ZPackage pkg, GameObject prefab)
        {
            if (!OpContext.IsOurPrefab(prefab))
            {
                return;
            }

            pkg.Write(OpContext.Magic);
            pkg.Write(__instance.m_levelRadius);
        }

        [HarmonyPatch(typeof(TerrainOp.Settings), nameof(TerrainOp.Settings.Deserialize))]
        [HarmonyPostfix]
        private static void Deserialize_Postfix(ZPackage pkg, ref TerrainOp.Settings __result)
        {
            if (__result == null)
            {
                return;
            }

            var shared = OpContext.SharedSettings;
            if (shared == null || !ReferenceEquals(__result, shared))
            {
                return;
            }

            var radius = shared.m_levelRadius;
            // int magic + float radius
            if (pkg.Size() - pkg.GetPos() >= 8)
            {
                var pos = pkg.GetPos();
                if (pkg.ReadInt() == OpContext.Magic)
                {
                    radius = pkg.ReadSingle();
                }
                else
                {
                    pkg.SetPos(pos);
                }
            }

            // The owner enforces the synced maximum; a client cannot exceed it by editing its own config.
            radius = RadiusState.Clamp(radius);

            var clone = OpContext.Clone(shared);
            OpContext.ApplyRadius(clone, radius);
            OpContext.Current = clone;
            __result = clone;
        }
    }
}
