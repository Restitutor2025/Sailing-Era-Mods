# 현재 패치 등록

현재 소스의 등록식과 기존 native-map 메타데이터 서명을 결합한 정적 목록. 실제 설치 성공이나 네이티브 본문 의미를 자동 검증한 것이 아니다. 서명이 여러 개 또는 없으면 변경 전에 해결해야 한다. Prefix/Postfix는 등록식과 CURRENT 지침을 함께 읽는다.

## UISettingView.OnVideoSettingItemRender
[소스](../../../TextSpeed/src/TextSpeedModule.cs) 줄 35
`Hook(typeof(UISettingView), "OnVideoSettingItemRender", typeof(OptionsRow), nameof(OptionsRow.BeforeRender), nameof(OptionsRow.AfterRender));`
- `System.Void Client.UILogic.UISystemSetting.UISettingView::OnVideoSettingItemRender(System.Int32,FairyGUI.GObject)` — RVA `0x64EA10`
## UIDialogView.OnInit
[소스](../../../TextSpeed/src/TextSpeedModule.cs) 줄 36
`Hook(typeof(UIDialogView), "OnInit", typeof(TextSpeedModule), null, nameof(RegisterDialog));`
- `System.Void Client.UILogic.UIDialog.UIDialogView::OnInit()` — RVA `0x1347D10`
## UIDialogView.StartEffect
[소스](../../../TextSpeed/src/TextSpeedModule.cs) 줄 38
`Hook(typeof(UIDialogView), "StartEffect", typeof(TextSpeedModule), nameof(RegisterDialog), null);`
- `System.Void Client.UILogic.UIDialog.UIDialogView::StartEffect()` — RVA `0x1349100`
## UIDialogView.HideHook
[소스](../../../TextSpeed/src/TextSpeedModule.cs) 줄 39
`Hook(typeof(UIDialogView), "HideHook", typeof(TextSpeedModule), null, nameof(ReleaseDialog));`
- `System.Void Client.UILogic.UIDialog.UIDialogView::HideHook()` — RVA `0x134EBC0`
## TypingEffectObject.Start
[소스](../../../TextSpeed/src/TextSpeedModule.cs) 줄 40
`Hook(typeof(TypingEffectObject), "Start", typeof(TextSpeedModule), null, nameof(AfterStart));`
- `System.Void Client.Utils.TypingEffectObject::Start(System.Action,System.String,System.Single,System.Single)` — RVA `0x69C080`
## UIDialogView.EndTyping (0.1.4)
[소스](../../../TextSpeed/src/TextSpeedModule.cs)
`Hook(typeof(UIDialogView), "EndTyping", typeof(TextSpeedModule), nameof(BeforeEndTyping), nameof(AfterEndTyping));`
- `System.Void Client.UILogic.UIDialog.UIDialogView::EndTyping()` — RVA `0x1349E40`
## UIDialogCtrl.OnAction_ContinueTalk (0.1.4)
[소스](../../../TextSpeed/src/TextSpeedModule.cs)
`Hook(typeof(UIDialogCtrl), "OnAction_ContinueTalk", typeof(TextSpeedModule), nameof(BeforeContinueTalk), null);`
- `System.Void Client.UILogic.UIDialog.UIDialogCtrl::OnAction_ContinueTalk()` — RVA `0x1343B10` (Prefix, 차단 없음)
