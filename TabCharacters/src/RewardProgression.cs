using Il2CppClient.UILogic.UIHeroLevelUp;
using Il2CppFairyGUI;
namespace Restitutor.TabCharacters;
public sealed partial class EntryPoint
{
    static UIHeroLevelUpCtrl? rewardOwner;
    static GComponent? rewardLabel;
    static bool rewardTouchable;
    void InstallRewardProgression()
    {
        foreach(var name in new[]{"OnClickBtnLevelUp","OnClickBtnLevelUpFive","OnAction_A","OnAction_X"})
            Patch(typeof(UIHeroLevelUpCtrl),name,Type.EmptyTypes,nameof(AdvancePastReward));
        Patch(typeof(UIHeroLevelUpCtrl),"ChangeRewardSkillState",Type.EmptyTypes,nameof(RewardBefore),nameof(RewardAfter));
    }
    static void RewardBefore(){}
    static void RewardAfter(UIHeroLevelUpCtrl __instance)
    {
        if(!Allowed)return;
        var view=__instance._View_k__BackingField;
        if(view==null||!view._model.IsRewardSkill)return;
        ClearRewardInput();rewardOwner=__instance;rewardLabel=view.ComSkillLabel;
        rewardTouchable=rewardLabel.touchable;rewardLabel.touchable=false;
    }
    static void AdvancePastReward(UIHeroLevelUpCtrl __instance)
    {
        if(!Allowed)return;
        var view=__instance._View_k__BackingField;
        if(view==null||!view.IsInputActive)return;
        var model=view._model;
        // Never bypass an unfinished stat/experience transaction.
        if(!model.IsRewardSkill||model.BanTouch||model.ResultAniCount!=0)return;
        var notice=view.ComSkillLabel.AniEnter;
        if(!notice.playing)return;
        // Invoke the original completion: clears reward/special flags and hides
        // this notice. It does not award points or debit experience again.
        notice.Stop(true,true);
        TickRewardInput();
    }
    static void TickRewardInput()
    {
        if(rewardOwner==null)return;
        var view=rewardOwner._View_k__BackingField;
        if(view==null||!view.IsInputActive||!view._model.IsRewardSkill||rewardLabel==null||rewardLabel.isDisposed)
            ClearRewardInput();
    }
    static void ClearRewardInput()
    {
        if(rewardLabel!=null&&!rewardLabel.isDisposed)rewardLabel.touchable=rewardTouchable;
        rewardOwner=null;rewardLabel=null;
    }
}
