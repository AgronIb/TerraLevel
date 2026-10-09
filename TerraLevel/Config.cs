using BepInEx.Configuration;
using UnityEngine;

namespace TerraLevel
{
    /// <summary>
    /// All BepInEx config entries. Entries marked admin-only are pushed from server to clients by Jötunn.
    /// </summary>
    internal static class Cfg
    {
        public const float VanillaLimit = 8f;

        public static ConfigEntry<float> MaxHeightDelta;
        public static ConfigEntry<bool> UnlimitedForVanillaTools;
        public static ConfigEntry<bool> SmoothEdges;

        public static ConfigEntry<float> DefaultRadius;
        public static ConfigEntry<float> MinRadius;
        public static ConfigEntry<float> MaxRadius;
        public static ConfigEntry<float> RadiusStep;
        public static ConfigEntry<KeyCode> ModifierKey;

        public static void Bind(ConfigFile config)
        {
            var adminOnly = new ConfigurationManagerAttributes { IsAdminOnly = true };

            MaxHeightDelta = config.Bind("General", "MaxHeightDelta", 10000f,
                new ConfigDescription(
                    "Maximum height change (meters) relative to the original terrain for the TerraLevel tool. Vanilla is 8. Synced from server.",
                    new AcceptableValueRange<float>(VanillaLimit, 100000f), adminOnly));

            UnlimitedForVanillaTools = config.Bind("General", "UnlimitedForVanillaTools", false,
                new ConfigDescription(
                    "Also lift the 8 m cap for the vanilla Level Ground and Raise Ground tools. Synced from server.",
                    null, adminOnly));

            SmoothEdges = config.Bind("General", "SmoothEdges", false,
                new ConfigDescription(
                    "Run the vanilla smoothing pass after levelling. Off = exactly flat disc with a hard edge. " +
                    "On = vanilla-style soft edge, but the disc itself is tilted by up to 1 m per click on slopes. Synced from server.",
                    null, adminOnly));

            DefaultRadius = config.Bind("Radius", "DefaultRadius", 0f,
                new ConfigDescription(
                    "Starting radius (meters) when the TerraLevel tool is selected. 0 = same as vanilla Level Ground.",
                    new AcceptableValueRange<float>(0f, 50f)));

            MinRadius = config.Bind("Radius", "MinRadius", 0.5f,
                new ConfigDescription("Smallest selectable radius (meters).",
                    new AcceptableValueRange<float>(0.25f, 50f)));

            MaxRadius = config.Bind("Radius", "MaxRadius", 20f,
                new ConfigDescription("Largest selectable radius (meters). Synced from server.",
                    new AcceptableValueRange<float>(0.5f, 50f), adminOnly));

            RadiusStep = config.Bind("Radius", "RadiusStep", 0.5f,
                new ConfigDescription("Radius change per scroll notch (meters).",
                    new AcceptableValueRange<float>(0.1f, 5f)));

            ModifierKey = config.Bind("Radius", "ModifierKey", KeyCode.LeftControl,
                "Hold this key and scroll the mouse wheel to change the radius while the TerraLevel tool is selected.");
        }
    }
}
