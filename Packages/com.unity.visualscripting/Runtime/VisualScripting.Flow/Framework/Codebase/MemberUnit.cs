using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityObject = UnityEngine.Object;

namespace Unity.VisualScripting
{
    [SpecialUnit]
    public abstract class MemberUnit : Unit, IAotStubbable
    {
        protected MemberUnit() : base() { }

        protected MemberUnit(Member member) : this()
        {
            this.member = member;
            Prewarm();
        }

        [Serialize]
        [MemberFilter(Fields = true, Properties = true, Methods = true, Constructors = true)]
        public Member member { get; set; }

        /// <summary>
        /// The target object.
        /// </summary>
        [DoNotSerialize]
        [PortLabelHidden]
        [NullMeansSelf]
        public ValueInput target { get; private set; }

        public override bool canDefine => member != null;

        protected override void Definition()
        {
            member.EnsureReflected();

            if (!IsMemberValid(member))
            {
                throw new NotSupportedException("The member type is not valid for this unit.");
            }

            if (member.requiresTarget)
            {
                target = ValueInput(member.targetType, nameof(target));

                target.SetDefaultValue(member.targetType.PseudoDefault());

                if (typeof(UnityObject).IsAssignableFrom(member.targetType))
                {
                    target.NullMeansSelf();
                }
            }

            Initialize();
        }

        protected enum AccessStrategy
        {
            Static,
            Instance,
            Reference
        }

        protected AccessStrategy strategy;
        protected bool requiresTarget;

        protected virtual void Initialize()
        {
            requiresTarget = member.requiresTarget;

            if (!requiresTarget)
            {
                strategy = AccessStrategy.Static;
            }
            else
            {
                bool isValueType = member.declaringType.IsValueType;
                strategy = isValueType ? AccessStrategy.Reference : AccessStrategy.Instance;
            }
        }

        protected abstract bool IsMemberValid(Member member);

        public override void Prewarm()
        {
            if (member != null && member.isReflected)
            {
                member.Prewarm();
            }
        }

        public override IEnumerable<object> GetAotStubs(HashSet<object> visited)
        {
            if (member != null && member.isReflected)
            {
                yield return member.info;
            }
        }

        protected override string GetUnitName()
        {
            return member.ToString();
        }
    }
}
