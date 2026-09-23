using Il2Cpp;
using Il2CppClient.Battle;
using Il2CppClient.Manager;
using Il2CppClient.WorldLogic.Entity.Data;
using Il2CppClient.WorldLogic.Entity.Component.Boat.BoatAttack;
using Il2CppClient.WorldLogic.Scenes;
using Restitutor.Cheats.Interface;
using SceneManager=Il2CppCore.SceneSystem.SceneManager;
namespace Restitutor.Cheats.Battle;

internal static class BattleRuntime {
    internal static readonly Selection Choice=new();
    internal static readonly Toggles Toggle=new();
    internal static bool Active { get; private set; }
    private static IntPtr oceanId,playerId;
    private static long focusId;
    private static ClientBattle? melee;
    private static readonly Dictionary<IntPtr,UnitLease> units=new();
    private static readonly Dictionary<IntPtr,ShotLease> shots=new();
    private static string lastError="";
    // 1.1.0 checkboxes: one lease per battle on the controlled ship (= flagship), no hooks, no per-frame search.
    private static HullLease? hull;
    private static BoardLink? board;
    private static IntPtr hullMissing,boardMissing; // entity already searched without result (search once)
    private static bool linkFault;                  // an exception in the link code: stay off until the battle ends
    private static volatile bool boardPending;      // set by the game's BoardShootingBegin delegate

    private static OceanScene? Resolve() {
        var p=Host.Player;var scene=SceneManager.Instance;
        if(!EntryPoint.Loaded || !Host.Enabled || p==null || PlayerDataManager.Instance?.Data?.Pointer!=p.Pointer ||
            scene==null || !scene.IsSceneEntered || scene.IsInLoadingOrStarting || !scene.IsInOceanScene ||
            p.PlayerPort?.IsStayInPort!=false)return null;
        var ocean=scene.CurScene()?.TryCast<OceanScene>();
        if(ocean==null || !ocean.IsInBattle || ocean._focusTeam?.isPlayer!=true)return null;
        var boat=ocean.GetFocusPlayer()?.Data?.TryCast<BoatEntityData>();
        return boat?.IsPlayer==true && boat.Guid!=0 ? ocean : null;
    }
    internal static void Update() {
        // 1.0.1: nothing to track or release outside the ocean scene; skip the per-frame lookups.
        // 1.1.0: the X1..X5 selection is kept between battles, so it no longer blocks this early exit.
        if(!Active && melee==null && units.Count==0 && shots.Count==0 && oceanId==IntPtr.Zero &&
            hull==null && board==null) {
            var s=SceneManager.Instance;
            if(s==null || !s.IsInOceanScene) return;
        }
        try {
            var ocean=Resolve();
            if(ocean==null) {
                Active=false;
                // A transient read/loading/pause is not an encounter boundary.
                // Native end/scene-exit hooks and Host session reset are authoritative.
                var scene=SceneManager.Instance;
                if(EntryPoint.Loaded && Host.Enabled && Host.Player!=null &&
                    PlayerDataManager.Instance?.Data?.Pointer==Host.Player.Pointer &&
                    scene!=null && scene.IsSceneEntered && !scene.IsInLoadingOrStarting) {
                    if(!scene.IsInOceanScene)Reset();
                    else {
                        var current=scene.CurScene()?.TryCast<OceanScene>();
                        if(current!=null && !current.IsInBattle)Reset();
                    }
                }
                return;
            }
            long id=ocean.GetFocusPlayer().Data.Guid;
            if((oceanId!=IntPtr.Zero && oceanId!=ocean.Pointer) ||
                (playerId!=IntPtr.Zero && playerId!=Host.Player!.Pointer)) {
                Reset();oceanId=ocean.Pointer;playerId=Host.Player!.Pointer;focusId=id;
            }
            if(focusId!=0 && focusId!=id)ReleaseUnits();
            oceanId=ocean.Pointer;playerId=Host.Player!.Pointer;focusId=id;
            Active=true;
            if(melee!=null && !melee.IsDone)ApplyUnits();
            SyncLinks(ocean);
        } catch(Exception ex) { Fail(ex); }
    }
    internal static void SetToggle(bool isBoard,bool on) {
        Update();Toggle.Set(isBoard,on,Active);
        Update(); // apply or release now, not next frame
    }
    // Called from the game's BeginShooting (physics trigger callback): only record it; act in Update.
    internal static void OnBoardShootingBegin() { boardPending=true; }
    private static void SyncLinks(OceanScene ocean) {
        if(linkFault)return;
        try {
            var entity=ocean.GetFocusPlayer();
            var key=entity?.Pointer ?? IntPtr.Zero;
            if(Toggle.Hull) {
                if(hull!=null && hull.Entity!=key)ReleaseHull();
                if(hull==null && key!=IntPtr.Zero && hullMissing!=key) {
                    var h=GameLinks.HitHandler(entity);
                    if(h==null){hullMissing=key;Note("hull lock: controlled ship has no hit handler; not applied");}
                    else {hull=new HullLease(h,key);hull.Apply();Note("hull lock ON (controlled ship; was "+hull.Original+")");}
                }
            } else if(hull!=null)ReleaseHull();
            if(Toggle.Board) {
                if(board!=null && board.Entity!=key)ReleaseBoard();
                if(board==null && key!=IntPtr.Zero && boardMissing!=key) {
                    var data=entity!.Data?.TryCast<BoatEntityData>();
                    var s=data?.IsPlayerFlagship==true ? GameLinks.BoardShoot(entity) : null;
                    if(s==null){boardMissing=key;Note("instant boarding: controlled ship is not the flagship or has no boarding component; not applied");}
                    else {board=new BoardLink(s,key);GameLinks.Subscribe(s,OnBoardShootingBegin);Note("instant boarding ON (flagship BoardShootingBegin subscribed)");}
                }
            } else if(board!=null)ReleaseBoard();
            if(boardPending) {
                boardPending=false;
                if(board!=null && Toggle.Board && melee==null)board.Enter();
            }
        } catch(Exception ex) {
            linkFault=true;ReleaseLinks();
            EntryPoint.Log?.Error("Battle checkboxes suspended until this sea battle ends: "+ex);
        }
    }
    private static void Note(string message)=>EntryPoint.Log?.Msg(message);
    private static void ReleaseHull() {
        var h=hull;hull=null;
        if(h==null)return;
        try{h.Restore();Note("hull lock OFF (restored "+h.Original+")");}catch(Exception ex){Log(ex);}
    }
    private static void ReleaseBoard() {
        var b=board;board=null;boardPending=false;
        if(b==null)return;
        try{GameLinks.Unsubscribe(b.Shoot);Note("instant boarding OFF (unsubscribed)");}catch(Exception ex){Log(ex);}
    }
    private static void ReleaseLinks() {
        ReleaseHull();ReleaseBoard();boardPending=false;hullMissing=boardMissing=IntPtr.Zero;
    }
    internal static void Select(bool isMelee,int value) {
        Update();Choice.Select(isMelee,value,Active);
        try { if(isMelee && melee!=null)ApplyUnits(); }catch(Exception ex){Fail(ex);}
    }
    internal static void Bind(MeleeBattleController controller) {
        try {
            Update();ReleaseUnits();
            if(!Active)return;
            var battle=controller._Battle;
            var focus=Resolve()?.GetFocusPlayer()?.Data?.TryCast<BoatEntityData>();
            if(battle==null || battle.IsDone || battle.PlayerShip==null || focus?.baseShipData==null ||
                battle.PlayerShip.ShipGuid!=focus.baseShipData.ShipGuid)return;
            melee=battle;ApplyUnits();
        }catch(Exception ex){Fail(ex);}
    }
    internal static void BeforeMelee(MeleeBattleController controller) {
        try {
            Update();
            if(melee!=null && controller._Battle?.Pointer==melee.Pointer && !melee.IsDone)ApplyUnits();
        }catch(Exception ex){Fail(ex);}
    }
    internal static void EndMelee(MeleeBattleController controller) {
        // Ending the boarding encounter is not ending the encompassing sea battle.
        // Keep BOTH user selections until the sea battle really ends.
        if(melee!=null && controller._Battle?.Pointer==melee.Pointer)ReleaseUnits();
    }
    private static void ApplyUnits() {
        var list=melee?.PlayerUnits;if(list==null)return;
        foreach(var unit in list) {
            if(unit==null || !unit.IsPlayerUnit || unit.IsPawn)continue;
            if(!units.TryGetValue(unit.Pointer,out var lease)) {
                if(units.Count>=256)throw new InvalidOperationException("Unexpected navigator count");
                lease=new UnitLease(unit);units.Add(unit.Pointer,lease);
            }
            lease.Apply(Choice.Melee);
        }
    }
    internal static void Cannon(BoatEntityGun gun,AttackInfo info) {
        try {
            Update();if(!Active || info==null || info.isSiege || gun.boatData?.IsPlayer!=true ||
                gun.boatData.Guid!=focusId || info.sourceEntityGuid!=focusId)return;
            if(shots.TryGetValue(info.Pointer,out var old) && old.Id==info.attackGuid)return;
            // Reused pooled object: the new attack starts a fresh lease, never restores the old one.
            if(!shots.ContainsKey(info.Pointer) && shots.Count>=4096)throw new InvalidOperationException("Unexpected active shot count");
            var lease=new ShotLease(info,Choice.Cannon);shots[info.Pointer]=lease;lease.Apply();
        }catch(Exception ex){Fail(ex);}
    }
    private static void ReleaseUnits() {
        foreach(var lease in units.Values)try{lease.Restore();}catch(Exception ex){Log(ex);}
        units.Clear();melee=null;
    }
    // End of a sea battle (and scene exit): every applied effect is restored/released, but the user's choices
    // (X1..X5 multipliers and checkboxes) are kept for the next battle (1.1.0, user decision 2026-09-23).
    internal static void Reset() {
        Active=false;ReleaseUnits();
        foreach(var lease in shots.Values)try{lease.Restore();}catch(Exception ex){Log(ex);}
        shots.Clear();oceanId=playerId=IntPtr.Zero;focusId=0;
        ReleaseLinks();linkFault=false;
    }
    // Session reset (save load, title, O off, panel error, unload): choices back to X1 / unchecked.
    internal static void ResetSession() { Reset();Choice.Reset();Toggle.Reset(); }
    private static void Fail(Exception ex) {
        Active=false;ReleaseUnits();ReleaseLinks();
        foreach(var lease in shots.Values)try{lease.Restore();}catch(Exception restore){Log(restore);}
        shots.Clear();Log(ex); // suspend effects, never change the user's selection on an error
    }
    private static void Log(Exception ex) {
        if(lastError==ex.Message)return;lastError=ex.Message;
        EntryPoint.Log?.Error("Battle cheat effect suspended; selection retained: "+ex);
    }
    private sealed class HullLease {
        private readonly Il2CppClient.WorldLogic.Entity.Component.Boat.BoatEntityHitHandler handler;
        internal readonly IntPtr Entity;
        internal bool Original { get; private set; }
        internal HullLease(Il2CppClient.WorldLogic.Entity.Component.Boat.BoatEntityHitHandler h,IntPtr entity){handler=h;Entity=entity;}
        internal void Apply(){Original=handler.lockHealth;handler.lockHealth=true;}
        // Only undo our own write: if it was already locked, or someone cleared it since, leave it.
        internal void Restore(){if(!Original && handler.lockHealth)handler.lockHealth=false;}
    }
    private sealed class BoardLink {
        internal readonly Il2CppClient.WorldLogic.Entity.Component.Boat.BoatEntityBoardShoot Shoot;
        internal readonly IntPtr Entity;
        internal BoardLink(Il2CppClient.WorldLogic.Entity.Component.Boat.BoatEntityBoardShoot s,IntPtr entity){Shoot=s;Entity=entity;}
        // Original path: AddProgress -> CheckProgressFull -> ShootingProgressIsFull -> StartMeleeBattleForPlayer.
        internal void Enter() {
            int need=BoardRule.Needed(Shoot.ShootProgress,Shoot.targetEnemy?.ShootProgress ?? 0);
            if(need>0){Shoot.AddProgress(need);Note("instant boarding: +"+need+" progress");}
        }
    }
    private sealed class UnitLease {
        private readonly CombatUnit unit;
        private readonly StatLease attack,craft,perception,physical;
        internal UnitLease(CombatUnit u) {
            unit=u;attack=new(u.Attack);craft=new(u.Craft);perception=new(u.Perception);physical=new(u.Physical);
        }
        internal void Apply(int n) {
            unit.Attack=attack.Apply(unit.Attack,n);unit.Craft=craft.Apply(unit.Craft,n);
            unit.Perception=perception.Apply(unit.Perception,n);unit.Physical=physical.Apply(unit.Physical,n);
        }
        internal void Restore() {
            unit.Attack=attack.Restore(unit.Attack);unit.Craft=craft.Restore(unit.Craft);
            unit.Perception=perception.Restore(unit.Perception);unit.Physical=physical.Restore(unit.Physical);
        }
    }
    private sealed class ShotLease {
        private readonly AttackInfo info;
        internal readonly long Id;
        private readonly float original,applied;
        internal ShotLease(AttackInfo value,int n) {
            info=value;Id=value.attackGuid;original=value.gunDamageFactor;applied=original*n;
            if(!float.IsFinite(applied) || original<0)throw new InvalidOperationException("Invalid cannon factor");
        }
        internal void Apply(){info.gunDamageFactor=applied;}
        internal void Restore(){if(info.attackGuid==Id && info.gunDamageFactor==applied)info.gunDamageFactor=original;}
    }
}
