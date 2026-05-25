using System;
using System.Collections.ObjectModel;

namespace Unity.VisualScripting
{
    [SerializationVersion("A")]
    public sealed class VariableDeclarationCollection : KeyedCollection<string, VariableDeclaration>, IKeyedCollection<string, VariableDeclaration>
    {
        public VariableDeclarationCollection() : base(StringComparer.Ordinal) { }

        protected override string GetKeyForItem(VariableDeclaration item)
        {
            return item.name;
        }

        public void EditorRename(VariableDeclaration item, string newName)
        {
            ChangeItemKey(item, newName);
        }

        public new bool TryGetValue(string key, out VariableDeclaration value)
        {
            if (Dictionary == null)
            {
                value = default(VariableDeclaration);
                return false;
            }

            return Dictionary.TryGetValue(key, out value);
        }
    }
}
