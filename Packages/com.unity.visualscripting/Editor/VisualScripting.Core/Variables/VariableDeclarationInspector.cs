using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Unity.VisualScripting
{
    [Inspector(typeof(VariableDeclaration))]
    public sealed class VariableDeclarationInspector : Inspector
    {
        private readonly Metadata nameMetadata;
        private readonly Metadata valueMetadata;
        private readonly Metadata typeMetadata;

        private Type cachedType;
        private Texture cachedIcon;
        private InlineTypeInfo cachedInlineInfo;
        private bool cachedIsInline;
        private SerializableType cachedTypeHandle;

        internal static EditorTexture NullIcon;

        public VariableDeclarationInspector(Metadata metadata)
            : base(metadata)
        {
            VSUsageUtility.isVisualScriptingUsed = true;
            nameMetadata = metadata[nameof(VariableDeclaration.name)];
            valueMetadata = metadata[nameof(VariableDeclaration.value)];
            typeMetadata = metadata[nameof(VariableDeclaration.typeHandle)];
        }

        private SystemObjectInspector systemObjectInspector;
        private Inspector valueInspector;

        private bool hasCachedType;

        public override void Initialize()
        {
            base.Initialize();

            valueInspector = valueMetadata.Inspector();

            if (valueInspector is SystemObjectInspector systemObjectInspector)
            {
                this.systemObjectInspector = systemObjectInspector;
            }

            RefreshCachedTypeInfo();
        }

        private void RefreshCachedTypeInfo()
        {
            var declaration = (VariableDeclaration)metadata.value;

            if (hasCachedType && cachedTypeHandle == declaration.typeHandle)
            {
                return;
            }

            hasCachedType = true;

            cachedTypeHandle = declaration.typeHandle;
            cachedType = cachedTypeHandle.Resolve();

            EditorTexture icon;

            if (cachedType == null || cachedType == typeof(Unknown))
            {
                icon = NullIcon;
            }
            else
            {
                icon = cachedType.Icon();
            }

            cachedIcon = icon[IconSize];

            cachedInlineInfo = new InlineTypeInfo(cachedType);
            cachedIsInline = cachedType != null && (cachedType.IsBasic() || cachedInlineInfo.isConstruct || cachedInlineInfo.isUnityObject);
        }

        protected override float GetHeight(float width, GUIContent label)
        {
            var declaration = (VariableDeclaration)metadata.value;

            var height = 0f;

            using (LudiqGUIUtility.labelWidth.Override(Styles.labelWidth))
            {
                height += Styles.padding;
                height += GetNameHeight(width);

                if (declaration.isOpen)
                {
                    height += Styles.spacing;
                    height += GetTypeHeight(width);
                    height += Styles.spacing;
                    height += GetValueHeight(width);
                }

                height += Styles.padding;
            }

            return height;
        }

        private float GetNameHeight(float width)
        {
            return EditorGUIUtility.singleLineHeight;
        }

        private float GetValueHeight(float width)
        {
            return LudiqGUI.GetInspectorHeight(this, valueMetadata, width);
        }

        float GetTypeHeight(float width)
        {
            return LudiqGUI.GetInspectorHeight(this, typeMetadata, width);
        }

        private bool initializedType;

        protected override void OnGUI(Rect position, GUIContent label)
        {
            position = BeginLabeledBlock(metadata, position, label);

            var declaration = (VariableDeclaration)metadata.value;

            RefreshCachedTypeInfo();

            using (LudiqGUIUtility.labelWidth.Override(Styles.labelWidth))
            {
                y += Styles.padding;

                var namePosition = position.VerticalSection(ref y, GetNameHeight(position.width));

                if (!initializedType)
                {
                    // We need this to call ResolveType for us
                    // if we don't the Inspector initializes with a Null type.
                    systemObjectInspector?.inspector.GetWidth();
                    initializedType = true;
                }

                OnNameGUI(namePosition);

                if (declaration.isOpen)
                {
                    y += Styles.spacing;
                    var typePosition = position.VerticalSection(ref y, GetTypeHeight(position.width));

                    y += Styles.spacing;
                    var valuePosition = position.VerticalSection(ref y, GetValueHeight(position.width));

                    OnTypeGUI(typePosition);
                    OnValueGUI(valuePosition);
                }

                y += Styles.padding;
            }

            EndBlock(metadata);
        }

        private static readonly GUIContent Temp = new GUIContent();

        private const int IconSize = 16;
        private const int InlineValueSpacing = 4;

        public void OnNameGUI(Rect namePosition)
        {
            var declaration = (VariableDeclaration)metadata.value;

            var foldoutRect = new Rect(
                namePosition.x,
                namePosition.y,
                16,
                namePosition.height);

            var oldMode = EditorGUIUtility.hierarchyMode;
            EditorGUIUtility.hierarchyMode = false;

            Temp.image = cachedIcon;

            declaration.isOpen = EditorGUI.Foldout(foldoutRect, declaration.isOpen, Temp, true);

            EditorGUIUtility.hierarchyMode = oldMode;

            var textRect = new Rect(foldoutRect.xMax + IconSize, namePosition.y, namePosition.width - foldoutRect.width - IconSize, namePosition.height);

            bool endBlock;
            string newName;
            if (!declaration.isOpen && cachedIsInline)
            {
                textRect.width -= cachedInlineInfo.width + InlineValueSpacing;

                BeginBlock(nameMetadata, namePosition);

                newName = EditorGUI.DelayedTextField(textRect, (string)nameMetadata.value);

                endBlock = EndBlock(nameMetadata);

                var valueRect = new Rect(textRect.xMax + InlineValueSpacing, textRect.y, cachedInlineInfo.width, textRect.height);

                if (!declaration.isOpen)
                {
                    if (cachedInlineInfo.isUnityObject)
                    {
                        EditorGUI.BeginChangeCheck();
                        var currentObj = valueMetadata.value as UnityEngine.Object;
                        var updatedObj = EditorGUI.ObjectField(valueRect, currentObj, cachedType, true);
                        if (EditorGUI.EndChangeCheck())
                        {
                            valueMetadata.RecordUndo();
                            valueMetadata.value = updatedObj;
                        }
                    }
                    else
                    {
                        valueInspector.Draw(valueRect, GUIContent.none);
                    }
                }
            }
            else
            {
                BeginBlock(nameMetadata, namePosition);
                newName = EditorGUI.DelayedTextField(textRect, (string)nameMetadata.value);
                endBlock = EndBlock(nameMetadata);
            }

            if (endBlock)
            {
                var variableDeclarations = (VariableDeclarationCollection)metadata.parent.value;

                if (StringUtility.IsNullOrWhiteSpace(newName))
                {
                    EditorUtility.DisplayDialog("Edit Variable Name", "Please enter a variable name.", "OK");
                    return;
                }
                else if (variableDeclarations.Contains(newName))
                {
                    EditorUtility.DisplayDialog("Edit Variable Name", "A variable with the same name already exists.", "OK");
                    return;
                }

                nameMetadata.RecordUndo();
                variableDeclarations.EditorRename(declaration, newName);
                nameMetadata.value = newName;
            }
        }

        private static readonly HashSet<Type> ConstructTypes = new HashSet<Type>()
        {
            typeof(Vector2), typeof(Vector3), typeof(Vector4), typeof(Vector2Int), typeof(Vector3Int),
            typeof(Quaternion),
            typeof(Rect),
            typeof(Color),
        };

        private readonly struct InlineTypeInfo
        {
            public readonly bool isUnityObject;
            public readonly bool isConstruct;
            public readonly float width;

            public InlineTypeInfo(Type type)
            {
                isUnityObject = type != null && typeof(UnityEngine.Object).IsAssignableFrom(type);
                isConstruct = type != null && ConstructTypes.Contains(type);
                if (isUnityObject || type == typeof(bool)) width = 20f;
                else if (type == typeof(Vector2) || type == typeof(Vector2Int) || type == typeof(Color)) width = 50f;
                else if (type == typeof(Vector3) || type == typeof(Vector3Int)) width = 65f;
                else if (type == typeof(Vector4)) width = 80f;
                else width = 35f;
            }
        }

        public void OnValueGUI(Rect valuePosition)
        {
            LudiqGUI.Inspector(valueMetadata, valuePosition, GUIContent.none);
        }

        public void OnTypeGUI(Rect position)
        {
            LudiqGUI.Inspector(typeMetadata, position, GUIContent.none);
        }

        public static class Styles
        {
            public static readonly float labelWidth = SystemObjectInspector.Styles.labelWidth;
            public static readonly float padding = 2;
            public static readonly float spacing = EditorGUIUtility.standardVerticalSpacing;
        }
    }
}
