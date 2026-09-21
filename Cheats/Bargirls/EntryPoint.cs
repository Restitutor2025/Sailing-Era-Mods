using MelonLoader;
using Il2CppClient.Manager;
using Il2CppClient.PlayerStore;
using Il2CppClient.UILogic.UIDrunkery;
using Il2CppClient.UILogic.UIBarGirl;
using Il2CppGyyx.Template;
using Il2CppFairyGUI;
using Restitutor.Cheats.Interface;
using SceneManager=Il2CppCore.SceneSystem.SceneManager;
[assembly: MelonInfo(typeof(Restitutor.Cheats.Bargirls.EntryPoint),"Restitutor Cheats Bargirls","1.1.3","Restitutor")]
[assembly: MelonGame("bolingo","SailingEra")]
[assembly: MelonAdditionalDependencies("Restitutor_Cheats_Interface")]
namespace Restitutor.Cheats.Bargirls;
public sealed class EntryPoint:MelonMod {
    private static bool loaded,applying;
    private static MelonLogger.Instance log=null!;
    private static readonly BargirlsPanel panel=new();
    internal static string Reason="여급이 있는 술집에서 사용 가능";
    internal sealed record Target(IntPtr Player,int City,int Role,PlayerSocialDB DB,PlayerSocialData? Data,int First,int Second,int Third);
    public override void OnInitializeMelon(){log=LoggerInstance;loaded=true;Host.Register(panel);log.Msg("Cheats Bargirls 1.1.3 loaded (panel writes only on change); no native hooks.");}
    private static bool Visible(bool open,GObject? content) {
        if(!open || content==null || content.isDisposed || !content.onStage)return false;
        for(int depth=0;content!=null;content=content.parent,depth++)
            if(depth>=128 || content.isDisposed || !content.internalVisible || !content.internalVisible2)return false;
        return true;
    }
    internal static bool IsInCity() {
        var p=Host.Player;var scene=SceneManager.Instance;
        if(!loaded || !Host.Enabled || p==null || PlayerDataManager.Instance?.Data?.Pointer!=p.Pointer || scene==null || !scene.IsSceneEntered || scene.IsInLoadingOrStarting || !scene.IsInHarborScene || p.PlayerPort?.IsStayInPort!=true)return false;
        return true;
    }
    internal static Target? Resolve() {
        Reason="여급이 있는 술집에서 사용 가능";
        if(!IsInCity())return null;
        var p=Host.Player!;
        int city=p.PlayerPort.StayInPortId;
        var port=TemplateManager.GetPort(city);int role=0;
        if(port?.npc!=null)foreach(int id in port.npc) {
            var npc=TemplateManager.GetPortFacilityNPC(id);
            if(npc==null || TemplateManager.GetBarmaid(npc.roleId)==null)continue;
            if(role!=0 && role!=npc.roleId){Reason="여급 대상이 여러 명입니다";return null;}
            role=npc.roleId;
        }
        if(role==0){Reason="이 도시에 여급이 없습니다";return null;}
        var opened=UIManager.Instance?._alreadyOpenedUICtrls;bool tavern=false;
        if(opened!=null)foreach(var entry in opened) {
            var ctrl=entry.Value;
            var bar=ctrl?.TryCast<UIDrunkeryCtrl>();
            if(bar?.View?._state!=null && bar.Model?.HarborId==city && Visible(bar.View.IsOpen(),bar.View.UIContent) && bar.Model.ListRole!=null)
                foreach(var npc in bar.Model.ListRole)if(npc?.RoleId==role)tavern=true;
            var girl=ctrl?.TryCast<UIBarGirlCtrl>();
            if(girl?.View?._state!=null && girl.Model?.CurBarGirl==role && Visible(girl.View.IsOpen(),girl.View.UIContent))tavern=true;
        }
        if(!tavern){Reason="술집 안에서만 사용 가능";return null;}
        if(p.SocialData==null)return null;
        if(!int.TryParse(TemplateManager.GetGameConst("BARMAID_FIRST_HEART")?.value,out int first) || !int.TryParse(TemplateManager.GetGameConst("BARMAID_SECOND_HEART")?.value,out int second) || !int.TryParse(TemplateManager.GetGameConst("BARMAID_THIRD_HEART")?.value,out int third))return null;
        second=checked(first+second);third=checked(second+third);
        var data=p.SocialData.FindSocialRole(role);
        if(!Rules.Valid(data?.Favorability??0,first,second,third)){Reason="애정 데이터 범위 확인 필요";return null;}
        return new(p.Pointer,city,role,p.SocialData,data,first,second,third);
    }
    internal static bool CanRaise(Target t,out int target,out string reason) {
        target=t.Data?.Favorability??0;
        reason="";
        if(Rules.Stage(target,t.First,t.Second,t.Third)>=3){reason="최대 호감도 · 3단계";return false;}
        var player=Host.Player;
        if(player?.Pointer!=t.Player || player.PlayerTalk?.DoneTalkParts==null || player.PlayerTask?.DoingTasks==null){reason="퀘스트 데이터 준비 중";return false;}
        // Match the NPC owner first. Never use a global 'any quest active' test.
        foreach(var npc in TemplateManager.PortFacilityNPCValues) {
            if(npc==null || npc.roleId!=t.Role || npc.topicId==null)continue;
            foreach(int id in npc.topicId) {
                if(id==0)continue;
                var topic=TemplateManager.GetTopic(id);
                if(topic==null){reason="여급 퀘스트 정보 확인 필요";return false;}
                if(topic.topicType!=8)continue;
                bool started=topic.openTalkpart>0 && (player.PlayerTalk.DoneTalkParts.Contains(topic.openTalkpart) || player.PlayerTalk.CurrentTalkPartId==topic.openTalkpart);
                bool finished=topic.finalEndTalkpart>0 && player.PlayerTalk.DoneTalkParts.Contains(topic.finalEndTalkpart);
                if(Rules.QuestBlocks(t.Role,npc.roleId,started,finished)) {reason="이 여급의 퀘스트 진행 중";return false;}
            }
        }
        // Also cover conventional tasks which explicitly carry this NPC's role ID.
        foreach(var task in player.PlayerTask.DoingTasks) {
            var template=task==null?null:TemplateManager.GetTask(task.TaskId);
            if(template!=null && Rules.QuestBlocks(t.Role,template.roleId,true,false)){reason="이 여급의 퀘스트 진행 중";return false;}
        }
        if(!Rules.TryRaise(target,t.Data?.CompleteTask??0,t.First,t.Second,t.Third,out target)) {
            reason="이 여급의 단계 해금 과제 완료 필요";return false;
        }
        return true;
    }
    internal static void Apply(Target? shown) {
        if(applying || shown==null)return;
        try {
            applying=true;var t=Resolve();
            if(t==null || t.Player!=shown.Player || t.City!=shown.City || t.Role!=shown.Role){panel.Message("대상이 변경되었습니다. 다시 선택하세요.");return;}
            // A stale click must not advance another stage or bypass a newly accepted quest.
            if(!CanRaise(t,out int target,out string reason)){panel.Message(reason);return;}
            int before=t.Data?.Favorability??0;
            var data=t.Data;
            if(data==null){if(!t.DB.AddSocialRole(t.Role))throw new InvalidOperationException("AddSocialRole failed");data=t.DB.FindSocialRole(t.Role);}
            if(data==null || data.Favorability!=before || data.FavorabilityUpperLimit<target)throw new InvalidOperationException("Social data changed or limit mismatch");
            int progress=data.CompleteTask;
            if(!t.DB.UpdateFavorability(t.Role,checked(target-before)))throw new InvalidOperationException("UpdateFavorability failed");
            if(data.Favorability!=target || data.CompleteTask!=progress)throw new InvalidOperationException("Target/progress mismatch; no retry");
            int stage=Rules.Stage(target,t.First,t.Second,t.Third);
            panel.Message($"{stage}단계로 상승했습니다");log.Msg($"Bargirl city={t.City} role={t.Role} affection={before}->{target}; quest progress unchanged={progress}");
        }catch(Exception ex){log.Error(ex.ToString());panel.Message("적용 오류 · 로그 확인");}
        finally{applying=false;}
    }
    public override void OnDeinitializeMelon(){loaded=false;Host.Unregister(panel);}
}

