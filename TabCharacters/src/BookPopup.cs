using Il2CppClient.Manager;
using Il2CppClient.PlayerStore;
using Il2CppClient.UILogic.UICharacter;
using Il2CppCore.InputSystem;
using Il2CppFairyGUI;
using UnityEngine;
using UnityEngine.InputSystem;
using Books = Il2CppSystem.Collections.Generic.List<Il2CppClient.UILogic.UICharacter.SkillBook>;

namespace Restitutor.TabCharacters;

public sealed partial class EntryPoint
{
    static partial void TraceUi(string reason, bool begin=false);
    static partial void TickUiTrace();
    static partial void ClearUiTrace();
    // 0.6.7 diagnostics (LanguageTrace.cs): language click path is unlogged; see docs 0.6.7.md.
    static partial void LangTrace(string text);
    static partial void LangBindTrace(UICharacterView view);
    static partial void TickLangProbe();
    // 0.6.8 fix (LanguageCatcher.cs): btnReturn lies above the language row and takes its presses.
    static partial void BindLanguageCatcher(UICharacterView view);
    static partial void UnbindLanguageCatcher();
    // 0.6.9 (LanguageCatcher.cs): language book details need roleInfo.listLanguage prepared like the retired skill sheet.
    static partial void LanguagePreviewBegin(UICharacterView view, PlayerRoleData role);
    static partial void LanguagePreviewEnd();
    private static BookPopup? books;
    private static bool BooksOpen => books != null;
    private static readonly Dictionary<IntPtr, BookBinding> bookBindings = new();
    private static readonly HashSet<string> bookHeldActions = new();
    private static bool bookFault;

    partial void InstallSkillTab();
    // 0.6.0 equipment panel (EquipPanel*.cs). Optional partials keep the popup test
    // suite independent of the equipment code.
    partial void InstallEquip();
    static partial void BindEquipSlots(UICharacterView view);
    static partial void ClearEquipSlotState();
    static partial void EquipCloseForBooks(string reason);
    static partial void ResetEquip();
    static partial void TickEquip();
    static partial void EquipCapture(InputAction.CallbackContext context, ref bool handled, ref bool result);
    static partial void EquipRowRendered(UICharacterView view, int index, GObject row);
    private void InstallBooks()
    {
        InstallRewardProgression();
        InstallSkillTab();
        InstallEquip();
        Patch(typeof(UICharacterView), "RefreshTipsSkill", new[]{typeof(PlayerRoleData)}, nameof(BookNativeTips));
        Patch(typeof(UICharacterView), "RenderListBtnSkill", new[] { typeof(int), typeof(GObject) }, nameof(BookRenderBefore), nameof(BookRenderAfter));
        Patch(typeof(UICharacterCtrl), "OnClickBtnBook", Type.EmptyTypes, nameof(BookUse));
        Patch(typeof(InputSystemManager), "OnEventCaptureInput", new[] { typeof(InputAction.CallbackContext) }, nameof(BookCapture));
        Patch(typeof(GameManager), "ForceReset", Type.EmptyTypes, nameof(ResetBooks));
        Patch(typeof(Il2CppClient.UILogic.UITips.UITipsCtrl), "ShowBaseTips", new[]{typeof(string),typeof(string)}, nameof(BookTips),nameof(BookTipsAfter));
    }
    private static void GuardBook(Action action)
    {
        try { action(); }
        catch (Exception ex) { bookFault = true; host?.LoggerInstance.Error("Book popup stopped: " + ex); }
    }
    private static bool BookNativeTips(UICharacterView __instance) => books == null || books.View.Pointer != __instance.Pointer || !books.HoldsNativeInfo || books.RefreshingNativeInfo;
    private static void BookRenderBefore(UICharacterView __instance, int __0, GObject __1)
    {
        GuardBook(() => { if (bookBindings.Remove(__1.Pointer, out var binding)) binding.Dispose(); });
    }
    private static void BookRenderAfter(UICharacterView __instance, int __0, GObject __1)
    {
        if (!Allowed || !IsCharacter(__instance)) return;
        GuardBook(() => BindBook(__1, () => OpenBooks(__instance, __0)));
        EquipRowRendered(__instance, __0, __1);
    }
    private static void RefreshBookBindings(UICharacterView view)
    {
        if (!IsCharacter(view)) return;
        var sheet = view.SheetCharacter;
        BindBook(sheet.texLanguage, () => { LangTrace("click received: texLanguage"); OpenBooks(view, -1); });
        BindBook(sheet.TexTitleCharacterLanguage, () => { LangTrace("click received: TexTitleCharacterLanguage"); OpenBooks(view, -1); });
        LangBindTrace(view);
        BindLanguageCatcher(view);
        RefreshPointFooter(view);
        BindEquipSlots(view);
        if (books != null && books.View.Pointer == view.Pointer) books.Refresh();
    }
    private static void BindBook(GObject target, Action click)
    {
        if (target == null || target.isDisposed || bookBindings.ContainsKey(target.Pointer)) return;
        bookBindings.Add(target.Pointer, new BookBinding(target, click));
    }
    private sealed class BookBinding : IDisposable
    {
        readonly GObject target;
        readonly bool touchable;
        readonly EventCallback1 clicked, removed;
        internal BookBinding(GObject target, Action action)
        {
            this.target = target; touchable = target.touchable; target.touchable = true;
            clicked = (EventCallback1)(Action<EventContext>)(e => { e.StopPropagation(); if (Allowed) GuardBook(action); });
            removed = (EventCallback1)(Action<EventContext>)(_ => { bookBindings.Remove(target.Pointer); Dispose(); });
            target.onClick.Add(clicked); target.onRemovedFromStage.Add(removed);
        }
        public void Dispose()
        {
            if (target.isDisposed) return;
            target.onClick.Remove(clicked); target.onRemovedFromStage.Remove(removed); target.touchable = touchable;
        }
    }
    private static void ClearBookBindings()
    {
        UnbindLanguageCatcher();
        var all = bookBindings.Values.ToArray(); bookBindings.Clear();
        foreach (var binding in all) binding.Dispose();
        ClearEquipSlotState();
        ClearPointFooter();
        ClearLearningNotice();ClearRewardInput();
    }
    private static void OpenBooks(UICharacterView view, int index)
    {
        if (!view.IsInputActive || !IsCharacter(view)) { if (index < 0) LangTrace($"open skipped: inputActive={view.IsInputActive} character={IsCharacter(view)}"); return; }
        EquipCloseForBooks("learning panel opened");
        if(books!=null)CloseBooks();
        var ctrl = UICharacterCtrl.Instance;
        if (ctrl._View_k__BackingField?.Pointer != view.Pointer) { if (index < 0) LangTrace("open skipped: UICharacterCtrl view is a different view"); return; }
        var model = view._model;
        if (model.RoleIndex < 0 || model.RoleIndex >= model.ListRole.Count || model.ListRole[model.RoleIndex].isSeaman) { if (index < 0) LangTrace($"open skipped: role index={model.RoleIndex} count={model.ListRole.Count} seaman={(model.RoleIndex >= 0 && model.RoleIndex < model.ListRole.Count && model.ListRole[model.RoleIndex].isSeaman)}"); return; }
        if (index >= 0) { model.SkillIndex = index; model.ChangeDictAddItem(); }
        var skill = index < 0 ? -1 : model.GetSkillIdByIndex(index);
        books = new BookPopup(ctrl, view, skill);
        books.Open();TraceUi("opened");
        if (index < 0) LangTrace($"language panel opened: books={(books != null)}");
    }
    private static void CloseBooks([System.Runtime.CompilerServices.CallerMemberName] string reason="")
    {
        TraceUi("close:"+reason);
        var closing = books; books = null;
        if(closing!=null)host?.LoggerInstance.Msg("Characters learning panel closed: "+reason);
        try { closing?.Dispose(); }
        catch(Exception ex) { host?.LoggerInstance.Error("Book popup cleanup: "+ex); }
    }
    private static void ResetBooks() { ClearUiTrace(); ResetEquip(); CloseBooks(); ClearBookBindings(); bookHeldActions.Clear();ClearLearningNotice();ClearRewardInput();closeKey=CloseKey.None;confirmLatch=false; }
    // 0.5.8: one learn per physical press, whichever path (Action_A callback or
    // physical Space poll) sees it first. Cleared once Space and Action_A are released.
    private static bool confirmLatch;
    private static void ConfirmLearn(string source)
    {
        var popup=books;
        if(popup==null||confirmLatch)return;
        var focus=Stage.inst.focus;
        if(focus!=null&&focus.TryCast<InputTextField>()!=null){host?.LoggerInstance.Msg($"Characters learn key ignored ({source}): text input has focus");return;}
        var block=popup.LearnBlock();
        if(block!=null){host?.LoggerInstance.Msg($"Characters learn key ignored ({source}): {block}");return;}
        confirmLatch=true;
        host?.LoggerInstance.Msg($"Characters learn by {source}");
        popup.Commit();
    }
    private static void TickBooks()
    {
        TickLearningNotice();TickPointFooter();TickRewardInput();
        TickEquip();
        TickLangProbe();
        if (bookFault) { bookFault = false; CloseBooks("fault recovery; see preceding error"); }
        if (books != null && !books.Valid) CloseBooks("view/model/role/bag changed");
        if(BooksOpen&&Application.isFocused&&(Keyboard.current?.escapeKey.wasPressedThisFrame==true||Mouse.current?.rightButton.wasPressedThisFrame==true))
        {bool esc=Keyboard.current?.escapeKey.wasPressedThisFrame==true;closeKey=esc?CloseKey.Escape:CloseKey.RightMouse;CloseBooks(esc?"physical Escape":"physical right mouse");}
        if(confirmLatch&&Keyboard.current?.spaceKey.isPressed!=true&&!bookHeldActions.Contains("Action_A"))confirmLatch=false;
        // The Action_A callback alone did not fire for Space in the 0.5.7 user run
        // (18:23 log), so the physical key is polled like Escape/right mouse above.
        if(BooksOpen&&Application.isFocused&&Keyboard.current?.spaceKey.wasPressedThisFrame==true)GuardBook(()=>ConfirmLearn("physical Space"));
        if (books != null) GuardBook(()=>{books?.DrainPointRequest();books?.Layout();});
        TickUiTrace();
    }
    private static bool BookUse(UICharacterCtrl __instance)
    {
        if (books == null || books.Ctrl.Pointer != __instance.Pointer) return true;
        if (books.Committing) return true;
        GuardBook(() => books?.Commit());
        return false;
    }
    private static bool BookCapture(InputAction.CallbackContext __0)
    {
        string name = __0.action?.name ?? "";
        if (closeKey != CloseKey.None)
        {
            // Swallow only the closing key's own Action_B: its presses while held and its release.
            bool held = CloseKeyHeld();
            bool ownRelease = name == "Action_B" && (__0.canceled || (__0.performed && !__0.ReadValueAsButton()));
            if (!held) closeKey = CloseKey.None;
            if (name == "Action_B" && (held || ownRelease)) return false;
        }
        bool command = name is "Action_Start" or "Action_B" or "Action_A";
        // Axis actions need not have a scalar/button value. Never read those as buttons.
        bool released = __0.canceled || (command && __0.performed && !__0.ReadValueAsButton());
        if (bookHeldActions.Contains(name))
        {
            if (released) bookHeldActions.Remove(name);
            return false;
        }
        bool equipHandled = false, equipResult = true;
        EquipCapture(__0, ref equipHandled, ref equipResult);
        if (equipHandled) return equipResult;
        if (!BooksOpen) return true;
        if (name=="Action_B"&&__0.performed&&__0.ReadValueAsButton())
        {
            bookHeldActions.Add(name);
            GuardBook(()=>CloseBooks());return false;
        }
        // 0.5.7: confirm (Space on keyboard, per the native skill sheet footer) learns
        // the selected book only when it is learnable now. Otherwise the input keeps its
        // original path; the native character sheet ignores Action_A (OnAction_A 0x94D580).
        // The press is held so repeats/release cannot learn twice.
        if (name=="Action_A"&&__0.performed&&__0.ReadValueAsButton()&&books!=null&&!confirmLatch&&books.LearnBlock()==null)
        {
            bookHeldActions.Add(name);
            GuardBook(()=>ConfirmLearn("Action_A"));
            return false;
        }
        return true;
    }

    private sealed partial class BookPopup : IDisposable
    {
        internal readonly UICharacterCtrl Ctrl;
        internal readonly UICharacterView View;
        readonly UICharacterModel model;
        readonly int skillId, roleIndex,selectedSkillIndex;
        readonly IntPtr rolePointer, bagPointer;
        readonly Il2CppCharacter.UISheetSkill sheet;
        readonly GComponent parent;
        readonly int childIndex;
        readonly float x, y, sx, sy;
        readonly bool visible;
        readonly bool sheetTouchable;
        readonly bool filterVisible, useTouchable;
        readonly string emptyText;
        readonly GGroup? group;
        uint displayLock;
        readonly ListItemRenderer originalRenderer, renderer;
        readonly ECurFoucType focusType;
        readonly GObject? oldFocus;
        readonly List<(EventListener Listener, EventCallback1 Callback)> listeners = new();
        readonly Dictionary<IntPtr, GTextField> badges = new();
        GComponent? overlay, panel;
        GTextField? summary, heading;
        Books filtered = new();
        long selectedGuid;
        bool refreshing, disposed;
        internal bool RefreshingNativeInfo;
        internal bool HoldsNativeInfo=>pinnedNativeInfo;
        internal bool Committing;
        internal bool Valid => !disposed && View._UIContent_k__BackingField != null && !View._UIContent_k__BackingField.isDisposed && !sheet.isDisposed
            && View._model.Pointer == model.Pointer && IsCharacter(View) && model.RoleIndex == roleIndex
            && roleIndex >= 0 && roleIndex < model.ListRole.Count && model.ListRole[roleIndex].HeroData.Pointer == rolePointer
            && UICharacterCtrl.Data?.PlayerBag?.ItemBag?.Pointer == bagPointer;
        PlayerRoleData Role => model.ListRole[roleIndex].HeroData;
        internal BookPopup(UICharacterCtrl ctrl, UICharacterView view, int skill)
        {
            Ctrl = ctrl; View = view; model = view._model; skillId = skill; roleIndex = model.RoleIndex;
            selectedSkillIndex=model.SkillIndex;
            rolePointer = Role.Pointer; bagPointer = UICharacterCtrl.Data.PlayerBag.ItemBag.Pointer;
            sheet = view.SheetSkill; parent = sheet.parent ?? throw new InvalidOperationException("Native book sheet has no parent."); childIndex = parent.GetChildIndex(sheet);
            x = sheet.x; y = sheet.y; sx = sheet.scaleX; sy = sheet.scaleY; visible = sheet.visible;
            sheetTouchable=sheet.touchable;
            group=sheet.group;filterVisible=sheet.comFilter.visible;useTouchable=sheet.btnSkillUp.touchable;
            emptyText=sheet.TexNoBook.text;
            focusType = model.CurFoucType; oldFocus = GRoot.inst.focus;
            originalRenderer = sheet.listBook.itemRenderer;
            renderer = (ListItemRenderer)(Action<int, GObject>)Render;
        }
        GTextField Text(GComponent host, string text, float x, float y, float w, float h, int size)
        {
            var t = new GTextField(); t.SetXY(x, y); t.SetSize(w, h); t.touchable = false;
            t.autoSize=AutoSizeType.Height;t.singleLine=false;
            var f = t.textFormat; f.font=View.SheetCharacter.texSkillDesc.textFormat.font; f.size = size; f.color = View.SheetCharacter.texSkillDesc.textFormat.color; t.textFormat = f;
            t.text = text; host.AddChild(t); return t;
        }
        static GGraph Rect(GComponent host, float x, float y, float w, float h, Color color)
        {
            var g = new GGraph(); g.SetXY(x, y); g.DrawRect(w,h,0,Color.clear,color); host.AddChild(g); return g;
        }
        void Listen(EventListener listener, Action<EventContext> action)
        { var callback = (EventCallback1)action; listeners.Add((listener,callback)); listener.Add(callback); }
        internal void Open()
        {
            overlay = new GComponent(); overlay.SetSize(GRoot.inst.width, GRoot.inst.height); overlay.sortingOrder = 32000;
            CreateOutsideDismiss();
            infoPane=new GComponent();overlay.AddChild(infoPane);infoPane.SetupOverflow(OverflowType.Hidden);
            panel = new GComponent(); overlay.AddChild(panel);
            float w = 488, h = 560;
            panel.SetSize(w,h);
            panel.SetupOverflow(OverflowType.Hidden);
            background=Rect(panel,0,0,w,h,new Color(.12f,.14f,.15f,.94f));
            heading = Text(panel,"스킬북",24,15,270,45,28);
            closeButton = Rect(panel,364,12,100,44,new Color(.25f,.28f,.29f,1));
            closeLabel=Text(panel,"닫기",380,19,80,32,21);
            Listen(closeButton.onClick,e=> {e.StopPropagation(); GuardBook(()=>CloseBooks());});
            leftPane=new GComponent();leftPane.SetSize(440,480);leftPane.SetXY(24,80);leftPane.SetupOverflow(OverflowType.Hidden);panel.AddChild(leftPane);
            rightPane=new GComponent();rightPane.SetSize(440,480);rightPane.SetXY(504,80);rightPane.SetupOverflow(OverflowType.Hidden);panel.AddChild(rightPane);
            divider=Rect(panel,24,80,440,1,new Color(.55f,.55f,.55f,1));
            summary = Text(infoPane,"",24,24,440,200,23);
            summary.UBBEnabled=true;
            status = Text(rightPane,"",0,0,440,60,22);
            effect = Text(rightPane,"+Lv.1",340,0,100,36,22);
            CreateSkillPointControls();
            CreateLearnInteraction();
            CaptureLayout();
            sheet.group=null; displayLock=sheet.AddDisplayLock();
            panel.AddChild(sheet); Geometry(sheet,()=>{sheet.SetXY(24,80);sheet.SetScale(1,1);}); sheet.visible = true;
            sheet.touchable=false; // All interactive controls are moved into the clipped columns.
            sheet.comFilter.visible = false;
            sheet.listBook.itemRenderer = renderer;
            GRoot.inst.AddChild(overlay);
            Ctrl.InitBookData(UICharacterCtrl.Data.PlayerBag);
            Refresh();
        }
        internal void Layout()=>LayoutSidePanels();
        void UpdateExperience()
        { model.Exp = UICharacterCtrl.Data.PlayerCurrency.FindCurrency(2)?.Amount ?? 0; }
        internal void Refresh()
        {
            if (refreshing || Committing || !Valid || overlay == null) return;
            refreshing = true;
            try
            {
                UpdateExperience();
                filtered = new Books();
                var source = model.ListBook;
                for (int i=0;i<source.Count;i++)
                {
                    var book = source[i];
                    if (book?.BookTemplate != null && BookRules.Matches((int)book.BookType,book.BookTemplate.skillId,skillId) && Stock(book)>0) filtered.Add(book);
                }
                model.ListFilterBook = filtered;
                int chosen = -1;
                for (int i=0;i<filtered.Count;i++) if (filtered[i].BookGuid==selectedGuid) {chosen=i;break;}
                if (chosen<0 && filtered.Count>0) chosen=0;
                model._bookIndex=chosen;
                selectedGuid=chosen<0?0:filtered[chosen].BookGuid;
                sheet.isSeaman.SetSelectedIndex(0);
                sheet.noBook.SetSelectedIndex(chosen<0?1:0);
                Geometry(sheet.listBook,()=>sheet.listBook.SetSize(440,330));
                sheet.listBook.numItems=filtered.Count;
                sheet.listBook.selectedIndex=chosen;
                if(chosen>=0) RefreshBookDetails(filtered[chosen]);
                sheet.visible=true; sheet.comFilter.visible=false;
                sheet.texCurExp.text=model.Exp.ToString("N0");
                sheet.btnSkillUp.touchable=chosen>=0;
                if(chosen<0) sheet.TexNoBook.text="관련된 보유 책이 없습니다.";
                UpdateSummary();
                CompactLayout(chosen);
            }
            finally {refreshing=false;}
        }
        int Stock(SkillBook book)
        {
            var items=UICharacterCtrl.Data.PlayerBag.ItemBag.Items;
            for(int i=0;i<items.Count;i++)
            {var item=items[i]; if(item.Guid==book.BookGuid && item.ItemId==book.BookId && !item.IsCommerce) return Math.Max(0,item.Number);}
            return 0;
        }
        void UpdateSummary()
        {
            if(summary==null||heading==null) return;
            var character=View.SheetCharacter;
            if(skillId<0)
            {heading.text="언어 학습";summary.text=character.TexTitleCharacterLanguage.text+"\n\n"+character.texLanguage.text+"\n\n"+character.texLanguageCount.text;PrepareLanguageInfo();return;}
            int previousIndex=model.SkillIndex;
            model.SkillIndex=selectedSkillIndex;model.ChangeDictAddItem();
            var previousFocus=model.CurFoucType;
            model.CurFoucType=ECurFoucType.Skill;
            RefreshingNativeInfo=true;
            try { View.RefreshTipsSkill(Role); }
            finally { RefreshingNativeInfo=false;model.CurFoucType=previousFocus;model.SkillIndex=previousIndex;model.ChangeDictAddItem(); }
            heading.text="스킬 학습";
            summary.visible=false;
            PrepareNativeInfo();
        }
        void Render(int index,GObject obj)
        {
            GuardBook(()=>
            {
                if(index<0||index>=filtered.Count||disposed)return;
                View.RenderListBook(index,obj);
                var book=filtered[index];
                var nativeSlot=obj.TryCast<Il2CppCharacter.UIBtn_SkillBook>();
                if(nativeSlot!=null)nativeSlot.isSelect.SetSelectedIndex(book.BookGuid==selectedGuid?1:0);
                if(!badges.TryGetValue(obj.Pointer,out var label)||label.isDisposed)
                {label=CreateCountBadge(obj.TryCast<GComponent>()!);badges[obj.Pointer]=label;}
                PlaceCountBadge(label,nativeSlot?.loaderItem,obj,Stock(book));
                var callback=(EventCallback1)(Action<EventContext>)(e=>
                {e.StopPropagation();GuardBook(()=>{if(!Valid)return;selectedGuid=book.BookGuid;Refresh();});});
                // Native renderer owns this slot's click callback and reassigns it each render.
                obj.onClick.Set(callback);
                obj.onRollOver.Clear(); // Native hover assumes the old skill sheet's selection contract.
                slotCallbacks[obj.Pointer]=callback;
                slotObjects[obj.Pointer]=obj;
            });
        }
        readonly Dictionary<IntPtr,EventCallback1> slotCallbacks=new();
        readonly Dictionary<IntPtr,GObject> slotObjects=new();
        // null = the selected book can be learned now; otherwise a log-friendly reason.
        internal string? LearnBlock()
        {
            if(!Valid)return "panel no longer valid";
            if(Committing||refreshing)return "panel busy";
            // The popup's own selection (GUID) is authoritative; Commit refreshes and
            // re-derives the native book index before using it.
            if(selectedGuid==0)return "no book selected";
            for(int i=0;i<filtered.Count;i++)
            {
                var book=filtered[i];
                if(book.BookGuid!=selectedGuid)continue;
                if(Stock(book)<=0)return "selected book out of stock";
                int state=Ctrl.GetBookStateByParam(book,Role);
                return state==0?null:$"native book state {state} (1 used/known, 2 language slots full, 3 max level, 4 condition/exp)";
            }
            return "selected book not in list";
        }
        internal void Commit()
        {
            if(!Valid||Committing)return;
            long intendedGuid=selectedGuid;
            Refresh();
            if(!Valid||selectedGuid!=intendedGuid)return; // Never substitute a different book after inventory changes.
            int index=model._bookIndex;
            if(index<0||index>=filtered.Count)return;
            var book=filtered[index];
            if(!BookRules.CanCommit(rolePointer.ToInt64(),Role.Pointer.ToInt64(),book.BookGuid,Stock(book),Ctrl.GetBookStateByParam(book,Role),Committing))return;
            Committing=true;
            try
            {
                model.CurSelectBook=book;model._isSelectBook=true;
                Ctrl.OnClickBtnBook(); // Item Rebuild owns quantity decrement; never subtract twice here.
                Ctrl.InitBookData(UICharacterCtrl.Data.PlayerBag);
                Ctrl.InitSkillData();
                View.RefreshSheetCharacter(model.ListRole[roleIndex]);
                View.RefreshTipsRoleInfo();
            }
            finally {Committing=false;model.CurSelectBook=null;model._isSelectBook=false;model.CurFoucType=focusType;}
            Refresh();
        }
        public void Dispose()
        {
            if(disposed)return;
            bool refreshDetails=Valid;
            disposed=true;
            try
            {
            foreach(var entry in listeners)entry.Listener.Remove(entry.Callback);
            listeners.Clear();
            if(!sheet.isDisposed)
            {
                foreach(var obj in slotObjects.Values)if(!obj.isDisposed)obj.onClick.Clear();
                sheet.listBook.numItems=0;
                sheet.listBook.itemRenderer=originalRenderer;
                foreach(var label in badges.Values)if(!label.isDisposed)label.Dispose();
                RestoreLayout();
                if(!parent.isDisposed)parent.AddChildAt(sheet,Math.Min(childIndex,parent.numChildren));
                else sheet.RemoveFromParent();
                sheet.group=group;
                sheet.touchable=sheetTouchable;
                Geometry(sheet,()=>{sheet.SetXY(x,y);sheet.SetScale(sx,sy);});sheet.visible=visible;
                sheet.ReleaseDisplayLock(displayLock);
                sheet.comFilter.visible=filterVisible;sheet.btnSkillUp.touchable=useTouchable;
                sheet.TexNoBook.text=emptyText;
            }
            badges.Clear();slotCallbacks.Clear();slotObjects.Clear();
            model.CurSelectBook=null;model._isSelectBook=false;model._bookIndex=-1;model.ListFilterBook=new Books();model.CurFoucType=focusType;model.MarkDirty();
            RestoreNativeInfo();
            if(refreshDetails)View.RefreshTipsRoleInfo();
            }
            finally
            {
            RestoreNativeInfo();
            if(nativeLayout.Count>0)RestoreLayout();
            previewSkills?.Dispose();previewSkills=null;
            if(OwnsFocus())GRoot.inst.focus=oldFocus!=null&&!oldFocus.isDisposed?oldFocus:null;
            // Even failed restoration must never let disposal of our overlay destroy native UI.
            if(!sheet.isDisposed && sheet.parent?.Pointer==panel?.Pointer)sheet.RemoveFromParent();
            overlay?.Dispose();overlay=null;
            }
        }
        bool OwnsFocus()
        {
            for(var node=GRoot.inst.focus;node!=null;node=node.parent)
                if(node.Pointer==overlay?.Pointer)return true;
            return false;
        }
    }
}
