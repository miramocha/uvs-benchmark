using UnityEngine;

namespace Unity.VisualScripting
{
    /// <summary>
    /// Called every fixed framerate frame.
    /// </summary>
    [UnitCategory("Events/Lifecycle")]
    [UnitOrder(4)]
    [UnitTitle("On Fixed Update")]
    public sealed class FixedUpdate : MachineEventUnit<EmptyEventArgs>
    {
        protected override string hookName => EventHooks.FixedUpdate;

        public override void StartListening(GraphStack stack)
        {
            base.StartListening(stack);

            if (stack.machine is IEventMachine eventMachine) eventMachine.RegisterFixedUpdate();
        }
    }
}
