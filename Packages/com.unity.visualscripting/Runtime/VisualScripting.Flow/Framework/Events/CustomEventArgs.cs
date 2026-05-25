using System;
using Unity.Collections;

namespace Unity.VisualScripting
{
    public readonly struct CustomEventArgs
    {
        public readonly string name;

        public readonly NativeArray<ParameterValue> arguments;

        public CustomEventArgs(string name, NativeArray<ParameterValue> arguments)
        {
            this.name = name;
            this.arguments = arguments;
        }
    }
}
