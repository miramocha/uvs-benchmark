#if UNITY_EDITOR
using UnityEditor.PackageManager;
#endif

namespace Miraluna.Uvs.Benchmarks.Tests
{
    public static class UvsPackageProbe
    {
        public const string PackageName = "com.unity.visualscripting";

        public static string VersionLabel { get; private set; } = "unknown";
        public static string SourceLabel { get; private set; } = "unknown";

        public static void Refresh()
        {
#if UNITY_EDITOR
            var info = PackageInfo.FindForAssetPath($"Packages/{PackageName}");
            if (info == null)
            {
                VersionLabel = "missing";
                SourceLabel = "missing";
                return;
            }

            VersionLabel = string.IsNullOrEmpty(info.version) ? "unknown" : info.version;
            SourceLabel = info.source switch
            {
                PackageSource.Embedded => "embedded",
                PackageSource.Git => "git",
                PackageSource.Local => "local",
                PackageSource.Registry => "registry",
                PackageSource.BuiltIn => "builtin",
                _ => info.source.ToString().ToLowerInvariant(),
            };
#else
            VersionLabel = "player";
            SourceLabel = "player";
#endif
        }
    }
}
