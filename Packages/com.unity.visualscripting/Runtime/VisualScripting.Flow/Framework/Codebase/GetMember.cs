using System.Runtime.CompilerServices;

namespace Unity.VisualScripting
{
    /// <summary>
    /// Gets the value of a field or property via reflection.
    /// </summary>
    public sealed class GetMember : MemberUnit
    {
        public GetMember() { }

        public GetMember(Member member) : base(member) { }

        [DoNotSerialize]
        [MemberFilter(Fields = true, Properties = true, WriteOnly = false)]
        public Member getter
        {
            get
            {
                return member;
            }
            set
            {
                member = value;
            }
        }

        [DoNotSerialize]
        [PortLabelHidden]
        public ValueOutput value { get; private set; }

        [Inspectable]
        [InspectorLabel("Cache Result", "After the first execution cache the result instead of calling the member again")]
        [Serialize]
        public bool CacheResult { get; private set; }

        [DoNotSerialize]
        private System.Func<Flow, ParameterValue> cachedGet;

        protected override void Definition()
        {
            base.Definition();

            value = ValueOutput(member.type, nameof(value), cachedGet);

            value.PredictableIf((flow) =>
            {
                return member.isPredictable;
            });

            if (CacheResult)
                value.CacheResult();

            if (member.requiresTarget)
            {
                Requirement(target, value);
            }
        }

        protected override bool IsMemberValid(Member member)
        {
            return member.isAccessor && member.isGettable;
        }

        protected override void Initialize()
        {
            base.Initialize();

            var m = member;
            var tPort = target;

            cachedGet = strategy switch
            {
                AccessStrategy.Static => flow => m.Get(ParameterValue.None),
                AccessStrategy.Instance => flow => m.Get(flow.GetValueData(tPort)),
                AccessStrategy.Reference => flow =>
                {
                    var reference = flow.GetValueData(tPort);
                    return m.GetRef(ref reference);
                }
                ,
                _ => throw new UnexpectedEnumValueException<AccessStrategy>(strategy)
            };
        }
        #region Analytics

        public override AnalyticsIdentifier GetAnalyticsIdentifier()
        {
            var aid = new AnalyticsIdentifier
            {
                Identifier = $"{member.targetType.FullName}.{member.name}(Get)",
                Namespace = member.targetType.Namespace
            };
            aid.Hashcode = aid.Identifier.GetHashCode();
            return aid;
        }

        #endregion
    }
}
