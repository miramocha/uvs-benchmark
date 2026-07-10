using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityObject = UnityEngine.Object;

namespace Unity.VisualScripting
{
    public sealed class ValueInput : UnitPort<ValueOutput, IUnitOutputPort, ValueConnection>, IUnitValuePort, IUnitInputPort, IDisposable
    {
        public ValueInput(string key, Type type) : base(key)
        {
            Ensure.That(nameof(type)).IsNotNull(type);

            this.type = type;
        }

        public Type type { get; }

        public bool hasDefaultValue => hasBakedDefaultValue || unit.defaultValues.ContainsKey(key);

        [DoNotSerialize]
        private ParameterValue _bakedDefaultValue;

        [DoNotSerialize]
        private bool hasBakedDefaultValue;

        public ParameterValue DefaultValue
        {
            get
            {
                if (!hasBakedDefaultValue)
                {
                    if (unit != null && unit.defaultValues.TryGetValue(key, out var rawValue))
                    {
                        _bakedDefaultValue = BakeValue(rawValue);
                    }
                    else
                    {
                        _bakedDefaultValue = ParameterValue.None;
                    }
                    hasBakedDefaultValue = true;
                }
                return _bakedDefaultValue;
            }
        }

        public override IEnumerable<ValueConnection> validConnections => unit?.graph?.valueConnections.WithDestination(this) ?? Enumerable.Empty<ValueConnection>();

        public override IEnumerable<InvalidConnection> invalidConnections => unit?.graph?.invalidConnections.WithDestination(this) ?? Enumerable.Empty<InvalidConnection>();

        public override IEnumerable<ValueOutput> validConnectedPorts => validConnections.Select(c => c.source);

        public override IEnumerable<IUnitOutputPort> invalidConnectedPorts => invalidConnections.Select(c => c.source);

        // Use for inspector metadata
        [DoNotSerialize]
        internal object _defaultValue
        {
            get
            {
                return unit.defaultValues[key];
            }
            set
            {
                if (hasBakedDefaultValue && _bakedDefaultValue.UsesObjectID)
                {
                    ParameterValueObjectRegistry.Free(_bakedDefaultValue.objectID);
                }

                unit.defaultValues[key] = value;

                _bakedDefaultValue = BakeValue(value);
                hasBakedDefaultValue = true;
            }
        }

        private ParameterValue BakeValue(object rawValue)
        {
            if (rawValue is null) return ParameterValue.None;

            return rawValue switch
            {
                int => new ParameterValue((int)rawValue),
                float => new ParameterValue((float)rawValue),
                bool => new ParameterValue((bool)rawValue),
                double => new ParameterValue((double)rawValue),
                long => new ParameterValue((long)rawValue),
                uint => new ParameterValue((uint)rawValue),
                byte => new ParameterValue((byte)rawValue),
                short => new ParameterValue((short)rawValue),
                ushort => new ParameterValue((ushort)rawValue),
                ulong => new ParameterValue((ulong)rawValue),
                sbyte => new ParameterValue((sbyte)rawValue),
                Vector3 v3 => new ParameterValue(v3),
                Vector2 v2 => new ParameterValue(v2),
                Vector4 v4 => new ParameterValue(v4),
                Quaternion q => new ParameterValue(q),
                Color c => new ParameterValue(c),
                _ => new ParameterValue(rawValue)
            };
        }

        public bool nullMeansSelf { get; private set; }

        public bool allowsNull { get; private set; }

        public ValueConnection connection => unit.graph?.valueConnections.SingleOrDefaultWithDestination(this);

        public override bool hasValidConnection => connection != null;

        // Used for the flow to avoid looking up the source port.
        [DoNotSerialize]
        internal ValueOutput connectedValueOutput;

        [DoNotSerialize]
        internal bool cachedValue;

        public bool supportsCache => cachedValue;

        public void SetDefaultValue(object value)
        {
            Ensure.That(nameof(value)).IsOfType(value, type);

            if (!SupportsDefaultValue(type))
            {
                return;
            }

            if (unit.defaultValues.ContainsKey(key))
            {
                unit.defaultValues[key] = value;
            }
            else
            {
                unit.defaultValues.Add(key, value);
            }

            hasBakedDefaultValue = false;
        }

        public override bool CanConnectToValid(ValueOutput port)
        {
            var source = port;
            var destination = this;

            return source.type.IsConvertibleTo(destination.type, false);
        }

        public override void ConnectToValid(ValueOutput port)
        {
            var source = port;
            var destination = this;

            destination.Disconnect();

            unit.graph.valueConnections.Add(new ValueConnection(source, destination));
            connectedValueOutput = port;
            hasBakedDefaultValue = false;
        }

        public override void ConnectToInvalid(IUnitOutputPort port)
        {
            ConnectInvalid(port, this);
            connectedValueOutput = null;
            hasBakedDefaultValue = false;
        }

        public override void DisconnectFromValid(ValueOutput port)
        {
            var connection = validConnections.SingleOrDefault(c => c.source == port);

            if (connection != null)
            {
                unit.graph.valueConnections.Remove(connection);
            }
            connectedValueOutput = null;
            hasBakedDefaultValue = false;
        }

        public override void DisconnectFromInvalid(IUnitOutputPort port)
        {
            DisconnectInvalid(port, this);
            connectedValueOutput = null;
            hasBakedDefaultValue = false;
        }

        public ValueInput NullMeansSelf()
        {
            if (ComponentHolderProtocol.IsComponentHolderType(type))
            {
                nullMeansSelf = true;
            }

            return this;
        }

        public ValueInput AllowsNull()
        {
            if (type.IsNullable())
            {
                allowsNull = true;
            }

            return this;
        }

        private static readonly HashSet<Type> typesWithDefaultValues = new HashSet<Type>()
        {
            typeof(Vector2),
            typeof(Vector3),
            typeof(Vector4),
            typeof(Color),
            typeof(AnimationCurve),
            typeof(Rect),
            typeof(Ray),
            typeof(Ray2D),
            typeof(Type),
#if PACKAGE_INPUT_SYSTEM_EXISTS
            typeof(UnityEngine.InputSystem.InputAction),
#endif
        };

        public static bool SupportsDefaultValue(Type type)
        {
            return
                typesWithDefaultValues.Contains(type) ||
                typesWithDefaultValues.Contains(Nullable.GetUnderlyingType(type)) ||
                type.IsBasic() ||
                typeof(UnityObject).IsAssignableFrom(type);
        }

        public override IUnitPort CompatiblePort(IUnit unit)
        {
            if (unit == this.unit) return null;

            return unit.CompatibleValueOutput(type);
        }

        internal void CacheValue()
        {
            cachedValue = true;
        }

        void IUnitValuePort.CacheValue() => CacheValue();

        public void Dispose()
        {
            if (hasBakedDefaultValue && _bakedDefaultValue.UsesObjectID)
            {
                ParameterValue.FreeObject(_bakedDefaultValue.objectID);
                hasBakedDefaultValue = false;
            }
        }

        ~ValueInput()
        {
            if (hasBakedDefaultValue && _bakedDefaultValue.UsesObjectID)
            {
                ParameterValue.FreeObject(_bakedDefaultValue.objectID);
                hasBakedDefaultValue = false;
            }
        }
    }
}
