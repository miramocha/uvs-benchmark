using UnityEngine;

namespace Unity.VisualScripting
{
    public static class PlatformUtility
    {
        public static readonly bool supportsJit;

        static PlatformUtility()
        {
            supportsJit = CheckJitSupport();
        }

        private static bool CheckJitSupport()
        {
#if UNITY_EDITOR
            return true;
#elif ENABLE_IL2CPP
            return false;
#elif UNITY_IOS || UNITY_TVOS || UNITY_VISIONOS || UNITY_SWITCH || UNITY_PS4 || UNITY_PS5 || UNITY_XBOXONE || UNITY_GAMECORE || UNITY_WEBGL
            return false;
#else
            return true;
#endif
        }

        public static bool IsEditor(this RuntimePlatform platform)
        {
            return
                platform == RuntimePlatform.WindowsEditor ||
                platform == RuntimePlatform.OSXEditor ||
                platform == RuntimePlatform.LinuxEditor;
        }

        public static bool IsStandalone(this RuntimePlatform platform)
        {
            return
                platform == RuntimePlatform.WindowsPlayer ||
                platform == RuntimePlatform.OSXPlayer ||
                platform == RuntimePlatform.LinuxPlayer;
        }
    }
}
