using HarmonyLib;
using UnityEngine;

namespace TerraLevel.Patches
{
    /// <summary>
    /// While the TerraLevel tool is selected and the modifier key is held, mouse wheel input is diverted
    /// to the radius and hidden from the rest of the game (camera zoom reads the same function).
    /// </summary>
    [HarmonyPatch(typeof(ZInput), nameof(ZInput.GetMouseScrollWheel))]
    internal static class InputPatches
    {
        private static int _lastFrame = -1;

        [HarmonyPostfix]
        private static void GetMouseScrollWheel_Postfix(ref float __result)
        {
            if (!RadiusInput.IsAdjusting())
            {
                return;
            }

            // The game calls this several times per frame; count the delta once.
            if (_lastFrame != Time.frameCount)
            {
                _lastFrame = Time.frameCount;
                RadiusState.PendingScroll += __result;
            }

            __result = 0f;
        }
    }

    internal static class RadiusInput
    {
        public static bool IsOurPieceSelected(Player player)
        {
            if (player == null || !player.InPlaceMode())
            {
                return false;
            }
            var piece = player.GetSelectedPiece();
            return piece != null && piece.gameObject.name == OpContext.PrefabName;
        }

        public static bool IsAdjusting()
        {
            var player = Player.m_localPlayer;
            if (player == null || !player.TakeInput())
            {
                return false;
            }
            if (!ZInput.GetKey(Cfg.ModifierKey.Value))
            {
                return false;
            }
            return IsOurPieceSelected(player);
        }
    }
}
