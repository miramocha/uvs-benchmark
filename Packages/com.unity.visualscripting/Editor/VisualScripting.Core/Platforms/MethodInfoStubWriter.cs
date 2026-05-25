using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Unity.VisualScripting
{
    [AotStubWriter(typeof(MethodInfo))]
    public class MethodInfoStubWriter : MethodBaseStubWriter<MethodInfo>
    {
        public MethodInfoStubWriter(MethodInfo methodInfo) : base(methodInfo) { }

        public override IEnumerable<CodeStatement> GetStubStatements()
        {
            var targetType = new CodeTypeReference(manipulator.targetType, CodeTypeReferenceOptions.GlobalReference);
            var declaringType = new CodeTypeReference(stub.DeclaringType, CodeTypeReferenceOptions.GlobalReference);
            var pvType = new CodeTypeReference(typeof(ParameterValue), CodeTypeReferenceOptions.GlobalReference);
            var pvTypeExpression = new CodeTypeReferenceExpression(pvType);
            var objectType = new CodeTypeReference(typeof(object), CodeTypeReferenceOptions.GlobalReference);

            // 1. Create a target expression
            CodeExpression targetValue;
            CodeExpression targetReference;

            if (manipulator.requiresTarget && !manipulator.isExtension)
            {
                targetValue = new CodeDefaultValueExpression(targetType);
                yield return new CodeVariableDeclarationStatement(targetType, "target", targetValue);
                targetReference = new CodeVariableReferenceExpression("target");
            }
            else
            {
                targetValue = new CodePrimitiveExpression(null);
                targetReference = manipulator.isExtension
                    ? (CodeExpression)new CodeTypeReferenceExpression(declaringType)
                    : new CodeTypeReferenceExpression(targetType);
            }

            var methodReference = new CodeMethodReferenceExpression(targetReference, manipulator.name);
            var arguments = new List<CodeExpression>();
            var includesOutOrRef = false;

            foreach (var parameterInfo in stub.GetParameters())
            {
                var parameterType = new CodeTypeReference(parameterInfo.UnderlyingParameterType(), CodeTypeReferenceOptions.GlobalReference);
                var argumentName = $"arg{arguments.Count}";

                yield return new CodeVariableDeclarationStatement(parameterType, argumentName, new CodeDefaultValueExpression(parameterType));

                FieldDirection direction = FieldDirection.In;
                if (parameterInfo.HasOutModifier()) { direction = FieldDirection.Out; includesOutOrRef = true; }
                else if (parameterInfo.ParameterType.IsByRef && !parameterInfo.IsIn) { direction = FieldDirection.Ref; includesOutOrRef = true; }

                arguments.Add(new CodeDirectionExpression(direction, new CodeVariableReferenceExpression(argumentName)));
            }

            // 2. Call its method
            if (UnaryOperators.TryGetValue(manipulator.name, out var unarySymbol))
            {
                var argRef = (CodeDirectionExpression)arguments[0];
                var argName = ((CodeVariableReferenceExpression)argRef.Expression).VariableName;
                var snippet = new CodeSnippetExpression($"{unarySymbol}{argName}");
                yield return new CodeVariableDeclarationStatement(manipulator.type, "op", snippet);
            }
            else if (operatorTypes.TryGetValue(manipulator.name, out var binaryOp))
            {
                var operation = new CodeBinaryOperatorExpression(arguments[0], binaryOp, arguments[1]);
                yield return new CodeVariableDeclarationStatement(manipulator.type, "op", operation);
            }
            else if (manipulator.isConversion)
            {
                yield return new CodeVariableDeclarationStatement(manipulator.type, "conversion", new CodeCastExpression(manipulator.type, arguments[0]));
            }
            else if (manipulator.isPubliclyInvocable)
            {
                yield return new CodeExpressionStatement(new CodeMethodInvokeExpression(methodReference, arguments.ToArray()));
            }

            // 3. Optimized Invoker Setup
            var optimzedInvoker = stub.Prewarm();
            var optimizedInvokerType = new CodeTypeReference(optimzedInvoker.GetType(), CodeTypeReferenceOptions.GlobalReference);

            var constructorArgs = new List<CodeExpression>
            {
                new CodeDefaultValueExpression(new CodeTypeReference(typeof(MethodInfo), CodeTypeReferenceOptions.GlobalReference))
            };

            if (optimzedInvoker is ReflectionInvoker)
            {
                constructorArgs.Add(new CodeFieldReferenceExpression(
                    new CodeTypeReferenceExpression(typeof(DelegateCompatiblity)),
                    nameof(DelegateCompatiblity.Compatible)));
            }

            yield return new CodeVariableDeclarationStatement(
                optimizedInvokerType,
                "optimized",
                new CodeObjectCreateExpression(optimizedInvokerType, constructorArgs.ToArray())
            );

            var optimizedRef = new CodeVariableReferenceExpression("optimized");
            bool isStructInstance = manipulator.targetType.IsValueType && !stub.IsStatic;

            // Use default(T) for ParameterValue.Create to ensure correct AOT overload resolution
            CodeExpression pvTargetDefault = new CodeDefaultValueExpression(targetType);

            // Call optimized method with ParameterValue overloads
            if (!includesOutOrRef)
            {
                if (isStructInstance)
                {
                    yield return new CodeVariableDeclarationStatement(pvType, "pvTarget", new CodeMethodInvokeExpression(pvTypeExpression, "Create", pvTargetDefault));
                    var pvTargetRef = new CodeDirectionExpression(FieldDirection.Ref, new CodeVariableReferenceExpression("pvTarget"));

                    var pvArgs = stub.GetParameters().Select((p, i) =>
                        (CodeExpression)new CodeMethodInvokeExpression(pvTypeExpression, "Create",
                        new CodeDefaultValueExpression(new CodeTypeReference(p.UnderlyingParameterType(), CodeTypeReferenceOptions.GlobalReference)))).ToList();

                    var invokeRefArgs = new[] { pvTargetRef }.Concat(pvArgs).ToArray();
                    yield return new CodeExpressionStatement(new CodeMethodInvokeExpression(optimizedRef, nameof(OptimizedInvokerBase.InvokeRef), invokeRefArgs));
                }
                else
                {
                    var pvTarget = new CodeMethodInvokeExpression(pvTypeExpression, "Create", pvTargetDefault);

                    var pvArgs = stub.GetParameters().Select((p, i) =>
                        (CodeExpression)new CodeMethodInvokeExpression(pvTypeExpression, "Create",
                        new CodeDefaultValueExpression(new CodeTypeReference(p.UnderlyingParameterType(), CodeTypeReferenceOptions.GlobalReference)))).ToList();

                    var invokeArgs = new[] { pvTarget }.Concat(pvArgs).ToArray();
                    yield return new CodeExpressionStatement(new CodeMethodInvokeExpression(optimizedRef, nameof(OptimizedInvokerBase.Invoke), invokeArgs));
                }
            }

            if (isStructInstance)
            {
                yield return new CodeVariableDeclarationStatement(objectType, "objTarget", pvTargetDefault);

                var objTargetRef = new CodeVariableReferenceExpression("objTarget");

                yield return new CodeExpressionStatement(new CodeMethodInvokeExpression(optimizedRef, nameof(OptimizedInvokerBase.Invoke),
                    objTargetRef, new CodeDefaultValueExpression(new CodeTypeReference(typeof(object[])))));
            }
            else
            {
                yield return new CodeExpressionStatement(new CodeMethodInvokeExpression(optimizedRef, nameof(OptimizedInvokerBase.Invoke),
                    pvTargetDefault, new CodeDefaultValueExpression(new CodeTypeReference(typeof(object[])))));
            }
        }

        private static readonly Dictionary<string, string> UnaryOperators = new Dictionary<string, string>
        {
            { "op_UnaryNegation", "-" },
            { "op_UnaryPlus", "+" },
            { "op_LogicalNot", "!" },
            { "op_OnesComplement", "~" },
            { "op_True", "true " },
            { "op_False", "false " },
            { "op_Increment", "++" },
            { "op_Decrement", "--" }
        };

        public static readonly Dictionary<string, CodeBinaryOperatorType> operatorTypes = new Dictionary<string, CodeBinaryOperatorType>
        {
            { "op_Addition", CodeBinaryOperatorType.Add },
            { "op_Subtraction", CodeBinaryOperatorType.Subtract },
            { "op_Multiply", CodeBinaryOperatorType.Multiply },
            { "op_Division", CodeBinaryOperatorType.Divide },
            { "op_Modulus", CodeBinaryOperatorType.Modulus },
            { "op_BitwiseAnd", CodeBinaryOperatorType.BitwiseAnd },
            { "op_BitwiseOr", CodeBinaryOperatorType.BitwiseOr },
            { "op_LogicalAnd", CodeBinaryOperatorType.BooleanAnd },
            { "op_LogicalOr", CodeBinaryOperatorType.BooleanOr },
            { "op_Assign", CodeBinaryOperatorType.Assign },
            { "op_Equality", CodeBinaryOperatorType.IdentityEquality },
            { "op_GreaterThan", CodeBinaryOperatorType.GreaterThan },
            { "op_LessThan", CodeBinaryOperatorType.LessThan },
            { "op_Inequality", CodeBinaryOperatorType.IdentityInequality },
            { "op_GreaterThanOrEqual", CodeBinaryOperatorType.GreaterThanOrEqual },
            { "op_LessThanOrEqual", CodeBinaryOperatorType.LessThanOrEqual }
        };
    }
}
