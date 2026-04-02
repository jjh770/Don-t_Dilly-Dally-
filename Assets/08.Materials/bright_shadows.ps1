$dir = "C:\Users\mknoh\Desktop\Don-t_Dilly-Dally-\Assets\08.Materials"
$files = Get-ChildItem -Path $dir -Recurse -Filter "*.mat" | Where-Object {
    (Get-Content $_.FullName -Raw) -match "bee44b4a58655ee4cbff107302a3e131"
}
foreach ($f in $files) {
    $c = Get-Content $f.FullName -Raw

    # Shadow: bright, clean, slight cool
    $c = $c -replace '(_ColorDim: \{r: )[\d.]+, (g: )[\d.]+, (b: )[\d.]+, (a: )[\d.]+\}', '${1}0.72, ${2}0.73, ${3}0.76, ${4}0.8}'

    # Extra shadow: medium, not dark
    $c = $c -replace '(_ColorDimExtra: \{r: )[\d.]+, (g: )[\d.]+, (b: )[\d.]+, (a: )[\d.]+\}', '${1}0.5, ${2}0.5, ${3}0.55, ${4}0.5}'

    # Rim: bright white
    $c = $c -replace '(_FlatRimColor: \{r: )[\d.]+, (g: )[\d.]+, (b: )[\d.]+, (a: )[\d.]+\}', '${1}0.95, ${2}0.95, ${3}1, ${4}1}'

    # Specular: pure white
    $c = $c -replace '(_FlatSpecularColor: \{r: )[\d.]+, (g: )[\d.]+, (b: )[\d.]+, (a: )[\d.]+\}', '${1}1, ${2}1, ${3}1, ${4}1}'

    # Unity shadow: lighter
    $c = $c -replace '(_UnityShadowColor: \{r: )[\d.]+, (g: )[\d.]+, (b: )[\d.]+, (a: )[\d.]+\}', '${1}0.65, ${2}0.65, ${3}0.68, ${4}1}'

    Set-Content -Path $f.FullName -Value $c -NoNewline
    Write-Host "Bright: $($f.Name)"
}
Write-Host "Done: $($files.Count) files"
