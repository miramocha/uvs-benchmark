namespace Unity.VisualScripting
{
    public sealed class OptimizedStructDefaultConstructorInvoker<TType> : OptimizedStructDefaultConstructorInvokerBase where TType : struct
    {
        public override object Invoke()
        {
            return new TType();
        }

        public override ParameterValue InvokeValue()
        {
            return ParameterValue.Create(new TType());
        }
    }
}