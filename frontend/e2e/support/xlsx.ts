// Ghi .xlsx 1 sheet cho e2e import (@BR-f02-import-excel-012 @BR-f02-import-excel-034): zip bằng fflate (MIT),
// XML SpreadsheetML tối thiểu mà ClosedXML đọc được — chuỗi qua sharedStrings, số là ô số, ô trống bỏ qua.
import { strToU8, zipSync } from 'fflate';

export type XlsxCell = string | number | null | undefined;

const MAIN = 'http://schemas.openxmlformats.org/spreadsheetml/2006/main';
const REL = 'http://schemas.openxmlformats.org/officeDocument/2006/relationships';
const PKG_REL = 'http://schemas.openxmlformats.org/package/2006/relationships';
const CT = 'application/vnd.openxmlformats-officedocument.spreadsheetml';
const XML = '<?xml version="1.0" encoding="UTF-8" standalone="yes"?>';

const escape = (s: string) =>
  s.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;').replace(/"/g, '&quot;');

/** 0 → A, 25 → Z, 26 → AA. */
function column(index: number): string {
  let n = index + 1;
  let name = '';
  while (n > 0) {
    const m = (n - 1) % 26;
    name = String.fromCharCode(65 + m) + name;
    n = Math.floor((n - 1) / 26);
  }
  return name;
}

/** `rows[0]` là dòng tiêu đề (dòng Excel 1); `rows[i]` là dòng Excel i + 1. */
export function buildXlsx(rows: XlsxCell[][]): Buffer {
  const strings: string[] = [];
  const indexOf = new Map<string, number>();
  const shared = (s: string) => {
    let i = indexOf.get(s);
    if (i === undefined) {
      i = strings.length;
      strings.push(s);
      indexOf.set(s, i);
    }
    return i;
  };
  const sheetData = rows.map((cells, r) => {
    const xml = cells.map((value, c) => {
      if (value === null || value === undefined || value === '') return '';
      const ref = `${column(c)}${r + 1}`;
      return typeof value === 'number'
        ? `<c r="${ref}"><v>${value}</v></c>`
        : `<c r="${ref}" t="s"><v>${shared(value)}</v></c>`;
    }).join('');
    return `<row r="${r + 1}">${xml}</row>`;
  }).join('');

  const parts: Record<string, string> = {
    '[Content_Types].xml': `<Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">`
      + `<Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/>`
      + `<Default Extension="xml" ContentType="application/xml"/>`
      + `<Override PartName="/xl/workbook.xml" ContentType="${CT}.sheet.main+xml"/>`
      + `<Override PartName="/xl/worksheets/sheet1.xml" ContentType="${CT}.worksheet+xml"/>`
      + `<Override PartName="/xl/sharedStrings.xml" ContentType="${CT}.sharedStrings+xml"/>`
      + `<Override PartName="/xl/styles.xml" ContentType="${CT}.styles+xml"/>`
      + `</Types>`,
    '_rels/.rels': `<Relationships xmlns="${PKG_REL}">`
      + `<Relationship Id="rId1" Type="${REL}/officeDocument" Target="xl/workbook.xml"/></Relationships>`,
    'xl/workbook.xml': `<workbook xmlns="${MAIN}" xmlns:r="${REL}"><sheets>`
      + `<sheet name="Câu trả lời biểu mẫu 1" sheetId="1" r:id="rId1"/></sheets></workbook>`,
    'xl/_rels/workbook.xml.rels': `<Relationships xmlns="${PKG_REL}">`
      + `<Relationship Id="rId1" Type="${REL}/worksheet" Target="worksheets/sheet1.xml"/>`
      + `<Relationship Id="rId2" Type="${REL}/sharedStrings" Target="sharedStrings.xml"/>`
      + `<Relationship Id="rId3" Type="${REL}/styles" Target="styles.xml"/></Relationships>`,
    'xl/worksheets/sheet1.xml': `<worksheet xmlns="${MAIN}"><sheetData>${sheetData}</sheetData></worksheet>`,
    'xl/sharedStrings.xml': `<sst xmlns="${MAIN}" count="${strings.length}" uniqueCount="${strings.length}">`
      + strings.map((s) => `<si><t xml:space="preserve">${escape(s)}</t></si>`).join('') + `</sst>`,
    'xl/styles.xml': `<styleSheet xmlns="${MAIN}">`
      + `<fonts count="1"><font><sz val="11"/><name val="Calibri"/></font></fonts>`
      + `<fills count="2"><fill><patternFill patternType="none"/></fill><fill><patternFill patternType="gray125"/></fill></fills>`
      + `<borders count="1"><border><left/><right/><top/><bottom/><diagonal/></border></borders>`
      + `<cellStyleXfs count="1"><xf numFmtId="0" fontId="0" fillId="0" borderId="0"/></cellStyleXfs>`
      + `<cellXfs count="1"><xf numFmtId="0" fontId="0" fillId="0" borderId="0" xfId="0"/></cellXfs>`
      + `</styleSheet>`,
  };
  const files = Object.fromEntries(Object.entries(parts).map(([path, xml]) => [path, strToU8(XML + xml)]));
  return Buffer.from(zipSync(files, { level: 6 }));
}
