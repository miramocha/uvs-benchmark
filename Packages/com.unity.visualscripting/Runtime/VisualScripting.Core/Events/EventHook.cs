using System;

namespace Unity.VisualScripting
{
    public readonly struct EventHook
    {
        public readonly string name;

        public readonly object target;

        public readonly object tag;

        public EventHook(string name, object target = null, object tag = null)
        {
            Ensure.That(nameof(name)).IsNotNull(name);

            this.name = name;
            this.target = target;
            this.tag = tag;
        }

        public readonly override bool Equals(object obj)
        {
            if (!(obj is EventHook other))
            {
                return false;
            }

            return Equals(other);
        }

        public readonly bool Equals(EventHook other)
        {
            return name == other.name && target == other.target && tag == other.tag;
        }

        public readonly override int GetHashCode()
        {
            return HashCode.Combine(name, target, tag);
        }

        public static bool operator ==(EventHook a, EventHook b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(EventHook a, EventHook b)
        {
            return !(a == b);
        }

        public static implicit operator EventHook(string name)
        {
            return new EventHook(name);
        }
    }
}
