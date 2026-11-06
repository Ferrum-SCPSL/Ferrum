using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Ferrum.Contracts;

/// <summary>Strict two-column RFC 4180 catalog reader, including embedded newlines.</summary>
public static class CsvCatalog
{
    public static Dictionary<string, string> Read(TextReader reader)
    {
        var rows = new List<string[]>();
        var fields = new List<string>();
        var value = new StringBuilder();
        bool quoted = false, closed = false;
        int next;
        while ((next = reader.Read()) != -1)
        {
            char c = (char)next;
            if (quoted)
            {
                if (c == '"')
                {
                    if (reader.Peek() == '"') { reader.Read(); value.Append('"'); }
                    else { quoted = false; closed = true; }
                }
                else value.Append(c);
                continue;
            }
            if (c == '"' && value.Length == 0 && !closed) { quoted = true; continue; }
            if (c == ',' || c == '\r' || c == '\n')
            {
                fields.Add(value.ToString()); value.Clear(); closed = false;
                if (c != ',')
                {
                    if (c == '\r' && reader.Peek() == '\n') reader.Read();
                    if (fields.Count != 1 || fields[0].Length != 0) rows.Add(fields.ToArray());
                    fields.Clear();
                }
            }
            else
            {
                if (closed || c == '"') throw new InvalidDataException("Malformed CSV quoting.");
                value.Append(c);
            }
        }
        if (quoted) throw new InvalidDataException("Unterminated CSV field.");
        if (value.Length != 0 || fields.Count != 0 || closed) { fields.Add(value.ToString()); rows.Add(fields.ToArray()); }
        if (rows.Count == 0 || rows[0].Length != 2 || rows[0][0].TrimStart('\uFEFF') != "key" || rows[0][1] != "value")
            throw new InvalidDataException("CSV header must be key,value.");
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        for (int i = 1; i < rows.Count; i++)
        {
            if (rows[i].Length != 2 || string.IsNullOrWhiteSpace(rows[i][0]) || result.ContainsKey(rows[i][0]))
                throw new InvalidDataException("Invalid or duplicate CSV key.");
            result.Add(rows[i][0], rows[i][1]);
        }
        return result;
    }
}
