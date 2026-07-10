using System.Collections.Generic;
using UnityEngine;

namespace Unity.VisualScripting
{
    [AddComponentMenu("")]
    internal sealed class GraphFixedUpdateManager : MonoBehaviour
    {
        private readonly List<IEventMachine> _updateableMachines = new List<IEventMachine>(2);

        public void Register(IEventMachine machine)
        {
            if (!_updateableMachines.Contains(machine))
                _updateableMachines.Add(machine);
        }

        public void Unregister(IEventMachine machine)
        {
            _updateableMachines.Remove(machine);

            if (_updateableMachines.Count == 0) Destroy(this);
        }

        private void FixedUpdate()
        {
            var count = _updateableMachines.Count;
            for (int i = 0; i < count; i++)
            {
                _updateableMachines[i].TriggerFixedUpdate();
            }
        }
    }
}