public static class Version
{
    public static int MajorVersion { get; private set; }
    public static int MinorVersion { get; private set; }
    public static int BuildNumber { get; private set; }

    public static string CommitId { get; private set; }

    public static string VersionString
    {
        get { return string.Format("{0}.{1}.{2}", MajorVersion, MinorVersion, BuildNumber); }
    }

    static Version()
    {
        MajorVersion = 1;
        MinorVersion = 0;
        BuildNumber = 0;
        CommitId = "";

        TryToGetInformationFromUnityCloudBuildManifest();
    }

    private static void TryToGetInformationFromUnityCloudBuildManifest()
    {
        var manifest = UnityCloudBuildManifest.Load();
        if (manifest != null)
        {
            var buildNumber = 0;
            if (int.TryParse(manifest.buildNumber, out buildNumber))
                BuildNumber = buildNumber;

            CommitId = manifest.scmCommitId ?? "";
        }
    }
}
