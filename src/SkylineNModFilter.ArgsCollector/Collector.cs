using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

namespace SkylineNModFilter.ArgsCollector
{
    public static class Collector
    {
        public static string[] CollectArgs(Control parent, string report, string[] oldArguments)
        {
            try
            {
                SetThreeLetterCode();
                var choices = GetProteomes();
                using (var form = new OptionsForm(choices, oldArguments))
                {
                    if (form.ShowDialog(parent) != DialogResult.OK) return null;
                    return global::SkylineNModFilter.CollectorArguments.Build(form.Associate,
                        form.Selected == null ? null : form.Selected.FilePath, form.Selected == null ? null : form.Selected.Name,
                        form.Reorder, form.ManifestPath, form.HasHeader, form.Rename, form.NameColumn,
                        form.FilterMissingness, form.MaximumMissingPercent, form.MissingnessOnly, form.MissingnessScope,
                        form.GroupColumn, form.SelectedGroup, form.ExcludeUnannotated, form.ImportAnnotations);
                }
            }
            catch (Exception exception)
            {
                MessageBox.Show("N-Mod Filter is not compatible with this Skyline build.\n\n" + exception.Message, "N-Mod Filter", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }
        }

        private static Assembly SkylineAssembly { get { return AppDomain.CurrentDomain.GetAssemblies().First(a => a.GetType("pwiz.Skyline.Model.DisplayModificationOption", false) != null); } }

        private static void SetThreeLetterCode()
        {
            var type = SkylineAssembly.GetType("pwiz.Skyline.Model.DisplayModificationOption", true);
            var value = type.GetField("THREE_LETTER_CODE", BindingFlags.Public | BindingFlags.Static).GetValue(null);
            type.GetProperty("Current", BindingFlags.Public | BindingFlags.Static).SetValue(null, value, null);
        }

        private static List<ProteomeChoice> GetProteomes()
        {
            var settingsType = SkylineAssembly.GetType("pwiz.Skyline.Properties.Settings", true);
            var settings = settingsType.GetProperty("Default", BindingFlags.Public | BindingFlags.Static).GetValue(null, null);
            var list = settingsType.GetProperty("BackgroundProteomeList").GetValue(settings, null) as IEnumerable;
            var result = new List<ProteomeChoice>();
            foreach (var item in list)
            {
                var type = item.GetType();
                var isNone = (bool)type.GetProperty("IsNone").GetValue(item, null);
                if (isNone) continue;
                var name = (string)type.GetProperty("Name").GetValue(item, null);
                var path = (string)type.GetProperty("DatabasePath").GetValue(item, null) ?? (string)type.GetProperty("FilePath").GetValue(item, null);
                if (!string.IsNullOrWhiteSpace(path)) result.Add(new ProteomeChoice(name, path));
            }
            return result;
        }
    }

    internal sealed class ProteomeChoice
    {
        public ProteomeChoice(string name, string filePath) { Name = name; FilePath = filePath; }
        public string Name { get; private set; }
        public string FilePath { get; private set; }
        public override string ToString() { return Name; }
    }

    internal sealed class OptionsForm : Form
    {
        private readonly CheckBox _associate = new CheckBox();
        private readonly ComboBox _proteomes = new ComboBox();
        private readonly CheckBox _reorder = new CheckBox();
        private readonly TextBox _manifest = new TextBox();
        private readonly Button _browse = new Button();
        private readonly CheckBox _header = new CheckBox();
        private readonly CheckBox _rename = new CheckBox();
        private readonly ComboBox _nameColumns = new ComboBox();
        private readonly CheckBox _filterMissingness = new CheckBox();
        private readonly CheckBox _importAnnotations = new CheckBox();
        private readonly NumericUpDown _maximumMissing = new NumericUpDown();
        private readonly CheckBox _missingnessOnly = new CheckBox();
        private readonly ComboBox _scope = new ComboBox();
        private readonly ComboBox _groupColumns = new ComboBox();
        private readonly ComboBox _selectedGroups = new ComboBox();
        private readonly CheckBox _excludeUnannotated = new CheckBox();
        private readonly int _priorColumn;
        private readonly int _priorGroupColumn;
        private readonly string _priorSelectedGroup;
        public OptionsForm(IList<ProteomeChoice> choices, string[] oldArguments)
        {
            Text = "N-Mod Filter Options"; Width = 620; Height = 620; FormBorderStyle = FormBorderStyle.FixedDialog; StartPosition = FormStartPosition.CenterParent; MaximizeBox = false; MinimizeBox = false;
            var note = new Label { Left = 16, Top = 15, Width = 570, Text = "Modification display will be set to Three-letter code." };
            _associate.Left = 16; _associate.Top = 45; _associate.Width = 200; _associate.Text = "Associate proteins";
            _proteomes.Left = 35; _proteomes.Top = 72; _proteomes.Width = 540; _proteomes.DropDownStyle = ComboBoxStyle.DropDownList;
            foreach (var choice in choices) _proteomes.Items.Add(choice);
            var priorName = ValueAfter(oldArguments, "--background-proteome-name");
            var prior = choices.FirstOrDefault(c => c.Name == priorName); if (prior != null) _proteomes.SelectedItem = prior; else if (_proteomes.Items.Count > 0) _proteomes.SelectedIndex = 0;
            _associate.Checked = oldArguments != null && oldArguments.Contains("--associate-proteins");
            _associate.Enabled = choices.Count > 0; _proteomes.Enabled = _associate.Checked && choices.Count > 0; _associate.CheckedChanged += delegate { _proteomes.Enabled = _associate.Checked; };

            _reorder.SetBounds(16, 112, 400, 24); _reorder.Text = global::SkylineNModFilter.ReplicateNamingText.EnableLabel;
            _manifest.SetBounds(35, 142, 455, 24); _browse.SetBounds(500, 140, 75, 26); _browse.Text = "Browse...";
            _header.SetBounds(35, 174, 200, 24); _header.Text = "File contains header row";
            _rename.SetBounds(35, 204, 310, 24); _rename.Text = global::SkylineNModFilter.ReplicateNamingText.RenameLabel;
            var columnLabel = new Label { Left = 55, Top = 238, Width = 90, Text = "Name column:" };
            _nameColumns.SetBounds(145, 234, 430, 26); _nameColumns.DropDownStyle = ComboBoxStyle.DropDownList;
            _reorder.Checked = Has(oldArguments, "--reorder-replicates"); _manifest.Text = ValueAfter(oldArguments, "--replicate-manifest") ?? string.Empty;
            _header.Checked = Has(oldArguments, "--manifest-has-header"); _rename.Checked = Has(oldArguments, "--rename-replicates");
            int parsedColumn; _priorColumn = int.TryParse(ValueAfter(oldArguments, "--replicate-name-column"), out parsedColumn) ? parsedColumn : 2;
            _browse.Click += delegate { using (var picker = new OpenFileDialog { Filter = "Metadata files (*.fp-manifest;*.tsv;*.csv)|*.fp-manifest;*.tsv;*.csv|All files (*.*)|*.*" }) { if (picker.ShowDialog(this) == DialogResult.OK) { _manifest.Text = picker.FileName; RefreshColumns(); } } };
            _header.CheckedChanged += delegate { RefreshColumns(); }; _rename.CheckedChanged += delegate { UpdateEnabled(); }; _reorder.CheckedChanged += delegate { UpdateEnabled(); };
            _manifest.Leave += delegate { RefreshColumns(); };
            RefreshColumns(); UpdateEnabled();

            _importAnnotations.SetBounds(16, 270, 390, 24); _importAnnotations.Text = "Import all metadata columns as replicate annotations"; _importAnnotations.Checked = Has(oldArguments, "--import-replicate-annotations");
            if (_importAnnotations.Checked) _header.Checked = true;
            _importAnnotations.CheckedChanged += delegate { if (_importAnnotations.Checked) _header.Checked = true; UpdateEnabled(); };
            _filterMissingness.SetBounds(16, 305, 300, 24); _filterMissingness.Text = "Filter precursors by peak-area missingness";
            var missingLabel = new Label { Left = 35, Top = 339, Width = 180, Text = "Maximum missing data (%):" };
            _maximumMissing.SetBounds(215, 335, 75, 24); _maximumMissing.Minimum = 0; _maximumMissing.Maximum = 100; _maximumMissing.Value = 50;
            _filterMissingness.Checked = Has(oldArguments, "--filter-precursor-missingness");
            int priorMissing; if (int.TryParse(ValueAfter(oldArguments, "--max-missing-percent"), out priorMissing) && priorMissing >= 0 && priorMissing <= 100) _maximumMissing.Value = priorMissing;
            _missingnessOnly.SetBounds(35, 366, 250, 24); _missingnessOnly.Text = "Missingness-only mode"; _missingnessOnly.Checked = Has(oldArguments, "--missingness-only");
            var scopeLabel = new Label { Left = 35, Top = 401, Width = 120, Text = "Missingness scope:" };
            _scope.SetBounds(155, 396, 220, 26); _scope.DropDownStyle = ComboBoxStyle.DropDownList;
            _scope.Items.Add(new ScopeChoice("all", "All replicates")); _scope.Items.Add(new ScopeChoice("selected", "Selected group")); _scope.Items.Add(new ScopeChoice("any", "Any group"));
            var priorScope = ValueAfter(oldArguments, "--missingness-scope") ?? "all"; _scope.SelectedItem = _scope.Items.Cast<ScopeChoice>().First(c => c.Value == priorScope);
            var groupColumnLabel = new Label { Left = 35, Top = 435, Width = 120, Text = "Group column:" };
            _groupColumns.SetBounds(155, 430, 420, 26); _groupColumns.DropDownStyle = ComboBoxStyle.DropDownList;
            int parsedGroupColumn; _priorGroupColumn = int.TryParse(ValueAfter(oldArguments, "--group-column"), out parsedGroupColumn) ? parsedGroupColumn : 2;
            _priorSelectedGroup = ValueAfter(oldArguments, "--selected-group");
            var selectedGroupLabel = new Label { Left = 35, Top = 469, Width = 120, Text = "Selected group:" };
            _selectedGroups.SetBounds(155, 464, 420, 26); _selectedGroups.DropDownStyle = ComboBoxStyle.DropDownList;
            _excludeUnannotated.SetBounds(35, 498, 260, 24); _excludeUnannotated.Text = "Exclude unannotated replicates"; _excludeUnannotated.Checked = Has(oldArguments, "--exclude-unannotated");
            _filterMissingness.CheckedChanged += delegate { if (!_filterMissingness.Checked) _missingnessOnly.Checked = false; UpdateEnabled(); };
            _missingnessOnly.CheckedChanged += delegate { if (_missingnessOnly.Checked) _filterMissingness.Checked = true; };
            _scope.SelectedIndexChanged += delegate { UpdateEnabled(); RefreshGroups(); }; _groupColumns.SelectedIndexChanged += delegate { RefreshGroups(); };
            _maximumMissing.Enabled = _filterMissingness.Checked;
            RefreshColumns(); RefreshGroups(); UpdateEnabled();

            var ok = new Button { Text = "OK", Left = 415, Top = 540, Width = 75, DialogResult = DialogResult.OK }; var cancel = new Button { Text = "Cancel", Left = 500, Top = 540, Width = 75, DialogResult = DialogResult.Cancel };
            ok.Click += delegate(object sender, EventArgs e) { ValidateSelection(); };
            Controls.AddRange(new Control[] { note, _associate, _proteomes, _reorder, _manifest, _browse, _header, _rename, columnLabel, _nameColumns, _importAnnotations, _filterMissingness, missingLabel, _maximumMissing, _missingnessOnly, scopeLabel, _scope, groupColumnLabel, _groupColumns, selectedGroupLabel, _selectedGroups, _excludeUnannotated, ok, cancel }); AcceptButton = ok; CancelButton = cancel;
        }
        public bool Associate { get { return _associate.Checked; } }
        public ProteomeChoice Selected { get { return _proteomes.SelectedItem as ProteomeChoice; } }
        public bool Reorder { get { return _reorder.Checked; } }
        public string ManifestPath { get { return _manifest.Text.Trim(); } }
        public bool HasHeader { get { return _header.Checked; } }
        public bool Rename { get { return _rename.Checked; } }
        public int NameColumn { get { var selected = _nameColumns.SelectedItem as ColumnChoice; return selected == null ? 0 : selected.Number; } }
        public bool FilterMissingness { get { return _filterMissingness.Checked; } }
        public int MaximumMissingPercent { get { return Decimal.ToInt32(_maximumMissing.Value); } }
        public bool MissingnessOnly { get { return _missingnessOnly.Checked; } }
        public string MissingnessScope { get { var selected = _scope.SelectedItem as ScopeChoice; return selected == null ? "all" : selected.Value; } }
        public int GroupColumn { get { var selected = _groupColumns.SelectedItem as ColumnChoice; return selected == null ? 0 : selected.Number; } }
        public string SelectedGroup { get { return _selectedGroups.SelectedItem as string; } }
        public bool ExcludeUnannotated { get { return _excludeUnannotated.Checked; } }
        public bool ImportAnnotations { get { return _importAnnotations.Checked; } }
        private void UpdateEnabled()
        {
            var grouped = _filterMissingness.Checked && MissingnessScope != "all"; var metadata = _reorder.Checked || grouped || _importAnnotations.Checked;
            _manifest.Enabled = metadata; _browse.Enabled = metadata; _header.Enabled = metadata; _rename.Enabled = _reorder.Checked; _nameColumns.Enabled = _reorder.Checked && _rename.Checked;
            _maximumMissing.Enabled = _filterMissingness.Checked; _missingnessOnly.Enabled = _filterMissingness.Checked; _scope.Enabled = _filterMissingness.Checked;
            _groupColumns.Enabled = grouped; _selectedGroups.Enabled = grouped && MissingnessScope == "selected"; _excludeUnannotated.Enabled = grouped;
        }
        private void RefreshColumns()
        {
            var selected = NameColumn > 0 ? NameColumn : _priorColumn; var selectedGroupColumn = GroupColumn > 0 ? GroupColumn : _priorGroupColumn; _nameColumns.Items.Clear(); _groupColumns.Items.Clear();
            var count = Math.Max(2, Math.Max(selected, selectedGroupColumn)); string[] header = null;
            if (File.Exists(ManifestPath)) try { var table = global::SkylineNModFilter.DelimitedMetadataReader.Read(ManifestPath, _header.Checked); header = table.Header; count = Math.Max(count, header == null ? table.Rows.Select(r => r.Length).DefaultIfEmpty(0).Max() : header.Length); } catch { }
            for (var i = 1; i <= count; i++) { var label = header != null && i <= header.Length ? (header[i - 1] ?? string.Empty).Trim() : string.Empty; _nameColumns.Items.Add(new ColumnChoice(i, label)); _groupColumns.Items.Add(new ColumnChoice(i, label)); }
            var choice = _nameColumns.Items.Cast<ColumnChoice>().FirstOrDefault(c => c.Number == selected); if (choice != null) _nameColumns.SelectedItem = choice; else if (_nameColumns.Items.Count > 1) _nameColumns.SelectedIndex = 1;
            var groupChoice = _groupColumns.Items.Cast<ColumnChoice>().FirstOrDefault(c => c.Number == selectedGroupColumn); if (groupChoice != null) _groupColumns.SelectedItem = groupChoice; else if (_groupColumns.Items.Count > 1) _groupColumns.SelectedIndex = 1;
            RefreshGroups();
        }
        private void RefreshGroups()
        {
            var prior = SelectedGroup ?? _priorSelectedGroup; var groups = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (File.Exists(ManifestPath) && GroupColumn > 0) try
            {
                var table = global::SkylineNModFilter.DelimitedMetadataReader.Read(ManifestPath, _header.Checked);
                foreach (var row in table.Rows)
                {
                    var value = row.Length >= GroupColumn ? (row[GroupColumn - 1] ?? string.Empty).Trim().Trim('"').Trim() : string.Empty;
                    if (value.Length == 0) value = "Unannotated"; if (!groups.ContainsKey(value)) groups.Add(value, value);
                }
            } catch { }
            if (!groups.ContainsKey("Unannotated")) groups.Add("Unannotated", "Unannotated");
            _selectedGroups.Items.Clear(); foreach (var group in groups.Values) _selectedGroups.Items.Add(group);
            var selected = _selectedGroups.Items.Cast<string>().FirstOrDefault(g => string.Equals(g, prior, StringComparison.OrdinalIgnoreCase)); if (selected != null) _selectedGroups.SelectedItem = selected; else if (_selectedGroups.Items.Count > 0) _selectedGroups.SelectedIndex = 0;
        }
        private void ValidateSelection()
        {
            if (_associate.Checked && Selected == null) { Reject("Select an installed background proteome."); return; }
            if (_associate.Checked && !File.Exists(Selected.FilePath)) { Reject("The selected .protdb file cannot be read:\n" + Selected.FilePath); return; }
            var grouped = _filterMissingness.Checked && MissingnessScope != "all"; var metadata = _reorder.Checked || grouped || _importAnnotations.Checked;
            if (!metadata) return;
            if (!File.Exists(ManifestPath)) { Reject("The selected metadata file cannot be read:\n" + ManifestPath); return; }
            try { global::SkylineNModFilter.DelimitedMetadataReader.Read(ManifestPath, _header.Checked); } catch (Exception exception) { Reject(exception.Message); return; }
            if (_importAnnotations.Checked && !_header.Checked) { Reject("Replicate annotation import requires a metadata header row."); return; }
            if (_rename.Checked && NameColumn < 2) Reject("Select a name column of 2 or greater.");
            if (grouped && GroupColumn < 2) { Reject("Select a group column of 2 or greater."); return; }
            if (grouped && MissingnessScope == "selected" && string.IsNullOrWhiteSpace(SelectedGroup)) { Reject("Select a metadata group."); return; }
            if (grouped && MissingnessScope == "selected" && _excludeUnannotated.Checked && string.Equals(SelectedGroup, "Unannotated", StringComparison.OrdinalIgnoreCase)) Reject("Unannotated cannot be selected while unannotated replicates are excluded.");
        }
        private void Reject(string message) { MessageBox.Show(message, "N-Mod Filter", MessageBoxButtons.OK, MessageBoxIcon.Warning); DialogResult = DialogResult.None; }
        private static bool Has(string[] args, string key) { return args != null && args.Contains(key); }
        private static string ValueAfter(string[] args, string key) { if (args == null) return null; for (var i = 0; i + 1 < args.Length; i++) if (args[i] == key) return args[i + 1]; return null; }
    }

    internal sealed class ColumnChoice
    {
        public ColumnChoice(int number, string label) { Number = number; Label = label; }
        public int Number { get; private set; }
        public string Label { get; private set; }
        public override string ToString() { return string.IsNullOrWhiteSpace(Label) ? "Column " + Number : "Column " + Number + " - " + Label; }
    }

    internal sealed class ScopeChoice
    {
        public ScopeChoice(string value, string label) { Value = value; Label = label; }
        public string Value { get; private set; }
        public string Label { get; private set; }
        public override string ToString() { return Label; }
    }
}
