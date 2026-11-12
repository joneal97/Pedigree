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
        private readonly bool _isExperimental;
        private Form1? _experimentalView;
        private JsonObject? _matchSettings;
        private JsonObject? _friendsDocument;
        private sealed record PersonRow(string Name, string Tg, string Details, string Verify)
        {
            public override string ToString() => string.IsNullOrWhiteSpace(Tg) ? Name : $"{Name} ({Tg})";
        }

        public Form1()
        {
            _isExperimental = false;
            InitializeComponent();
        }

        private Form1(bool experimental)
        {
            _isExperimental = experimental;
            InitializeComponent();
            if (experimental) ConfigureExperimentalMatching();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            InitializeView();
            CreateExperimentalView();
        }

        private void InitializeView()
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
            foreach (var button in new[] { updateImportButton, updateExportButton })
            {
                button.BackColor = Color.FromArgb(225, 236, 252);
                button.FlatStyle = FlatStyle.Flat;
                button.FlatAppearance.BorderColor = Color.FromArgb(162, 189, 228);
            }
            updateInstructionsTextBox.Text = ChatGptInstructions();
            friendsInstructionsTextBox.Text = FriendsInstructions();
            exportStyleTextBox.Text = "CLASSROOM POSTER STYLE\r\n" +
                "Parents above • Selected person in the center • Children below\r\n" +
                "Each card includes a name and TG identifier. A fact box holds original notes.\r\n" +
                "Unverified research is never silently presented as proven.\r\n" +
                "The built-in family members are written in C# source. Import JSON adds relatives.\r\n" +
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
            LoadFriendsDocument();
        }

        private void CreateExperimentalView()
        {
            _experimentalView = new Form1(true);
            _experimentalView.InitializeView();
            experimentalTabPage.Controls.Clear();
            experimentalTabPage.Controls.Add(_experimentalView.DetachMainLayout());
        }

        private Control DetachMainLayout()
        {
            mainTabPage.Controls.Remove(mainTableLayout);
            mainTableLayout.Dock = DockStyle.Fill;
            return mainTableLayout;
        }

        private static string ExperimentalSettingsPath()
        {
            string projectFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "source", "repos", "Pedigree", "Pedigree");
            Directory.CreateDirectory(projectFolder);
            return Path.Combine(projectFolder, "settings_experimental_familymatch.json");
        }

        private void ConfigureExperimentalMatching()
        {
            familyListBox.DrawMode = DrawMode.OwnerDrawFixed;
            familyListBox.DrawItem += familyListBox_DrawItem;
            LoadMatchSettings();

        }

        private void LoadMatchSettings()
        {
            try
            {
                string path = ExperimentalSettingsPath();
                _matchSettings = File.Exists(path)
                    ? JsonNode.Parse(File.ReadAllText(path)) as JsonObject
                    : null;
            }
            catch
            {
                _matchSettings = null;
            }
            _matchSettings ??= new JsonObject { ["matches"] = new JsonArray() };
            if (_matchSettings["matches"] is not JsonArray)
                _matchSettings["matches"] = new JsonArray();
        }

        private JsonArray MatchRecords => (JsonArray)_matchSettings!["matches"]!;

        private void SaveMatchSettings()
        {
            File.WriteAllText(ExperimentalSettingsPath(),
                _matchSettings!.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
        }

        private void RefreshMatchState()
        {
            if (!_isExperimental || _document == null) return;
            var knownNames = _people.Select(p => p.Name)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            var previous = MatchRecords.Where(record => Flag(record, "manual"))
                .ToDictionary(record => Value(record, "display_name"), StringComparer.OrdinalIgnoreCase);
            var records = new JsonArray();
            foreach (PersonRow person in _people)
            {
                if (previous.TryGetValue(person.Name, out JsonNode? manualRecord))
                {
                    records.Add(manualRecord.DeepClone());
                    continue;
                }

                string[] parents = ParentsOf(person.Name).ToArray();
                string[] spouses = SpousesOf(person.Name).Concat(CoparentsOf(person.Name)).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
                string[] children = ChildrenOf(person.Name).ToArray();
                bool matched = parents.Concat(spouses).Concat(children).All(knownNames.Contains);
                records.Add(new JsonObject
                {
                    ["display_name"] = person.Name,
                    ["parents"] = ToJsonArray(parents),
                    ["spouses"] = ToJsonArray(spouses),
                    ["children"] = ToJsonArray(children),
                    ["justification"] = "Automatically matched by exact family-member name.",
                    ["manual"] = false,
                    ["matched"] = matched
                });
            }
            _matchSettings["matches"] = records;
            SaveMatchSettings();
            familyListBox.Invalidate();
        }

        private bool IsFamilyMatchComplete(string name) =>
            MatchRecords.FirstOrDefault(record => Value(record, "display_name") == name) is JsonNode record &&
            Flag(record, "matched");

        private void familyListBox_DrawItem(object? sender, DrawItemEventArgs e)
        {
            if (e.Index < 0 || e.Index >= familyListBox.Items.Count) return;
            e.DrawBackground();
            var person = (PersonRow)familyListBox.Items[e.Index];
            Color color = IsFamilyMatchComplete(person.Name) ? Color.ForestGreen : e.ForeColor;
            using var brush = new SolidBrush(color);
            e.Graphics.DrawString(person.ToString(), e.Font, brush, e.Bounds);
            e.DrawFocusRectangle();
        }

        private static JsonArray EnsureArray(JsonObject document, string key)
        {
            if (document[key] is JsonArray array) return array;
            throw new InvalidDataException($"Expected a JSON array named '{key}'. The file was not modified.");
        }
        private static string Value(JsonNode? node, string key) => node?[key]?.ToString() ?? "";
        private static bool Flag(JsonNode? node, string key) => node?[key]?.GetValue<bool>() ?? false;
        private JsonArray PeopleArray => EnsureArray(_document!, "people");
        private JsonArray IdentifierArray => EnsureArray(_document!, "identifiers");
        private JsonArray RelationshipArray => EnsureArray(_document!, "relationships");

        private void ReadBuiltInDocument()
        {
            // Every member of the original family tree is encoded in
            // BuiltInFamilyData.cs along with every associated data section.
            // Each run recreates a fresh in-memory copy without touching the C# source.
            JsonObject loaded = BuiltInFamilyData.Create();
            foreach (string name in new[] { "people", "identifiers", "relationships" })
                EnsureArray(loaded, name);
            if (EnsureArray(loaded, "people").Count != 129)
                throw new InvalidDataException("Built-in tree must contain 129 original people.");
            _document = loaded;
            _currentPath = null;
            RefreshPeople();
            UpdateStatus("129 original family members loaded from C# source code. Import JSON to add more family members.");
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
                $"The original 129 built-in family members were not changed.",
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
                    _people.Add(new PersonRow(name, tg, Value(p, "details"), Value(p, "verify")));
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
            BuildRelationshipTree();
            if (_isExperimental) RefreshMatchState();
        }

        private void BuildRelationshipTree()
        {
            if (_document == null) return;
            const string startingPerson = "Jeremiah O'Neal";
            PersonRow? start = _people.FirstOrDefault(person =>
                person.Name.Equals(startingPerson, StringComparison.OrdinalIgnoreCase));

            treeView.BeginUpdate();
            treeView.Nodes.Clear();
            try
            {
                if (start == null)
                {
                    treeView.Nodes.Add("Jeremiah O'Neal was not found in the family members.");
                    return;
                }

                string DisplayName(string name)
                {
                    PersonRow? person = _people.FirstOrDefault(candidate =>
                        candidate.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
                    return person?.ToString() ?? name;
                }

                void AddAncestors(string name, TreeNodeCollection nodes, HashSet<string> path)
                {
                    foreach (string parent in ParentsOf(name))
                    {
                        bool cycle = path.Contains(parent);
                        TreeNode parentNode = nodes.Add("Parent: " + DisplayName(parent) +
                            (cycle ? " (cycle detected)" : ""));
                        if (cycle) continue;
                        var nextPath = new HashSet<string>(path, StringComparer.OrdinalIgnoreCase) { parent };
                        AddAncestors(parent, parentNode.Nodes, nextPath);
                    }
                }

                TreeNode root = treeView.Nodes.Add(start.ToString());
                AddAncestors(start.Name, root.Nodes,
                    new HashSet<string>(new[] { start.Name }, StringComparer.OrdinalIgnoreCase));
                root.Expand();
            }
            finally
            {
                treeView.EndUpdate();
            }
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
        private static JsonArray ToJsonArray(IEnumerable<string> values)
        {
            var array = new JsonArray();
            foreach (string value in values) array.Add(value);
            return array;
        }
        private JsonObject CreatePersonSnapshot(PersonRow person)
        {
            return new JsonObject
            {
                ["display_name"] = person.Name,
                ["tg_number"] = person.Tg,
                ["born"] = Provisional(person.Name, "birth_date") ?? "See original notes",
                ["died"] = Provisional(person.Name, "death_date") ?? "See original notes",
                ["parents"] = ToJsonArray(ParentsOf(person.Name)),
                ["spouse"] = ToJsonArray(SpousesOf(person.Name)),
                ["other_parents"] = ToJsonArray(CoparentsOf(person.Name)),
                ["children"] = ToJsonArray(ChildrenOf(person.Name)),
                ["details"] = person.Details,
                ["notes"] = NotesOf(person.Name),
                ["verify"] = person.Verify,
                ["captured_at_utc"] = DateTime.UtcNow.ToString("O")
            };
        }
        private void CaptureAllPeople()
        {
            var captured = new JsonArray();
            foreach (PersonRow person in _people)
                captured.Add(CreatePersonSnapshot(person));
            _document!["captured_people"] = captured;
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
            funFactTextBox.Text = string.Join(Environment.NewLine, new[] {
                person.Details,
                notes,
                string.IsNullOrWhiteSpace(person.Verify) ? "" : "Verify: " + person.Verify
            }
                .Where(x => !string.IsNullOrWhiteSpace(x)));
            if (string.IsNullOrWhiteSpace(funFactTextBox.Text)) funFactTextBox.Text = "No notes recorded.";
            UpdateStatus($"Showing {person.Name}. Birth/death field suggestions are labeled unverified.");
        }

        private void searchTextBox_TextChanged(object? sender, EventArgs e) => RefreshPeople();
        private void UpdateStatus(string message) => statusLabel.Text = message;

        private static string TreeFolder()
        {
            // Keep all user-selected imports and exports in C:\tree by default.
            // No installers or external files are required for the original members or resume.
            string path = @"C:\Users\161119\source\repos\John-McCabe-et-al-628417\";
            Directory.CreateDirectory(path);
            return path;
        }

        private static string FriendsPath() => Path.Combine(UpdatePackageFolder(), "BP.json");
        private static string FriendsSamplePath() => Path.Combine(UpdatePackageFolder(), "BP-sample.json");
        private static string FriendsInstructionsPath() => Path.Combine(UpdatePackageFolder(), "bp.md");

        private static string FriendsInstructions()
        {
            return "BP.json FORMAT\r\n\r\n" +
                "Create a separate friend-family tree in the people and relationships arrays. " +
                "Use roles subject, father, mother, grandmother-father-line, grandfather-father-line, " +
                "grandmother-mother-line, and grandfather-mother-line, continuing the same pattern for generation 3.\r\n" +
                "Each person needs display_name, role, birth_year, source_apa, and findagrave. " +
                "Use tg_number when the person matches someone in Tree; also preserve tree_marker when supplied. " +
                "When no identifier is supplied, the program assigns a permanent UF###### identifier.\r\n" +
                "Relationships use child and parent names with relation parent. Spouses use person1 and person2.\r\n";
        }

        private static string FriendsMarkdown()
        {
            return "# BP.json instructions for ChatGPT\r\n\r\n" +
                "Use BP-sample.json as the template and write the completed result to BP.json. This is a separate Friends family tree; do not rename or delete people in the main Pedigree Tree.\r\n\r\n" +
                "## Required fields\r\n" +
                "Every person requires `display_name`, `role`, `birth_year`, `source_apa`, and `findagrave`. Use an empty string for `findagrave` if no Find a Grave page exists. Use a valid APA citation in `source_apa`.\r\n\r\n" +
                "## Three generations\r\n" +
                "Fill every sample line beginning with `subject` for generation 1, `father` and `mother` for generation 2, and the four grandparent lines for generation 3: `grandmother-father-line`, `grandfather-father-line`, `grandmother-mother-line`, and `grandfather-mother-line`. Use plausible birth years for a subject born in the 1970s: parents commonly around 1940–1955 and grandparents around 1905–1935.\r\n\r\n" +
                "## Tree links\r\n" +
                "If a Friends person is also in the existing Tree, include `tg_number` and `tree_marker` in that person. Do not invent a TG number. The program preserves supplied identifiers and assigns UF###### when none is supplied.\r\n\r\n" +
                "## Relationships\r\n" +
                "Add `{ \"child\": \"Name\", \"parent\": \"Name\", \"relation\": \"parent\" }` for every parent-child link. Add spouse records to `spouse_relationships`.\r\n";
        }

        private static JsonObject CreateFriendsSample()
        {
            var people = new JsonArray();
            void Add(string name, string role, int year) => people.Add(new JsonObject
            {
                ["display_name"] = name, ["role"] = role, ["birth_year"] = year,
                ["source_apa"] = "Author, A. A. (2026). Family record title. Archive or database.",
                ["findagrave"] = "", ["tg_number"] = "", ["tree_marker"] = ""
            });
            Add("Subject Example", "subject", 1974);
            Add("Father Example", "father", 1948);
            Add("Mother Example", "mother", 1950);
            Add("Grandfather Father Example", "grandfather-father-line", 1920);
            Add("Grandmother Father Example", "grandmother-father-line", 1924);
            Add("Grandfather Mother Example", "grandfather-mother-line", 1922);
            Add("Grandmother Mother Example", "grandmother-mother-line", 1927);
            return new JsonObject
            {
                ["schema"] = "Pedigree.BP.FriendsFamily.v1",
                ["family_name"] = "Replace with the friend family name",
                ["people"] = people,
                ["relationships"] = new JsonArray
                {
                    new JsonObject { ["child"] = "Subject Example", ["parent"] = "Father Example", ["relation"] = "parent" },
                    new JsonObject { ["child"] = "Subject Example", ["parent"] = "Mother Example", ["relation"] = "parent" },
                    new JsonObject { ["child"] = "Father Example", ["parent"] = "Grandfather Father Example", ["relation"] = "parent" },
                    new JsonObject { ["child"] = "Father Example", ["parent"] = "Grandmother Father Example", ["relation"] = "parent" },
                    new JsonObject { ["child"] = "Mother Example", ["parent"] = "Grandfather Mother Example", ["relation"] = "parent" },
                    new JsonObject { ["child"] = "Mother Example", ["parent"] = "Grandmother Mother Example", ["relation"] = "parent" }
                },
                ["spouse_relationships"] = new JsonArray(),
                ["tree_links"] = new JsonArray()
            };
        }

        private void LoadFriendsDocument()
        {
            if (!File.Exists(FriendsPath())) return;
            try { ImportFriendsPackage(FriendsPath(), false); }
            catch { _friendsDocument = null; friendsTreeView.Nodes.Clear(); }
        }

        private void friendsSampleButton_Click(object? sender, EventArgs e)
        {
            try
            {
                File.WriteAllText(FriendsSamplePath(), CreateFriendsSample().ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
                File.WriteAllText(FriendsInstructionsPath(), FriendsMarkdown());
                friendsInstructionsTextBox.Text = FriendsInstructions();
                UpdateStatus($"Created {FriendsSamplePath()} and {FriendsInstructionsPath()}.");
                MessageBox.Show(this, "BP-sample.json and bp.md were created.", "Friends template created", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex) { MessageBox.Show(this, ex.Message, "Could not create Friends template", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        private void friendsExportButton_Click(object? sender, EventArgs e)
        {
            if (_friendsDocument == null) { MessageBox.Show(this, "Import or create a Friends BP package first."); return; }
            try
            {
                File.WriteAllText(FriendsPath(), _friendsDocument.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
                File.WriteAllText(FriendsInstructionsPath(), FriendsMarkdown());
                UpdateStatus($"Wrote Friends family to {FriendsPath()}.");
            }
            catch (Exception ex) { MessageBox.Show(this, ex.Message, "Could not write BP.json", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        private void friendsImportButton_Click(object? sender, EventArgs e)
        {
            using var dialog = new OpenFileDialog { Title = "Import BP.json", Filter = "BP JSON (*.json)|*.json|All files (*.*)|*.*", InitialDirectory = UpdatePackageFolder(), FileName = "BP.json" };
            if (dialog.ShowDialog(this) != DialogResult.OK) return;
            try
            {
                ImportFriendsPackage(dialog.FileName, true);
                UpdateStatus("Imported BP.json into the Friends tab and wrote a validated BP.json copy.");
            }
            catch (Exception ex) { MessageBox.Show(this, ex.Message, "Could not import BP.json", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        private void ImportFriendsPackage(string path, bool persist)
        {
            JsonObject package = JsonNode.Parse(File.ReadAllText(path)) as JsonObject
                ?? throw new InvalidDataException("BP.json must contain a JSON object.");
            if (Value(package, "schema") != "Pedigree.BP.FriendsFamily.v1")
                throw new InvalidDataException("BP.json has an unsupported schema.");
            if (package["people"] is not JsonArray people || package["relationships"] is not JsonArray relationships)
                throw new InvalidDataException("BP.json must contain people and relationships arrays.");

            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var identifiers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (JsonNode? person in people)
            {
                string name = Value(person, "display_name");
                if (string.IsNullOrWhiteSpace(name) || !names.Add(name))
                    throw new InvalidDataException("Every Friends person must have a unique display_name.");
                if (string.IsNullOrWhiteSpace(Value(person, "role")) || !int.TryParse(Value(person, "birth_year"), out _))
                    throw new InvalidDataException($"Friends person {name} must have role and birth_year.");
                if (string.IsNullOrWhiteSpace(Value(person, "source_apa")))
                    throw new InvalidDataException($"Friends person {name} must have source_apa.");
                string supplied = Value(person, "identifier");
                if (string.IsNullOrWhiteSpace(supplied)) supplied = Value(person, "tg_number");
                string identifier = string.IsNullOrWhiteSpace(supplied) ? NextFriendUf(identifiers) : supplied;
                if (!identifiers.Add(identifier)) throw new InvalidDataException($"Duplicate Friends identifier: {identifier}.");
                person!["identifier"] = identifier;
                person["findagrave"] = Value(person, "findagrave");
            }
            foreach (JsonNode? relation in relationships)
            {
                if (!names.Contains(Value(relation, "child")) || !names.Contains(Value(relation, "parent")))
                    throw new InvalidDataException("Every Friends relationship must reference people in the people array.");
            }
            if (package["spouse_relationships"] is JsonArray spouses)
                foreach (JsonNode? spouse in spouses)
                    if (!names.Contains(Value(spouse, "person1")) || !names.Contains(Value(spouse, "person2")))
                        throw new InvalidDataException("Every Friends spouse relationship must reference people in the people array.");

            _friendsDocument = package;
            RenderFriendsTree();
            if (persist)
            {
                File.WriteAllText(FriendsPath(), package.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
                File.WriteAllText(FriendsInstructionsPath(), FriendsMarkdown());
            }
        }

        private static string NextFriendUf(HashSet<string> current)
        {
            for (int i = 0; i < 1_000_000; i++)
            {
                string candidate = $"UF{i:D6}";
                if (!current.Contains(candidate)) return candidate;
            }
            throw new InvalidOperationException("No UF###### identifiers remain available for Friends.");
        }

        private void RenderFriendsTree()
        {
            friendsTreeView.BeginUpdate();
            friendsTreeView.Nodes.Clear();
            try
            {
                if (_friendsDocument?["people"] is not JsonArray people || _friendsDocument["relationships"] is not JsonArray relationships) return;
                var children = relationships.GroupBy(r => Value(r, "child"), StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(g => g.Key, g => g.Select(r => Value(r, "parent")).ToArray(), StringComparer.OrdinalIgnoreCase);
                string Label(string name)
                {
                    JsonNode? person = people.FirstOrDefault(p => Value(p, "display_name").Equals(name, StringComparison.OrdinalIgnoreCase));
                    if (person == null) return name;
                    string identifier = Value(person, "identifier");
                    string marker = Value(person, "tree_marker");
                    return $"{name} ({identifier})" + (string.IsNullOrWhiteSpace(marker) ? "" : $" [{marker}]");
                }
                var roots = people.Select(p => Value(p, "display_name"))
                    .Where(name => !relationships.Any(r => Value(r, "child").Equals(name, StringComparison.OrdinalIgnoreCase)))
                    .ToArray();
                foreach (string rootName in roots)
                {
                    TreeNode root = friendsTreeView.Nodes.Add(Label(rootName));
                    AddFriendParents(rootName, root.Nodes, children, Label, new HashSet<string>(StringComparer.OrdinalIgnoreCase) { rootName });
                    root.Expand();
                }
            }
            finally { friendsTreeView.EndUpdate(); }
        }

        private static void AddFriendParents(string name, TreeNodeCollection nodes,
            Dictionary<string, string[]> parents, Func<string, string> label, HashSet<string> path)
        {
            if (!parents.TryGetValue(name, out string[]? values)) return;
            foreach (string parent in values)
            {
                bool cycle = !path.Add(parent);
                TreeNode node = nodes.Add("Parent: " + label(parent) + (cycle ? " (cycle detected)" : ""));
                if (!cycle) AddFriendParents(parent, node.Nodes, parents, label, path);
                path.Remove(parent);
            }
        }

        private static string WholeTreePath()
        {
            string projectFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "source", "repos", "Pedigree", "Pedigree");
            Directory.CreateDirectory(projectFolder);
            return Path.Combine(projectFolder, "whole_tree.json");
        }

        private static string UpdatePackageFolder()
        {
            string path = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "source", "repos", "Pedigree", "Pedigree");
            Directory.CreateDirectory(path);
            return path;
        }

        private static string BuildVersion()
        {
            Version version = System.Reflection.Assembly.GetEntryAssembly()?.GetName().Version ?? new Version(1, 0, 0, 0);
            return version.ToString();
        }

        private static string ChatGptInstructions()
        {
            return "CHATGPT FAMILY TREE UPDATE FORMAT\r\n\r\n" +
                "This file is an interchange package. Do not rewrite or delete existing people. " +
                "Add requested people to updates.people and relationships to updates.relationships.\r\n" +
                "Each new person must have display_name, source_apa, and findagrave (use an empty string when unavailable). " +
                "A TG number may be supplied as tg_number; if it is omitted, the program assigns a permanent UF###### identifier.\r\n" +
                "To update an existing person, include display_name and verify: 11/11/2026 in updates.verify_updates. " +
                "Do not change existing names or TG identifiers. Use parent links with child and parent names. " +
                "Add spouse links with person1 and person2. You may include people and links for parents of newly added people.\r\n\r\n" +
                "Example: updates.people can contain Linda McCabe and Kerrie McCabe; updates.relationships can contain " +
                "Dudley McCabe -> Linda McCabe as spouse, Dudley McCabe -> Kerrie McCabe as parent, and Linda McCabe -> Kerrie McCabe as parent.\r\n";
        }

        private static string InstructionsMarkdown()
        {
            return "# Pedigree ChatGPT Update Instructions\r\n\r\n" +
                "The exported JSON is a read-only copy of the current family tree. Do not delete or rename existing people.\r\n\r\n" +
                "## How to add people\r\n" +
                "Add objects to `updates.people`. Each object requires `display_name`, `source_apa`, and `findagrave`. " +
                "Use an empty string for `findagrave` when no Find a Grave page exists. You may provide `tg_number`; otherwise the program assigns a permanent `UF######` identifier.\r\n\r\n" +
                "## How to update verification\r\n" +
                "Add `{ \"display_name\": \"Name\", \"verify\": \"11/11/2026\" }` to `updates.verify_updates`. Existing names and TG identifiers are preserved.\r\n\r\n" +
                "## How to preserve relationships\r\n" +
                "Use `updates.relationships` with `{ \"child\": \"Child\", \"parent\": \"Parent\", \"relation\": \"parent\" }`. " +
                "Use `updates.spouse_relationships` with `{ \"person1\": \"Dudley McCabe\", \"person2\": \"Linda McCabe\" }`. " +
                "Include parents of newly added people as additional people and links in the same package.\r\n\r\n" +
                "## Example\r\n" +
                "```json\r\n" +
                "{\r\n  \"updates\": {\r\n    \"people\": [\r\n      { \"display_name\": \"Linda McCabe\", \"source_apa\": \"Author, A. (2026). Record title. Archive.\", \"findagrave\": \"\" },\r\n      { \"display_name\": \"Kerrie McCabe\", \"source_apa\": \"Author, A. (2026). Record title. Archive.\", \"findagrave\": \"\" }\r\n    ],\r\n    \"relationships\": [\r\n      { \"child\": \"Kerrie McCabe\", \"parent\": \"Dudley McCabe\", \"relation\": \"parent\" },\r\n      { \"child\": \"Kerrie McCabe\", \"parent\": \"Linda McCabe\", \"relation\": \"parent\" }\r\n    ],\r\n    \"spouse_relationships\": [\r\n      { \"person1\": \"Dudley McCabe\", \"person2\": \"Linda McCabe\", \"relation\": \"spouse\" }\r\n    ],\r\n    \"verify_updates\": [\r\n      { \"display_name\": \"Sarah McAvoy\", \"verify\": \"11/11/2026\" }\r\n    ]\r\n  }\r\n}\r\n" +
                "```\r\n";
        }

        private string UniqueUf()
        {
            var used = PeopleArray.Select(p => Value(p, "uf_number"))
                .Concat(IdentifierArray.Select(p => Value(p, "code")))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < 1_000_000; i++)
            {
                string candidate = $"UF{i:D6}";
                if (!used.Contains(candidate)) return candidate;
            }
            throw new InvalidOperationException("No UF###### identifiers remain available.");
        }

        private JsonObject CreateUpdatePackage()
        {
            CaptureAllPeople();
            var updates = new JsonObject
            {
                ["people"] = new JsonArray(),
                ["relationships"] = new JsonArray(),
                ["spouse_relationships"] = new JsonArray(),
                ["verify_updates"] = new JsonArray()
            };
            return new JsonObject
            {
                ["schema"] = "Pedigree.ChatGptFamilyTreeUpdate.v1",
                ["export_version_build"] = BuildVersion(),
                ["read_only"] = true,
                ["read_only_notice"] = "This is a read-only family tree export. Editing this file does not change the Pedigree program until it is imported.",
                ["instructions"] = ChatGptInstructions(),
                ["examples"] = JsonNode.Parse("[{\"description\":\"Dudley McCabe and Linda McCabe have Kerrie McCabe\",\"updates\":{\"people\":[{\"display_name\":\"Linda McCabe\",\"source_apa\":\"Author, A. (2026). Record title. Archive.\",\"findagrave\":\"\"},{\"display_name\":\"Kerrie McCabe\",\"source_apa\":\"Author, A. (2026). Record title. Archive.\",\"findagrave\":\"\"}],\"relationships\":[{\"child\":\"Kerrie McCabe\",\"parent\":\"Dudley McCabe\",\"relation\":\"parent\"},{\"child\":\"Kerrie McCabe\",\"parent\":\"Linda McCabe\",\"relation\":\"parent\"}],\"spouse_relationships\":[{\"person1\":\"Dudley McCabe\",\"person2\":\"Linda McCabe\",\"relation\":\"spouse\"}]}}]")!, 
                ["tree"] = _document!.DeepClone(),
                ["updates"] = updates
            };
        }

        private void updateExportButton_Click(object? sender, EventArgs e)
        {
            if (_document == null) { MessageBox.Show(this, "The family tree could not be loaded."); return; }
            try
            {
                string folder = UpdatePackageFolder();
                string version = BuildVersion();
                string path = Path.Combine(folder, $"Pedigree-ver-{version}.json");
                File.WriteAllText(path, CreateUpdatePackage().ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
                File.WriteAllText(Path.Combine(folder, "instructions.md"), InstructionsMarkdown());
                UpdateStatus($"Exported ChatGPT update package to {path}");
                MessageBox.Show(this, $"The versioned family tree package and instructions.md were saved in:\n{folder}", "ChatGPT package exported", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex) { MessageBox.Show(this, ex.Message, "ChatGPT export failed", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        private void updateImportButton_Click(object? sender, EventArgs e)
        {
            using var dialog = new OpenFileDialog { Title = "Import ChatGPT family tree updates", Filter = "Pedigree update JSON (*.json)|*.json|All files (*.*)|*.*", InitialDirectory = UpdatePackageFolder() };
            if (dialog.ShowDialog(this) != DialogResult.OK) return;
            try
            {
                ImportUpdatePackage(dialog.FileName);
                MessageBox.Show(this, "The ChatGPT family tree updates were imported into Main, Experimental, and Tree.", "Updates imported", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex) { MessageBox.Show(this, ex.Message, "ChatGPT import failed", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        private void ImportUpdatePackage(string path)
        {
            JsonObject package = JsonNode.Parse(File.ReadAllText(path)) as JsonObject
                ?? throw new InvalidDataException("The selected file must contain a JSON object.");
            if (Value(package, "schema") != "Pedigree.ChatGptFamilyTreeUpdate.v1")
                throw new InvalidDataException("This is not a supported Pedigree ChatGPT update package.");
            if (_document == null) ReadBuiltInDocument();
            if (package["updates"] is not JsonObject updates)
                throw new InvalidDataException("The package must contain an updates object.");

            var existingNames = PeopleArray.Select(p => Value(p, "display_name"))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            var knownCodes = IdentifierArray.Select(p => Value(p, "code"))
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            int nextPerson = PeopleArray.Select(p => int.TryParse(Value(p, "person_no"), out int n) ? n : 0)
                .DefaultIfEmpty(0).Max() + 1;

            if (updates["people"] is JsonArray people)
            {
                foreach (JsonNode? source in people)
                {
                    string name = Value(source, "display_name");
                    if (string.IsNullOrWhiteSpace(name)) continue;
                    if (existingNames.Contains(name)) continue;
                    string supplied = Value(source, "tg_number");
                    string code = string.IsNullOrWhiteSpace(supplied) ? UniqueUf() : supplied;
                    if (knownCodes.Contains(code))
                        throw new InvalidDataException($"The identifier {code} is already assigned.");
                    var person = new JsonObject
                    {
                        ["person_no"] = nextPerson++, ["display_name"] = name,
                        ["details"] = Value(source, "details"), ["name_permanent"] = 0,
                        ["source_apa"] = Value(source, "source_apa"),
                        ["findagrave"] = Value(source, "findagrave")
                    };
                    if (!string.IsNullOrWhiteSpace(Value(source, "verify")))
                        person["verify"] = Value(source, "verify");
                    PeopleArray.Add(person);
                    IdentifierArray.Add(new JsonObject
                    {
                        ["display_name"] = name, ["code"] = code,
                        ["kind"] = string.IsNullOrWhiteSpace(supplied) ? "UF" : "TG",
                        ["permanent"] = 1
                    });
                    existingNames.Add(name);
                    knownCodes.Add(code);
                }
            }

            AddImportedRelationships(updates["relationships"] as JsonArray, "parent");
            AddImportedSpouses(updates["spouse_relationships"] as JsonArray);
            if (updates["verify_updates"] is JsonArray verifications)
            {
                foreach (JsonNode? update in verifications)
                {
                    string name = Value(update, "display_name");
                    JsonNode? person = PeopleArray.FirstOrDefault(p => Value(p, "display_name").Equals(name, StringComparison.OrdinalIgnoreCase));
                    if (person != null && Value(update, "verify") == "11/11/2026") person["verify"] = "11/11/2026";
                }
            }
            RefreshPeople();
            SynchronizeExperimentalView();
            UpdateStatus($"Imported ChatGPT updates. The tree now contains {_people.Count} people.");
        }

        private void AddImportedRelationships(JsonArray? relationships, string defaultRelation)
        {
            if (relationships == null) return;
            var known = RelationshipArray.Select(r => (Value(r, "child"), Value(r, "parent"), Value(r, "relation"))).ToHashSet();
            foreach (JsonNode? relation in relationships)
            {
                string child = Value(relation, "child"), parent = Value(relation, "parent");
                string type = string.IsNullOrWhiteSpace(Value(relation, "relation")) ? defaultRelation : Value(relation, "relation");
                if (string.IsNullOrWhiteSpace(child) || string.IsNullOrWhiteSpace(parent) ||
                    !PeopleArray.Any(p => Value(p, "display_name").Equals(child, StringComparison.OrdinalIgnoreCase)) ||
                    !PeopleArray.Any(p => Value(p, "display_name").Equals(parent, StringComparison.OrdinalIgnoreCase))) continue;
                if (known.Add((child, parent, type)))
                    RelationshipArray.Add(new JsonObject { ["child"] = child, ["parent"] = parent, ["relation"] = type, ["confidence"] = 1.0, ["permanent"] = 0 });
            }
        }

        private void AddImportedSpouses(JsonArray? spouses)
        {
            if (spouses == null) return;
            var target = SupplementalArray("spouse_relationships");
            foreach (JsonNode? spouse in spouses)
            {
                string first = Value(spouse, "person1"), second = Value(spouse, "person2");
                if (string.IsNullOrWhiteSpace(first) || string.IsNullOrWhiteSpace(second) || first.Equals(second, StringComparison.OrdinalIgnoreCase) ||
                    !PeopleArray.Any(p => Value(p, "display_name").Equals(first, StringComparison.OrdinalIgnoreCase)) ||
                    !PeopleArray.Any(p => Value(p, "display_name").Equals(second, StringComparison.OrdinalIgnoreCase))) continue;
                bool exists = target.Any(r => (Value(r, "person1").Equals(first, StringComparison.OrdinalIgnoreCase) && Value(r, "person2").Equals(second, StringComparison.OrdinalIgnoreCase)) ||
                    (Value(r, "person1").Equals(second, StringComparison.OrdinalIgnoreCase) && Value(r, "person2").Equals(first, StringComparison.OrdinalIgnoreCase)));
                if (!exists) target.Add(new JsonObject { ["person1"] = first, ["person2"] = second, ["relation"] = "spouse", ["source"] = "ChatGPT update", ["permanent"] = 0 });
            }
        }

        private void SynchronizeExperimentalView()
        {
            if (_experimentalView == null || _document == null) return;
            _experimentalView._document = _document.DeepClone() as JsonObject;
            _experimentalView.RefreshPeople();
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
            try
            {
                CaptureAllPeople();
                string path = WholeTreePath();
                File.WriteAllText(path, _document.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
                UpdateStatus($"Exported {_people.Count} people to {path}");
                MessageBox.Show(this, $"The complete family tree was saved to:\n{path}", "Whole tree exported", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex) { MessageBox.Show(this, ex.Message, "Whole-tree export failed", MessageBoxButtons.OK, MessageBoxIcon.Error); }
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

            // Family members and editing tools stay inside About. All built-in
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
                familyCount.Text = $"{_people.Count} family members (129 built into C#)";
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
