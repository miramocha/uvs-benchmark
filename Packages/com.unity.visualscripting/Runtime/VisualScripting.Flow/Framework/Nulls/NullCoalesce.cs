using UnityObject = UnityEngine.Object;

namespace Unity.VisualScripting
{
    /// <summary>
    /// Provides a fallback value if the input value is null.
    /// </summary>
    [UnitCategory("Nulls")]
    [TypeIcon(typeof(Null))]
    public sealed class NullCoalesce : Unit
    {
        /// <summary>
        /// The value.
        /// </summary>
        [DoNotSerialize]
        public ValueInput input { get; private set; }

        /// <summary>
        /// The fallback to use if the value is null.
        /// </summary>
        [DoNotSerialize]
        public ValueInput fallback { get; private set; }

        /// <summary>
        /// The returned value.
        /// </summary>
        [DoNotSerialize]
        [PortLabelHidden]
        public ValueOutput result { get; private set; }

        protected override void Definition()
        {
            input = ValueInput<object>(nameof(input)).AllowsNull();
            fallback = ValueInput<object>(nameof(fallback));
            result = ValueOutput(typeof(object), nameof(result), Coalesce).Predictable();

            Requirement(input, result);
            Requirement(fallback, result);
        }

        public ParameterValue Coalesce(Flow flow)
        {
            var input = flow.GetValueData(this.input);

            bool isNull = input.type == ParameterValue.ValueType.Null;

            if (input.UsesObjectID) // Cannot be null if UsesObjectID is false unless type == None
            {
                if (input.ObjectValue is UnityObject @object)
                {
                    // Required cast because of Unity's custom == operator.
                    // ReSharper disable once ConditionIsAlwaysTrueOrFalse
                    isNull = @object == null;
                }
                else
                {
                    isNull = input.IsNull();
                }
            }

            return isNull ? flow.GetValueData(fallback) : input;
        }
    }
}
