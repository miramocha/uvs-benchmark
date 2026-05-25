using System;
using System.Collections.Generic;

namespace Unity.VisualScripting
{
    public static class GenericPool<T> where T : class, IPoolable
    {
        private static readonly Stack<T> free = new Stack<T>();
        private static readonly HashSet<T> busy = new HashSet<T>(ReferenceEqualityComparer<T>.Instance);

        private static readonly object @lock = new object();

        public static T New(Func<T> constructor)
        {
            lock (@lock)
            {
                T item;
                if (free.Count == 0)
                {
                    item = constructor();
                }
                else
                {
                    item = free.Pop();
                }

                item.New();
                busy.Add(item);

                return item;
            }
        }

        public static void Free(T item)
        {
            lock (@lock)
            {
                if (!busy.Remove(item))
                {
                    throw new ArgumentException("The item to free is not in use by the pool.", nameof(item));
                }

                item.Free();

                free.Push(item);
            }
        }
    }

    public static class GraphStackPool
    {
        private static readonly Stack<GraphStack> _free = new Stack<GraphStack>(32);

        public static GraphStack New(IGraphRoot root, List<IGraphParentElement> parentElements, Func<GraphStack> constructor)
        {
            GraphStack instance;

            if (_free.Count > 0)
            {
                instance = _free.Pop();
            }
            else
            {
                instance = constructor();
            }

            instance.InitializeNoAlloc(root, parentElements, true);

            return instance;
        }

        public static GraphStack New(GraphPointer source, Func<GraphStack> constructor)
        {
            GraphStack instance;

            if (_free.Count > 0)
            {
                instance = _free.Pop();
            }
            else
            {
                instance = constructor();
            }

            instance.CopyFrom(source, true);

            return instance;
        }

        public static void Free(GraphStack stack)
        {
            if (stack == null) return;

            if (!_free.Contains(stack))
            {
                (stack as IPoolable).Free();
                _free.Push(stack);
            }
        }
    }
}
