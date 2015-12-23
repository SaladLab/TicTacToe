using System;
using UnityEngine;

[Serializable]
public class UnityCloudBuildManifest
{
    public string scmCommitId;
    public string scmBranch;
    public string buildNumber;
    public string buildStartTime;
    public string projectId;
    public string bundleId;
    public string unityVersion;
    public string xcodeVersion;
    public string cloudBuildTargetName;

    public static UnityCloudBuildManifest Load()
    {
        var manifest = (TextAsset)Resources.Load("UnityCloudBuildManifest.json");
        if (manifest == null)
            return null;

        return JsonUtility.FromJson<UnityCloudBuildManifest>(manifest.text);
    }
}
