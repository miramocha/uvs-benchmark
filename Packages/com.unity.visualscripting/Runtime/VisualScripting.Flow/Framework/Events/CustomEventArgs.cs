using System;
using Unity.Collections;

namespace Unity.VisualScripting
{
    public readonly struct CustomEventArgs
    {
        public readonly string name;

        public readonly NativeArray<ParameterValue> arguments;
        
        /// <summary>
        /// Used to enable debug flow for custom events, 
        /// which is disabled by default since custom events can run frequently and enabling debug flow can cause performance issues. 
        /// This is ignored in a build, where debug flow is always disabled.
        /// </summary>
        public readonly bool debug;

        public CustomEventArgs(string name, NativeArray<ParameterValue> arguments, bool debug = false)
        {
            this.name = name;
            this.arguments = arguments;
            this.debug = debug;
        }
    }
}
