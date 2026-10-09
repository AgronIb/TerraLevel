namespace TerraLevel
{
    /// <summary>
    /// Tracks which terrain operation is currently being applied on this peer (the zone owner),
    /// and provides the clamp bounds the transpiled TerrainComp methods call instead of the ±8 literals.
    /// </summary>
    internal static class OpContext
    {
        /// <summary>Prefab name of our custom hoe piece (clone of mud_road_v2).</summary>
        public const string PrefabName = "terralevel_level";

        /// <summary>Marker written after the vanilla settings hash in the ApplyOperation package.</summary>
        public const int Magic = unchecked((int)0x7E44A1E7);

        /// <summary>True while TerrainComp.DoOperation runs for one of our ops.</summary>
        public static bool Unlimited;

        /// <summary>Settings clone produced by the last Deserialize of one of our ops (carries the chosen radius).</summary>
        public static TerrainOp.Settings Current;

        private static TerrainOp.Settings _shared;
        private static ObjectDB _sharedDb;
        private static int _prefabHash;

        public static int PrefabHash
        {
            get
            {
                if (_prefabHash == 0)
                {
                    _prefabHash = PrefabName.GetStableHashCode();
                }
                return _prefabHash;
            }
        }

        /// <summary>The shared Settings object of our registered prefab, as stored in ObjectDB.</summary>
        public static TerrainOp.Settings SharedSettings
        {
            get
            {
                var db = ObjectDB.instance;
                if (db == null)
                {
                    return null;
                }
                // ObjectDB is recreated on each world load; re-resolve when it changes.
                if (_shared == null || !ReferenceEquals(db, _sharedDb))
                {
                    _shared = db.TryGetTerrainOp(PrefabName, out var op) && op != null ? op.m_settings : null;
                    _sharedDb = db;
                }
                return _shared;
            }
        }

        public static void ResetCache()
        {
            _shared = null;
            _sharedDb = null;
            Current = null;
            Unlimited = false;
        }

        public static bool IsOurs(TerrainOp.Settings settings)
        {
            if (settings == null)
            {
                return false;
            }
            if (Current != null && ReferenceEquals(settings, Current))
            {
                return true;
            }
            var shared = SharedSettings;
            return shared != null && ReferenceEquals(settings, shared);
        }

        public static bool IsOurPrefab(UnityEngine.GameObject go)
        {
            return go != null && Utils.GetPrefabName(go.name) == PrefabName;
        }

        private static readonly System.Reflection.MethodInfo MemberwiseCloneMethod =
            HarmonyLib.AccessTools.Method(typeof(object), "MemberwiseClone");

        /// <summary>Shallow copy of a Settings object (all fields are value types, strings, enums or read-only references).</summary>
        public static TerrainOp.Settings Clone(TerrainOp.Settings source)
        {
            return (TerrainOp.Settings)MemberwiseCloneMethod.Invoke(source, null);
        }

        /// <summary>
        /// Applies the TerraLevel behaviour to a Settings instance: level radius (smooth and paint radii scaled by
        /// the same factor so the edge and the dirt paint follow the levelled area) and the smoothing switch.
        /// Called on the placer (instance copy) and on the zone owner (clone of the shared settings), so the
        /// owner's synced config decides.
        /// </summary>
        public static void ApplyRadius(TerrainOp.Settings settings, float levelRadius)
        {
            if (settings == null || levelRadius <= 0f)
            {
                return;
            }

            // Vanilla 1.0 mud_road_v2 is level=false, smooth=true (r=3, pow=1), paint=true (r=3):
            // "Level Ground" is only a ±1 m/click smoothing toward the cursor height. TerraLevel is a real level op.
            settings.m_level = true;
            settings.m_levelRadius = levelRadius;
            settings.m_levelOffset = 0f;
            settings.m_raise = false;

            // Dirt paint follows the levelled disc.
            settings.m_paintRadius = levelRadius;

            // Vanilla smoothing overshoots inside the levelled disc (computed from pre-level heights, up to ±1 m
            // per click), which tilts the disc on slopes. Off by default for a true flatten; when on, the ring
            // extends past the disc so the edge blends.
            settings.m_smooth = Cfg.SmoothEdges != null && Cfg.SmoothEdges.Value;
            settings.m_smoothRadius = levelRadius * 1.5f;
        }

        public static string Describe(TerrainOp.Settings s)
        {
            return s == null
                ? "null"
                : $"level={s.m_level} r={s.m_levelRadius:0.00} offset={s.m_levelOffset:0.00} square={s.m_square} " +
                  $"smooth={s.m_smooth} r={s.m_smoothRadius:0.00} pow={s.m_smoothPower:0.0} " +
                  $"raise={s.m_raise} paint={s.m_paintCleared} r={s.m_paintRadius:0.00} type={s.m_paintType}";
        }

        private static float Limit => Cfg.MaxHeightDelta != null ? Cfg.MaxHeightDelta.Value : Cfg.VanillaLimit;

        private static bool LiftCap => Unlimited || (Cfg.UnlimitedForVanillaTools != null && Cfg.UnlimitedForVanillaTools.Value);

        // Called from transpiled LevelTerrain / RaiseTerrain: only lifted for our op (or when configured for vanilla tools).
        public static float MinDelta() => LiftCap ? -Limit : -Cfg.VanillaLimit;
        public static float MaxDelta() => LiftCap ? Limit : Cfg.VanillaLimit;

        // Called from transpiled ApplyToHeightmap: runs on every client when terrain is rebuilt, outside any op context,
        // so it must always allow the stored delta or clients would render a clamped hill.
        public static float MinDeltaAlways() => -Limit;
        public static float MaxDeltaAlways() => Limit;
    }
}
