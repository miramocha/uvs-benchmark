using System;
using System.IO;
using System.Linq;
using Unity.VisualScripting.Community.Libraries.CSharp;
using Unity.VisualScripting.Community.Libraries.Humility;
using UnityEditor;
using UnityEngine;
using SMachine = Unity.VisualScripting.ScriptMachine;

namespace Unity.VisualScripting.Community.CSharp
{
    public class ScriptMachineCompiler : BaseCompiler
    {
        protected override string GetFilePath(UnityEngine.Object asset, PathConfig paths)
        {
            var machine = (SMachine)asset;
            return Path.Combine(paths.ObjectsPath, GetMachineName(machine).LegalMemberName() + ".cs");
        }

        protected override string GenerateCode(UnityEngine.Object asset)
        {
            var machine = (SMachine)asset;
            var generator = (GameObjectGenerator)GameObjectGenerator.GetSingleDecorator(machine.gameObject);
            generator.current = machine;
            var code = generator.GenerateClean(new CodeWriter(), generator.GetGenerationData());
            return code;
        }

        protected override string GetRelativeFilePath(UnityEngine.Object asset, PathConfig paths)
        {
            var machine = (SMachine)asset;
            return Path.Combine(paths.ObjectsRelativePath, GetMachineName(machine).LegalMemberName() + ".cs");
        }

        public override void PostProcess(UnityEngine.Object asset, PathConfig paths, Type type)
        {
            if (!(asset is SMachine machine) || type == null) return;

            machine.compiledType = type;
            machine.compiledReferences.Clear();
            machine.compiledReferenceNames.Clear();

            var values = CodeGeneratorValueUtility.GetAllValues(machine, false);

            var objects = machine.graph.variables
                .Where(v => typeof(UnityEngine.Object).IsAssignableFrom(v.value?.GetType()))
                .Select(v => (v.name, (UnityEngine.Object)v.value))
                .Concat(values.Select(v => (v.Key.LegalMemberName(), v.Value)))
                .ToArray();

            foreach (var (name, obj) in objects)
            {
                var field = type.GetFields().FirstOrDefault(f => f.Name == name);
                if (field != null)
                {
                    machine.compiledReferenceNames.Add(name);
                    machine.compiledReferences.Add(obj);
                }
            }

            EditorUtility.SetDirty(machine);
        }

        private string GetMachineName(SMachine machine)
        {
            return machine.nest?.graph.title?.Length > 0
                ? machine.nest.graph.title.LegalMemberName()
                : machine.gameObject.name.Capitalize().First().Letter() + $"_{typeof(SMachine).Name}_" + Array.IndexOf(machine.gameObject.GetComponents<SMachine>(), machine);
        }
    }
}