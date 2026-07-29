using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Unity.VisualScripting
{
    [Descriptor(typeof(CompiledMemberUnit))]
    public class CompiledMemberUnitDescriptor : UnitDescriptor<CompiledMemberUnit>
    {
        public CompiledMemberUnitDescriptor(CompiledMemberUnit unit) : base(unit)
        {
        }

        protected string name => unit.CSharpName;

        protected string summary => unit.Summary;

        protected Type type => unit.PseudoDeclaringType;

        protected ActionDirection direction => unit.Direction;

        private string Name(bool @short)
        {
            string name;

            if (BoltCore.Configuration.humanNaming)
            {
                name = unit.HumanName;
            }
            else
            {
                name = unit.CSharpName;
            }

            if (direction == ActionDirection.Get) name += " (get)";
            else if (direction == ActionDirection.Set) name += " (set)";

            return name + (!@short ? " (Compiled)" : string.Empty);
        }

        protected override string DefinedTitle()
        {
            return Name(false);
        }

        protected override string ErrorSurtitle(Exception exception)
        {
            if (type != null)
            {
                return type.DisplayName();
            }
            else
            {
                return "Missing Type";
            }
        }

        protected override string ErrorTitle(Exception exception)
        {
            if (!string.IsNullOrEmpty(name))
            {
                if (BoltCore.Configuration.humanNaming)
                {
                    return name.Prettify();
                }
                else
                {
                    return name;
                }
            }

            return base.ErrorTitle(exception);
        }

        protected override string DefinedShortTitle()
        {
            return Name(true);
        }

        protected override EditorTexture DefinedIcon()
        {
            return type.Icon();
        }

        protected override EditorTexture ErrorIcon(Exception exception)
        {
            if (type != null)
            {
                return type.Icon();
            }

            return base.ErrorIcon(exception);
        }

        protected override string DefinedSurtitle()
        {
            return type.DisplayName();
        }

        protected override string DefinedSummary()
        {
            return summary;
        }

        private static EditorTexture _eventIcon;

        private static EditorTexture EventIcon
        {
            get
            {
                if (_eventIcon == null || !_eventIcon.IsValid())
                {
                    Texture2D originalTex = typeof(BoltUnityEvent).Icon()?[IconSize.Small];

                    if (originalTex == null)
                    {
                        return null;
                    }

                    Texture2D solidColorTex = originalTex.CreateSolidColorTextureCopy(new Color(0.1960784f, 41f / 51f, 0.1960784f, 1f));
                    _eventIcon = EditorTexture.Single(solidColorTex);
                }

                return _eventIcon;
            }
        }

        protected override IEnumerable<EditorTexture> DefinedIcons()
        {
            yield return EventIcon;
        }
    }
}
