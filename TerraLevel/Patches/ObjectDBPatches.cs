using HarmonyLib;
using Jotunn.Managers;

namespace TerraLevel.Patches
{
    /// <summary>
    /// Keeps our TerrainOp in ObjectDB's terrain-op registry.
    ///
    /// The zone owner resolves a TerrainOp from <c>ObjectDB.m_terrainOpsByHash</c> by prefab hash
    /// (TerrainOp.Settings.Deserialize). Jötunn 2.30.2 adds custom TerrainOps in its ZNetScene.Awake postfix,
    /// but <c>ObjectDB.CopyOtherDB</c> (called while a world loads) replaces <c>m_terrainOps</c> with the menu
    /// DB's list and rebuilds the hash dictionary, which drops that registration. Observed on Valheim 1.0.17:
    /// "Failed to deserialize TerrainOp settings for prefab hash ..., cancelling TerrainOp".
    ///
    /// <c>UpdateRegisters</c> is the method that rebuilds the dictionary (called from Awake and CopyOtherDB),
    /// so a postfix there re-adds our entry after every rebuild. Idempotent; silent when nothing to do.
    /// </summary>
    [HarmonyPatch(typeof(ObjectDB), "UpdateRegisters")]
    internal static class ObjectDBPatches
    {
        private static ObjectDB _lastLoggedDb;

        [HarmonyPostfix]
        private static void UpdateRegisters_Postfix(ObjectDB __instance)
        {
            EnsureRegistered(__instance, "ObjectDB.UpdateRegisters");
        }

        internal static void EnsureRegistered(ObjectDB db, string where)
        {
            if (db == null || db.m_terrainOpsByHash.ContainsKey(OpContext.PrefabHash))
            {
                return;
            }

            var prefab = PrefabManager.Instance.GetPrefab(OpContext.PrefabName);
            var op = prefab != null ? prefab.GetComponent<TerrainOp>() : null;
            if (op == null)
            {
                // Piece not created yet (first menu ObjectDB.Awake runs before OnVanillaPrefabsAvailable).
                return;
            }

            if (!db.m_terrainOps.Contains(op))
            {
                db.m_terrainOps.Add(op);
            }
            db.m_terrainOpsByHash[OpContext.PrefabHash] = op;
            OpContext.ResetCache();

            if (!ReferenceEquals(_lastLoggedDb, db))
            {
                _lastLoggedDb = db;
                TerraLevelPlugin.Log.LogInfo($"Registered TerrainOp '{OpContext.PrefabName}' in ObjectDB ({where})");
            }
        }
    }

    /// <summary>
    /// Belt and braces: after Jötunn's own ZNetScene.Awake registration, make sure ours is present too.
    /// </summary>
    [HarmonyPatch(typeof(ZNetScene), "Awake")]
    [HarmonyAfter(Jotunn.Main.ModGuid)]
    internal static class ZNetScenePatches
    {
        [HarmonyPostfix]
        private static void Awake_Postfix()
        {
            ObjectDBPatches.EnsureRegistered(ObjectDB.instance, "ZNetScene.Awake");
        }
    }
}
