namespace Unity.VisualScripting
{
    public static class ExceptionMessages
    {
        public const string Common_IsNull_Failed = "Value must be null.";
        public const string Common_IsNotNull_Failed = "Value cannot be null.";

        public const string Booleans_IsTrueFailed = "Expected an expression that evaluates to true.";
        public const string Booleans_IsFalseFailed = "Expected an expression that evaluates to false.";

        public const string Collections_Any_Failed = "The predicate did not match any elements.";
        public const string Collections_ContainsKey_Failed = "{1} '{0}' was not found.";
        public const string Collections_HasItemsFailed = "Empty collection is not allowed.";
        public const string Collections_HasNoNullItemFailed = "Collection with null items is not allowed.";
        public const string Collections_SizeIs_Failed = "Expected size '{0}' but found '{1}'.";

        public const string Comp_Is_Failed = "Value '{0}' is not '{1}'.";
        public const string Comp_IsNot_Failed = "Value '{0}' is '{1}', which was not expected.";
        public const string Comp_IsNotLt = "Value '{0}' is not lower than limit '{1}'.";
        public const string Comp_IsNotLte = "Value '{0}' is not lower than or equal to limit '{1}'.";
        public const string Comp_IsNotGt = "Value '{0}' is not greater than limit '{1}'.";
        public const string Comp_IsNotGte = "Value '{0}' is not greater than or equal to limit '{1}'.";
        public const string Comp_IsNotInRange_ToLow = "Value '{0}' is < min '{1}'.";
        public const string Comp_IsNotInRange_ToHigh = "Value '{0}' is > max '{1}'.";

        public const string Guids_IsNotEmpty_Failed = "An empty GUID is not allowed.";

        public const string Strings_IsEqualTo_Failed = "Value '{0}' is not '{1}'.";
        public const string Strings_IsNotEqualTo_Failed = "Value '{0}' is '{1}', which was not expected.";
        public const string Strings_SizeIs_Failed = "Expected length '{0}' but got '{1}'.";
        public const string Strings_IsNotNullOrWhiteSpace_Failed = "The string can't be left empty, null or consist of only whitespaces.";
        public const string Strings_IsNotNullOrEmpty_Failed = "The string can't be null or empty.";
        public const string Strings_HasLengthBetween_Failed_ToShort = "The string is not long enough. Must be between '{0}' and '{1}' but was '{2}' characters long.";
        public const string Strings_HasLengthBetween_Failed_ToLong = "The string is too long. Must be between '{0}' and  '{1}'. Must be between '{0}' and '{1}' but was '{2}' characters long.";
        public const string Strings_Matches_Failed = "Value '{0}' does not match '{1}'";
        public const string Strings_IsNotEmpty_Failed = "Empty String is not allowed.";
        public const string Strings_IsGuid_Failed = "Value '{0}' is not a valid GUID.";

        public const string Types_IsOfType_Failed = "Expected a '{0}' but got '{1}'.";

        public const string Reflection_HasAttribute_Failed = "Type '{0}' does not define the [{1}] attribute.";
        public const string Reflection_HasConstructor_Failed = "Type '{0}' does not provide a constructor accepting ({1}).";
        public const string Reflection_HasPublicConstructor_Failed = "Type '{0}' does not provide a public constructor accepting ({1}).";

        public const string ValueTypes_IsNotDefault_Failed = "The param was expected to not be of default value.";
    }
}
