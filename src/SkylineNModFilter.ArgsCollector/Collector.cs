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
                    if (!form.Associate) return new string[0];
                    return new[] { "--associate-proteins", "--background-proteome-file", form.Selected.FilePath, "--background-proteome-name", form.Selected.Name };
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
        public OptionsForm(IList<ProteomeChoice> choices, string[] oldArguments)
        {
            Text = "N-Mod Filter Options"; Width = 520; Height = 190; FormBorderStyle = FormBorderStyle.FixedDialog; StartPosition = FormStartPosition.CenterParent; MaximizeBox = false; MinimizeBox = false;
            var note = new Label { Left = 16, Top = 15, Width = 470, Text = "Modification display will be set to Three-letter code." };
            _associate.Left = 16; _associate.Top = 45; _associate.Width = 200; _associate.Text = "Associate proteins";
            _proteomes.Left = 35; _proteomes.Top = 75; _proteomes.Width = 440; _proteomes.DropDownStyle = ComboBoxStyle.DropDownList;
            foreach (var choice in choices) _proteomes.Items.Add(choice);
            var priorName = ValueAfter(oldArguments, "--background-proteome-name");
            var prior = choices.FirstOrDefault(c => c.Name == priorName); if (prior != null) _proteomes.SelectedItem = prior; else if (_proteomes.Items.Count > 0) _proteomes.SelectedIndex = 0;
            _associate.Checked = oldArguments != null && oldArguments.Contains("--associate-proteins");
            _associate.Enabled = choices.Count > 0; _proteomes.Enabled = _associate.Checked && choices.Count > 0; _associate.CheckedChanged += delegate { _proteomes.Enabled = _associate.Checked; };
            var ok = new Button { Text = "OK", Left = 315, Top = 112, Width = 75, DialogResult = DialogResult.OK }; var cancel = new Button { Text = "Cancel", Left = 400, Top = 112, Width = 75, DialogResult = DialogResult.Cancel };
            ok.Click += delegate(object sender, EventArgs e) { if (_associate.Checked && Selected == null) { MessageBox.Show("Select an installed background proteome."); DialogResult = DialogResult.None; } else if (_associate.Checked && !File.Exists(Selected.FilePath)) { MessageBox.Show("The selected .protdb file cannot be read:\n" + Selected.FilePath); DialogResult = DialogResult.None; } };
            Controls.AddRange(new Control[] { note, _associate, _proteomes, ok, cancel }); AcceptButton = ok; CancelButton = cancel;
        }
        public bool Associate { get { return _associate.Checked; } }
        public ProteomeChoice Selected { get { return _proteomes.SelectedItem as ProteomeChoice; } }
        private static string ValueAfter(string[] args, string key) { if (args == null) return null; for (var i = 0; i + 1 < args.Length; i++) if (args[i] == key) return args[i + 1]; return null; }
    }
}
