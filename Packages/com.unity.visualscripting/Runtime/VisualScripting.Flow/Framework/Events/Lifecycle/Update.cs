using UnityEngine;

namespace Unity.VisualScripting
{
    /// <summary>
    /// Called every frame.
    /// </summary>
    [UnitCategory("Events/Lifecycle")]
    [UnitOrder(3)]
    [UnitTitle("On Update")]
    public sealed class Update : MachineEventUnit<EmptyEventArgs>
    {
        protected override string hookName => EventHooks.Update;

        public override void StartListening(GraphStack stack)
        {
            base.StartListening(stack);

            if (stack.machine is IEventMachine eventMachine) eventMachine.RegisterUpdate();
        }

        public override void StopListening(GraphStack stack)
        {
            if (stack.machine is IEventMachine eventMachine && EventBus.WillRemoveHook(new EventHook(EventHooks.Update, stack.rootObject))) eventMachine.UnregisterUpdate();

            base.StopListening(stack);
        }
    }
}