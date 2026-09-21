# 현재 패치 등록

현재 소스의 등록식과 기존 native-map 메타데이터 서명을 결합한 정적 목록. 실제 설치 성공이나 네이티브 본문 의미를 자동 검증한 것이 아니다. 서명이 여러 개 또는 없으면 변경 전에 해결해야 한다. Prefix/Postfix는 등록식과 CURRENT 지침을 함께 읽는다.

## UILaunchView.OnInit
[소스](../../../src/IntroSkipMod.cs) 줄 30
`Patch(typeof(UILaunchView), "OnInit", nameof(RegisterLaunch), false);`
- `System.Void Client.UILogic.UILaunch.UILaunchView::OnInit()` — RVA `0xA24DA0`
## UILaunchView.HideHook
[소스](../../../src/IntroSkipMod.cs) 줄 31
`Patch(typeof(UILaunchView), "HideHook", nameof(ReleaseLaunch), false);`
- `System.Void Client.UILogic.UILaunch.UILaunchView::HideHook()` — RVA `0xA28F70`
## UILaunchView.UpdateHook
[소스](../../../src/IntroSkipMod.cs) 줄 32
`Patch(typeof(UILaunchView), "UpdateHook", nameof(BeforeLaunchUpdate), true);`
- `System.Void Client.UILogic.UILaunch.UILaunchView::UpdateHook()` — RVA `0xA27270`
## Transition._Play
[소스](../../../src/IntroSkipMod.cs) 줄 33
`Patch(typeof(Transition), "_Play", nameof(AfterTransition), false);`
- `System.Void FairyGUI.Transition::_Play(System.Int32,System.Single,System.Single,System.Single,FairyGUI.PlayCompleteCallback,System.Boolean)` — RVA `0x181D2A0`
