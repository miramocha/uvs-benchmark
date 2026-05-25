using UnityEngine;

namespace Unity.VisualScripting
{
    /// <summary>
    /// Called every frame after all update functions have been called.
    /// </summary>
    [UnitCategory("Events/Lifecycle")]
    [UnitOrder(5)]
    [UnitTitle("On Late Update")]
    public sealed class LateUpdate : MachineEventUnit<EmptyEventArgs>
    {
        protected override string hookName => EventHooks.LateUpdate;

        public override void StartListening(GraphStack stack)
        {
            base.StartListening(stack);

            if (stack.machine is IEventMachine eventMachine) eventMachine.RegisterLateUpdate();
        }

        public override void StopListening(GraphStack stack)
        {
            if (stack.machine is IEventMachine eventMachine && EventBus.WillRemoveHook(new EventHook(EventHooks.LateUpdate, stack.rootObject))) eventMachine.UnregisterLateUpdate();

            base.StopListening(stack);
        }
    }
}
