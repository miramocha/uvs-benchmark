using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Unity.VisualScripting
{
    [Editor(typeof(FlowGraph))]
    public class FlowGraphEditor : GraphEditor
    {
        public FlowGraphEditor(Metadata metadata) : base(metadata)
        {
            controlInputDefinitionsMetadata = metadata[nameof(FlowGraph.controlInputDefinitions)];
            controlOutputDefinitionsMetadata = metadata[nameof(FlowGraph.controlOutputDefinitions)];
            valueInputDefinitionsMetadata = metadata[nameof(FlowGraph.valueInputDefinitions)];
            valueOutputDefinitionsMetadata = metadata[nameof(FlowGraph.valueOutputDefinitions)];
        }

        private new FlowGraph graph => (FlowGraph)base.graph;

        private readonly Metadata controlInputDefinitionsMetadata;
        private readonly Metadata controlOutputDefinitionsMetadata;
        private readonly Metadata valueInputDefinitionsMetadata;
        private readonly Metadata valueOutputDefinitionsMetadata;

        private IEnumerable<Warning> warnings => UnitPortDefinitionUtility.Warnings((FlowGraph)metadata.value);

        protected override float GetHeight(float width, GUIContent label)
        {
            var height = 0f;

            height += GetHeaderHeight(width);

            height += GetControlInputDefinitionsHeight(width);

            height += EditorGUIUtility.standardVerticalSpacing;

            height += GetControlOutputDefinitionsHeight(width);

            height += EditorGUIUtility.standardVerticalSpacing;

            height += GetValueInputDefinitionsHeight(width);

            height += EditorGUIUtility.standardVerticalSpacing;

            height += GetValueOutputDefinitionsHeight(width);

            if (warnings.Any())
            {
                height += EditorGUIUtility.standardVerticalSpacing;

                foreach (var warning in warnings)
                {
                    height += warning.GetHeight(width);
                }
            }

            return height;
        }

        protected override void OnGUI(Rect position, GUIContent label)
        {
            BeginLabeledBlock(metadata, position, label);

            OnHeaderGUI(position);

            EditorGUI.BeginChangeCheck();

            LudiqGUI.Inspector(controlInputDefinitionsMetadata, position.VerticalSection(ref y, GetControlInputDefinitionsHeight(position.width)));

            y += EditorGUIUtility.standardVerticalSpacing;

            LudiqGUI.Inspector(controlOutputDefinitionsMetadata, position.VerticalSection(ref y, GetControlOutputDefinitionsHeight(position.width)));

            y += EditorGUIUtility.standardVerticalSpacing;

            LudiqGUI.Inspector(valueInputDefinitionsMetadata, position.VerticalSection(ref y, GetValueInputDefinitionsHeight(position.width)));

            y += EditorGUIUtility.standardVerticalSpacing;

            LudiqGUI.Inspector(valueOutputDefinitionsMetadata, position.VerticalSection(ref y, GetValueOutputDefinitionsHeight(position.width)));

            if (EditorGUI.EndChangeCheck())
            {
                graph.PortDefinitionsChanged();
            }

            if (warnings.Any())
            {
                y += EditorGUIUtility.standardVerticalSpacing;

                foreach (var warning in warnings)
                {
                    y--;
                    warning.OnGUI(position.VerticalSection(ref y, warning.GetHeight(position.width) + 1));
                }
            }

            EndBlock(metadata);
        }

        private float GetControlInputDefinitionsHeight(float width)
        {
            return LudiqGUI.GetInspectorHeight(this, controlInputDefinitionsMetadata, width);
        }

        private float GetControlOutputDefinitionsHeight(float width)
        {
            return LudiqGUI.GetInspectorHeight(this, controlOutputDefinitionsMetadata, width);
        }

        private float GetValueInputDefinitionsHeight(float width)
        {
            return LudiqGUI.GetInspectorHeight(this, valueInputDefinitionsMetadata, width);
        }

        private float GetValueOutputDefinitionsHeight(float width)
        {
            return LudiqGUI.GetInspectorHeight(this, valueOutputDefinitionsMetadata, width);
        }
    }
}
