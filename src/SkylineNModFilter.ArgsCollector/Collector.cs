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
                        form.Reorder, form.ManifestPath, form.HasHeader, form.Rename, form.NameColumn);
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
        private readonly int _priorColumn;
        public OptionsForm(IList<ProteomeChoice> choices, string[] oldArguments)
        {
            Text = "N-Mod Filter Options"; Width = 620; Height = 365; FormBorderStyle = FormBorderStyle.FixedDialog; StartPosition = FormStartPosition.CenterParent; MaximizeBox = false; MinimizeBox = false;
            var note = new Label { Left = 16, Top = 15, Width = 570, Text = "Modification display will be set to Three-letter code." };
            _associate.Left = 16; _associate.Top = 45; _associate.Width = 200; _associate.Text = "Associate proteins";
            _proteomes.Left = 35; _proteomes.Top = 72; _proteomes.Width = 540; _proteomes.DropDownStyle = ComboBoxStyle.DropDownList;
            foreach (var choice in choices) _proteomes.Items.Add(choice);
            var priorName = ValueAfter(oldArguments, "--background-proteome-name");
            var prior = choices.FirstOrDefault(c => c.Name == priorName); if (prior != null) _proteomes.SelectedItem = prior; else if (_proteomes.Items.Count > 0) _proteomes.SelectedIndex = 0;
            _associate.Checked = oldArguments != null && oldArguments.Contains("--associate-proteins");
            _associate.Enabled = choices.Count > 0; _proteomes.Enabled = _associate.Checked && choices.Count > 0; _associate.CheckedChanged += delegate { _proteomes.Enabled = _associate.Checked; };

            _reorder.SetBounds(16, 112, 280, 24); _reorder.Text = "Reorder replicates from metadata file";
            _manifest.SetBounds(35, 142, 455, 24); _browse.SetBounds(500, 140, 75, 26); _browse.Text = "Browse...";
            _header.SetBounds(35, 174, 200, 24); _header.Text = "File contains header row";
            _rename.SetBounds(35, 204, 200, 24); _rename.Text = "Rename matched replicates";
            var columnLabel = new Label { Left = 55, Top = 238, Width = 90, Text = "Name column:" };
            _nameColumns.SetBounds(145, 234, 430, 26); _nameColumns.DropDownStyle = ComboBoxStyle.DropDownList;
            _reorder.Checked = Has(oldArguments, "--reorder-replicates"); _manifest.Text = ValueAfter(oldArguments, "--replicate-manifest") ?? string.Empty;
            _header.Checked = Has(oldArguments, "--manifest-has-header"); _rename.Checked = Has(oldArguments, "--rename-replicates");
            int parsedColumn; _priorColumn = int.TryParse(ValueAfter(oldArguments, "--replicate-name-column"), out parsedColumn) ? parsedColumn : 2;
            _browse.Click += delegate { using (var picker = new OpenFileDialog { Filter = "Metadata files (*.fp-manifest;*.tsv;*.csv)|*.fp-manifest;*.tsv;*.csv|All files (*.*)|*.*" }) { if (picker.ShowDialog(this) == DialogResult.OK) { _manifest.Text = picker.FileName; RefreshColumns(); } } };
            _header.CheckedChanged += delegate { RefreshColumns(); }; _rename.CheckedChanged += delegate { UpdateEnabled(); }; _reorder.CheckedChanged += delegate { UpdateEnabled(); };
            _manifest.Leave += delegate { RefreshColumns(); };
            RefreshColumns(); UpdateEnabled();

            var ok = new Button { Text = "OK", Left = 415, Top = 282, Width = 75, DialogResult = DialogResult.OK }; var cancel = new Button { Text = "Cancel", Left = 500, Top = 282, Width = 75, DialogResult = DialogResult.Cancel };
            ok.Click += delegate(object sender, EventArgs e) { ValidateSelection(); };
            Controls.AddRange(new Control[] { note, _associate, _proteomes, _reorder, _manifest, _browse, _header, _rename, columnLabel, _nameColumns, ok, cancel }); AcceptButton = ok; CancelButton = cancel;
        }
        public bool Associate { get { return _associate.Checked; } }
        public ProteomeChoice Selected { get { return _proteomes.SelectedItem as ProteomeChoice; } }
        public bool Reorder { get { return _reorder.Checked; } }
        public string ManifestPath { get { return _manifest.Text.Trim(); } }
        public bool HasHeader { get { return _header.Checked; } }
        public bool Rename { get { return _rename.Checked; } }
        public int NameColumn { get { var selected = _nameColumns.SelectedItem as ColumnChoice; return selected == null ? 0 : selected.Number; } }
        private void UpdateEnabled() { var enabled = _reorder.Checked; _manifest.Enabled = enabled; _browse.Enabled = enabled; _header.Enabled = enabled; _rename.Enabled = enabled; _nameColumns.Enabled = enabled && _rename.Checked; }
        private void RefreshColumns()
        {
            var selected = NameColumn > 0 ? NameColumn : _priorColumn; _nameColumns.Items.Clear();
            var count = Math.Max(2, selected); string[] header = null;
            if (File.Exists(ManifestPath)) try { var table = global::SkylineNModFilter.DelimitedMetadataReader.Read(ManifestPath, _header.Checked); header = table.Header; count = Math.Max(count, header == null ? table.Rows.Select(r => r.Length).DefaultIfEmpty(0).Max() : header.Length); } catch { }
            for (var i = 1; i <= count; i++) { var label = header != null && i <= header.Length ? (header[i - 1] ?? string.Empty).Trim() : string.Empty; _nameColumns.Items.Add(new ColumnChoice(i, label)); }
            var choice = _nameColumns.Items.Cast<ColumnChoice>().FirstOrDefault(c => c.Number == selected); if (choice != null) _nameColumns.SelectedItem = choice; else if (_nameColumns.Items.Count > 1) _nameColumns.SelectedIndex = 1;
        }
        private void ValidateSelection()
        {
            if (_associate.Checked && Selected == null) { Reject("Select an installed background proteome."); return; }
            if (_associate.Checked && !File.Exists(Selected.FilePath)) { Reject("The selected .protdb file cannot be read:\n" + Selected.FilePath); return; }
            if (!_reorder.Checked) return;
            if (!File.Exists(ManifestPath)) { Reject("The selected metadata file cannot be read:\n" + ManifestPath); return; }
            try { global::SkylineNModFilter.DelimitedMetadataReader.Read(ManifestPath, _header.Checked); } catch (Exception exception) { Reject(exception.Message); return; }
            if (_rename.Checked && NameColumn < 2) Reject("Select a name column of 2 or greater.");
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
}
