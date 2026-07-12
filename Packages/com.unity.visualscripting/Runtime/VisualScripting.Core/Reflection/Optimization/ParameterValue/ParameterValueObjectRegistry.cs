using System;
using System.Runtime.CompilerServices;
#if UNITY_6000_6_OR_NEWER
using Unity.Scripting.LifecycleManagement;
#endif
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Unity.VisualScripting
{
#if UNITY_6000_6_OR_NEWER
    public static partial class ParameterValueObjectRegistry
#else
    public static class ParameterValueObjectRegistry
#endif
    {
        private const int PageShift = 12;
        private const int PageSize = 1 << PageShift;
        private const int SlotMask = PageSize - 1;
        private const int MaxPages = 16384;
        private const int IndexMask = 0x03FFFFFF;

        private static readonly object[][] Pages = new object[MaxPages][];
        private static readonly int[] PageActiveCounts = new int[MaxPages];

        private static int[] FreeIndices = new int[1024];
        private static int freeCount = 0;
        private static int nextGlobalIndex = 0;

        private static readonly object WriteLock = new object();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static object Get(int handle)
        {
            if ((uint)handle > IndexMask) return null;

            return Pages[handle >> PageShift]?[handle & SlotMask];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Update(int handle, object newValue)
        {
            if ((uint)handle > IndexMask) return;

            var page = Pages[handle >> PageShift];
            if (page != null)
            {
                page[handle & SlotMask] = newValue;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static int Allocate(object value)
        {
            if (value is null) return -1;

            lock (WriteLock)
            {
                int index = freeCount > 0 ? FreeIndices[--freeCount] : nextGlobalIndex++;

                if ((uint)index > IndexMask)
                {
                    if (freeCount == 0) nextGlobalIndex--;

                    throw new OverflowException(
                        $"[ParameterValueObjectRegistry] Limit reached! " +
                        $"More than {IndexMask + 1} concurrent object handles are active.");
                }

                int pageIdx = index >> PageShift;

                if (Pages[pageIdx] == null)
                {
                    Pages[pageIdx] = new object[PageSize];
                }

                Pages[pageIdx][index & SlotMask] = value;
                PageActiveCounts[pageIdx]++;

                return index;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void Free(int handle)
        {
            if ((uint)handle > IndexMask) return;

            int[] nextFreeIndices = null;
            if (freeCount >= FreeIndices.Length)
            {
                nextFreeIndices = new int[FreeIndices.Length * 2];
            }

            lock (WriteLock)
            {
                int pageIdx = handle >> PageShift;
                var page = Pages[pageIdx];
                if (page == null) return;

                int slotIdx = handle & SlotMask;

                if (page[slotIdx] != null)
                {
                    page[slotIdx] = null;
                    PageActiveCounts[pageIdx]--;

                    if (freeCount >= FreeIndices.Length)
                    {
                        Array.Copy(FreeIndices, nextFreeIndices, FreeIndices.Length);
                        FreeIndices = nextFreeIndices;
                    }

                    FreeIndices[freeCount++] = handle;
                }
            }
        }

        public static void TrimExcess()
        {
            lock (WriteLock)
            {
                for (int i = 0; i < Pages.Length; i++)
                {
                    if (Pages[i] != null && PageActiveCounts[i] == 0)
                    {
                        Pages[i] = null;
                    }
                }
            }
        }

        public static void Purge()
        {
            lock (WriteLock)
            {
                Array.Clear(Pages, 0, Pages.Length);
                Array.Clear(PageActiveCounts, 0, PageActiveCounts.Length);

                freeCount = 0;
                nextGlobalIndex = 0;
            }
        }

#if UNITY_6000_6_OR_NEWER
        [OnCodeInitializing]
#else
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
#endif
        private static void OnSubsystemInit()
        {
            Purge();
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
        }

        private static void OnSceneUnloaded(Scene scene) => TrimExcess();
    }
}