$dir = "C:\Users\mknoh\Desktop\Don-t_Dilly-Dally-\Assets\08.Materials"
$files = Get-ChildItem -Path $dir -Recurse -Filter "*.mat" | Where-Object {
    (Get-Content $_.FullName -Raw) -match "bee44b4a58655ee4cbff107302a3e131"
}
foreach ($f in $files) {
    $c = Get-Content $f.FullName -Raw

    # Shadow color: cool blue-gray (was warm)
    $c = $c -replace '(_ColorDim: \{r: )[\d.]+, (g: )[\d.]+, (b: )[\d.]+, (a: )[\d.]+\}', '${1}0.62, ${2}0.65, ${3}0.72, ${4}0.85}'

    # Extra shadow: cool deeper
    $c = $c -replace '(_ColorDimExtra: \{r: )[\d.]+, (g: )[\d.]+, (b: )[\d.]+, (a: )[\d.]+\}', '${1}0.35, ${2}0.37, ${3}0.45, ${4}0.6}'

    # Rim color: clean cool white
    $c = $c -replace '(_FlatRimColor: \{r: )[\d.]+, (g: )[\d.]+, (b: )[\d.]+, (a: )[\d.]+\}', '${1}0.88, ${2}0.92, ${3}1, ${4}1}'

    # Specular: pure white
    $c = $c -replace '(_FlatSpecularColor: \{r: )[\d.]+, (g: )[\d.]+, (b: )[\d.]+, (a: )[\d.]+\}', '${1}1, ${2}1, ${3}1, ${4}1}'

    # Unity shadow color: cool
    $c = $c -replace '(_UnityShadowColor: \{r: )[\d.]+, (g: )[\d.]+, (b: )[\d.]+, (a: )[\d.]+\}', '${1}0.55, ${2}0.58, ${3}0.65, ${4}1}'

    Set-Content -Path $f.FullName -Value $c -NoNewline
    Write-Host "Cooled: $($f.Name)"
}
Write-Host "Done: $($files.Count) files"
