$ErrorActionPreference='Stop'
$bin='E:\Download\dnSpy-net-win64\bin'
foreach($n in @('dnlib','dnSpy.Contracts.DnSpy','dnSpy.Contracts.Logic','ICSharpCode.NRefactory','ICSharpCode.NRefactory.CSharp','ICSharpCode.Decompiler')){Add-Type -Path "$bin\$n.dll"}
$source='E:\Program\steam\steamapps\common\Sailing Era\Mods\Additional_Functions\Restitutor_Additional_Contribution_Goods_Recognition.dll'
$dest=Join-Path $PSScriptRoot 'exports/Contribution-0.3.1-dnSpy'
$csDir=Join-Path $dest 'CSharp'
New-Item -ItemType Directory -Force $csDir | Out-Null
$m=[dnlib.DotNet.ModuleDefMD]::Load($source)
$hash=(Get-FileHash -LiteralPath $source).Hash
$combined=[Text.StringBuilder]::new()
[void]$combined.AppendLine("// dnSpy C# decompilation of installed DLL; not original source.")
[void]$combined.AppendLine("// Assembly: $($m.Assembly.FullName)")
[void]$combined.AppendLine("// SHA256: $hash")
$count=0
foreach($t in $m.Types){
 if($t.IsGlobalModuleType){continue}
 $ctx=[ICSharpCode.Decompiler.DecompilerContext]::new(0,$m,$null)
 $b=[ICSharpCode.Decompiler.Ast.AstBuilder]::new($ctx)
 $b.AddType($t)
 $b.RunTransformations()
 $output=[dnSpy.Contracts.Decompiler.StringBuilderDecompilerOutput]::new()
 $b.GenerateCode($output)
 $code=$output.GetText()
 if([string]::IsNullOrWhiteSpace($code)){throw "Empty output: $($t.FullName)"}
 $safeName=$t.FullName -replace '[<>:"/\\|?*]','_'
 [IO.File]::WriteAllText((Join-Path $csDir ($safeName+'.cs')),$code,[Text.UTF8Encoding]::new($false))
 [void]$combined.AppendLine("`n// ===== TYPE: $($t.FullName) =====")
 [void]$combined.AppendLine($code)
 $count++
}
$textPath=Join-Path $dest 'Restitutor_fixes_Contribution-0.3.1-decompiled.txt'
[IO.File]::WriteAllText($textPath,$combined.ToString(),[Text.UTF8Encoding]::new($false))
$il=[IO.StreamWriter]::new((Join-Path $dest 'Restitutor_fixes_Contribution-0.3.1-IL.txt'),$false,[Text.UTF8Encoding]::new($false))
try {
 foreach($t in $m.GetTypes()){
  $il.WriteLine('TYPE '+$t.FullName)
  foreach($method in $t.Methods){
   $il.WriteLine('METHOD '+$method.FullName+' TOKEN '+$method.MDToken)
   if($method.HasBody){foreach($ins in $method.Body.Instructions){$il.WriteLine($ins.ToString())}}
  }
 }
} finally {$il.Dispose()}
$readme=@"
Contribution 0.3.1 dnSpy export

Upload Restitutor_fixes_Contribution-0.3.1-decompiled.txt to GPT for review.
CSharp contains the same decompilation split by top-level type.
The IL text includes nested/compiler-generated methods for cross-checking.
This is decompiled managed mod code, not the game's native implementation.
Game APIs are external references; their bodies are not included.
Decompilation may differ from original syntax and is not a build-ready project.

Source: $source
Assembly: $($m.Assembly.FullName)
SHA256: $hash
Top-level types exported: $count
Tool: local dnSpy ICSharpCode.Decompiler
"@
[IO.File]::WriteAllText((Join-Path $dest 'README.txt'),$readme,[Text.UTF8Encoding]::new($false))
Compress-Archive -Path (Join-Path $dest '*') -DestinationPath ($dest+'.zip') -Force
"Exported $count top-level types with no failures."
Get-Item $textPath,($dest+'.zip') | Select-Object FullName,Length
