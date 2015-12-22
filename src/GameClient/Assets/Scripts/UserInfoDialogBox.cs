using System;
using System.Text;
using Domain.Data;
using UnityEngine.UI;

public class UserInfoDialogBox : UiDialog
{
    public Text UserIdText;
    public Text UserStatText;
    public Text UserDataText;

    public class Argument
    {
        public IUserContext UserContext;
    }

    public override void OnShow(object param)
    {
        var arg = (Argument)param;
        var uc = arg.UserContext;
        UserIdText.text = uc.Data.Name;
        UserStatText.text = string.Format(
            "<size=30>Win:</size> {0}  /  <size=30>Draw:</size> {1}  /  <size=30>Lose:</size> {2}",
            uc.Data.WinCount, uc.Data.DrawCount, uc.Data.LoseCount);
        UserDataText.text = GetUserDataText(uc);
    }

    private string GetUserDataText(IUserContext uc)
    {
        var sb = new StringBuilder();

        var data = uc.Data;
        sb.AppendLine("Data<size=28>");
        sb.AppendFormat("RegisterTime: {0}\n", data.RegisterTime);
        sb.AppendFormat("LastLoginTime: {0}\n", data.LastLoginTime);
        sb.AppendFormat("LoginCount: {0}\n", data.LoginCount);
        sb.AppendLine("</size>");

        var achivements = uc.Achivements;
        sb.AppendLine("Achievement<size=28>");
        foreach (AchievementKey key in Enum.GetValues(typeof(AchievementKey)))
        {
            UserAchievement ach;
            if (achivements.TryGetValue((int)key, out ach))
            {
                if (ach.AchieveTime.HasValue)
                    sb.AppendFormat("{0}: Achieved({1})\n", key, ach.AchieveTime.Value);
                else
                    sb.AppendFormat("{0}: Progress({1})\n", key, ach.Value);
            }
        }
        sb.AppendLine("</size>");

        return sb.ToString();
    }
}
