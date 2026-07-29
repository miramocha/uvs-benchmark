using System;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.VisualScripting
{
    public class Recursion<T> : IPoolable, IDisposable
    {
        private static readonly EqualityComparer<T> Comparer = EqualityComparer<T>.Default;
        private static readonly Func<Recursion<T>> recursionFactory = static () => new Recursion<T>();

        private T[] traversedStack;
        private readonly Dictionary<T, int> recursionCounts;

        private int traversedSize;
        private bool disposed;
        protected int maxDepth;

        protected Recursion()
        {
            traversedStack = new T[64];
            recursionCounts = new Dictionary<T, int>(64, Comparer);
            traversedSize = 0;
        }

        public void Enter(T o)
        {
            if (!TryEnter(o))
            {
                throw new StackOverflowException($"Max recursion depth of {maxDepth} has been exceeded. Consider increasing '{nameof(Recursion)}.{nameof(Recursion.maxDepth)}'.");
            }
        }

        public bool TryEnter(T o)
        {
            if (disposed)
            {
                throw new ObjectDisposedException(ToString());
            }

            recursionCounts.TryGetValue(o, out int count);

            if (count >= maxDepth)
            {
                return false;
            }

            recursionCounts[o] = count + 1;

            if (traversedSize >= traversedStack.Length)
            {
                Array.Resize(ref traversedStack, traversedStack.Length * 2);
            }

            traversedStack[traversedSize++] = o;

            return true;
        }

        public void Exit(T o)
        {
            if (traversedSize == 0)
            {
                throw new InvalidOperationException("Trying to exit an empty recursion stack.");
            }

            int lastIdx = traversedSize - 1;
            var current = traversedStack[lastIdx];

            if (!Comparer.Equals(current, o))
            {
                throw new InvalidOperationException($"Exiting recursion stack in a non-consecutive order:\nProvided: {o} / Expected: {current}");
            }

            traversedStack[lastIdx] = default;
            traversedSize--;

            int count = recursionCounts[current] - 1;

            if (count == 0)
            {
                recursionCounts.Remove(current);
            }
            else
            {
                recursionCounts[current] = count;
            }
        }

        public void Dispose()
        {
            if (disposed)
            {
                throw new ObjectDisposedException(ToString());
            }

            Free();
        }

        protected virtual void Free()
        {
            GenericPool<Recursion<T>>.Free(this);
        }

        void IPoolable.New()
        {
            disposed = false;
        }

        void IPoolable.Free()
        {
            disposed = true;

            Array.Clear(traversedStack, 0, traversedSize);
            traversedSize = 0;

            recursionCounts.Clear();
        }

        public static Recursion<T> New()
        {
            return New(Recursion.defaultMaxDepth);
        }

        public static Recursion<T> New(int maxDepth)
        {
            if (!Recursion.safeMode)
            {
                return null;
            }

            if (maxDepth < 1)
            {
                throw new ArgumentException("Max recursion depth must be at least one.", nameof(maxDepth));
            }

            var recursion = GenericPool<Recursion<T>>.New(recursionFactory);
            recursion.maxDepth = maxDepth;
            return recursion;
        }
    }

    public sealed class Recursion : Recursion<object>
    {
        private static readonly Func<Recursion> recursionFactory = static () => new Recursion();
        private Recursion() : base() { }

        public static int defaultMaxDepth { get; set; } = 100;

        public static bool safeMode { get; set; }

        internal static void OnRuntimeMethodLoad()
        {
            safeMode = Application.isEditor || Debug.isDebugBuild;
        }

        protected override void Free()
        {
            GenericPool<Recursion>.Free(this);
        }

        public new static Recursion New()
        {
            return New(defaultMaxDepth);
        }

        public new static Recursion New(int maxDepth)
        {
            if (!safeMode)
            {
                return null;
            }

            if (maxDepth < 1)
            {
                throw new ArgumentException("Max recursion depth must be at least one.", nameof(maxDepth));
            }

            var recursion = GenericPool<Recursion>.New(recursionFactory);

            recursion.maxDepth = maxDepth;

            return recursion;
        }
    }
}
