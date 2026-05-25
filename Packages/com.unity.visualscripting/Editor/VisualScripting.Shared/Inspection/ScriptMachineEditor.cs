using Unity.VisualScripting;
using Unity.VisualScripting.Community;
using Unity.VisualScripting.Community.CSharp;
using UnityEngine;

namespace Unity.VisualScripting
{
    [Editor(typeof(ScriptMachine))]
    public class ScriptMachineEditor : EventMachineEditor
    {
        public ScriptMachineEditor(Metadata metadata) : base(metadata)
        {
        }

        protected override void Compile()
        {
            var machine = metadata.value as ScriptMachine;
            CodeGeneratorValueUtility.currentAsset = machine;
            AssetCompiler.CompileAsset(machine);
        }
    }
}
