using System;
using System.Reflection;

namespace Unity.VisualScripting
{
    public abstract class ConstructorInvokerBase<TResult> : ConstructorInvokerBase
    {
        protected ConstructorInvokerBase(ConstructorInfo constructorInfo) : base(constructorInfo)
        {
            if (OptimizedReflection.safeMode)
            {
                if (constructorInfo.DeclaringType != typeof(TResult))
                {
                    throw new ArgumentException("Declaring type of constructor info doesn't match generic type.", nameof(constructorInfo));
                }
            }
        }
    }
}