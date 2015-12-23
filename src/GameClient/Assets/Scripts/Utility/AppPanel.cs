using UnityEngine;
using UnityEngine.UI;

public class AppPanel : MonoBehaviour
{
    public Text VersionText;

    private void Start()
    {
        var commitId = string.IsNullOrEmpty(Version.CommitId)
                           ? ""
                           : "(" + Version.CommitId.Substring(0, 6) + ")";
        VersionText.text = "Ver: " + Version.VersionString + " " + commitId;
    }
}
