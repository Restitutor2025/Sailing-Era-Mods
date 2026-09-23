using Il2CppInterop.Runtime;
using Il2CppClient.WorldLogic.Entity.Component.Boat;
using GameEntity=Il2CppClient.WorldLogic.Entity.Entity;
namespace Restitutor.Cheats.Battle;

// 1.1.0: Il2Cpp-only access, kept apart from BattleRuntime so the managed tests can stub it.
// No Harmony hooks: components are found by walking the entity's own component list once per battle,
// and instant boarding uses the game's own BoardShootingBegin delegate field (never assigned by the game —
// GameAssembly scan 2026-09-23, handoff/CHEAT_BATTLE_TOGGLES_REVIEW.md).
internal static class GameLinks {
    private static Il2CppSystem.Action? handler; // one il2cpp delegate for the whole session, kept alive here

    private static T? Find<T>(GameEntity? entity) where T:Il2CppInterop.Runtime.InteropTypes.Il2CppObjectBase {
        var list=entity?._comps;
        if(list==null)return null;
        for(int i=0;i<list.Count;i++) {
            var found=list[i]?.TryCast<T>();
            if(found!=null)return found;
        }
        return null;
    }
    internal static BoatEntityHitHandler? HitHandler(GameEntity? entity)=>Find<BoatEntityHitHandler>(entity);
    internal static BoatEntityBoardShoot? BoardShoot(GameEntity? entity)=>Find<BoatEntityBoardShoot>(entity);

    internal static void Subscribe(BoatEntityBoardShoot shoot,Action callback) {
        handler??=DelegateSupport.ConvertDelegate<Il2CppSystem.Action>(callback);
        var current=shoot.BoardShootingBegin;
        shoot.BoardShootingBegin=current==null ? handler :
            Il2CppSystem.Delegate.Combine(current,handler).Cast<Il2CppSystem.Action>();
    }
    internal static void Unsubscribe(BoatEntityBoardShoot shoot) {
        var current=shoot.BoardShootingBegin;
        if(current==null || handler==null)return;
        if(current.Pointer==handler.Pointer) { shoot.BoardShootingBegin=null; return; }
        shoot.BoardShootingBegin=Il2CppSystem.Delegate.Remove(current,handler)?.Cast<Il2CppSystem.Action>();
    }
}
