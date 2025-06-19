using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using SoulsFormats;
using System.IO;
using System.Numerics; // For Vector2

namespace MATBIN_Editor
{
    public class MainForm : Form
    {
        private MATBIN currentMatbin;
        private string currentFilePath;
        private bool isLoading = false; // To prevent event firing during loading

        // --- UI Elements ---
        private MenuStrip menuStripMain;
        private ToolStripMenuItem menuItemFile;
        private ToolStripMenuItem menuItemFileOpen;
        private ToolStripMenuItem menuItemFileSave;
        private ToolStripMenuItem menuItemFileSaveAs;
        private ToolStripSeparator menuItemFileSeparator;
        private ToolStripMenuItem menuItemFileExit;

        private GroupBox gbMatbinProperties;
        private Label lblShaderPath;
        private TextBox txtShaderPath;
        private Label lblSourcePath;
        private TextBox txtSourcePath;
        private Label lblMatbinKey;
        private NumericUpDown numMatbinKey;

        private TableLayoutPanel tlpMainLayout; // Main layout for params and samplers sections

        // Parameters Section
        private GroupBox gbParamsList;
        private ListBox lstParams;
        private Panel pnlParamButtons;
        private Button btnAddParam;
        private Button btnRemoveParam;

        private GroupBox gbParamDetails;
        private Label lblParamName;
        private TextBox txtParamName;
        private Label lblParamKeyVal; // Renamed to avoid conflict with MATBIN.Key
        private NumericUpDown numParamKeyVal;
        private Label lblParamType;
        private ComboBox cmbParamType;
        private Label lblParamValue;
        private TextBox txtParamValue;

        // Samplers Section
        private GroupBox gbSamplersList;
        private ListBox lstSamplers;
        private Panel pnlSamplerButtons;
        private Button btnAddSampler;
        private Button btnRemoveSampler;

        private GroupBox gbSamplerDetails;
        private Label lblSamplerType;
        private TextBox txtSamplerType;
        private Label lblSamplerPath;
        private TextBox txtSamplerPath;
        private Label lblSamplerKeyVal; // Renamed
        private NumericUpDown numSamplerKeyVal;
        private Label lblSamplerUnk14X;
        private NumericUpDown numSamplerUnk14X;
        private Label lblSamplerUnk14Y;
        private NumericUpDown numSamplerUnk14Y;

        public MainForm()
        {
            InitializeComponentManual();
            InitializeParamTypeComboBox();
            //DisableAllControls(); // Start with controls disabled
            this.Load += MainForm_Load; // For setting DisplayMember
        }

        private void InitializeComponentManual()
        {
            this.SuspendLayout(); // Suspend layout for performance

            // Form Properties
            this.Text = "MATBIN Editor 1.0 by Forsakensilver";
            this.Size = new Size(900, 700);
            this.MinimumSize = new Size(700, 500);

            // --- MenuStrip ---
            menuStripMain = new MenuStrip();
            menuItemFile = new ToolStripMenuItem("&File");
            menuItemFileOpen = new ToolStripMenuItem("&Open...");
            menuItemFileSave = new ToolStripMenuItem("&Save");
            menuItemFileSaveAs = new ToolStripMenuItem("Save &As...");
            menuItemFileSeparator = new ToolStripSeparator();
            menuItemFileExit = new ToolStripMenuItem("E&xit");

            menuItemFileOpen.Click += openToolStripMenuItem_Click;
            menuItemFileSave.Click += saveToolStripMenuItem_Click;
            menuItemFileSaveAs.Click += saveAsToolStripMenuItem_Click;
            menuItemFileExit.Click += exitToolStripMenuItem_Click;

            menuItemFile.DropDownItems.AddRange(new ToolStripItem[] {
                menuItemFileOpen, menuItemFileSave, menuItemFileSaveAs, menuItemFileSeparator, menuItemFileExit
            });
            menuStripMain.Items.Add(menuItemFile);
            this.MainMenuStrip = menuStripMain;
            // Add MenuStrip to the Form's controls
            this.Controls.Add(menuStripMain);
            
            // --- MATBIN Properties GroupBox ---
            gbMatbinProperties = new GroupBox { Text = "MATBIN Properties", Dock = DockStyle.Bottom, Height = 110, Padding = new Padding(10) };
            lblShaderPath = new Label { Text = "Shader Path:", Location = new Point(10, 25), AutoSize = true };
            txtShaderPath = new TextBox { Location = new Point(100, 22), Width = gbMatbinProperties.Width - 120, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
            txtShaderPath.TextChanged += MatbinProperty_Changed;

            lblSourcePath = new Label { Text = "Source Path:", Location = new Point(10, 55), AutoSize = true };
            txtSourcePath = new TextBox { Location = new Point(100, 52), Width = gbMatbinProperties.Width - 120, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
            txtSourcePath.TextChanged += MatbinProperty_Changed;

            lblMatbinKey = new Label { Text = "Key:", Location = new Point(10, 85), AutoSize = true };
            numMatbinKey = new NumericUpDown { Location = new Point(100, 82), Width = 120, Maximum = uint.MaxValue };
            numMatbinKey.ValueChanged += MatbinProperty_Changed;

            gbMatbinProperties.Controls.AddRange(new Control[] { lblShaderPath, txtShaderPath, lblSourcePath, txtSourcePath, lblMatbinKey, numMatbinKey });
            this.Controls.Add(gbMatbinProperties);


            // --- Main Layout Panel (TableLayoutPanel) ---
            tlpMainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1, // Will add rows for details panels later if needed or stack vertically
                Padding = new Padding(5)
            };
            tlpMainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tlpMainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            this.Controls.Add(tlpMainLayout);
            //tlpMainLayout.BringToFront(); // Ensure it's above gbMatbinProperties if Dock.Fill overlaps

            // --- Parameters Section (Left Column of tlpMainLayout) ---
            var pnlParamsContainer = new Panel { Dock = DockStyle.Fill }; // Container for params list and details
            tlpMainLayout.Controls.Add(pnlParamsContainer, 0, 0);

            gbParamsList = new GroupBox { Text = "Parameters", Dock = DockStyle.Top, Height = 200, Padding = new Padding(5) }; // Adjust height as needed
            lstParams = new ListBox { Dock = DockStyle.Fill };
            lstParams.SelectedIndexChanged += lstParams_SelectedIndexChanged;

            pnlParamButtons = new Panel { Dock = DockStyle.Bottom, Height = 30, Padding = new Padding(0, 5, 0, 0) };
            btnAddParam = new Button { Text = "Add", Location = new Point(0, 0), Width = 75 };
            btnRemoveParam = new Button { Text = "Remove", Location = new Point(80, 0), Width = 75 };
            btnAddParam.Click += btnAddParam_Click;
            btnRemoveParam.Click += btnRemoveParam_Click;
            pnlParamButtons.Controls.AddRange(new Control[] { btnAddParam, btnRemoveParam });

            gbParamsList.Controls.Add(lstParams);
            gbParamsList.Controls.Add(pnlParamButtons);
            pnlParamsContainer.Controls.Add(gbParamsList);

            gbParamDetails = new GroupBox { Text = "Parameter Details", Dock = DockStyle.Fill, Padding = new Padding(10) }; // Fills remaining space in pnlParamsContainer
            lblParamName = new Label { Text = "Name:", Location = new Point(10, 25), AutoSize = true };
            txtParamName = new TextBox { Location = new Point(100, 22), Width = gbParamDetails.Width - 120, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
            txtParamName.TextChanged += ParamDetail_Changed;

            lblParamKeyVal = new Label { Text = "Key:", Location = new Point(10, 55), AutoSize = true };
            numParamKeyVal = new NumericUpDown { Location = new Point(100, 52), Width = 120, Maximum = uint.MaxValue };
            numParamKeyVal.ValueChanged += ParamDetail_Changed;

            lblParamType = new Label { Text = "Type:", Location = new Point(10, 85), AutoSize = true };
            cmbParamType = new ComboBox { Location = new Point(100, 82), Width = 150, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbParamType.SelectedIndexChanged += cmbParamType_SelectedIndexChanged;

            lblParamValue = new Label { Text = "Value:", Location = new Point(10, 115), AutoSize = true };
            txtParamValue = new TextBox { Location = new Point(100, 112), Width = gbParamDetails.Width - 120, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
            txtParamValue.TextChanged += ParamDetail_Changed;

            gbParamDetails.Controls.AddRange(new Control[] { lblParamName, txtParamName, lblParamKeyVal, numParamKeyVal, lblParamType, cmbParamType, lblParamValue, txtParamValue });
            pnlParamsContainer.Controls.Add(gbParamDetails);
            gbParamDetails.BringToFront(); // Ensure details are above the list if docking causes overlap

            // --- Samplers Section (Right Column of tlpMainLayout) ---
            var pnlSamplersContainer = new Panel { Dock = DockStyle.Fill };
            tlpMainLayout.Controls.Add(pnlSamplersContainer, 1, 0);

            gbSamplersList = new GroupBox { Text = "Samplers", Dock = DockStyle.Top, Height = 200, Padding = new Padding(5) }; // Adjust height
            lstSamplers = new ListBox { Dock = DockStyle.Fill };
            lstSamplers.SelectedIndexChanged += lstSamplers_SelectedIndexChanged;

            pnlSamplerButtons = new Panel { Dock = DockStyle.Bottom, Height = 30, Padding = new Padding(0, 5, 0, 0) };
            btnAddSampler = new Button { Text = "Add", Location = new Point(0, 0), Width = 75 };
            btnRemoveSampler = new Button { Text = "Remove", Location = new Point(80, 0), Width = 75 };
            btnAddSampler.Click += btnAddSampler_Click;
            btnRemoveSampler.Click += btnRemoveSampler_Click;
            pnlSamplerButtons.Controls.AddRange(new Control[] { btnAddSampler, btnRemoveSampler });

            gbSamplersList.Controls.Add(lstSamplers);
            gbSamplersList.Controls.Add(pnlSamplerButtons);
            pnlSamplersContainer.Controls.Add(gbSamplersList);

            gbSamplerDetails = new GroupBox { Text = "Sampler Details", Dock = DockStyle.Fill, Padding = new Padding(10) };
            lblSamplerType = new Label { Text = "Type:", Location = new Point(10, 25), AutoSize = true };
            txtSamplerType = new TextBox { Location = new Point(100, 22), Width = gbSamplerDetails.Width - 120, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
            txtSamplerType.TextChanged += SamplerDetail_Changed;

            lblSamplerPath = new Label { Text = "Path:", Location = new Point(10, 55), AutoSize = true };
            txtSamplerPath = new TextBox { Location = new Point(100, 52), Width = gbSamplerDetails.Width - 120, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
            txtSamplerPath.TextChanged += SamplerDetail_Changed;

            lblSamplerKeyVal = new Label { Text = "Key:", Location = new Point(10, 85), AutoSize = true };
            numSamplerKeyVal = new NumericUpDown { Location = new Point(100, 82), Width = 120, Maximum = uint.MaxValue };
            numSamplerKeyVal.ValueChanged += SamplerDetail_Changed;

            lblSamplerUnk14X = new Label { Text = "Unk14 X:", Location = new Point(10, 115), AutoSize = true };
            numSamplerUnk14X = new NumericUpDown { Location = new Point(100, 112), Width = 100, DecimalPlaces = 3, Minimum = -10000, Maximum = 10000 }; // Adjust min/max
            numSamplerUnk14X.ValueChanged += SamplerDetail_Changed;

            lblSamplerUnk14Y = new Label { Text = "Unk14 Y:", Location = new Point(10, 145), AutoSize = true };
            numSamplerUnk14Y = new NumericUpDown { Location = new Point(100, 142), Width = 100, DecimalPlaces = 3, Minimum = -10000, Maximum = 10000 }; // Adjust min/max
            numSamplerUnk14Y.ValueChanged += SamplerDetail_Changed;

            gbSamplerDetails.Controls.AddRange(new Control[] { lblSamplerType, txtSamplerType, lblSamplerPath, txtSamplerPath, lblSamplerKeyVal, numSamplerKeyVal, lblSamplerUnk14X, numSamplerUnk14X, lblSamplerUnk14Y, numSamplerUnk14Y });
            pnlSamplersContainer.Controls.Add(gbSamplerDetails);
            gbSamplerDetails.BringToFront();

            // Handle resizing for TextBoxes within GroupBoxes
            gbMatbinProperties.Resize += (s, e) => { txtShaderPath.Width = gbMatbinProperties.ClientSize.Width - txtShaderPath.Left - 10; txtSourcePath.Width = gbMatbinProperties.ClientSize.Width - txtSourcePath.Left - 10; };
            gbParamDetails.Resize += (s, e) => { txtParamName.Width = gbParamDetails.ClientSize.Width - txtParamName.Left - 10; txtParamValue.Width = gbParamDetails.ClientSize.Width - txtParamValue.Left - 10; };
            gbSamplerDetails.Resize += (s, e) => { txtSamplerType.Width = gbSamplerDetails.ClientSize.Width - txtSamplerType.Left - 10; txtSamplerPath.Width = gbSamplerDetails.ClientSize.Width - txtSamplerPath.Left - 10; };

            MainMenuStrip.BringToFront();
            this.ResumeLayout(false);
            this.PerformLayout();
        }


        private void InitializeParamTypeComboBox()
        {
            cmbParamType.DataSource = Enum.GetValues(typeof(MATBIN.ParamType));
        }

        private void DisableAllControls()
        {
            gbMatbinProperties.Enabled = false;
            gbParamsList.Enabled = false;
            gbParamDetails.Enabled = false;
            gbSamplersList.Enabled = false;
            gbSamplerDetails.Enabled = false;
            menuItemFileSave.Enabled = false;
            menuItemFileSaveAs.Enabled = false;
        }

        private void EnableMainControls()
        {
            gbMatbinProperties.Enabled = true;
            gbParamsList.Enabled = true;
            // Details are enabled on selection
            gbSamplersList.Enabled = true;
            menuItemFileSave.Enabled = true;
            menuItemFileSaveAs.Enabled = true;
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            lstParams.DisplayMember = "Name";
            lstSamplers.DisplayMember = "Type"; // Or Path, based on preference
        }

        // --- Menu Event Handlers ---
        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Filter = "MATBIN Files (*.matbin)|*.matbin|All Files (*.*)|*.*";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        currentMatbin = MATBIN.Read(ofd.FileName);
                        currentFilePath = ofd.FileName;
                        Text = $"MATBIN Editor - {Path.GetFileName(currentFilePath)}";
                        LoadMatbinToUI();
                        EnableMainControls();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error loading MATBIN file: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        currentMatbin = null;
                        currentFilePath = null;
                        DisableAllControls();
                        Text = "MATBIN Editor (Pure C#)";
                    }
                }
            }
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (currentMatbin == null) return;
            if (string.IsNullOrEmpty(currentFilePath))
            {
                saveAsToolStripMenuItem_Click(sender, e);
                return;
            }
            SaveChangesToMatbin(); // Ensure pending changes are captured
            try
            {
                currentMatbin.Write(currentFilePath);
                MessageBox.Show("File saved successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving MATBIN file: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (currentMatbin == null) return;

            using (SaveFileDialog sfd = new SaveFileDialog())
            {
                sfd.Filter = "MATBIN Files (*.matbin)|*.matbin|All Files (*.*)|*.*";
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    SaveChangesToMatbin(); // Ensure pending changes are captured
                    currentFilePath = sfd.FileName;
                    Text = $"MATBIN Editor - {Path.GetFileName(currentFilePath)}";
                    try
                    {
                        currentMatbin.Write(currentFilePath);
                        MessageBox.Show("File saved successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error saving MATBIN file: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        // --- UI Population and Update Logic ---

        private void LoadMatbinToUI()
        {
            if (currentMatbin == null) return;
            isLoading = true;

            txtShaderPath.Text = currentMatbin.ShaderPath;
            txtSourcePath.Text = currentMatbin.SourcePath;
            numMatbinKey.Value = currentMatbin.Key;

            PopulateParamsList();
            PopulateSamplersList();

            gbParamDetails.Enabled = false;
            gbSamplerDetails.Enabled = false;
            isLoading = false;
        }

        private void PopulateParamsList()
        {
            lstParams.Items.Clear();
            if (currentMatbin?.Params != null)
            {
                foreach (var param in currentMatbin.Params)
                {
                    lstParams.Items.Add(param);
                }
            }
            gbParamDetails.Enabled = lstParams.SelectedItem != null;
        }

        private void PopulateSamplersList()
        {
            lstSamplers.Items.Clear();
            if (currentMatbin?.Samplers != null)
            {
                foreach (var sampler in currentMatbin.Samplers)
                {
                    lstSamplers.Items.Add(sampler);
                }
            }
            gbSamplerDetails.Enabled = lstSamplers.SelectedItem != null;
        }

        private void MatbinProperty_Changed(object sender, EventArgs e)
        {
            if (isLoading || currentMatbin == null) return;
            currentMatbin.ShaderPath = txtShaderPath.Text;
            currentMatbin.SourcePath = txtSourcePath.Text;
            currentMatbin.Key = (uint)numMatbinKey.Value;
        }


        private void SaveChangesToMatbin() // Called before saving the file
        {
            if (currentMatbin == null) return;

            // Save currently selected item's details if any are selected
            if (lstParams.SelectedItem is MATBIN.Param selectedParam)
            {
                UpdateParamFromDetails(selectedParam);
            }
            if (lstSamplers.SelectedItem is MATBIN.Sampler selectedSampler)
            {
                UpdateSamplerFromDetails(selectedSampler);
            }

            // General MATBIN properties are updated via their TextChanged/ValueChanged events
            // The lists (currentMatbin.Params and currentMatbin.Samplers) are modified directly by Add/Remove
        }


        // --- Param Controls Event Handlers ---
        private void lstParams_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (isLoading) return;
            // Save previous param details before loading new one
            if (lstParams.Tag is MATBIN.Param previouslySelectedParam) // Use Tag to store previous
            {
                UpdateParamFromDetails(previouslySelectedParam);
            }

            if (lstParams.SelectedItem is MATBIN.Param selectedParam)
            {
                LoadParamDetails(selectedParam);
                gbParamDetails.Enabled = true;
                lstParams.Tag = selectedParam; // Store current selection
            }
            else
            {
                gbParamDetails.Enabled = false;
                lstParams.Tag = null;
            }
        }

        private void LoadParamDetails(MATBIN.Param param)
        {
            isLoading = true;
            txtParamName.Text = param.Name;
            numParamKeyVal.Value = param.Key;
            cmbParamType.SelectedItem = param.Type;
            txtParamValue.Text = FormatParamValue(param.Value, param.Type);
            isLoading = false;
        }

        private void UpdateParamFromDetails(MATBIN.Param param)
        {
            if (param == null || isLoading) return;

            param.Name = txtParamName.Text;
            param.Key = (uint)numParamKeyVal.Value;

            // Only update type if it's different, to avoid re-parsing value unnecessarily
            // if only other fields changed.
            MATBIN.ParamType newType = (MATBIN.ParamType)cmbParamType.SelectedItem;
            bool typeChanged = param.Type != newType;
            param.Type = newType;

            try
            {
                param.Value = ParseParamValue(txtParamValue.Text, param.Type);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error parsing param value for '{param.Name}': {ex.Message}\nValue not updated.", "Parse Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                // Revert displayed value if parsing failed
                txtParamValue.Text = FormatParamValue(param.Value, param.Type);
            }

            // Refresh item in ListBox if its display text might change (e.g. if Name changed)
            int index = currentMatbin.Params.IndexOf(param); // Use the source list
            if (index != -1)
            {
                // To refresh display in ListBox:
                lstParams.Items[lstParams.Items.IndexOf(param)] = param; // This forces a refresh if DisplayMember is used
            }
        }

        private void ParamDetail_Changed(object sender, EventArgs e)
        {
            if (isLoading || !(lstParams.SelectedItem is MATBIN.Param selectedParam)) return;
            UpdateParamFromDetails(selectedParam);
        }

        private void cmbParamType_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (isLoading || !(lstParams.SelectedItem is MATBIN.Param selectedParam)) return;
            // When type changes, the current value might be invalid for the new type.
            // UpdateParamFromDetails will handle parsing and potential errors.
            // We might want to clear txtParamValue or provide a default for the new type.
            // For now, it will attempt to parse, which might be fine.
            UpdateParamFromDetails(selectedParam);
        }

        private string FormatParamValue(object value, MATBIN.ParamType type)
        {
            if (value == null) return "";
            try
            {
                switch (type)
                {
                    case MATBIN.ParamType.Bool:
                        return ((bool)value).ToString();
                    case MATBIN.ParamType.Int:
                        return ((int)value).ToString();
                    case MATBIN.ParamType.Int2:
                        return string.Join(", ", (int[])value);
                    case MATBIN.ParamType.Float:
                        return ((float)value).ToString(System.Globalization.CultureInfo.InvariantCulture);
                    case MATBIN.ParamType.Float2:
                    case MATBIN.ParamType.Float3:
                    case MATBIN.ParamType.Float4:
                    case MATBIN.ParamType.Float5:
                        return string.Join(", ", ((float[])value).Select(f => f.ToString(System.Globalization.CultureInfo.InvariantCulture)));
                    default:
                        return value.ToString();
                }
            }
            catch (InvalidCastException) // Handle cases where value might not match type (e.g., after type change)
            {
                return ""; // Or some default like "0" or "0.0"
            }
        }

        private object ParseParamValue(string text, MATBIN.ParamType type)
        {
            text = text.Trim();
            try
            {
                switch (type)
                {
                    case MATBIN.ParamType.Bool:
                        return bool.Parse(text);
                    case MATBIN.ParamType.Int:
                        return int.Parse(text);
                    case MATBIN.ParamType.Int2:
                        var ints = text.Split(',').Select(s => int.Parse(s.Trim())).ToArray();
                        if (ints.Length != 2) throw new FormatException("Int2 requires 2 values.");
                        return ints;
                    case MATBIN.ParamType.Float:
                        return float.Parse(text, System.Globalization.CultureInfo.InvariantCulture);
                    case MATBIN.ParamType.Float2:
                        var f2 = text.Split(',').Select(s => float.Parse(s.Trim(), System.Globalization.CultureInfo.InvariantCulture)).ToArray();
                        if (f2.Length != 2) throw new FormatException("Float2 requires 2 values.");
                        return f2;
                    case MATBIN.ParamType.Float3:
                        var f3 = text.Split(',').Select(s => float.Parse(s.Trim(), System.Globalization.CultureInfo.InvariantCulture)).ToArray();
                        if (f3.Length != 3) throw new FormatException("Float3 requires 3 values.");
                        return f3;
                    case MATBIN.ParamType.Float4:
                        var f4 = text.Split(',').Select(s => float.Parse(s.Trim(), System.Globalization.CultureInfo.InvariantCulture)).ToArray();
                        if (f4.Length != 4) throw new FormatException("Float4 requires 4 values.");
                        return f4;
                    case MATBIN.ParamType.Float5:
                        var f5 = text.Split(',').Select(s => float.Parse(s.Trim(), System.Globalization.CultureInfo.InvariantCulture)).ToArray();
                        if (f5.Length != 5) throw new FormatException("Float5 requires 5 values.");
                        return f5;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(type), $"Unsupported param type for parsing: {type}");
                }
            }
            catch (FormatException ex)
            {
                throw new FormatException($"Invalid format for type {type}. Expected comma-separated values if applicable. Details: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error parsing value for type {type}. Details: {ex.Message}", ex);
            }
        }


        private void btnAddParam_Click(object sender, EventArgs e)
        {
            if (currentMatbin == null) return;
            MATBIN.Param newParam = new MATBIN.Param()
            {
                Name = "NewParam" + (currentMatbin.Params.Count + 1),
                Type = MATBIN.ParamType.Float,
                Value = 0f,
                Key = 0
            };
            currentMatbin.Params.Add(newParam);
            PopulateParamsList();
            lstParams.SelectedItem = newParam;
        }

        private void btnRemoveParam_Click(object sender, EventArgs e)
        {
            if (currentMatbin == null || lstParams.SelectedItem == null) return;
            MATBIN.Param selectedParam = (MATBIN.Param)lstParams.SelectedItem;
            currentMatbin.Params.Remove(selectedParam);
            PopulateParamsList();
            gbParamDetails.Enabled = false;
        }


        // --- Sampler Controls Event Handlers ---
        private void lstSamplers_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (isLoading) return;
            if (lstSamplers.Tag is MATBIN.Sampler previouslySelectedSampler)
            {
                UpdateSamplerFromDetails(previouslySelectedSampler);
            }

            if (lstSamplers.SelectedItem is MATBIN.Sampler selectedSampler)
            {
                LoadSamplerDetails(selectedSampler);
                gbSamplerDetails.Enabled = true;
                lstSamplers.Tag = selectedSampler;
            }
            else
            {
                gbSamplerDetails.Enabled = false;
                lstSamplers.Tag = null;
            }
        }

        private void LoadSamplerDetails(MATBIN.Sampler sampler)
        {
            isLoading = true;
            txtSamplerType.Text = sampler.Type;
            txtSamplerPath.Text = sampler.Path;
            numSamplerKeyVal.Value = sampler.Key;
            numSamplerUnk14X.Value = (decimal)sampler.Unk14.X;
            numSamplerUnk14Y.Value = (decimal)sampler.Unk14.Y;
            isLoading = false;
        }

        private void UpdateSamplerFromDetails(MATBIN.Sampler sampler)
        {
            if (sampler == null || isLoading) return;

            sampler.Type = txtSamplerType.Text;
            sampler.Path = txtSamplerPath.Text;
            sampler.Key = (uint)numSamplerKeyVal.Value;
            sampler.Unk14 = new Vector2((float)numSamplerUnk14X.Value, (float)numSamplerUnk14Y.Value);

            int index = currentMatbin.Samplers.IndexOf(sampler);
            if (index != -1)
            {
                lstSamplers.Items[lstSamplers.Items.IndexOf(sampler)] = sampler;
            }
        }

        private void SamplerDetail_Changed(object sender, EventArgs e)
        {
            if (isLoading || !(lstSamplers.SelectedItem is MATBIN.Sampler selectedSampler)) return;
            UpdateSamplerFromDetails(selectedSampler);
        }

        private void btnAddSampler_Click(object sender, EventArgs e)
        {
            if (currentMatbin == null) return;
            MATBIN.Sampler newSampler = new MATBIN.Sampler()
            {
                Type = "NewSamplerType" + (currentMatbin.Samplers.Count + 1),
                Path = "",
                Key = 0,
                Unk14 = Vector2.Zero
            };
            currentMatbin.Samplers.Add(newSampler);
            PopulateSamplersList();
            lstSamplers.SelectedItem = newSampler;
        }

        private void btnRemoveSampler_Click(object sender, EventArgs e)
        {
            if (currentMatbin == null || lstSamplers.SelectedItem == null) return;
            MATBIN.Sampler selectedSampler = (MATBIN.Sampler)lstSamplers.SelectedItem;
            currentMatbin.Samplers.Remove(selectedSampler);
            PopulateSamplersList();
            gbSamplerDetails.Enabled = false;
        }
    }

    // Add this to your project, e.g., in Program.cs or a new file
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}