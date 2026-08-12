using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Unity.VisualScripting
{
    public static class GenericPool<T> where T : class, IPoolable
    {
        private static readonly ConcurrentStack<T> free = new ConcurrentStack<T>();

        public static T New(Func<T> constructor)
        {
            if (!free.TryPop(out T item))
            {
                item = constructor();
            }

            item.New();
            return item;
        }

        public static void Free(T item)
        {
            item.Free();
            free.Push(item);
        }
    }

    public static class GraphStackPool
    {
        private static readonly ConcurrentStack<GraphStack> _free = new ConcurrentStack<GraphStack>();

        public static GraphStack New(IGraphRoot root, List<IGraphParentElement> parentElements, Func<GraphStack> constructor)
        {
            if (!_free.TryPop(out GraphStack instance))
            {
                instance = constructor();
            }

            instance.InitializeNoAlloc(root, parentElements, true);
            return instance;
        }

        public static GraphStack New(GraphPointer source, Func<GraphStack> constructor)
        {
            if (!_free.TryPop(out GraphStack instance))
            {
                instance = constructor();
            }

            instance.CopyFrom(source, true);
            return instance;
        }

        public static void Free(GraphStack stack)
        {
            if (stack == null) return;
            
            if (stack is IPoolable poolable)
            {
                poolable.Free();
            }
            
            _free.Push(stack);
        }
    }
}
