using ExcelDataReader;
using System.Globalization;
using System.Text;

namespace ModerBox.QuestionBank;

internal static class LegacyExcelWorkbookReader {
    public static bool IsLegacyExcel(string filePath) {
        return string.Equals(Path.GetExtension(filePath), ".xls", StringComparison.OrdinalIgnoreCase);
    }

    public static IReadOnlyList<LegacyExcelWorksheet> ReadWorksheets(string filePath) {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        using var stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var reader = ExcelReaderFactory.CreateReader(stream);

        var worksheets = new List<LegacyExcelWorksheet>();

        do {
            var rows = new List<List<string>>();
            while (reader.Read()) {
                var row = new List<string>(reader.FieldCount);
                for (var i = 0; i < reader.FieldCount; i++) {
                    row.Add(GetCellString(reader.GetValue(i)));
                }

                rows.Add(row);
            }

            worksheets.Add(new LegacyExcelWorksheet(reader.Name, rows));
        } while (reader.NextResult());

        return worksheets;
    }

    private static string GetCellString(object? value) {
        return value switch {
            null => string.Empty,
            DateTime dateTime => dateTime.ToString(CultureInfo.InvariantCulture),
            IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture) ?? string.Empty,
            _ => value.ToString() ?? string.Empty
        };
    }
}

internal sealed class LegacyExcelWorksheet {
    private readonly IReadOnlyList<IReadOnlyList<string>> _rows;

    public LegacyExcelWorksheet(string name, IReadOnlyList<IReadOnlyList<string>> rows) {
        Name = name;
        _rows = rows;
    }

    public string Name { get; }

    public int LastRowNumber {
        get {
            for (var row = _rows.Count; row >= 1; row--) {
                if (!IsRowEmpty(row)) {
                    return row;
                }
            }

            return 0;
        }
    }

    public string GetString(int row, int column) {
        if (row <= 0 || column <= 0 || row > _rows.Count) {
            return string.Empty;
        }

        var rowData = _rows[row - 1];
        return column > rowData.Count ? string.Empty : rowData[column - 1].Trim();
    }

    public bool IsEmpty(int row, int column) {
        return string.IsNullOrWhiteSpace(GetString(row, column));
    }

    private bool IsRowEmpty(int row) {
        if (row <= 0 || row > _rows.Count) {
            return true;
        }

        return _rows[row - 1].All(string.IsNullOrWhiteSpace);
    }
}
