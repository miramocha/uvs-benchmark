using System;
using System.Linq;
using System.Text.RegularExpressions;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Unity.VisualScripting
{
    [InitializeOnLoad]
    public static class StackTraceHandler
    {
        static StackTraceHandler()
        {
            EditorGUI.hyperLinkClicked += (editorWindow, args) =>
            {
                if (!args.hyperLinkData.TryGetValue("href", out string href)) return;
                if (!args.hyperLinkData.TryGetValue("line", out string line)) return;
                if (!href.Contains("VSUnit")) return;

                int openingBracketIndex = line.IndexOf('[');
                if (openingBracketIndex == -1) return;

                string targetUnitString = line.Substring(0, openingBracketIndex).Replace(",", "").Trim();

                string referenceContent = line.Substring(openingBracketIndex + 1).Replace("]", "").Trim();

                string targetGraphRefString = referenceContent.Replace(" | ", " > ");

                string[] segments = href.Split('/');
                if (segments.Length < 2) return;

                string sceneName = segments[0];
                string rootObjectName = segments[1];

                var scene = SceneManager.GetSceneByName(sceneName);
                if (!scene.IsValid()) return;

                var targets = UnityObjectUtility.FindObjectsOfTypeIncludingInactive<IMachine>().Where(machine => machine.GetReference() != null && machine.GetReference().gameObject.name == rootObjectName);

                foreach (var go in targets)
                {
                    var baseReference = go.GetReference();
                    var graph = baseReference?.graph;
                    if (graph == null) continue;

                    var reference = baseReference.AsReference();

                    if (graph is FlowGraph)
                    {
                        if (TryFindAndNavigate(GraphTraversal.TraverseFlowGraph<Unit>(reference), targetUnitString, targetGraphRefString, go as UnityEngine.Object))
                            return;
                    }
                    else
                    {
                        if (TryFindAndNavigate(GraphTraversal.TraverseStateGraph<Unit>(reference), targetUnitString, targetGraphRefString, go as UnityEngine.Object))
                            return;
                    }
                }
            };
        }

        private static bool TryFindAndNavigate(System.Collections.Generic.IEnumerable<(GraphReference, Unit)> traversal, string targetUnit, string targetGraphRef, UnityEngine.Object contextObject)
        {
            (GraphReference reference, Unit unit) fallbackMatch = (null, null);

            foreach (var item in traversal)
            {
                var currentReference = item.Item1;
                var unit = item.Item2;

                if (unit.ToString() == targetUnit)
                {
                    string currentRefString = currentReference.ToString();

                    if (!string.IsNullOrEmpty(targetGraphRef) && currentRefString == targetGraphRef)
                    {
                        NavigateToUnit(currentReference, unit, contextObject);
                        return true;
                    }

                    var exception = unit.GetException(currentReference);
                    if (exception != null)
                    {
                        fallbackMatch = (currentReference, unit);
                    }
                    else if (fallbackMatch.unit == null)
                    {
                        fallbackMatch = (currentReference, unit);
                    }
                }
            }

            if (fallbackMatch.unit != null)
            {
                NavigateToUnit(fallbackMatch.reference, fallbackMatch.unit, contextObject);
                return true;
            }

            return false;
        }

        static void NavigateToUnit(GraphReference reference, Unit unit, UnityEngine.Object go)
        {
            GraphWindow.OpenActive(reference);

            EditorApplication.delayCall += () =>
            {
                using (LudiqGraphsEditorUtility.editedContext.Override(GraphWindow.activeContext))
                    GraphWindow.activeContext.canvas.ViewElements(unit.Yield());
            };
            Selection.activeObject = go;
        }
    }
}