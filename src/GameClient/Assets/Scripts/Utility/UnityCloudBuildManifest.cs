using System;
using UnityEngine;

[Serializable]
public class UnityCloudBuildManifest
{
#pragma warning disable SA1307 // Accessible fields must begin with upper-case letter

    public string scmCommitId;
    public string scmBranch;
    public string buildNumber;
    public string buildStartTime;
    public string projectId;
    public string bundleId;
    public string unityVersion;
    public string xcodeVersion;
    public string cloudBuildTargetName;

 #pragma warning restore SA1307 // Accessible fields must begin with upper-case letter

    public static UnityCloudBuildManifest Load()
    {
        var manifest = (TextAsset)Resources.Load("UnityCloudBuildManifest.json");
        if (manifest == null)
            return null;

        return JsonUtility.FromJson<UnityCloudBuildManifest>(manifest.text);
    }
}
