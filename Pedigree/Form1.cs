//Copyright (c) November 6, 2026 Jeremiah Burke O'Neal
//
//Permission is hereby granted, free of charge, to any person obtaining a copy
//of this software and associated documentation files (the "Software"), to deal
//in the Software without restriction, including without limitation the rights
//to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//copies of the Software, and to permit persons to whom the Software is
//furnished to do so, subject to the following conditions:
//
//The above copyright notice and this permission notice shall be included in all
//copies or substantial portions of the Software.

//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
//SOFTWARE.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Windows.Forms;

namespace Pedigree
{
    public partial class Form1 : Form
    {
        // The original imported JSON is retained as a tree. Only explicit new records/links are appended.
        // We don't flatten the source to a new schema or discard unknown metadata.
        private JsonObject? _document;
        private string? _currentPath;
        private readonly List<PersonRow> _people = new();
        private sealed record PersonRow(string Name, string Tg, string Details)
        {
            public override string ToString() => string.IsNullOrWhiteSpace(Tg) ? Name : $"{Name} ({Tg})";
        }

        public Form1() => InitializeComponent();

        private void Form1_Load(object sender, EventArgs e)
        {
            BackColor = Color.FromArgb(245, 249, 255);
            headerPanel.BackColor = Color.FromArgb(218, 232, 252);
            footerPanel.BackColor = Color.FromArgb(235, 242, 250);
            memberPanel.BackColor = detailsPanel.BackColor = exportsPanel.BackColor = Color.White;
            titleLabel.ForeColor = Color.FromArgb(41, 74, 122);
            subtitleLabel.ForeColor = Color.FromArgb(71, 94, 128);
            foreach (var button in new[] { addPersonButton, connectButton, importButton, exportPdfButton, exportJsonButton, aboutButton })
            {
                button.BackColor = Color.FromArgb(225, 236, 252);
                button.FlatStyle = FlatStyle.Flat;
                button.FlatAppearance.BorderColor = Color.FromArgb(162, 189, 228);
            }
            exportStyleTextBox.Text = "CLASSROOM POSTER STYLE\r\n" +
                "Parents above • Selected person in the center • Children below\r\n" +
                "Each card includes a name and TG identifier. A fact box holds original notes.\r\n" +
                "Unverified research is never silently presented as proven.\r\n" +
                "The first 128 family members are written in C# source. Import JSON adds relatives.\r\n" +
                "Export PDF creates a one-page landscape family project poster.";
            // Build the entire original family tree from C# statements.
            // No PS1 installer, external JSON, or resource file is needed.
            try { ReadBuiltInDocument(); }
            catch (Exception ex)
            {
                UpdateStatus("Could not load built-in family members: " + ex.Message);
                MessageBox.Show(this, ex.Message, "Built-in family tree error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static JsonArray EnsureArray(JsonObject document, string key)
        {
            if (document[key] is JsonArray array) return array;
            throw new InvalidDataException($"Expected a JSON array named '{key}'. The file was not modified.");
        }
        private static string Value(JsonNode? node, string key) => node?[key]?.ToString() ?? "";
        private JsonArray PeopleArray => EnsureArray(_document!, "people");
        private JsonArray IdentifierArray => EnsureArray(_document!, "identifiers");
        private JsonArray RelationshipArray => EnsureArray(_document!, "relationships");

        private void ReadBuiltInDocument()
        {
            // Every member of the original 128-person family tree is encoded in
            // BuiltInFamilyData.cs along with every associated data section.
            // Each run recreates a fresh in-memory copy without touching the C# source.
            JsonObject loaded = BuiltInFamilyData.Create();
            foreach (string name in new[] { "people", "identifiers", "relationships" })
                EnsureArray(loaded, name);
            if (EnsureArray(loaded, "people").Count != 128)
                throw new InvalidDataException("Built-in tree must contain 128 original people.");
            _document = loaded;
            _currentPath = null;
            RefreshPeople();
            UpdateStatus("128 original family members loaded from C# source code. Import JSON to add more family members.");
        }

        private void ReadDocument(string path)
        {
            // IMPORT IS ADDITIVE: never replace or delete any built-in person.
            // A later exported JSON containing 129 or 130 people can add records.
            JsonObject incoming = JsonNode.Parse(File.ReadAllText(path)) as JsonObject
                ?? throw new InvalidDataException("The selected file must contain a JSON object.");
            foreach (string name in new[] { "people", "identifiers", "relationships" })
                EnsureArray(incoming, name);
            if (_document == null) ReadBuiltInDocument();
            int peopleAdded = 0, identifiersAdded = 0, linksAdded = 0;
            var localPeople = PeopleArray;
            var localIdentifiers = IdentifierArray;
            var localRelationships = RelationshipArray;
            var existingNames = localPeople.Select(p => Value(p, "display_name"))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            var knownCodes = localIdentifiers.Select(p => Value(p, "code"))
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            var importedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (JsonNode? p in EnsureArray(incoming, "people"))
            {
                string name = Value(p, "display_name");
                if (string.IsNullOrWhiteSpace(name) || existingNames.Contains(name)) continue;
                localPeople.Add(p!.DeepClone());
                importedNames.Add(name);
                existingNames.Add(name);
                peopleAdded++;
            }
            // Only import TG codes for new names. Never change a built-in TG number.
            foreach (JsonNode? id in EnsureArray(incoming, "identifiers"))
            {
                string name = Value(id, "display_name"), code = Value(id, "code");
                if (!importedNames.Contains(name) || string.IsNullOrWhiteSpace(code) || knownCodes.Contains(code)) continue;
                localIdentifiers.Add(id!.DeepClone());
                knownCodes.Add(code);
                identifiersAdded++;
            }
            // If an imported newcomer has no unique TG number, allocate TG20####.
            foreach (string name in importedNames)
            {
                if (localIdentifiers.Any(id => Value(id, "display_name").Equals(name, StringComparison.OrdinalIgnoreCase)
                    && Value(id, "kind") == "TG")) continue;
                string tg = UniqueTg();
                localIdentifiers.Add(new JsonObject
                {
                    ["display_name"] = name, ["code"] = tg, ["kind"] = "TG", ["permanent"] = 0
                });
                knownCodes.Add(tg);
                identifiersAdded++;
            }
            var knownRelations = localRelationships.Select(r =>
                (Value(r, "parent"), Value(r, "child"), Value(r, "relation")))
                .ToHashSet();
            foreach (JsonNode? rel in EnsureArray(incoming, "relationships"))
            {
                string parent = Value(rel, "parent"), child = Value(rel, "child"), type = Value(rel, "relation");
                var key = (parent, child, type);
                if (string.IsNullOrWhiteSpace(parent) || string.IsNullOrWhiteSpace(child) ||
                    !existingNames.Contains(parent) || !existingNames.Contains(child) || knownRelations.Contains(key)) continue;
                localRelationships.Add(rel!.DeepClone());
                knownRelations.Add(key);
                linksAdded++;
            }
            // Preserve any unknown metadata in the current tree without changing it.
            // Imported supplemental records are only added when not already present.
            foreach (var section in incoming)
            {
                if (section.Key is "people" or "identifiers" or "relationships") continue;
                if (section.Value is not JsonArray importedArray) continue;
                // New optional arrays (e.g. spouse_relationships/person_notes) may not exist in
                // the original 128-person source. They are added only as supplemental sections.
                if (_document![section.Key] is not JsonArray)
                    _document[section.Key] = new JsonArray();
                var currentArray = (JsonArray)_document[section.Key]!;
                var fingerprints = currentArray.Select(x => x?.ToJsonString() ?? "null")
                    .ToHashSet(StringComparer.Ordinal);
                foreach (JsonNode? record in importedArray)
                {
                    string fingerprint = record?.ToJsonString() ?? "null";
                    if (!fingerprints.Add(fingerprint)) continue;
                    currentArray.Add(record?.DeepClone());
                }
            }
            RefreshPeople();
            UpdateStatus($"Imported {peopleAdded} new relatives, {identifiersAdded} identifiers, " +
                $"and {linksAdded} relationships. Now {_people.Count} people. Export JSON to save a copy.");
            MessageBox.Show(this,
                $"Added {peopleAdded} family members, {identifiersAdded} identifiers, and {linksAdded} relationships.\n" +
                $"The original 128 built-in family members were not changed.",
                "Family members imported", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void RefreshPeople(string? selectName = null)
        {
            _people.Clear();
            if (_document != null)
            {
                foreach (JsonNode? p in PeopleArray)
                {
                    string name = Value(p, "display_name");
                    if (string.IsNullOrWhiteSpace(name)) continue;
                    string tg = IdentifierArray
                        .Where(id => Value(id, "display_name") == name && Value(id, "kind") == "TG")
                        .Select(id => Value(id, "code")).FirstOrDefault() ?? "";
                    _people.Add(new PersonRow(name, tg, Value(p, "details")));
                }
            }
            familyListBox.BeginUpdate();
            familyListBox.Items.Clear();
            foreach (PersonRow p in _people.Where(p => string.IsNullOrWhiteSpace(searchTextBox.Text) ||
                p.Name.Contains(searchTextBox.Text, StringComparison.OrdinalIgnoreCase) ||
                p.Tg.Contains(searchTextBox.Text, StringComparison.OrdinalIgnoreCase))) familyListBox.Items.Add(p);
            familyListBox.EndUpdate();
            if (familyListBox.Items.Count > 0)
            {
                int idx = 0;
                if (selectName != null)
                    for (int i = 0; i < familyListBox.Items.Count; i++)
                        if (((PersonRow)familyListBox.Items[i]).Name == selectName) { idx = i; break; }
                familyListBox.SelectedIndex = idx;
            }
            else ClearPerson();
        }
        private void ClearPerson()
        {
            nameValueLabel.Text = tgValueLabel.Text = birthValueLabel.Text = deathValueLabel.Text =
                parentsValueLabel.Text = spouseValueLabel.Text = childrenValueLabel.Text = funFactTextBox.Text = "";
        }
        private IEnumerable<string> ParentsOf(string name) => RelationshipArray
            .Where(r => Value(r, "child") == name && Value(r, "relation") == "parent")
            .Select(r => Value(r, "parent")).Where(s => s.Length > 0).Distinct();
        private IEnumerable<string> ChildrenOf(string name) => RelationshipArray
            .Where(r => Value(r, "parent") == name && Value(r, "relation") == "parent")
            .Select(r => Value(r, "child")).Where(s => s.Length > 0).Distinct();
        private static string ListOrUnknown(IEnumerable<string> values) =>
            values.Any() ? string.Join(", ", values) : "Not recorded";
        private IEnumerable<string> CoparentsOf(string name) => ChildrenOf(name)
            .SelectMany(child => ParentsOf(child).Where(parent => parent != name)).Distinct();
        private JsonArray SupplementalArray(string section)
        {
            if (_document == null) throw new InvalidOperationException("Load the family tree first.");
            if (_document[section] is JsonArray existing) return existing;
            _document[section] = new JsonArray();
            return (JsonArray)_document[section]!;
        }
        private IEnumerable<string> SpousesOf(string name)
        {
            if (_document?["spouse_relationships"] is not JsonArray spouses)
                return Array.Empty<string>();
            return spouses.Where(r => Value(r, "person1") == name || Value(r, "person2") == name)
                .Select(r => Value(r, "person1") == name ? Value(r, "person2") : Value(r, "person1"))
                .Where(other => !string.IsNullOrWhiteSpace(other)).Distinct(StringComparer.OrdinalIgnoreCase);
        }
        private string NotesOf(string name)
        {
            if (_document?["person_notes"] is not JsonArray notes) return "";
            return string.Join(Environment.NewLine, notes.Where(n => Value(n, "display_name") == name)
                .Select(n => Value(n, "note")));
        }
        private void AddPersonNote(string person)
        {
            string? note = AskText(this, "Add Family Details", $"Add a note for {person} (birthplace, dates, story, or research):");
            if (string.IsNullOrWhiteSpace(note)) return;
            SupplementalArray("person_notes").Add(new JsonObject
            {
                ["display_name"] = person, ["note"] = note, ["source"] = "user-entered",
                ["verified"] = false
            });
            RefreshPeople(person);
            UpdateStatus("Added a family note. Export JSON to save the addition to C:\\tree.");
        }
        private void AddSpouseConnection(string first, string second)
        {
            if (first.Equals(second, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("A person cannot be connected as their own spouse.");
            var spouses = SupplementalArray("spouse_relationships");
            if (spouses.Any(r =>
                (Value(r, "person1") == first && Value(r, "person2") == second) ||
                (Value(r, "person1") == second && Value(r, "person2") == first)))
                throw new InvalidOperationException("This spouse relationship already exists.");
            spouses.Add(new JsonObject
            {
                ["person1"] = first, ["person2"] = second, ["relation"] = "spouse",
                ["source"] = "user-entered", ["permanent"] = 0
            });
        }

        private string? Provisional(string name, string field)
        {
            if (_document?["provisional_facts"] is not JsonArray facts) return null;
            return facts.Where(f => Value(f, "display_name") == name && Value(f, "field") == field && Value(f, "status") == "active")
                .Select(f => Value(f, "value")).FirstOrDefault(v => !string.IsNullOrWhiteSpace(v));
        }

        private void familyListBox_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (familyListBox.SelectedItem is not PersonRow person) { ClearPerson(); return; }
            nameValueLabel.Text = person.Name;
            tgValueLabel.Text = string.IsNullOrEmpty(person.Tg) ? "Unassigned" : person.Tg;
            birthValueLabel.Text = Provisional(person.Name, "birth_date") is string bd ? bd + " (unverified)" : "See original notes";
            deathValueLabel.Text = Provisional(person.Name, "death_date") is string dd ? dd + " (unverified)" : "See original notes";
            parentsValueLabel.Text = ListOrUnknown(ParentsOf(person.Name));
            spouseValueLabel.Text = ListOrUnknown(SpousesOf(person.Name));
            if (CoparentsOf(person.Name).Any())
                spouseValueLabel.Text += "  |  Other parent(s): " + ListOrUnknown(CoparentsOf(person.Name)) + " (not proof of marriage)";
            childrenValueLabel.Text = ListOrUnknown(ChildrenOf(person.Name));
            string notes = NotesOf(person.Name);
            funFactTextBox.Text = string.Join(Environment.NewLine, new[] { person.Details, notes }
                .Where(x => !string.IsNullOrWhiteSpace(x)));
            if (string.IsNullOrWhiteSpace(funFactTextBox.Text)) funFactTextBox.Text = "No notes recorded.";
            UpdateStatus($"Showing {person.Name}. Birth/death field suggestions are labeled unverified.");
        }

        private void searchTextBox_TextChanged(object? sender, EventArgs e) => RefreshPeople();
        private void UpdateStatus(string message) => statusLabel.Text = message;

        private static string TreeFolder()
        {
            // Keep all user-selected imports and exports in C:\tree by default.
            // No installers or external files are required for the original 128 members or resume.
            string path = @"C:\Users\161119\source\repos\John-McCabe-et-al-628417\";
            Directory.CreateDirectory(path);
            return path;
        }

        private void importButton_Click(object? sender, EventArgs e)
        {
            using var dialog = new OpenFileDialog { Title = "Open your family tree", Filter = "Family tree JSON (*.json)|*.json|All files (*.*)|*.*" };
            dialog.InitialDirectory = TreeFolder();
            if (dialog.ShowDialog(this) != DialogResult.OK) return;
            try { ReadDocument(dialog.FileName); }
            catch (Exception ex) { MessageBox.Show(this, ex.Message, "Could not open family tree", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        private static string? AskText(IWin32Window owner, string heading, string label)
        {
            using var dialog = new Form { Text = heading, Width = 510, Height = 215, StartPosition = FormStartPosition.CenterParent, FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false };
            var prompt = new Label { Text = label, Left = 20, Top = 17, Width = 460, Height = 45 };
            var entry = new TextBox { Left = 20, Top = 70, Width = 450 };
            var ok = new Button { Text = "Save", DialogResult = DialogResult.OK, Left = 285, Top = 115, Width = 85 };
            var cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Left = 380, Top = 115, Width = 85 };
            dialog.Controls.AddRange(new Control[] { prompt, entry, ok, cancel });
            dialog.AcceptButton = ok; dialog.CancelButton = cancel;
            return dialog.ShowDialog(owner) == DialogResult.OK ? entry.Text.Trim() : null;
        }
        private string UniqueTg()
        {
            var used = IdentifierArray.Select(x => Value(x, "code")).ToHashSet(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < 10000; i++)
            {
                string code = "TG20" + RandomNumberGenerator.GetInt32(0, 10000).ToString("D4");
                if (!used.Contains(code)) return code;
            }
            throw new InvalidOperationException("No TG20 identifiers remain available.");
        }
        private void addPersonButton_Click(object? sender, EventArgs e)
        {
            if (_document == null) { MessageBox.Show(this, "The built-in family tree could not be loaded."); return; }
            string? name = AskText(this, "Add Family Member", "Enter the person's full name (as it should appear on the tree):");
            if (name == null) return;
            if (string.IsNullOrWhiteSpace(name)) { MessageBox.Show(this, "A name is required."); return; }
            if (_people.Any(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase)))
            { MessageBox.Show(this, "That name already exists. To avoid mixing two people together, use a distinguishing full name."); return; }
            int nextId = PeopleArray.Select(p => int.TryParse(Value(p, "person_no"), out int n) ? n : 0).DefaultIfEmpty(0).Max() + 1;
            string tg = UniqueTg();
            PeopleArray.Add(new JsonObject { ["person_no"] = nextId, ["display_name"] = name, ["details"] = "", ["name_permanent"] = 0 });
            IdentifierArray.Add(new JsonObject { ["display_name"] = name, ["code"] = tg, ["kind"] = "TG", ["permanent"] = 0 });
            RefreshPeople(name);
            UpdateStatus($"Added {name} ({tg}) to memory. Use Export JSON to save a new file.");
        }

        private void connectButton_Click(object? sender, EventArgs e)
        {
            if (_document == null || familyListBox.SelectedItem is not PersonRow selected)
            { MessageBox.Show(this, "Select a family member first."); return; }
            using var dialog = new Form { Text = "Connect Family Members", Width = 540, Height = 300, StartPosition = FormStartPosition.CenterParent, FormBorderStyle = FormBorderStyle.FixedDialog };
            var header = new Label { Left = 20, Top = 16, Width = 490, Height = 42, Text = $"Selected person: {selected.Name}\nChoose a second person and how they connect." };
            var combo = new ComboBox { Left = 20, Top = 77, Width = 470, DropDownStyle = ComboBoxStyle.DropDownList };
            foreach (var p in _people.Where(p => p.Name != selected.Name)) combo.Items.Add(p);
            if (combo.Items.Count > 0) combo.SelectedIndex = 0;
            var kind = new ComboBox { Left = 20, Top = 123, Width = 470, DropDownStyle = ComboBoxStyle.DropDownList };
            kind.Items.AddRange(new object[] { "Second person is a parent of the selected person", "Selected person is a parent of the second person", "These two people are spouses" });
            kind.SelectedIndex = 0;
            var save = new Button { Text = "Add connection", Left = 280, Top = 185, Width = 130, DialogResult = DialogResult.OK };
            var cancel = new Button { Text = "Cancel", Left = 415, Top = 185, Width = 78, DialogResult = DialogResult.Cancel };
            dialog.Controls.AddRange(new Control[] { header, combo, kind, save, cancel }); dialog.AcceptButton = save;
            if (dialog.ShowDialog(this) != DialogResult.OK || combo.SelectedItem is not PersonRow second) return;
            if (kind.SelectedIndex == 2)
            {
                try
                {
                    AddSpouseConnection(selected.Name, second.Name);
                    RefreshPeople(selected.Name);
                    UpdateStatus("Spouse connection added. Export JSON to save it to C:\\tree.");
                }
                catch (Exception ex) { MessageBox.Show(this, ex.Message, "Could not add spouse", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
                return;
            }
            string parent = kind.SelectedIndex == 0 ? second.Name : selected.Name;
            string child = kind.SelectedIndex == 0 ? selected.Name : second.Name;
            bool exists = RelationshipArray.Any(r => Value(r, "child") == child && Value(r, "parent") == parent && Value(r, "relation") == "parent");
            if (exists) { MessageBox.Show(this, "That connection already exists."); return; }
            // Prevent cycles of any length. A parent cannot already descend from the child.
            bool IsAncestor(string possibleAncestor, string descendant)
            {
                var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var pending = new Stack<string>();
                pending.Push(descendant);
                while (pending.Count > 0)
                {
                    string current = pending.Pop();
                    if (!visited.Add(current)) continue;
                    if (current.Equals(possibleAncestor, StringComparison.OrdinalIgnoreCase)) return true;
                    foreach (string p in ParentsOf(current)) pending.Push(p);
                }
                return false;
            }
            if (IsAncestor(child, parent)) { MessageBox.Show(this, "That connection would create a family-tree cycle."); return; }
            RelationshipArray.Add(new JsonObject { ["child"] = child, ["parent"] = parent, ["relation"] = "parent", ["confidence"] = 1.0, ["permanent"] = 0 });
            RefreshPeople(selected.Name);
            UpdateStatus($"Added: {parent} is a parent of {child}. Export JSON to save this change.");
        }

        private void exportJsonButton_Click(object? sender, EventArgs e)
        {
            if (_document == null) { MessageBox.Show(this, "The built-in family tree could not be loaded."); return; }
            using var dialog = new SaveFileDialog { Title = "Save a copy of your family tree", Filter = "JSON files (*.json)|*.json", FileName = "my-family-tree-updated.json", OverwritePrompt = true };
            dialog.InitialDirectory = TreeFolder();
            if (dialog.ShowDialog(this) != DialogResult.OK) return;
            try
            {
                // Never silently overwrite the imported master file.
                if (_currentPath != null && string.Equals(Path.GetFullPath(dialog.FileName), Path.GetFullPath(_currentPath), StringComparison.OrdinalIgnoreCase))
                { MessageBox.Show(this, "Choose a different file name. Your imported master JSON is protected from overwriting."); return; }
                File.WriteAllText(dialog.FileName, _document.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
                UpdateStatus($"Saved a copy: {dialog.FileName}");
                MessageBox.Show(this, "Your family tree copy was saved. Review privacy before publishing this JSON to GitHub.", "Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex) { MessageBox.Show(this, ex.Message, "Save failed", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        private void exportPdfButton_Click(object? sender, EventArgs e)
        {
            if (_document == null || familyListBox.SelectedItem is not PersonRow person)
            { MessageBox.Show(this, "Select a family member first."); return; }
            using var dialog = new SaveFileDialog { Title = "Save a classroom family tree poster", Filter = "PDF documents (*.pdf)|*.pdf", FileName = "family-tree-" + (string.IsNullOrEmpty(person.Tg) ? "selected" : person.Tg) + ".pdf", OverwritePrompt = true };
            dialog.InitialDirectory = TreeFolder();
            if (dialog.ShowDialog(this) != DialogResult.OK) return;
            try
            {
                SchoolPosterPdf.Export(dialog.FileName, person.Name, person.Tg, person.Details,
                    ParentsOf(person.Name).ToList(), ChildrenOf(person.Name).ToList(),
                    CoparentsOf(person.Name).ToList());
                UpdateStatus($"Saved one-page school poster: {dialog.FileName}");
                MessageBox.Show(this, "Your family tree poster PDF was saved. Check privacy before sharing it.", "PDF ready", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex) { MessageBox.Show(this, ex.Message, "PDF export failed", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }
        // Display all data in the supplied JSON concerning the selected person.
        // This is read-only, with source labels and original JSON values shown intact.
        // Provisional research is explicitly marked as not yet verified.
        private void ShowAllPersonInformation(object? sender, EventArgs e)
        {
            if (_document == null || familyListBox.SelectedItem is not PersonRow p)
            {
                MessageBox.Show(this, "Load the family tree and choose a person first.");
                return;
            }
            string name = p.Name;
            var sections = new List<string> { "FAMILY TREE RECORD: " + name };
            void AddSection(string heading, IEnumerable<JsonNode?> records)
            {
                var entries = records.ToList();
                sections.Add("\r\n" + heading + " (" + entries.Count + ")");
                foreach (var record in entries)
                    sections.Add(record?.ToJsonString(new JsonSerializerOptions { WriteIndented = true }) ?? "null");
            }
            AddSection("Person record", PeopleArray.Where(x => Value(x, "display_name") == name));
            AddSection("Identifiers", IdentifierArray.Where(x => Value(x, "display_name") == name));
            AddSection("Family relationships", RelationshipArray.Where(x =>
                Value(x, "child") == name || Value(x, "parent") == name));
            if (_document["provisional_facts"] is JsonArray facts)
                AddSection("Provisional research facts (UNVERIFIED)", facts.Where(x => Value(x, "display_name") == name));
            if (_document["project_references"] is JsonArray refs)
            {
                var codes = IdentifierArray.Where(x => Value(x, "display_name") == name)
                    .Select(x => Value(x, "code")).ToHashSet(StringComparer.OrdinalIgnoreCase);
                AddSection("Project references", refs.Where(x => codes.Contains(Value(x, "code"))));
            }
            if (_document["person_notes"] is JsonArray personalNotes)
                AddSection("Added family notes (user-entered)", personalNotes.Where(x => Value(x, "display_name") == name));
            if (_document["spouse_relationships"] is JsonArray spouses)
                AddSection("Spouse connections", spouses.Where(x => Value(x, "person1") == name || Value(x, "person2") == name));
            using var dialog = new Form
            {
                Text = name + " - All Recorded Information", ClientSize = new Size(950, 720),
                MinimumSize = new Size(610, 450), StartPosition = FormStartPosition.CenterParent,
                Font = new Font("Segoe UI", 10F)
            };
            var notes = new TextBox
            {
                Multiline = true, ReadOnly = true, Dock = DockStyle.Fill,
                ScrollBars = ScrollBars.Both, Font = new Font("Consolas", 10F),
                WordWrap = false, Text = string.Join("\r\n\r\n", sections)
            };
            dialog.Controls.Add(notes);
            dialog.ShowDialog(this);
        }

        // About displays the entire resume from C# source code, never C:\tree\resume.txt.
        // Family changes and annotations are stored in memory and may be exported to C:\tree.
        private void aboutButton_Click(object? sender, EventArgs e)
        {
            using var window = new Form
            {
                Text = "About My Family Tree",
                StartPosition = FormStartPosition.CenterParent,
                ClientSize = new Size(900, 690),
                MinimumSize = new Size(620, 450),
                BackColor = Color.FromArgb(245, 249, 255),
                Font = new Font("Segoe UI", 10F)
            };
            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3, ColumnCount = 1, Padding = new Padding(14) };
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 72F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 52F));
            var heading = new Label
            {
                Text = "My Family Tree  |  About the Project",
                Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI", 17F, FontStyle.Bold),
                ForeColor = Color.FromArgb(41, 74, 122)
            };
            var tabs = new TabControl { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 11F) };
            var overview = new TabPage("Our Project") { BackColor = Color.White };
            var resume = new TabPage("Creator's Résumé") { BackColor = Color.White };
            var family = new TabPage("Family Members") { BackColor = Color.White };
            tabs.TabPages.AddRange(new[] { overview, resume, family });

            static TextBox ReadOnlyPage(string value) => new TextBox
            {
                Text = value, Multiline = true, ReadOnly = true, Dock = DockStyle.Fill,
                ScrollBars = ScrollBars.Both, WordWrap = true, BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", 11F), BackColor = Color.White,
                ShortcutsEnabled = true
            };

            overview.Padding = resume.Padding = family.Padding = new Padding(20);
            overview.Controls.Add(ReadOnlyPage(
                "Welcome to My Family Tree!\r\n\r\n" +
                "Discover relatives, learn about family history, and see how generations connect. " +
                "This project uses simple labels and a classroom poster layout for elementary " +
                "and middle-school learners.\r\n\r\n" +
                "Choose a family member to see their TG number and known family connections. " +
                "You can import or export JSON, add a relative, connect parents and children, " +
                "and make a one-page family tree PDF. The About window lets you add relatives, spouses, and notes.\r\n\r\n" +
                "Research notes can be provisional. Do not assume every proposed connection is verified.\r\n\r\n" +
                "Privacy: Check living relatives' details before sharing or posting an export. " +
                "This private application includes your original genealogy data in C# source. Do not publish this source or executable.\r\n\r\n" +
                "Copyright 2026 Jeremiah O'Neal. MIT / Apache-2.0 / GPL-3.0."));

            // The complete resume is compiled directly from the uploaded DOCX into
            // EmbeddedResume.cs. No file access is performed when opening About.
            resume.Controls.Add(ReadOnlyPage(EmbeddedResume.Text));

            // Family members and editing tools stay inside About. All 128 built-in
            // people remain in C#; newcomers, spouse links, and notes are supplemental.
            var familyLayout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3, ColumnCount = 1 };
            familyLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));
            familyLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            familyLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 64F));
            var familyCount = new Label { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI", 12F, FontStyle.Bold) };
            var memberList = new ListBox { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 11F) };
            void RefreshAboutMembers(string? selectedName = null)
            {
                familyCount.Text = $"{_people.Count} family members (128 built into C#)";
                memberList.Items.Clear();
                foreach (var entry in _people.OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase))
                    memberList.Items.Add(entry);
                if (selectedName != null)
                {
                    for (int i = 0; i < memberList.Items.Count; i++)
                        if (((PersonRow)memberList.Items[i]).Name == selectedName) { memberList.SelectedIndex = i; break; }
                }
                if (memberList.SelectedIndex < 0 && memberList.Items.Count > 0) memberList.SelectedIndex = 0;
            }
            RefreshAboutMembers();
            var actions = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = true, AutoScroll = true };
            Button Action(string label, Action perform)
            {
                var button = new Button { Text = label, Width = 148, Height = 43,
                    BackColor = Color.FromArgb(225, 236, 252) };
                button.Click += (_, __) => perform();
                actions.Controls.Add(button);
                return button;
            }
            void SelectForMain()
            {
                if (memberList.SelectedItem is not PersonRow member) return;
                searchTextBox.Text = "";
                RefreshPeople(member.Name);
            }
            Action("Add Person", () =>
            {
                int before = _people.Count;
                addPersonButton_Click(null, EventArgs.Empty);
                RefreshAboutMembers(_people.Count > before ? _people.Last().Name : null);
            });
            Action("Add Connection", () =>
            {
                SelectForMain();
                connectButton_Click(null, EventArgs.Empty);
                RefreshAboutMembers((memberList.SelectedItem as PersonRow)?.Name);
            });
            Action("Add Details", () =>
            {
                if (memberList.SelectedItem is PersonRow person)
                    AddPersonNote(person.Name);
                else MessageBox.Show(window, "Choose a family member first.");
            });
            Action("View All Details", () =>
            {
                SelectForMain();
                ShowAllPersonInformation(null, EventArgs.Empty);
            });
            familyLayout.Controls.Add(familyCount, 0, 0);
            familyLayout.Controls.Add(memberList, 0, 1);
            familyLayout.Controls.Add(actions, 0, 2);
            family.Controls.Add(familyLayout);

            var close = new Button { Text = "Close", Dock = DockStyle.Right, Width = 130,
                Height = 40, DialogResult = DialogResult.OK, BackColor = Color.FromArgb(225, 236, 252) };
            var footer = new Panel { Dock = DockStyle.Fill };
            footer.Controls.Add(close);
            layout.Controls.Add(heading, 0, 0);
            layout.Controls.Add(tabs, 0, 1);
            layout.Controls.Add(footer, 0, 2);
            window.Controls.Add(layout);
            window.AcceptButton = close;
            window.ShowDialog(this);
        }
    }
}
