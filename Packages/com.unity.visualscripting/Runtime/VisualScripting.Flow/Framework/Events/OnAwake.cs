namespace Unity.VisualScripting
{
    [UnitCategory("Events/Lifecycle")]
    [UnitTitle("On Awake")]
    [TypeIcon(typeof(Start))]
    public class OnAwake : EventUnit<EmptyEventArgs>
    {
        protected override bool register => false;
    
        public override void Instantiate(GraphReference instance)
        {
            base.Instantiate(instance);
            var flow = Flow.New(instance);
            flow.Run(trigger);
        }
    } 
}