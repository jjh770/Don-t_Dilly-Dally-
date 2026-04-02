$dir = "C:\Users\mknoh\Desktop\Don-t_Dilly-Dally-\Assets\08.Materials"
$files = Get-ChildItem -Path $dir -Recurse -Filter "*.mat" | Where-Object {
    (Get-Content $_.FullName -Raw) -match "bee44b4a58655ee4cbff107302a3e131"
}
foreach ($f in $files) {
    $c = Get-Content $f.FullName -Raw
    $c = $c -replace '(_LightContribution:) [\d.]+', '${1} 0.05'
    Set-Content -Path $f.FullName -Value $c -NoNewline
    Write-Host "Fixed: $($f.Name)"
}
Write-Host "Done: $($files.Count) files"
