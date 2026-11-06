using System;
using System.Collections.Generic;
using System.IO;

namespace Ferrum.Installer;

/// <summary>Preserves replaced Ferrum files and restores them if the installation cannot commit.</summary>
public sealed class FileTransaction : IDisposable
{
    private readonly string backup;
    private readonly List<(string Target, string? Backup)> changes = new();
    private bool committed;
    public FileTransaction(string directory) { backup = Path.Combine(directory, "backups", DateTime.UtcNow.ToString("yyyyMMddTHHmmss") + "-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(backup); }
    private void Record(string target)
    {
        string? saved = null;
        if (File.Exists(target)) { saved = Path.Combine(backup, changes.Count.ToString() + "-" + Path.GetFileName(target)); File.Copy(target, saved); }
        changes.Add((target, saved));
    }
    public void Replace(string source, string target)
    {
        Record(target); Directory.CreateDirectory(Path.GetDirectoryName(target)!);
        string temp = target + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try { File.Copy(source, temp); if (File.Exists(target)) File.Move(temp, target, true); else File.Move(temp, target); }
        finally { if (File.Exists(temp)) File.Delete(temp); }
    }
    public void Remove(string target) { if (File.Exists(target)) { Record(target); File.Delete(target); } }
    public void CreateText(string target, string text)
    {
        if (File.Exists(target)) return;
        Record(target); Directory.CreateDirectory(Path.GetDirectoryName(target)!); File.WriteAllText(target, text, new System.Text.UTF8Encoding(false));
    }
    public void Commit() => committed = true;
    public void Dispose()
    {
        if (committed) return;
        var failures = new List<Exception>();
        for (int i = changes.Count - 1; i >= 0; i--)
        {
            try { var change = changes[i]; if (change.Backup != null) File.Copy(change.Backup, change.Target, true); else if (File.Exists(change.Target)) File.Delete(change.Target); }
            catch (Exception error) { failures.Add(error); }
        }
        if (failures.Count != 0)
        {
            var error = new AggregateException("Restore failed; backups: " + backup, failures);
            error.Data["Ferrum.Backup"] = backup;
            throw error;
        }
    }
}
