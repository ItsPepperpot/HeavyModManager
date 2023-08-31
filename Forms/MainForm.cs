﻿using HeavyModManager.Classes;
using HeavyModManager.Enum;
using HeavyModManager.Forms;
using HeavyModManager.Functions;
using System.Text.Json;

namespace HeavyModManager;

public partial class MainForm : Form
{
    public MainForm()
    {
        InitializeComponent();

        foreach (Game game in ModManager.Games)
            comboBoxGame.Items.Add(new ComboBoxGameItem(game));

        ModManager.LoadSettings();

        checkForUpdatesOnStartupToolStripMenuItem.Checked = ModManager.CheckForUpdatesOnStartup;

        if (ModManager.CheckForUpdatesOnStartup)
        {
            TryUpdate();
        }

        if (ModManager.CurrentGame == Game.Null)
            comboBoxGame.SelectedIndex = -1;
        else
            for (int i = 0; i < comboBoxGame.Items.Count; i++)
                if (((ComboBoxGameItem)comboBoxGame.Items[i]).Game == ModManager.CurrentGame)
                {
                    comboBoxGame.SelectedIndex = i;
                    break;
                }

        labelModInfo.AutoSize = true;
        labelModInfo.MaximumSize = new Size(panelLabelModInfo.Width - SystemInformation.VerticalScrollBarWidth, 0);
        labelModInfo.Text = "";

        UpdateDolphinLabel();
    }

    private async void TryUpdate()
    {
        if (await AutomaticUpdater.Update())
        {
            Close();
            System.Diagnostics.Process.Start(Path.Combine(Application.StartupPath, "HeavyModManager.exe"));
        }
    }

    private void comboBoxGame_SelectedIndexChanged(object sender, EventArgs e)
    {
        ModManager.SetCurrentGame(comboBoxGame.SelectedIndex == -1 ? Game.Null : ((ComboBoxGameItem)comboBoxGame.SelectedItem).Game);

        PopulateModList();

        groupBoxMods.Enabled = comboBoxGame.SelectedIndex != -1;
        buttonApplyMods.Enabled = comboBoxGame.SelectedIndex != -1 && ModManager.GameBackupExists;
        buttonRunGame.Enabled = comboBoxGame.SelectedIndex != -1 && ModManager.GameExists;
        buttonCreateBackup.Enabled = comboBoxGame.SelectedIndex != -1;

        ModManager.SaveSettings();
    }

    private void createModToolStripMenuItem_Click(object sender, EventArgs e)
    {
        new CreateMod().ShowDialog();
        ModManager.RefreshModList();
        PopulateModList();
    }

    private void editModToolStripMenuItem_Click(object sender, EventArgs e)
    {
        if (listMods.SelectedIndices.Count == 1)
        {
            var mod = (Mod)listMods.SelectedItem;
            var prev = listMods.SelectedIndex;
            new CreateMod(mod).ShowDialog();
            ModManager.RefreshModList();
            PopulateModList();
            listMods.SelectedIndex = prev;
        }
    }

    private void deleteModToolStripMenuItem_Click(object sender, EventArgs e)
    {
        if (listMods.SelectedIndices.Count == 1)
        {
            var mod = (Mod)listMods.SelectedItem;
            var dr = MessageBox.Show($"Are you sure you want to delete the mod '{mod.ModName}' by '{mod.Author}'?", "Delete Mod", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (dr == DialogResult.Yes)
                ModManager.DeleteMod(mod.ModId);
            PopulateModList();
        }
    }

    private void zipModToolStripMenuItem_Click(object sender, EventArgs e)
    {
        if (listMods.SelectedIndices.Count == 1)
        {
            var mod = (Mod)listMods.SelectedItem;
            try
            {
                ZipManager.ZipMod(mod.ModId, $"{ModManager.GameToStringFull(mod.Game).Replace(":", "")} - {mod.Author} - {mod.ModName}");
            }
            catch (Exception ex)
            {
                MessageBox.Show("There was an error creating your mod ZIP archive: " + ex.Message);
            }
        }
    }

    private bool programChangingData = false;

    private bool activeModWithCheats = false;

    private void PopulateModList()
    {
        programChangingData = true;

        labelModInfo.Text = "";
        listMods.Items.Clear();

        editModToolStripMenuItem.Enabled = false;
        deleteModToolStripMenuItem.Enabled = false;
        zipModToolStripMenuItem.Enabled = false;

        activeModWithCheats = false;

        foreach (var modId in ModManager.CurrentGameSettings.Mods)
        {
            var mod = JsonSerializer.Deserialize<Mod>(File.ReadAllText(ModManager.GetModJsonPath(modId)));
            bool active = ModManager.CurrentGameSettings.ActiveMods.Contains(mod.ModId);
            listMods.Items.Add(mod, active);

            if (active)
                SetActiveModWithCheats(mod);
        }

        UpdateDolphinLabel();

        programChangingData = false;
    }

    private void SetActiveModWithCheats(Mod mod)
    {
        if (!string.IsNullOrEmpty(mod.ArCodes) || !string.IsNullOrEmpty(mod.GeckoCodes))
            activeModWithCheats = true;
    }

    private void listMods_ItemCheck(object sender, ItemCheckEventArgs e)
    {
        if (programChangingData)
            return;

        var mod = (Mod)listMods.Items[e.Index];

        if (e.NewValue == CheckState.Checked)
            ModManager.CurrentGameSettings.ActivateMod(mod.ModId);
        else
            ModManager.CurrentGameSettings.DeactivateMod(mod.ModId);

        SetActiveModWithCheats(mod);
        ModManager.SaveGameSettings();
    }

    private void listMods_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (listMods.SelectedIndex == -1)
        {
            labelModInfo.Text = "";
            editModToolStripMenuItem.Enabled = false;
            deleteModToolStripMenuItem.Enabled = false;
            zipModToolStripMenuItem.Enabled = false;
        }
        else
        {
            var mod = (Mod)listMods.SelectedItem;
            labelModInfo.Text = $"Author: {mod.Author}\n\n" +
                $"Description:\n{mod.Description}\n\n" +
                $"Mod ID:\n{mod.ModId}\n\n" +
                //$"Has AR codes: {(string.IsNullOrEmpty(mod.ArCodes) ? "No" : "Yes")}\n" +
                //$"Has Gecko codes: {(string.IsNullOrEmpty(mod.GeckoCodes) ? "No" : "Yes")}" +
                "";
            editModToolStripMenuItem.Enabled = true;
            deleteModToolStripMenuItem.Enabled = true;
            zipModToolStripMenuItem.Enabled = true;
        }
    }

    private void buttonMoveUp_Click(object sender, EventArgs e)
    {
        if (listMods.SelectedItems.Count == 1)
        {
            int previndex = listMods.SelectedIndex;

            if (previndex > 0)
            {
                var previous = ModManager.CurrentGameSettings.Mods[previndex - 1];
                ModManager.CurrentGameSettings.Mods[previndex - 1] = ModManager.CurrentGameSettings.Mods[previndex];
                ModManager.CurrentGameSettings.Mods[previndex] = previous;
                ModManager.CurrentGameSettings.Invalidated = true;

                ModManager.SaveGameSettings();
            }

            PopulateModList();
            listMods.SelectedIndex = Math.Max(previndex - 1, 0);
        }
    }

    private void buttonMoveDown_Click(object sender, EventArgs e)
    {
        if (listMods.SelectedItems.Count == 1)
        {
            int previndex = listMods.SelectedIndex;

            if (previndex < listMods.Items.Count - 1)
            {
                var post = ModManager.CurrentGameSettings.Mods[previndex + 1];
                ModManager.CurrentGameSettings.Mods[previndex + 1] = ModManager.CurrentGameSettings.Mods[previndex];
                ModManager.CurrentGameSettings.Mods[previndex] = post;
                ModManager.CurrentGameSettings.Invalidated = true;

                ModManager.SaveGameSettings();
            }

            PopulateModList();
            listMods.SelectedIndex = Math.Min(previndex + 1, listMods.Items.Count - 1);
        }
    }

    private void buttonRestoreBackup_Click(object sender, EventArgs e)
    {
        var openFile = new OpenFileDialog()
        {
            Filter = "main.dol|main.dol",
            Title = "Please select your game's main.dol from an unchanged Dolphin dump."
        };

        if (openFile.ShowDialog() == DialogResult.OK)
        {
            Enabled = false;
            ModManager.RestoreBackup(Path.GetDirectoryName(Path.GetDirectoryName(openFile.FileName)));
            Enabled = true;

            buttonApplyMods.Enabled = comboBoxGame.SelectedIndex != -1 && ModManager.GameBackupExists;
            buttonRunGame.Enabled = comboBoxGame.SelectedIndex != -1 && ModManager.GameExists;
        }
    }

    private void buttonApplyMods_Click(object sender, EventArgs e)
    {
        Enabled = false;
        ModManager.ApplyMods(developerModeToolStripMenuItem.Checked);
        Enabled = true;
        buttonRunGame.Enabled = comboBoxGame.SelectedIndex != -1 && ModManager.GameExists;
    }

    private void buttonRunGame_Click(object sender, EventArgs e)
    {
        Enabled = false;
        ModManager.ApplyMods(developerModeToolStripMenuItem.Checked);
        Enabled = true;
        ModManager.RunGame();
    }

    private void buttonAddMod_Click(object sender, EventArgs e)
    {
        ModManager.InstallMod();
        PopulateModList();
    }

    private void refreshModListToolStripMenuItem_Click(object sender, EventArgs e)
    {
        ModManager.RefreshModList();
        PopulateModList();
    }

    private void chooseDolphinPathToolStripMenuItem_Click(object sender, EventArgs e)
    {
        ModManager.SetDolphinPath();
        UpdateDolphinLabel();
    }

    private void developerModeToolStripMenuItem_Click(object sender, EventArgs e)
    {
        developerModeToolStripMenuItem.Checked = !developerModeToolStripMenuItem.Checked;
    }

    private void checkForUpdatesOnStartupToolStripMenuItem_Click(object sender, EventArgs e)
    {
        ModManager.CheckForUpdatesOnStartup = !ModManager.CheckForUpdatesOnStartup;
        checkForUpdatesOnStartupToolStripMenuItem.Checked = ModManager.CheckForUpdatesOnStartup;
    }

    private void UpdateDolphinLabel()
    {
        if (string.IsNullOrEmpty(ModManager.DolphinPath))
        {
            labelDolphin.Text = "Dolphin executable path not set.";
            return;
        }

        if (!File.Exists(ModManager.DolphinPath))
        {
            labelDolphin.Text = "Dolphin executable not found on set path.";
            return;
        }

        labelDolphin.Text = "Dolphin path: " + ModManager.DolphinPath;

        if (activeModWithCheats)
        {
            labelDolphin.Text += "\nOne or more active mods use codes. Remember to activate \"Enable Cheats\" on Dolphin settings.";
        }
    }
}