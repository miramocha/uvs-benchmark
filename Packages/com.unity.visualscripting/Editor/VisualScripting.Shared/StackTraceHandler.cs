using System.Linq;
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
                if (!args.hyperLinkData.ContainsKey("href")) return;

                if (!args.hyperLinkData.ContainsKey("line")) return;

                if (!args.hyperLinkData["href"].Contains("VSUnit")) return;

                string href = args.hyperLinkData["href"];
                string line = args.hyperLinkData["line"];

                string targetUnitString = line.Replace("]", "").Trim();

                string wholeLine = href + line;

                string[] segments = href.Split('/');

                if (segments.Length < 2) return;

                string sceneName = segments[0];
                string rootObjectName = segments[1];

                var scene = SceneManager.GetSceneByName(sceneName);
                if (!scene.IsValid())
                {
                    return;
                }

                var rootObjects = scene.GetRootGameObjects();
                var targets = UnityObjectUtility.FindObjectsOfTypeIncludingInactive<IMachine>().Where(machine => machine.GetReference() != null && machine.GetReference().gameObject.name == rootObjectName);

                foreach (var go in targets)
                {
                    var graph = go.GetReference()?.graph;
                    if (graph == null)
                    {
                        continue;
                    }

                    var reference = go.GetReference().AsReference();

                    if (graph is FlowGraph flowGraph)
                    {
                        (GraphReference reference, Unit unit) fallbackMatch = (null, null);

                        foreach (var item in GraphTraversal.TraverseFlowGraph<Unit>(reference))
                        {
                            var currentReference = item.Item1;
                            var unit = item.Item2;

                            if (unit.ToString() == targetUnitString)
                            {
                                var exception = unit.GetException(currentReference);

                                if (exception != null)
                                {
                                    NavigateToUnit(currentReference, unit, go as Object);
                                    return;
                                }

                                if (fallbackMatch.unit == null)
                                {
                                    fallbackMatch = (currentReference, unit);
                                }
                            }
                        }
                        if (fallbackMatch.unit != null)
                        {
                            NavigateToUnit(fallbackMatch.reference, fallbackMatch.unit, go as Object);
                        }
                    }
                    else
                    {
                        (GraphReference reference, Unit unit) fallbackMatch = (null, null);
                        foreach (var item in GraphTraversal.TraverseStateGraph<Unit>(reference))
                        {
                            var currentReference = item.Item1;
                            var unit = item.Item2;

                            if (unit.ToString() == targetUnitString)
                            {
                                var exception = unit.GetException(currentReference);

                                if (exception != null)
                                {
                                    NavigateToUnit(currentReference, unit, go as Object);
                                    return;
                                }

                                if (fallbackMatch.unit == null)
                                {
                                    fallbackMatch = (currentReference, unit);
                                }
                            }
                        }
                        if (fallbackMatch.unit != null)
                        {
                            NavigateToUnit(fallbackMatch.reference, fallbackMatch.unit, go as Object);
                        }
                    }
                }
            };
        }

        static void NavigateToUnit(GraphReference reference, Unit unit, Object go)
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
