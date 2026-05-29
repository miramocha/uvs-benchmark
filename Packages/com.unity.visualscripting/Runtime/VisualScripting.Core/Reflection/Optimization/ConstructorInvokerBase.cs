using System;
using System.Linq.Expressions;
using System.Reflection;

namespace Unity.VisualScripting
{
    public abstract class ConstructorInvokerBase : OptimizedConstructorInvokerBase
    {
        protected ConstructorInvokerBase(ConstructorInfo constructorInfo) : base(constructorInfo)
        {
        }

        protected sealed override void CompileExpression()
        {
            var parameterExpressions = GetParameterExpressions();
            var newExpression = Expression.New(constructorInfo, parameterExpressions);

            CompileExpression(newExpression, parameterExpressions);
        }

        protected abstract void CompileExpression(NewExpression newExpression, ParameterExpression[] parameterExpressions);

        protected override void VerifyTarget(object target)
        {
            OptimizedReflection.VerifyStaticTarget(targetType, target);
        }

        protected override void VerifyTarget(ParameterValue target)
        {
            OptimizedReflection.VerifyStaticTarget(targetType, target);
        }
    }
}