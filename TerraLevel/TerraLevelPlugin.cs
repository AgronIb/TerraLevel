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

            UpdateGhostRing(player.m_placementGhost);
        }

        private const string RingObjectName = "TerraLevelRing";
        private const int RingSegmentsPerMeter = 6;

        private CircleProjector _ringTemplate;
        private GameObject _ringGhost;
        private CircleProjector _ring;

        /// <summary>
        /// Valheim draws ward / crafting-station areas with CircleProjector: a ring of small segment objects
        /// re-placed on the terrain every frame. The hoe ghost has none, so we borrow the ward's segment prefab
        /// and layer mask and attach our own ring to the ghost.
        /// </summary>
        private void CaptureRingTemplate()
        {
            var ward = PrefabManager.Instance.GetPrefab("guard_stone");
            var area = ward != null ? ward.GetComponent<PrivateArea>() : null;
            _ringTemplate = area != null ? area.m_areaMarker : null;

            if (_ringTemplate == null)
            {
                var bench = PrefabManager.Instance.GetPrefab("piece_workbench");
                _ringTemplate = bench != null ? bench.GetComponentInChildren<CircleProjector>(true) : null;
            }

            if (_ringTemplate == null || _ringTemplate.m_prefab == null)
            {
                Log.LogWarning("No CircleProjector template found (guard_stone / piece_workbench); radius ring disabled.");
                _ringTemplate = null;
            }
        }

        private void UpdateGhostRing(GameObject ghost)
        {
            if (ghost == null)
            {
                _ringGhost = null;
                _ring = null;
                return;
            }

            if (!ReferenceEquals(ghost, _ringGhost))
            {
                // New ghost instance (piece re-selected). Reuse a vanilla ring if the prefab has one, else add ours.
                _ringGhost = ghost;
                _ring = ghost.GetComponentInChildren<CircleProjector>(true);
                if (_ring == null && _ringTemplate != null)
                {
                    var ringObject = new GameObject(RingObjectName);
                    ringObject.transform.SetParent(ghost.transform, false);
                    _ring = ringObject.AddComponent<CircleProjector>();
                    _ring.m_prefab = _ringTemplate.m_prefab;
                    _ring.m_mask = _ringTemplate.m_mask;
                    _ring.m_speed = _ringTemplate.m_speed;
                    _ring.m_turns = 1f;
                    _ring.m_start = 0f;
                    _ring.m_sliceLines = false;
                }
            }

            if (_ring == null)
            {
                return;
            }

            var radius = RadiusState.Current;
            _ring.m_radius = radius;
            _ring.m_nrOfSegments = Mathf.Clamp(Mathf.RoundToInt(radius * RingSegmentsPerMeter), 12, 160);
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

                CaptureRingTemplate();

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
