using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;

#if UNITY_6000_0_OR_NEWER
using UnityEditor.Build.Profile;
#endif
using UnityEngine;

namespace Unity.VisualScripting
{
    internal class CoreSettings
    {
        private readonly PluginConfigurationItemMetadata _aotSafeMode;
        private readonly PluginConfigurationItemMetadata _flowDebugging;

        private const string Title = "Core Settings";
        private const string ProfilingSymbol = "ENABLE_UVS_PROFILING";

        readonly GUIContent _toggleAOTSafeModeLabel = new GUIContent("AOT Safe Mode");
        readonly GUIContent _toggleFlowDebuggingLabel = new GUIContent("Flow Debugging", BoltFlowConfiguration.FlowDebuggingTooltip);
        readonly GUIContent _toggleProfilingLabel = new GUIContent("Enable Profiling", "Enables Units to appear in the Profiler Window.");

        private bool _setting;
        private Flow.FlowDebuggingMode _flowDebuggingSetting;
        private bool _enableProfilingSetting;

        public CoreSettings(BoltCoreConfiguration coreConfig, BoltFlowConfiguration flowConfig)
        {
            _aotSafeMode = coreConfig.GetMetadata(nameof(BoltCoreConfiguration.aotSafeMode));
            _flowDebugging = flowConfig.GetMetadata(nameof(BoltFlowConfiguration.flowDebugging));

            _setting = (bool)_aotSafeMode.value;
            _flowDebuggingSetting = (Flow.FlowDebuggingMode)_flowDebugging.value;

            _enableProfilingSetting = IsDefineEnabled(ProfilingSymbol);
        }

        private void SaveIfNeeded()
        {
            var settings = (bool)_aotSafeMode.value;

            if (_setting != settings)
            {
                _aotSafeMode.value = _setting;
                _aotSafeMode.SaveImmediately();
            }

            if (_flowDebuggingSetting != (Flow.FlowDebuggingMode)_flowDebugging.value)
            {
                _flowDebugging.value = _flowDebuggingSetting;
                _flowDebugging.SaveImmediately();

                Flow.debuggingMode = _flowDebuggingSetting;
            }

            if (_enableProfilingSetting != IsDefineEnabled(ProfilingSymbol))
            {
                SetDefine(ProfilingSymbol, _enableProfilingSetting);
            }
        }

        public void OnGUI()
        {
            GUILayout.Space(5f);

            GUILayout.Label(Title, EditorStyles.boldLabel);

            GUILayout.Space(5f);

            _setting = GUILayout.Toggle(_setting, _toggleAOTSafeModeLabel);

            _enableProfilingSetting = GUILayout.Toggle(_enableProfilingSetting, _toggleProfilingLabel);

            _flowDebuggingSetting = (Flow.FlowDebuggingMode)EditorGUILayout.EnumPopup(_toggleFlowDebuggingLabel, _flowDebuggingSetting);

            SaveIfNeeded();
        }

        #region Scripting Defines Helpers

        private static bool IsDefineEnabled(string symbol)
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

        private static void SetDefine(string symbol, bool enabled)
        {
            if (string.IsNullOrWhiteSpace(symbol)) return;

#if UNITY_6000_0_OR_NEWER
            BuildProfile activeProfile = BuildProfile.GetActiveBuildProfile();

            if (activeProfile != null)
            {
                List<string> list = activeProfile.scriptingDefines != null 
                    ? activeProfile.scriptingDefines.Where(d => !string.IsNullOrWhiteSpace(d)).ToList() 
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
                    activeProfile.scriptingDefines = list.ToArray();
                    EditorUtility.SetDirty(activeProfile);
                    AssetDatabase.SaveAssetIfDirty(activeProfile);
                }
                return;
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

        #endregion
    }
}