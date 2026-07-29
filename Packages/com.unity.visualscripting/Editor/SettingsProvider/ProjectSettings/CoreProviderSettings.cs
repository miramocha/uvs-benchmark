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
        private readonly PluginConfigurationItemMetadata _flowRecursionSafety;

        private const string Title = "Core Settings";

        readonly GUIContent _toggleAOTSafeModeLabel = new GUIContent("AOT Safe Mode");
        readonly GUIContent _toggleFlowDebuggingLabel = new GUIContent("Flow Debugging", BoltFlowConfiguration.FlowDebuggingTooltip);
        readonly GUIContent _toggleRecursionSafetyLabel = new GUIContent("Recursion Safety", BoltFlowConfiguration.RecursionSafetyTooltip);
        readonly GUIContent _toggleProfilingLabel = new GUIContent("Enable Profiling", "Enables Units to appear in the Profiler Window.");

        private bool _setting;
        private Flow.FlowDebuggingMode _flowDebuggingSetting;
        private Flow.FlowRecursionSafety _flowRecursionSafetySetting;
        private bool _enableProfilingSetting;

        public CoreSettings(BoltCoreConfiguration coreConfig, BoltFlowConfiguration flowConfig)
        {
            _aotSafeMode = coreConfig.GetMetadata(nameof(BoltCoreConfiguration.aotSafeMode));
            _flowDebugging = flowConfig.GetMetadata(nameof(BoltFlowConfiguration.flowDebugging));
            _flowRecursionSafety = flowConfig.GetMetadata(nameof(BoltFlowConfiguration.flowRecursionSafety));

            _setting = (bool)_aotSafeMode.value;
            _flowDebuggingSetting = (Flow.FlowDebuggingMode)_flowDebugging.value;
            _flowRecursionSafetySetting = (Flow.FlowRecursionSafety)_flowRecursionSafety.value;

            _enableProfilingSetting = ScriptingDefineUtility.IsDefineEnabled(ScriptingDefineUtility.ProfilingSymbol);
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

            if (_flowRecursionSafetySetting != (Flow.FlowRecursionSafety)_flowRecursionSafety.value)
            {
                _flowRecursionSafety.value = _flowRecursionSafetySetting;
                _flowRecursionSafety.SaveImmediately();

                bool enableEditorDefine =
                    _flowRecursionSafetySetting == Flow.FlowRecursionSafety.Editor ||
                    _flowRecursionSafetySetting == Flow.FlowRecursionSafety.EditorAndBuild;

                bool enableBuildDefine =
                    _flowRecursionSafetySetting == Flow.FlowRecursionSafety.Build ||
                    _flowRecursionSafetySetting == Flow.FlowRecursionSafety.EditorAndBuild;

                bool editorEnabled = ScriptingDefineUtility.IsDefineEnabled(ScriptingDefineUtility.EditorRecursionSymbol);
                bool buildEnabled = ScriptingDefineUtility.IsDefineEnabled(ScriptingDefineUtility.BuildRecursionSymbol);

                if (enableEditorDefine != editorEnabled)
                {
                    ScriptingDefineUtility.SetDefine(
                        ScriptingDefineUtility.EditorRecursionSymbol,
                        enableEditorDefine);
                }

                if (enableBuildDefine != buildEnabled)
                {
                    ScriptingDefineUtility.SetDefine(
                        ScriptingDefineUtility.BuildRecursionSymbol,
                        enableBuildDefine);
                }
            }

            if (_enableProfilingSetting != ScriptingDefineUtility.IsDefineEnabled(ScriptingDefineUtility.ProfilingSymbol))
            {
                ScriptingDefineUtility.SetDefine(ScriptingDefineUtility.ProfilingSymbol, _enableProfilingSetting);
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

            _flowRecursionSafetySetting = (Flow.FlowRecursionSafety)EditorGUILayout.EnumPopup(_toggleRecursionSafetyLabel, _flowRecursionSafetySetting);

            SaveIfNeeded();
        }
    }
}