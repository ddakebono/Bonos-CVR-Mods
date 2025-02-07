using System.Reflection;
using ABI_RC.Core.Savior;
using HarmonyLib;

namespace SelfModNormalizationFix
{
    public static class BuildInfo
    {
        public const string Name = "SelfModNormalizationFix";
        public const string Author = "DDAkebono";
        public const string Company = "BTKDevelopment";
        public const string Version = "1.0.1";
    }

    [HarmonyPatch(typeof(CVRSelfModerationManager))]
    class SelfModFix
    {
        private static FieldInfo _modIndex = typeof(CVRSelfModerationManager).GetField("_moderationIndex", BindingFlags.Instance | BindingFlags.NonPublic);
        private static CVRSelfModerationIndex _index;

        [HarmonyPatch(nameof(CVRSelfModerationManager.GetUserNormalization))]
        [HarmonyPrefix]
        static bool GetUserNormalization(CVRSelfModerationManager __instance, string userId, ref bool __result)
        {
            _index ??= _modIndex.GetValue(__instance) as CVRSelfModerationIndex;

            if (_index == null) return true;

            CVRSelfModerationEntryUser orCreateUserEntry = _index.GetOrCreateUserEntry(userId, false);
            __result = orCreateUserEntry is { normalization: true } or null;

            return false;
        }
    }
}