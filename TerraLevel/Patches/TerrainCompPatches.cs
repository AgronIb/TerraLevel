using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;

namespace TerraLevel.Patches
{
    /// <summary>
    /// Lifts the hard-coded ±8 m terrain delta limit.
    ///
    /// Valheim 1.0 (build 25730771):
    ///   TerrainComp.LevelTerrain   : m_levelDelta[i] = Mathf.Clamp(m_levelDelta[i], -8f, 8f);
    ///   TerrainComp.RaiseTerrain   : same
    ///   TerrainComp.ApplyToHeightmap: value = Mathf.Clamp(value, base - 8f, base + 8f);
    /// The transpilers swap the float literals for calls into OpContext. If a game update changes the
    /// number of literals, the method is left untouched and an error is logged.
    /// </summary>
    [HarmonyPatch(typeof(TerrainComp))]
    internal static class TerrainCompPatches
    {
        [HarmonyPatch("DoOperation")]
        [HarmonyPrefix]
        private static void DoOperation_Prefix(UnityEngine.Vector3 pos, TerrainOp.Settings modifier)
        {
            OpContext.Unlimited = OpContext.IsOurs(modifier);
            if (OpContext.Unlimited)
            {
                TerraLevelPlugin.Log.LogDebug($"TerraLevel op at y={pos.y:0.00} limit=±{OpContext.MaxDelta():0} {OpContext.Describe(modifier)}");
            }
        }

        [HarmonyPatch("DoOperation")]
        [HarmonyFinalizer]
        private static void DoOperation_Finalizer()
        {
            OpContext.Unlimited = false;
            OpContext.Current = null;
        }

        [HarmonyPatch("LevelTerrain")]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> LevelTerrain_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original)
        {
            return ReplaceClampLiterals(instructions, original,
                AccessTools.Method(typeof(OpContext), nameof(OpContext.MinDelta)),
                AccessTools.Method(typeof(OpContext), nameof(OpContext.MaxDelta)),
                expected: 2);
        }

        [HarmonyPatch("RaiseTerrain")]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> RaiseTerrain_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original)
        {
            return ReplaceClampLiterals(instructions, original,
                AccessTools.Method(typeof(OpContext), nameof(OpContext.MinDelta)),
                AccessTools.Method(typeof(OpContext), nameof(OpContext.MaxDelta)),
                expected: 2);
        }

        /// <summary>
        /// Runs on every client whenever a heightmap is rebuilt, outside any op context,
        /// so this one always uses the configured limit.
        /// </summary>
        [HarmonyPatch("ApplyToHeightmap")]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> ApplyToHeightmap_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original)
        {
            return ReplaceClampLiterals(instructions, original,
                AccessTools.Method(typeof(OpContext), nameof(OpContext.MinDeltaAlways)),
                AccessTools.Method(typeof(OpContext), nameof(OpContext.MaxDeltaAlways)),
                expected: 2);
        }

        private static IEnumerable<CodeInstruction> ReplaceClampLiterals(IEnumerable<CodeInstruction> instructions, MethodBase original,
            MethodInfo minMethod, MethodInfo maxMethod, int expected)
        {
            var list = instructions.ToList();
            var targets = new List<(int index, MethodInfo replacement)>();

            for (var i = 0; i < list.Count; i++)
            {
                var ci = list[i];
                if (ci.opcode != OpCodes.Ldc_R4 || !(ci.operand is float value))
                {
                    continue;
                }
                if (value == -Cfg.VanillaLimit)
                {
                    targets.Add((i, minMethod));
                }
                else if (value == Cfg.VanillaLimit)
                {
                    targets.Add((i, maxMethod));
                }
            }

            if (targets.Count != expected)
            {
                TerraLevelPlugin.Log.LogError(
                    $"{original.DeclaringType?.Name}.{original.Name}: expected {expected} clamp literals (±{Cfg.VanillaLimit}), found {targets.Count}. Method left unpatched; the game version is probably not supported.");
                return list;
            }

            foreach (var (index, replacement) in targets)
            {
                // Mutating in place keeps labels and exception blocks attached to the instruction.
                var ci = list[index];
                ci.opcode = OpCodes.Call;
                ci.operand = replacement;
            }

            TerraLevelPlugin.Log.LogInfo($"{original.DeclaringType?.Name}.{original.Name}: replaced {targets.Count}/{expected} clamp literals");
            return list;
        }
    }
}
