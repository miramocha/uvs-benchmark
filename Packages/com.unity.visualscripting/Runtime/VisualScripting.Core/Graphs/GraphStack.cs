using System;
using System.Collections.Generic;

namespace Unity.VisualScripting
{
    public sealed class GraphStack : GraphPointer, IPoolable, IDisposable
    {
        #region Lifecycle

        private GraphStack() { }

        private static Func<GraphStack> stackFactory = static () => new GraphStack();

        internal void InitializeNoAlloc(IGraphRoot root, List<IGraphParentElement> parentElements, bool ensureValid)
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
                    else
                    {
                        break;
                    }
                }
            }
        }

        internal static GraphStack New(IGraphRoot root, List<IGraphParentElement> parentElements)
        {
            var stack = GraphStackPool.New(root, parentElements, stackFactory);
            return stack;
        }

        internal static GraphStack New(GraphPointer model)
        {
            var stack = GraphStackPool.New(model, stackFactory);
            return stack;
        }

        public GraphStack Clone()
        {
            return New(this);
        }

        public void Dispose()
        {
            GraphStackPool.Free(this);
        }

        void IPoolable.New()
        {
        }

        void IPoolable.Free()
        {
            root = null;
            for (int i = 0; i < frames.Length; i++)
            {
                frames[i] = default;
            }
            version = -1;
            depth = 1;
        }

        #endregion

        #region Conversion

        public override GraphReference AsReference()
        {
            return ToReference();
        }

        public GraphReference ToReference()
        {
            return GraphReference.Intern(this);
        }

        internal void ClearReference()
        {
            GraphReference.ClearIntern(this);
        }

        #endregion

        #region Traversal

        public new void EnterParentElement(IGraphParentElement parentElement)
        {
            base.EnterParentElement(parentElement);
        }

        public bool TryEnterParentElement(IGraphParentElement parentElement)
        {
            return TryEnterParentElement(parentElement, out var error);
        }

        public bool TryEnterParentElementUnsafe(IGraphParentElement parentElement)
        {
            return TryEnterParentElement(parentElement, out var error, -1, true);
        }

        public new void ExitParentElement()
        {
            base.ExitParentElement();
        }

        #endregion
    }
}
