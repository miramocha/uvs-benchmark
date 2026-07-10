using System;
using UnityEditor;
using UnityEngine;

namespace Unity.VisualScripting
{
    [Editor(typeof(MemberUnit))]
    public class MemberUnitEditor : UnitEditor
    {
        private const float compileButtonHeight = 20f;

        public MemberUnitEditor(Metadata metadata) : base(metadata) { }

        protected override float GetInspectorHeight(float width)
        {
            return base.GetInspectorHeight(width) + compileButtonHeight;
        }

        protected override void OnInspectorGUI(Rect position)
        {
            base.OnInspectorGUI(position);

            position.y += base.GetInspectorHeight(position.width);
            position.height = compileButtonHeight;

            var unit = (MemberUnit)metadata.value;
            var type = Type.GetType(unit.CompiledTypeName, false);

            if (type != null)
            {
                if (GUI.Button(position, "Switch to Compiled Node"))
                {
                    var currentSelection = this.selection;
                    var currentContext = this.context;
                    EditorApplication.delayCall += () => UnitWidgetHelper.ReplaceMemberUnitUnit(unit, type, currentContext, currentSelection);
                }
                return;
            }

            if (GUI.Button(position, "Compile & Auto-Replace"))
            {
                MemberUnitCompiler.CompileAndReplace(unit);
            }
        }
    }
}
