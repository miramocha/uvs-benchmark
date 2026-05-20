using UnityEngine;
using UnityEngine.PackageManager;

namespace Miraluna.Uvs.Benchmarks
{
    public static class UvsPackageProbe
    {
        public const string PackageName = "com.unity.visualscripting";

        public static string VersionLabel { get; private set; } = "unknown";
        public static string SourceLabel { get; private set; } = "unknown";

        public static void Refresh()
        {
            var info = PackageInfo.FindForAssetPath($"Packages/{PackageName}");
            if (info == null)
            {
                VersionLabel = "missing";
                SourceLabel = "missing";
                return;
            }

            VersionLabel = string.IsNullOrEmpty(info.version) ? "unknown" : info.version;
            SourceLabel = string.IsNullOrEmpty(info.source) ? "unknown" : info.source.ToLowerInvariant();
        }
    }
}
