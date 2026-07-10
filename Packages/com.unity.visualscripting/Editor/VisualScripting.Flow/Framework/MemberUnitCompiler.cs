using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace Unity.VisualScripting
{
    public static class MemberUnitCompiler
    {
        private const string compilePath = "Assets/Unity.VisualScripting.Generated/VisualScripting.Flow/Generated Units";

        private const string BatchQueueKey = "VS_AutoReplace_BatchQueue";

        [Serializable]
        private class CompilePayload
        {
            public string unitGuid;
            public string typeName;
        }

        [Serializable]
        private class CompilePayloadBatch
        {
            public List<CompilePayload> items = new List<CompilePayload>();
        }

        public static void Compile(MemberUnit unit)
        {
            if (unit == null || unit.member == null) return;

            Member member = unit.member;

            member.EnsureReflected();

            string targetTypeName = member.targetType.Name;
            string memberName = member.name;
            string className = unit.compiledClassName;
            string unitTitle = "";

            if (unit is GetMember)
            {
                unitTitle = member.ToString() + " (Get)";
            }
            else if (unit is SetMember)
            {
                unitTitle = member.ToString() + " (Set)";
            }
            else
            {
                if (member.isConstructor)
                {
                    unitTitle = $"New {member.targetType.Name}";
                }
                else
                {
                    unitTitle = member.ToString();
                }
            }

            string safeMemberName = member.isConstructor ? "Constructor" : memberName.Replace(".", string.Empty);

            StringBuilder sb = new StringBuilder();

            sb.AppendLine("using System;");
            sb.AppendLine("using UnityEngine;");
            sb.AppendLine("using UnityEngine.Scripting;");

            if (member.isExtension)
            {
                var @namespace = member.info.DeclaringType.Namespace;
                if (!string.IsNullOrEmpty(@namespace) && @namespace != "System" && @namespace != "UnityEngine" && @namespace != "UnityEngine.Scripting")
                    sb.AppendLine($"using {@namespace};");
            }
            sb.AppendLine();
            sb.AppendLine($"namespace {MemberUnit.CompiledNamespace}");
            sb.AppendLine("{");
            sb.AppendLine($"    [UnitTitle(\"{unitTitle}\")]");
            sb.AppendLine($"    [UnitCategory(\"Optimized/{member.info.DeclaringType.Namespace.Replace(".", "/")}/{member.info.DeclaringType.CSharpName()}\")]");
            sb.AppendLine("    [Preserve]");
            sb.AppendLine($"    public sealed class {className.Replace(".", string.Empty)} : CompiledMemberUnit");
            sb.AppendLine("    {");

            sb.AppendLine("        [Preserve]");
            sb.AppendLine($"        public override Type PseudoDeclaringType {{ get; protected set; }} = typeof({GetTypeCSharpString(member.pseudoDeclaringType)});");
            sb.AppendLine();

            sb.AppendLine("        [Preserve]");
            sb.AppendLine($"        public override string Name {{ get; protected set; }} = \"{(member.isConstructor ? "new" : memberName)}\";");
            sb.AppendLine();

            sb.AppendLine("        [Preserve]");
            sb.AppendLine($"        public override string ActualName {{ get; protected set; }} = \"{memberName}\";");
            sb.AppendLine();

            sb.AppendLine("        [Preserve]");
            sb.AppendLine($"        public override string Summary {{ get; protected set; }} = @\"{member.info.Summary()}\";");
            sb.AppendLine();

            if (unit is SetMember)
            {
                sb.AppendLine("        [DoNotSerialize]");
                sb.AppendLine("        [PortLabelHidden]");
                sb.AppendLine("        [Preserve]");
                sb.AppendLine("        public ControlInput assign { get; private set; }");
                sb.AppendLine();
                sb.AppendLine("        [DoNotSerialize]");
                sb.AppendLine("        [PortLabelHidden]");
                sb.AppendLine("        [Preserve]");
                sb.AppendLine("        public ControlOutput assigned { get; private set; }");
                sb.AppendLine();
            }
            else if (unit is InvokeMember)
            {
                sb.AppendLine("        [Preserve]");
                if (member.parameterTypes.Length > 0)
                    sb.AppendLine($"        public override Type[] ParameterTypes => new Type[] {{ {string.Join(", ", member.parameterTypes.Select(t => $"typeof({GetTypeCSharpString(t)})"))} }};");
                else
                    sb.AppendLine($"        public override Type[] ParameterTypes => new Type[0];");

                sb.AppendLine();

                sb.AppendLine("        [DoNotSerialize]");
                sb.AppendLine("        [PortLabelHidden]");
                sb.AppendLine("        [Preserve]");
                sb.AppendLine("        public ControlInput enter { get; private set; }");
                sb.AppendLine();
                sb.AppendLine("        [DoNotSerialize]");
                sb.AppendLine("        [PortLabelHidden]");
                sb.AppendLine("        [Preserve]");
                sb.AppendLine("        public ControlOutput exit { get; private set; }");
                sb.AppendLine();
            }

            if (member.requiresTarget)
            {
                sb.AppendLine("        [DoNotSerialize]");
                if (IsNullMeansSelf(member.targetType))
                {
                    sb.AppendLine("        [NullMeansSelf]");
                }
                sb.AppendLine("        [PortLabelHidden]");
                sb.AppendLine("        [Preserve]");
                sb.AppendLine($"        public ValueInput target {{ get; private set; }}");
                sb.AppendLine();

                if (unit is SetMember || unit is InvokeMember)
                {
                    sb.AppendLine("        [Serialize]");
                    sb.AppendLine("        [Inspectable]");
                    sb.AppendLine("        [Preserve]");
                    sb.AppendLine("        public bool chainable;");
                    sb.AppendLine();
                    sb.AppendLine("        [DoNotSerialize]");
                    sb.AppendLine("        [PortLabel(\"Target\")]");
                    sb.AppendLine("        [PortLabelHidden]");
                    sb.AppendLine("        [Preserve]");
                    sb.AppendLine("        public ValueOutput targetOutput { get; private set; }");
                    sb.AppendLine();
                }
            }

            if (unit is GetMember)
            {
                sb.AppendLine("        [Preserve]");
                sb.AppendLine("        public override ActionDirection Direction { get; protected set; } = ActionDirection.Get;");
                sb.AppendLine();
                sb.AppendLine("        [Serialize]");
                sb.AppendLine("        [Inspectable]");
                sb.AppendLine("        [InspectorLabel(\"Cache result\", \"After the first execution cache the result instead of calling the member again\")]");
                sb.AppendLine("        [Preserve]");
                sb.AppendLine("        public bool CacheResult { get; set; }");
                sb.AppendLine();
                sb.AppendLine("        [DoNotSerialize]");
                sb.AppendLine("        [PortLabelHidden]");
                sb.AppendLine("        [Preserve]");
                sb.AppendLine("        public ValueOutput value { get; private set; }");
                sb.AppendLine();
            }
            else if (unit is SetMember)
            {
                sb.AppendLine("        [Preserve]");
                sb.AppendLine("        public override ActionDirection Direction { get; protected set; } = ActionDirection.Set;");
                sb.AppendLine();
                sb.AppendLine("        [DoNotSerialize]");
                sb.AppendLine("        [PortLabel(\"Value\")]");
                sb.AppendLine("        [PortLabelHidden]");
                sb.AppendLine("        [Preserve]");
                sb.AppendLine("        public ValueInput input { get; private set; }");
                sb.AppendLine();
                sb.AppendLine("        [DoNotSerialize]");
                sb.AppendLine("        [PortLabel(\"Value\")]");
                sb.AppendLine("        [PortLabelHidden]");
                sb.AppendLine("        [Preserve]");
                sb.AppendLine("        public ValueOutput output { get; private set; }");
                sb.AppendLine();
                sb.AppendLine("        [Preserve]");
                sb.AppendLine("        private bool _isTargetOutputConnected;");
                sb.AppendLine();
                sb.AppendLine("        [Preserve]");
                sb.AppendLine("        private bool _isOutputConnected;");
                sb.AppendLine();
            }
            else if (unit is InvokeMember)
            {
                sb.AppendLine("        [Preserve]");
                sb.AppendLine("        public override ActionDirection Direction { get; protected set; } = ActionDirection.Any;");
                sb.AppendLine();
                var parameters = member.GetParameterInfos().ToArray();
                foreach (var p in parameters)
                {
                    if (!p.IsOut)
                    {
                        sb.AppendLine("        [DoNotSerialize]");
                        sb.AppendLine("        [PortLabelHidden]");
                        sb.AppendLine("        [Preserve]");
                        sb.AppendLine($"        public ValueInput {p.Name} {{ get; private set; }}");
                        sb.AppendLine();
                    }
                    if (p.ParameterType.IsByRef || p.IsOut)
                    {
                        sb.AppendLine("        [DoNotSerialize]");
                        sb.AppendLine($"        [PortLabel(\"{p.Name}\")]");
                        sb.AppendLine("        [PortLabelHidden]");
                        sb.AppendLine("        [Preserve]");
                        sb.AppendLine($"        public ValueOutput {p.Name}Output {{ get; private set; }}");
                        sb.AppendLine();
                    }
                }

                if (member.type != typeof(void))
                {
                    sb.AppendLine("        [DoNotSerialize]");
                    sb.AppendLine("        [PortLabelHidden]");
                    sb.AppendLine("        [Preserve]");
                    sb.AppendLine("        public ValueOutput result { get; private set; }");
                    sb.AppendLine();
                }
            }

            sb.AppendLine("        [Preserve]");
            sb.AppendLine("        protected override void Definition()");
            sb.AppendLine("        {");

            if (unit is GetMember)
            {
                if (member.requiresTarget)
                {
                    string targetTypeStr = GetTypeCSharpString(member.targetType);
                    string nullMeansSelfStr = IsNullMeansSelf(member.targetType) ? ".NullMeansSelf()" : "";
                    sb.AppendLine($"            target = ValueInput<{targetTypeStr}>(nameof(target), default){nullMeansSelfStr};");
                }
                sb.AppendLine($"            value = ValueOutput(typeof({GetTypeCSharpString(member.type)}), nameof(value), Get{safeMemberName});");
                sb.AppendLine("            if (CacheResult)");
                sb.AppendLine("                value.CacheResult();");
                if (member.requiresTarget)
                {
                    sb.AppendLine("            Requirement(target, value);");
                }
            }
            else if (unit is SetMember)
            {
                sb.AppendLine($"            assign = ControlInput(nameof(assign), Set{safeMemberName});");
                sb.AppendLine("            assigned = ControlOutput(nameof(assigned));");
                if (member.requiresTarget)
                {
                    string targetTypeStr = GetTypeCSharpString(member.targetType);
                    string nullMeansSelfStr = IsNullMeansSelf(member.targetType) ? ".NullMeansSelf()" : "";
                    sb.AppendLine($"            target = ValueInput<{targetTypeStr}>(nameof(target), default){nullMeansSelfStr};");
                    sb.AppendLine("            if (chainable)");
                    sb.AppendLine($"            targetOutput = ValueOutput<{targetTypeStr}>(nameof(targetOutput));");
                    sb.AppendLine("            Requirement(target, assign);");
                }
                string valueTypeStr = GetTypeCSharpString(member.type);
                sb.AppendLine($"            input = ValueInput<{valueTypeStr}>(nameof(input), default);");
                sb.AppendLine($"            output = ValueOutput<{valueTypeStr}>(nameof(output));");
                sb.AppendLine();
                sb.AppendLine("            Succession(assign, assigned);");
                if (member.requiresTarget)
                {
                    sb.AppendLine("            Requirement(target, output);");
                    sb.AppendLine("            if (chainable)");
                    sb.AppendLine("                Assignment(assign, targetOutput);");
                }
                sb.AppendLine("            Assignment(assign, output);");
            }
            else if (unit is InvokeMember)
            {
                sb.AppendLine($"            enter = ControlInput(nameof(enter), Assign{safeMemberName});");
                sb.AppendLine("            exit = ControlOutput(nameof(exit));");
                if (member.requiresTarget)
                {
                    string targetTypeStr = GetTypeCSharpString(member.targetType);
                    string nullMeansSelfStr = IsNullMeansSelf(member.targetType) ? ".NullMeansSelf()" : "";
                    sb.AppendLine($"            target = ValueInput<{targetTypeStr}>(nameof(target), default){nullMeansSelfStr};");
                    sb.AppendLine("            if (chainable)");
                    sb.AppendLine($"                targetOutput = ValueOutput<{targetTypeStr}>(nameof(targetOutput));");
                    sb.AppendLine("            Requirement(target, enter);");
                }

                var parameters = member.GetParameterInfos().ToArray();
                foreach (var p in parameters)
                {
                    string pTypeStr = GetTypeCSharpString(p.ParameterType.IsByRef ? p.ParameterType.GetElementType() : p.ParameterType);
                    if (!p.HasOutModifier())
                    {
                        string allowsNullStr = p.AllowsNull() ? ".AllowsNull()" : "";
                        sb.AppendLine($"            {p.Name} = ValueInput<{pTypeStr}>($\"%{p.Name}\", default){allowsNullStr};");
                    }
                    if (p.ParameterType.IsByRef || p.IsOut)
                    {
                        sb.AppendLine($"            {p.Name}Output = ValueOutput<{pTypeStr}>($\"&{p.Name}\");");
                    }
                }

                if (member.type != typeof(void))
                {
                    sb.AppendLine($"            result = ValueOutput(typeof({GetTypeCSharpString(member.type)}), nameof(result), Get{safeMemberName});");
                }

                sb.AppendLine();
                sb.AppendLine("            Succession(enter, exit);");
                if (member.requiresTarget)
                {
                    sb.AppendLine("            if (chainable)");
                    sb.AppendLine("                Assignment(enter, targetOutput);");
                }
            }

            sb.AppendLine("        }");
            sb.AppendLine();

            if (unit is SetMember)
            {
                sb.AppendLine("        [Preserve]");
                sb.AppendLine("        public override void AfterAdd()");
                sb.AppendLine("        {");
                sb.AppendLine("            base.AfterAdd();");
                sb.AppendLine("            _isTargetOutputConnected = chainable && targetOutput.hasValidConnection;");
                sb.AppendLine("            _isOutputConnected = output.hasValidConnection;");
                sb.AppendLine("        }");
                sb.AppendLine();
            }

            if (unit is GetMember)
            {
                sb.AppendLine("        [Preserve]");
                sb.AppendLine($"        private ParameterValue Get{safeMemberName}(Flow flow)");
                sb.AppendLine("        {");
                if (member.requiresTarget)
                {
                    sb.AppendLine($"            var instance = flow{GetValueString(member.targetType, "this.target")};");
                    sb.AppendLine($"            return new ParameterValue({GetValidParameterValueTypeCast(member.type)}instance.{memberName});");
                }
                else
                {
                    string targetTypeStr = GetTypeCSharpString(member.targetType);
                    sb.AppendLine($"            return new ParameterValue({GetValidParameterValueTypeCast(member.type)}{targetTypeStr}.{memberName});");
                }
                sb.AppendLine("        }");
            }
            else if (unit is SetMember)
            {
                sb.AppendLine("        [Preserve]");
                sb.AppendLine($"        private ControlOutput Set{safeMemberName}(Flow flow)");
                sb.AppendLine("        {");
                if (member.requiresTarget)
                {
                    sb.AppendLine($"            var instance = flow{GetValueString(member.targetType, "this.target")};");
                    sb.AppendLine($"            var input = flow{GetValueString(member.type, "this.input")};");
                    sb.AppendLine($"            instance.{memberName} = input;");

                    sb.AppendLine();

                    sb.AppendLine("            if (_isTargetOutputConnected)");
                    sb.AppendLine("                flow.SetValue(targetOutput, instance);");

                    sb.AppendLine();
                }
                else
                {
                    string targetTypeStr = GetTypeCSharpString(member.targetType);
                    sb.AppendLine($"            var input = flow{GetValueString(member.type, "this.input")};");
                    sb.AppendLine($"            {targetTypeStr}.{memberName} = input;");
                }
                sb.AppendLine("            if (_isOutputConnected)");
                sb.AppendLine("                flow.SetValue(output, input);");
                sb.AppendLine();
                sb.AppendLine("            return assigned;");
                sb.AppendLine("        }");
            }
            else if (unit is InvokeMember)
            {
                var parameters = member.GetParameterInfos().ToArray();
                string targetDeclaration = "";
                string invocationTarget = GetTypeCSharpString(member.targetType);
                if (member.requiresTarget)
                {
                    targetDeclaration = $"            var instance = flow{GetValueString(member.targetType, "this.target")};\r\n";
                    invocationTarget = "instance";
                }

                StringBuilder fetchInputsSB = new StringBuilder();
                foreach (var p in parameters)
                {
                    var pType = p.ParameterType.IsByRef ? p.ParameterType.GetElementType() : p.ParameterType;
                    string pTypeStr = GetTypeCSharpString(pType);
                    if (!p.IsOut)
                    {
                        fetchInputsSB.AppendLine($"            var local_{p.Name} = flow{GetValueString(pType, p.Name)};");
                    }
                    else
                    {
                        fetchInputsSB.AppendLine($"            {pTypeStr} local_{p.Name};");
                    }
                }

                string[] argNames = new string[parameters.Length];
                for (int i = 0; i < parameters.Length; i++)
                {
                    var p = parameters[i];
                    string prefix = "";
                    if (p.ParameterType.IsByRef)
                    {
                        prefix = p.IsOut ? "out " : "ref ";
                    }
                    argNames[i] = $"{prefix}local_{p.Name}";
                }
                string argumentsStr = string.Join(", ", argNames);

                StringBuilder setOutputsSB = new StringBuilder();
                foreach (var p in parameters)
                {
                    if (p.ParameterType.IsByRef || p.IsOut)
                    {
                        setOutputsSB.AppendLine($"            flow.SetValue({p.Name}Output, local_{p.Name});");
                    }
                }

                sb.AppendLine("        [Preserve]");
                sb.AppendLine($"        private ControlOutput Assign{safeMemberName}(Flow flow)");
                sb.AppendLine("        {");
                if (member.requiresTarget) sb.Append(targetDeclaration);
                sb.Append(fetchInputsSB.ToString());

                if (member.isConstructor)
                {
                    sb.AppendLine($"            var value = new {invocationTarget}({argumentsStr});");
                    sb.Append(setOutputsSB.ToString());
                    sb.AppendLine("            flow.SetValue(result, value);");
                }
                else if (member.type == typeof(void))
                {
                    sb.AppendLine($"            {invocationTarget}.{memberName}({argumentsStr});");
                    sb.Append(setOutputsSB.ToString());
                }
                else
                {
                    sb.AppendLine($"            var value = {invocationTarget}.{memberName}({argumentsStr});");
                    sb.Append(setOutputsSB.ToString());
                    sb.AppendLine("            flow.SetValue(result, value);");
                }

                if (member.requiresTarget)
                {
                    sb.AppendLine("            if (chainable)");
                    sb.AppendLine("                flow.SetValue(targetOutput, instance);");
                }

                sb.AppendLine("            return exit;");
                sb.AppendLine("        }");

                if (member.type != typeof(void))
                {
                    sb.AppendLine();
                    sb.AppendLine("        [Preserve]");
                    sb.AppendLine($"        private ParameterValue Get{safeMemberName}(Flow flow)");
                    sb.AppendLine("        {");
                    if (member.requiresTarget) sb.Append(targetDeclaration);
                    sb.Append(fetchInputsSB.ToString());

                    if (member.isConstructor)
                    {
                        sb.AppendLine($"            var value = new {invocationTarget}({argumentsStr});");
                    }
                    else
                    {
                        sb.AppendLine($"            var value = {invocationTarget}.{memberName}({argumentsStr});");
                    }

                    sb.Append(setOutputsSB.ToString());
                    sb.AppendLine($"            return new ParameterValue({GetValidParameterValueTypeCast(member.type)}value);");
                    sb.AppendLine("        }");
                }
            }

            sb.AppendLine();

            sb.AppendLine("        [Preserve]");
            sb.AppendLine("        protected override void Copy(MemberUnit source)");
            sb.AppendLine("        {");
            if (unit is GetMember)
            {
                sb.AppendLine("            if (source is GetMember getMember)");
                sb.AppendLine("                this.CacheResult = getMember.CacheResult;");
            }
            else if (member.requiresTarget)
            {
                if (unit is InvokeMember)
                {
                    sb.AppendLine("            if (source is InvokeMember invokeMember)");
                    sb.AppendLine("                this.chainable = invokeMember.chainable;");
                }
                else if (unit is SetMember)
                {
                    sb.AppendLine("            if (source is SetMember setMember)");
                    sb.AppendLine("                this.chainable = setMember.chainable;");
                }
            }

            sb.AppendLine("        }");

            sb.AppendLine();

            sb.AppendLine("        [Preserve]");
            sb.AppendLine("        public override void CopyTo(MemberUnit source)");
            sb.AppendLine("        {");
            if (unit is GetMember)
            {
                sb.AppendLine("            if (source is GetMember getMember)");
                sb.AppendLine("                getMember.CacheResult = this.CacheResult;");
            }
            else if (member.requiresTarget)
            {
                if (unit is InvokeMember)
                {
                    sb.AppendLine("            if (source is InvokeMember invokeMember)");
                    sb.AppendLine("                invokeMember.chainable = this.chainable;");
                }
                else if (unit is SetMember)
                {
                    sb.AppendLine("            if (source is SetMember setMember)");
                    sb.AppendLine("                setMember.chainable = this.chainable;");
                }
            }

            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine("}");

            if (!string.IsNullOrEmpty(compilePath) && !Directory.Exists(compilePath))
            {
                Directory.CreateDirectory(compilePath);
            }

            string fullPath = Path.Combine(compilePath, $"{className}.cs");
            File.WriteAllText(fullPath, sb.ToString());

#if UNITY_EDITOR
            UnityEditor.AssetDatabase.Refresh();
#endif
        }

        public static void CompileAndReplaceBatch(IEnumerable<MemberUnit> memberUnits)
        {
            if (memberUnits == null) throw new ArgumentNullException(nameof(memberUnits));

            var context = GraphWindow.activeContext ?? throw new InvalidOperationException("[MemberUnitCompiler] No active graph to replace units in!");
            if (!(context.graph is FlowGraph)) throw new InvalidOperationException("[MemberUnitCompiler] Active graph is not a FlowGraph!");

            CompilePayloadBatch batch = new CompilePayloadBatch();
            if (EditorPrefs.HasKey(BatchQueueKey))
            {
                string existingJson = EditorPrefs.GetString(BatchQueueKey);
                try { batch = JsonUtility.FromJson<CompilePayloadBatch>(existingJson) ?? new CompilePayloadBatch(); } catch { }
            }

            int compileCount = 0;

            foreach (var memberUnit in memberUnits)
            {
                if (memberUnit == null) continue;
                if (!context.graph.elements.Contains(memberUnit)) continue;

                batch.items.Add(new CompilePayload
                {
                    unitGuid = memberUnit.guid.ToString(),
                    typeName = memberUnit.CompiledTypeName
                });

                Compile(memberUnit);
                compileCount++;
            }

            if (compileCount > 0)
            {
                string jsonOutput = JsonUtility.ToJson(batch);
                EditorPrefs.SetString(BatchQueueKey, jsonOutput);

                AssetDatabase.Refresh();
            }
        }

        public static void CompileAndReplace(MemberUnit memberUnit)
        {
            CompileAndReplaceBatch(new[] { memberUnit });
        }

        private static bool IsNullMeansSelf(Type type)
        {
            return ComponentHolderProtocol.IsComponentHolderType(type);
        }

        private static string GetTypeCSharpString(Type type)
        {
            return type.CSharpFullName();
        }

        private static string GetValueString(Type type, string Port)
        {
            if (IsValidParameterValueFieldType(type))
                return $".GetValueData({Port}).{GetExplicitConversionMethod(type)}";
            return $".GetValue<{GetTypeCSharpString(type)}>({Port})";
        }

        private static bool IsValidParameterValueFieldType(Type type)
        {
            var valueType = ParameterValue.GetParameterValueType(type);

            if (valueType != ParameterValue.ValueType.None && valueType != ParameterValue.ValueType.Object)
                return true;
            return false;
        }

        private static string GetValidParameterValueTypeCast(Type type)
        {
            if (IsValidParameterValueFieldType(type))
                return "";

            if (typeof(UnityEngine.Object).IsAssignableFrom(type))
                return "";

            return $"({typeof(object).CSharpFullName()})";
        }

        private static string GetExplicitConversionMethod(Type type)
        {
            if (type == typeof(byte)) return "ToByte()";
            if (type == typeof(sbyte)) return "ToSByte()";
            if (type == typeof(short)) return "ToInt16()";
            if (type == typeof(ushort)) return "ToUInt16()";
            if (type == typeof(int)) return "ToInt32()";
            if (type == typeof(uint)) return "ToUInt32()";
            if (type == typeof(long)) return "ToInt64()";
            if (type == typeof(ulong)) return "ToUInt64()";
            if (type == typeof(float)) return "ToSingle()";
            if (type == typeof(double)) return "ToDouble()";
            if (type == typeof(bool)) return "ToBool()";
            if (type == typeof(string)) return "ToString()";

            if (type == typeof(Vector2)) return "ToVector2()";
            if (type == typeof(Vector3)) return "ToVector3()";
            if (type == typeof(Vector4)) return "ToVector4()";
            if (type == typeof(Color)) return "ToColor()";
            if (type == typeof(Quaternion)) return "ToQuaternion()";

            throw new ArgumentException($"Unsupported explicit conversion type: {type?.FullName}");
        }

        [DidReloadScripts]
        private static void OnScriptsCompiled()
        {
            if (!EditorPrefs.HasKey(BatchQueueKey)) return;

            string jsonQueue = EditorPrefs.GetString(BatchQueueKey);
            EditorPrefs.DeleteKey(BatchQueueKey);

            if (string.IsNullOrEmpty(jsonQueue)) return;

            CompilePayloadBatch batch;
            try
            {
                batch = JsonUtility.FromJson<CompilePayloadBatch>(jsonQueue);
                if (batch == null || batch.items == null || batch.items.Count == 0) return;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MemberUnitCompiler] Failed to parse batch: {ex.Message}");
                return;
            }

            IGraphContext context = GraphWindow.activeContext;
            if (context == null || context.graph == null || !(context.graph is FlowGraph flowGraph))
            {
                Debug.LogWarning("[MemberUnitCompiler] Auto-replace skipped: The active FlowGraph editor context is no longer open.");
                return;
            }

            EditorApplication.delayCall += () =>
            {
                foreach (var payload in batch.items)
                {
                    Type compiledType = Type.GetType(payload.typeName);
                    if (compiledType == null)
                    {
                        Debug.LogError($"[MemberUnitCompiler] Dynamic lookup failed for type '{payload.typeName}'");
                        continue;
                    }

                    Guid unitGuid = new Guid(payload.unitGuid);
                    var targetUnit = flowGraph.units.OfType<MemberUnit>().FirstOrDefault(u => u.guid == unitGuid);

                    if (targetUnit != null)
                    {
                        try
                        {
                            UnitWidgetHelper.ReplaceMemberUnitUnit(targetUnit, compiledType, context, null);
                        }
                        catch (Exception ex)
                        {
                            Debug.LogError($"[MemberUnitCompiler] Node replacement exception on '{payload.typeName}': {ex.Message}");
                        }
                    }
                    else
                    {
                        Debug.LogWarning(
                            $"[MemberUnitCompiler] Compilation finished but target unit was not found! Skipping Unit replace for {compiledType.CSharpName()}\n" +
                            $"<b>Target GUID:</b> {payload.unitGuid}\n" +
                            $"<b>Target Type:</b> <color=#9cdcfe>{payload.typeName}</color>\n" +
                            $"<i>Possible Reasons:</i>\n" +
                            $"- The unit was deleted or replaced, before compilation finished.\n" +
                            $"- The active graph context changed while the background compilation loop was running. Ensure you do not change the Graph while compiling."
                        );
                    }
                }
            };
        }
    }
}