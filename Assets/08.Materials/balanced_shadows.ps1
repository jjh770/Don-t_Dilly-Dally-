$dir = "C:\Users\mknoh\Desktop\Don-t_Dilly-Dally-\Assets\08.Materials"
$files = Get-ChildItem -Path $dir -Recurse -Filter "*.mat" | Where-Object {
    (Get-Content $_.FullName -Raw) -match "bee44b4a58655ee4cbff107302a3e131"
}
foreach ($f in $files) {
    $c = Get-Content $f.FullName -Raw

    # Shadow: visible but not dark, neutral tone
    $c = $c -replace '(_ColorDim: \{r: )[\d.]+, (g: )[\d.]+, (b: )[\d.]+, (a: )[\d.]+\}', '${1}0.65, ${2}0.64, ${3}0.66, ${4}0.85}'

    # Extra shadow: deeper, gives depth
    $c = $c -replace '(_ColorDimExtra: \{r: )[\d.]+, (g: )[\d.]+, (b: )[\d.]+, (a: )[\d.]+\}', '${1}0.4, ${2}0.38, ${3}0.42, ${4}0.6}'

    # Self shading size: bigger shadow area
    $c = $c -replace '(_SelfShadingSize:) [\d.]+', '${1} 0.5'

    # Unity shadow power: stronger
    $c = $c -replace '(_UnityShadowPower:) [\d.]+', '${1} 0.6'

    # Rim: neutral white
    $c = $c -replace '(_FlatRimColor: \{r: )[\d.]+, (g: )[\d.]+, (b: )[\d.]+, (a: )[\d.]+\}', '${1}0.9, ${2}0.9, ${3}0.92, ${4}1}'

    # Unity shadow color: neutral
    $c = $c -replace '(_UnityShadowColor: \{r: )[\d.]+, (g: )[\d.]+, (b: )[\d.]+, (a: )[\d.]+\}', '${1}0.6, ${2}0.6, ${3}0.62, ${4}1}'

    Set-Content -Path $f.FullName -Value $c -NoNewline
    Write-Host "Balanced: $($f.Name)"
}
Write-Host "Done: $($files.Count) files"
