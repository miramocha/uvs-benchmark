using System.Collections.Generic;

namespace Unity.VisualScripting
{
    [InitializeAfterPlugins]
    public static class FlowEditorBindings
    {
        static FlowEditorBindings()
        {
            Flow.isInspectedBinding = IsInspected;
        }

        private static bool IsInspected(GraphPointer pointer)
        {
            if (pointer == null) return false;

            var tabs = GraphWindow.tabsNoAlloc;

            foreach (var window in tabs)
            {
                if (window == null) continue;

                var windowRef = window.reference;
                if (windowRef == null) continue;

                if (windowRef.InstanceEquals(pointer))
                {
                    var root = window.rootVisualElement;
                    if (root != null && root.visible)
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
