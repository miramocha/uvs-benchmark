using System;
using UnityEditor;
using UnityEngine;

namespace Unity.VisualScripting
{
    [Editor(typeof(CompiledMemberUnit))]
    public class CompiledMemberUnitEditor : UnitEditor
    {
        private const float revertButtonHeight = 20f;

        public CompiledMemberUnitEditor(Metadata metadata) : base(metadata) { }

        protected override float GetInspectorHeight(float width)
        {
            return base.GetInspectorHeight(width) + revertButtonHeight;
        }

        protected override void OnInspectorGUI(Rect position)
        {
            base.OnInspectorGUI(position);

            position.y += base.GetInspectorHeight(position.width);
            position.height = revertButtonHeight;

            var unit = (CompiledMemberUnit)metadata.value;
            var member = unit.Member;

            if (member != null)
            {
                if (GUI.Button(position, "Switch to Reflection Node"))
                {
                    var currentSelection = this.selection;
                    var currentContext = this.context;
                    EditorApplication.delayCall += () => UnitWidgetHelper.ReplaceCompiledMemberUnitUnit(unit, member, currentContext, currentSelection);
                }
            }
        }
    }
}
