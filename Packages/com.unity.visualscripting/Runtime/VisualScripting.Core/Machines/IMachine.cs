using UnityEngine;

namespace Unity.VisualScripting
{
    public interface IMachine : IGraphRoot, IGraphNester, IAotStubbable
    {
        bool UseCompiledGraph { get; }

        IGraphData graphData { get; set; }

        GameObject threadSafeGameObject { get; }
    }
}
