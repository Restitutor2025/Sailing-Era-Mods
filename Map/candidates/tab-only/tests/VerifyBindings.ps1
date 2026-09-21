$ErrorActionPreference = 'Stop'
$gameDir = 'E:\Program\steam\steamapps\common\Sailing Era'
Add-Type -Path "$gameDir\MelonLoader\net6\Mono.Cecil.dll"
$resolver = [Mono.Cecil.DefaultAssemblyResolver]::new()
$resolver.AddSearchDirectory("$gameDir\MelonLoader\Il2CppAssemblies")
$resolver.AddSearchDirectory("$gameDir\MelonLoader\net6")
$parameters = [Mono.Cecil.ReaderParameters]::new()
$parameters.AssemblyResolver = $resolver
$interop = [Mono.Cecil.AssemblyDefinition]::ReadAssembly("$gameDir\MelonLoader\Il2CppAssemblies\Assembly-CSharp.dll", $parameters)
$mapDir = Split-Path $PSScriptRoot -Parent
$mod = [Mono.Cecil.AssemblyDefinition]::ReadAssembly("$mapDir\bin\Release\net6.0\Restitutor_fixes_map.dll", $parameters)
$native = Get-Content (Join-Path $mapDir '../../../analysis/native-map.json') -Raw | ConvertFrom-Json
$hookCount = 0
$patchedAddresses = @{}
foreach ($file in Get-ChildItem "$mapDir\src" -Filter '*.cs') {
    foreach ($line in Get-Content -LiteralPath $file.FullName) {
        if ($line -match 'H\(typeof\((\w+)\), "([^"]+)", nameof\((\w+)\)(.*)\);') { $line = 'EntryPoint.Hook(typeof('+$Matches[1]+'), "'+$Matches[2]+'", typeof(SharedMap), before: nameof('+$Matches[3]+')'+($Matches[4] -replace ', ', ', args: ')+');' }
        $match = [regex]::Match($line, 'EntryPoint\.Hook\(typeof\((\w+)\), "([^"]+)", typeof\((\w+)\)')
        if (!$match.Success) { continue }
        $types = @($interop.MainModule.Types | Where-Object Name -eq $match.Groups[1].Value)
        if ($types.Count -ne 1) { throw "Ambiguous hook type: $line" }
        $methods = @($types[0].Methods | Where-Object Name -eq $match.Groups[2].Value)
        if ($line -match 'args: Type.EmptyTypes') {
            $methods = @($methods | Where-Object { $_.Parameters.Count -eq 0 })
        } elseif ($line -match 'args: new\[\] \{ typeof\(int\) \}') {
            $methods = @($methods | Where-Object { $_.Parameters.Count -eq 1 -and $_.Parameters[0].ParameterType.FullName -eq 'System.Int32' })
        } elseif ($line -match 'args:') { throw "Unrecognized explicit hook signature: $line" }
        if ($methods.Count -ne 1) { throw "Ambiguous hook method: $line" }
        $target = $methods[0]
        $handlers = @($mod.MainModule.Types | Where-Object Name -eq $match.Groups[3].Value)
        if ($handlers.Count -ne 1) { throw "Missing handler class: $line" }
        foreach ($handlerName in [regex]::Matches($line, '(before|after): nameof\((\w+)\)')) {
            $handler = @($handlers[0].Methods | Where-Object Name -eq $handlerName.Groups[2].Value)
            if ($handler.Count -ne 1 -or !$handler[0].IsStatic) { throw "Invalid handler: $line" }
            foreach ($arg in $handler[0].Parameters) {
                $typeName = $arg.ParameterType.FullName.TrimEnd('&')
                switch -Regex ($arg.Name) {
                    '^__instance$' { if ($typeName -ne $target.DeclaringType.FullName) { throw "Instance mismatch: $arg in $line" } }
                    '^__result$' { if ($typeName -ne $target.ReturnType.FullName) { throw "Result mismatch: $arg in $line" } }
                    '^__(\d+)$' {
                        $index = [int]$Matches[1]
                        if ($index -ge $target.Parameters.Count -or $typeName -ne $target.Parameters[$index].ParameterType.FullName) { throw "Argument mismatch: $arg in $line" }
                    }
                    '^__state$' { if ($typeName -ne 'System.Boolean') { throw "Unexpected state type: $arg in $line" } }
                    default { throw "Unverified Harmony parameter: $arg in $line" }
                }
            }
        }
        # Refuse shared native entrypoints (e.g. common empty ret bodies) as hook sites.
        $nativeType = $target.DeclaringType.FullName -replace '^Il2Cpp', ''
        $nativeName = $target.Name
        if ($nativeName -match '^_([A-Za-z]+)_b__(\d+_\d+)$') { $nativeName = '<'+$Matches[1]+'>b__'+$Matches[2] }
        # Explicit verified interop spelling of the harbour onClick callback.
        if ($nativeType -eq 'Client.UILogic.UIMap.UIMapIcon.UIMapHarbourIcon' -and $nativeName -eq '_InitComponent_b__16_0') { $nativeName = '<InitComponent>b__16_0' }
        $nativeMatches = @($native | Where-Object { $_.type -eq $nativeType -and $_.method -eq $nativeName })
        $targetParams = (@($target.Parameters | ForEach-Object {
            $parameterType = $_.ParameterType.FullName
            $parameterType = $parameterType -replace '^Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppStructArray`1<(.+)>$', '$1[]'
            $parameterType -replace '^Il2Cpp', ''
        }) -join ',')
        $nativeMatches = @($nativeMatches | Where-Object { $_.signature.EndsWith("($targetParams)") })
        if ($nativeMatches.Count -ne 1) { throw "Native signature not unique: $target" }
        $sameEntry = @($native | Where-Object RVA -eq $nativeMatches[0].RVA)
        $auditedPreviewAlias = $sameEntry.Count -eq 2 -and
            (@($sameEntry | Where-Object type -ne 'Client.UILogic.UIMap.UIMapCtrl').Count -eq 0) -and
            (($sameEntry.method | Sort-Object) -join ',') -eq 'ClosePortCheckLine,OnAction_B'
        if ($sameEntry.Count -ne 1 -and !$auditedPreviewAlias) { throw "Shared native hook entry: $target at $($nativeMatches[0].RVA)" }
        $address = $nativeMatches[0].RVA
        if ($patchedAddresses.ContainsKey($address)) { throw "Duplicate native address patched: $address" }
        $patchedAddresses[$address] = $target.FullName
        $hookCount++
    }
}
$refs = @($mod.MainModule.GetMemberReferences() | Where-Object { $_.DeclaringType.FullName -match '^(Il2Cpp|UnityEngine\.)' })
foreach ($member in $refs) {
    if ($null -eq $member.Resolve()) { throw "Unresolved game member: $member" }
}
"PASS $hookCount hook signatures/handler parameters resolve; no duplicate addresses or unaudited shared native entrypoints."
"PASS $($refs.Count) game/Unity/interop member references resolve without executing game code."
$mod.Dispose(); $interop.Dispose(); $resolver.Dispose()


