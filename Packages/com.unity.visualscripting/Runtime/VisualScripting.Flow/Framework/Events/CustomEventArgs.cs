using System;
using System.Runtime.CompilerServices;

namespace Unity.VisualScripting
{
    public unsafe readonly struct CustomEventArgs
    {
        public readonly string name;

        public readonly ParameterValue* argumentPtr;
        public readonly int argumentCount;

        public ref readonly ParameterValue this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref argumentPtr[index];
        }

        private const string ArgumentArrayObsoleteMessage = "CustomEventArgs does not support allocating an array of arguments. Use AsSpan() to access the arguments as a ReadOnlySpan<ParameterValue> instead, or use indexer to access items.";
        private const string ConstructorObsoleteMessage = "CustomEventArgs does not support allocating an array of arguments, use Span<ParameterValue> or ParameterValue* constructor instead.";
        
        [Obsolete(ArgumentArrayObsoleteMessage, true)]
        public readonly object[] arguments
        {
            get => throw new NotSupportedException(ArgumentArrayObsoleteMessage);
            set => throw new NotSupportedException(ArgumentArrayObsoleteMessage);
        }

        public CustomEventArgs(string name, ParameterValue* argumentPtr, int argumentCount)
        {
            this.name = name;
            this.argumentPtr = argumentPtr;
            this.argumentCount = argumentCount;
        }

        public CustomEventArgs(string name, Span<ParameterValue> arguments)
        {
            this.name = name;
            fixed (ParameterValue* ptr = arguments)
            {
                argumentPtr = ptr;
            }
            argumentCount = arguments.Length;
        }

        [Obsolete(ConstructorObsoleteMessage, true)]
        public CustomEventArgs(string name, params object[] arguments)
        {
            throw new NotSupportedException(ConstructorObsoleteMessage);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly ReadOnlySpan<ParameterValue> AsSpan()
        {
            if (argumentCount == 0 || argumentPtr == null)
                return ReadOnlySpan<ParameterValue>.Empty;

            return new ReadOnlySpan<ParameterValue>(argumentPtr, argumentCount);
        }
    }
}