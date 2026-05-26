using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Unity.VisualScripting
{
    public sealed class Flow : IPoolable, IDisposable
    {
        // We need to check for recursion by passing some additional
        // context information to avoid the same port in multiple different
        // nested flow graphs to count as the same item. Naively,
        // we're using the parent as the context, which seems to work;
        // it won't theoretically catch recursive nesting, but then recursive
        // nesting already isn't supported anyway, so this way we avoid hashing
        // or turning the stack into a reference.
        // https://support.ludiq.io/communities/5/topics/2122-r
        // We make this an equatable struct to avoid any allocation.
        private readonly struct RecursionNode : IEquatable<RecursionNode>
        {
            public readonly IUnitPort port;
            public readonly IGraphParent context;
            private readonly int _hash;

            public RecursionNode(IUnitPort port, GraphPointer pointer)
            {
                this.port = port;
                this.context = pointer.parent;

                unchecked
                {
                    int h1 = port?.GetHashCode() ?? 0;
                    int h2 = context?.GetHashCode() ?? 0;
                    _hash = h1 ^ (h2 << 5) + h2;
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool Equals(RecursionNode other)
            {
                return ReferenceEquals(port, other.port) && ReferenceEquals(context, other.context);
            }

            public override bool Equals(object obj) => obj is RecursionNode other && Equals(other);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override int GetHashCode() => _hash;
        }

        public GraphStack stack { get; private set; }

        private Recursion<RecursionNode> recursion;
        private sealed class PortDictionary
        {
            private IUnitValuePort[] _keys;
            private ParameterValue[] _values;
            private int _mask;
            private int _count;
            private int _resizeAmount;

            public PortDictionary(int capacity)
            {
                int size = 1;
                while (size < capacity) size <<= 1;

                _keys = new IUnitValuePort[size];
                _values = new ParameterValue[size];
                _mask = size - 1;
                _resizeAmount = (size >> 1) + (size >> 2);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool Contains(IUnitValuePort key)
            {
                var keys = _keys;
                int m = _mask;
                int hash = (int)((uint)RuntimeHelpers.GetHashCode(key) * 2654435769u);
                int index = hash & m;

                IUnitValuePort k = keys[index];

                while (k != null)
                {
                    if (ReferenceEquals(k, key)) return true;
                    index = (index + 1) & m;
                    k = keys[index];
                }
                return false;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool TryGetValue(IUnitValuePort key, out ParameterValue value)
            {
                var keys = _keys;
                int m = _mask;
                int hash = (int)((uint)RuntimeHelpers.GetHashCode(key) * 2654435769u);
                int index = hash & m;

                IUnitValuePort k = keys[index];

                while (k != null)
                {
                    if (ReferenceEquals(k, key))
                    {
                        value = _values[index];
                        return true;
                    }
                    index = (index + 1) & m;
                    k = keys[index];
                }

                value = default;
                return false;
            }

            public ref ParameterValue GetValueRefOrAdd(IUnitValuePort key, out bool exists)
            {
                var keys = _keys;

                if (_count >= _resizeAmount)
                {
                    Resize();
                    keys = _keys;
                }

                int m = _mask;
                int hash = (int)((uint)RuntimeHelpers.GetHashCode(key) * 2654435769u);
                int i = hash & m;

                IUnitValuePort k = keys[i];
                while (k != null)
                {
                    if (ReferenceEquals(k, key))
                    {
                        exists = true;
                        return ref _values[i];
                    }
                    i = (i + 1) & m;
                    k = keys[i];
                }

                exists = false;
                keys[i] = key;
                _count++;
                return ref _values[i];
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Set(IUnitValuePort key, ParameterValue value)
            {
                ref var entryValue = ref GetValueRefOrAdd(key, out _);
                entryValue = value;
            }

            private void Resize()
            {
                var oldKeys = _keys;
                var oldValues = _values;
                int newSize = oldKeys.Length * 2;

                _keys = new IUnitValuePort[newSize];
                _values = new ParameterValue[newSize];
                _mask = newSize - 1;
                _count = 0;
                _resizeAmount = (newSize >> 1) + (newSize >> 2);

                for (int i = 0; i < oldKeys.Length; i++)
                {
                    var key = oldKeys[i];
                    if (key != null)
                    {
                        int hash = (int)((uint)RuntimeHelpers.GetHashCode(key) * 2654435769u);
                        int idx = hash & _mask;
                        while (_keys[idx] != null)
                        {
                            idx = (idx + 1) & _mask;
                        }

                        _keys[idx] = key;
                        _values[idx] = oldValues[i];
                        _count++;
                    }
                }
            }

            public void Clear()
            {
                if (_count > 0)
                {
                    Array.Clear(_keys, 0, _keys.Length);
                    _count = 0;
                }
            }
        }

        private readonly PortDictionary locals = new PortDictionary(64);

        private readonly List<int> usedIDs = new List<int>(64);
        public readonly VariableDeclarations variables = new VariableDeclarations();

        private readonly Stack<int> loops = new Stack<int>();

        private readonly List<GraphStack> preservedStacks = new List<GraphStack>(6);

        public MonoBehaviour coroutineRunner { get; private set; }

        private ICollection<Flow> activeCoroutinesRegistry;

        private bool coroutineStopRequested;

        public bool isCoroutine { get; private set; }

        private IEnumerator coroutineEnumerator;

        public bool isPrediction { get; private set; }

        public bool useDebugFlow;

        private bool disposed;
#if UNITY_EDITOR
        private int stackDepth;
#endif
        public bool enableDebug
        {
            get
            {
#if !UNITY_EDITOR
                return false;
#else
                if (useDebugFlow)
                {
                    return true;
                }

                if (stackDepth != stack.depth)
                {
                    stackDepth = stack.depth;
                    isInspected = isInspectedBinding?.Invoke(stack) ?? false;
                }

                if (!isInspected) return false;

                if (isPrediction || !stack.hasDebugData) return false;

                return true;
#endif
            }
        }

        private bool isInspected;

        public static Func<GraphPointer, bool> isInspectedBinding { get; set; } = null;

        #region Lifecycle

        private Flow() { }

        private static readonly Func<Flow> flowFactory = static () => new Flow();

        public static Flow New(GraphReference reference)
        {
            Ensure.That(nameof(reference)).IsNotNull(reference);

            var flow = GenericPool<Flow>.New(flowFactory);

            flow.stack = reference.ToStackPooled();
#if UNITY_EDITOR
            flow.stackDepth = -1;
#endif
            return flow;
        }

        void IPoolable.New()
        {
            disposed = false;

            recursion = Recursion<RecursionNode>.New();
        }

        public void Dispose()
        {
            if (disposed)
            {
                throw new ObjectDisposedException(ToString());
            }

            GenericPool<Flow>.Free(this);
        }

        void IPoolable.Free()
        {
            stack?.Dispose();
            recursion?.Dispose();
            loops.Clear();
            variables.Clear();

            // Preserved stacks could remain if coroutine was interrupted
            if (preservedStacks.Count > 0)
            {
                for (int i = 0; i < preservedStacks.Count; i++)
                {
                    preservedStacks[i].Dispose();
                }

                preservedStacks.Clear();
            }


            foreach (var id in usedIDs)
            {
                if (id != -1)
                {
                    ParameterValue.FreeObject(id);
                }
            }
            usedIDs.Clear();

            locals.Clear();

            loopIdentifier = -1;
            stack = null;
            recursion = null;
            isCoroutine = false;
            coroutineEnumerator = null;
            coroutineRunner = null;
            activeCoroutinesRegistry?.Remove(this);
            activeCoroutinesRegistry = null;
            coroutineStopRequested = false;
            isPrediction = false;

            disposed = true;
        }

        public GraphStack PreserveStack()
        {
            var preservedStack = stack.Clone();
            preservedStacks.Add(preservedStack);
            return preservedStack;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RestoreStack(GraphStack stack)
        {
            this.stack.CopyFrom(stack, false);
        }

        public void DisposePreservedStack(GraphStack stack)
        {
            stack.Dispose();
            int index = preservedStacks.LastIndexOf(stack);
            if (index >= 0)
            {
                int lastIndex = preservedStacks.Count - 1;
                preservedStacks[index] = preservedStacks[lastIndex];
                preservedStacks.RemoveAt(lastIndex);
            }
        }
        #endregion

        #region Loops
        public int loopIdentifier = -1;

        public int currentLoop
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if (loops.Count > 0)
                {
                    return loops.Peek();
                }
                else
                {
                    return -1;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool LoopIsNotBroken(int loop)
        {
            return currentLoop == loop;
        }

        public int EnterLoop()
        {
            var loop = ++loopIdentifier;

            loops.Push(loop);

            return loop;
        }

        public void BreakLoop()
        {
            if (currentLoop < 0)
            {
                throw new InvalidOperationException("No active loop to break.");
            }

            loops.Pop();
        }

        public void ExitLoop(int loop)
        {
            if (loop != currentLoop)
            {
                // Already exited through break
                return;
            }

            loops.Pop();
        }

        #endregion


        #region Control

        public void Run(ControlOutput port)
        {
            try
            {
                Invoke(port);
            }
            catch (Exception ex)
            {
                HandleException(ex, port.unit);
                throw;
            }
            finally
            {
                Dispose();
            }
        }

        public void StartCoroutine(ControlOutput port, ICollection<Flow> registry = null)
        {
            isCoroutine = true;

            coroutineRunner = stack.component;

            if (coroutineRunner == null)
            {
                coroutineRunner = CoroutineRunner.instance;
            }

            activeCoroutinesRegistry = registry;

            activeCoroutinesRegistry?.Add(this);

            // We have to store the enumerator because Coroutine itself
            // can't be cast to IDisposable, which we'll need when stopping.
            coroutineEnumerator = Coroutine(port);

            coroutineRunner.StartCoroutine(coroutineEnumerator);
        }

        public void StopCoroutine(bool disposeInstantly)
        {
            if (!isCoroutine)
            {
                throw new NotSupportedException("Stop may only be called on coroutines.");
            }

            if (disposeInstantly)
            {
                StopCoroutineImmediate();
            }
            else
            {
                // We prefer a soft coroutine stop here that will happen at the *next frame*,
                // because we don't want the flow to be disposed just yet when the event node stops
                // listening, as we still need it for clean up operations.
                coroutineStopRequested = true;
            }
        }

        internal void StopCoroutineImmediate()
        {
            if (coroutineRunner && coroutineEnumerator != null)
            {
                coroutineRunner.StopCoroutine(coroutineEnumerator);

                // Unity doesn't dispose coroutines enumerators when calling StopCoroutine, so we have to do it manually:
                // https://forum.unity.com/threads/finally-block-not-executing-in-a-stopped-coroutine.320611/
                ((IDisposable)coroutineEnumerator).Dispose();
            }
        }

        private IEnumerator Coroutine(ControlOutput startPort)
        {
            try
            {
                foreach (var instruction in InvokeCoroutine(startPort))
                {
                    if (coroutineStopRequested)
                    {
                        yield break;
                    }

                    yield return instruction;

                    if (coroutineStopRequested)
                    {
                        yield break;
                    }
                }
            }
            finally
            {
                // Manual disposal might have already occurred from StopCoroutine,
                // so we have to avoid double disposal, which would throw.
                if (!disposed)
                {
                    Dispose();
                }
            }
        }

        public void Invoke(ControlOutput output)
        {
            Ensure.That(nameof(output)).IsNotNull(output);

            var input = output.connectedControlInput;

            if (input == null) return;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            var recursionNode = new RecursionNode(output, stack);

            BeforeInvoke(output, recursionNode);

            try
            {
                if (input.requiresCoroutine)
                    throw new InvalidOperationException($"Port '{input.key}' on '{input.unit}' can only be triggered in a coroutine.");

                var nextPort = input.action(this);

                if (nextPort != null)
                {
                    Invoke(nextPort);
                }
            }
            catch (Exception ex)
            {
                HandleException(ex, input.unit);
                throw;
            }
            finally
            {
                AfterInvoke(recursionNode);
            }
#else
            try
            {
                if (input.requiresCoroutine)
                    throw new InvalidOperationException($"Port '{input.key}' on '{input.unit}' can only be triggered in a coroutine.");

                var nextPort = input.action(this);

                if (nextPort != null)
                {
                    Invoke(nextPort);
                }
            }
            catch (Exception ex)
            {
                HandleException(ex, input.unit);
                throw;
            }
#endif
        }

        private IEnumerable InvokeCoroutine(ControlOutput output)
        {
            Ensure.That(nameof(output)).IsNotNull(output);

            ControlInput input = output.connectedControlInput;

            if (input == null) yield break;

            if (Recursion.safeMode)
            {
                var recursionNode = new RecursionNode(output, stack);

                BeforeInvoke(output, recursionNode);

                if (input.supportsCoroutine)
                {
                    IEnumerable instructions;

                    try
                    {
                        instructions = InvokeCoroutineDelegate(input);
                    }
                    catch (Exception ex)
                    {
                        HandleException(ex, input.unit);
                        AfterInvoke(recursionNode);
                        throw;
                    }

                    foreach (var instruction in instructions)
                    {
                        if (instruction is ControlOutput)
                        {
                            foreach (var unwrappedInstruction in InvokeCoroutine((ControlOutput)instruction))
                            {
                                yield return unwrappedInstruction;
                            }
                        }
                        else
                        {
                            yield return instruction;
                        }
                    }
                }
                else
                {
                    ControlOutput nextPort;

                    try
                    {
                        if (input.requiresCoroutine)
                            throw new InvalidOperationException($"Port '{input.key}' on '{input.unit}' can only be triggered in a coroutine.");

                        nextPort = input.action(this);
                    }
                    catch (Exception ex)
                    {
                        HandleException(ex, input.unit);
                        AfterInvoke(recursionNode);
                        throw;
                    }

                    if (nextPort != null)
                    {
                        foreach (var instruction in InvokeCoroutine(nextPort))
                        {
                            yield return instruction;
                        }
                    }
                }

                AfterInvoke(recursionNode);
            }
            else
            {
                if (input.supportsCoroutine)
                {
                    IEnumerable instructions;

                    try
                    {
                        instructions = InvokeCoroutineDelegate(input);
                    }
                    catch (Exception ex)
                    {
                        HandleException(ex, input.unit);
                        throw;
                    }

                    foreach (var instruction in instructions)
                    {
                        if (instruction is ControlOutput)
                        {
                            foreach (var unwrappedInstruction in InvokeCoroutine((ControlOutput)instruction))
                            {
                                yield return unwrappedInstruction;
                            }
                        }
                        else
                        {
                            yield return instruction;
                        }
                    }
                }
                else
                {
                    ControlOutput nextPort;

                    try
                    {
                        if (input.requiresCoroutine)
                            throw new InvalidOperationException($"Port '{input.key}' on '{input.unit}' can only be triggered in a coroutine.");

                        nextPort = input.action(this);
                    }
                    catch (Exception ex)
                    {
                        HandleException(ex, input.unit);
                        throw;
                    }

                    if (nextPort != null)
                    {
                        foreach (var instruction in InvokeCoroutine(nextPort))
                        {
                            yield return instruction;
                        }
                    }
                }

            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private RecursionNode BeforeInvoke(ControlOutput output, RecursionNode recursionNode)
        {
            try
            {
                recursion?.Enter(recursionNode);
            }
            catch (StackOverflowException ex)
            {
                HandleException(ex, output.unit);
                throw;
            }

#if UNITY_EDITOR
            if (enableDebug)
            {
                var connection = output.connection;
                var input = output.connectedControlInput;

                var connectionEditorData = stack.GetElementDebugData<IUnitConnectionDebugData>(connection);
                var inputUnitEditorData = stack.GetElementDebugData<IUnitDebugData>(input.unit);

                connectionEditorData.lastInvokeFrame = EditorTimeBinding.frame;
                connectionEditorData.lastInvokeTime = EditorTimeBinding.time;
                inputUnitEditorData.lastInvokeFrame = EditorTimeBinding.frame;
                inputUnitEditorData.lastInvokeTime = EditorTimeBinding.time;
            }
#endif

            return recursionNode;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void AfterInvoke(RecursionNode recursionNode)
        {
            recursion?.Exit(recursionNode);
        }

        private static readonly FieldInfo StackTraceField = typeof(Exception).GetField("_stackTraceString", BindingFlags.Instance | BindingFlags.NonPublic);

        private void HandleException(Exception ex, IUnit unit)
        {
            var stackTrace = ex.StackTrace;

            if (stackTrace.IndexOf("---VisualScripting Nodes Trace---") == -1)
            {
                stackTrace += "\n---VisualScripting Nodes Trace---";
            }
            StackTraceField.SetValueOptimized(ex, stackTrace + "\n" + unit.GetElementStackTrace(stack.AsReference(), unit as Unit, "/"));
            unit.HandleException(stack, ex);
        }

        private IEnumerable InvokeCoroutineDelegate(ControlInput input)
        {
            var instructions = input.coroutineAction(this);

            while (true)
            {
                object instruction;

                if (!instructions.MoveNext())
                {
                    break;
                }

                instruction = instructions.Current;

                yield return instruction;
            }
        }

        #endregion


        #region Values

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsLocal(IUnitValuePort port)
        {
            return locals.Contains(port);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetValue(IUnitValuePort port, byte value) => locals.Set(port, new ParameterValue(value));
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetValue(IUnitValuePort port, sbyte value) => locals.Set(port, new ParameterValue(value));
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetValue(IUnitValuePort port, short value) => locals.Set(port, new ParameterValue(value));
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetValue(IUnitValuePort port, ushort value) => locals.Set(port, new ParameterValue(value));
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetValue(IUnitValuePort port, uint value) => locals.Set(port, new ParameterValue(value));
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetValue(IUnitValuePort port, long value) => locals.Set(port, new ParameterValue(value));
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetValue(IUnitValuePort port, ulong value) => locals.Set(port, new ParameterValue(value));
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetValue(IUnitValuePort port, int value) => locals.Set(port, new ParameterValue(value));
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetValue(IUnitValuePort port, float value) => locals.Set(port, new ParameterValue(value));
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetValue(IUnitValuePort port, double value) => locals.Set(port, new ParameterValue(value));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetValue(IUnitValuePort port, string value)
        {
            ref var existing = ref locals.GetValueRefOrAdd(port, out bool exists);

            if (exists & existing.UsesObjectID)
            {
                ParameterValue.UpdateObject(existing.objectID, value);

                if (existing.type != ParameterValue.ValueType.String)
                {
                    Unsafe.AsRef(in existing.type) = ParameterValue.ValueType.String;
                }
            }
            else
            {
                var parameterValue = new ParameterValue(value, out int handle);

                if (handle != -1) usedIDs.Add(handle);

                existing = parameterValue;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetValue(IUnitValuePort port, Vector2 value) => locals.Set(port, new ParameterValue(value));
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetValue(IUnitValuePort port, Vector3 value) => locals.Set(port, new ParameterValue(value));
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetValue(IUnitValuePort port, Vector4 value) => locals.Set(port, new ParameterValue(value));
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetValue(IUnitValuePort port, Quaternion value) => locals.Set(port, new ParameterValue(value));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetValue(IUnitValuePort port, object value)
        {
            ref var existing = ref locals.GetValueRefOrAdd(port, out bool exists);
            if (exists & existing.UsesObjectID)
            {
                ParameterValue.UpdateObject(existing.objectID, value);
                if (existing.type != ParameterValue.ValueType.Object)
                {
                    Unsafe.AsRef(in existing.type) = ParameterValue.ValueType.Object;
                }
                return;
            }

            var parameterValue = new ParameterValue(value, out int handle);
            if (handle != -1) usedIDs.Add(handle);
            existing = parameterValue;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetValue(IUnitValuePort port, ParameterValue value)
        {
            ref var existing = ref locals.GetValueRefOrAdd(port, out bool exists);

            if (exists)
            {
                if (existing.UsesObjectID & value.UsesObjectID)
                {
                    ParameterValue.UpdateObject(existing.objectID, value.ObjectValue);
                    existing = value;
                    return;
                }
            }

            if (value.UsesObjectID)
            {
                usedIDs.Add(value.objectID);
            }

            existing = value;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public ParameterValue GetValueData(ValueInput input)
        {
            if (locals.TryGetValue(input, out var local))
                return local;

            var output = input.connectedValueOutput;
            if (output != null)
            {
                if (!locals.TryGetValue(output, out var value))
                {
                    try
                    {
                        value = output.getValue(this);
                        if (value.UsesObjectID) usedIDs.Add(value.objectID);
                        if (output.supportsCache) locals.Set(output, value);
                    }
                    catch (Exception ex)
                    {
                        HandleException(ex, output.unit);
                        throw;
                    }
                }

#if UNITY_EDITOR
                if (enableDebug)
                {
                    RecordDebugData(input, output, value);
                }
#endif
                return value;
            }

            if (TryGetDefaultValue(input, out var defaultValue))
            {
                var value = new ParameterValue(defaultValue, out int handle);
                usedIDs.Add(handle);

                return value;
            }

            throw new MissingValuePortInputException(input.key);
        }

#if UNITY_EDITOR
        private void RecordDebugData(ValueInput input, ValueOutput output, ParameterValue value)
        {
            var connection = input.connection;
            if (connection != null)
            {
                var connectionEditorData = stack.GetElementDebugData<ValueConnection.DebugData>(connection);
                connectionEditorData.lastInvokeFrame = EditorTimeBinding.frame;
                connectionEditorData.lastInvokeTime = EditorTimeBinding.time;
                connectionEditorData.assignedLastValue = true;
                connectionEditorData.lastValue = value.ToObject();
            }

            var inputUnitEditorData = stack.GetElementDebugData<IUnitDebugData>(output.unit);
            inputUnitEditorData.lastInvokeFrame = EditorTimeBinding.frame;
            inputUnitEditorData.lastInvokeTime = EditorTimeBinding.time;
        }
#endif

        public object GetValue(ValueInput input)
        {
            return GetValueData(input).ToObject();
        }

        public T GetValue<T>(ValueInput input)
        {
            return GetValueData(input).Cast<T>();
        }

        public object GetValue(ValueInput input, Type type)
        {
            return ConversionUtility.Convert(GetValue(input), type);
        }

        public object GetConvertedValue(ValueInput input)
        {
            return GetValue(input, input.type);
        }

        public static object FetchValue(ValueInput input, GraphReference reference)
        {
            var flow = New(reference);

            var result = flow.GetValue(input);

            flow.Dispose();

            return result;
        }

        public static object FetchValue(ValueInput input, Type type, GraphReference reference)
        {
            return ConversionUtility.Convert(FetchValue(input, reference), type);
        }

        public static T FetchValue<T>(ValueInput input, GraphReference reference)
        {
            return (T)FetchValue(input, typeof(T), reference);
        }

        public bool TryGetDefaultValue(ValueInput input, out object defaultValue)
        {
            if (!input.unit.defaultValues.TryGetValue(input.key, out defaultValue))
            {
                return false;
            }

            if (input.nullMeansSelf && defaultValue == null)
            {
                defaultValue = stack.self;
            }

            return true;
        }

        #endregion


        #region Value Prediction

        public static bool CanPredict(IUnitValuePort port, GraphReference reference)
        {
            Ensure.That(nameof(port)).IsNotNull(port);

            var flow = New(reference);

            flow.isPrediction = true;

            bool canPredict;

            if (port is ValueInput)
            {
                canPredict = flow.CanPredict((ValueInput)port);
            }
            else if (port is ValueOutput)
            {
                canPredict = flow.CanPredict((ValueOutput)port);
            }
            else
            {
                throw new NotSupportedException();
            }

            flow.Dispose();

            return canPredict;
        }

        private bool CanPredict(ValueInput input)
        {
            if (!input.hasValidConnection)
            {
                if (!TryGetDefaultValue(input, out var defaultValue))
                {
                    return false;
                }

                if (typeof(Component).IsAssignableFrom(input.type))
                {
                    defaultValue = defaultValue?.ConvertTo(input.type);
                }

                if (!input.allowsNull && defaultValue == null)
                {
                    return false;
                }

                return true;
            }

            var output = input.connectedValueOutput;

            if (!CanPredict(output))
            {
                return false;
            }

            if (!locals.TryGetValue(output, out var value))
            {
                try
                {
                    value = output.getValue(this);

                    if (value.UsesObjectID)
                        usedIDs.Add(value.objectID);
                }
                catch (Exception ex)
                {
                    HandleException(ex, output.unit);
                    throw;
                }
            }

            var connectedValue = value.ToObject();

            if (!ConversionUtility.CanConvert(connectedValue, input.type, false))
            {
                return false;
            }

            if (typeof(Component).IsAssignableFrom(input.type))
            {
                connectedValue = connectedValue?.ConvertTo(input.type);
            }

            if (!input.allowsNull && connectedValue == null)
            {
                return false;
            }

            return true;
        }

        private bool CanPredict(ValueOutput output)
        {
            // Shortcircuit the expensive check if the port isn't marked as predictable
            if (!output.supportsPrediction)
            {
                return false;
            }

            var recursionNode = new RecursionNode(output, stack);

            if (!recursion?.TryEnter(recursionNode) ?? false)
            {
                return false;
            }

            // Check each value dependency
            foreach (var relation in output.unit.relations.WithDestination(output))
            {
                if (relation.source is ValueInput)
                {
                    var source = (ValueInput)relation.source;

                    if (!CanPredict(source))
                    {
                        recursion?.Exit(recursionNode);
                        return false;
                    }
                }
            }

            var value = CanPredictDelegate(output);

            recursion?.Exit(recursionNode);

            return value;
        }

        private bool CanPredictDelegate(ValueOutput output)
        {
            try
            {
                return output.canPredictValue(this);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Prediction check failed for '{output.key}' on '{output.unit}':\n{ex}");

                return false;
            }
        }

        public static object Predict(IUnitValuePort port, GraphReference reference)
        {
            Ensure.That(nameof(port)).IsNotNull(port);

            var flow = New(reference);

            flow.isPrediction = true;

            object value;

            if (port is ValueInput input)
            {
                value = flow.GetValue(input);
            }
            else if (port is ValueOutput output)
            {
                if (!flow.locals.TryGetValue(output, out var parameterValue))
                {
                    try
                    {
                        parameterValue = output.getValue(flow);

                        if (parameterValue.UsesObjectID)
                            flow.usedIDs.Add(parameterValue.objectID);
                    }
                    catch (Exception ex)
                    {
                        flow.HandleException(ex, output.unit);
                        throw;
                    }
                }

                value = parameterValue.ToObject();
            }
            else
            {
                throw new NotSupportedException();
            }

            flow.Dispose();

            return value;
        }

        public static object Predict(IUnitValuePort port, GraphReference reference, Type type)
        {
            return ConversionUtility.Convert(Predict(port, reference), type);
        }

        public static T Predict<T>(IUnitValuePort port, GraphReference pointer)
        {
            return (T)Predict(port, pointer, typeof(T));
        }

        #endregion
    }
}
