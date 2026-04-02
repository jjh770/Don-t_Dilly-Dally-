$dir = "C:\Users\mknoh\Desktop\Don-t_Dilly-Dally-\Assets\08.Materials"
$files = Get-ChildItem -Path $dir -Recurse -Filter "*.mat" | Where-Object {
    (Get-Content $_.FullName -Raw) -match "bee44b4a58655ee4cbff107302a3e131"
}
foreach ($f in $files) {
    $c = Get-Content $f.FullName -Raw
    $c = $c -replace '(_UnityShadowPower:) [\d.]+', '${1} 0.8'
    Set-Content -Path $f.FullName -Value $c -NoNewline
    Write-Host "Done: $($f.Name)"
}
Write-Host "Total: $($files.Count) files"
