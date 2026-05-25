using System.Collections.Generic;
using UnityEngine;

namespace Unity.VisualScripting
{
    [AddComponentMenu("")]
    internal sealed class GraphUpdateManager : MonoBehaviour
    {
        private readonly List<IEventMachine> _updateableMachines = new List<IEventMachine>(4);

        public void Register(IEventMachine machine)
        {
            if (!_updateableMachines.Contains(machine))
                _updateableMachines.Add(machine);
        }

        public void Unregister(IEventMachine machine)
        {
            if (!_updateableMachines.Contains(machine)) return;

            _updateableMachines.Remove(machine);

            if (_updateableMachines.Count == 0) Destroy(this);
        }

        private void Update()
        {
            var count = _updateableMachines.Count;
            for (int i = 0; i < count; i++)
            {
                _updateableMachines[i].TriggerUpdate();
            }
        }
    }
}