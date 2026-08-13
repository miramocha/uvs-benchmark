using System;
using System.Collections.Generic;

namespace Unity.VisualScripting
{
    public static class GenericPool<T> where T : class, IPoolable
    {
        private static readonly Stack<T> free = new Stack<T>();

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

                return item;
            }
        }

        public static void Free(T item)
        {
            lock (@lock)
            {
                item.Free();

                free.Push(item);
            }
        }
    }

    public static class GraphStackPool
    {
        private static readonly object @lock = new object();
        private static readonly Stack<GraphStack> _free = new Stack<GraphStack>();

        public static GraphStack New(IGraphRoot root, List<IGraphParentElement> parentElements, Func<GraphStack> constructor)
        {
            lock (@lock)
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
        }

        public static GraphStack New(GraphPointer source, Func<GraphStack> constructor)
        {
            lock (@lock)
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
        }

        public static void Free(GraphStack stack)
        {
            if (stack == null) return;
            lock (@lock)
            {
                (stack as IPoolable).Free();
                _free.Push(stack);
            }
        }
    }
}