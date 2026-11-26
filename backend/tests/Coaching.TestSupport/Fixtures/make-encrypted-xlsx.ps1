<#
.SYNOPSIS
Sinh Fixtures/encrypted.xlsx: workbook .xlsx tối thiểu (tiêu đề + 1 dòng) đặt mật khẩu mở file bằng chính Microsoft Excel.

.DESCRIPTION
@BR-f02-import-excel-014: ClosedXML không tạo được file mã hoá nên test dùng file này. Excel lưu .xlsx có mật khẩu
dưới dạng container OLE theo MS-OFFCRYPTO (agile encryption) — file bắt đầu bằng D0 CF 11 E0 A1 B1 1A E1.
Cần Windows có Microsoft Excel (COM). Chạy một lần, commit file sinh ra:
  powershell -NoProfile -File backend/tests/Coaching.TestSupport/Fixtures/make-encrypted-xlsx.ps1 backend/tests/Coaching.TestSupport/Fixtures/encrypted.xlsx
#>
param([Parameter(Mandatory = $true)][string] $OutFile)

$ErrorActionPreference = 'Stop'
$target = [System.IO.Path]::GetFullPath($OutFile)
if (Test-Path $target) { Remove-Item $target -Confirm:$false }

$excel = New-Object -ComObject Excel.Application
try {
    $excel.Visible = $false
    $excel.DisplayAlerts = $false
    $workbook = $excel.Workbooks.Add()
    $sheet = $workbook.Worksheets.Item(1)
    $sheet.Cells.Item(1, 1).Value2 = 'Địa chỉ email'
    $sheet.Cells.Item(1, 2).Value2 = 'Họ Và Tên (Name)'
    $sheet.Cells.Item(2, 1).Value2 = 'khoa@gmail.com'
    $sheet.Cells.Item(2, 2).Value2 = 'Khoa'
    # 51 = xlOpenXMLWorkbook (.xlsx); tham số thứ 3 = mật khẩu mở file
    $workbook.SaveAs($target, 51, 'Coach2026')
    $workbook.Close($false)
}
finally {
    $excel.Quit()
    [void][System.Runtime.InteropServices.Marshal]::ReleaseComObject($excel)
}

$head = [System.IO.File]::ReadAllBytes($target)[0..7] | ForEach-Object { $_.ToString('x2') }
Write-Output ("{0} ({1} bytes): {2}" -f $target, (Get-Item $target).Length, ($head -join ' '))
