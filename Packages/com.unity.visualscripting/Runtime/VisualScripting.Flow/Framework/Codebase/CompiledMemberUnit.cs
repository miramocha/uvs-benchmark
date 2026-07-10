using System;

namespace Unity.VisualScripting
{
    public abstract class CompiledMemberUnit : Unit
    {
        public abstract ActionDirection Direction { get; protected set; }
        public abstract Type PseudoDeclaringType { get; protected set; }
        public abstract string Name { get; protected set; }
        public abstract string ActualName { get; protected set; }
        public abstract string Summary { get; protected set; }
        public virtual Type[] ParameterTypes { get; } = null;

        public Member Member => new Member(PseudoDeclaringType, ActualName, ParameterTypes);

        public void CopyFrom(MemberUnit source)
        {
            base.CopyFrom(source);

            Copy(source);
        }

        protected abstract void Copy(MemberUnit source);

        public abstract void CopyTo(MemberUnit source);
    }
}
