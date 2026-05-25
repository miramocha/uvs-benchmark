using System;

namespace Unity.VisualScripting
{
    public abstract class OptimizedAccessorBase
    {
        public virtual void Compile() { throw new NotImplementedException(); }
        public virtual object GetValue(object target) { throw new NotImplementedException(); }
        public virtual ParameterValue GetValue(ParameterValue target) { throw new NotImplementedException(); }
        public virtual ParameterValue GetValueRef(ref ParameterValue target) { throw new NotImplementedException(); }
        public virtual void SetValue(object target, object value) { throw new NotImplementedException(); }
        public virtual void SetValue(ParameterValue target, ParameterValue value) { throw new NotImplementedException(); }
        public virtual void SetValueRef(ref ParameterValue target, ParameterValue value) { throw new NotImplementedException(); }
    }
}
