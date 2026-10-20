using System.Globalization;
using System.IO.Compression;
using System.Text;

namespace CareerSyncAI.Services;

/// <summary>Built-in cell styles (bold header, percentages, dates…).</summary>
public enum XlsxStyle
{
    Default = 0,
    Title = 1,      // large bold navy
    Subtitle = 2,   // grey
    Header = 3,     // bold white on navy, wraps
    Integer = 4,    // 1,234
    Decimal = 5,    // 4.5
    Percent = 6,    // 84%   (store 0.84)
    DateTime = 7,   // 2026-10-14 09:30
    Bold = 8,
    Wrap = 9,       // long text that wraps
    Date = 10,      // 2026-10-14
}

public sealed record XlsxCell(object? Value, XlsxStyle Style = XlsxStyle.Default);

/// <summary>One worksheet. Add a title, then one table (header + rows).</summary>
public sealed class XlsxSheet
{
    internal readonly List<List<XlsxCell>> Rows = new();
    internal readonly List<double> ColumnWidths = new();
    internal int? HeaderRow;      // 1-based row number of the table header
    internal int TableColumns;

    public string Name { get; }
    public XlsxSheet(string name) => Name = name;

    public XlsxSheet SetColumnWidths(params double[] widths)
    {
        ColumnWidths.Clear();
        ColumnWidths.AddRange(widths);
        return this;
    }

    public XlsxSheet AddRow(params XlsxCell[] cells) { Rows.Add(cells.ToList()); return this; }
    public XlsxSheet AddBlankRow() { Rows.Add(new List<XlsxCell>()); return this; }
    public XlsxSheet AddTitle(string text) => AddRow(new XlsxCell(text, XlsxStyle.Title));
    public XlsxSheet AddSubtitle(string text) => AddRow(new XlsxCell(text, XlsxStyle.Subtitle));

    /// <summary>Adds a table. The header row is frozen, gets filter buttons and repeats on every printed page.
    /// Each column gets its style from <paramref name="columnStyles"/>; pass an XlsxCell as a value to override one cell.</summary>
    public XlsxSheet AddTable(string[] headers, XlsxStyle[] columnStyles, IEnumerable<object?[]> rows)
    {
        if (columnStyles.Length != headers.Length) throw new ArgumentException("One style per column is required.", nameof(columnStyles));
        Rows.Add(headers.Select(h => new XlsxCell(h, XlsxStyle.Header)).ToList());
        HeaderRow = Rows.Count;
        TableColumns = headers.Length;
        foreach (var row in rows)
            Rows.Add(row.Select((value, i) => value as XlsxCell   // a value can carry its own style
                ?? new XlsxCell(value, i < columnStyles.Length ? columnStyles[i] : XlsxStyle.Default)).ToList());
        return this;
    }
}

/// <summary>
/// Writes a real Excel workbook (.xlsx, Office Open XML) without any NuGet package.
/// Supports several sheets, styled headers, number/percent/date formats, column
/// widths, frozen header rows, filter buttons and print settings (landscape A4,
/// fit to page width, header row repeated on every page, page numbers).
/// </summary>
public sealed class XlsxWriter
{
    private const string MainNs = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
    private const string RelNs = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
    private const int MaxCellText = 32_767;   // Excel's limit per cell

    private readonly List<XlsxSheet> _sheets = new();
    private readonly HashSet<string> _names = new(StringComparer.OrdinalIgnoreCase);

    public string Title { get; set; } = "Report";
    public string Author { get; set; } = "CareerSync AI";

    public XlsxSheet AddSheet(string name)
    {
        var sheet = new XlsxSheet(UniqueSheetName(name));
        _sheets.Add(sheet);
        return sheet;
    }

    public byte[] ToBytes()
    {
        if (_sheets.Count == 0) AddSheet("Sheet1");

        using var output = new MemoryStream();
        using (var zip = new ZipArchive(output, ZipArchiveMode.Create, leaveOpen: true))
        {
            Write(zip, "[Content_Types].xml", ContentTypes());
            Write(zip, "_rels/.rels", RootRels());
            Write(zip, "docProps/core.xml", CoreProps());
            Write(zip, "docProps/app.xml", AppProps());
            Write(zip, "xl/workbook.xml", Workbook());
            Write(zip, "xl/_rels/workbook.xml.rels", WorkbookRels());
            Write(zip, "xl/styles.xml", Styles());
            for (var i = 0; i < _sheets.Count; i++)
                Write(zip, $"xl/worksheets/sheet{i + 1}.xml", Worksheet(_sheets[i], i == 0));
        }
        return output.ToArray();
    }

    // ------------------------------------------------------------------ parts

    private string ContentTypes()
    {
        var sb = new StringBuilder();
        sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
        sb.Append("<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\">");
        sb.Append("<Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/>");
        sb.Append("<Default Extension=\"xml\" ContentType=\"application/xml\"/>");
        sb.Append("<Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/>");
        for (var i = 1; i <= _sheets.Count; i++)
            sb.Append($"<Override PartName=\"/xl/worksheets/sheet{i}.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/>");
        sb.Append("<Override PartName=\"/xl/styles.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml\"/>");
        sb.Append("<Override PartName=\"/docProps/core.xml\" ContentType=\"application/vnd.openxmlformats-package.core-properties+xml\"/>");
        sb.Append("<Override PartName=\"/docProps/app.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.extended-properties+xml\"/>");
        sb.Append("</Types>");
        return sb.ToString();
    }

    private static string RootRels() =>
        "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
        "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
        "<Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"xl/workbook.xml\"/>" +
        "<Relationship Id=\"rId2\" Type=\"http://schemas.openxmlformats.org/package/2006/relationships/metadata/core-properties\" Target=\"docProps/core.xml\"/>" +
        "<Relationship Id=\"rId3\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/extended-properties\" Target=\"docProps/app.xml\"/>" +
        "</Relationships>";

    private string CoreProps()
    {
        var now = System.DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture);
        return "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
               "<cp:coreProperties xmlns:cp=\"http://schemas.openxmlformats.org/package/2006/metadata/core-properties\" " +
               "xmlns:dc=\"http://purl.org/dc/elements/1.1/\" xmlns:dcterms=\"http://purl.org/dc/terms/\" " +
               "xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\">" +
               $"<dc:title>{Escape(Title)}</dc:title><dc:creator>{Escape(Author)}</dc:creator>" +
               $"<dcterms:created xsi:type=\"dcterms:W3CDTF\">{now}</dcterms:created>" +
               $"<dcterms:modified xsi:type=\"dcterms:W3CDTF\">{now}</dcterms:modified>" +
               "</cp:coreProperties>";
    }

    private static string AppProps() =>
        "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
        "<Properties xmlns=\"http://schemas.openxmlformats.org/officeDocument/2006/extended-properties\">" +
        "<Application>CareerSync AI</Application></Properties>";

    private string Workbook()
    {
        var sb = new StringBuilder();
        sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
        sb.Append($"<workbook xmlns=\"{MainNs}\" xmlns:r=\"{RelNs}\">");
        sb.Append("<bookViews><workbookView activeTab=\"0\"/></bookViews><sheets>");
        for (var i = 0; i < _sheets.Count; i++)
            sb.Append($"<sheet name=\"{Escape(_sheets[i].Name)}\" sheetId=\"{i + 1}\" r:id=\"rId{i + 1}\"/>");
        sb.Append("</sheets>");

        // Filter range and "repeat header row on every printed page".
        var names = new StringBuilder();
        for (var i = 0; i < _sheets.Count; i++)
        {
            var s = _sheets[i];
            if (s.HeaderRow is not int h) continue;
            var quoted = "'" + s.Name.Replace("'", "''") + "'";
            var lastCol = ColumnName(s.TableColumns);
            var lastRow = Math.Max(h, s.Rows.Count);
            names.Append($"<definedName name=\"_xlnm._FilterDatabase\" localSheetId=\"{i}\" hidden=\"1\">{Escape(quoted)}!$A${h}:${lastCol}${lastRow}</definedName>");
            names.Append($"<definedName name=\"_xlnm.Print_Titles\" localSheetId=\"{i}\">{Escape(quoted)}!${h}:${h}</definedName>");
        }
        if (names.Length > 0) sb.Append("<definedNames>").Append(names).Append("</definedNames>");
        sb.Append("</workbook>");
        return sb.ToString();
    }

    private string WorkbookRels()
    {
        var sb = new StringBuilder();
        sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
        sb.Append("<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">");
        for (var i = 1; i <= _sheets.Count; i++)
            sb.Append($"<Relationship Id=\"rId{i}\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet{i}.xml\"/>");
        sb.Append($"<Relationship Id=\"rId{_sheets.Count + 1}\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles\" Target=\"styles.xml\"/>");
        sb.Append("</Relationships>");
        return sb.ToString();
    }

    /// <summary>Style indexes here must match the XlsxStyle enum values.</summary>
    private static string Styles() =>
        "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
        $"<styleSheet xmlns=\"{MainNs}\">" +
        "<numFmts count=\"3\">" +
        "<numFmt numFmtId=\"164\" formatCode=\"yyyy-mm-dd hh:mm\"/>" +
        "<numFmt numFmtId=\"165\" formatCode=\"yyyy-mm-dd\"/>" +
        "<numFmt numFmtId=\"166\" formatCode=\"0.0\"/>" +
        "</numFmts>" +
        "<fonts count=\"5\">" +
        "<font><sz val=\"11\"/><color rgb=\"FF16202A\"/><name val=\"Calibri\"/><family val=\"2\"/></font>" +
        "<font><b/><sz val=\"16\"/><color rgb=\"FF122647\"/><name val=\"Calibri\"/><family val=\"2\"/></font>" +
        "<font><sz val=\"11\"/><color rgb=\"FF5B6B7C\"/><name val=\"Calibri\"/><family val=\"2\"/></font>" +
        "<font><b/><sz val=\"11\"/><color rgb=\"FFFFFFFF\"/><name val=\"Calibri\"/><family val=\"2\"/></font>" +
        "<font><b/><sz val=\"11\"/><color rgb=\"FF16202A\"/><name val=\"Calibri\"/><family val=\"2\"/></font>" +
        "</fonts>" +
        "<fills count=\"3\">" +
        "<fill><patternFill patternType=\"none\"/></fill>" +
        "<fill><patternFill patternType=\"gray125\"/></fill>" +
        "<fill><patternFill patternType=\"solid\"><fgColor rgb=\"FF122647\"/><bgColor indexed=\"64\"/></patternFill></fill>" +
        "</fills>" +
        "<borders count=\"2\">" +
        "<border><left/><right/><top/><bottom/><diagonal/></border>" +
        "<border><left/><right/><top/><bottom style=\"thin\"><color rgb=\"FFE3E9ED\"/></bottom><diagonal/></border>" +
        "</borders>" +
        "<cellStyleXfs count=\"1\"><xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\"/></cellStyleXfs>" +
        "<cellXfs count=\"11\">" +
        "<xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"1\" xfId=\"0\" applyBorder=\"1\" applyAlignment=\"1\"><alignment vertical=\"top\"/></xf>" +             // 0 Default
        "<xf numFmtId=\"0\" fontId=\"1\" fillId=\"0\" borderId=\"0\" xfId=\"0\" applyFont=\"1\"/>" +                                                                // 1 Title
        "<xf numFmtId=\"0\" fontId=\"2\" fillId=\"0\" borderId=\"0\" xfId=\"0\" applyFont=\"1\"/>" +                                                                // 2 Subtitle
        "<xf numFmtId=\"0\" fontId=\"3\" fillId=\"2\" borderId=\"0\" xfId=\"0\" applyFont=\"1\" applyFill=\"1\" applyAlignment=\"1\"><alignment vertical=\"center\" wrapText=\"1\"/></xf>" + // 3 Header
        "<xf numFmtId=\"3\" fontId=\"0\" fillId=\"0\" borderId=\"1\" xfId=\"0\" applyNumberFormat=\"1\" applyBorder=\"1\" applyAlignment=\"1\"><alignment vertical=\"top\"/></xf>" +   // 4 Integer
        "<xf numFmtId=\"166\" fontId=\"0\" fillId=\"0\" borderId=\"1\" xfId=\"0\" applyNumberFormat=\"1\" applyBorder=\"1\" applyAlignment=\"1\"><alignment vertical=\"top\"/></xf>" + // 5 Decimal
        "<xf numFmtId=\"9\" fontId=\"0\" fillId=\"0\" borderId=\"1\" xfId=\"0\" applyNumberFormat=\"1\" applyBorder=\"1\" applyAlignment=\"1\"><alignment vertical=\"top\"/></xf>" +   // 6 Percent
        "<xf numFmtId=\"164\" fontId=\"0\" fillId=\"0\" borderId=\"1\" xfId=\"0\" applyNumberFormat=\"1\" applyBorder=\"1\" applyAlignment=\"1\"><alignment horizontal=\"left\" vertical=\"top\"/></xf>" + // 7 DateTime
        "<xf numFmtId=\"0\" fontId=\"4\" fillId=\"0\" borderId=\"1\" xfId=\"0\" applyFont=\"1\" applyBorder=\"1\" applyAlignment=\"1\"><alignment vertical=\"top\"/></xf>" +       // 8 Bold
        "<xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"1\" xfId=\"0\" applyBorder=\"1\" applyAlignment=\"1\"><alignment vertical=\"top\" wrapText=\"1\"/></xf>" +      // 9 Wrap
        "<xf numFmtId=\"165\" fontId=\"0\" fillId=\"0\" borderId=\"1\" xfId=\"0\" applyNumberFormat=\"1\" applyBorder=\"1\" applyAlignment=\"1\"><alignment horizontal=\"left\" vertical=\"top\"/></xf>" + // 10 Date
        "</cellXfs>" +
        "<cellStyles count=\"1\"><cellStyle name=\"Normal\" xfId=\"0\" builtinId=\"0\"/></cellStyles>" +
        "</styleSheet>";

    private static string Worksheet(XlsxSheet sheet, bool selected)
    {
        var maxCols = Math.Max(1, Math.Max(sheet.ColumnWidths.Count, sheet.Rows.Count == 0 ? 1 : sheet.Rows.Max(r => r.Count)));
        var lastRow = Math.Max(1, sheet.Rows.Count);

        var sb = new StringBuilder();
        sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
        sb.Append($"<worksheet xmlns=\"{MainNs}\" xmlns:r=\"{RelNs}\">");
        sb.Append("<sheetPr><pageSetUpPr fitToPage=\"1\"/></sheetPr>");
        sb.Append($"<dimension ref=\"A1:{ColumnName(maxCols)}{lastRow}\"/>");

        sb.Append($"<sheetViews><sheetView workbookViewId=\"0\"{(selected ? " tabSelected=\"1\"" : "")}>");
        if (sheet.HeaderRow is int frozen)
            sb.Append($"<pane ySplit=\"{frozen}\" topLeftCell=\"A{frozen + 1}\" activePane=\"bottomLeft\" state=\"frozen\"/>" +
                      $"<selection pane=\"bottomLeft\" activeCell=\"A{frozen + 1}\" sqref=\"A{frozen + 1}\"/>");
        sb.Append("</sheetView></sheetViews>");
        sb.Append("<sheetFormatPr defaultRowHeight=\"15\"/>");

        if (sheet.ColumnWidths.Count > 0)
        {
            sb.Append("<cols>");
            for (var c = 0; c < sheet.ColumnWidths.Count; c++)
                sb.Append($"<col min=\"{c + 1}\" max=\"{c + 1}\" width=\"{sheet.ColumnWidths[c].ToString("0.##", CultureInfo.InvariantCulture)}\" customWidth=\"1\"/>");
            sb.Append("</cols>");
        }

        sb.Append("<sheetData>");
        for (var r = 0; r < sheet.Rows.Count; r++)
        {
            var rowNumber = r + 1;
            var isHeader = sheet.HeaderRow == rowNumber;
            sb.Append($"<row r=\"{rowNumber}\"{(isHeader ? " ht=\"30\" customHeight=\"1\"" : "")}>");
            for (var c = 0; c < sheet.Rows[r].Count; c++)
                AppendCell(sb, $"{ColumnName(c + 1)}{rowNumber}", sheet.Rows[r][c]);
            sb.Append("</row>");
        }
        sb.Append("</sheetData>");

        if (sheet.HeaderRow is int h)
            sb.Append($"<autoFilter ref=\"A{h}:{ColumnName(sheet.TableColumns)}{Math.Max(h, sheet.Rows.Count)}\"/>");

        sb.Append("<pageMargins left=\"0.4\" right=\"0.4\" top=\"0.6\" bottom=\"0.6\" header=\"0.3\" footer=\"0.3\"/>");
        sb.Append("<pageSetup paperSize=\"9\" orientation=\"landscape\" fitToWidth=\"1\" fitToHeight=\"0\"/>");
        sb.Append($"<headerFooter><oddFooter>{Escape("&L" + sheet.Name + "&RPage &P of &N")}</oddFooter></headerFooter>");
        sb.Append("</worksheet>");
        return sb.ToString();
    }

    private static void AppendCell(StringBuilder sb, string reference, XlsxCell cell)
    {
        var style = (int)cell.Style;
        switch (cell.Value)
        {
            case null:
                if (style != 0) sb.Append($"<c r=\"{reference}\" s=\"{style}\"/>");
                return;
            case System.DateTime dt:
                sb.Append($"<c r=\"{reference}\" s=\"{(cell.Style is XlsxStyle.Date ? style : (int)XlsxStyle.DateTime)}\"><v>{ToOaDate(dt)}</v></c>");
                return;
            case DateTimeOffset dto:
                sb.Append($"<c r=\"{reference}\" s=\"{(cell.Style is XlsxStyle.Date ? style : (int)XlsxStyle.DateTime)}\"><v>{ToOaDate(dto.LocalDateTime)}</v></c>");
                return;
            case bool b:
                sb.Append($"<c r=\"{reference}\" s=\"{style}\" t=\"b\"><v>{(b ? 1 : 0)}</v></c>");
                return;
            case int or long or short or byte or decimal or double or float:
                var number = Convert.ToDouble(cell.Value, CultureInfo.InvariantCulture);
                if (double.IsNaN(number) || double.IsInfinity(number))
                {
                    if (style != 0) sb.Append($"<c r=\"{reference}\" s=\"{style}\"/>");
                    return;
                }
                sb.Append($"<c r=\"{reference}\" s=\"{style}\"><v>{number.ToString("R", CultureInfo.InvariantCulture)}</v></c>");
                return;
            default:
                // Stored as inline text, never as a formula, so a value such as
                // "=HYPERLINK(...)" typed by a user can't run in Excel.
                var text = Clean(cell.Value.ToString() ?? string.Empty);
                var preserve = text.Length > 0 && (char.IsWhiteSpace(text[0]) || char.IsWhiteSpace(text[^1])) ? " xml:space=\"preserve\"" : "";
                sb.Append($"<c r=\"{reference}\" s=\"{style}\" t=\"inlineStr\"><is><t{preserve}>{Escape(text)}</t></is></c>");
                return;
        }
    }

    // ---------------------------------------------------------------- helpers

    private static void Write(ZipArchive zip, string path, string content)
    {
        var entry = zip.CreateEntry(path, CompressionLevel.Optimal);
        using var stream = entry.Open();
        var bytes = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false).GetBytes(content);
        stream.Write(bytes, 0, bytes.Length);
    }

    /// <summary>1 → A, 26 → Z, 27 → AA.</summary>
    public static string ColumnName(int column)
    {
        var name = string.Empty;
        while (column > 0)
        {
            var rem = (column - 1) % 26;
            name = (char)('A' + rem) + name;
            column = (column - 1) / 26;
        }
        return name;
    }

    private static string ToOaDate(System.DateTime value) =>
        value.ToOADate().ToString("R", CultureInfo.InvariantCulture);

    /// <summary>Removes characters XML can't contain and trims to Excel's per-cell limit.</summary>
    private static string Clean(string text)
    {
        var sb = new StringBuilder(text.Length);
        foreach (var ch in text)
            if (ch == '\t' || ch == '\n' || ch == '\r' || (ch >= 0x20 && ch != 0xFFFE && ch != 0xFFFF))
                sb.Append(ch);
        var cleaned = sb.ToString();
        return cleaned.Length > MaxCellText ? cleaned[..MaxCellText] : cleaned;
    }

    private static string Escape(string text) =>
        text.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");

    /// <summary>Excel sheet names: max 31 characters, none of : \ / ? * [ ], and unique.</summary>
    private string UniqueSheetName(string name)
    {
        var cleaned = new string((name ?? "Sheet").Where(ch => ":\\/?*[]".IndexOf(ch) < 0).ToArray()).Trim().Trim('\'');
        if (cleaned.Length == 0) cleaned = "Sheet";
        if (cleaned.Length > 31) cleaned = cleaned[..31];

        var candidate = cleaned;
        for (var n = 2; !_names.Add(candidate); n++)
        {
            var suffix = $" ({n})";
            candidate = (cleaned.Length + suffix.Length > 31 ? cleaned[..(31 - suffix.Length)] : cleaned) + suffix;
        }
        return candidate;
    }
}

