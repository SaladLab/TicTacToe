using DG.Tweening;

public static class TweenHelper
{
    public static void KillAllTweensOfObject(object target)
    {
        var tweens = DOTween.TweensByTarget(target);
        if (tweens != null)
        {
            foreach (var tween in tweens)
                tween.Kill();
        }
    }
}
