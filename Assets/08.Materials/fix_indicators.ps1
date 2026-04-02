$dir = "C:\Users\mknoh\Desktop\Don-t_Dilly-Dally-\Assets\08.Materials\Indicator"
$files = Get-ChildItem -Path $dir -Filter "*.mat"

foreach ($f in $files) {
    $c = Get-Content $f.FullName -Raw

    # Remove _EMISSION keyword
    $c = $c -replace '\s*- _EMISSION\r?\n', "`n"

    # Set EmissionColor to black (no emission)
    $c = $c -replace '(_EmissionColor: \{r: )[\d.]+, (g: )[\d.]+, (b: )[\d.]+, (a: )[\d.]+\}', '${1}0, ${2}0, ${3}0, ${4}1}'

    Set-Content -Path $f.FullName -Value $c -NoNewline
    Write-Host "Fixed: $($f.Name)"
}
Write-Host "Done: $($files.Count) indicator materials"
