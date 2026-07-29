using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Profile;

namespace Unity.VisualScripting
{
    public static class ScriptingDefineUtility
    {
        public const string ProfilingSymbol = "ENABLE_UVS_PROFILING";
        public const string EditorRecursionSymbol = "ENABLE_UVS_RECURSION_EDITOR";
        public const string BuildRecursionSymbol = "ENABLE_UVS_RECURSION_BUILD";

        public static bool IsDefineEnabled(string symbol)
        {
#if UNITY_6000_0_OR_NEWER
            BuildProfile activeProfile = BuildProfile.GetActiveBuildProfile();
            if (activeProfile != null)
            {
                return activeProfile.scriptingDefines != null && activeProfile.scriptingDefines.Contains(symbol);
            }
#endif

#if UNITY_2022_1_OR_NEWER
            var target = NamedBuildTarget.FromBuildTargetGroup(EditorUserBuildSettings.selectedBuildTargetGroup);
            string definesString = PlayerSettings.GetScriptingDefineSymbols(target);
#else
                var target = EditorUserBuildSettings.selectedBuildTargetGroup;
                string definesString = PlayerSettings.GetScriptingDefineSymbolsForGroup(target);
#endif
            return definesString.Split(';').Contains(symbol);
        }

        public static void SetDefine(string symbol, bool enabled)
        {
            if (string.IsNullOrWhiteSpace(symbol)) return;

#if UNITY_6000_5_OR_NEWER
            foreach (var profile in BuildProfile.GetAllBuildProfiles())
            {
                if (profile != null)
                {
                    List<string> list = profile.scriptingDefines != null
                        ? profile.scriptingDefines.Where(d => !string.IsNullOrWhiteSpace(d)).ToList()
                        : new List<string>();

                    bool changed = false;
                    if (enabled && !list.Contains(symbol))
                    {
                        list.Add(symbol);
                        changed = true;
                    }
                    else if (!enabled && list.Contains(symbol))
                    {
                        list.Remove(symbol);
                        changed = true;
                    }

                    if (changed)
                    {
                        profile.scriptingDefines = list.ToArray();
                        EditorUtility.SetDirty(profile);
                        AssetDatabase.SaveAssetIfDirty(profile);
                    }
                }
            }
#elif UNITY_6000_0_OR_NEWER
            var profile = BuildProfile.GetActiveBuildProfile();
            {
                if (profile != null)
                {
                    List<string> list = profile.scriptingDefines != null
                        ? profile.scriptingDefines.Where(d => !string.IsNullOrWhiteSpace(d)).ToList()
                        : new List<string>();

                    bool changed = false;
                    if (enabled && !list.Contains(symbol))
                    {
                        list.Add(symbol);
                        changed = true;
                    }
                    else if (!enabled && list.Contains(symbol))
                    {
                        list.Remove(symbol);
                        changed = true;
                    }

                    if (changed)
                    {
                        profile.scriptingDefines = list.ToArray();
                        EditorUtility.SetDirty(profile);
                        AssetDatabase.SaveAssetIfDirty(profile);
                    }
                }
            }
#endif

#if UNITY_2022_1_OR_NEWER
            var target = NamedBuildTarget.FromBuildTargetGroup(EditorUserBuildSettings.selectedBuildTargetGroup);
            string definesString = PlayerSettings.GetScriptingDefineSymbols(target);
#else
            var target = EditorUserBuildSettings.selectedBuildTargetGroup;
            string definesString = PlayerSettings.GetScriptingDefineSymbolsForGroup(target);
#endif

            List<string> legacyList = definesString.Split(';').Where(d => !string.IsNullOrWhiteSpace(d)).ToList();
            bool legacyChanged = false;

            if (enabled && !legacyList.Contains(symbol))
            {
                legacyList.Add(symbol);
                legacyChanged = true;
            }
            else if (!enabled && legacyList.Contains(symbol))
            {
                legacyList.Remove(symbol);
                legacyChanged = true;
            }

            if (!legacyChanged) return;

            string result = string.Join(";", legacyList);

#if UNITY_2022_1_OR_NEWER
            PlayerSettings.SetScriptingDefineSymbols(target, result);
#else
            PlayerSettings.SetScriptingDefineSymbolsForGroup(target, result);
#endif
        }
    }
}
