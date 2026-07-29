using System;

namespace Unity.VisualScripting
{
    [Descriptor(typeof(Sequence))]
    public class SequenceDescriptor : UnitDescriptor<Sequence>
    {
        public SequenceDescriptor(Sequence unit) : base(unit) { }

        protected override void DefinedPort(IUnitPort port, UnitPortDescription description)
        {
            base.DefinedPort(port, description);

            if (port is ControlOutput)
            {
                var index = Array.IndexOf(unit.multiOutputs, (ControlOutput)port);

                if (index >= 0)
                {
                    description.label = index.ToString();
                }
            }
        }
    }
}
