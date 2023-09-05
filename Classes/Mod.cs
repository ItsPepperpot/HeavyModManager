﻿using HeavyModManager.Enum;
using HeavyModManager.Functions;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace HeavyModManager.Classes;

public class Mod
{
    [JsonInclude]
    public Game Game { get; set; } = Game.Null;
    [JsonInclude]
    public string ModName { get; set; } = "";
    [JsonInclude]
    public string Author { get; set; } = "";
    [JsonInclude]
    public string Description { get; set; } = "";
    [JsonInclude]
    public string ModId { get; set; } = "";
    [JsonInclude]
    public string GameId { get; set; } = "";
    [JsonInclude]
    public string INIReplacements { get; set; } = "";
    [JsonInclude]
    public string MergeFiles { get; set; } = "";
    [JsonInclude]
    public string RemoveFiles { get; set; } = "";
    [JsonInclude]
    public string DOLPatches { get; set; } = "";
    [JsonInclude]
    public string ArCodes { get; set; } = "";
    [JsonInclude]
    public string GeckoCodes { get; set; } = "";
    [JsonInclude]
    public DateTime CreatedAt { get; set; }
    [JsonInclude]
    public DateTime UpdatedAt { get; set; }

    [JsonConstructor]
    public Mod()
    {
        CreatedAt = DateTime.Now.ToUniversalTime().Date;
        UpdatedAt = DateTime.Now.ToUniversalTime().Date;
    }

    public override string ToString()
    {
        return ModName;
    }

    public void SaveModJson(bool isEditing)
    {
        // Temporary
        ArCodes = "";
        GeckoCodes = "";

        var modPath = ModManager.GetModPath(ModId);

        if (!Directory.Exists(modPath))
            Directory.CreateDirectory(modPath);

        var modJsonPath = ModManager.GetModJsonPath(ModId);
        File.WriteAllText(modJsonPath, JsonSerializer.Serialize(this));

        if (!isEditing)
        {
            var files = Path.Combine(modPath, "files");
            if (!Directory.Exists(files))
                Directory.CreateDirectory(files);

            MessageBox.Show("Mod created at " + modPath);
            System.Diagnostics.Process.Start("explorer.exe", modPath);
        }
    }

    public void Apply()
    {
        TempMergeFiles = MergeFiles.Split('\n').Select(p => p.ToLower());

        RemoveRemoveFiles();

        var modFilesPath = ModManager.GetModFilesPath(ModId);
        CopyDirectory(modFilesPath, modFilesPath);
    }

    private void RemoveRemoveFiles()
    {
        if (!string.IsNullOrWhiteSpace(RemoveFiles))
            foreach (var path in RemoveFiles.Split('\n'))
            {
                if (string.IsNullOrWhiteSpace(path))
                    continue;

                var file = Path.Combine(ModManager.GameGameFilesPath, path);
                if (Directory.Exists(file))
                    Directory.Delete(file, true);
                else if (File.Exists(file))
                    File.Delete(file);
            }
    }

    [JsonIgnore]
    private IEnumerable<string> TempMergeFiles;

    private void CopyDirectory(string root, string path)
    {
        if (Directory.Exists(path))
        {
            foreach (var directory in Directory.GetDirectories(path))
                CopyDirectory(root, directory);

            foreach (var file in Directory.GetFiles(path))
            {
                var relativePath = Path.GetRelativePath(root, file);

                if (TempMergeFiles.Contains(relativePath.ToLower()))
                {
                    HipManager.Merge(file, Path.Combine(ModManager.GameGameFilesPath, relativePath));
                }
                else
                    File.Copy(file, Path.Combine(ModManager.GameGameFilesPath, relativePath), true);
            }
        }
    }
}