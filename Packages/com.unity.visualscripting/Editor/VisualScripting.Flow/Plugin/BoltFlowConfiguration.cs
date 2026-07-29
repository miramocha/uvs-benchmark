using System.Collections.Generic;

namespace Unity.VisualScripting
{
    [Plugin(BoltFlow.ID)]
    public sealed class BoltFlowConfiguration : PluginConfiguration
    {
        private BoltFlowConfiguration(BoltFlow plugin) : base(plugin) { }

        public override string header => "Script Graphs";

        /// <summary>
        /// (Experimental) Whether the node database should be incrementally updated
        /// whenever a codebase change is detected.
        /// </summary>
        [EditorPref, RenamedFrom("updateUnitsAutomatically")]
        public bool updateNodesAutomatically { get; set; } = false;

        /// <summary>
        /// Whether predictive debugging should warn about null value inputs.
        /// Note that in some cases, this setting may report false positives.
        /// </summary>
        [EditorPref]
        public bool predictPotentialNullReferences { get; set; } = true;

        /// <summary>
        /// Whether predictive debugging should warn about missing components.
        /// Note that in some cases, this setting may report false positives.
        /// </summary>
        [EditorPref]
        public bool predictPotentialMissingComponents { get; set; } = true;

        /// <summary>
        /// Whether values should be shown on flow graph connections.
        /// </summary>
        [EditorPref]
        public bool showConnectionValues { get; set; } = true;

        /// <summary>
        /// Whether predictable values should be shown on flow graph connections.
        /// </summary>
        [EditorPref]
        public bool predictConnectionValues { get; set; } = false;

        /// <summary>
        /// Whether labels should be hidden on ports when the value can be deduced from the context.
        /// Disabling will make nodes more explicit but less compact.
        /// </summary>
        [EditorPref]
        public bool hidePortLabels { get; set; } = true;

        /// <summary>
        /// Whether active control connections should show a droplet animation.
        /// </summary>
        [EditorPref]
        public bool animateControlConnections { get; set; } = true;

        /// <summary>
        /// Whether active value connections should show a droplet animation.
        /// </summary>
        [EditorPref]
        public bool animateValueConnections { get; set; } = true;

        /// <summary>
        /// When active, right-clicking a flow graph will skip the context menu
        /// and instantly open the fuzzy finder. To open the context menu, hold shift.
        /// </summary>
        [EditorPref]
        public bool skipContextMenu { get; set; } = false;

        public const string FlowDebuggingTooltip =
        @"Controls whether the graph window highlights active nodes and displays live data values.

• Enabled: Displays all live data and highlights. This can cause a noticeable performance drop.
• Enabled When Visible: Only captures data while the Graph window is open. When the window is hidden, performance is more optimized, but early startup events (like OnStart) won't show data if the window was not open when they triggered.
• Disabled: Maximum performance. Value Connections will not show values, and nodes will not glow blue.

Note: This feature is entirely stripped out of standalone builds, ensuring zero impact on your final game's performance.";

        public const string RecursionSafetyTooltip = @"Enables whether the flow does extra checks to find infinite recursions. 
This avoids crashing if there is but the flow is much slower
• None: Recursion safety is turned off completely.
• Editor: Recursion safety inside the editor only (Recommended).
• Build: Recursion safety inside a build only.
• Editor/Build: Recursion safety inside the editor and builds.
";

        /// <summary>
        /// Determines whether flow execution information is displayed in the Graph window.
        /// </summary>
        /// <remarks>
        /// Enabling this feature introduces significant performance overhead but is highly useful for debugging. 
        /// When disabled, value connections will not display live data, and nodes will not turn blue when triggered.
        /// <para>
        /// If set to <c>EnabledWhenVisible</c>, flow data is only captured while the Graph window is open. 
        /// Any execution that occurs while the window is hidden will not be tracked or displayed. Values are commonly hidden with OnStart and similar Events when this setting is enabled.
        /// </para>
        /// <para><strong>Note:</strong> This feature is completely disabled in builds.</para>
        /// </remarks>
        [ProjectSetting(visible = false, resettable = true)]
        [InspectorLabel("Flow Debugging", FlowDebuggingTooltip)]
        public Flow.FlowDebuggingMode flowDebugging { get; set; } = Flow.FlowDebuggingMode.Enabled;

        [ProjectSetting(visible = false, resettable = true)]
        [InspectorLabel("Flow Recursion Safety", RecursionSafetyTooltip)]
        public Flow.FlowRecursionSafety flowRecursionSafety { get; set; } = Flow.FlowRecursionSafety.Editor;

        [ProjectSetting(visible = false, resettable = false)]
        public HashSet<string> favoriteUnitOptions { get; set; } = new HashSet<string>();

        public override void LateInitialize()
        {
            base.LateInitialize();

            Flow.debuggingMode = flowDebugging;

            bool enableEditorDefine = flowRecursionSafety == Flow.FlowRecursionSafety.Editor || flowRecursionSafety == Flow.FlowRecursionSafety.EditorAndBuild;

            bool enableBuildDefine = flowRecursionSafety == Flow.FlowRecursionSafety.Build || flowRecursionSafety == Flow.FlowRecursionSafety.EditorAndBuild;

            bool editorEnabled = ScriptingDefineUtility.IsDefineEnabled(ScriptingDefineUtility.EditorRecursionSymbol);
            bool buildEnabled = ScriptingDefineUtility.IsDefineEnabled(ScriptingDefineUtility.BuildRecursionSymbol);

            if (enableEditorDefine != editorEnabled)
            {
                ScriptingDefineUtility.SetDefine(ScriptingDefineUtility.EditorRecursionSymbol, enableEditorDefine);
            }

            if (enableBuildDefine != buildEnabled)
            {
                ScriptingDefineUtility.SetDefine(ScriptingDefineUtility.BuildRecursionSymbol, enableBuildDefine);
            }
        }
    }
}
