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

            parentStack.Add(root);

            graphStack.Add(root.childGraph);

            dataStack.Add(machine?.graphData);

            _currentDataCache = dataStack[0];

            depth = parentStack.Count;

            debugDataStack.Add(fetchRootDebugDataBinding?.Invoke(root));

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

            _currentDataCache = other._currentDataCache;
            depth = other.depth;

            if (!isCloning && version == other.version) return;

            int otherVersion = other.version;

            root = other.root;
            gameObject = other.gameObject;

            SyncList(parentStack, other.parentStack);
            SyncList(parentElementStack, other.parentElementStack);
            SyncList(graphStack, other.graphStack);
            SyncList(dataStack, other.dataStack);
            SyncList(debugDataStack, other.debugDataStack);

            version = otherVersion;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void SyncList<T>(List<T> target, List<T> source)
        {
            int count = source.Count;
            int targetCount = target.Count;

            if (count == 0 && targetCount == 0) return;

            int minCount = count < targetCount ? count : targetCount;

            if (count > targetCount && target.Capacity < count)
            {
                target.Capacity = count;
            }

            for (int i = 0; i < minCount; i++)
            {
                target[i] = source[i];
            }

            if (count > targetCount)
            {
                for (int i = minCount; i < count; i++)
                {
                    target.Add(source[i]);
                }
            }
            else if (targetCount > minCount)
            {
                target.RemoveRange(minCount, targetCount - minCount);
            }
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
                    var parent = parentStack[depth - 1];

                    if (parent.isSerializationRoot)
                    {
                        return parent.serializedObject;
                    }

                    depth--;
                }

                throw new GraphPointerException("Could not find serialized object.", this);
            }
        }

        protected readonly List<IGraphParent> parentStack = new List<IGraphParent>();

        protected readonly List<IGraphParentElement> parentElementStack = new List<IGraphParentElement>();

        protected readonly List<IGraph> graphStack = new List<IGraph>();

        protected readonly List<IGraphData> dataStack = new List<IGraphData>();

        protected readonly List<IGraphDebugData> debugDataStack = new List<IGraphDebugData>();

        public IEnumerable<Guid> parentElementGuids => parentElementStack.Select(parentElement => parentElement.guid);

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
            return parent is T;
        }

        public void EnsureWithin<T>() where T : IGraphParent
        {
            if (!IsWithin<T>())
            {
                throw new GraphPointerException($"Graph pointer must be within a {typeof(T)} for this operation.", this);
            }
        }

        public IGraphParent parent => parentStack[parentStack.Count - 1];

        public T GetParent<T>() where T : IGraphParent
        {
            EnsureWithin<T>();

            return (T)parent;
        }

        public IGraphParentElement parentElement
        {
            get
            {
                EnsureChild();

                return parentElementStack[parentElementStack.Count - 1];
            }
        }

        public IGraph rootGraph => graphStack[0];

        public IGraph graph => graphStack[graphStack.Count - 1];

        private IGraphData _currentDataCache;

        protected IGraphData _data
        {
            get => _currentDataCache;
            set
            {
                version++;
                dataStack[dataStack.Count - 1] = value;
                _currentDataCache = value;
            }
        }

        public IGraphData data
        {
            get
            {
                EnsureDataAvailable();
                return _currentDataCache;
            }
        }

        public IGraphData _parentData => dataStack[dataStack.Count - 2];

        public bool hasData => _currentDataCache != null;

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

        public bool hasDebugData => _debugData != null;

        public void EnsureDebugDataAvailable()
        {
            if (!hasDebugData)
            {
                throw new GraphPointerException($"Graph debug data is not available.", this);
            }
        }

        protected IGraphDebugData _debugData
        {
            get => debugDataStack[debugDataStack.Count - 1];
            set
            {
                version++;
                debugDataStack[debugDataStack.Count - 1] = value;
            }
        }

        public IGraphDebugData debugData
        {
            get
            {
                EnsureDebugDataAvailable();
                return _debugData;
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

        protected bool TryEnterParentElement(Guid parentElementGuid, out string error, int? maxRecursionDepth = null)
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

        protected bool TryEnterParentElement(IGraphParentElement parentElement, out string error, int? maxRecursionDepth = null, bool skipContainsCheck = false)
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
                var recursionDepth = 0;
                var _maxRecursionDepth = maxRecursionDepth ?? Recursion.defaultMaxDepth;

                foreach (var parentGraph in graphStack)
                {
                    if (parentGraph == childGraph)
                    {
                        recursionDepth++;
                    }
                }

                if (recursionDepth > _maxRecursionDepth)
                {
                    error = $"Max recursion depth of {_maxRecursionDepth} has been exceeded. Are you nesting a graph within itself?\nIf not, consider increasing '{nameof(Recursion)}.{nameof(Recursion.defaultMaxDepth)}'.";
                    return false;
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

            parentStack.Add(parentElement);
            parentElementStack.Add(parentElement);
            graphStack.Add(childGraph);

            IGraphData childGraphData = null;
            _data?.TryGetChildGraphData(parentElement, out childGraphData);
            dataStack.Add(childGraphData);

            _currentDataCache = childGraphData;

            var childGraphDebugData = _debugData?.GetOrCreateChildGraphData(parentElement);
            debugDataStack.Add(childGraphDebugData);

            depth = parentStack.Count;
        }

        protected void ExitParentElement()
        {
            if (!isChild)
            {
                throw new GraphPointerException("Trying to exit the root graph.", this);
            }

            version++;

            parentStack.RemoveAt(parentStack.Count - 1);
            parentElementStack.RemoveAt(parentElementStack.Count - 1);
            graphStack.RemoveAt(graphStack.Count - 1);
            dataStack.RemoveAt(dataStack.Count - 1);
            debugDataStack.RemoveAt(debugDataStack.Count - 1);

            _currentDataCache = dataStack[dataStack.Count - 1];

            depth = parentStack.Count;
        }

        #endregion


        #region Validation

        public bool isValid
        {
            get
            {
                try
                {
                    if (rootObject == null)
                    {
                        // Root object has been destroyed
                        return false;
                    }

                    if (rootGraph != root.childGraph)
                    {
                        // Root graph has changed
                        return false;
                    }

                    if (serializedObject == null)
                    {
                        // Serialized object has been destroyed
                        return false;
                    }

                    for (var depth = 1; depth < this.depth; depth++)
                    {
                        var parentElement = parentElementStack[depth - 1];
                        var parentGraph = graphStack[depth - 1];
                        var childGraph = graphStack[depth];

                        // Important to check by object and not by GUID here,
                        // because object stack integrity has to be guaranteed
                        // (GUID integrity is implied because they're immutable)
                        if (!parentGraph.elements.Contains(parentElement))
                        {
                            // Parent graph no longer contains the parent element
                            return false;
                        }

                        if (parentElement.childGraph != childGraph)
                        {
                            // Child graph has changed
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

            if (depth != other.depth || rootGraph != other.rootGraph) return false;

            if (!UnityObjectUtility.TrulyEqual(rootObject, other.rootObject)) return false;

            int currentDepth = depth;
            if (currentDepth == 0) return true;

            var dataStack = this.dataStack;
            var otherDataStack = other.dataStack;
            var parentElementStack = this.parentElementStack;
            var otherParentStack = other.parentElementStack;

            for (int d = currentDepth - 1; d >= 0; d--)
            {
                if (dataStack[d] != otherDataStack[d]) return false;

                if (d > 0 && parentElementStack[d - 1] != otherParentStack[d - 1]) return false;
            }

            return true;
        }

        public bool DefinitionEquals(GraphPointer other)
        {
            if (other == null)
            {
                return false;
            }

            if (rootGraph != other.rootGraph)
            {
                return false;
            }

            var depth = this.depth; // Micro optimization

            if (depth != other.depth)
            {
                return false;
            }

            for (int d = 1; d < depth; d++)
            {
                var parentElement = parentElementStack[d - 1];
                var otherParentElement = other.parentElementStack[d - 1];

                if (parentElement != otherParentElement)
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

            var depth = this.depth;

            for (int d = 1; d < depth; d++)
            {
                hash.Add(parentElementStack[d - 1].guid);
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

            for (var depth = 1; depth < this.depth; depth++)
            {
                sb.Append(" > ");

                var parentElementIndex = depth - 1;

                if (parentElementIndex >= parentElementStack.Count)
                {
                    sb.Append("?");
                    break;
                }

                var parentElement = parentElementStack[parentElementIndex];

                sb.Append(parentElement);
            }

            sb.Append(" ]");

            return sb.ToString();
        }

        #endregion
    }
}