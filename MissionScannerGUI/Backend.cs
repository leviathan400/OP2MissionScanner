using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace MissionScannerGUI
{
    // Wraps the OPU MissionScanner CLI. Launches it, captures stdout, parses each row.
    // Row format (legend suppressed with -L):
    //   DLL NAME TYP # U MAP NAME          TECH TREE NAME          MISSION DESCRIPTION
    //   CEP1     Col 2 F cm02.map          MULTITEK.TXT            Colony Builder - Eden, Population
    internal static class Backend
    {
        // 6 whitespace-separated fields, then description = rest of line.
        // Safe because DLL/Type/#/U/Map/Techtree never contain whitespace.
        private static readonly Regex RowRegex = new Regex(
            @"^(\S+)\s+(\S+)\s+(\S+)\s+(\S+)\s+(\S+)\s+(\S+)\s+(.+)$",
            RegexOptions.Compiled);

        public sealed class Result
        {
            public List<MissionRow> Rows { get; } = new List<MissionRow>();
            public string RawOutput { get; set; }
            public int ExitCode { get; set; }
            public string Error { get; set; }
        }

        public static Result Run(string backendExe, string scanFolder)
        {
            var result = new Result();

            if (string.IsNullOrWhiteSpace(backendExe) || !File.Exists(backendExe))
            {
                result.Error = "Backend executable not found:\r\n" + backendExe;
                return result;
            }
            if (string.IsNullOrWhiteSpace(scanFolder) || !Directory.Exists(scanFolder))
            {
                result.Error = "Scan folder not found:\r\n" + scanFolder;
                return result;
            }

            var psi = new ProcessStartInfo
            {
                FileName = backendExe,
                Arguments = "-L \"" + scanFolder.TrimEnd('\\') + "\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                WorkingDirectory = Path.GetDirectoryName(backendExe) ?? string.Empty,
            };

            var stdout = new StringBuilder();
            var stderr = new StringBuilder();
            using (var p = new Process { StartInfo = psi })
            {
                p.OutputDataReceived += (s, e) => { if (e.Data != null) stdout.AppendLine(e.Data); };
                p.ErrorDataReceived += (s, e) => { if (e.Data != null) stderr.AppendLine(e.Data); };
                p.Start();
                p.BeginOutputReadLine();
                p.BeginErrorReadLine();
                p.WaitForExit();
                result.ExitCode = p.ExitCode;
            }

            result.RawOutput = stdout.ToString();
            ParseRows(result.RawOutput, result.Rows);

            if (result.Rows.Count == 0 && stderr.Length > 0)
            {
                result.Error = stderr.ToString().Trim();
            }
            return result;
        }

        private static void ParseRows(string output, List<MissionRow> rows)
        {
            using (var reader = new StringReader(output))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    // Skip header and blank lines
                    if (line.Length == 0 || line.StartsWith("DLL NAME", StringComparison.Ordinal))
                        continue;

                    var m = RowRegex.Match(line);
                    if (!m.Success) continue;

                    rows.Add(new MissionRow
                    {
                        DllName      = m.Groups[1].Value,
                        Type         = m.Groups[2].Value,
                        Players      = m.Groups[3].Value,
                        UnitOnly     = m.Groups[4].Value,
                        MapName      = m.Groups[5].Value,
                        TechtreeName = m.Groups[6].Value,
                        Description  = m.Groups[7].Value.TrimEnd(),
                    });
                }
            }
        }
    }
}
