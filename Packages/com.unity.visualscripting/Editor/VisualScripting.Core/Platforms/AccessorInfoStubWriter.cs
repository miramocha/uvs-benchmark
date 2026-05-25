using System.CodeDom;
using System.Collections.Generic;
using System.Reflection;

namespace Unity.VisualScripting
{
    public abstract class AccessorInfoStubWriter<TAccessor> : MemberInfoStubWriter<TAccessor> where TAccessor : MemberInfo
    {
        protected AccessorInfoStubWriter(TAccessor accessorInfo) : base(accessorInfo) { }

        protected abstract OptimizedAccessorBase GetOptimizedAccessor(TAccessor accessorInfo);

        public override IEnumerable<CodeStatement> GetStubStatements()
        {
            var targetType = new CodeTypeReference(manipulator.targetType, CodeTypeReferenceOptions.GlobalReference);
            var accessorType = new CodeTypeReference(manipulator.type, CodeTypeReferenceOptions.GlobalReference);
            var pvType = new CodeTypeReference(typeof(ParameterValue), CodeTypeReferenceOptions.GlobalReference);
            var pvTypeExpression = new CodeTypeReferenceExpression(pvType);
            var objectType = new CodeTypeReference(typeof(object), CodeTypeReferenceOptions.GlobalReference);

            // 1. Define targets
            // directAccessTarget: used for the "val = ..." line (Type name for static, variable for instance)
            // pvTargetValue: used inside ParameterValue.Create (default(Type) to ensure AOT overload resolution)
            CodeExpression directAccessTarget;
            CodeExpression pvTargetValue = new CodeDefaultValueExpression(targetType);

            if (manipulator.requiresTarget)
            {
                yield return new CodeVariableDeclarationStatement(targetType, "target", new CodeDefaultValueExpression(targetType));
                directAccessTarget = new CodeVariableReferenceExpression("target");
            }
            else
            {
                directAccessTarget = new CodeTypeReferenceExpression(targetType);
            }

            // 2. Direct Property/Field access
            var memberReference = new CodeFieldReferenceExpression(directAccessTarget, manipulator.name);

            if (manipulator.isGettable)
            {
                yield return new CodeVariableDeclarationStatement(accessorType, "val", memberReference);
            }
            if (manipulator.isSettable)
            {
                yield return new CodeAssignStatement(memberReference, new CodeDefaultValueExpression(accessorType));
            }

            // 3. Optimized Accessor Setup
            var optimizedAccessorType = new CodeTypeReference(GetOptimizedAccessor(stub).GetType(), CodeTypeReferenceOptions.GlobalReference);
            yield return new CodeVariableDeclarationStatement(optimizedAccessorType, "optimized",
                new CodeObjectCreateExpression(optimizedAccessorType, new CodeDefaultValueExpression(new CodeTypeReference(typeof(TAccessor), CodeTypeReferenceOptions.GlobalReference))));

            var optimizedRef = new CodeVariableReferenceExpression("optimized");
            bool isStructInstance = manipulator.targetType.IsValueType && !manipulator.info.IsStatic();

            if (isStructInstance)
            {
                yield return new CodeVariableDeclarationStatement(pvType, "pvTarget", new CodeMethodInvokeExpression(pvTypeExpression, "Create", pvTargetValue));
                var pvTargetRef = new CodeDirectionExpression(FieldDirection.Ref, new CodeVariableReferenceExpression("pvTarget"));

                if (manipulator.isGettable)
                {
                    yield return new CodeExpressionStatement(new CodeMethodInvokeExpression(optimizedRef, nameof(OptimizedAccessorBase.GetValueRef), pvTargetRef));
                }
                if (manipulator.isSettable)
                {
                    var pvValue = new CodeMethodInvokeExpression(pvTypeExpression, "Create", new CodeDefaultValueExpression(accessorType));
                    yield return new CodeExpressionStatement(new CodeMethodInvokeExpression(optimizedRef, nameof(OptimizedAccessorBase.SetValueRef), pvTargetRef, pvValue));
                }
            }
            else
            {
                // Static or Class Instance
                var pvTarget = new CodeMethodInvokeExpression(pvTypeExpression, "Create", pvTargetValue);

                if (manipulator.isGettable)
                {
                    yield return new CodeExpressionStatement(new CodeMethodInvokeExpression(optimizedRef, nameof(OptimizedAccessorBase.GetValue), pvTarget));
                }
                if (manipulator.isSettable)
                {
                    var pvValue = new CodeMethodInvokeExpression(pvTypeExpression, "Create", new CodeDefaultValueExpression(accessorType));
                    yield return new CodeExpressionStatement(new CodeMethodInvokeExpression(optimizedRef, nameof(OptimizedAccessorBase.SetValue), pvTarget, pvValue));
                }
            }

            if (isStructInstance)
            {
                yield return new CodeVariableDeclarationStatement(objectType, "objTarget", pvTargetValue);
                var objTarget = new CodeVariableReferenceExpression("objTarget");

                if (manipulator.isGettable)
                {
                    yield return new CodeExpressionStatement(new CodeMethodInvokeExpression(optimizedRef, nameof(OptimizedAccessorBase.GetValue), objTarget));
                }
                if (manipulator.isSettable)
                {
                    yield return new CodeExpressionStatement(new CodeMethodInvokeExpression(optimizedRef, nameof(OptimizedAccessorBase.SetValue), objTarget, new CodeDefaultValueExpression(objectType)));
                }
            }
            else
            {
                if (manipulator.isGettable)
                {
                    yield return new CodeExpressionStatement(new CodeMethodInvokeExpression(optimizedRef, nameof(OptimizedAccessorBase.GetValue), pvTargetValue));
                }
                if (manipulator.isSettable)
                {
                    yield return new CodeExpressionStatement(new CodeMethodInvokeExpression(optimizedRef, nameof(OptimizedAccessorBase.SetValue), pvTargetValue, new CodeDefaultValueExpression(objectType)));
                }
            }
        }
    }
}