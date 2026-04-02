$matDir = "C:\Users\mknoh\Desktop\Don-t_Dilly-Dally-\Assets\08.Materials"

# Get all .mat files with Flat Kit shader guid
$matFiles = Get-ChildItem -Path $matDir -Recurse -Filter "*.mat" | Where-Object {
    (Get-Content $_.FullName -Raw) -match "bee44b4a58655ee4cbff107302a3e131"
}

Write-Host "=== Found $($matFiles.Count) Flat Kit materials ==="

foreach ($file in $matFiles) {
    $content = Get-Content $file.FullName -Raw
    $name = $file.Name
    Write-Host "Processing: $name"

    # === FLOAT PROPERTIES ===
    # LightContribution: standardize to 0.15
    $content = $content -replace '(_LightContribution:) [\d.]+', '${1} 0.15'

    # LightFalloffSize: 0.3
    $content = $content -replace '(_LightFalloffSize:) [\d.]+', '${1} 0.3'

    # Self Shading (primary shadow)
    $content = $content -replace '(_SelfShadingSize:) [\d.]+', '${1} 0.45'
    $content = $content -replace '(_Flatness:) [\d.]+', '${1} 0.85'

    # Extra shadow step
    $content = $content -replace '(_CelExtraEnabled:) [\d.]+', '${1} 1'
    $content = $content -replace '(_SelfShadingSizeExtra:) [\d.]+', '${1} 0.12'
    $content = $content -replace '(_FlatnessExtra:) [\d.]+', '${1} 0.35'

    # Shadow edge
    $content = $content -replace '(_ShadowEdgeSize:) [\d.]+', '${1} 0.03'
    $content = $content -replace '(_ShadowEdgeSizeExtra:) [\d.]+', '${1} 0.04'
    $content = $content -replace '(_ShadowEdgeSmoothness:) [\d.]+', '${1} 0.05'

    # Rim light - enabled, subtle cool white
    $content = $content -replace '(_RimEnabled:) [\d.]+', '${1} 1'
    $content = $content -replace '(_FlatRimEnabled:) [\d.]+', '${1} 1'
    $content = $content -replace '(_FlatRimSize:) [\d.]+', '${1} 0.2'
    $content = $content -replace '(_FlatRimEdgeSmoothness:) [\d.]+', '${1} 0.5'
    $content = $content -replace '(_FlatRimLightAlign:) [\d.]+', '${1} 0.3'
    $content = $content -replace '(_FlatRimAmount:) [\d.]+', '${1} 0.6'

    # Specular - enabled, small clean highlight
    $content = $content -replace '(_SpecularEnabled:) [\d.]+', '${1} 1'
    $content = $content -replace '(_FlatSpecularEnabled:) [\d.]+', '${1} 1'
    $content = $content -replace '(_FlatSpecularSize:) [\d.]+', '${1} 0.15'
    $content = $content -replace '(_FlatSpecularEdgeSmoothness:) [\d.]+', '${1} 0.7'
    $content = $content -replace '(_FlatSpecularSmoothness:) [\d.]+', '${1} 0.5'

    # Outline OFF
    $content = $content -replace '(_OutlineEnabled:) [\d.]+', '${1} 0'

    # Unity Shadow - multiply mode, moderate
    $content = $content -replace '(_UnityShadowMode:) [\d.]+', '${1} 1'
    $content = $content -replace '(_UnityShadowPower:) [\d.]+', '${1} 0.5'
    $content = $content -replace '(_UnityShadowSharpness:) [\d.]+', '${1} 0.8'
    $content = $content -replace '(_UnityShadowOcclusion:) [\d.]+', '${1} 1'

    # Override shadows enabled
    $content = $content -replace '(_OverrideShadows:) [\d.]+', '${1} 1'
    $content = $content -replace '(_OverrideShadowsEnabled:) [\d.]+', '${1} 1'

    # === COLOR PROPERTIES ===
    # Shadow color (primary) - warm, not too dark
    $content = $content -replace '(_ColorDim: \{r: )[\d.]+, (g: )[\d.]+, (b: )[\d.]+, (a: )[\d.]+\}', '${1}0.68, ${2}0.64, ${3}0.62, ${4}0.85}'

    # Shadow color (extra) - deeper warm shadow
    $content = $content -replace '(_ColorDimExtra: \{r: )[\d.]+, (g: )[\d.]+, (b: )[\d.]+, (a: )[\d.]+\}', '${1}0.4, ${2}0.36, ${3}0.35, ${4}0.6}'

    # Rim color - cool white for nice separation
    $content = $content -replace '(_FlatRimColor: \{r: )[\d.]+, (g: )[\d.]+, (b: )[\d.]+, (a: )[\d.]+\}', '${1}0.85, ${2}0.88, ${3}0.95, ${4}1}'

    # Specular color - warm white
    $content = $content -replace '(_FlatSpecularColor: \{r: )[\d.]+, (g: )[\d.]+, (b: )[\d.]+, (a: )[\d.]+\}', '${1}1, ${2}0.97, ${3}0.92, ${4}1}'

    # Unity shadow color - soft warm
    $content = $content -replace '(_UnityShadowColor: \{r: )[\d.]+, (g: )[\d.]+, (b: )[\d.]+, (a: )[\d.]+\}', '${1}0.6, ${2}0.58, ${3}0.55, ${4}1}'

    # Write back
    Set-Content -Path $file.FullName -Value $content -NoNewline
    Write-Host "  Updated: $name"
}

# === KEYWORDS FIX ===
# Make sure important keywords are present
foreach ($file in $matFiles) {
    $content = Get-Content $file.FullName -Raw
    $name = $file.Name

    # Ensure DR_CEL_EXTRA_ON keyword exists
    if ($content -notmatch 'DR_CEL_EXTRA_ON') {
        $content = $content -replace '(m_ValidKeywords:)', "`$1`n  - DR_CEL_EXTRA_ON"
        Write-Host "  Added DR_CEL_EXTRA_ON to $name"
    }

    # Ensure DR_RIM_ON keyword exists
    if ($content -notmatch 'DR_RIM_ON') {
        $content = $content -replace '(m_ValidKeywords:)', "`$1`n  - DR_RIM_ON"
        Write-Host "  Added DR_RIM_ON to $name"
    }

    # Ensure DR_SPECULAR_ON keyword exists
    if ($content -notmatch 'DR_SPECULAR_ON') {
        $content = $content -replace '(m_ValidKeywords:)', "`$1`n  - DR_SPECULAR_ON"
        Write-Host "  Added DR_SPECULAR_ON to $name"
    }

    # Ensure _UNITYSHADOWMODE_MULTIPLY keyword
    if ($content -notmatch '_UNITYSHADOWMODE_MULTIPLY') {
        # Remove _UNITYSHADOWMODE_NONE if present
        $content = $content -replace '\s*- _UNITYSHADOWMODE_NONE\r?\n', "`n"
        $content = $content -replace '(m_ValidKeywords:)', "`$1`n  - _UNITYSHADOWMODE_MULTIPLY"
        Write-Host "  Added _UNITYSHADOWMODE_MULTIPLY to $name"
    }

    # Ensure _UNITYSHADOW_OCCLUSION keyword
    if ($content -notmatch '_UNITYSHADOW_OCCLUSION') {
        $content = $content -replace '(m_ValidKeywords:)', "`$1`n  - _UNITYSHADOW_OCCLUSION"
        Write-Host "  Added _UNITYSHADOW_OCCLUSION to $name"
    }

    # Remove from InvalidKeywords if present
    $content = $content -replace '\s*- _UNITYSHADOWMODE_NONE\r?\n', "`n"

    Set-Content -Path $file.FullName -Value $content -NoNewline
}

Write-Host ""
Write-Host "=== All Flat Kit materials updated ==="
Write-Host "Total: $($matFiles.Count) files"
