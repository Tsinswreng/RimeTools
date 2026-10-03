# v2：修正「字格中的記號被誤當成獨立字」的問題
# 站方在字格裏用小括號、尖括號等記號標注異文/疑字，且把記號也做成了連結，
# 故須把記號與相鄰字黏回去，並在逐字表中另立一欄記錄記號形式。
# 用法: pwsh -File Convert-KaomBzd2.ps1 -Html <全表html> -OutDir <輸出目錄>

param(
  [Parameter(Mandatory=$true)][string]$Html,
  [Parameter(Mandatory=$true)][string]$OutDir
)

$ErrorActionPreference = "Stop"
$utf8 = [System.Text.UTF8Encoding]::new($false)
$open  = @('<','(','[','{')
$close = @('>',')',']','}')
$allMarkers = $open + $close

function Get-PlainText([string]$frag) {
  if ($null -eq $frag) { return "" }
  $s = $frag -replace '(?s)<br\s*/?>', ' '
  $s = $s -replace '(?s)<[^>]*>', ''
  $s = $s -replace '&nbsp;', ' '
  $s = $s -replace '&lt;', '<'
  $s = $s -replace '&gt;', '>'
  $s = $s -replace '&quot;', '"'
  $s = $s -replace '&#39;', "'"
  $s = $s -replace '&amp;', '&'
  $s = $s -replace '\s+', ' '
  return $s.Trim()
}

function Get-Cells([string]$rowHtml) {
  $ms = [regex]::Matches($rowHtml, '(?s)<td[^>]*>(.*?)(?=<td|</tr>)')
  $out = @()
  foreach ($m in $ms) { $out += ,$m.Groups[1].Value }
  return ,$out
}

function Get-LinkTexts([string]$cellHtml) {
  $out = @()
  foreach ($m in [regex]::Matches($cellHtml, 'sgy_bzd88\.php\?word=([^"]+)"')) {
    $out += [System.Uri]::UnescapeDataString($m.Groups[1].Value)
  }
  return ,$out
}

function Format-WithMarkers($tokens) {
  # 回傳 原樣字串 與 逐字清單（每項含 字 與 記號形式）
  $display = ""
  $attachNext = $false
  $pendingPrefix = ""
  $items = New-Object System.Collections.Generic.List[object]
  foreach ($t in $tokens) {
    if ($open -contains $t) {
      if ($display.Length -gt 0 -and -not $attachNext) { $display += ' ' }
      $display += $t
      $attachNext = $true
      $pendingPrefix = $t
      continue
    }
    if ($close -contains $t) {
      $display = $display.TrimEnd() + $t
      $attachNext = $false
      continue
    }
    if ($display.Length -gt 0 -and -not $attachNext) { $display += ' ' }
    $display += $t
    $attachNext = $false
    $items.Add([pscustomobject]@{ Zi = $t; Form = $pendingPrefix })
    $pendingPrefix = ""
  }
  return [pscustomobject]@{ Display = $display.Trim(); Items = $items }
}

function Esc([string]$s) {
  if ($null -eq $s) { return "" }
  return ($s -replace "`t", ' ' -replace "`r?`n", ' ')
}

$txt = [System.IO.File]::ReadAllText($Html, $utf8)
$rows = [regex]::Matches($txt, '(?s)<tr\b.*?</tr>')
New-Item -ItemType Directory -Force -Path $OutDir | Out-Null

$mainRows = New-Object System.Collections.Generic.List[string]
$longRows = New-Object System.Collections.Generic.List[string]
$appRows  = New-Object System.Collections.Generic.List[string]

$mainRows.Add((@('聲符','諧聲域','轄字(原樣)','轄字(純字)','反切','中古地位','切拼','上古音參考','考釋備註') -join "`t"))
$longRows.Add((@('聲符','諧聲域','字','記號形式','反切','中古地位','切拼','上古音參考','考釋備註') -join "`t"))
$appRows.Add((@('序號','字','類別','正字','中古地位','切拼','考釋備註') -join "`t"))

$cur = $null
$stat = [ordered]@{ main = 0; long = 0; appendix = 0; groups = 0; markerRows = 0; markers = 0 }

foreach ($r in $rows) {
  $h = $r.Value
  if ($h -match '<th') { continue }
  $cells = Get-Cells $h

  if ($h -match '<td rowspan="(\d+)"[^>]*>(?s)(.*?)(?=<td|</tr>)') {
    $cur = [pscustomobject]@{
      ShengFu = (Get-PlainText ([regex]::Match($Matches[2], '(?s)<b>(.*?)</b>').Groups[1].Value))
      XieShengYu = (Get-PlainText ([regex]::Replace($Matches[2], '(?s)<b>.*?</b>', ' ')))
    }
    $stat.groups++
    if ($cells.Count -lt 2) { continue }
    $cells = $cells[1..($cells.Count - 1)]
  }
  if ($cells.Count -lt 6) { continue }

  $first = (Get-PlainText $cells[0])
  if ($cells.Count -ge 8 -and $first -match '^\d+$') {
    $appRows.Add((@(
      (Get-PlainText $cells[0]), (Get-PlainText $cells[1]), (Get-PlainText $cells[2]),
      (Get-PlainText $cells[4]), (Get-PlainText $cells[5]), (Get-PlainText $cells[6]),
      (Get-PlainText $cells[7])
    ) | ForEach-Object { Esc $_ }) -join "`t")
    $stat.appendix++
    continue
  }

  $fmt = Format-WithMarkers (Get-LinkTexts $cells[0])
  $pureChars = @()
  $hasMarker = $false
  foreach ($it in $fmt.Items) {
    if ($allMarkers -contains $it.Zi) { continue }
    $pureChars += $it.Zi
    if ($it.Form -ne "") { $hasMarker = $true }
  }
  if ($hasMarker) { $stat.markerRows++ }
  $stat.markers += ($fmt.Items | Where-Object { $allMarkers -contains $_.Zi }).Count

  $fanqie = Get-PlainText $cells[1]
  $midPos = Get-PlainText $cells[2]
  $qiepin = Get-PlainText $cells[3]
  $old    = Get-PlainText $cells[4]
  $note   = Get-PlainText $cells[5]
  $sf = if ($cur) { $cur.ShengFu } else { "" }
  $xy = if ($cur) { $cur.XieShengYu } else { "" }

  $mainRows.Add((@($sf, $xy, $fmt.Display, ($pureChars -join ' '), $fanqie, $midPos, $qiepin, $old, $note) | ForEach-Object { Esc $_ }) -join "`t")
  $stat.main++

  foreach ($it in $fmt.Items) {
    if ($allMarkers -contains $it.Zi) { continue }
    $longRows.Add((@($sf, $xy, $it.Zi, $it.Form, $fanqie, $midPos, $qiepin, $old, $note) | ForEach-Object { Esc $_ }) -join "`t")
    $stat.long++
  }
}

[System.IO.File]::WriteAllLines((Join-Path $OutDir '廣韻形聲考_主表.tsv'), $mainRows, $utf8)
[System.IO.File]::WriteAllLines((Join-Path $OutDir '廣韻形聲考_字表.tsv'), $longRows, $utf8)
[System.IO.File]::WriteAllLines((Join-Path $OutDir '廣韻形聲考_附錄.tsv'), $appRows, $utf8)

"主表資料行 = $($stat.main)"
"字表資料行 = $($stat.long)（只含真字，記號不另立行）"
"附錄資料行 = $($stat.appendix)"
"聲符組 = $($stat.groups)"
"含記號的行 = $($stat.markerRows)；記號總數 = $($stat.markers)"
