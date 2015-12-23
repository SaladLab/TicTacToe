using UnityEngine;
using UnityEngine.UI;

public class AppPanel : MonoBehaviour
{
    public Text VersionText;

    private void Start()
    {
        var commitId = string.IsNullOrEmpty(Version.CommitId)
                           ? ""
                           : "(" + Version.CommitId + ")";
        VersionText.text = "Ver: " + Version.VersionString + " " + commitId;
    }
}
