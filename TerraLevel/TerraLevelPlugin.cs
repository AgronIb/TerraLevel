using System;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using Jotunn.Utils;
using TerraLevel.Patches;
using UnityEngine;

namespace TerraLevel
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    [BepInDependency(Jotunn.Main.ModGuid)]
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Minor)]
    public class TerraLevelPlugin : BaseUnityPlugin
    {
        public const string PluginGuid = "com.terra.terralevel";
        public const string PluginName = "TerraLevel";
        public const string PluginVersion = "0.1.0";

        private const string BasePrefabName = "mud_road_v2";

        internal static ManualLogSource Log;

        private Harmony _harmony;

        private void Awake()
        {
            Log = Logger;
            Log.LogInfo($"{PluginName} {PluginVersion} loading");

            Cfg.Bind(Config);
            AddLocalization();

            PrefabManager.OnVanillaPrefabsAvailable += AddPiece;

            _harmony = new Harmony(PluginGuid);
            _harmony.PatchAll();

            Log.LogInfo($"{PluginName} loaded");
        }

        private void OnDestroy()
        {
            PrefabManager.OnVanillaPrefabsAvailable -= AddPiece;
            _harmony?.UnpatchSelf();
        }

        /// <summary>
        /// Applies scroll input to the radius and keeps the placement ghost scaled to it.
        /// LateUpdate so it runs after Player.Update has (re)built the ghost for this frame.
        /// </summary>
        private void LateUpdate()
        {
            var player = Player.m_localPlayer;
            if (player == null || !RadiusInput.IsOurPieceSelected(player))
            {
                RadiusState.PendingScroll = 0f;
                return;
            }

            if (RadiusState.ApplyPendingScroll())
            {
                player.Message(MessageHud.MessageType.Center, $"Radius {RadiusState.Current:0.0} m");
            }

            var ghost = player.m_placementGhost;
            if (ghost == null)
            {
                return;
            }

            var prefab = player.m_buildPieces != null ? player.m_buildPieces.GetSelectedPrefab() : null;
            var baseScale = prefab != null ? prefab.transform.localScale : Vector3.one;
            var s = RadiusState.GhostScale;
            ghost.transform.localScale = new Vector3(baseScale.x * s, baseScale.y, baseScale.z * s);
        }

        private static void AddLocalization()
        {
            var localization = LocalizationManager.Instance.GetLocalization();
            localization.AddTranslation("English", new Dictionary<string, string>
            {
                { "terralevel_level", "Level Ground (TerraLevel)" },
                { "terralevel_level_desc", "Levels terrain to the ground under the cursor with no height limit. Hold the modifier key and scroll to change the radius." },
            });
        }

        private void AddPiece()
        {
            // Jötunn fires this before every vanilla prefab copy; we only need to register once.
            PrefabManager.OnVanillaPrefabsAvailable -= AddPiece;

            try
            {
                var basePrefab = PrefabManager.Instance.GetPrefab(BasePrefabName);
                if (basePrefab == null)
                {
                    Log.LogError($"Vanilla prefab '{BasePrefabName}' not found. TerraLevel piece not registered.");
                    return;
                }

                // Effective vanilla radius (largest enabled pass; 3 m smooth/paint in 1.0). Drives the ghost scale.
                var baseOp = basePrefab.GetComponent<TerrainOp>();
                RadiusState.Init(baseOp != null ? baseOp.m_settings.GetRadius() : 3f);

                var config = new PieceConfig
                {
                    PieceTable = PieceTables.Hoe,
                    Name = "$terralevel_level",
                    Description = "$terralevel_level_desc",
                };

                var piece = new CustomPiece(OpContext.PrefabName, BasePrefabName, config);
                PieceManager.Instance.AddPiece(piece);

                Log.LogInfo($"Registered hoe piece '{OpContext.PrefabName}' (base radius {RadiusState.BaseRadius:0.00} m)");
                Log.LogInfo($"Vanilla {BasePrefabName} settings: {OpContext.Describe(baseOp != null ? baseOp.m_settings : null)}");
            }
            catch (Exception ex)
            {
                Log.LogError($"Failed to register TerraLevel piece: {ex}");
            }
        }
    }
}
