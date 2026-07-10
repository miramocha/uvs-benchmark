using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Profiling;

namespace Unity.VisualScripting
{
    public sealed class Flow : IPoolable, IDisposable
    {
        public enum FlowDebuggingMode
        {
            Enabled = 0,
            EnabledWhenVisible = 1,
            Disabled = 2
        }
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

            public RecursionNode(IUnitPort port, GraphPointer context)
            {
                this.port = port;
                this.context = context.parent;

                _hash = HashCode.Combine(
                    RuntimeHelpers.GetHashCode(this.port),
                    RuntimeHelpers.GetHashCode(this.context));
            }

            public bool Equals(RecursionNode other)
            {
                return ReferenceEquals(port, other.port) && ReferenceEquals(context, other.context);
            }

            public override bool Equals(object obj) => obj is RecursionNode other && Equals(other);

            public override int GetHashCode() => _hash;
        }

        private GraphStack _stack;
        public GraphStack stack => _stack;

        private Recursion<RecursionNode> recursion;

        private sealed class PortDictionary
        {
            [StructLayout(LayoutKind.Sequential)]
            private struct Entry
            {
                public IUnitValuePort Key;
                public ParameterValue Value;
            }

            private Entry[] _entries;
            private int _mask;
            private int _shift;
            private int _count;
            private int _resizeAmount;

            public PortDictionary(int capacity)
            {
                int size = 1;
                while (size < capacity) size <<= 1;

                _entries = new Entry[size];
                _mask = size - 1;
                _resizeAmount = (size >> 1) + (size >> 2);

                int shift = 32;
                int temp = _mask;
                while (temp > 0)
                {
                    shift--;
                    temp >>= 1;
                }
                _shift = shift;
            }

            public bool Contains(IUnitValuePort key)
            {
                var entries = _entries;
                int m = _mask;
                uint hash = (uint)RuntimeHelpers.GetHashCode(key) * 2654435769u;
                int index = (int)(hash >> _shift) & m;

                while (true)
                {
                    ref Entry entry = ref entries[index];
                    if (entry.Key == null) break;

                    if (ReferenceEquals(entry.Key, key))
                    {
                        return true;
                    }

                    index = (index + 1) & m;
                }

                return false;
            }
            
            public bool TryGetValue(IUnitValuePort key, out ParameterValue value)
            {
                var entries = _entries;
                int m = _mask;
                uint hash = (uint)RuntimeHelpers.GetHashCode(key) * 2654435769u;
                int index = (int)(hash >> _shift) & m;

                while (true)
                {
                    ref Entry entry = ref entries[index];
                    if (entry.Key == null) break;

                    if (ReferenceEquals(entry.Key, key))
                    {
                        value = entry.Value;
                        return true;
                    }

                    index = (index + 1) & m;
                }

                value = default;
                return false;
            }

            public ref ParameterValue GetValueRefOrAdd(IUnitValuePort key, out bool exists)
            {
                var entries = _entries;
                int m = _mask;
                uint hash = (uint)RuntimeHelpers.GetHashCode(key) * 2654435769u;
                int i = (int)(hash >> _shift) & m;

                while (true)
                {
                    ref Entry entry = ref entries[i];
                    if (entry.Key == null) break;

                    if (ReferenceEquals(entry.Key, key))
                    {
                        exists = true;
                        return ref entry.Value;
                    }

                    i = (i + 1) & m;
                }

                if (_count >= _resizeAmount)
                {
                    Resize();

                    entries = _entries;
                    m = _mask;
                    i = (int)(hash >> _shift) & m;

                    while (entries[i].Key != null)
                    {
                        i = (i + 1) & m;
                    }
                }

                key.CacheValue();

                exists = false;
                _count++;

                ref Entry newEntry = ref entries[i];
                newEntry.Key = key;
                return ref newEntry.Value;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Set(IUnitValuePort key, in ParameterValue value)
            {
                ref var entryValue = ref GetValueRefOrAdd(key, out _);
                entryValue = value;
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            private void Resize()
            {
                var oldEntries = _entries;
                int newSize = oldEntries.Length * 2;

                var newEntries = new Entry[newSize];
                int newMask = newSize - 1;

                int newShift = 32;
                int temp = newMask;
                while (temp > 0) { newShift--; temp >>= 1; }

                for (int i = 0; i < oldEntries.Length; i++)
                {
                    var key = oldEntries[i].Key;
                    if (key != null)
                    {
                        uint hash = (uint)RuntimeHelpers.GetHashCode(key) * 2654435769u;
                        int idx = (int)(hash >> newShift) & newMask;

                        while (newEntries[idx].Key != null)
                        {
                            idx = (idx + 1) & newMask;
                        }

                        ref Entry targetEntry = ref newEntries[idx];
                        targetEntry.Key = key;
                        targetEntry.Value = oldEntries[i].Value;
                    }
                }

                _entries = newEntries;
                _mask = newMask;
                _shift = newShift;
                _resizeAmount = (newSize >> 1) + (newSize >> 2);
            }

            public void Clear()
            {
                if (_count > 0)
                {
                    Array.Clear(_entries, 0, _entries.Length);
                    _count = 0;
                }
            }
        }

        private readonly PortDictionary locals = new PortDictionary(64);

        private readonly HashSet<int> usedIDs = new HashSet<int>(64);
        public readonly VariableDeclarations variables = new VariableDeclarations();

        private readonly Stack<int> loops = new Stack<int>();

        private readonly HashSet<GraphStack> preservedStacks = new HashSet<GraphStack>(6);

        public MonoBehaviour coroutineRunner { get; private set; }

        private ICollection<Flow> activeCoroutinesRegistry;

        private bool coroutineStopRequested;

        public bool isCoroutine { get; private set; }

        private IEnumerator coroutineEnumerator;

        public bool isPrediction { get; private set; }

        public static FlowDebuggingMode debuggingMode;

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
                switch (debuggingMode)
                {
                    case FlowDebuggingMode.Disabled:
                        return false;

                    case FlowDebuggingMode.Enabled:
                        return !isPrediction && _stack.hasDebugData;

                    case FlowDebuggingMode.EnabledWhenVisible:
                        if (isPrediction || !_stack.hasDebugData)
                            return false;

                        if (stackDepth != _stack.depth)
                        {
                            stackDepth = _stack.depth;
                            isInspected = isInspectedBinding?.Invoke(_stack) ?? false;
                        }
                        return isInspected;

                    default:
                        return false;
                }
#endif
            }
        }

        private bool isInspected;
        public static Func<GraphPointer, bool> isInspectedBinding { get; set; } = null;

        private ParameterValue Self;

        #region Lifecycle

        private Flow() { }

        private static readonly Func<Flow> flowFactory = static () => new Flow();

        public static Flow New(GraphReference reference)
        {
            Ensure.That(nameof(reference)).IsNotNull(reference);

            var flow = GenericPool<Flow>.New(flowFactory);

            var stack = reference.ToStackPooled();

            flow._stack = stack;

            flow.Self = new ParameterValue(stack.self, out int handle);
            flow.usedIDs.Add(handle);

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
            _stack?.Dispose();
            recursion?.Dispose();
            loops.Clear();
            variables.Clear();

            // Preserved stacks could remain if coroutine was interrupted
            if (preservedStacks.Count > 0)
            {
                foreach (var preservedStack in preservedStacks)
                {
                    preservedStack.Dispose();
                }

                preservedStacks.Clear();
            }

            foreach (var id in usedIDs)
            {
                ParameterValue.FreeObject(id);
            }
            usedIDs.Clear();

            locals.Clear();

            loopIdentifier = -1;
            _stack = null;
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
            var preservedStack = _stack.Clone();
            preservedStacks.Add(preservedStack);
            return preservedStack;
        }

        public void RestoreStack(GraphStack stack)
        {
            _stack.CopyFrom(stack, false);
        }

        public void DisposePreservedStack(GraphStack stack)
        {
            stack.Dispose();
            preservedStacks.Remove(stack);
        }
        #endregion

        #region Loops
        public int loopIdentifier = -1;

        private int _currentLoop = -1;

        public int currentLoop => _currentLoop;

        public bool LoopIsNotBroken(int loop)
        {
            return _currentLoop == loop;
        }

        public int EnterLoop()
        {
            var loop = ++loopIdentifier;

            loops.Push(loop);

            _currentLoop = loop;

            return loop;
        }

        public void BreakLoop()
        {
            if (_currentLoop < 0)
            {
                throw new InvalidOperationException("No active loop to break.");
            }

            loops.Pop();

            _currentLoop = loops.Count > 0 ? loops.Peek() : -1;
        }

        public void ExitLoop(int loop)
        {
            if (loop != _currentLoop)
            {
                // Already exited through break
                return;
            }

            loops.Pop();

            _currentLoop = loops.Count > 0 ? loops.Peek() : -1;
        }

        #endregion


        #region Control

        public void Run(ControlOutput port)
        {
            try
            {
#if ENABLE_UVS_PROFILING
                var marker = port.ProfilerMarker;
                marker.Begin(_stack.rootObject);
                Invoke(port);
                marker.End();
#else
                Invoke(port);
#endif
            }
            catch (Exception ex)
            {
                HandleException(ex, null, port);
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

            coroutineRunner = _stack.component;

            if (coroutineRunner == null)
            {
                coroutineRunner = CoroutineRunner.instance;
            }

            activeCoroutinesRegistry = registry;

            activeCoroutinesRegistry?.Add(this);

            // We have to store the enumerator because Coroutine itself
            // can't be cast to IDisposable, which we'll need when stopping.
#if ENABLE_UVS_PROFILING
            var marker = port.ProfilerMarker;
            marker.Begin(_stack.rootObject);

            coroutineEnumerator = Coroutine(port);

            coroutineRunner.StartCoroutine(coroutineEnumerator);

            marker.End();
#else
            coroutineEnumerator = Coroutine(port);

            coroutineRunner.StartCoroutine(coroutineEnumerator);
#endif
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

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Invoke(ControlOutput output)
        {
            if (output == null) ThrowArgumentNull(nameof(output));

            var input = output.connectedControlInput;
            if (input == null) return;

#if ENABLE_UVS_PROFILING
            var marker = input.ProfilerMarker;
            marker.Begin(_stack.rootObject);

            try
            {
#endif
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            var recursionNode = new RecursionNode(output, _stack);
            BeforeInvoke(output, recursionNode);

            try
            {
                if (input.requiresCoroutine)
                    ThrowCoroutineException(input);

                var nextPort = input.action(this);
                if (nextPort != null)
                {
                    Invoke(nextPort);
                }
            }
            catch (Exception ex)
            {
                HandleException(ex, output, input);
                throw;
            }
            finally
            {
                AfterInvoke(recursionNode);
            }
#else
            if (input == null) return;

            try
            {
                if (input.requiresCoroutine)
                    ThrowCoroutineException(input);

                var nextPort = input.action(this);
                if (nextPort != null)
                {
                    Invoke(nextPort);
                }
            }
            catch (Exception ex)
            {
                HandleException(ex, output, input);
                throw;
            }
#endif
#if ENABLE_UVS_PROFILING
            }
            finally
            {
                marker.End();
            }
#endif
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowArgumentNull(string paramName)
        {
            throw new ArgumentNullException(paramName);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowCoroutineException(ControlInput input)
        {
            throw new InvalidOperationException($"Port '{input.key}' on '{input.unit}' can only be triggered in a coroutine.");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private IEnumerable InvokeCoroutine(ControlOutput output)
        {
            Ensure.That(nameof(output)).IsNotNull(output);

            ControlInput input = output.connectedControlInput;
            if (input == null) yield break;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            var recursionNode = new RecursionNode(output, _stack);
            BeforeInvoke(output, recursionNode);
#endif

#if ENABLE_UVS_PROFILING
            var marker = input.ProfilerMarker;
            var context = _stack.rootObject;
            marker.Begin(context);
            bool isMarkerOpen = true;
            try
            {
#endif
            if (input.supportsCoroutine)
            {
                IEnumerable instructions;
                try
                {
                    instructions = InvokeCoroutineDelegate(input);
                }
                catch (Exception ex)
                {
                    HandleException(ex, output, input);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                    AfterInvoke(recursionNode);
#endif
                    throw;
                }

                foreach (var instruction in instructions)
                {
                    if (instruction is ControlOutput controlOutput)
                    {
                        foreach (var unwrappedInstruction in InvokeCoroutine(controlOutput))
                        {
#if ENABLE_UVS_PROFILING
                                if (isMarkerOpen)
                                {
                                    marker.End();
                                    isMarkerOpen = false;
                                }
#endif
                            yield return unwrappedInstruction;
                        }

#if ENABLE_UVS_PROFILING
                            if (!isMarkerOpen)
                            {
                                marker.Begin(context);
                                isMarkerOpen = true;
                            }
#endif
                    }
                    else
                    {
#if ENABLE_UVS_PROFILING
                            if (isMarkerOpen)
                            {
                                marker.End(); isMarkerOpen = false;
                            }
                            yield return instruction;
                            marker.Begin(context);
                            isMarkerOpen = true;
#else
                        yield return instruction;
#endif
                    }
                }
            }
            else
            {
                ControlOutput nextPort;
                try
                {
                    if (input.requiresCoroutine)
                        ThrowCoroutineException(input);

                    nextPort = input.action(this);
                }
                catch (Exception ex)
                {
                    HandleException(ex, output, input);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                    AfterInvoke(recursionNode);
#endif
                    throw;
                }

                if (nextPort != null)
                {
                    foreach (var instruction in InvokeCoroutine(nextPort))
                    {
#if ENABLE_UVS_PROFILING
                            if (isMarkerOpen)
                            {
                                marker.End();
                                isMarkerOpen = false;
                            }
#endif
                        yield return instruction;
                    }
                }
            }
#if ENABLE_UVS_PROFILING
            }
            finally
            {
                if (isMarkerOpen)
                {
                    marker.End();
                }
            }
#endif

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            AfterInvoke(recursionNode);
#endif
        }

        private RecursionNode BeforeInvoke(ControlOutput output, RecursionNode recursionNode)
        {
            try
            {
                recursion?.Enter(recursionNode);
            }
            catch (StackOverflowException ex)
            {
                var input = output.connectedControlInput;
                HandleException(ex, output, input);
                throw;
            }

#if UNITY_EDITOR
            if (enableDebug)
            {
                var connection = output.connection;
                var input = output.connectedControlInput;

                var connectionEditorData = _stack.GetElementDebugData<IUnitConnectionDebugData>(connection);
                var inputUnitEditorData = _stack.GetElementDebugData<IUnitDebugData>(input.unit);

                connectionEditorData.lastInvokeFrame = EditorTimeBinding.frame;
                connectionEditorData.lastInvokeTime = EditorTimeBinding.time;
                inputUnitEditorData.lastInvokeFrame = EditorTimeBinding.frame;
                inputUnitEditorData.lastInvokeTime = EditorTimeBinding.time;
            }
#endif

            return recursionNode;
        }

        private void AfterInvoke(RecursionNode recursionNode)
        {
            recursion?.Exit(recursionNode);
        }

        private static readonly FieldInfo StackTraceField = typeof(Exception).GetField("_stackTraceString", BindingFlags.Instance | BindingFlags.NonPublic);

        private const string VisualScriptingStackTraceHeader = "\n---VisualScripting Nodes Trace---\n";

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void HandleException(Exception ex, IUnitPort from, IUnitPort to)
        {
            var unit = to.unit;
            var stackTrace = ex.StackTrace;

            if (stackTrace.IndexOf(VisualScriptingStackTraceHeader) == -1)
            {
                stackTrace += VisualScriptingStackTraceHeader;
            }
            StackTraceField.SetValueOptimized(ex, stackTrace + "\n" + unit.GetElementStackTrace(_stack.AsReference(), from, to, "/"));
            unit.HandleException(_stack, ex);
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

            if (exists && existing.UsesObjectID)
            {
                existing.UpdateObject(value);

                existing.SetTypeUnsafe(ParameterValue.ValueType.String);
                return;
            }

            var parameterValue = new ParameterValue(value, out int handle);
            usedIDs.Add(handle);
            existing = parameterValue;
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
            if (exists && existing.UsesObjectID)
            {
                existing.UpdateObject(value);
                existing.SetTypeUnsafe(ParameterValue.ValueType.Object);
                return;
            }

            var parameterValue = new ParameterValue(value, out int handle);
            usedIDs.Add(handle);
            existing = parameterValue;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetValue(IUnitValuePort port, ParameterValue value)
        {
            ref var existing = ref locals.GetValueRefOrAdd(port, out bool exists);

            if (exists && existing.UsesObjectID && value.UsesObjectID)
            {
                existing.UpdateObject(value.ObjectValue);
                existing.SetTypeUnsafe(value.type);
                return;
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
            // cachedValue should only ever be true if it was used in Flow.SetValue
            // its a hacky way to improve performance, since the cachedValue
            // is directly on the port class it's part of it's definition so is value
            // will be shared across subgraphs but for the most part that is fine
            // unless the unit mixes Flow.SetValue with the Action on the same port. Then this
            // will go back to the original logic of using TryGetValue for both ports
            if (input.cachedValue && locals.TryGetValue(input, out var local))
                return local;

            var output = input.connectedValueOutput;
            if (output != null)
            {
#if ENABLE_UVS_PROFILING
                var marker = output.ProfilerMarker;
                marker.Begin(_stack.rootObject);
                try
                {
#endif
                // cachedValue should only ever be true if it was used in Flow.SetValue
                if (!output.cachedValue || !locals.TryGetValue(output, out var value))
                {
                    try
                    {
                        value = output.getValue(this);
                        if (value.UsesObjectID) usedIDs.Add(value.objectID);
                    }
                    catch (Exception ex)
                    {
                        HandleException(ex, input, output);
                        throw;
                    }
                }

                // Supports cache will be true if ValueOutput.CacheResult is called
                if (output.supportsCache)
                {
                    locals.Set(output, value);
                    // We can set the value on the Input because the ValueOutput does not change
                    // while the flow is running and since only 1 ValueOutput can be connected it's the same
                    // as caching the output but now the value will be found with the first TryGetValue instead of the
                    // second saving us from a wasted lookup.
                    locals.Set(input, value);
                }

#if UNITY_EDITOR
                if (enableDebug)
                {
                    RecordDebugData(input, output, value);
                }
#endif
                return value;
#if ENABLE_UVS_PROFILING
                }
                finally
                {
                    marker.End();
                }
#endif
            }

            if (input.hasDefaultValue)
            {
                if (input.DefaultValue.IsNull() && input.nullMeansSelf)
                {
                    return Self;
                }
                return input.DefaultValue;
            }

            if (input.allowsNull) return default;

            return ThrowMissingValueInputPortException(input.key);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static ParameterValue ThrowMissingValueInputPortException(string key) => throw new MissingValuePortInputException(key);

#if UNITY_EDITOR
        private void RecordDebugData(ValueInput input, ValueOutput output, ParameterValue value)
        {
            var connection = input.connection;
            if (connection != null)
            {
                var connectionEditorData = _stack.GetElementDebugData<ValueConnection.DebugData>(connection);
                connectionEditorData.lastInvokeFrame = EditorTimeBinding.frame;
                connectionEditorData.lastInvokeTime = EditorTimeBinding.time;
                connectionEditorData.assignedLastValue = true;
                connectionEditorData.lastValue = value.ToObject();
            }

            var inputUnitEditorData = _stack.GetElementDebugData<IUnitDebugData>(output.unit);
            inputUnitEditorData.lastInvokeFrame = EditorTimeBinding.frame;
            inputUnitEditorData.lastInvokeTime = EditorTimeBinding.time;
        }
#endif

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public object GetValue(ValueInput input)
        {
            return GetValueData(input).ToObject();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T GetValue<T>(ValueInput input)
        {
            return GetValueData(input).Cast<T>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public object GetValue(ValueInput input, Type type)
        {
            return ConversionUtility.Convert(GetValue(input), type);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
                defaultValue = _stack.self;
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
                    HandleException(ex, input, output);
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

            var recursionNode = new RecursionNode(output, _stack);

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
                        flow.HandleException(ex, output, null);
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