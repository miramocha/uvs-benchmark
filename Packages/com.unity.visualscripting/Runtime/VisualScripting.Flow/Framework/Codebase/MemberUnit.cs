using System;
using System.Collections.Generic;
using UnityObject = UnityEngine.Object;

namespace Unity.VisualScripting
{
    [SpecialUnit]
    public abstract class MemberUnit : Unit, IAotStubbable
    {
        protected MemberUnit() : base()
        {
        }

        protected MemberUnit(Member member) : this()
        {
            this.member = member;
            Prewarm();
        }

        public override bool HandleDependencies()
        {
            if (!base.HandleDependencies())
                return false;

            if (!initialized)
            {
                initialized = true;
                if (this is GetMember)
                {
                    compiledClassName = $"{member.targetType.CSharpFileName(true)}_{member.name}_Get_Generated";
                }
                else if (this is SetMember)
                {
                    compiledClassName = $"{member.targetType.CSharpFileName(true)}_{member.name}_Set_Generated";
                }
                else if (this is InvokeMember)
                {
                    if (member.isConstructor)
                    {
                        compiledClassName = $"{member.targetType.CSharpFileName(true)}_Constructor_Generated";
                    }
                    else
                    {
                        compiledClassName = $"{member.targetType.CSharpFileName(true)}_{member.name}_Invoke_Generated";
                    }
                }
            }
            return true;
        }

        private void EnsureInitialized()
        {
            if (!initialized) HandleDependencies();
        }

        [DoNotSerialize]
        private bool initialized;

        [Serialize]
        public string compiledClassName { get; private set; }

        public const string CompiledNamespace = "Unity.VisualScripting.Generated";
        public const string CompiledAssemblyQualifiedName = ", Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null";

        /// <summary>
        /// This is what the name of the Compiled Unit Type will be.
        /// </summary>
        [DoNotSerialize]
        public string CompiledTypeName
        {
            get
            {
                EnsureInitialized();
                return CompiledNamespace + "." + compiledClassName?.Replace(".", string.Empty) + CompiledAssemblyQualifiedName;
            }
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
        public ValueInput target
        {
            get; private set;
        }

        public bool IsCompiled => Type.GetType(CompiledTypeName, false) != null;

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
            ActionDirection actionDirection = ActionDirection.Any;

            if (this is GetMember) actionDirection = ActionDirection.Get;
            else if (this is SetMember) actionDirection = ActionDirection.Set;

            return member.info.CSharpTypeName(actionDirection);
        }
    }
}
