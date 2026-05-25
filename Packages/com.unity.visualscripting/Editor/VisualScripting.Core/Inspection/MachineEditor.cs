using UnityEditor;
using UnityEngine;

namespace Unity.VisualScripting
{
    [Editor(typeof(IMachine))]
    public class MachineEditor : Inspector
    {
        public MachineEditor(Metadata metadata) : base(metadata) { }

        private Metadata useCompiledGraphMetadata
        {
            get
            {
                var meta = metadata[nameof(IMachine.UseCompiledGraph)];
                meta.isEditable = !Application.isPlaying;
                return meta;
            }
        }

        private Metadata nestMetadata => metadata[nameof(IMachine.nest)];

        private Metadata graphMetadata => nestMetadata[nameof(IGraphNest.graph)];

        protected Metadata headerTitleMetadata => graphMetadata[nameof(IGraph.title)];

        protected Metadata headerSummaryMetadata => graphMetadata[nameof(IGraph.summary)];

        protected virtual bool showHeader => graphMetadata.value != null;

        protected virtual bool showConfiguration => false;

        protected sealed override float GetHeight(float width, GUIContent label)
        {
            var height = 0f;

            if (showHeader)
            {
                height += GetHeaderHeight(width);
            }

            height += EditorGUIUtility.singleLineHeight;

            height += GetNestHeight(width);

            if (showConfiguration)
            {
                height += GetConfigurationHeight(width);
            }

            return height;
        }

        protected sealed override void OnGUI(Rect position, GUIContent label)
        {
            position = BeginLabeledBlock(metadata, position, GUIContent.none);

            if (showHeader)
            {
                var headerPosition = position;
                headerPosition.x = 0;
                headerPosition.width = LudiqGUIUtility.currentInspectorWidthWithoutScrollbar;
                OnHeaderGUI(headerPosition);
            }

            var compiledPosition = position.VerticalSection(ref y, EditorGUIUtility.singleLineHeight);
            EditorGUI.BeginDisabledGroup(!useCompiledGraphMetadata.isEditable);

            // 1. Split the rect: 'left' for the Toggle, 'right' for the Button
            // We give the button a fixed width of 80px
            var buttonWidth = 80f;
            var spacing = 5f;
            var leftWidth = compiledPosition.width - buttonWidth - spacing;
            var rightWidth = buttonWidth + spacing;
            var toggleRect = new Rect(compiledPosition.x, compiledPosition.y, leftWidth, compiledPosition.height);
            var buttonRect = new Rect(compiledPosition.x + leftWidth, compiledPosition.y, rightWidth, compiledPosition.height);
            buttonRect.width -= spacing; // Adjust for the gap
            buttonRect.x += spacing;

            // 2. Draw the Toggle
            var wasCompiled = (bool)useCompiledGraphMetadata.value;
            LudiqGUI.Inspector(useCompiledGraphMetadata, toggleRect);
            var isCompiled = (bool)useCompiledGraphMetadata.value;

            // Trigger compile if toggled ON
            if (wasCompiled != isCompiled && isCompiled)
            {
                Compile();
            }

            // 3. Draw the Recompile Button
            // Only show/enable the button if we are actually using the compiled graph
            EditorGUI.BeginDisabledGroup(!isCompiled);
            if (GUI.Button(buttonRect, "Recompile", EditorStyles.miniButton))
            {
                Compile();
            }
            EditorGUI.EndDisabledGroup();

            EditorGUI.EndDisabledGroup();

            var nestPosition = position.VerticalSection(ref y, LudiqGUI.GetEditorHeight(this, nestMetadata, position.width));
            OnNestGUI(nestPosition);

            if (showConfiguration)
            {
                OnConfigurationGUI(position);
            }

            EndBlock(metadata);
        }

        protected virtual float GetHeaderHeight(float width)
        {
            return LudiqGUI.GetHeaderHeight(this, headerTitleMetadata, headerSummaryMetadata, null, LudiqGUIUtility.currentInspectorWidthWithoutScrollbar);
        }

        protected virtual void OnHeaderGUI(Rect headerPosition)
        {
            LudiqGUI.OnHeaderGUI(headerTitleMetadata, headerSummaryMetadata, null, headerPosition, ref y);
        }

        protected virtual float GetNestHeight(float width)
        {
            return LudiqGUI.GetEditorHeight(this, nestMetadata, width);
        }

        protected virtual void OnNestGUI(Rect nestPosition)
        {
            LudiqGUI.Editor(nestMetadata, nestPosition);
        }

        protected virtual float GetConfigurationHeight(float width)
        {
            return 0;
        }

        protected virtual void OnConfigurationGUI(Rect position)
        {
        }

        protected virtual void Compile() { }
    }
}
