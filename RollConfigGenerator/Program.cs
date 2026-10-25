using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Forms;

internal static class Program
{
  [STAThread]
  private static void Main()
  {
    ApplicationConfiguration.Initialize();
    Application.Run(new ProjectEditorForm());
  }
}

public class ProjectEditorForm : Form
{
  private readonly string appFolder = AppContext.BaseDirectory;
  private const string StandardProjectFileName = "standard_project.txt";

  private string currentProjectPath;
  private bool isDirty = false;

  // ---------------------- Nozzels ----------------------
  private readonly TextBox nozzleIdInput = new();
  private readonly TextBox nozzleBrandInput = new();
  private readonly TextBox nozzleTypBezInput = new();
  private readonly TextBox nozzleTypeInput = new();
  private readonly TextBox nozzleMaxFlowLMinInput = new();
  private readonly TextBox nozzleRefPressureInput = new();
  private readonly TextBox nozzleAngleDegInput = new();

  private readonly TextBox nozzleOutput = new()
  {
    Multiline = true,
    ReadOnly = true,
    WordWrap = false,
    ScrollBars = ScrollBars.Both,
    Dock = DockStyle.Fill,
    Font = new Font("Consolas", 10),
    BackColor = Color.White
  };

  private readonly ListBox nozzleList = new() { Dock = DockStyle.Fill };
  private readonly List<NozzleData> nozzles = new();
  private int selectedNozzleIndex = -1;
  private bool internalNozzleSelectionChange = false;

  // -------------------- CoolingBank --------------------
  private readonly TextBox cbIdInput = new();
  private readonly TextBox cbTypeBankInput = new();

  private readonly NumericUpDown cbZoneCountInput = new()
  {
    Minimum = 1,
    Maximum = 3,
    Value = 1,
    Dock = DockStyle.Fill
  };

  private readonly TextBox cbTotalInput = new()
  {
    ReadOnly = true,
    Dock = DockStyle.Fill
  };

  private readonly TextBox[] zoneCountInputs = { new(), new(), new() };
  private readonly TextBox[] zoneDistanceInputs = { new(), new(), new() };

  private readonly ComboBox[] zoneNozzleInputs =
  {
        new() { DropDownStyle = ComboBoxStyle.DropDownList },
        new() { DropDownStyle = ComboBoxStyle.DropDownList },
        new() { DropDownStyle = ComboBoxStyle.DropDownList }
    };

  private readonly TextBox[] zoneGapInputs = { new(), new() };

  private readonly TextBox cbOutput = new()
  {
    Multiline = true,
    ReadOnly = true,
    WordWrap = false,
    ScrollBars = ScrollBars.Both,
    Dock = DockStyle.Fill,
    Font = new Font("Consolas", 10),
    BackColor = Color.White
  };

  private readonly ListBox cbList = new() { Dock = DockStyle.Fill };
  private readonly List<CoolingBankData> coolingBanks = new();
  private int selectedCbIndex = -1;
  private bool internalCbSelectionChange = false;

  // ---------------------- RollParam ----------------------
  private readonly TextBox rpIdInput = new() { Text = "workRollParam" };
  private readonly ComboBox rpTypeRollInput = new() { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill };
  private readonly NumericUpDown rpBankCountInput = new() { Minimum = 1, Maximum = 8, Value = 1, Dock = DockStyle.Fill };

  private readonly ComboBox[] rpBankTypeInputs = {
        new() { DropDownStyle = ComboBoxStyle.DropDownList },
        new() { DropDownStyle = ComboBoxStyle.DropDownList },
        new() { DropDownStyle = ComboBoxStyle.DropDownList },
        new() { DropDownStyle = ComboBoxStyle.DropDownList },
        new() { DropDownStyle = ComboBoxStyle.DropDownList },
        new() { DropDownStyle = ComboBoxStyle.DropDownList },
        new() { DropDownStyle = ComboBoxStyle.DropDownList },
        new() { DropDownStyle = ComboBoxStyle.DropDownList }
    };

  private readonly TextBox[] rpBankIndexInputs = { new(), new(), new(), new(), new(), new(), new(), new() };
  private readonly TextBox[] rpBankXPosInputs = { new(), new(), new(), new(), new(), new(), new(), new() };
  private readonly TextBox[] rpBankYPosInputs = { new(), new(), new(), new(), new(), new(), new(), new() };
  private readonly TextBox[] rpBankAngleInputs = { new(), new(), new(), new(), new(), new(), new(), new() };

  private readonly TextBox rpOutput = new()
  {
    Multiline = true,
    ReadOnly = true,
    WordWrap = false,
    ScrollBars = ScrollBars.Both,
    Dock = DockStyle.Fill,
    Font = new Font("Consolas", 10),
    BackColor = Color.White
  };

  private readonly ListBox rpList = new() { Dock = DockStyle.Fill };
  private readonly List<RollParamData> rollParams = new();
  private int selectedRpIndex = -1;
  private bool internalRpSelectionChange = false;

  // -------------------- Shared/UI state --------------------
  private SplitContainer nozzleSplit = null!;
  private SplitContainer cbSplit = null!;
  private SplitContainer rpSplit = null!;
  private TabControl tabs = null!;

  public ProjectEditorForm()
  {
    Text = "Project Editor";
    ClientSize = new Size(1500, 860);
    MinimumSize = new Size(1100, 700);
    StartPosition = FormStartPosition.CenterScreen;

    currentProjectPath = Path.Combine(appFolder, StandardProjectFileName);

    BuildLayout();
    HookAutoPreviewEvents();

    LoadStandardProjectIfExists();

    this.FormClosing += OnFormClosingAskSave;
    this.Shown += (_, _) => SetSplitsHalf();
    this.Resize += (_, _) => SetSplitsHalf();
  }

  // =========================================================
  // Layout
  // =========================================================
  private void BuildLayout()
  {
    var root = new TableLayoutPanel
    {
      Dock = DockStyle.Fill,
      ColumnCount = 1,
      RowCount = 2
    };
    root.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
    root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

    var menuHost = new Panel
    {
      Dock = DockStyle.Fill,
      Padding = new Padding(8, 6, 8, 6),
      BackColor = SystemColors.Control
    };

    MainMenuStrip = BuildMenu();
    MainMenuStrip.Dock = DockStyle.Fill;
    menuHost.Controls.Add(MainMenuStrip);

    tabs = new TabControl { Dock = DockStyle.Fill };

    var tabNozzels = new TabPage("Nozzels");
    tabNozzels.Controls.Add(BuildNozzelsTab());

    var tabCoolingBank = new TabPage("CoolingBank");
    tabCoolingBank.Controls.Add(BuildCoolingBankTab());

    var tabRollParam = new TabPage("RollParam");
    tabRollParam.Controls.Add(BuildRollParamTab());

    tabs.TabPages.Add(tabNozzels);
    tabs.TabPages.Add(tabCoolingBank);
    tabs.TabPages.Add(tabRollParam);

    root.Controls.Add(menuHost, 0, 0);
    root.Controls.Add(tabs, 0, 1);

    Controls.Add(root);
  }

  private MenuStrip BuildMenu()
  {
    var menu = new MenuStrip();
    var project = new ToolStripMenuItem("Projekt");

    var miLoad = new ToolStripMenuItem("Laden...");
    miLoad.Click += (_, _) => LoadProjectByDialog();

    var miSave = new ToolStripMenuItem("Speichern");
    miSave.Click += (_, _) => SaveProject(currentProjectPath, true);

    var miSaveAs = new ToolStripMenuItem("Speichern unter...");
    miSaveAs.Click += (_, _) => SaveProjectAs();

    var miExportNozzles = new ToolStripMenuItem("Export Nozzels env-TXT...");
    miExportNozzles.Click += (_, _) => ExportNozzleEnvBlocks();

    var miExportCooling = new ToolStripMenuItem("Export CoolingBank-TXT...");
    miExportCooling.Click += (_, _) => ExportCoolingBanks();

    var miExportRoll = new ToolStripMenuItem("Export RollParam-TXT...");
    miExportRoll.Click += (_, _) => ExportRollParams();

    var miExit = new ToolStripMenuItem("Beenden");
    miExit.Click += (_, _) => Close();

    project.DropDownItems.Add(miLoad);
    project.DropDownItems.Add(miSave);
    project.DropDownItems.Add(miSaveAs);
    project.DropDownItems.Add(new ToolStripSeparator());
    project.DropDownItems.Add(miExportNozzles);
    project.DropDownItems.Add(miExportCooling);
    project.DropDownItems.Add(miExportRoll);
    project.DropDownItems.Add(new ToolStripSeparator());
    project.DropDownItems.Add(miExit);

    menu.Items.Add(project);
    return menu;
  }

  private Control BuildNozzelsTab()
  {
    nozzleSplit = new SplitContainer { Dock = DockStyle.Fill, FixedPanel = FixedPanel.None };
    nozzleSplit.Panel1.Controls.Add(BuildNozzelsLeft());
    nozzleSplit.Panel2.Controls.Add(BuildNozzelsRight());
    return nozzleSplit;
  }

  private Control BuildCoolingBankTab()
  {
    cbSplit = new SplitContainer { Dock = DockStyle.Fill, FixedPanel = FixedPanel.None };
    cbSplit.Panel1.Controls.Add(BuildCoolingBankLeft());
    cbSplit.Panel2.Controls.Add(BuildCoolingBankRight());
    return cbSplit;
  }

  private Control BuildRollParamTab()
  {
    rpSplit = new SplitContainer { Dock = DockStyle.Fill, FixedPanel = FixedPanel.None };
    rpSplit.Panel1.Controls.Add(BuildRollParamLeft());
    rpSplit.Panel2.Controls.Add(BuildRollParamRight());
    return rpSplit;
  }

  // ---------------- Nozzels UI ----------------
  private Control BuildNozzelsLeft()
  {
    var panel = new TableLayoutPanel
    {
      Dock = DockStyle.Fill,
      ColumnCount = 2,
      RowCount = 10,
      Padding = new Padding(12)
    };

    panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 230));
    panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

    for (int i = 0; i < 8; i++) panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
    panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));
    panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

    AddInput(panel, 0, "ID", nozzleIdInput, "");
    AddInput(panel, 1, "Brand", nozzleBrandInput, "");
    AddInput(panel, 2, "nozzelTypbezeichnung", nozzleTypBezInput, "");
    AddInput(panel, 3, "typeNozzel", nozzleTypeInput, "");
    AddInput(panel, 4, "maxVolFlow [l/min]", nozzleMaxFlowLMinInput, "");
    AddInput(panel, 5, "refPressure", nozzleRefPressureInput, "");
    AddInput(panel, 6, "angleSpray [deg]", nozzleAngleDegInput, "");

    var btnRow = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, WrapContents = true };

    var bNew = new Button { Text = "Neu", AutoSize = true };
    var bApply = new Button { Text = "Übernehmen", AutoSize = true };
    var bCopy = new Button { Text = "Kopieren", AutoSize = true };
    var bDelete = new Button { Text = "Löschen", AutoSize = true };

    bNew.Click += (_, _) => ClearNozzleInputs();
    bApply.Click += (_, _) => AddOrUpdateNozzle();
    bCopy.Click += (_, _) => CopySelectedNozzle();
    bDelete.Click += (_, _) => DeleteNozzle();

    btnRow.Controls.AddRange(new Control[] { bNew, bApply, bCopy, bDelete });

    panel.Controls.Add(btnRow, 0, 8);
    panel.SetColumnSpan(btnRow, 2);

    panel.Controls.Add(nozzleOutput, 0, 9);
    panel.SetColumnSpan(nozzleOutput, 2);

    return panel;
  }

  private Control BuildNozzelsRight()
  {
    var panel = new TableLayoutPanel
    {
      Dock = DockStyle.Fill,
      ColumnCount = 1,
      RowCount = 3,
      Padding = new Padding(8)
    };
    panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
    panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
    panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));

    var title = new Label
    {
      Text = "Nozzles (ID / Kommentar)",
      Dock = DockStyle.Fill,
      TextAlign = ContentAlignment.MiddleLeft,
      Font = new Font(Font, FontStyle.Bold)
    };

    nozzleList.SelectedIndexChanged += OnNozzleSelected;
    var footer = new Label { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
    nozzleList.Tag = footer;

    panel.Controls.Add(title, 0, 0);
    panel.Controls.Add(nozzleList, 0, 1);
    panel.Controls.Add(footer, 0, 2);
    return panel;
  }

  // ---------------- CoolingBank UI ----------------
  private Control BuildCoolingBankLeft()
  {
    var panel = new TableLayoutPanel
    {
      Dock = DockStyle.Fill,
      ColumnCount = 2,
      RowCount = 7,
      Padding = new Padding(12)
    };

    panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160));
    panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

    for (int i = 0; i < 4; i++)
      panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));

    panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
    panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
    panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

    AddInput(panel, 0, "ID", cbIdInput, "");
    AddInput(panel, 1, "typeBank", cbTypeBankInput, "");

    panel.Controls.Add(new Label { Text = "Anzahl Zonen", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 2);
    panel.Controls.Add(cbZoneCountInput, 1, 2);

    panel.Controls.Add(new Label { Text = "noNozzel (Summe)", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 3);
    panel.Controls.Add(cbTotalInput, 1, 3);

    var zones = new TableLayoutPanel
    {
      Dock = DockStyle.Top,
      AutoSize = true,
      ColumnCount = 4,
      RowCount = 6,
      Margin = new Padding(0, 8, 0, 8)
    };

    zones.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100));
    zones.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
    zones.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35));
    zones.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));

    zones.Controls.Add(new Label { Text = "Zone", AutoSize = true }, 0, 0);
    zones.Controls.Add(new Label { Text = "Anzahl", AutoSize = true }, 1, 0);
    zones.Controls.Add(new Label { Text = "Abstand [m]", AutoSize = true }, 2, 0);
    zones.Controls.Add(new Label { Text = "Nozzle", AutoSize = true }, 3, 0);

    for (int i = 0; i < 3; i++)
    {
      int row = 1 + i * 2;

      zones.Controls.Add(new Label { Text = $"Zone {i + 1}", AutoSize = true, Anchor = AnchorStyles.Left }, 0, row);

      zoneCountInputs[i].Dock = DockStyle.Fill;
      zoneDistanceInputs[i].Dock = DockStyle.Fill;
      zoneNozzleInputs[i].Dock = DockStyle.Fill;

      zones.Controls.Add(zoneCountInputs[i], 1, row);
      zones.Controls.Add(zoneDistanceInputs[i], 2, row);
      zones.Controls.Add(zoneNozzleInputs[i], 3, row);

      if (i < 2)
      {
        zones.Controls.Add(new Label { Text = $"Übergang {i + 1} → {i + 2} [m]", AutoSize = true, Anchor = AnchorStyles.Left }, 0, row + 1);
        zoneGapInputs[i].Dock = DockStyle.Fill;
        zones.Controls.Add(zoneGapInputs[i], 1, row + 1);
        zones.SetColumnSpan(zoneGapInputs[i], 3);
      }
    }

    panel.Controls.Add(zones, 0, 4);
    panel.SetColumnSpan(zones, 2);

    var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, WrapContents = true };

    var newButton = new Button { Text = "Neu", AutoSize = true };
    var applyButton = new Button { Text = "Übernehmen", AutoSize = true };
    var copyButton = new Button { Text = "Kopieren", AutoSize = true };
    var deleteButton = new Button { Text = "Löschen", AutoSize = true };

    newButton.Click += (_, _) => ClearCoolingBankInputs();
    applyButton.Click += (_, _) => AddOrUpdateCoolingBank();
    copyButton.Click += (_, _) => CopySelectedCoolingBank();
    deleteButton.Click += (_, _) => DeleteCoolingBank();

    buttons.Controls.AddRange(new Control[] { newButton, applyButton, copyButton, deleteButton });

    panel.Controls.Add(buttons, 0, 5);
    panel.SetColumnSpan(buttons, 2);

    panel.Controls.Add(cbOutput, 0, 6);
    panel.SetColumnSpan(cbOutput, 2);

    UpdateZoneInputState();
    return panel;
  }

  private Control BuildCoolingBankRight()
  {
    var panel = new TableLayoutPanel
    {
      Dock = DockStyle.Fill,
      ColumnCount = 1,
      RowCount = 3,
      Padding = new Padding(8)
    };
    panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
    panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
    panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));

    var title = new Label
    {
      Text = "CoolingBanks (ID / typeBank / Anzahl / Zonen)",
      Dock = DockStyle.Fill,
      TextAlign = ContentAlignment.MiddleLeft,
      Font = new Font(Font, FontStyle.Bold)
    };

    cbList.SelectedIndexChanged += OnCoolingBankSelected;
    var footer = new Label { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
    cbList.Tag = footer;

    panel.Controls.Add(title, 0, 0);
    panel.Controls.Add(cbList, 0, 1);
    panel.Controls.Add(footer, 0, 2);
    return panel;
  }

  // ---------------- RollParam UI ----------------
  private Control BuildRollParamLeft()
  {
    var panel = new TableLayoutPanel
    {
      Dock = DockStyle.Fill,
      ColumnCount = 2,
      RowCount = 7,
      Padding = new Padding(12)
    };

    panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 170));
    panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

    for (int i = 0; i < 4; i++) panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
    panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
    panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
    panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

    rpTypeRollInput.Items.AddRange(new object[] { "WR", "IR", "BR" });
    rpTypeRollInput.SelectedIndex = 0;

    AddInput(panel, 0, "ParamName", rpIdInput, "workRollParam");

    panel.Controls.Add(new Label { Text = "typeRoll", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 1);
    panel.Controls.Add(rpTypeRollInput, 1, 1);

    panel.Controls.Add(new Label { Text = "noBank", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 2);
    panel.Controls.Add(rpBankCountInput, 1, 2);

    var banks = new TableLayoutPanel
    {
      Dock = DockStyle.Top,
      AutoSize = true,
      ColumnCount = 6,
      RowCount = 9,
      Margin = new Padding(0, 8, 0, 8)
    };

    banks.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 70));
    banks.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30));
    banks.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 15));
    banks.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 18));
    banks.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 18));
    banks.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 19));

    banks.Controls.Add(new Label { Text = "bank_x", AutoSize = true }, 0, 0);
    banks.Controls.Add(new Label { Text = "Typ (CoolingBank)", AutoSize = true }, 1, 0);
    banks.Controls.Add(new Label { Text = "Index", AutoSize = true }, 2, 0);
    banks.Controls.Add(new Label { Text = "XPos [m]", AutoSize = true }, 3, 0);
    banks.Controls.Add(new Label { Text = "YPos [m]", AutoSize = true }, 4, 0);
    banks.Controls.Add(new Label { Text = "AngleSlope [°]", AutoSize = true }, 5, 0);

    for (int i = 0; i < 8; i++)
    {
      int row = i + 1;

      rpBankTypeInputs[i].Dock = DockStyle.Fill;
      rpBankIndexInputs[i].Dock = DockStyle.Fill;
      rpBankXPosInputs[i].Dock = DockStyle.Fill;
      rpBankYPosInputs[i].Dock = DockStyle.Fill;
      rpBankAngleInputs[i].Dock = DockStyle.Fill;

      banks.Controls.Add(new Label { Text = $"bank_{i}", AutoSize = true, Anchor = AnchorStyles.Left }, 0, row);
      banks.Controls.Add(rpBankTypeInputs[i], 1, row);
      banks.Controls.Add(rpBankIndexInputs[i], 2, row);
      banks.Controls.Add(rpBankXPosInputs[i], 3, row);
      banks.Controls.Add(rpBankYPosInputs[i], 4, row);
      banks.Controls.Add(rpBankAngleInputs[i], 5, row);
    }

    panel.Controls.Add(banks, 0, 4);
    panel.SetColumnSpan(banks, 2);

    var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, WrapContents = true };
    var bNew = new Button { Text = "Neu", AutoSize = true };
    var bApply = new Button { Text = "Übernehmen", AutoSize = true };
    var bCopy = new Button { Text = "Kopieren", AutoSize = true };
    var bDelete = new Button { Text = "Löschen", AutoSize = true };

    bNew.Click += (_, _) => ClearRollParamInputs();
    bApply.Click += (_, _) => AddOrUpdateRollParam();
    bCopy.Click += (_, _) => CopySelectedRollParam();
    bDelete.Click += (_, _) => DeleteRollParam();

    buttons.Controls.AddRange(new Control[] { bNew, bApply, bCopy, bDelete });

    panel.Controls.Add(buttons, 0, 5);
    panel.SetColumnSpan(buttons, 2);

    rpOutput.MinimumSize = new Size(0, 320);
    panel.Controls.Add(rpOutput, 0, 6);
    panel.SetColumnSpan(rpOutput, 2);

    UpdateRollParamInputState();
    return panel;
  }

  private Control BuildRollParamRight()
  {
    var panel = new TableLayoutPanel
    {
      Dock = DockStyle.Fill,
      ColumnCount = 1,
      RowCount = 3,
      Padding = new Padding(8)
    };

    panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
    panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
    panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));

    var title = new Label
    {
      Text = "RollParams (ParamName / typeRoll / noBank)",
      Dock = DockStyle.Fill,
      TextAlign = ContentAlignment.MiddleLeft,
      Font = new Font(Font, FontStyle.Bold)
    };

    rpList.SelectedIndexChanged += OnRollParamSelected;
    var footer = new Label { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
    rpList.Tag = footer;

    panel.Controls.Add(title, 0, 0);
    panel.Controls.Add(rpList, 0, 1);
    panel.Controls.Add(footer, 0, 2);

    return panel;
  }

  private static void AddInput(TableLayoutPanel layout, int row, string label, TextBox input, string initial)
  {
    var lbl = new Label { Text = label, AutoSize = true, Anchor = AnchorStyles.Left };
    input.Text = initial;
    input.Dock = DockStyle.Fill;
    layout.Controls.Add(lbl, 0, row);
    layout.Controls.Add(input, 1, row);
  }

  private void UpdateZoneInputState()
  {
    int count = (int)cbZoneCountInput.Value;

    for (int i = 0; i < 3; i++)
    {
      bool active = i < count;
      zoneCountInputs[i].Enabled = active;
      zoneDistanceInputs[i].Enabled = active;
      zoneNozzleInputs[i].Enabled = active;
    }

    for (int i = 0; i < 2; i++)
      zoneGapInputs[i].Enabled = i < count - 1;
  }

  private void UpdateRollParamInputState()
  {
    int count = (int)rpBankCountInput.Value;

    for (int i = 0; i < 8; i++)
    {
      bool active = i < count;
      rpBankTypeInputs[i].Enabled = active;
      rpBankIndexInputs[i].Enabled = active;
      rpBankXPosInputs[i].Enabled = active;
      rpBankYPosInputs[i].Enabled = active;
      rpBankAngleInputs[i].Enabled = active;
    }
  }

  private void SetSplitsHalf()
  {
    if (nozzleSplit.Width > 2) nozzleSplit.SplitterDistance = nozzleSplit.Width / 2;
    if (cbSplit.Width > 2) cbSplit.SplitterDistance = cbSplit.Width / 2;
    if (rpSplit.Width > 2) rpSplit.SplitterDistance = rpSplit.Width / 2;
  }

  // =========================================================
  // Auto preview hooks
  // =========================================================
  private void HookAutoPreviewEvents()
  {
    nozzleIdInput.TextChanged += (_, _) => AutoPreviewNozzle();
    nozzleBrandInput.TextChanged += (_, _) => AutoPreviewNozzle();
    nozzleTypBezInput.TextChanged += (_, _) => AutoPreviewNozzle();
    nozzleTypeInput.TextChanged += (_, _) => AutoPreviewNozzle();
    nozzleMaxFlowLMinInput.TextChanged += (_, _) => AutoPreviewNozzle();
    nozzleRefPressureInput.TextChanged += (_, _) => AutoPreviewNozzle();
    nozzleAngleDegInput.TextChanged += (_, _) => AutoPreviewNozzle();

    cbIdInput.TextChanged += (_, _) => AutoPreviewCoolingBank();
    cbTypeBankInput.TextChanged += (_, _) => AutoPreviewCoolingBank();

    cbZoneCountInput.ValueChanged += (_, _) =>
    {
      UpdateZoneInputState();
      AutoPreviewCoolingBank();
    };

    for (int i = 0; i < 3; i++)
    {
      zoneCountInputs[i].TextChanged += (_, _) => AutoPreviewCoolingBank();
      zoneDistanceInputs[i].TextChanged += (_, _) => AutoPreviewCoolingBank();
      zoneNozzleInputs[i].SelectedIndexChanged += (_, _) => AutoPreviewCoolingBank();
    }

    foreach (var gapInput in zoneGapInputs)
      gapInput.TextChanged += (_, _) => AutoPreviewCoolingBank();

    rpIdInput.TextChanged += (_, _) => AutoPreviewRollParam();
    rpTypeRollInput.SelectedIndexChanged += (_, _) => AutoPreviewRollParam();

    rpBankCountInput.ValueChanged += (_, _) =>
    {
      UpdateRollParamInputState();
      AutoPreviewRollParam();
    };

    for (int i = 0; i < 8; i++)
    {
      rpBankTypeInputs[i].SelectedIndexChanged += (_, _) => AutoPreviewRollParam();
      rpBankIndexInputs[i].TextChanged += (_, _) => AutoPreviewRollParam();
      rpBankXPosInputs[i].TextChanged += (_, _) => AutoPreviewRollParam();
      rpBankYPosInputs[i].TextChanged += (_, _) => AutoPreviewRollParam();
      rpBankAngleInputs[i].TextChanged += (_, _) => AutoPreviewRollParam();
    }
  }

  private void AutoPreviewNozzle()
  {
    if (TryCreateNozzleFromInputs(false, out var n))
      nozzleOutput.Text = BuildNozzleEnvBlock(n);
    else
      nozzleOutput.Text = "";
  }

  private void AutoPreviewCoolingBank()
  {
    long total = 0;
    bool validCounts = true;

    for (int i = 0; i < (int)cbZoneCountInput.Value; i++)
    {
      if (int.TryParse(zoneCountInputs[i].Text.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int count) && count > 0)
        total += count;
      else
        validCounts = false;
    }

    cbTotalInput.Text = validCounts ? total.ToString(CultureInfo.InvariantCulture) : "";

    if (!TryCreateCoolingBankFromInputs(false, out var bank))
    {
      cbOutput.Clear();
      return;
    }

    try { cbOutput.Text = BuildCoolingBankBlock(bank); }
    catch { cbOutput.Clear(); }
  }

  private void AutoPreviewRollParam()
  {
    if (!TryCreateRollParamFromInputs(false, out var rp))
    {
      rpOutput.Clear();
      return;
    }

    try { rpOutput.Text = BuildRollParamBlock(rp); }
    catch { rpOutput.Clear(); }
  }

  // =========================================================
  // Nozzels logic
  // =========================================================
  private void AddOrUpdateNozzle()
  {
    if (!TryCreateNozzleFromInputs(true, out var data))
      return;

    int duplicateIndex = nozzles.FindIndex(n => string.Equals(n.Id, data.Id, StringComparison.OrdinalIgnoreCase));
    bool duplicateOnOther = duplicateIndex >= 0 && duplicateIndex != selectedNozzleIndex;

    if (duplicateOnOther)
    {
      MessageBox.Show(this, $"Die Nozzle-ID '{data.Id}' existiert bereits.", "Duplikat-ID",
          MessageBoxButtons.OK, MessageBoxIcon.Warning);
      nozzleIdInput.Focus();
      nozzleIdInput.SelectAll();
      return;
    }

    if (selectedNozzleIndex >= 0 && selectedNozzleIndex < nozzles.Count)
      nozzles[selectedNozzleIndex] = data;
    else
    {
      nozzles.Add(data);
      selectedNozzleIndex = nozzles.Count - 1;
    }

    RefreshNozzleList();
    internalNozzleSelectionChange = true;
    nozzleList.SelectedIndex = selectedNozzleIndex;
    internalNozzleSelectionChange = false;

    nozzleOutput.Text = BuildNozzleEnvBlock(data);
    MarkDirty();
  }

  private void CopySelectedNozzle()
  {
    int index = nozzleList.SelectedIndex;
    if (index < 0 || index >= nozzles.Count)
    {
      MessageBox.Show(this, "Bitte zuerst eine Nozzle in der Liste auswählen.", "Nozzle kopieren",
          MessageBoxButtons.OK, MessageBoxIcon.Information);
      return;
    }

    var source = nozzles[index];

    string? newId = AskForCopyId("Nozzle kopieren", source.Id,
        id => nozzles.Exists(n => string.Equals(n.Id, id, StringComparison.OrdinalIgnoreCase)));

    if (newId == null) return;

    var copy = new NozzleData
    {
      Id = newId,
      Brand = source.Brand,
      NozzelTypbezeichnung = source.NozzelTypbezeichnung,
      TypeNozzel = source.TypeNozzel,
      MaxVolFlowLMin = source.MaxVolFlowLMin,
      RefPressure = source.RefPressure,
      AngleSprayDeg = source.AngleSprayDeg
    };

    nozzles.Add(copy);
    selectedNozzleIndex = nozzles.Count - 1;

    internalNozzleSelectionChange = true;
    try
    {
      RefreshNozzleList();
      nozzleList.SelectedIndex = selectedNozzleIndex;
    }
    finally { internalNozzleSelectionChange = false; }

    LoadNozzleIntoInputs(selectedNozzleIndex);
    AutoPreviewNozzle();
    MarkDirty();
  }

  private void DeleteNozzle()
  {
    if (selectedNozzleIndex < 0 || selectedNozzleIndex >= nozzles.Count) return;

    string id = nozzles[selectedNozzleIndex].Id;

    foreach (var bank in coolingBanks)
    {
      foreach (var z in bank.Zones)
      {
        if (string.Equals(z.NozzleId, id, StringComparison.OrdinalIgnoreCase))
        {
          MessageBox.Show(this,
              $"Nozzle '{id}' wird noch in CoolingBank '{bank.Id}' verwendet.",
              "Löschen nicht möglich", MessageBoxButtons.OK, MessageBoxIcon.Warning);
          return;
        }
      }
    }

    nozzles.RemoveAt(selectedNozzleIndex);
    RefreshNozzleList();

    if (nozzles.Count == 0)
    {
      ClearNozzleInputs();
      nozzleOutput.Clear();
      MarkDirty();
      return;
    }

    selectedNozzleIndex = Math.Max(0, selectedNozzleIndex - 1);
    internalNozzleSelectionChange = true;
    nozzleList.SelectedIndex = selectedNozzleIndex;
    internalNozzleSelectionChange = false;
    LoadNozzleIntoInputs(selectedNozzleIndex);

    AutoPreviewNozzle();
    MarkDirty();
  }

  private void OnNozzleSelected(object? sender, EventArgs e)
  {
    if (internalNozzleSelectionChange) return;

    int idx = nozzleList.SelectedIndex;
    if (idx < 0 || idx >= nozzles.Count) return;

    selectedNozzleIndex = idx;
    LoadNozzleIntoInputs(idx);
    AutoPreviewNozzle();
  }

  private void LoadNozzleIntoInputs(int idx)
  {
    var n = nozzles[idx];
    nozzleIdInput.Text = n.Id;
    nozzleBrandInput.Text = n.Brand;
    nozzleTypBezInput.Text = n.NozzelTypbezeichnung;
    nozzleTypeInput.Text = n.TypeNozzel;
    nozzleMaxFlowLMinInput.Text = n.MaxVolFlowLMin.ToString("0.##", CultureInfo.InvariantCulture);
    nozzleRefPressureInput.Text = n.RefPressure.ToString("0.0", CultureInfo.InvariantCulture);
    nozzleAngleDegInput.Text = n.AngleSprayDeg.ToString("0.###", CultureInfo.InvariantCulture);
  }

  private void ClearNozzleInputs()
  {
    selectedNozzleIndex = -1;
    nozzleIdInput.Text = "";
    nozzleBrandInput.Text = "";
    nozzleTypBezInput.Text = "";
    nozzleTypeInput.Text = "";
    nozzleMaxFlowLMinInput.Text = "";
    nozzleRefPressureInput.Text = "";
    nozzleAngleDegInput.Text = "";
    nozzleOutput.Text = "";
  }

  private bool TryCreateNozzleFromInputs(bool showValidation, out NozzleData data)
  {
    data = new NozzleData();

    if (string.IsNullOrWhiteSpace(nozzleIdInput.Text) ||
        string.IsNullOrWhiteSpace(nozzleBrandInput.Text) ||
        string.IsNullOrWhiteSpace(nozzleTypBezInput.Text) ||
        string.IsNullOrWhiteSpace(nozzleTypeInput.Text))
      return false;

    if (!TryReadDouble(nozzleMaxFlowLMinInput, "maxVolFlow [l/min]", showValidation, out double flow) ||
        !TryReadDouble(nozzleRefPressureInput, "refPressure", showValidation, out double p) ||
        !TryReadDouble(nozzleAngleDegInput, "angleSpray [deg]", showValidation, out double a))
      return false;

    data.Id = nozzleIdInput.Text.Trim();
    data.Brand = nozzleBrandInput.Text.Trim();
    data.NozzelTypbezeichnung = nozzleTypBezInput.Text.Trim();
    data.TypeNozzel = nozzleTypeInput.Text.Trim();
    data.MaxVolFlowLMin = flow;
    data.RefPressure = p;
    data.AngleSprayDeg = a;
    return true;
  }

  private string BuildNozzleEnvBlock(NozzleData n)
  {
    double maxFlowM3s = n.MaxVolFlowLMin / 1000.0 / 60.0;
    double angleRad = n.AngleSprayDeg * Math.PI / 180.0;
    var ci = CultureInfo.InvariantCulture;

    string kommentar = $"{n.Brand} nozzel typ {n.NozzelTypbezeichnung} ({n.AngleSprayDeg.ToString("0.###", ci)}°) {n.MaxVolFlowLMin.ToString("0.##", ci)} l/min";

    return string.Join(Environment.NewLine, new[]
    {
            n.Id,
            $"env{{                          // {kommentar}  ",
            $"    typeNozzel   {n.TypeNozzel}    ",
            "    ",
            $"    minVolFlow        {"0.0".PadRight(8)}// [m3/s]  nozzel min volume flow",
            $"    maxVolFlow        {ToScientificNoLeadingZero(maxFlowM3s, ci).PadRight(8)}// [m3/s]  nozzel max volume flow  ",
            $"    refPressure       {n.RefPressure.ToString("0.0", ci).PadRight(8)}// [N/m*m] referenze pressure  on max flow",
            $"    angleSpray        {angleRad.ToString("0.000", ci).PadRight(8)}// [ rad ] angle of the spray ",
            "    ",
            $"}} // end of param {n.Id}"
        });
  }

  private void RefreshNozzleList()
  {
    nozzleList.BeginUpdate();
    nozzleList.Items.Clear();

    foreach (var n in nozzles)
      nozzleList.Items.Add($"{n.Id} | {n.Brand} nozzel typ {n.NozzelTypbezeichnung} ({n.AngleSprayDeg:0.###}°) {n.MaxVolFlowLMin:0.##} l/min");

    nozzleList.EndUpdate();

    if (nozzleList.Tag is Label f) f.Text = $"Anzahl: {nozzles.Count}";

    RefreshCoolingBankNozzleDropdown();
  }

  // =========================================================
  // CoolingBank logic
  // =========================================================
  private void RefreshCoolingBankNozzleDropdown()
  {
    foreach (var combo in zoneNozzleInputs)
    {
      string? selected = combo.SelectedItem as string;

      combo.BeginUpdate();
      combo.Items.Clear();
      foreach (var n in nozzles) combo.Items.Add(n.Id);
      combo.EndUpdate();

      SelectComboValue(combo, selected);
    }
  }

  private void AddOrUpdateCoolingBank()
  {
    if (!TryCreateCoolingBankFromInputs(true, out var data))
      return;

    int duplicateIndex = coolingBanks.FindIndex(x => string.Equals(x.Id, data.Id, StringComparison.OrdinalIgnoreCase));
    bool duplicateOnOther = duplicateIndex >= 0 && duplicateIndex != selectedCbIndex;

    if (duplicateOnOther)
    {
      MessageBox.Show(this, $"Die CoolingBank-ID '{data.Id}' existiert bereits.", "Duplikat-ID",
          MessageBoxButtons.OK, MessageBoxIcon.Warning);
      cbIdInput.Focus();
      cbIdInput.SelectAll();
      return;
    }

    if (selectedCbIndex >= 0 && selectedCbIndex < coolingBanks.Count)
      coolingBanks[selectedCbIndex] = data;
    else
    {
      coolingBanks.Add(data);
      selectedCbIndex = coolingBanks.Count - 1;
    }

    RefreshCoolingBankList();
    internalCbSelectionChange = true;
    cbList.SelectedIndex = selectedCbIndex;
    internalCbSelectionChange = false;

    cbOutput.Text = BuildCoolingBankBlock(data);

    RefreshRollParamBankDropdowns();
    MarkDirty();
  }

  private void CopySelectedCoolingBank()
  {
    int index = cbList.SelectedIndex;
    if (index < 0 || index >= coolingBanks.Count)
    {
      MessageBox.Show(this, "Bitte zuerst eine CoolingBank auswählen.", "CoolingBank kopieren",
          MessageBoxButtons.OK, MessageBoxIcon.Information);
      return;
    }

    var source = coolingBanks[index];

    string? newId = AskForCopyId("CoolingBank kopieren", source.Id,
        id => coolingBanks.Exists(b => string.Equals(b.Id, id, StringComparison.OrdinalIgnoreCase)));

    if (newId == null) return;

    var copy = new CoolingBankData
    {
      Id = newId,
      TypeBank = source.TypeBank,
      Gaps = new List<double>(source.Gaps)
    };

    foreach (var z in source.Zones)
    {
      copy.Zones.Add(new CoolingBankZoneData
      {
        Count = z.Count,
        Distance = z.Distance,
        NozzleId = z.NozzleId
      });
    }

    coolingBanks.Add(copy);
    selectedCbIndex = coolingBanks.Count - 1;

    internalCbSelectionChange = true;
    try
    {
      RefreshCoolingBankList();
      cbList.SelectedIndex = selectedCbIndex;
    }
    finally { internalCbSelectionChange = false; }

    LoadCoolingBankIntoInputs(selectedCbIndex);
    RefreshRollParamBankDropdowns();
    MarkDirty();
  }

  private void DeleteCoolingBank()
  {
    if (selectedCbIndex < 0 || selectedCbIndex >= coolingBanks.Count) return;

    string bankId = coolingBanks[selectedCbIndex].Id;

    foreach (var rp in rollParams)
    {
      foreach (var b in rp.Banks)
      {
        if (string.Equals(b.BankType, bankId, StringComparison.OrdinalIgnoreCase))
        {
          MessageBox.Show(this,
              $"CoolingBank '{bankId}' wird noch in RollParam '{rp.ParamName}' verwendet.",
              "Löschen nicht möglich", MessageBoxButtons.OK, MessageBoxIcon.Warning);
          return;
        }
      }
    }

    coolingBanks.RemoveAt(selectedCbIndex);
    RefreshCoolingBankList();

    if (coolingBanks.Count == 0)
    {
      ClearCoolingBankInputs();
      cbOutput.Clear();
      RefreshRollParamBankDropdowns();
      MarkDirty();
      return;
    }

    selectedCbIndex = Math.Max(0, selectedCbIndex - 1);
    internalCbSelectionChange = true;
    cbList.SelectedIndex = selectedCbIndex;
    internalCbSelectionChange = false;
    LoadCoolingBankIntoInputs(selectedCbIndex);

    AutoPreviewCoolingBank();
    RefreshRollParamBankDropdowns();
    MarkDirty();
  }

  private void OnCoolingBankSelected(object? sender, EventArgs e)
  {
    if (internalCbSelectionChange) return;

    int idx = cbList.SelectedIndex;
    if (idx < 0 || idx >= coolingBanks.Count) return;

    selectedCbIndex = idx;
    LoadCoolingBankIntoInputs(idx);
    AutoPreviewCoolingBank();
  }

  private void LoadCoolingBankIntoInputs(int idx)
  {
    var bank = coolingBanks[idx];

    cbIdInput.Text = bank.Id;
    cbTypeBankInput.Text = bank.TypeBank;
    cbZoneCountInput.Value = Math.Clamp(bank.Zones.Count, 1, 3);

    for (int i = 0; i < 3; i++)
    {
      if (i < bank.Zones.Count)
      {
        zoneCountInputs[i].Text = bank.Zones[i].Count.ToString(CultureInfo.InvariantCulture);
        zoneDistanceInputs[i].Text = bank.Zones[i].Distance.ToString("G", CultureInfo.InvariantCulture);
        SelectComboValue(zoneNozzleInputs[i], bank.Zones[i].NozzleId);
      }
      else
      {
        zoneCountInputs[i].Clear();
        zoneDistanceInputs[i].Clear();
        zoneNozzleInputs[i].SelectedIndex = -1;
      }
    }

    for (int i = 0; i < 2; i++)
      zoneGapInputs[i].Text = i < bank.Gaps.Count ? bank.Gaps[i].ToString("G", CultureInfo.InvariantCulture) : "";

    UpdateZoneInputState();
  }

  private void ClearCoolingBankInputs()
  {
    selectedCbIndex = -1;
    cbList.ClearSelected();

    cbIdInput.Clear();
    cbTypeBankInput.Clear();
    cbZoneCountInput.Value = 1;
    cbTotalInput.Clear();

    for (int i = 0; i < 3; i++)
    {
      zoneCountInputs[i].Clear();
      zoneDistanceInputs[i].Clear();
      zoneNozzleInputs[i].SelectedIndex = -1;
    }

    foreach (var g in zoneGapInputs) g.Clear();

    cbOutput.Clear();
    UpdateZoneInputState();
  }

  private bool TryCreateCoolingBankFromInputs(bool showValidation, out CoolingBankData data)
  {
    data = new CoolingBankData();

    bool Fail(string message)
    {
      if (showValidation)
        MessageBox.Show(this, message, "CoolingBank", MessageBoxButtons.OK, MessageBoxIcon.Warning);
      return false;
    }

    if (string.IsNullOrWhiteSpace(cbIdInput.Text) || string.IsNullOrWhiteSpace(cbTypeBankInput.Text))
      return Fail("Bitte ID und typeBank eingeben.");

    data.Id = cbIdInput.Text.Trim();
    data.TypeBank = cbTypeBankInput.Text.Trim();

    int zoneCount = (int)cbZoneCountInput.Value;
    long total = 0;

    for (int i = 0; i < zoneCount; i++)
    {
      if (!TryReadInt(zoneCountInputs[i], $"Zone {i + 1}: Anzahl", showValidation, out int count) ||
          !TryReadDouble(zoneDistanceInputs[i], $"Zone {i + 1}: Abstand", showValidation, out double distance))
        return false;

      if (count <= 0 || distance < 0)
        return Fail($"Zone {i + 1}: Anzahl muss > 0 und Abstand >= 0 sein.");

      if (zoneNozzleInputs[i].SelectedItem is not string nozzleId)
        return Fail($"Bitte für Zone {i + 1} eine Nozzle auswählen.");

      total += count;
      if (total > 10000)
        return Fail("Maximal 10000 Düsen pro Bank.");

      data.Zones.Add(new CoolingBankZoneData
      {
        Count = count,
        Distance = distance,
        NozzleId = nozzleId
      });
    }

    for (int i = 0; i < zoneCount - 1; i++)
    {
      if (!TryReadDouble(zoneGapInputs[i], $"Übergang {i + 1} → {i + 2}", showValidation, out double gap))
        return false;

      if (gap < 0)
        return Fail("Zonenübergänge dürfen nicht negativ sein.");

      data.Gaps.Add(gap);
    }

    if (!double.IsFinite(GetBankLength(data)))
      return Fail("Ungültige Banklänge.");

    return true;
  }

  private static double GetBankLength(CoolingBankData bank)
  {
    double length = 0;
    foreach (var z in bank.Zones) length += (z.Count - 1.0) * z.Distance;
    foreach (var g in bank.Gaps) length += g;
    return length;
  }

  private string BuildCoolingBankBlock(CoolingBankData bank)
  {
    if (bank.Zones.Count < 1 || bank.Zones.Count > 3 || bank.Gaps.Count != bank.Zones.Count - 1)
      throw new InvalidOperationException($"Bank '{bank.Id}' enthält ungültige Zonendaten.");

    var ci = CultureInfo.InvariantCulture;
    var zoneComments = new List<string>();

    foreach (var z in bank.Zones)
    {
      var nozzle = nozzles.Find(n => string.Equals(n.Id, z.NozzleId, StringComparison.OrdinalIgnoreCase));
      if (nozzle == null) throw new InvalidOperationException($"Nozzle '{z.NozzleId}' fehlt.");
      zoneComments.Add($"{z.Count.ToString(ci)} nozzles a {nozzle.MaxVolFlowLMin.ToString("0.##", ci)} l/min");
    }

    double length = GetBankLength(bank);

    var lines = new List<string>
        {
            bank.Id,
            "env{",
            $"    typeBank     {bank.TypeBank}  // {string.Join("; ", zoneComments)} bankLength   {length.ToString("0.###", ci)} m",
            $"    noNozzel     {bank.NoNozzel.ToString(ci)}",
            "  ",
            "    ",
            "    switchWidth  switch_A     // steps for switching",
            "    "
        };

    int nozzleIndex = 0;
    for (int z = 0; z < bank.Zones.Count; z++)
    {
      var zone = bank.Zones[z];

      for (int local = 0; local < zone.Count; local++)
      {
        double dis = z > 0 && local == 0 ? bank.Gaps[z - 1] : zone.Distance;
        string suffix = nozzleIndex == 0 ? "        // [m] from left to right" : "";

        lines.Add($"    nozzel_{nozzleIndex}_dis {dis.ToString("0.#########", ci)}{suffix}");
        lines.Add($"    {($"nozzel_{nozzleIndex}").PadRight(12)}{zone.NozzleId}");

        nozzleIndex++;
      }
    }

    double finalDis = bank.Zones[^1].Distance;
    lines.Add($"    nozzel_{nozzleIndex}_dis {finalDis.ToString("0.#########", ci)}");
    lines.Add($"}}  // end of param {bank.Id}");

    return string.Join(Environment.NewLine, lines);
  }

  private void RefreshCoolingBankList()
  {
    bool prev = internalCbSelectionChange;
    internalCbSelectionChange = true;

    cbList.BeginUpdate();
    cbList.Items.Clear();
    foreach (var b in coolingBanks)
      cbList.Items.Add($"{b.Id} | {b.TypeBank} | {b.NoNozzel} Nozzles | {b.Zones.Count} Zonen");
    cbList.EndUpdate();

    internalCbSelectionChange = prev;

    if (cbList.Tag is Label f) f.Text = $"Anzahl: {coolingBanks.Count}";
  }

  // =========================================================
  // RollParam logic
  // =========================================================
  private void RefreshRollParamBankDropdowns()
  {
    foreach (var combo in rpBankTypeInputs)
    {
      string? selected = combo.SelectedItem as string;

      combo.BeginUpdate();
      combo.Items.Clear();
      foreach (var b in coolingBanks) combo.Items.Add(b.Id);
      combo.EndUpdate();

      SelectComboValue(combo, selected);
    }
  }

  private void AddOrUpdateRollParam()
  {
    if (!TryCreateRollParamFromInputs(true, out var data))
      return;

    int duplicateIndex = rollParams.FindIndex(r => string.Equals(r.ParamName, data.ParamName, StringComparison.OrdinalIgnoreCase));
    bool duplicateOnOther = duplicateIndex >= 0 && duplicateIndex != selectedRpIndex;

    if (duplicateOnOther)
    {
      MessageBox.Show(this, $"Der ParamName '{data.ParamName}' existiert bereits.", "Duplikat-ID",
          MessageBoxButtons.OK, MessageBoxIcon.Warning);
      rpIdInput.Focus();
      rpIdInput.SelectAll();
      return;
    }

    if (selectedRpIndex >= 0 && selectedRpIndex < rollParams.Count)
      rollParams[selectedRpIndex] = data;
    else
    {
      rollParams.Add(data);
      selectedRpIndex = rollParams.Count - 1;
    }

    RefreshRollParamList();
    internalRpSelectionChange = true;
    rpList.SelectedIndex = selectedRpIndex;
    internalRpSelectionChange = false;

    rpOutput.Text = BuildRollParamBlock(data);
    MarkDirty();
  }

  private void CopySelectedRollParam()
  {
    int idx = rpList.SelectedIndex;
    if (idx < 0 || idx >= rollParams.Count)
    {
      MessageBox.Show(this, "Bitte zuerst ein RollParam auswählen.", "RollParam kopieren",
          MessageBoxButtons.OK, MessageBoxIcon.Information);
      return;
    }

    var source = rollParams[idx];

    string? newId = AskForCopyId("RollParam kopieren", source.ParamName,
        id => rollParams.Exists(r => string.Equals(r.ParamName, id, StringComparison.OrdinalIgnoreCase)));

    if (newId == null) return;

    var copy = new RollParamData
    {
      ParamName = newId,
      TypeRoll = source.TypeRoll
    };

    foreach (var b in source.Banks)
    {
      copy.Banks.Add(new RollParamBankData
      {
        BankType = b.BankType,
        Index = b.Index,
        XPos = b.XPos,
        YPos = b.YPos,
        AngleSlope = b.AngleSlope
      });
    }

    rollParams.Add(copy);
    selectedRpIndex = rollParams.Count - 1;

    internalRpSelectionChange = true;
    try
    {
      RefreshRollParamList();
      rpList.SelectedIndex = selectedRpIndex;
    }
    finally { internalRpSelectionChange = false; }

    LoadRollParamIntoInputs(selectedRpIndex);
    MarkDirty();
  }

  private void DeleteRollParam()
  {
    if (selectedRpIndex < 0 || selectedRpIndex >= rollParams.Count) return;

    rollParams.RemoveAt(selectedRpIndex);
    RefreshRollParamList();

    if (rollParams.Count == 0)
    {
      ClearRollParamInputs();
      rpOutput.Clear();
      MarkDirty();
      return;
    }

    selectedRpIndex = Math.Max(0, selectedRpIndex - 1);
    internalRpSelectionChange = true;
    rpList.SelectedIndex = selectedRpIndex;
    internalRpSelectionChange = false;

    LoadRollParamIntoInputs(selectedRpIndex);
    AutoPreviewRollParam();
    MarkDirty();
  }

  private void OnRollParamSelected(object? sender, EventArgs e)
  {
    if (internalRpSelectionChange) return;

    int idx = rpList.SelectedIndex;
    if (idx < 0 || idx >= rollParams.Count) return;

    selectedRpIndex = idx;
    LoadRollParamIntoInputs(idx);
    AutoPreviewRollParam();
  }

  private void LoadRollParamIntoInputs(int idx)
  {
    var rp = rollParams[idx];

    rpIdInput.Text = rp.ParamName;
    SelectComboValue(rpTypeRollInput, rp.TypeRoll);
    rpBankCountInput.Value = Math.Clamp(rp.Banks.Count, 1, 8);

    for (int i = 0; i < 8; i++)
    {
      if (i < rp.Banks.Count)
      {
        var b = rp.Banks[i];
        SelectComboValue(rpBankTypeInputs[i], b.BankType);
        rpBankIndexInputs[i].Text = b.Index.ToString(CultureInfo.InvariantCulture);
        rpBankXPosInputs[i].Text = b.XPos.ToString("G", CultureInfo.InvariantCulture);
        rpBankYPosInputs[i].Text = b.YPos.ToString("G", CultureInfo.InvariantCulture);
        rpBankAngleInputs[i].Text =
            (b.AngleSlope * (180.0 / Math.PI))
            .ToString("0.#########", CultureInfo.InvariantCulture);
      }
      else
      {
        rpBankTypeInputs[i].SelectedIndex = -1;
        rpBankIndexInputs[i].Clear();
        rpBankXPosInputs[i].Clear();
        rpBankYPosInputs[i].Clear();
        rpBankAngleInputs[i].Clear();
      }
    }

    UpdateRollParamInputState();
  }

  private void ClearRollParamInputs()
  {
    selectedRpIndex = -1;
    rpList.ClearSelected();

    rpIdInput.Text = "workRollParam";
    rpTypeRollInput.SelectedIndex = 0;
    rpBankCountInput.Value = 1;

    for (int i = 0; i < 8; i++)
    {
      rpBankTypeInputs[i].SelectedIndex = -1;
      rpBankIndexInputs[i].Clear();
      rpBankXPosInputs[i].Clear();
      rpBankYPosInputs[i].Clear();
      rpBankAngleInputs[i].Clear();
    }

    rpOutput.Clear();
    UpdateRollParamInputState();
  }

  private bool TryCreateRollParamFromInputs(bool showValidation, out RollParamData data)
  {
    data = new RollParamData();

    bool Fail(string message)
    {
      if (showValidation)
        MessageBox.Show(this, message, "RollParam", MessageBoxButtons.OK, MessageBoxIcon.Warning);
      return false;
    }

    if (string.IsNullOrWhiteSpace(rpIdInput.Text))
      return Fail("Bitte ParamName eingeben.");

    if (rpTypeRollInput.SelectedItem is not string typeRoll || string.IsNullOrWhiteSpace(typeRoll))
      return Fail("Bitte typeRoll auswählen.");

    data.ParamName = rpIdInput.Text.Trim();
    data.TypeRoll = typeRoll;

    int bankCount = (int)rpBankCountInput.Value;
    var usedIndices = new HashSet<int>();

    for (int i = 0; i < bankCount; i++)
    {
      if (rpBankTypeInputs[i].SelectedItem is not string bankType)
        return Fail($"Bitte bank_{i} (CoolingBank) auswählen.");

      if (!TryReadInt(rpBankIndexInputs[i], $"bank_{i}_Index", showValidation, out int index))
        return false;

      if (!usedIndices.Add(index))
        return Fail($"bank_{i}_Index = {index} ist doppelt.");

      if (!TryReadDouble(rpBankXPosInputs[i], $"bank_{i}_XPos", showValidation, out double xPos))
        return false;

      if (!TryReadDouble(rpBankYPosInputs[i], $"bank_{i}_YPos", showValidation, out double yPos))
        return false;

      if (!TryReadDouble(
              rpBankAngleInputs[i],
              $"bank_{i}_AngleSlope [°]",
              showValidation,
              out double angleDeg))
      {
        return false;
      }

      data.Banks.Add(new RollParamBankData
      {
        BankType = bankType,
        Index = index,
        XPos = xPos,
        YPos = yPos,
        AngleSlope = angleDeg * (Math.PI / 180.0)
      });
    }

    return true;
  }

  private string BuildRollParamBlock(RollParamData rp)
  {
    var ci = CultureInfo.InvariantCulture;

    string[] bankComments =
    {
            "WTET roll cool spray entry side  lubrication",
            "WTEB roll gap spray entry side  lubrication",
            "WTB spray entry side lower row S1"
        };

    int keyWidth = Math.Max("typeRoll".Length, "noBank".Length);
    int integerPartWidth = 1;

    foreach (var bank in rp.Banks)
    {
      double[] values =
      {
                bank.XPos,
                bank.YPos,
                bank.AngleSlope,
                0.80
            };

      foreach (double value in values)
      {
        string formatted = value.ToString("0.000", ci);
        int dotPosition = formatted.IndexOf('.');
        integerPartWidth = Math.Max(integerPartWidth, dotPosition);
      }

      integerPartWidth = Math.Max(integerPartWidth, bank.Index.ToString(ci).Length);
    }

    integerPartWidth = Math.Max(integerPartWidth, rp.Banks.Count.ToString(ci).Length);

    for (int i = 0; i < rp.Banks.Count; i++)
      keyWidth = Math.Max(keyWidth, $"bank_{i}_FacFlow2Strip".Length);

    int numberWidth = integerPartWidth + 4;

    string rollComment = rp.TypeRoll switch
    {
      "WR" => "work roll",
      "IR" => "intermediate roll",
      "BR" => "backup roll",
      _ => "roll"
    };

    var lines = new List<string>
        {
            "// ============================================================================",
            "// Roll Parameter for a Workroll on Stand X",
            "// ============================================================================",
            $"{rp.ParamName} env",
            "{"
        };

    void AddLine(string key, string value, string comment)
    {
      lines.Add($"  {key.PadRight(keyWidth)}  {value}  // {comment}");
    }

    void AddDecimal(string key, double value, string comment)
    {
      string formatted = value.ToString("0.000", ci).PadLeft(numberWidth);
      AddLine(key, formatted, comment);
    }

    void AddInteger(string key, int value, string comment)
    {
      string formatted = value.ToString(ci).PadLeft(integerPartWidth).PadRight(numberWidth);
      AddLine(key, formatted, comment);
    }

    AddLine("typeRoll", rp.TypeRoll.PadRight(numberWidth), rollComment);
    AddInteger("noBank", rp.Banks.Count, "number of banks");
    lines.Add("  ");

    for (int i = 0; i < rp.Banks.Count; i++)
    {
      var b = rp.Banks[i];
      string prefix = $"bank_{i}";
      string bankComment = i < bankComments.Length ? bankComments[i] : "cooling bank";

      AddLine(prefix, b.BankType.PadRight(numberWidth), bankComment);

      AddInteger(
          $"{prefix}_Index",
          b.Index,
          "      index of the bank counted in rolling direction around the stand");

      AddInteger(
          $"{prefix}_Switchable",
          1,
          "      switchable ( = 0 bank always ON )");

      AddDecimal(
          $"{prefix}_XPos",
          b.XPos,
          "[m]   x-position , 0 == middle of the stand  || - == befor stand || ++ behinde stand");

      AddDecimal(
          $"{prefix}_YPos",
          b.YPos,
          "[m]   y-position , 0 == passline");

      AddInteger(
          $"{prefix}_Distri",
          1,
          "      distribution of cooling flow < 0 off , 0 min , 1 max working area");

      AddDecimal(
          $"{prefix}_AngleSlope",
          b.AngleSlope,
          "[rad] angle of the slope of the nozzels to the rolls");

      AddDecimal(
          $"{prefix}_FacFlow2Strip",
          0.80,
          "");

      lines.Add("  ");
    }

    lines.Add("}// end of roll param");

    return string.Join(Environment.NewLine, lines);
  }

  private void RefreshRollParamList()
  {
    bool prev = internalRpSelectionChange;
    internalRpSelectionChange = true;

    rpList.BeginUpdate();
    rpList.Items.Clear();
    foreach (var rp in rollParams)
      rpList.Items.Add($"{rp.ParamName} | {rp.TypeRoll} | noBank={rp.Banks.Count}");
    rpList.EndUpdate();

    internalRpSelectionChange = prev;

    if (rpList.Tag is Label f) f.Text = $"Anzahl: {rollParams.Count}";
  }

  // =========================================================
  // Generic helpers
  // =========================================================
  private static void SelectComboValue(ComboBox combo, string? value)
  {
    combo.SelectedIndex = -1;
    if (value == null) return;

    for (int i = 0; i < combo.Items.Count; i++)
    {
      if (string.Equals(combo.Items[i]?.ToString(), value, StringComparison.OrdinalIgnoreCase))
      {
        combo.SelectedIndex = i;
        return;
      }
    }
  }

  private string? AskForCopyId(string title, string oldId, Func<string, bool> idExists)
  {
    using var dialog = new Form
    {
      Text = title,
      ClientSize = new Size(460, 150),
      FormBorderStyle = FormBorderStyle.FixedDialog,
      StartPosition = FormStartPosition.CenterParent,
      MaximizeBox = false,
      MinimizeBox = false,
      ShowInTaskbar = false
    };

    var label = new Label
    {
      Text = "Neue ID für die Kopie:",
      AutoSize = true,
      Location = new Point(16, 16)
    };

    var idInput = new TextBox
    {
      Text = oldId,
      Location = new Point(16, 44),
      Size = new Size(428, 27)
    };

    var okButton = new Button
    {
      Text = "Kopieren",
      Location = new Point(232, 100),
      Size = new Size(100, 30)
    };

    var cancelButton = new Button
    {
      Text = "Abbrechen",
      Location = new Point(344, 100),
      Size = new Size(100, 30),
      DialogResult = DialogResult.Cancel
    };

    okButton.Click += (_, _) =>
    {
      string candidate = idInput.Text.Trim();

      if (string.IsNullOrWhiteSpace(candidate))
      {
        MessageBox.Show(dialog, "Bitte eine neue ID eingeben.", "Fehlende ID",
            MessageBoxButtons.OK, MessageBoxIcon.Warning);
      }
      else if (idExists(candidate))
      {
        MessageBox.Show(dialog, $"Die ID '{candidate}' existiert bereits.", "Duplikat-ID",
            MessageBoxButtons.OK, MessageBoxIcon.Warning);
      }
      else
      {
        idInput.Text = candidate;
        dialog.DialogResult = DialogResult.OK;
        return;
      }

      idInput.Focus();
      idInput.SelectAll();
    };

    dialog.Controls.AddRange(new Control[] { label, idInput, okButton, cancelButton });
    dialog.AcceptButton = okButton;
    dialog.CancelButton = cancelButton;

    dialog.Shown += (_, _) =>
    {
      idInput.Focus();
      idInput.SelectAll();
    };

    return dialog.ShowDialog(this) == DialogResult.OK ? idInput.Text.Trim() : null;
  }

  // =========================================================
  // Load/Save Project file
  // =========================================================
  private void LoadStandardProjectIfExists()
  {
    try
    {
      if (File.Exists(currentProjectPath))
        LoadProject(currentProjectPath);
      else
      {
        ClearNozzleInputs();
        ClearCoolingBankInputs();
        ClearRollParamInputs();
        MarkClean();
      }
    }
    catch (Exception ex)
    {
      MessageBox.Show(this, $"Standard-Projekt konnte nicht geladen werden:\n{ex.Message}",
          "Ladefehler", MessageBoxButtons.OK, MessageBoxIcon.Warning);
      ClearNozzleInputs();
      ClearCoolingBankInputs();
      ClearRollParamInputs();
      MarkClean();
    }
  }

  private void LoadProjectByDialog()
  {
    if (!ConfirmDiscardUnsavedChanges()) return;

    using var ofd = new OpenFileDialog
    {
      Filter = "Textdatei (*.txt)|*.txt|Alle Dateien (*.*)|*.*",
      InitialDirectory = appFolder
    };

    if (ofd.ShowDialog(this) == DialogResult.OK)
      LoadProject(ofd.FileName);
  }

  private void LoadProject(string path)
  {
    var loaded = ParseProjectFile(path);

    nozzles.Clear();
    nozzles.AddRange(loaded.Nozzles);

    coolingBanks.Clear();
    coolingBanks.AddRange(loaded.CoolingBanks);

    rollParams.Clear();
    rollParams.AddRange(loaded.RollParams);

    currentProjectPath = path;

    RefreshNozzleList();
    RefreshCoolingBankList();
    RefreshRollParamBankDropdowns();
    RefreshRollParamList();

    if (nozzles.Count > 0)
    {
      internalNozzleSelectionChange = true;
      nozzleList.SelectedIndex = 0;
      internalNozzleSelectionChange = false;
      LoadNozzleIntoInputs(0);
    }
    else
    {
      ClearNozzleInputs();
    }

    if (coolingBanks.Count > 0)
    {
      internalCbSelectionChange = true;
      cbList.SelectedIndex = 0;
      internalCbSelectionChange = false;
      LoadCoolingBankIntoInputs(0);
    }
    else
    {
      ClearCoolingBankInputs();
    }

    if (rollParams.Count > 0)
    {
      internalRpSelectionChange = true;
      rpList.SelectedIndex = 0;
      internalRpSelectionChange = false;
      LoadRollParamIntoInputs(0);
    }
    else
    {
      ClearRollParamInputs();
    }

    AutoPreviewNozzle();
    AutoPreviewCoolingBank();
    AutoPreviewRollParam();

    MarkClean();
  }

  private void SaveProjectAs()
  {
    using var sfd = new SaveFileDialog
    {
      Filter = "Textdatei (*.txt)|*.txt|Alle Dateien (*.*)|*.*",
      InitialDirectory = appFolder,
      FileName = Path.GetFileName(currentProjectPath)
    };

    if (sfd.ShowDialog(this) == DialogResult.OK)
      SaveProject(sfd.FileName, true);
  }

  private void SaveProject(string path, bool showSuccessMessage)
  {
    var content = BuildProjectFileText(nozzles, coolingBanks, rollParams);
    File.WriteAllText(path, content);
    currentProjectPath = path;
    MarkClean();

    if (showSuccessMessage)
      MessageBox.Show(this, "Projekt gespeichert.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
  }

  // =========================================================
  // Export
  // =========================================================
  private void ExportNozzleEnvBlocks()
  {
    if (nozzles.Count == 0)
    {
      MessageBox.Show(this, "Keine Nozzles zum Exportieren vorhanden.", "Hinweis",
          MessageBoxButtons.OK, MessageBoxIcon.Information);
      return;
    }

    using var sfd = new SaveFileDialog
    {
      Filter = "Textdatei (*.txt)|*.txt|Alle Dateien (*.*)|*.*",
      InitialDirectory = appFolder,
      FileName = $"nozzles_env_export_{DateTime.Now:yyyyMMdd_HHmmss}.txt"
    };

    if (sfd.ShowDialog(this) != DialogResult.OK) return;

    var blocks = new List<string>();
    foreach (var n in nozzles) blocks.Add(BuildNozzleEnvBlock(n));
    File.WriteAllText(sfd.FileName, string.Join(Environment.NewLine + Environment.NewLine, blocks));

    MessageBox.Show(this, "Nozzels-Export abgeschlossen.", "Info",
        MessageBoxButtons.OK, MessageBoxIcon.Information);
  }

  private void ExportCoolingBanks()
  {
    if (coolingBanks.Count == 0)
    {
      MessageBox.Show(this, "Keine CoolingBanks zum Exportieren vorhanden.", "Hinweis",
          MessageBoxButtons.OK, MessageBoxIcon.Information);
      return;
    }

    using var sfd = new SaveFileDialog
    {
      Filter = "Textdatei (*.txt)|*.txt|Alle Dateien (*.*)|*.*",
      InitialDirectory = appFolder,
      FileName = $"coolingbank_export_{DateTime.Now:yyyyMMdd_HHmmss}.txt"
    };

    if (sfd.ShowDialog(this) != DialogResult.OK) return;

    var blocks = new List<string>();
    foreach (var c in coolingBanks) blocks.Add(BuildCoolingBankBlock(c));
    File.WriteAllText(sfd.FileName, string.Join(Environment.NewLine + Environment.NewLine, blocks));

    MessageBox.Show(this, "CoolingBank-Export abgeschlossen.", "Info",
        MessageBoxButtons.OK, MessageBoxIcon.Information);
  }

  private void ExportRollParams()
  {
    if (rollParams.Count == 0)
    {
      MessageBox.Show(this, "Keine RollParams zum Exportieren vorhanden.", "Hinweis",
          MessageBoxButtons.OK, MessageBoxIcon.Information);
      return;
    }

    using var sfd = new SaveFileDialog
    {
      Filter = "Textdatei (*.txt)|*.txt|Alle Dateien (*.*)|*.*",
      InitialDirectory = appFolder,
      FileName = $"rollparam_export_{DateTime.Now:yyyyMMdd_HHmmss}.txt"
    };

    if (sfd.ShowDialog(this) != DialogResult.OK) return;

    var blocks = new List<string>();
    foreach (var rp in rollParams) blocks.Add(BuildRollParamBlock(rp));
    File.WriteAllText(sfd.FileName, string.Join(Environment.NewLine + Environment.NewLine, blocks));

    MessageBox.Show(this, "RollParam-Export abgeschlossen.", "Info",
        MessageBoxButtons.OK, MessageBoxIcon.Information);
  }

  // =========================================================
  // Parse/Build project file
  // =========================================================
  private static string BuildProjectFileText(
      List<NozzleData> nozzles,
      List<CoolingBankData> coolingBanks,
      List<RollParamData> rollParams)
  {
    var ci = CultureInfo.InvariantCulture;
    var lines = new List<string> { "#PROJECT_V4", "[NOZZELS]" };

    foreach (var nozzle in nozzles)
    {
      lines.Add("[NOZZLE]");
      lines.Add($"id={nozzle.Id}");
      lines.Add($"brand={nozzle.Brand}");
      lines.Add($"nozzelTypbezeichnung={nozzle.NozzelTypbezeichnung}");
      lines.Add($"typeNozzel={nozzle.TypeNozzel}");
      lines.Add($"maxVolFlowLMin={nozzle.MaxVolFlowLMin.ToString("R", ci)}");
      lines.Add($"refPressure={nozzle.RefPressure.ToString("R", ci)}");
      lines.Add($"angleSprayDeg={nozzle.AngleSprayDeg.ToString("R", ci)}");
      lines.Add("[/NOZZLE]");
    }

    lines.Add("[/NOZZELS]");
    lines.Add("[COOLINGBANKS]");

    foreach (var bank in coolingBanks)
    {
      lines.Add("[COOLINGBANK]");
      lines.Add($"id={bank.Id}");
      lines.Add($"typeBank={bank.TypeBank}");
      lines.Add($"zoneCount={bank.Zones.Count.ToString(ci)}");

      for (int i = 0; i < bank.Zones.Count; i++)
      {
        var zone = bank.Zones[i];
        string prefix = $"zone{i + 1}";
        lines.Add($"{prefix}.count={zone.Count.ToString(ci)}");
        lines.Add($"{prefix}.distance={zone.Distance.ToString("R", ci)}");
        lines.Add($"{prefix}.nozzleId={zone.NozzleId}");
      }

      for (int i = 0; i < bank.Gaps.Count; i++)
        lines.Add($"gap{i + 1}={bank.Gaps[i].ToString("R", ci)}");

      lines.Add("[/COOLINGBANK]");
    }

    lines.Add("[/COOLINGBANKS]");
    lines.Add("[ROLLPARAMS]");

    foreach (var rp in rollParams)
    {
      lines.Add("[ROLLPARAM]");
      lines.Add($"paramName={rp.ParamName}");
      lines.Add($"typeRoll={rp.TypeRoll}");
      lines.Add($"bankCount={rp.Banks.Count}");

      for (int i = 0; i < rp.Banks.Count; i++)
      {
        var b = rp.Banks[i];
        string p = $"bank{i}";
        lines.Add($"{p}.type={b.BankType}");
        lines.Add($"{p}.index={b.Index}");
        lines.Add($"{p}.xpos={b.XPos.ToString("R", ci)}");
        lines.Add($"{p}.ypos={b.YPos.ToString("R", ci)}");
        lines.Add($"{p}.angle={b.AngleSlope.ToString("R", ci)}");
      }

      lines.Add("[/ROLLPARAM]");
    }

    lines.Add("[/ROLLPARAMS]");
    return string.Join(Environment.NewLine, lines);
  }

  private static ProjectData ParseProjectFile(string path)
  {
    var result = new ProjectData();
    var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    string? section = null;

    foreach (string raw in File.ReadAllLines(path))
    {
      string line = raw.Trim();
      if (line.Length == 0 || line.StartsWith("#")) continue;

      if (line.Equals("[NOZZLE]", StringComparison.OrdinalIgnoreCase) ||
          line.Equals("[COOLINGBANK]", StringComparison.OrdinalIgnoreCase) ||
          line.Equals("[ROLLPARAM]", StringComparison.OrdinalIgnoreCase))
      {
        if (section != null) throw new FormatException("Verschachtelter Datensatz.");
        section = line.Trim('[', ']').ToUpperInvariant();
        values.Clear();
        continue;
      }

      if (line.Equals("[/NOZZLE]", StringComparison.OrdinalIgnoreCase) ||
          line.Equals("[/COOLINGBANK]", StringComparison.OrdinalIgnoreCase) ||
          line.Equals("[/ROLLPARAM]", StringComparison.OrdinalIgnoreCase))
      {
        string expected = $"[/{section}]";
        if (section == null || !line.Equals(expected, StringComparison.OrdinalIgnoreCase))
          throw new FormatException("Ungültiges Datensatzende.");

        if (section == "NOZZLE")
        {
          result.Nozzles.Add(new NozzleData
          {
            Id = RequiredValue(values, "id"),
            Brand = RequiredValue(values, "brand"),
            NozzelTypbezeichnung = RequiredValue(values, "nozzelTypbezeichnung"),
            TypeNozzel = RequiredValue(values, "typeNozzel"),
            MaxVolFlowLMin = ReadProjectDouble(values, "maxVolFlowLMin"),
            RefPressure = ReadProjectDouble(values, "refPressure"),
            AngleSprayDeg = ReadProjectDouble(values, "angleSprayDeg")
          });
        }
        else if (section == "COOLINGBANK")
        {
          var bank = new CoolingBankData
          {
            Id = RequiredValue(values, "id"),
            TypeBank = RequiredValue(values, "typeBank")
          };

          if (values.ContainsKey("zoneCount"))
          {
            int zoneCount = ReadProjectInt(values, "zoneCount");
            if (zoneCount < 1 || zoneCount > 3) throw new FormatException("Erlaubt sind 1 bis 3 Zonen.");

            for (int i = 0; i < zoneCount; i++)
            {
              string p = $"zone{i + 1}";
              bank.Zones.Add(new CoolingBankZoneData
              {
                Count = ReadProjectInt(values, $"{p}.count"),
                Distance = ReadProjectDouble(values, $"{p}.distance"),
                NozzleId = RequiredValue(values, $"{p}.nozzleId")
              });
            }

            for (int i = 0; i < zoneCount - 1; i++)
              bank.Gaps.Add(ReadProjectDouble(values, $"gap{i + 1}"));
          }
          else
          {
            bank.Zones.Add(new CoolingBankZoneData
            {
              Count = ReadProjectInt(values, "noNozzel"),
              Distance = ReadProjectDouble(values, "distance"),
              NozzleId = RequiredValue(values, "nozzleId")
            });
          }

          result.CoolingBanks.Add(bank);
        }
        else if (section == "ROLLPARAM")
        {
          var rp = new RollParamData
          {
            ParamName = RequiredValue(values, "paramName"),
            TypeRoll = RequiredValue(values, "typeRoll")
          };

          int bankCount = values.ContainsKey("bankCount") ? ReadProjectInt(values, "bankCount") : 0;

          for (int i = 0; i < bankCount; i++)
          {
            string p = $"bank{i}";
            rp.Banks.Add(new RollParamBankData
            {
              BankType = RequiredValue(values, $"{p}.type"),
              Index = ReadProjectInt(values, $"{p}.index"),
              XPos = ReadProjectDouble(values, $"{p}.xpos"),
              YPos = ReadProjectDouble(values, $"{p}.ypos"),
              AngleSlope = ReadProjectDouble(values, $"{p}.angle")
            });
          }

          result.RollParams.Add(rp);
        }

        section = null;
        values.Clear();
        continue;
      }

      if (section != null)
      {
        int separator = line.IndexOf('=');
        if (separator <= 0) throw new FormatException($"Ungültige Projektzeile: {line}");
        values[line[..separator].Trim()] = line[(separator + 1)..].Trim();
      }
    }

    if (section != null)
      throw new FormatException("Ein Datensatz wurde nicht abgeschlossen.");

    return result;
  }

  private static string RequiredValue(Dictionary<string, string> values, string key)
  {
    if (!values.TryGetValue(key, out string? value) || string.IsNullOrWhiteSpace(value))
      throw new FormatException($"Projektfeld '{key}' fehlt.");
    return value;
  }

  private static int ReadProjectInt(Dictionary<string, string> values, string key)
  {
    if (!int.TryParse(RequiredValue(values, key), NumberStyles.Integer, CultureInfo.InvariantCulture, out int value))
      throw new FormatException($"Projektfeld '{key}' ist keine Ganzzahl.");
    return value;
  }

  private static double ReadProjectDouble(Dictionary<string, string> values, string key)
  {
    if (!double.TryParse(RequiredValue(values, key).Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out double value) ||
        !double.IsFinite(value))
      throw new FormatException($"Projektfeld '{key}' ist keine gültige Zahl.");
    return value;
  }

  // =========================================================
  // Save-prompt and dirty state
  // =========================================================
  private void OnFormClosingAskSave(object? sender, FormClosingEventArgs e)
  {
    if (!isDirty) return;

    var result = MessageBox.Show(this,
        "Es gibt ungespeicherte Änderungen. Möchtest du vor dem Beenden speichern?",
        "Änderungen speichern",
        MessageBoxButtons.YesNoCancel,
        MessageBoxIcon.Question);

    if (result == DialogResult.Cancel) { e.Cancel = true; return; }
    if (result == DialogResult.No) return;

    try { SaveProject(currentProjectPath, false); }
    catch { e.Cancel = true; }
  }

  private bool ConfirmDiscardUnsavedChanges()
  {
    if (!isDirty) return true;

    var result = MessageBox.Show(this,
        "Es gibt ungespeicherte Änderungen. Sollen diese vor dem Laden gespeichert werden?",
        "Ungespeicherte Änderungen",
        MessageBoxButtons.YesNoCancel,
        MessageBoxIcon.Question);

    if (result == DialogResult.Cancel) return false;
    if (result == DialogResult.No) return true;

    try { SaveProject(currentProjectPath, false); return true; }
    catch { return false; }
  }

  private void MarkDirty()
  {
    isDirty = true;
    Text = $"Project Editor - {currentProjectPath} *";
  }

  private void MarkClean()
  {
    isDirty = false;
    Text = $"Project Editor - {currentProjectPath}";
  }

  // =========================================================
  // Helpers
  // =========================================================
  private bool TryReadDouble(TextBox input, string fieldName, bool showValidation, out double value)
  {
    value = 0.0;
    var text = input.Text.Trim().Replace(',', '.');
    if (string.IsNullOrWhiteSpace(text)) return false;

    if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value) && double.IsFinite(value))
      return true;

    if (showValidation)
      MessageBox.Show(this, $"Bitte für '{fieldName}' eine gültige Zahl eingeben.", "Ungültige Eingabe",
          MessageBoxButtons.OK, MessageBoxIcon.Warning);

    return false;
  }

  private bool TryReadInt(TextBox input, string fieldName, bool showValidation, out int value)
  {
    value = 0;
    var text = input.Text.Trim();
    if (string.IsNullOrWhiteSpace(text)) return false;

    if (int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out value))
      return true;

    if (showValidation)
      MessageBox.Show(this, $"Bitte für '{fieldName}' eine gültige Ganzzahl eingeben.", "Ungültige Eingabe",
          MessageBoxButtons.OK, MessageBoxIcon.Warning);

    return false;
  }

  private static string ToScientificNoLeadingZero(double value, CultureInfo ci)
  {
    var s = value.ToString("0.00e+00", ci);
    return Regex.Replace(s, @"e([+-])0+(\d+)$", "e$1$2");
  }
}

// =========================================================
// Models
// =========================================================
public class NozzleData
{
  public string Id { get; set; } = "";
  public string Brand { get; set; } = "";
  public string NozzelTypbezeichnung { get; set; } = "";
  public string TypeNozzel { get; set; } = "";
  public double MaxVolFlowLMin { get; set; }
  public double RefPressure { get; set; }
  public double AngleSprayDeg { get; set; }
}

public class CoolingBankZoneData
{
  public int Count { get; set; }
  public double Distance { get; set; }
  public string NozzleId { get; set; } = "";
}

public class CoolingBankData
{
  public string Id { get; set; } = "";
  public string TypeBank { get; set; } = "";
  public List<CoolingBankZoneData> Zones { get; set; } = new();
  public List<double> Gaps { get; set; } = new();

  public int NoNozzel
  {
    get
    {
      int total = 0;
      foreach (var zone in Zones) total = checked(total + zone.Count);
      return total;
    }
  }
}

public class RollParamBankData
{
  public string BankType { get; set; } = "";
  public int Index { get; set; }
  public double XPos { get; set; }
  public double YPos { get; set; }
  public double AngleSlope { get; set; }
}

public class RollParamData
{
  public string ParamName { get; set; } = "workRollParam";
  public string TypeRoll { get; set; } = "WR";
  public List<RollParamBankData> Banks { get; set; } = new();
}

public class ProjectData
{
  public List<NozzleData> Nozzles { get; set; } = new();
  public List<CoolingBankData> CoolingBanks { get; set; } = new();
  public List<RollParamData> RollParams { get; set; } = new();
}