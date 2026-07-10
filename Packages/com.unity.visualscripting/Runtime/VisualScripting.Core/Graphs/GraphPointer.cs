using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityObject = UnityEngine.Object;

namespace Unity.VisualScripting
{
    public abstract class GraphPointer
    {
        protected struct GraphStackFrame
        {
            public IGraphParent Parent;
            public IGraphParentElement ParentElement;
            public IGraph Graph;
            public IGraphData Data;
            public IGraphDebugData DebugData;
        }

        #region Lifecycle

        protected static bool IsValidRoot(IGraphRoot root)
        {
            return root?.childGraph != null && root as UnityObject != null;
        }

        protected static bool IsValidRoot(UnityObject rootObject)
        {
            return rootObject != null && (rootObject as IGraphRoot)?.childGraph != null;
        }

        internal GraphPointer() { }

        protected void Initialize(IGraphRoot root)
        {
            if (!IsValidRoot(root))
            {
                throw new ArgumentException("Graph pointer root must be a valid Unity object with a non-null child graph.", nameof(root));
            }

            if (!(root is IMachine && root is MonoBehaviour || root is IMacro && root is ScriptableObject))
            {
                throw new ArgumentException("Graph pointer root must be either a machine or a macro.", nameof(root));
            }

            version++;

            this.root = root;
            ref var frame = ref frames[0];
            frame.Parent = root;
            frame.Graph = root.childGraph;
            frame.Data = machine?.graphData;
            var debugData = fetchRootDebugDataBinding?.Invoke(root);
            frame.DebugData = debugData;

            _currentDataCache = machine?.graphData;
            _currentDebugDataCache = debugData;
            _currentParentCache = root;

            depth = 1;
            frameCount = 1;

            if (machine != null)
            {
                // Annoyingly, getting the gameObject property is an API call
                // First, we'll try using our IMachine safe reference that is assigned in play mode on Awake
                // If that fails, we'll try fetching it dynamically

                if (machine.threadSafeGameObject != null)
                {
                    gameObject = machine.threadSafeGameObject;
                }
                else if (UnityThread.allowsAPI)
                {
                    gameObject = component.gameObject;
                }
                else
                {
                    throw new GraphPointerException("Could not fetch graph pointer root game object.", this);
                }
            }
            else
            {
                gameObject = null;
            }
        }

        protected void Initialize(IGraphRoot root, IEnumerable<IGraphParentElement> parentElements, bool ensureValid)
        {
            Initialize(root);

            Ensure.That(nameof(parentElements)).IsNotNull(parentElements);

            foreach (var parentElement in parentElements)
            {
                if (!TryEnterParentElement(parentElement, out var error))
                {
                    if (ensureValid)
                    {
                        throw new GraphPointerException(error, this);
                    }

                    break;
                }
            }
        }

        protected void Initialize(UnityObject rootObject, IEnumerable<Guid> parentElementGuids, bool ensureValid)
        {
            Initialize(rootObject as IGraphRoot);

            Ensure.That(nameof(parentElementGuids)).IsNotNull(parentElementGuids);

            foreach (var parentElementGuid in parentElementGuids)
            {
                if (!TryEnterParentElement(parentElementGuid, out var error))
                {
                    if (ensureValid)
                    {
                        throw new GraphPointerException(error, this);
                    }

                    break;
                }
            }
        }

        #endregion


        #region Conversion

        protected int version = -1;

        public abstract GraphReference AsReference();

        public virtual void CopyFrom(GraphPointer other, bool isCloning = true)
        {
            if (other == null) return;

            // With built-in functionality isCloning will only be false when using Flow.RestoreStack
            // this will avoid cloning if the stack never changed during that Invoke, which is good
            // because if the stack never changed nothing needs to be restored from the Flow.PreserveStack
            if (!isCloning && version == other.version) return;

            _currentDataCache = other._currentDataCache;
            _currentDebugDataCache = other._currentDebugDataCache;
            _currentParentCache = other._currentParentCache;

            depth = other.depth;
            root = other.root;
            gameObject = other.gameObject;

            int sourceCount = other.frameCount;
            int targetCount = frameCount;

            if (sourceCount > 0 || targetCount > 0)
            {
                if (sourceCount > frames.Length)
                {
                    int nextPower = Mathf.NextPowerOfTwo(sourceCount);
                    frames = new GraphStackFrame[nextPower];
                }

                if (sourceCount > 0)
                {
                    for (int i = 0; i < sourceCount; i++)
                    {
                        this.frames[i] = other.frames[i];
                    }
                }

                if (targetCount > sourceCount)
                {
                    for (int i = sourceCount; i < targetCount; i++)
                    {
                        this.frames[i] = default;
                    }
                }
            }

            frameCount = sourceCount;
            version = other.version;
        }

        #endregion

        #region Stack

        public IGraphRoot root { get; protected set; }

        public UnityObject rootObject => root as UnityObject;

        public IMachine machine => root as IMachine;

        public IMacro macro => root as IMacro;

        public MonoBehaviour component => root as MonoBehaviour;

        public GameObject gameObject { get; private set; }

        public GameObject self => gameObject;

        public ScriptableObject scriptableObject => root as ScriptableObject;

        public Scene? scene
        {
            get
            {
                if (gameObject == null)
                {
                    return null;
                }

                var scene = gameObject.scene;

                // We must allow to return unloaded scenes, because
                // On Enable might try fetching scene variables for example
                // See: https://support.ludiq.io/communities/5/topics/1864-/

                if (!scene.IsValid() /* || !scene.isLoaded */)
                {
                    return null;
                }

                return scene;
            }
        }

        public UnityObject serializedObject
        {
            get
            {
                var depth = this.depth;

                while (depth > 0)
                {
                    var parent = frames[depth - 1].Parent;

                    if (parent.isSerializationRoot)
                    {
                        return parent.serializedObject;
                    }

                    depth--;
                }

                throw new GraphPointerException("Could not find serialized object.", this);
            }
        }

        protected GraphStackFrame[] frames = new GraphStackFrame[4];
        protected int frameCount = 0;

        public IEnumerable<Guid> parentElementGuids
        {
            get
            {
                for (int i = 0; i < frameCount; i++)
                {
                    var parentElement = frames[i].ParentElement;
                    if (parentElement != null)
                    {
                        yield return parentElement.guid;
                    }
                }
            }
        }

        #endregion


        #region Utility

        public int depth { get; protected set; } = 1;

        public bool isRoot => depth == 1;

        public bool isChild => depth > 1;

        public void EnsureDepthValid(int depth)
        {
            Ensure.That(nameof(depth)).IsGte(depth, 1);

            if (depth > this.depth)
            {
                throw new GraphPointerException($"Trying to fetch a graph pointer level above depth: {depth} > {this.depth}", this);
            }
        }

        public void EnsureChild()
        {
            if (!isChild)
            {
                throw new GraphPointerException("Graph pointer does not point to a child graph.", this);
            }
        }

        public bool IsWithin<T>() where T : IGraphParent
        {
            return _currentParentCache is T;
        }

        public void EnsureWithin<T>() where T : IGraphParent
        {
            if (!IsWithin<T>())
            {
                throw new GraphPointerException($"Graph pointer must be within a {typeof(T)} for this operation.", this);
            }
        }

        public IGraphParent parent => _currentParentCache;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T GetParent<T>() where T : IGraphParent
        {
            EnsureWithin<T>();

            return (T)_currentParentCache;
        }

        public IGraphParentElement parentElement
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                EnsureChild();
                return frames[frameCount - 1].ParentElement;
            }
        }

        public IGraph rootGraph => frames[0].Graph;

        public IGraph graph => frames[frameCount - 1].Graph;

        private IGraphData _currentDataCache;
        private IGraphDebugData _currentDebugDataCache;
        private IGraphParent _currentParentCache;

        protected IGraphData _data
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _currentDataCache;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                version++;
                ref var frame = ref frames[frameCount - 1];
                frame.Data = value;
                _currentDataCache = value;
            }
        }

        public IGraphData data
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                EnsureDataAvailable();
                return _currentDataCache;
            }
        }

        protected IGraphData _parentData => frames[frameCount - 2].Data;

        public bool hasData => _currentDataCache != null;

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void EnsureDataAvailable()
        {
            if (!hasData)
            {
                throw new GraphPointerException($"Graph data is not available.", this);
            }
        }

        public T GetGraphData<T>() where T : IGraphData
        {
            var data = this.data;

            if (data is T)
            {
                return (T)data;
            }

            throw new GraphPointerException($"Graph data type mismatch. Found {data.GetType()}, expected {typeof(T)}.", this);
        }

        public T GetElementData<T>(IGraphElementWithData element) where T : IGraphElementData
        {
            if (_data.TryGetElementData(element, out var elementData))
            {
                if (elementData is T)
                {
                    return (T)elementData;
                }

                throw new GraphPointerException($"Graph element data type mismatch. Found {elementData.GetType()}, expected {typeof(T)}.", this);
            }

            throw new GraphPointerException($"Missing graph element data for {element}.", this);
        }

        public static Func<IGraphRoot, IGraphDebugData> fetchRootDebugDataBinding { get; set; }
        internal static Action<IGraphRoot> releaseDebugDataBinding;

        public bool hasDebugData => _currentDebugDataCache != null;

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void EnsureDebugDataAvailable()
        {
            if (!hasDebugData)
            {
                throw new GraphPointerException($"Graph debug data is not available.", this);
            }
        }

        protected IGraphDebugData _debugData
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _currentDebugDataCache;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                version++;
                ref var frame = ref frames[frameCount - 1];
                frame.DebugData = value;
                _currentDebugDataCache = value;
            }
        }

        public IGraphDebugData debugData
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                EnsureDebugDataAvailable();
                return _currentDebugDataCache;
            }
        }

        public T GetGraphDebugData<T>() where T : IGraphDebugData
        {
            var debugData = this.debugData;

            if (debugData is T)
            {
                return (T)debugData;
            }

            throw new GraphPointerException($"Graph debug data type mismatch. Found {debugData.GetType()}, expected {typeof(T)}.", this);
        }

        public T GetElementDebugData<T>(IGraphElementWithDebugData element)
        {
            var elementDebugData = debugData.GetOrCreateElementData(element);

            if (elementDebugData is T)
            {
                return (T)elementDebugData;
            }

            throw new GraphPointerException($"Graph element runtime debug data type mismatch. Found {elementDebugData.GetType()}, expected {typeof(T)}.", this);
        }

        #endregion


        #region Traversal

        protected bool TryEnterParentElement(Guid parentElementGuid, out string error, int maxRecursionDepth = -1)
        {
            if (!graph.elements.TryGetValue(parentElementGuid, out var element))
            {
                error = "Trying to enter a graph parent element with a GUID that is not within the current graph.";
                return false;
            }

            if (!(element is IGraphParentElement))
            {
                error = "Provided element GUID does not point to a graph parent element.";
                return false;
            }

            var parentElement = (IGraphParentElement)element;

            return TryEnterParentElement(parentElement, out error, maxRecursionDepth);
        }

        protected bool TryEnterParentElement(IGraphParentElement parentElement, out string error, int maxRecursionDepth = -1, bool skipContainsCheck = false)
        {
            if (!skipContainsCheck && parentElement.graph != graph)
            {
                error = "Trying to enter a graph parent element that is not within the current graph.";
                return false;
            }

            var childGraph = parentElement.childGraph;

            if (childGraph == null)
            {
                error = "Trying to enter a graph parent element without a child graph.";
                return false;
            }

            if (Recursion.safeMode)
            {
                int _maxRecursionDepth = maxRecursionDepth >= 0 ? maxRecursionDepth : Recursion.defaultMaxDepth;
                int recursionDepth = 0;
                int stackCount = frameCount;

                for (int i = stackCount - 1; i >= 0; i--)
                {
                    if (frames[i].Graph == childGraph)
                    {
                        recursionDepth++;
                        if (recursionDepth > _maxRecursionDepth)
                        {
                            error = $"Max recursion depth of {_maxRecursionDepth} has been exceeded. Are you nesting a graph within itself?\nIf not, consider increasing '{nameof(Recursion)}.{nameof(Recursion.defaultMaxDepth)}'.";
                            return false;
                        }
                    }
                }
            }

            EnterValidParentElement(parentElement);
            error = null;
            return true;
        }

        protected void EnterParentElement(IGraphParentElement parentElement)
        {
            if (!TryEnterParentElement(parentElement, out var error))
            {
                throw new GraphPointerException(error, this);
            }
        }

        protected void EnterParentElement(Guid parentElementGuid)
        {
            if (!TryEnterParentElement(parentElementGuid, out var error))
            {
                throw new GraphPointerException(error, this);
            }
        }

        protected void EnterValidParentElement(IGraphParentElement parentElement)
        {
            version++;
            var childGraph = parentElement.childGraph;

            if (frameCount >= frames.Length)
            {
                Array.Resize(ref frames, frames.Length * 2);
            }

            IGraphData childGraphData = null;
            _data?.TryGetChildGraphData(parentElement, out childGraphData);
            _currentDataCache = childGraphData;

            IGraphDebugData childGraphDebugData = null;
            if (_debugData != null)
            {
                childGraphDebugData = _debugData.GetOrCreateChildGraphData(parentElement);
                _currentDebugDataCache = childGraphDebugData;
            }
            else
            {
                _currentDebugDataCache = null;
            }

            _currentParentCache = parentElement;

            ref var frame = ref frames[frameCount];
            frame.Parent = parentElement;
            frame.ParentElement = parentElement;
            frame.Graph = childGraph;
            frame.Data = childGraphData;
            frame.DebugData = childGraphDebugData;

            frameCount++;
            depth = frameCount;
        }

        protected void ExitParentElement()
        {
            if (!isChild)
            {
                throw new GraphPointerException("Trying to exit the root graph.", this);
            }

            version++;
            frameCount--;

            ref var discardedFrame = ref frames[frameCount];
            discardedFrame.Parent = null;
            discardedFrame.ParentElement = null;
            discardedFrame.Graph = null;
            discardedFrame.Data = null;
            discardedFrame.DebugData = null;

            if (frameCount > 0)
            {
                ref var activeFrame = ref frames[frameCount - 1];
                _currentDataCache = activeFrame.Data;
                _currentDebugDataCache = activeFrame.DebugData;
                _currentParentCache = activeFrame.Parent;
            }
            else
            {
                _currentDataCache = null;
                _currentDebugDataCache = null;
                _currentParentCache = null;
            }

            depth = frameCount;
        }

        #endregion


        #region Validation

        public bool isValid
        {
            get
            {
                try
                {
                    if (rootObject == null) return false;
                    if (rootGraph != root.childGraph) return false;
                    if (serializedObject == null) return false;

                    int currentDepth = this.depth;

                    for (var d = 1; d < currentDepth; d++)
                    {
                        ref readonly var parentFrame = ref frames[d - 1];
                        ref readonly var childFrame = ref frames[d];

                        var parentElement = childFrame.ParentElement;
                        var parentGraph = parentFrame.Graph;
                        var childGraph = childFrame.Graph;

                        if (parentElement == null) return false;

                        if (parentGraph != parentElement.graph)
                        {
                            return false;
                        }

                        if (parentElement.childGraph != childGraph)
                        {
                            return false;
                        }
                    }

                    return true;
                }
                catch (Exception ex)
                {
                    Debug.LogWarning("Failed to check graph pointer validity: \n" + ex);
                    return false;
                }
            }
        }

        public void EnsureValid()
        {
            if (!isValid)
            {
                throw new GraphPointerException("Graph pointer is invalid.", this);
            }
        }

        #endregion


        #region Equality

        public bool InstanceEquals(GraphPointer other)
        {
            if (ReferenceEquals(this, other)) return true;
            if (other == null) return false;

            int currentDepth = depth;
            if (currentDepth != other.depth || rootGraph != other.rootGraph) return false;
            if (!UnityObjectUtility.TrulyEqual(rootObject, other.rootObject)) return false;
            if (currentDepth == 0) return true;

            for (int d = currentDepth - 1; d >= 0; d--)
            {
                ref readonly var localFrame = ref this.frames[d];
                ref readonly var otherFrame = ref other.frames[d];

                if (localFrame.Data != otherFrame.Data) return false;

                if (d > 0 && localFrame.ParentElement != otherFrame.ParentElement) return false;
            }

            return true;
        }

        public bool DefinitionEquals(GraphPointer other)
        {
            if (other == null) return false;
            if (rootGraph != other.rootGraph) return false;

            int currentDepth = this.depth;
            if (currentDepth != other.depth) return false;

            for (int d = 1; d < currentDepth; d++)
            {
                if (this.frames[d - 1].ParentElement != other.frames[d - 1].ParentElement)
                {
                    return false;
                }
            }

            return true;
        }

        public int ComputeHashCode()
        {
            var hash = new HashCode();

            if (!ReferenceEquals(rootObject, null))
            {
                hash.Add(rootObject.GetHashCode());
            }

            if (rootGraph != null)
            {
                hash.Add(rootGraph.GetHashCode());
            }

            int currentDepth = this.depth;

            for (int d = 1; d < currentDepth; d++)
            {
                var element = frames[d - 1].ParentElement;
                if (element != null)
                {
                    hash.Add(element.guid);
                }
            }

            return hash.ToHashCode();
        }
        #endregion


        #region Breadcrumbs

        public override string ToString()
        {
            var sb = new StringBuilder();

            sb.Append("[ ");
            sb.Append(rootObject.ToSafeString());

            int currentDepth = this.depth;

            for (var d = 1; d < currentDepth; d++)
            {
                sb.Append(" > ");

                if (d >= frameCount)
                {
                    sb.Append('?');
                    continue;
                }

                var parentElement = frames[d].ParentElement;
                sb.Append(parentElement != null ? parentElement.ToString() : "null");
            }

            sb.Append(" ]");

            return sb.ToString();
        }

        #endregion
    }
}