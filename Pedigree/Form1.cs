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
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
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
        private Form1? _mainViewOwner;
        private JsonObject? _matchSettings;
        private JsonObject? _friendsDocument;
        private Label? _chronologicalSummaryLabel;
        private DataGridView? _chronologicalGrid;
        private DataGridView? _geographyGrid;
        private ListBox? _editPeopleListBox;
        private TextBox? _editSearchTextBox;
        private Label? _updateWorkspaceStatusLabel;
        private TextBox? _researchReviewSummaryTextBox;
        private DataGridView? _researchReviewGrid;
        private readonly HashSet<string> _orangeCodes = new(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _blueCodes = new(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _purpleCodes = new(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _excludedChronologicalNames = new(StringComparer.OrdinalIgnoreCase);
        private sealed record GeographyRow(string Name, string Location, string LocationBasis,
            string TimePeriod, string RecommendedSources, int Difficulty, string DifficultyReason);
        private sealed record PersonRow(string Name, string Tg, string Details, string Verify, string OriginalName)
        {
            public override string ToString() => string.IsNullOrWhiteSpace(Tg) ? Name : $"{Name} ({Tg})";
        }

        private const string GhtreeUrl = "https://raw.githubusercontent.com/we6jbo/AncesTaleextended/refs/heads/master/ghtree.json";
        private static readonly System.Net.Http.HttpClient GhtreeHttpClient = new()
        {
            Timeout = TimeSpan.FromSeconds(30)
        };

        public Form1()
        {
            _isExperimental = false;
            InitializeComponent();
            InitializeExportTab();
        }

        private void InitializeExportTab()
        {
            var tab = new TabPage("Export") { BackColor = Color.White };
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(24),
                ColumnCount = 1,
                RowCount = 2
            };
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 142F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 64F));
            var description = new Label
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 11F),
                Text = "Export the complete family tree to C:\\Users\\161119\\source\\repos\\Pedigree\\xwtree.json and E:\\xwtree.json.\r\n" +
                    "Import fetches the latest GitHub ghtree.json, previews the additive merge, and keeps imported relationships and captured research unverified.\r\n" +
                    "Each GitHub version and available E:\\ghtree.txt sample is archived in C:\\Users\\161119\\source\\repos\\Pedigree\\ghtree-versions.",
                TextAlign = ContentAlignment.MiddleLeft
            };
            var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight };
            var exportButton = new Button
            {
                Text = "Export",
                Width = 180,
                Height = 48,
                BackColor = Color.FromArgb(225, 236, 252)
            };
            exportButton.Click += (_, __) => ExportTreeToRequestedPaths();
            var importButton = new Button
            {
                Text = "Import",
                Width = 180,
                Height = 48,
                BackColor = Color.FromArgb(225, 236, 252)
            };
            importButton.Click += async (_, __) => await ImportGithubTreeAsync();
            buttons.Controls.Add(exportButton);
            buttons.Controls.Add(importButton);
            layout.Controls.Add(description, 0, 0);
            layout.Controls.Add(buttons, 0, 1);
            tab.Controls.Add(layout);
            tabControl.TabPages.Add(tab);
        }

        private void ExportTreeToRequestedPaths()
        {
            Form1 source = _experimentalView ?? this;
            if (source._document == null)
            {
                MessageBox.Show(this, "The family tree has not loaded, so it cannot be exported.",
                    "Export unavailable", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                source.CaptureAllPeople();
                string json = source._document.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
                var failures = new List<string>();
                string[] destinations =
                {
                    @"C:\Users\161119\source\repos\Pedigree\xwtree.json",
                    @"E:\xwtree.json"
                };
                foreach (string destination in destinations)
                {
                    try
                    {
                        string? folder = Path.GetDirectoryName(destination);
                        if (!string.IsNullOrWhiteSpace(folder)) Directory.CreateDirectory(folder);
                        File.WriteAllText(destination, json, new UTF8Encoding(false));
                    }
                    catch (Exception ex)
                    {
                        failures.Add($"{destination}: {ex.Message}");
                    }
                }

                if (failures.Count == 0)
                {
                    UpdateStatus($"Exported the complete family tree ({source._people.Count} people) to both requested files.");
                    MessageBox.Show(this,
                        "The complete family tree was exported to:\r\n" + string.Join("\r\n", destinations),
                        "Export complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    string message = "Export attempted for both destinations. Errors:\r\n" + string.Join("\r\n", failures);
                    UpdateStatus(message);
                    MessageBox.Show(this, message, "Export incomplete", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                UpdateStatus("Family tree export failed: " + ex.Message);
                MessageBox.Show(this, ex.Message, "Export failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task ImportGithubTreeAsync()
        {
            Form1 target = _experimentalView ?? this;
            if (target._document == null)
            {
                MessageBox.Show(this, "The family tree has not loaded, so GitHub data cannot be imported.",
                    "Import unavailable", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                byte[]? localSampleBytes = null;
                JsonObject? localSample = null;
                string localSamplePath = @"E:\ghtree.txt";
                if (File.Exists(localSamplePath))
                {
                    localSampleBytes = await File.ReadAllBytesAsync(localSamplePath);
                    localSample = ParseGhtree(localSampleBytes, "E:\\ghtree.txt");
                }

                using var response = await GhtreeHttpClient.GetAsync(GhtreeUrl);
                response.EnsureSuccessStatusCode();
                byte[] remoteBytes = await response.Content.ReadAsByteArrayAsync();
                JsonObject incoming = ParseGhtree(remoteBytes, GhtreeUrl);
                JsonObject current = target._document;
                string remoteHash = GhtreeSha256(remoteBytes);
                string remoteArchive = ArchiveGhtreeVersion("github", remoteHash, remoteBytes);
                string? sampleArchive = null;
                string? sampleHash = null;
                if (localSampleBytes != null)
                {
                    sampleHash = GhtreeSha256(localSampleBytes);
                    sampleArchive = ArchiveGhtreeVersion("local-sample", sampleHash, localSampleBytes);
                }

                JsonArray imports = current["imports"] as JsonArray ?? new JsonArray();
                bool alreadyImported = imports.Any(item =>
                    Value(item, "source_url") == GhtreeUrl && Value(item, "sha256") == remoteHash);
                if (alreadyImported)
                {
                    MessageBox.Show(this,
                        $"This exact GitHub version was already imported. The version is archived at:\r\n{remoteArchive}",
                        "Version already imported", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                JsonArray incomingPeople = EnsureArray(incoming, "people");
                JsonArray incomingIdentifiers = EnsureArray(incoming, "identifiers");
                JsonArray incomingRelationships = EnsureArray(incoming, "relationships");
                JsonArray incomingSnapshots = incoming["captured_people"] as JsonArray ?? new JsonArray();
                var existingNames = EnsureArray(current, "people").Select(person => Value(person, "display_name"))
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);
                int newPeopleCount = incomingPeople.Count(person => !existingNames.Contains(Value(person, "display_name")));
                int detailReviewCount = incomingPeople.Count(person =>
                {
                    string name = Value(person, "display_name");
                    JsonNode? existing = EnsureArray(current, "people").FirstOrDefault(candidate =>
                        Value(candidate, "display_name").Equals(name, StringComparison.OrdinalIgnoreCase));
                    return existing != null && !string.IsNullOrWhiteSpace(Value(person, "details")) &&
                        Value(existing, "details") != Value(person, "details");
                });
                var knownCodes = EnsureArray(current, "identifiers").ToDictionary(
                    identifier => Value(identifier, "code"), identifier => Value(identifier, "display_name"),
                    StringComparer.OrdinalIgnoreCase);
                int identifierConflictCount = incomingIdentifiers.Count(identifier =>
                    knownCodes.TryGetValue(Value(identifier, "code"), out string? owner) &&
                    !owner.Equals(Value(identifier, "display_name"), StringComparison.OrdinalIgnoreCase));
                var existingLinks = EnsureArray(current, "relationships").Select(RelationshipKey)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);
                int newRelationships = incomingRelationships.Count(link => !existingLinks.Contains(RelationshipKey(link)));
                int incomingDetailCount = incomingPeople.Count(person => !string.IsNullOrWhiteSpace(Value(person, "details")));
                int snapshotCount = incomingSnapshots.Count(snapshot => !string.IsNullOrWhiteSpace(GhtreeSnapshotNote(snapshot)));
                string sampleStatus = localSample == null
                    ? "E:\\ghtree.txt was not available; the GitHub file will be imported."
                    : JsonNode.DeepEquals(localSample, incoming)
                        ? "The local E:\\ghtree.txt sample matches the GitHub data."
                        : "The local sample differs from GitHub; both files have been archived, and the GitHub version will be imported.";
                string preview =
                    $"GitHub ghtree.json preview\r\n\r\n" +
                    $"People in source: {incomingPeople.Count} ({newPeopleCount} new names)\r\n" +
                    $"Source identifiers: {incomingIdentifiers.Count} ({identifierConflictCount} code ownership conflicts)\r\n" +
                    $"Parent-child links: {incomingRelationships.Count} ({newRelationships} not already present; imported as unverified)\r\n" +
                    $"People with source details: {incomingDetailCount} ({detailReviewCount} existing records have differing details to preserve as notes)\r\n" +
                    $"Captured snapshots with dates/notes/links: {snapshotCount} (preserved as unverified notes)\r\n\r\n" +
                    $"{sampleStatus}\r\n" +
                    $"Version archive: {Path.GetDirectoryName(remoteArchive)}\r\n\r\n" +
                    "Continue with additive import? Existing people, identifiers, and facts will not be overwritten. Conflicting identifiers will be retained as unverified review entries.";
                if (MessageBox.Show(this, preview, "Review GitHub family-tree import",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Information) != DialogResult.Yes)
                    return;

                JsonObject merged = MergeGhtree(current, incoming, remoteHash, remoteArchive);
                target._document = merged;
                target.RefreshPeople();
                target.SynchronizeExperimentalView();
                JsonObject importedSummary = (merged["imports"] as JsonArray)?.LastOrDefault() as JsonObject
                    ?? throw new InvalidDataException("Import metadata was not recorded after merging the GitHub tree.");
                string importSummary = $"Added {Value(importedSummary, "people_added")} people and {Value(importedSummary, "identifiers_added")} identifiers; " +
                    $"kept {Value(importedSummary, "identifier_conflicts")} identifier conflicts for review and added {Value(importedSummary, "relationships_added")} unverified links.";
                UpdateStatus($"Imported GitHub ghtree version {remoteHash[..12]}. {importSummary}");
                string archives = sampleArchive == null
                    ? remoteArchive
                    : remoteArchive + "\r\n" + sampleArchive;
                MessageBox.Show(this,
                    $"Imported GitHub ghtree.json. {importSummary} Captured snapshot details were retained as unverified notes.\r\n\r\n{archives}",
                    "GitHub family tree imported", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                UpdateStatus("GitHub ghtree import failed: " + ex.Message);
                MessageBox.Show(this, ex.Message, "GitHub import failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static JsonObject ParseGhtree(byte[] bytes, string source)
        {
            JsonObject document = JsonNode.Parse(bytes) as JsonObject
                ?? throw new InvalidDataException($"{source} must contain a JSON object.");
            foreach (string key in new[] { "people", "identifiers", "relationships" })
                if (document[key] is not JsonArray)
                    throw new InvalidDataException($"{source} is missing the required '{key}' array.");
            if (EnsureArray(document, "people").Any(person => string.IsNullOrWhiteSpace(Value(person, "display_name"))))
                throw new InvalidDataException($"{source} contains a person without a display_name.");
            JsonArray people = EnsureArray(document, "people");
            var names = people.Select(person => Value(person, "display_name"))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            if (EnsureArray(document, "identifiers").Any(identifier =>
                string.IsNullOrWhiteSpace(Value(identifier, "code")) ||
                string.IsNullOrWhiteSpace(Value(identifier, "display_name")) ||
                !names.Contains(Value(identifier, "display_name"))))
                throw new InvalidDataException($"{source} contains an invalid identifier or an identifier for a person missing from people.");
            foreach (JsonNode? relationship in EnsureArray(document, "relationships"))
                if (!names.Contains(Value(relationship, "child")) || !names.Contains(Value(relationship, "parent")))
                    throw new InvalidDataException($"{source} contains a relationship whose child or parent is missing from people.");
            if (document["spouse_relationships"] is JsonArray spouses)
                foreach (JsonNode? spouse in spouses)
                    if (!names.Contains(Value(spouse, "person1")) || !names.Contains(Value(spouse, "person2")))
                        throw new InvalidDataException($"{source} contains a spouse link whose person is missing from people.");
            return document;
        }

        private static JsonArray GetOrCreateArray(JsonObject document, string key)
        {
            if (document[key] is JsonArray array) return array;
            if (document[key] != null)
                throw new InvalidDataException($"Expected '{key}' to be a JSON array.");
            var created = new JsonArray();
            document[key] = created;
            return created;
        }

        private static string GhtreeSha256(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();

        private static string ArchiveGhtreeVersion(string source, string sha256, byte[] bytes)
        {
            string folder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "source", "repos", "Pedigree", "ghtree-versions");
            Directory.CreateDirectory(folder);
            string path = Path.Combine(folder, $"ghtree-{source}-{sha256}.json");
            if (File.Exists(path))
            {
                if (GhtreeSha256(File.ReadAllBytes(path)) != sha256)
                    throw new IOException($"A different file already exists at the version archive path {path}.");
                return path;
            }
            using var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None);
            stream.Write(bytes);
            return path;
        }

        private static string RelationshipKey(JsonNode? relationship) =>
            string.Join("\u001f", Value(relationship, "child"), Value(relationship, "parent"), Value(relationship, "relation"));

        private static string GhtreeSnapshotNote(JsonNode? snapshot)
        {
            var parts = new List<string>();
            void Add(string label, string value)
            {
                if (!string.IsNullOrWhiteSpace(value)) parts.Add(label + ": " + value.Trim());
            }
            Add("TG/code", Value(snapshot, "tg_number"));
            Add("Born", Value(snapshot, "born"));
            Add("Died", Value(snapshot, "died"));
            Add("Parents", snapshot?["parents"] is JsonArray parents
                ? string.Join(", ", parents.Select(parent => parent?.ToString()).Where(value => !string.IsNullOrWhiteSpace(value))) : "");
            Add("Spouse", snapshot?["spouse"] is JsonArray spouses
                ? string.Join(", ", spouses.Select(spouse => spouse?.ToString()).Where(value => !string.IsNullOrWhiteSpace(value)))
                : Value(snapshot, "spouse"));
            Add("Children", snapshot?["children"] is JsonArray children
                ? string.Join(", ", children.Select(child => child?.ToString()).Where(value => !string.IsNullOrWhiteSpace(value))) : "");
            Add("Details", Value(snapshot, "details"));
            Add("Notes", Value(snapshot, "notes"));
            if (snapshot?["record_override"] is JsonNode recordOverride)
                Add("Record override", recordOverride.ToJsonString());
            return string.Join("; ", parts);
        }

        private JsonObject MergeGhtree(JsonObject current, JsonObject incoming, string sha256, string archivePath)
        {
            JsonObject merged = current.DeepClone() as JsonObject
                ?? throw new InvalidDataException("Could not clone the current family tree.");
            JsonArray people = EnsureArray(merged, "people");
            JsonArray identifiers = EnsureArray(merged, "identifiers");
            JsonArray relationships = EnsureArray(merged, "relationships");
            JsonArray overrides = GetOrCreateArray(merged, "record_overrides");
            JsonArray notes = GetOrCreateArray(merged, "person_notes");
            int nextPersonNumber = people.Select(person => int.TryParse(Value(person, "person_no"), out int number) ? number : 0)
                .DefaultIfEmpty(0).Max() + 1;
            int addedPeople = 0, addedIdentifiers = 0, identifierConflicts = 0, addedRelationships = 0, addedNotes = 0;
            var newPeopleNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var existingNames = people.Select(person => Value(person, "display_name"))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            var incomingIdentifiers = EnsureArray(incoming, "identifiers");
            foreach (JsonNode? sourcePerson in EnsureArray(incoming, "people"))
            {
                string name = Value(sourcePerson, "display_name");
                JsonNode? existing = people.FirstOrDefault(person =>
                    Value(person, "display_name").Equals(name, StringComparison.OrdinalIgnoreCase));
                if (existing == null)
                {
                    JsonObject added = sourcePerson?.DeepClone() as JsonObject
                        ?? throw new InvalidDataException($"Could not copy incoming person '{name}'.");
                    added["person_no"] = nextPersonNumber++;
                    added["import_source_url"] = GhtreeUrl;
                    added["import_source_sha256"] = sha256;
                    people.Add(added);
                    existingNames.Add(name);
                    newPeopleNames.Add(name);
                    addedPeople++;
                    continue;
                }

                string sourceDetails = Value(sourcePerson, "details");
                if (string.IsNullOrWhiteSpace(sourceDetails) || sourceDetails == Value(existing, "details")) continue;
                if (!notes.Any(note => Value(note, "display_name").Equals(name, StringComparison.OrdinalIgnoreCase) &&
                    Value(note, "source_sha256") == sha256 && Value(note, "note") == sourceDetails))
                {
                    notes.Add(new JsonObject
                    {
                        ["display_name"] = name,
                        ["note"] = "Unverified details from GitHub ghtree.json: " + sourceDetails,
                        ["source_url"] = GhtreeUrl,
                        ["source_sha256"] = sha256,
                        ["verified"] = false
                    });
                    addedNotes++;
                }
            }

            if (incoming["captured_people"] is JsonArray snapshots)
            {
                foreach (JsonNode? snapshot in snapshots)
                {
                    string name = Value(snapshot, "display_name");
                    string snapshotNote = GhtreeSnapshotNote(snapshot);
                    if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(snapshotNote) || !existingNames.Contains(name))
                        continue;
                    if (notes.Any(note => Value(note, "display_name").Equals(name, StringComparison.OrdinalIgnoreCase) &&
                        Value(note, "source_sha256") == sha256 && Value(note, "note") == snapshotNote))
                        continue;
                    notes.Add(new JsonObject
                    {
                        ["display_name"] = name,
                        ["note"] = "Unverified data from captured ghtree snapshot: " + snapshotNote,
                        ["source_url"] = GhtreeUrl,
                        ["source_sha256"] = sha256,
                        ["verified"] = false
                    });
                    addedNotes++;
                }
            }

            var existingCodes = identifiers.ToDictionary(
                identifier => Value(identifier, "code"), identifier => Value(identifier, "display_name"),
                StringComparer.OrdinalIgnoreCase);
            foreach (JsonNode? sourceIdentifier in incomingIdentifiers)
            {
                string code = Value(sourceIdentifier, "code"), name = Value(sourceIdentifier, "display_name");
                if (existingCodes.TryGetValue(code, out string? owner))
                {
                    if (owner.Equals(name, StringComparison.OrdinalIgnoreCase)) continue;
                    overrides.Add(new JsonObject
                    {
                        ["display_name"] = name,
                        ["code"] = code,
                        ["field"] = "ghtree_identifier_conflict",
                        ["original_value"] = owner,
                        ["value"] = name,
                        ["source"] = GhtreeUrl,
                        ["source_sha256"] = sha256,
                        ["reason"] = "Incoming identifier is already assigned to a different person; preserved for review and not reassigned.",
                        ["verified"] = false
                    });
                    identifierConflicts++;
                    continue;
                }

                JsonObject identifier = sourceIdentifier?.DeepClone() as JsonObject
                    ?? throw new InvalidDataException($"Could not copy identifier '{code}'.");
                identifiers.Add(identifier);
                existingCodes.Add(code, name);
                addedIdentifiers++;
            }

            var usedCodes = identifiers.Select(identifier => Value(identifier, "code"))
                .Concat(people.Select(person => Value(person, "uf_number")))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            int nextUf = 0;
            foreach (string name in newPeopleNames)
            {
                if (identifiers.Any(identifier => Value(identifier, "display_name").Equals(name, StringComparison.OrdinalIgnoreCase)))
                    continue;
                string code;
                do code = $"UF{nextUf++:D6}"; while (usedCodes.Contains(code));
                identifiers.Add(new JsonObject
                {
                    ["display_name"] = name, ["code"] = code, ["kind"] = "UF", ["permanent"] = 0,
                    ["source_url"] = GhtreeUrl, ["source_sha256"] = sha256
                });
                usedCodes.Add(code);
                addedIdentifiers++;
            }

            var linkKeys = relationships.Select(RelationshipKey).ToHashSet(StringComparer.OrdinalIgnoreCase);
            foreach (JsonNode? sourceRelationship in EnsureArray(incoming, "relationships"))
            {
                if (string.IsNullOrWhiteSpace(Value(sourceRelationship, "child")) ||
                    string.IsNullOrWhiteSpace(Value(sourceRelationship, "parent")))
                    throw new InvalidDataException("An incoming relationship is missing a child or parent name.");
                string key = RelationshipKey(sourceRelationship);
                if (!linkKeys.Add(key)) continue;
                JsonObject relationship = sourceRelationship?.DeepClone() as JsonObject
                    ?? throw new InvalidDataException("Could not copy an incoming relationship.");
                if (!double.TryParse(Value(relationship, "confidence"), System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out double confidence)) confidence = 0.5;
                relationship["confidence"] = Math.Clamp(confidence, 0, 0.5);
                relationship["permanent"] = 0;
                relationship["status"] = "unverified";
                relationship["source_url"] = GhtreeUrl;
                relationship["source_sha256"] = sha256;
                relationships.Add(relationship);
                addedRelationships++;
            }

            foreach (string key in new[] { "aliases", "provisional_facts", "duplicate_records", "duplicate_reviews" })
            {
                if (incoming[key] is not JsonArray sourceArray || sourceArray.Count == 0) continue;
                JsonArray targetArray = GetOrCreateArray(merged, key);
                foreach (JsonNode? item in sourceArray)
                    if (!targetArray.Any(existing => JsonNode.DeepEquals(existing, item)))
                        targetArray.Add(item?.DeepClone());
            }

            if (incoming["spouse_relationships"] is JsonArray incomingSpouses)
            {
                JsonArray spouses = GetOrCreateArray(merged, "spouse_relationships");
                foreach (JsonNode? sourceSpouse in incomingSpouses)
                    if (!spouses.Any(existing => JsonNode.DeepEquals(existing, sourceSpouse)))
                    {
                        JsonObject spouse = sourceSpouse?.DeepClone() as JsonObject
                            ?? throw new InvalidDataException("Could not copy an incoming spouse relationship.");
                        spouse["permanent"] = 0;
                        spouse["status"] = "unverified";
                        spouse["source_url"] = GhtreeUrl;
                        spouse["source_sha256"] = sha256;
                        spouses.Add(spouse);
                    }
            }

            JsonArray imports = GetOrCreateArray(merged, "imports");
            imports.Add(new JsonObject
            {
                ["source_url"] = GhtreeUrl,
                ["sha256"] = sha256,
                ["archive_path"] = archivePath,
                ["imported_at_utc"] = DateTime.UtcNow.ToString("O"),
                ["people_added"] = addedPeople,
                ["identifiers_added"] = addedIdentifiers,
                ["identifier_conflicts"] = identifierConflicts,
                ["relationships_added"] = addedRelationships,
                ["detail_notes_added"] = addedNotes,
                ["verified"] = false
            });
            return merged;
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
            tabControl.TabPages.Remove(mainTabPage);
            tabControl.SelectedTab = experimentalTabPage;
        }

        private void InitializeView()
        {
            BackColor = Color.FromArgb(245, 249, 255);
            headerPanel.BackColor = Color.FromArgb(218, 232, 252);
            footerPanel.BackColor = Color.FromArgb(235, 242, 250);
            memberPanel.BackColor = detailsPanel.BackColor = exportsPanel.BackColor = Color.White;
            titleLabel.ForeColor = Color.FromArgb(41, 74, 122);
            subtitleLabel.ForeColor = Color.FromArgb(71, 94, 128);
            familyListBox.DrawMode = DrawMode.OwnerDrawFixed;
            familyListBox.DrawItem -= familyListBox_DrawItem;
            familyListBox.DrawItem += familyListBox_DrawItem;
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
            _experimentalView._mainViewOwner = this;
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

        private void InitializeEditTab()
        {
            var tab = new TabPage("Edit") { BackColor = Color.White };
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(18),
                ColumnCount = 1,
                RowCount = 3
            };
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 58F));
            var header = new Label
            {
                Dock = DockStyle.Fill,
                Text = "Edit family records. Original built-in records remain preserved; changes are saved as overrides.",
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI", 10F)
            };
            var body = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2 };
            body.RowStyles.Add(new RowStyle(SizeType.Absolute, 38F));
            body.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            _editSearchTextBox = new TextBox { Dock = DockStyle.Fill, PlaceholderText = "Search name or TG" };
            _editSearchTextBox.TextChanged += (_, __) => RefreshEditPeople();
            _editPeopleListBox = new ListBox { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 11F) };
            _editPeopleListBox.DoubleClick += (_, __) => EditSelectedPerson();
            body.Controls.Add(_editSearchTextBox, 0, 0);
            body.Controls.Add(_editPeopleListBox, 0, 1);
            var actions = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft };
            var editButton = new Button { Text = "Edit Selected Record", Width = 180, Height = 40 };
            editButton.Click += (_, __) => EditSelectedPerson();
            actions.Controls.Add(editButton);
            layout.Controls.Add(header, 0, 0);
            layout.Controls.Add(body, 0, 1);
            layout.Controls.Add(actions, 0, 2);
            tab.Controls.Add(layout);
            tabControl.TabPages.Add(tab);
        }

        private void InitializeUpdateWorkspaceTab()
        {
            var tab = new TabPage("Update Files") { BackColor = Color.White };
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(24),
                ColumnCount = 1,
                RowCount = 5
            };
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 90F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 66F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 66F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 66F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            var description = new Label
            {
                Dock = DockStyle.Fill,
                Text = "Create a portable full-tree update workspace, then import a completed new.json. " +
                    "The import replaces the in-memory tree only after validating that it contains the complete existing family.",
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI", 11F)
            };
            var createButton = new Button
            {
                Text = "Create Update Files",
                Dock = DockStyle.Left,
                Width = 220,
                Height = 48,
                BackColor = Color.FromArgb(225, 236, 252)
            };
            createButton.Click += (_, __) => CreateUpdateWorkspace();
            var importButton = new Button
            {
                Text = "Import new.json",
                Dock = DockStyle.Left,
                Width = 220,
                Height = 48,
                BackColor = Color.FromArgb(225, 236, 252)
            };
            importButton.Click += (_, __) => ImportUpdatedTree();
            var exportButton = new Button
            {
                Text = "Export Current Tree As...",
                Dock = DockStyle.Left,
                Width = 220,
                Height = 48,
                BackColor = Color.FromArgb(225, 236, 252)
            };
            exportButton.Click += (_, __) => ExportCurrentTreeToFile();
            _updateWorkspaceStatusLabel = new Label
            {
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.TopLeft,
                Font = new Font("Segoe UI", 10F),
                Text = "The package destination is E:\\11-18-2026. A separate Linux import location is documented in readme.txt."
            };
            layout.Controls.Add(description, 0, 0);
            layout.Controls.Add(createButton, 0, 1);
            layout.Controls.Add(importButton, 0, 2);
            layout.Controls.Add(exportButton, 0, 3);
            layout.Controls.Add(_updateWorkspaceStatusLabel, 0, 4);
            tab.Controls.Add(layout);
            tabControl.TabPages.Add(tab);
        }

        private void InitializeResearchReviewTab()
        {
            var tab = new TabPage("Research Review") { BackColor = Color.White };
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(18),
                ColumnCount = 1,
                RowCount = 2
            };
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 150F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            _researchReviewSummaryTextBox = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                WordWrap = true,
                Font = new Font("Segoe UI", 10F),
                BackColor = Color.White
            };
            _researchReviewGrid = new DataGridView
            {
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White,
                Dock = DockStyle.Fill,
                ReadOnly = true,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells
            };
            _researchReviewGrid.Columns.Add("Code", "TG / Code");
            _researchReviewGrid.Columns.Add("Person", "Person");
            _researchReviewGrid.Columns.Add("Review", "Research review (UNVERIFIED; not a canonical fact)");
            _researchReviewGrid.Columns[0].FillWeight = 12;
            _researchReviewGrid.Columns[1].FillWeight = 20;
            _researchReviewGrid.Columns[2].FillWeight = 68;
            _researchReviewGrid.Columns[2].DefaultCellStyle.WrapMode = DataGridViewTriState.True;
            layout.Controls.Add(_researchReviewSummaryTextBox, 0, 0);
            layout.Controls.Add(_researchReviewGrid, 0, 1);
            tab.Controls.Add(layout);
            tabControl.TabPages.Add(tab);
        }

        private void RefreshResearchReviewTab()
        {
            if (_researchReviewGrid == null || _researchReviewSummaryTextBox == null) return;
            _researchReviewGrid.Rows.Clear();
            if (_document?["record_overrides"] is JsonArray overrides)
            {
                foreach (JsonNode? item in overrides.Where(item =>
                    Value(item, "field") == "research_review_2026_11_18"))
                {
                    string summary = "UNVERIFIED RESEARCH LEAD - " + Value(item, "status") + Environment.NewLine +
                        Value(item, "value");
                    string source = Value(item, "source");
                    string reason = Value(item, "reason");
                    if (!string.IsNullOrWhiteSpace(source)) summary += Environment.NewLine + "Source: " + source;
                    if (!string.IsNullOrWhiteSpace(reason)) summary += Environment.NewLine + "Review note: " + reason;
                    _researchReviewGrid.Rows.Add(Value(item, "code"), Value(item, "display_name"), summary);
                }
            }

            JsonNode? followup = _document?["update_workflow"]?["followup_research_2026_11_18"];
            if (followup == null)
            {
                _researchReviewSummaryTextBox.Text = "No 18 Nov 2026 follow-up research review is present in this tree.\r\n" +
                    "Research notes in this tab are display-only leads; they do not establish facts or relationships.";
                return;
            }

            var summaryLines = new List<string>
            {
                "FOLLOW-UP RESEARCH REVIEW - UNVERIFIED; NO CANDIDATE RELATIONSHIPS ARE AUTOMATICALLY PROMOTED",
                "Status: " + Value(followup, "review_status"),
                "Review date: " + Value(followup, "reviewed_at"),
                $"Reviewed TG records: {((followup["target_codes"] as JsonArray)?.Count.ToString() ?? "not specified")}"
            };
            foreach (JsonNode? discrepancy in followup["code_discrepancies"] as JsonArray ?? new JsonArray())
                summaryLines.Add($"Identifier warning: {Value(discrepancy, "name")}: supplied {Value(discrepancy, "supplied")}; existing {Value(discrepancy, "existing")}. {Value(discrepancy, "action")}");
            foreach (JsonNode? issue in followup["chronological_flags"] as JsonArray ?? new JsonArray())
                summaryLines.Add($"Chronology/identity warning {Value(issue, "code")}: {Value(issue, "issue")}");
            if (followup["request_to_copilot"] is JsonValue request && request.TryGetValue<string>(out string? requestText) && !string.IsNullOrWhiteSpace(requestText))
                summaryLines.Add("Review instructions: " + requestText);
            _researchReviewSummaryTextBox.Text = string.Join(Environment.NewLine, summaryLines);
        }

        private void EditSelectedPerson()
        {
            if (_editPeopleListBox?.SelectedItem is not PersonRow person)
            {
                MessageBox.Show(this, "Choose a record to edit.", "Edit family record", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            EditPerson(person, this);
        }

        private void RefreshEditPeople(string? selectName = null)
        {
            if (_editPeopleListBox == null) return;
            string search = _editSearchTextBox?.Text ?? "";
            _editPeopleListBox.BeginUpdate();
            _editPeopleListBox.Items.Clear();
            foreach (PersonRow person in _people
                .Where(person => string.IsNullOrWhiteSpace(search) ||
                    person.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    person.Tg.Contains(search, StringComparison.OrdinalIgnoreCase))
                .OrderBy(person => person.Name, StringComparer.OrdinalIgnoreCase))
                _editPeopleListBox.Items.Add(person);
            if (_editPeopleListBox.Items.Count > 0)
            {
                int selected = 0;
                if (!string.IsNullOrWhiteSpace(selectName))
                    for (int i = 0; i < _editPeopleListBox.Items.Count; i++)
                        if (((PersonRow)_editPeopleListBox.Items[i]).Name.Equals(selectName, StringComparison.OrdinalIgnoreCase))
                        {
                            selected = i;
                            break;
                        }
                _editPeopleListBox.SelectedIndex = selected;
            }
            _editPeopleListBox.EndUpdate();
        }

        private static string UpdateWorkspaceWindowsPath() => @"E:\11-18-2026";

        private void CreateUpdateWorkspace()
        {
            if (_document == null)
            {
                MessageBox.Show(this, "The family tree has not loaded, so update files cannot be created.",
                    "Update files unavailable", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string sourceChanges = @"E:\changes18z.txt";
            if (!File.Exists(sourceChanges))
            {
                MessageBox.Show(this, $"The required change instructions were not found:\n{sourceChanges}",
                    "Update files unavailable", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                string folder = UpdateWorkspaceWindowsPath();
                Directory.CreateDirectory(folder);
                CaptureAllPeople();
                JsonObject original = _document.DeepClone() as JsonObject
                    ?? throw new InvalidDataException("Could not clone the family tree JSON.");
                JsonObject starter = original.DeepClone() as JsonObject
                    ?? throw new InvalidDataException("Could not create the new.json starter.");
                starter["update_workflow"] = new JsonObject
                {
                    ["status"] = "starter - apply changes18z.txt before importing",
                    ["source_file"] = "changes18z.txt",
                    ["instructions"] = "Apply every applicable instruction from changes18z.txt to this complete tree. Preserve every person, identifier, relationship, and unknown metadata. Do not mark uncertain facts as verified."
                };

                var options = new JsonSerializerOptions { WriteIndented = true };
                File.WriteAllText(Path.Combine(folder, "old.json"), original.ToJsonString(options), new UTF8Encoding(false));
                File.WriteAllText(Path.Combine(folder, "new.json"), starter.ToJsonString(options), new UTF8Encoding(false));
                File.WriteAllText(Path.Combine(folder, "structure.txt"), UpdateWorkspaceStructure(), new UTF8Encoding(false));
                File.WriteAllText(Path.Combine(folder, "readme.txt"), UpdateWorkspaceReadme(), new UTF8Encoding(false));
                File.Copy(sourceChanges, Path.Combine(folder, "changes18z.txt"), true);

                string message = $"Created old.json, new.json (starter copy), structure.txt, readme.txt, and changes18z.txt in {folder}. " +
                    "Have ChatGPT apply the changes before importing new.json.";
                UpdateStatus(message);
                if (_updateWorkspaceStatusLabel != null) _updateWorkspaceStatusLabel.Text = message;
                MessageBox.Show(this, message, "Update files created", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                if (_updateWorkspaceStatusLabel != null) _updateWorkspaceStatusLabel.Text = "Could not create update files: " + ex.Message;
                MessageBox.Show(this, ex.Message, "Could not create update files", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static string UpdateWorkspaceStructure() =>
            "Pedigree full-tree update package structure\r\n" +
            "=============================================\r\n\r\n" +
            "old.json and new.json are complete JSON documents. new.json begins as a copy of old.json and is only a starter until changes18z.txt has been applied.\r\n\r\n" +
            "Top-level fields:\r\n" +
            "- people: array of original and added person objects. Existing objects retain their fields, including person_no, display_name, details, verify, and other source metadata.\r\n" +
            "- identifiers: array mapping display_name to code and kind (including TG and AKA_TE identifiers). Do not change existing identifiers.\r\n" +
            "- relationships: array of parent-child links, typically child, parent, relation, confidence, and permanent.\r\n" +
            "- spouse_relationships: array of spouse links, person1, person2, relation, and source/permanence metadata.\r\n" +
            "- provisional_facts: array of sourced or user-provided facts. Preserve status, confidence, source, and permanence fields.\r\n" +
            "- person_notes, project_references, record_overrides, duplicate_records, duplicate_reviews, and other top-level arrays: optional application data; preserve them even when not editing them.\r\n" +
            "- update_workflow: package-generation notes; it is not a family fact.\r\n\r\n" +
            "The app imports new.json as a complete tree, not as a partial update package. Required arrays are people, identifiers, and relationships. The import is rejected unless all current original people and identifier codes are retained.\r\n";

        private static string UpdateWorkspaceReadme() =>
            "Pedigree family tree update workspace\r\n" +
            "======================================\r\n\r\n" +
            "Windows app location: E:\\11-18-2026\\\r\n" +
            "The application writes old.json, new.json (an unapplied starter copy), structure.txt, readme.txt, and a copy of changes18z.txt there. The E: location is specific to the Windows development machine.\r\n\r\n" +
            "Deployment/container location: /opt/11-18-2026/ (not E:\\). When working in the Linux deployment/container, stage the package files under /opt/11-18-2026/; do not assume the Windows E: drive exists there.\r\n\r\n" +
            "Files:\r\n" +
            "- old.json: complete baseline tree exported from the running program. Keep an untouched copy.\r\n" +
            "- changes18z.txt: user-provided update instructions and genealogical information.\r\n" +
            "- structure.txt: JSON schema overview and import requirements.\r\n" +
            "- new.json: starter copy only. It is not updated merely by creating this workspace.\r\n" +
            "- readme.txt: this workflow guide.\r\n\r\n" +
            "Instructions for ChatGPT: read old.json, changes18z.txt, and structure.txt. Apply the requested changes to a full copy of old.json, preserving all family members, original identifiers, all relationship and supplemental arrays, and unknown metadata. Do not invent missing facts or present uncertain dates as verified. Keep original records and express user edits using the existing record_overrides pattern where appropriate. Retain duplicate records and mark them for export rather than silently deleting them. Save the finished complete tree as /home/we6jbo/11-19-2026/new.json. Do not overwrite old.json. The file produced by this app at E:\\11-18-2026\\new.json is only a starter.\r\n\r\n" +
            "Instructions for ChatGPT to create the handoff note: write /home/we6jbo/11-19-2026/share-to-github-copilot.txt. Explain that the user should attach or paste old.json, the completed new.json, changes18z.txt, structure.txt, and this readme.txt in a Copilot conversation, and explicitly ask Copilot to review/import the new full tree and implement any required program changes. The user must actually attach the files; Copilot cannot read files merely because they exist on the user's computer. If the ChatGPT environment cannot write to /home/we6jbo/11-19-2026/, provide share-to-github-copilot.txt as downloadable text for the user to save there.\r\n\r\n" +
            "To import: copy the completed new.json to the Windows machine or otherwise make it accessible, then use the app's Update Files tab > Import new.json and select it. Import validates required arrays and checks that existing family members and identifier codes have not been omitted. A successful import replaces the program's in-memory tree; use Export JSON afterward to save a program copy.\r\n";

        private void ExportCurrentTreeToFile()
        {
            if (_document == null)
            {
                MessageBox.Show(this, "The family tree has not loaded, so it cannot be exported.",
                    "Export unavailable", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            using var dialog = new SaveFileDialog
            {
                Title = "Export the complete current family tree",
                Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                InitialDirectory = Directory.Exists(UpdateWorkspaceWindowsPath()) ? UpdateWorkspaceWindowsPath() : "",
                FileName = "new-research-review-imported.json",
                OverwritePrompt = true
            };
            if (dialog.ShowDialog(this) != DialogResult.OK) return;
            try
            {
                CaptureAllPeople();
                File.WriteAllText(dialog.FileName,
                    _document.ToJsonString(new JsonSerializerOptions { WriteIndented = true }), new UTF8Encoding(false));
                string message = $"Exported complete current tree with research review metadata to {dialog.FileName}.";
                UpdateStatus(message);
                if (_updateWorkspaceStatusLabel != null) _updateWorkspaceStatusLabel.Text = message;
                MessageBox.Show(this, message, "Tree exported", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                if (_updateWorkspaceStatusLabel != null) _updateWorkspaceStatusLabel.Text = "Export failed: " + ex.Message;
                MessageBox.Show(this, ex.Message, "Could not export tree", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ImportUpdatedTree()
        {
            using var dialog = new OpenFileDialog
            {
                Title = "Select the completed full-tree JSON or research-review candidate",
                Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                InitialDirectory = Directory.Exists(UpdateWorkspaceWindowsPath()) ? UpdateWorkspaceWindowsPath() : ""
            };
            if (dialog.ShowDialog(this) != DialogResult.OK) return;

            try
            {
                if (_document == null)
                    throw new InvalidOperationException("The current family tree is not loaded.");
                JsonObject incoming = JsonNode.Parse(File.ReadAllText(dialog.FileName)) as JsonObject
                    ?? throw new InvalidDataException("new.json must contain a JSON object.");
                foreach (string key in new[] { "people", "identifiers", "relationships" })
                    EnsureArray(incoming, key);
                foreach (string key in new[] { "record_overrides", "duplicate_records", "duplicate_reviews" })
                    if (incoming[key] is not JsonArray) incoming[key] = new JsonArray();

                JsonArray people = EnsureArray(incoming, "people");
                JsonArray identifiers = EnsureArray(incoming, "identifiers");
                if (people.Count == 0)
                    throw new InvalidDataException("new.json contains no people.");
                if (people.Any(person => string.IsNullOrWhiteSpace(Value(person, "display_name"))))
                    throw new InvalidDataException("Every person in new.json must have a display_name.");
                if (identifiers.Any(identifier => string.IsNullOrWhiteSpace(Value(identifier, "code")) ||
                    string.IsNullOrWhiteSpace(Value(identifier, "display_name"))))
                    throw new InvalidDataException("Every identifier must have both code and display_name.");

                foreach (JsonNode? originalPerson in PeopleArray)
                {
                    string originalName = Value(originalPerson, "display_name");
                    JsonNode? candidatePerson = people.FirstOrDefault(person =>
                        Value(person, "display_name").Equals(originalName, StringComparison.OrdinalIgnoreCase));
                    if (candidatePerson == null)
                        throw new InvalidDataException($"new.json is missing original family member '{originalName}'. Keep every original person record in people.");
                    string originalNumber = Value(originalPerson, "person_no");
                    if (!string.IsNullOrWhiteSpace(originalNumber) && Value(candidatePerson, "person_no") != originalNumber)
                        throw new InvalidDataException($"Original person_no for '{originalName}' changed. Preserve original person_no values.");
                    string candidateDetails = Value(candidatePerson, "details");
                    if (candidateDetails != Value(originalPerson, "details") &&
                        (candidateDetails.Contains("â", StringComparison.Ordinal) || candidateDetails.Contains("Ã", StringComparison.Ordinal) || candidateDetails.Contains('\uFFFD')))
                        throw new InvalidDataException($"Details for '{originalName}' appear to have text-encoding corruption. Restore the original text from old.json before importing.");
                }
                foreach (JsonNode? originalIdentifier in IdentifierArray)
                {
                    string originalCode = Value(originalIdentifier, "code");
                    if (string.IsNullOrWhiteSpace(originalCode)) continue;
                    JsonNode? importedIdentifier = identifiers.FirstOrDefault(identifier =>
                        Value(identifier, "code").Equals(originalCode, StringComparison.OrdinalIgnoreCase));
                    if (importedIdentifier == null)
                        throw new InvalidDataException($"new.json is missing original identifier '{originalCode}'. Existing identifiers must be preserved.");
                    if (!Value(importedIdentifier, "display_name").Equals(Value(originalIdentifier, "display_name"), StringComparison.OrdinalIgnoreCase))
                        throw new InvalidDataException($"Original identifier '{originalCode}' was reassigned. Existing identifier ownership must be preserved.");
                }

                JsonArray relationships = EnsureArray(incoming, "relationships");
                foreach (JsonNode? relationship in relationships)
                {
                    string parent = Value(relationship, "parent"), child = Value(relationship, "child");
                    if (string.IsNullOrWhiteSpace(parent) || string.IsNullOrWhiteSpace(child))
                        throw new InvalidDataException("Every relationship must specify child and parent.");
                }

                static string RelationshipKey(JsonNode? relationship) =>
                    string.Join("\u001f", Value(relationship, "child"), Value(relationship, "parent"), Value(relationship, "relation"));
                var baselineRelationshipKeys = RelationshipArray.Select(RelationshipKey).ToHashSet(StringComparer.OrdinalIgnoreCase);
                var candidateRelationshipKeys = relationships.Select(RelationshipKey).ToHashSet(StringComparer.OrdinalIgnoreCase);
                foreach (JsonNode? baselineRelationship in RelationshipArray)
                {
                    string baselineLink = RelationshipKey(baselineRelationship);
                    JsonNode? candidateLink = relationships.FirstOrDefault(relationship =>
                        RelationshipKey(relationship).Equals(baselineLink, StringComparison.OrdinalIgnoreCase));
                    if (candidateLink == null)
                        throw new InvalidDataException("new.json omits an existing parent-child link. Original relationships must be retained.");
                    if (!JsonNode.DeepEquals(baselineRelationship, candidateLink))
                        throw new InvalidDataException($"An existing parent-child link was modified: {Value(baselineRelationship, "parent")} -> {Value(baselineRelationship, "child")}. Preserve the original relationship row and review candidates separately.");
                }

                bool isResearchReviewCandidate = incoming["update_workflow"]?["followup_research_2026_11_18"] != null;
                if (isResearchReviewCandidate && candidateRelationshipKeys.Except(baselineRelationshipKeys, StringComparer.OrdinalIgnoreCase).Any())
                    throw new InvalidDataException("This research-review candidate adds parent-child links. The review requires that candidate relationships remain unpromoted; review and confirm links separately before importing.");

                JsonArray baselineSpouses = _document?["spouse_relationships"] as JsonArray ?? new JsonArray();
                JsonArray candidateSpouses = incoming["spouse_relationships"] as JsonArray ?? new JsonArray();
                static string SpouseKey(JsonNode? spouse) =>
                    string.Join("\u001f", Value(spouse, "person1"), Value(spouse, "person2"));
                var candidateSpouseKeys = candidateSpouses.Select(SpouseKey).ToHashSet(StringComparer.OrdinalIgnoreCase);
                foreach (JsonNode? baselineSpouse in baselineSpouses)
                {
                    string baselineSpouseKey = SpouseKey(baselineSpouse);
                    JsonNode? candidateSpouse = candidateSpouses.FirstOrDefault(spouse =>
                        SpouseKey(spouse).Equals(baselineSpouseKey, StringComparison.OrdinalIgnoreCase));
                    if (candidateSpouse == null)
                        throw new InvalidDataException("new.json omits an existing spouse link. Original spouse relationships must be retained.");
                    if (!JsonNode.DeepEquals(baselineSpouse, candidateSpouse))
                        throw new InvalidDataException("An existing spouse relationship row was modified. Preserve its original values during research review.");
                }
                if (isResearchReviewCandidate)
                {
                    var baselineSpouseKeys = baselineSpouses.Select(SpouseKey).ToHashSet(StringComparer.OrdinalIgnoreCase);
                    if (candidateSpouseKeys.Except(baselineSpouseKeys, StringComparer.OrdinalIgnoreCase).Any())
                        throw new InvalidDataException("This research-review candidate adds spouse links. Research leads must not be automatically promoted to relationships.");
                    foreach (JsonNode? review in EnsureArray(incoming, "record_overrides")
                        .Where(item => Value(item, "field") == "research_review_2026_11_18"))
                    {
                        if (review?["verified"] is not JsonValue verifiedValue ||
                            !verifiedValue.TryGetValue<bool>(out bool verified) || verified)
                            throw new InvalidDataException("Research review entries must remain explicitly unverified.");
                    }
                }

                _document = incoming;
                RefreshPeople();
                SynchronizeExperimentalView();
                string message = $"Imported complete updated tree from {dialog.FileName}. The program now has {_people.Count} visible family members.";
                UpdateStatus(message);
                if (_updateWorkspaceStatusLabel != null) _updateWorkspaceStatusLabel.Text = message;
                MessageBox.Show(this, message, "Updated tree imported", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                if (_updateWorkspaceStatusLabel != null) _updateWorkspaceStatusLabel.Text = "Import failed; current family data was not replaced: " + ex.Message;
                MessageBox.Show(this, ex.Message, "Could not import updated tree", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
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

        private bool IsFamilyMatchComplete(string name)
        {
            if (!_isExperimental || _matchSettings?["matches"] is not JsonArray records)
                return false;

            JsonNode? record = records.FirstOrDefault(item =>
                Value(item, "display_name").Equals(name, StringComparison.OrdinalIgnoreCase));
            return Flag(record, "matched");
        }

        private void familyListBox_DrawItem(object? sender, DrawItemEventArgs e)
        {
            if (e.Index < 0 || e.Index >= familyListBox.Items.Count) return;
            e.DrawBackground();
            var person = (PersonRow)familyListBox.Items[e.Index];
            using var brush = new SolidBrush(e.ForeColor);
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
        private static string NormalizeCode(string code) => code.StartsWith("TH", StringComparison.OrdinalIgnoreCase)
            ? "TG" + code[2..]
            : code;
        private JsonArray PeopleArray => EnsureArray(_document!, "people");
        private JsonArray IdentifierArray => EnsureArray(_document!, "identifiers");
        private JsonArray RelationshipArray => EnsureArray(_document!, "relationships");

        private JsonArray RecordOverrides => _document?["record_overrides"] is JsonArray array
            ? array
            : (_document!["record_overrides"] = new JsonArray()) as JsonArray ?? new JsonArray();

        private JsonArray DuplicateRecords => _document?["duplicate_records"] is JsonArray array
            ? array
            : (_document!["duplicate_records"] = new JsonArray()) as JsonArray ?? new JsonArray();

        private JsonNode? OverrideFor(string originalName)
        {
            if (_document == null) return null;
            return RecordOverrides.FirstOrDefault(record =>
                Value(record, "original_name").Equals(originalName, StringComparison.OrdinalIgnoreCase));
        }

        private static string ReplaceDeathInDetails(string details, string death)
        {
            Match deathMatch = Regex.Match(details, @"\bD:\s*", RegexOptions.IgnoreCase);
            if (deathMatch.Success)
            {
                string prefix = details[..deathMatch.Index].Trim();
                return $"{prefix} D:{death}".Trim();
            }
            return string.IsNullOrWhiteSpace(details) ? $"D:{death}" : $"{details.Trim()} D:{death}";
        }

        private void ApplyChangeRequestStatus(JsonObject document)
        {
            _orangeCodes.Clear(); _blueCodes.Clear(); _purpleCodes.Clear(); _excludedChronologicalNames.Clear();
            foreach (JsonNode? duplicate in DuplicateRecords) { /* populated below */ }

            string[] purpleNames = { "John Elliott", "Margaret Scott", "Patrick Henry Shapley", "Mary Evett", "Marshall" };
            foreach (string name in purpleNames) _excludedChronologicalNames.Add(name);

            string path = @"E:\changes18z.txt";
            if (!File.Exists(path)) return;
            string text = File.ReadAllText(path);
            var codeLines = Regex.Matches(text, @"(?im)^((?:TG|TH)[A-Z0-9]*\d+[A-Z0-9]*)[^\r\n]*");
            for (int i = 0; i < codeLines.Count; i++)
            {
                Match codeLine = codeLines[i];
                string code = NormalizeCode(codeLine.Groups[1].Value);
                int end = i + 1 < codeLines.Count ? codeLines[i + 1].Index : text.Length;
                string block = text.Substring(codeLine.Index, end - codeLine.Index);
                _orangeCodes.Add(code);

                if (block.Contains("***", StringComparison.Ordinal) || code is "TG749502" or "TG182976" or "TG151970" or "TG932996")
                {
                    _purpleCodes.Add(code);
                    Match nameLine = Regex.Match(codeLine.Value, @"(?:TG|TH)\d+\s*(?:\(([^)]+)\)|\s*\(([^)]+)\)|\s*\*+)?", RegexOptions.IgnoreCase);
                    if (nameLine.Success)
                    {
                        string purpleName = nameLine.Groups[1].Success ? nameLine.Groups[1].Value : nameLine.Groups[2].Value;
                        if (!string.IsNullOrWhiteSpace(purpleName)) _excludedChronologicalNames.Add(purpleName);
                    }
                }

                Match death = Regex.Match(block, @"(?im)(?:Died|Death|D):?\s*(.+)", RegexOptions.None);
                if (!death.Success)
                    death = Regex.Match(block, @"(?im)^\s*([0-9]{1,2}\s+[A-Za-z]+\s+[0-9]{4}[^\r\n]*)");
                if (!death.Success) continue;
                string deathText = death.Groups[1].Value.Trim().TrimEnd('*');
                if (string.IsNullOrWhiteSpace(deathText) || deathText.Equals("***", StringComparison.Ordinal)) continue;

                if (Regex.IsMatch(deathText, @"(?i)unknown|none|not indicated|not recorded|before|about|between| or |\*\*\*|\b187-\b"))
                    _blueCodes.Add(code);

                JsonNode? identifier = IdentifierArray.FirstOrDefault(id =>
                    NormalizeCode(Value(id, "code")).Equals(code, StringComparison.OrdinalIgnoreCase));
                if (identifier == null) continue;
                string name = Value(identifier, "display_name");
                if (string.IsNullOrWhiteSpace(name)) continue;

                JsonNode? person = PeopleArray.FirstOrDefault(p =>
                    Value(p, "display_name").Equals(name, StringComparison.OrdinalIgnoreCase));
                if (person != null)
                    person!["details"] = ReplaceDeathInDetails(Value(person, "details"), deathText);

                if (document["provisional_facts"] is not JsonArray facts)
                    document["provisional_facts"] = facts = new JsonArray();
                bool already = facts.Any(f => Value(f, "display_name").Equals(name, StringComparison.OrdinalIgnoreCase) &&
                    Value(f, "field") == "death_description" && Value(f, "value") == deathText);
                if (!already)
                    facts.Add(new JsonObject
                    {
                        ["display_name"] = name,
                        ["field"] = "death_description",
                        ["value"] = deathText,
                        ["confidence"] = 0.55,
                        ["source_url"] = "E:\\changes18z.txt",
                        ["source_title"] = "User-provided family information",
                        ["status"] = "active",
                        ["permanent"] = 0
                    });
            }

            var duplicateNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var group in PeopleArray
                .Where(person => !string.IsNullOrWhiteSpace(Value(person, "display_name")))
                .GroupBy(person => Value(person, "display_name") + "\u001f" + Value(person, "details"), StringComparer.OrdinalIgnoreCase)
                .Where(group => group.Count() > 1))
                foreach (JsonNode? person in group)
                    duplicateNames.Add(Value(person, "display_name"));
            foreach (var group in IdentifierArray
                .Where(identifier => Value(identifier, "kind") == "TG" && !string.IsNullOrWhiteSpace(Value(identifier, "code")))
                .GroupBy(identifier => NormalizeCode(Value(identifier, "code")), StringComparer.OrdinalIgnoreCase)
                .Where(group => group.Select(identifier => Value(identifier, "display_name")).Distinct(StringComparer.OrdinalIgnoreCase).Count() > 1))
                foreach (JsonNode? identifier in group)
                    duplicateNames.Add(Value(identifier, "display_name"));
            if (PeopleArray.Any(p => Value(p, "display_name").Equals("mom", StringComparison.OrdinalIgnoreCase)) &&
                PeopleArray.Any(p => Value(p, "display_name").Equals("Dudley McCabe", StringComparison.OrdinalIgnoreCase)))
            {
                JsonArray spouses = document["spouse_relationships"] is JsonArray existingSpouses
                    ? existingSpouses : (document["spouse_relationships"] = new JsonArray()) as JsonArray;
                if (!spouses.Any(r => (Value(r, "person1") == "mom" && Value(r, "person2") == "Dudley McCabe") ||
                                      (Value(r, "person1") == "Dudley McCabe" && Value(r, "person2") == "mom")))
                    spouses.Add(new JsonObject { ["person1"] = "mom", ["person2"] = "Dudley McCabe", ["relation"] = "spouse", ["permanent"] = 1 });
            }

            JsonNode? amelia = PeopleArray.FirstOrDefault(p => Value(p, "display_name").Equals("Amelia Carolina Luedtke", StringComparison.OrdinalIgnoreCase));
            if (amelia != null)
            {
                JsonArray spouses = document["spouse_relationships"] is JsonArray existingSpouses
                    ? existingSpouses : (document["spouse_relationships"] = new JsonArray()) as JsonArray;
                if (!spouses.Any(r => (Value(r, "person1") == "Amelia Carolina Luedtke" && Value(r, "person2") == "August Friedrich Hedtke") ||
                                      (Value(r, "person1") == "August Friedrich Hedtke" && Value(r, "person2") == "Amelia Carolina Luedtke")))
                    spouses.Add(new JsonObject { ["person1"] = "Amelia Carolina Luedtke", ["person2"] = "August Friedrich Hedtke", ["relation"] = "spouse", ["permanent"] = 1 });
                if (PeopleArray.Any(p => Value(p, "display_name").Equals("Robert R Hedtke", StringComparison.OrdinalIgnoreCase)) &&
                    !RelationshipArray.Any(r => Value(r, "child") == "Robert R Hedtke" && Value(r, "parent") == "Amelia Carolina Luedtke"))
                    RelationshipArray.Add(new JsonObject { ["child"] = "Robert R Hedtke", ["parent"] = "Amelia Carolina Luedtke", ["relation"] = "parent", ["confidence"] = 1.0, ["permanent"] = 1 });
            }

            string[] reviewCodes =
            {
                "TG204761", "TG204221", "TG202755", "TG207145", "TG200793", "TG202762", "TG202675",
                "TG209679", "TG209417", "TG204672", "TG206921", "TG200915", "TG209691", "TG204295"
            };
            JsonArray duplicateReviews = document["duplicate_reviews"] is JsonArray existingReviews
                ? existingReviews
                : (document["duplicate_reviews"] = new JsonArray()) as JsonArray ?? new JsonArray();
            foreach (string code in reviewCodes)
            {
                var matchingIdentifiers = IdentifierArray.Where(id =>
                    NormalizeCode(Value(id, "code")).Equals(code, StringComparison.OrdinalIgnoreCase) && Value(id, "kind") == "TG").ToList();
                string name = matchingIdentifiers.Count == 1 ? Value(matchingIdentifiers[0], "display_name") : "";
                bool duplicate = matchingIdentifiers.Count != 1 || duplicateNames.Contains(name);
                if (duplicate && !string.IsNullOrWhiteSpace(name)) duplicateNames.Add(name);
                if (!duplicateReviews.Any(review => Value(review, "code").Equals(code, StringComparison.OrdinalIgnoreCase)))
                    duplicateReviews.Add(new JsonObject
                    {
                        ["code"] = code,
                        ["display_name"] = name,
                        ["duplicate"] = duplicate,
                        ["checked"] = true,
                        ["check_basis"] = "TG identifier uniqueness and exact duplicate-record check in the built-in tree"
                    });
            }

            foreach (string name in duplicateNames)
            {
                if (!DuplicateRecords.Any(r => Value(r, "display_name").Equals(name, StringComparison.OrdinalIgnoreCase)))
                    DuplicateRecords.Add(new JsonObject { ["display_name"] = name, ["reason"] = "Duplicate record detected; retained in exports only", ["duplicate"] = true });
                foreach (JsonNode? person in PeopleArray.Where(p => Value(p, "display_name").Equals(name, StringComparison.OrdinalIgnoreCase)))
                    person!["duplicate"] = true;
            }
        }

        private void ReadBuiltInDocument()
        {
            // Every member of the original family tree is encoded in
            // BuiltInFamilyData.cs along with every associated data section.
            // Each run recreates a fresh in-memory copy without touching the C# source.
            JsonObject loaded = BuiltInFamilyData.Create();
            BuiltInFamilyData.ApplyVerifiedBirthYearCorrections(loaded);
            foreach (string name in new[] { "people", "identifiers", "relationships", "record_overrides", "duplicate_records" })
            {
                if (name is "record_overrides" or "duplicate_records")
                {
                    if (loaded[name] is not JsonArray) loaded[name] = new JsonArray();
                }
                else EnsureArray(loaded, name);
            }
            _document = loaded;
            ApplyChangeRequestStatus(loaded);
            if (EnsureArray(loaded, "people").Count != 129)
                throw new InvalidDataException("Built-in tree must contain 129 original people.");
            _currentPath = null;
            RefreshPeople();
            if (!_isExperimental) GenerateRecheckFiles();
            UpdateStatus("129 original family members and verified birth-year corrections loaded from C# source code. Import JSON to add more family members.");
        }

        private void GenerateRecheckFiles()
        {
            if (_document == null) throw new InvalidOperationException("Load the family tree before generating recheck files.");

            var rows = new List<string>
            {
                "RecordType,PersonSerial,Name,BirthYear,FullDateOfBirth,CaseID,Child,ParentAgeAtChildBirth,RelatedCaseID,ReviewNote"
            };
            var usedSuffixes = new HashSet<string>(StringComparer.Ordinal);
            var visitedPeople = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var visitedLinks = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var parentReviews = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var otherParentReviews = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            string NewSuffix()
            {
                string suffix;
                do suffix = RandomNumberGenerator.GetInt32(1_000_000, 10_000_000).ToString();
                while (!usedSuffixes.Add(suffix));
                return suffix;
            }

            void AddRow(params string[] columns) => rows.Add(string.Join(",", columns.Select(Csv)));

            string? SerialFor(string name)
            {
                JsonNode? record = PeopleArray.FirstOrDefault(person =>
                    Value(person, "display_name").Equals(name, StringComparison.OrdinalIgnoreCase));
                return int.TryParse(Value(record, "person_no"), out int number) ? $"TS{number:D6}" : null;
            }

            string FullBirthDate(string name)
            {
                string recorded = BirthDateOf(name) ?? "";
                string details = _people.FirstOrDefault(person =>
                    person.Name.Equals(name, StringComparison.OrdinalIgnoreCase))?.Details ?? "";
                string birthEvidence = string.Join(" ", recorded, BirthDetailsOnly(details), BirthDetailsOnly(NotesOf(name)));
                Match exactDate = Regex.Match(birthEvidence,
                    @"\b(?:\d{1,2}[/-]\d{1,2}[/-]\d{4}|\d{4}-\d{1,2}-\d{1,2}|\d{1,2}\s+[A-Za-z]{3,9}\.?[,]?\s+\d{4}|[A-Za-z]{3,9}\.?\s+\d{1,2},?\s+\d{4})\b");
                return exactDate.Success ? exactDate.Value : "";
            }

            void AddUnverifiedPerson(int personNo, string relatedPerson, string reason)
            {
                JsonNode? person = PeopleArray.FirstOrDefault(item =>
                    int.TryParse(Value(item, "person_no"), out int number) && number == personNo);
                if (person == null)
                    throw new InvalidDataException($"Unresolved record TS{personNo:D6} refers to a person not found in the built-in tree.");
                string name = Value(person, "display_name");
                int? year = BirthYearOf(name);
                string suffix = NewSuffix();
                AddRow("UnverifiedPerson", $"TS{personNo:D6}", name,
                    year?.ToString() ?? "", FullBirthDate(name), "E" + suffix, relatedPerson, "", "",
                    reason + " Ask whether this person or the related person needs a date correction.");
            }

            foreach (var unresolved in BuiltInFamilyData.UnverifiedDateCorrections)
                AddUnverifiedPerson(unresolved.PersonNo, unresolved.RelatedPerson, unresolved.Reason);

            if (!_people.Any(person => person.Name.Equals("Jeremiah O'Neal", StringComparison.OrdinalIgnoreCase)))
                throw new InvalidDataException("Jeremiah O'Neal was not found; the requested ancestor-line check cannot run.");

            void VisitAncestors(string child)
            {
                if (!visitedPeople.Add(child)) return;
                int? childYear = BirthYearOf(child);
                foreach (string parent in ParentsOf(child))
                {
                    string linkKey = parent + "\u001f" + child;
                    if (!visitedLinks.Add(linkKey)) continue;
                    int? parentYear = BirthYearOf(parent);
                    if (parentYear.HasValue && childYear.HasValue)
                    {
                        int parentAge = childYear.Value - parentYear.Value;
                        if (parentAge < 16 || parentAge > 42)
                        {
                            string reviewKey = parent + "\u001f" + child;
                            string suffix = NewSuffix();
                            parentReviews.Add(reviewKey);
                            AddRow("ParentDateCheck", SerialFor(parent) ?? "", parent, parentYear.Value.ToString(),
                                FullBirthDate(parent), "E" + suffix, child, parentAge.ToString(), "",
                                $"Year-only age is outside the requested maternal review range of 16-42. Confirm whether this parent is the mother and verify the child birth year first.");

                            if (parentAge >= 50)
                            {
                                int approximateYear = parentYear.Value + 25;
                                AddRow("PossibleMissingGeneration", "", "missing", approximateYear.ToString(),
                                    $"01/01/{approximateYear:D4}", "U" + NewSuffix(), child, "", "E" + suffix,
                                    $"Possible intermediate generation only; inferred from a {parentAge}-year parent-child year gap. Verify with Mr. O'Neal.");
                            }

                            if (parentAge > 42)
                            {
                                foreach (string otherParent in ParentsOf(child).Where(name =>
                                    !name.Equals(parent, StringComparison.OrdinalIgnoreCase)))
                                {
                                    int? otherYear = BirthYearOf(otherParent);
                                    if (!otherYear.HasValue || childYear.Value - otherYear.Value <= 48) continue;
                                    string otherKey = otherParent + "\u001f" + child;
                                    if (!otherParentReviews.Add(otherKey)) continue;
                                    AddRow("OtherParentDateCheck", SerialFor(otherParent) ?? "", otherParent,
                                        otherYear.Value.ToString(), FullBirthDate(otherParent), "U" + suffix, child,
                                        (childYear.Value - otherYear.Value).ToString(), "E" + suffix,
                                        "Other parent is over 48 by year-only calculation; verify identity, role, and date. U shares the associated E case number.");
                                }
                            }
                        }
                    }
                    VisitAncestors(parent);
                }
            }

            VisitAncestors("Jeremiah O'Neal");
            string outputFolder = TreeFolder();
            string csvPath = Path.Combine(outputFolder, "recheck.csv");
            string instructionsPath = Path.Combine(outputFolder, "recheck.md");
            File.WriteAllText(csvPath, string.Join(Environment.NewLine, rows) + Environment.NewLine, Encoding.UTF8);
            File.WriteAllText(instructionsPath, RecheckInstructions(), Encoding.UTF8);
        }

        private static string Csv(string value) => "\"" + value.Replace("\"", "\"\"") + "\"";

        private static string RecheckInstructions() =>
            "# Instructions for reviewing recheck.csv\r\n\r\n" +
            "Review every row with Mr. O'Neal and ask him to confirm or correct the information. Do not silently assume a relationship, a person's sex, or an exact date. The requested hard deadline is 11/16/2026.\r\n\r\n" +
            "## How to read the file\r\n\r\n" +
            "- `BirthYear` contains only a four-digit year; age checks use year subtraction only, not place names or other numbers.\r\n" +
            "- `FullDateOfBirth` is included only when an exact day/month/year was recorded. Do not infer a full date from a year or location.\r\n" +
            "- `ParentDateCheck` rows have an E followed by seven digits and are parent-child links whose year-only age is below 16 or above 42. The tree has no mother/father field, so ask whether the parent is the mother before applying the maternal rule. Verify the child's date first; if still outside the requested range, ask to verify the parent's date. Unusual ages are investigation prompts, not proof the link is wrong.\r\n" +
            "- `OtherParentDateCheck` rows have a U followed by seven digits whose number matches the associated E parent case. The other parent is over 48 by year-only calculation and is included for confirmation when relevant.\r\n" +
            "- `PossibleMissingGeneration` rows say `missing`, use an approximate year and `01/01/YYYY`, and have a U followed by seven digits. The program suggests one only for a parent-child year gap of at least 50 years, using the parent's year plus 25 as an estimate. These are hypotheses for possible intermediate people, not known people. Confirm or reject each; never add one as fact without evidence.\r\n" +
            "- `UnverifiedPerson` rows preserve the 14 items from persons.csv that could not be resolved. Their E-plus-seven-digit IDs are individual research cases, not proof that the listed date or relationship is wrong.\r\n\r\n" +
            "## Your response\r\n\r\n" +
            "Ask Mr. O'Neal for each case. Then write only confirmed corrections or explicit unresolved decisions to `fixes.csv` with columns `CaseID,PersonSerial,Name,Field,OldValue,CorrectedValue,Decision,SourceOrNotes`. Preserve the exact case ID; use `birth_year` for a year-only correction or `birth_date` for a confirmed full date. For a possible missing generation, use `Decision` = `confirmed_missing`, `not_missing`, or `needs_research`, and provide a name/year only when Mr. O'Neal confirms it. Do not overwrite recheck.csv.\r\n";

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
                    string originalName = Value(p, "display_name");
                    if (string.IsNullOrWhiteSpace(originalName)) continue;
                    JsonNode? duplicate = DuplicateRecords.FirstOrDefault(r =>
                        Value(r, "display_name").Equals(originalName, StringComparison.OrdinalIgnoreCase));
                    if (Flag(duplicate, "duplicate")) continue;

                    string name = Value(OverrideFor(originalName), "display_name");
                    if (string.IsNullOrWhiteSpace(name)) name = originalName;
                    string tg = IdentifierArray
                        .Where(id => Value(id, "display_name") == originalName && Value(id, "kind") == "TG")
                        .Select(id => Value(id, "code")).FirstOrDefault() ?? "";
                    string overrideTg = Value(OverrideFor(originalName), "tg");
                    if (!string.IsNullOrWhiteSpace(overrideTg)) tg = overrideTg;
                    string details = Value(OverrideFor(originalName), "details");
                    if (string.IsNullOrWhiteSpace(details)) details = Value(p, "details");
                    _people.Add(new PersonRow(name, tg, details, Value(p, "verify"), originalName));
                }
            }
            familyListBox.BeginUpdate();
            familyListBox.Items.Clear();
            foreach (PersonRow p in _people.Where(p => string.IsNullOrWhiteSpace(searchTextBox.Text) ||
                p.Name.Contains(searchTextBox.Text, StringComparison.OrdinalIgnoreCase) ||
                p.Tg.Contains(searchTextBox.Text, StringComparison.OrdinalIgnoreCase))) familyListBox.Items.Add(p);
            familyListBox.EndUpdate();
            RefreshEditPeople(selectName);
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
            RefreshChronologicalConsistencySummary();
            RefreshGeographyReport();
            RefreshResearchReviewTab();
            if (_isExperimental) RefreshMatchState();
        }

        private void InitializeChronologicalConsistencyTab()
        {
            var tab = new TabPage("Chronological Consistency Summary") { BackColor = Color.White };
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(18),
                ColumnCount = 1,
                RowCount = 5
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 130F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 82F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            var heading = new Label
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 18F, FontStyle.Bold),
                Text = "Genealogical chronological consistency check",
                TextAlign = ContentAlignment.MiddleLeft
            };
            _chronologicalSummaryLabel = new Label
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                Text = "Load the family tree to calculate the summary.",
                TextAlign = ContentAlignment.MiddleLeft
            };
            var guidance = new Label
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9F),
                Text = "Birth years come from active birth_date facts, or from the first year in a person's recorded details/notes when no fact is available. Age is estimated by subtracting years (not exact dates). Detail rows for parent ages 17-36 are hidden but included in totals. Unknown parent names imported from corrections.csv remain visible as unassessed links. Under 12 is flagged as highly implausible; ages 12-14 are unusually young and need review; 15-49 are plausible; 50+ is unusual but possible. A listed parent may be adoptive, a step-parent, or a guardian, and an unusual gap is not proof that a relationship is incorrect.",
                TextAlign = ContentAlignment.MiddleLeft
            };
            _chronologicalGrid = new DataGridView
            {
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White,
                Dock = DockStyle.Fill,
                ReadOnly = true,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect
            };
            foreach (string column in new[] { "Parent", "Child", "Parent birth year", "Child birth year", "Parent age at child's birth", "Assessment" })
                _chronologicalGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = column, Name = column });

            var correctionButtons = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false
            };
            var makeCorrectionsButton = new Button { Text = "Make Corrections", Width = 205, Height = 38 };
            makeCorrectionsButton.Click += makeCorrectionsButton_Click;
            correctionButtons.Controls.Add(makeCorrectionsButton);

            layout.Controls.Add(heading, 0, 0);
            layout.Controls.Add(_chronologicalSummaryLabel, 0, 1);
            layout.Controls.Add(guidance, 0, 2);
            layout.Controls.Add(correctionButtons, 0, 3);
            layout.Controls.Add(_chronologicalGrid, 0, 4);
            tab.Controls.Add(layout);
            tabControl.TabPages.Add(tab);
        }

        private void makeCorrectionsButton_Click(object? sender, EventArgs e)
        {
            if (_document == null) { MessageBox.Show(this, "The family tree could not be loaded."); return; }

            var reviewWindow = new Form
            {
                Text = "Make Corrections",
                Width = 680,
                Height = 260,
                MinimumSize = new Size(600, 220),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false
            };
            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(18), ColumnCount = 1, RowCount = 3 };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
            var instructions = new Label
            {
                Dock = DockStyle.Fill,
                Text = "This review uses four-digit birth years only to calculate parent ages. It checks parent ages outside 16-38 at a child's birth, then asks about the parent only if the child's verified year still leaves an unusual age. It also checks spouse/co-parent birth-year gaps of 20 or more years and parent ages over 48 at a shared child's birth. Verified years are written to corrections.csv; dates that cannot be verified are listed in persons.csv.",
                TextAlign = ContentAlignment.MiddleLeft
            };
            var status = new Label { Dock = DockStyle.Fill, Text = "Ready to review the current family tree.", TextAlign = ContentAlignment.MiddleLeft };
            var actions = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft };
            var closeButton = new Button { Text = "Close", Width = 100, Height = 34, DialogResult = DialogResult.Cancel };
            var startButton = new Button { Text = "Start Review", Width = 130, Height = 34 };
            actions.Controls.Add(closeButton);
            actions.Controls.Add(startButton);
            layout.Controls.Add(instructions, 0, 0);
            layout.Controls.Add(status, 0, 1);
            layout.Controls.Add(actions, 0, 2);
            reviewWindow.Controls.Add(layout);
            reviewWindow.CancelButton = closeButton;
            startButton.Click += (_, __) =>
            {
                startButton.Enabled = false;
                try
                {
                    (int verified, int unresolved, string outputFolder) = RunDateCorrectionReview(reviewWindow, message => status.Text = message);
                    instructions.Text = $"Review complete. Verified {verified} date(s); could not verify {unresolved} person/date item(s).\r\nFiles saved to: {outputFolder}\r\nCorrections are applied in memory; export the family tree JSON to save them in the tree.";
                    status.Text = "The chronological summary and both family views have been refreshed.";
                    closeButton.Text = "Done";
                    UpdateStatus($"Date review complete: {verified} verified, {unresolved} unresolved. CSV files saved to {outputFolder}.");
                    MessageBox.Show(reviewWindow, $"Review complete.\nVerified dates: {verified}\nUnresolved people: {unresolved}\n\nFiles saved to:\n{outputFolder}\n\nExport JSON to save verified dates in the family tree.", "Corrections ready", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (OperationCanceledException)
                {
                    status.Text = "Review cancelled; no date changes or CSV files were saved.";
                    startButton.Enabled = true;
                }
                catch (Exception ex)
                {
                    status.Text = "Could not complete the review.";
                    MessageBox.Show(reviewWindow, ex.Message, "Make Corrections failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    startButton.Enabled = true;
                }
            };
            reviewWindow.ShowDialog(this);
        }

        private Dictionary<string, int> CorrectionSerialNumbers()
        {
            var result = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var used = new HashSet<int>();
            foreach (PersonRow person in _people)
            {
                JsonNode? record = PeopleArray.FirstOrDefault(item =>
                    Value(item, "display_name").Equals(person.Name, StringComparison.OrdinalIgnoreCase));
                if (int.TryParse(Value(record, "person_no"), out int number) && number is >= 1 and <= 999999 && used.Add(number))
                    result.Add(person.Name, number);
            }

            int next = 1;
            foreach (PersonRow person in _people)
            {
                if (result.ContainsKey(person.Name)) continue;
                while (!used.Add(next)) next++;
                result.Add(person.Name, next++);
            }
            return result;
        }

        private static string CsvEscape(string value) => "\"" + value.Replace("\"", "\"\"") + "\"";

        private sealed record DateReviewAnswer(string? BirthDate, bool UnableToVerify, bool Cancelled);

        private (int Verified, int Unresolved, string OutputFolder) RunDateCorrectionReview(
            IWin32Window owner, Action<string> reportProgress)
        {
            if (_document == null) throw new InvalidOperationException("Load the family tree before reviewing dates.");

            var verifiedDates = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var unresolved = new Dictionary<string, (string Reason, string Related)>(StringComparer.OrdinalIgnoreCase);
            var correctionRows = new List<(string Name, string Date, string Reason, string Related)>();

            int? WorkingBirthYear(string name) => verifiedDates.TryGetValue(name, out string? correctedDate)
                ? ExtractYear(correctedDate)
                : BirthYearOf(name);

            void AddUnresolved(string name, string reason, string related)
            {
                if (unresolved.TryGetValue(name, out var existing))
                {
                    string combinedReason = string.Join("; ", new[] { existing.Reason, reason }.Distinct(StringComparer.OrdinalIgnoreCase));
                    string combinedRelated = string.Join("; ", new[] { existing.Related, related }.Where(value => !string.IsNullOrWhiteSpace(value)).Distinct(StringComparer.OrdinalIgnoreCase));
                    unresolved[name] = (combinedReason, combinedRelated);
                }
                else unresolved.Add(name, (reason, related));
            }

            int? VerifyBirthDate(string name, string reason, string related)
            {
                if (verifiedDates.TryGetValue(name, out string? alreadyVerified)) return ExtractYear(alreadyVerified);
                if (unresolved.ContainsKey(name)) return null;
                if (!_people.Any(person => person.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
                {
                    AddUnresolved(name, "Person is referenced by a family link but is not in the people list", related);
                    return null;
                }

                reportProgress($"Verifying birth year for {name}. {reason}");
                DateReviewAnswer answer = AskForBirthDate(owner, name, reason, WorkingBirthYear(name)?.ToString());
                if (answer.Cancelled) throw new OperationCanceledException();
                if (answer.UnableToVerify || string.IsNullOrWhiteSpace(answer.BirthDate))
                {
                    AddUnresolved(name, reason, related);
                    return null;
                }

                verifiedDates.Add(name, answer.BirthDate);
                correctionRows.Add((name, answer.BirthDate, reason, related));
                return ExtractYear(answer.BirthDate);
            }

            var parentChildLinks = RelationshipArray
                .Where(relationship => Value(relationship, "relation") == "parent")
                .Select(relationship => (Parent: Value(relationship, "parent"), Child: Value(relationship, "child")))
                .Where(link => !string.IsNullOrWhiteSpace(link.Parent) && !string.IsNullOrWhiteSpace(link.Child))
                .Distinct()
                .ToList();

            foreach (var link in parentChildLinks)
            {
                int? parentYear = WorkingBirthYear(link.Parent);
                int? childYear = WorkingBirthYear(link.Child);
                bool needsReview = !parentYear.HasValue || !childYear.HasValue ||
                    childYear.Value - parentYear.Value < 16 || childYear.Value - parentYear.Value > 38;
                if (!needsReview) continue;

                string issue = !parentYear.HasValue || !childYear.HasValue
                    ? "A parent or child birth year is missing, so the parent age cannot be checked."
                    : $"Recorded parent age at the child's birth is {childYear.Value - parentYear.Value}; the review range is 16-38.";
                int? verifiedChildYear = VerifyBirthDate(link.Child,
                    $"First verify the child's birth date. {issue}", link.Parent);
                if (!verifiedChildYear.HasValue) continue;

                int? checkedParentYear = WorkingBirthYear(link.Parent);
                if (checkedParentYear.HasValue)
                {
                    int ageAtChildBirth = verifiedChildYear.Value - checkedParentYear.Value;
                    if (ageAtChildBirth is >= 16 and <= 38) continue;
                    issue = $"After verifying the child's date, the parent would be age {ageAtChildBirth}; verify the parent's birth date next.";
                }
                else
                {
                    issue = "The child's date is verified, but the parent's birth year is missing; verify the parent's birth date.";
                }
                VerifyBirthDate(link.Parent, issue, link.Child);
            }

            var spousePairs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var spousePairNames = new List<(string First, string Second)>();
            foreach (PersonRow person in _people)
            foreach (string partner in SpousesOf(person.Name).Concat(CoparentsOf(person.Name)))
            {
                if (person.Name.Equals(partner, StringComparison.OrdinalIgnoreCase)) continue;
                string first = string.Compare(person.Name, partner, StringComparison.OrdinalIgnoreCase) <= 0 ? person.Name : partner;
                string second = first.Equals(person.Name, StringComparison.OrdinalIgnoreCase) ? partner : person.Name;
                if (spousePairs.Add(first + "\u001f" + second)) spousePairNames.Add((first, second));
            }

            foreach (var pair in spousePairNames)
            {
                int? firstYear = WorkingBirthYear(pair.First);
                int? secondYear = WorkingBirthYear(pair.Second);
                if (!firstYear.HasValue || !secondYear.HasValue) continue;

                int ageGap = Math.Abs(firstYear.Value - secondYear.Value);
                bool parentAgeOver48 = parentChildLinks
                    .Where(link => (link.Parent.Equals(pair.First, StringComparison.OrdinalIgnoreCase) &&
                                    linkChildHasOtherParent(link.Child, pair.Second)) ||
                                   (link.Parent.Equals(pair.Second, StringComparison.OrdinalIgnoreCase) &&
                                    linkChildHasOtherParent(link.Child, pair.First)))
                    .Any(link =>
                    {
                        int? childYear = WorkingBirthYear(link.Child);
                        int? parentYear = WorkingBirthYear(link.Parent);
                        return childYear.HasValue && parentYear.HasValue && childYear.Value - parentYear.Value > 48;
                    });
                if (ageGap < 20 && !parentAgeOver48) continue;

                string reason = parentAgeOver48
                    ? "Spouse/co-parent dates imply a parent age over 48 at a shared child's birth. Verify both partners' birth dates."
                    : $"Spouse/co-parent birth years are {ageGap} years apart. Verify both partners' birth dates.";
                VerifyBirthDate(pair.First, reason, pair.Second);
                VerifyBirthDate(pair.Second, reason, pair.First);
            }

            string outputFolder = TreeFolder();
            Dictionary<string, int> serialNumbers = CorrectionSerialNumbers();
            string correctionsPath = Path.Combine(outputFolder, "corrections.csv");
            string personsPath = Path.Combine(outputFolder, "persons.csv");
            using (var writer = new StreamWriter(correctionsPath, false, new UTF8Encoding(true)))
            {
                writer.WriteLine("SerialNumber,DisplayName,VerifiedBirthYear,Reason,RelatedPerson");
                foreach (var correction in correctionRows)
                {
                    string serial = "TS" + serialNumbers[correction.Name].ToString("D6");
                    writer.WriteLine(string.Join(",", new[] { serial, correction.Name, correction.Date, correction.Reason, correction.Related }.Select(CsvEscape)));
                }
            }
            using (var writer = new StreamWriter(personsPath, false, new UTF8Encoding(true)))
            {
                writer.WriteLine("SerialNumber,DisplayName,Reason,RelatedPerson");
                foreach (var item in unresolved.OrderBy(entry => entry.Key, StringComparer.OrdinalIgnoreCase))
                {
                    string serial = serialNumbers.TryGetValue(item.Key, out int number) ? "TS" + number.ToString("D6") : "";
                    writer.WriteLine(string.Join(",", new[] { serial, item.Key, item.Value.Reason, item.Value.Related }.Select(CsvEscape)));
                }
            }

            foreach (var correction in verifiedDates)
                ApplyVerifiedBirthDate(correction.Key, correction.Value);
            RefreshPeople();
            SynchronizeExperimentalView();
            UpdateStatus($"Applied {verifiedDates.Count} verified birth dates. Chronological summary refreshed.");
            return (verifiedDates.Count, unresolved.Count, outputFolder);

            bool linkChildHasOtherParent(string childName, string otherParent) =>
                ParentsOf(childName).Any(parent => parent.Equals(otherParent, StringComparison.OrdinalIgnoreCase));
        }

        private DateReviewAnswer AskForBirthDate(IWin32Window owner, string person, string reason, string? currentDate)
        {
            using var dialog = new Form
            {
                Text = $"Verify Birth Year: {person}",
                Width = 620,
                Height = 300,
                MinimumSize = new Size(560, 260),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false
            };
            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(18), ColumnCount = 1, RowCount = 4 };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 46F));
                var personLabel = new Label { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 11F, FontStyle.Bold), Text = person, TextAlign = ContentAlignment.MiddleLeft };
            var reasonLabel = new Label { Dock = DockStyle.Fill, Text = reason, TextAlign = ContentAlignment.MiddleLeft };
            var dateBox = new TextBox { Dock = DockStyle.Fill, Text = currentDate ?? "", PlaceholderText = "Four-digit birth year, e.g. 1926" };
            var actions = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, WrapContents = false };
            var cancel = new Button { Text = "Cancel Review", Width = 110, Height = 34, DialogResult = DialogResult.Cancel };
            var unable = new Button { Text = "Can't Verify", Width = 110, Height = 34 };
            var verify = new Button { Text = "Verify Year", Width = 110, Height = 34 };
            actions.Controls.Add(cancel);
            actions.Controls.Add(unable);
            actions.Controls.Add(verify);
            layout.Controls.Add(personLabel, 0, 0);
            layout.Controls.Add(reasonLabel, 0, 1);
            layout.Controls.Add(dateBox, 0, 2);
            layout.Controls.Add(actions, 0, 3);
            dialog.Controls.Add(layout);
            dialog.CancelButton = cancel;

            DateReviewAnswer answer = new(null, false, true);
            verify.Click += (_, __) =>
            {
                string enteredYear = dateBox.Text.Trim();
                if (!Regex.IsMatch(enteredYear, @"^\d{4}$") ||
                    !int.TryParse(enteredYear, out int year) || year < 1000 || year > DateTime.Now.Year)
                {
                    MessageBox.Show(dialog, "Enter only a four-digit birth year between 1000 and the current year. Location text and full dates are not accepted here.", "Birth year required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                answer = new DateReviewAnswer(enteredYear, false, false);
                dialog.DialogResult = DialogResult.OK;
                dialog.Close();
            };
            unable.Click += (_, __) =>
            {
                answer = new DateReviewAnswer(null, true, false);
                dialog.DialogResult = DialogResult.OK;
                dialog.Close();
            };
            DialogResult result = dialog.ShowDialog(owner);
            return result == DialogResult.OK ? answer : new DateReviewAnswer(null, false, true);
        }

        private void ApplyVerifiedBirthDate(string name, string birthDate)
        {
            JsonArray facts = SupplementalArray("provisional_facts");
            foreach (JsonNode? fact in facts.Where(item =>
                Value(item, "display_name").Equals(name, StringComparison.OrdinalIgnoreCase) &&
                Value(item, "field") == "birth_date" && Value(item, "status") == "active"))
                fact!["status"] = "inactive";
            facts.Add(new JsonObject
            {
                ["display_name"] = name,
                ["field"] = "birth_date",
                ["value"] = birthDate,
                ["confidence"] = 1.0,
                ["source_url"] = "user-correction:guided-date-review",
                ["source_title"] = "User-verified birth year",
                ["status"] = "active",
                ["permanent"] = 0
            });
        }

        private static string ReplaceBirthDateInDetails(string details, string birthDate)
        {
            int birthIndex = details.IndexOf("B:", StringComparison.OrdinalIgnoreCase);
            int deathIndex = details.IndexOf("D:", StringComparison.OrdinalIgnoreCase);
            if (birthIndex >= 0)
            {
                int birthEnd = deathIndex > birthIndex ? deathIndex : details.Length;
                string recordedBirthDetails = details[(birthIndex + 2)..birthEnd].Trim();
                Match oldDate = Regex.Match(recordedBirthDetails,
                    @"\b\d{1,2}\s+[A-Za-z]{3,9}\.?[,]?\s+\d{4}\b|\b[A-Za-z]{3,9}\.?\s*\d{4}\b|\b\d{4}\b");
                if (oldDate.Success)
                    recordedBirthDetails = recordedBirthDetails.Remove(oldDate.Index, oldDate.Length).Trim(' ', ',', '-');

                string birthValue = string.Join(" ", new[] { birthDate, recordedBirthDetails }
                    .Where(value => !string.IsNullOrWhiteSpace(value)));
                string prefix = details[..birthIndex].Trim();
                string birthDetails = string.IsNullOrWhiteSpace(birthValue) ? "" : $"B:{birthValue}";
                string deathDetails = deathIndex > birthIndex ? details[deathIndex..].Trim() : "";
                return string.Join(" ", new[] { prefix, birthDetails, deathDetails }
                    .Where(value => !string.IsNullOrWhiteSpace(value)));
            }
            if (string.IsNullOrWhiteSpace(birthDate))
            {
                if (deathIndex == 0) return details;
                MatchCollection dates = Regex.Matches(details, @"\b\d{1,2}\s+[A-Za-z]{3,9}\.?[,]?\s+\d{4}\b|\b\d{4}\b");
                if (dates.Count > 1) return $"D:{details[dates[1].Index..]}";
                if (dates.Count == 1) return details.Remove(dates[0].Index, dates[0].Length).Trim();
                return details;
            }
            return $"B:{birthDate} {details}".Trim();
        }

        private void InitializeGeographyTab()
        {
            var tab = new TabPage("Geography") { BackColor = Color.White };
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(18),
                ColumnCount = 1,
                RowCount = 4
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 78F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            var heading = new Label
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 18F, FontStyle.Bold),
                Text = "Geographic research planner",
                TextAlign = ContentAlignment.MiddleLeft
            };
            var guidance = new Label
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9F),
                Text = "Recorded birth places are taken from active birth_place facts or birth details. If a location is missing, a parent's recorded birth place is shown only as an unverified family-region estimate; otherwise the location remains unknown. Difficulty is a rough research-priority guide based on location certainty and birth period, not a guarantee of record availability. Recommendations are starting points; record access depends on jurisdiction, privacy, and historical boundaries.",
                TextAlign = ContentAlignment.MiddleLeft
            };
            _geographyGrid = new DataGridView
            {
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White,
                Dock = DockStyle.Fill,
                ReadOnly = true,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect
            };
            foreach (string column in new[] { "Person", "Location and basis", "Birth period", "Difficulty", "Recommended sources for person and family" })
                _geographyGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = column, Name = column });

            var actions = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, WrapContents = false };
            var exportButton = new Button { Text = "Export geography.csv", Width = 205, Height = 38 };
            exportButton.Click += exportGeographyButton_Click;
            actions.Controls.Add(exportButton);

            layout.Controls.Add(heading, 0, 0);
            layout.Controls.Add(guidance, 0, 1);
            layout.Controls.Add(actions, 0, 2);
            layout.Controls.Add(_geographyGrid, 0, 3);
            tab.Controls.Add(layout);
            tabControl.TabPages.Add(tab);
        }

        private void exportGeographyButton_Click(object? sender, EventArgs e)
        {
            if (_document == null) { MessageBox.Show(this, "The family tree could not be loaded."); return; }
            using var dialog = new SaveFileDialog
            {
                Title = "Export geographic research plan",
                Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
                FileName = "geography.csv",
                InitialDirectory = TreeFolder(),
                OverwritePrompt = true
            };
            if (dialog.ShowDialog(this) != DialogResult.OK) return;

            try
            {
                List<GeographyRow> report = BuildGeographyReport();
                Dictionary<string, int> serialNumbers = CorrectionSerialNumbers();
                using var writer = new StreamWriter(dialog.FileName, false, new UTF8Encoding(true));
                writer.WriteLine("SerialNumber,DisplayName,Difficulty,InformationType,Information");
                foreach (GeographyRow row in report)
                {
                    string serial = "TS" + serialNumbers[row.Name].ToString("D6");
                    string difficulty = $"{row.Difficulty}/5 - {row.DifficultyReason}";
                    foreach ((string type, string information) in new[]
                    {
                        ("Location", $"{row.Location} ({row.LocationBasis})"),
                        ("Time period", row.TimePeriod),
                        ("Recommended sources", row.RecommendedSources)
                    })
                    {
                        writer.WriteLine(string.Join(",", new[] { serial, row.Name, difficulty, type, information }.Select(CsvEscape)));
                    }
                }
                UpdateStatus($"Exported geographic research plan for {report.Count} people, hardest to easiest, to {dialog.FileName}.");
                MessageBox.Show(this, "Exported one three-row section per person, sorted from most difficult to easiest: Location, Time period, and Recommended sources. Estimates are labeled and should be verified.", "Geography exported", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex) { MessageBox.Show(this, ex.Message, "Geography export failed", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        private void RefreshGeographyReport()
        {
            if (_geographyGrid == null) return;
            _geographyGrid.Rows.Clear();
            foreach (GeographyRow row in BuildGeographyReport())
                _geographyGrid.Rows.Add(row.Name, $"{row.Location} ({row.LocationBasis})", row.TimePeriod,
                    $"{row.Difficulty}/5 - {row.DifficultyReason}", row.RecommendedSources);
        }

        private List<GeographyRow> BuildGeographyReport()
        {
            return _people.Select(BuildGeographyRow)
                .OrderByDescending(row => row.Difficulty)
                .ThenBy(row => row.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private GeographyRow BuildGeographyRow(PersonRow person)
        {
            int? birthYear = BirthYearOf(person.Name);
            string timePeriod = birthYear.HasValue
                ? $"Around {birthYear.Value} ({birthYear.Value / 10 * 10}s)"
                : "Unknown; establish an approximate birth date first";

            string? recordedLocation = BirthPlaceOf(person.Name);
            string location;
            string basis;
            bool estimated = false;
            if (!string.IsNullOrWhiteSpace(recordedLocation))
            {
                location = recordedLocation;
                basis = "recorded; verify source";
            }
            else
            {
                string[] parentPlaces = ParentsOf(person.Name)
                    .Select(BirthPlaceOf)
                    .Where(place => !string.IsNullOrWhiteSpace(place))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray()!;
                if (parentPlaces.Length > 0)
                {
                    location = string.Join(" / ", parentPlaces);
                    basis = "possible family-region estimate from parent birth place; unverified";
                    estimated = true;
                }
                else
                {
                    location = "Unknown";
                    basis = "no defensible location estimate from available records";
                }
            }

            int difficulty = !birthYear.HasValue ? 4 : birthYear.Value < 1800 ? 5 :
                birthYear.Value < 1850 ? 4 : birthYear.Value < 1900 ? 3 :
                birthYear.Value < 1950 ? 2 : 1;
            if (recordedLocation == null) difficulty = Math.Min(5, difficulty + 1);
            if (estimated) difficulty = Math.Min(5, difficulty + 1);
            string difficultyReason = recordedLocation == null
                ? estimated ? "location estimated" : "birth location unknown"
                : "location recorded";
            difficultyReason += birthYear.HasValue
                ? $"; {birthYear.Value / 10 * 10}s records"
                : "; birth period unknown";

            string sources = RecommendedGeographySources(person.Name, recordedLocation ?? (estimated ? location : null), birthYear);
            return new GeographyRow(person.Name, location, basis, timePeriod, sources, difficulty, difficultyReason);
        }

        private string? BirthPlaceOf(string name)
        {
            string? fact = Provisional(name, "birth_place");
            if (!string.IsNullOrWhiteSpace(fact)) return fact.Trim();

            PersonRow? person = _people.FirstOrDefault(candidate =>
                candidate.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            string details = person?.Details ?? "";
            Match birth = Regex.Match(details, @"\bB:\s*(.*?)(?=\s+D:|$)", RegexOptions.IgnoreCase);
            if (birth.Success)
            {
                string birthText = birth.Groups[1].Value.Trim();
                Match year = Regex.Match(birthText, @"\b\d{4}\b");
                if (year.Success) birthText = (birthText[..year.Index] + birthText[(year.Index + year.Length)..]).Trim(' ', ',', '-');
                if (!string.IsNullOrWhiteSpace(birthText)) return birthText;
            }

            string detailsWithoutDate = BirthDetailsOnly(details);
            MatchCollection dates = Regex.Matches(detailsWithoutDate, @"\b\d{1,2}\s+[A-Za-z]{3,9}\.?[,]?\s+\d{4}\b|\b\d{4}\b");
            if (dates.Count > 0)
            {
                Match birthDate = dates[0];
                int locationEnd = dates.Count > 1 ? dates[1].Index : detailsWithoutDate.Length;
                string place = (detailsWithoutDate[..birthDate.Index] + detailsWithoutDate[(birthDate.Index + birthDate.Length)..locationEnd]).Trim(' ', ',', '-');
                if (!string.IsNullOrWhiteSpace(place)) return place;
            }
            return null;
        }

        private string RecommendedGeographySources(string name, string? location, int? birthYear)
        {
            var sources = new List<string>();
            if (!string.IsNullOrWhiteSpace(location))
            {
                sources.Add("local civil vital-registration office and county/state archives for birth records");
                if (birthYear < 1900) sources.Add("church/parish baptism registers and local historical or genealogical society collections");
                if (birthYear is >= 1850 and <= 1950) sources.Add("census schedules and household records for parents and siblings");
                if (birthYear < 1850) sources.Add("probate, land, tax, church, and town/county records to identify parents and siblings");
                if (location.Contains("England", StringComparison.OrdinalIgnoreCase) ||
                    location.Contains("Scotland", StringComparison.OrdinalIgnoreCase) ||
                    location.Contains("Ireland", StringComparison.OrdinalIgnoreCase) ||
                    location.Contains("Germany", StringComparison.OrdinalIgnoreCase) ||
                    location.Contains("Russia", StringComparison.OrdinalIgnoreCase))
                    sources.Add("jurisdiction-specific civil and church registers, plus emigration/immigration and naturalization records");
            }
            else
            {
                sources.Add("identify the jurisdiction first using relatives' records, obituaries, census households, and family correspondence");
                sources.Add("search parents' and siblings' birth, marriage, death, church, probate, and immigration records for shared locations");
            }
            sources.Add("search obituaries, probate files, marriage records, and census households for sibling names and both parents");
            sources.Add("check the record dates, historical jurisdiction boundaries, image citations, and access/privacy restrictions before treating a match as proof");
            return string.Join("; ", sources.Distinct(StringComparer.OrdinalIgnoreCase));
        }

        private void RefreshChronologicalConsistencySummary()
        {
            if (_chronologicalSummaryLabel == null || _chronologicalGrid == null) return;

            var birthYears = _people.ToDictionary(person => person.Name,
                person => BirthYearOf(person.Name), StringComparer.OrdinalIgnoreCase);
            var knownPeople = birthYears.Keys.ToHashSet(StringComparer.OrdinalIgnoreCase);
            var relationships = RelationshipArray
                .Where(relationship => Value(relationship, "relation") == "parent")
                .Select(relationship => (Parent: Value(relationship, "parent"), Child: Value(relationship, "child")))
                .Where(link => !string.IsNullOrWhiteSpace(link.Parent) && !string.IsNullOrWhiteSpace(link.Child))
                .Where(link => !_excludedChronologicalNames.Contains(link.Parent) && !_excludedChronologicalNames.Contains(link.Child))
                .Distinct()
                .OrderBy(link => link.Parent, StringComparer.OrdinalIgnoreCase)
                .ThenBy(link => link.Child, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var linkedPeople = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var peopleMissingDates = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var peopleNeedingYoungParentReview = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var peopleWithImplausibleGaps = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var peopleWithReversedBirthOrder = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            int assessedLinks = 0;
            int hiddenPlausibleLinks = 0;
            _chronologicalGrid.Rows.Clear();
            foreach (var link in relationships)
            {
                bool knownParent = knownPeople.Contains(link.Parent);
                bool knownChild = knownPeople.Contains(link.Child);
                if (knownParent) linkedPeople.Add(link.Parent);
                if (knownChild) linkedPeople.Add(link.Child);
                int? parentYear = knownParent ? birthYears[link.Parent] : null;
                int? childYear = knownChild ? birthYears[link.Child] : null;
                string ageText = "Not assessed";
                string assessment;
                if (parentYear is int parentBirthYear && childYear is int childBirthYear)
                {
                    int age = childBirthYear - parentBirthYear;
                    ageText = age.ToString();
                    assessedLinks++;
                    if (knownParent && knownChild)
                    {
                        if (age < 15)
                        {
                            peopleNeedingYoungParentReview.Add(link.Parent);
                            peopleNeedingYoungParentReview.Add(link.Child);
                        }
                        if (age < 0)
                        {
                            peopleWithReversedBirthOrder.Add(link.Parent);
                            peopleWithReversedBirthOrder.Add(link.Child);
                        }
                        else if (age < 12)
                        {
                            peopleWithImplausibleGaps.Add(link.Parent);
                            peopleWithImplausibleGaps.Add(link.Child);
                        }
                    }
                    assessment = age < 0 ? "Inconsistent: child year precedes parent" :
                        age < 12 ? "Highly implausible: parent under 12" :
                        age < 15 ? "Unusually young: review" :
                        age < 50 ? "Plausible" : "Unusual, but possible";
                }
                else
                {
                    assessment = "Not assessed: birth year missing";
                    if (knownParent) peopleMissingDates.Add(link.Parent);
                    if (knownChild) peopleMissingDates.Add(link.Child);
                }
                if (parentYear.HasValue && childYear.HasValue)
                {
                    int age = childYear.Value - parentYear.Value;
                    if (age >= 17 && age <= 36)
                    {
                        hiddenPlausibleLinks++;
                        continue;
                    }
                }
                int rowIndex = _chronologicalGrid.Rows.Add(link.Parent, link.Child,
                    parentYear?.ToString() ?? "Not recorded", childYear?.ToString() ?? "Not recorded",
                    ageText, assessment);
                foreach (DataGridViewCell cell in _chronologicalGrid.Rows[rowIndex].Cells)
                {
                    string name = cell.OwningColumn.HeaderText is "Parent" or "Child"
                        ? cell.Value?.ToString() ?? ""
                        : "";
                    JsonNode? person = PeopleArray.FirstOrDefault(p =>
                        Value(p, "display_name").Equals(name, StringComparison.OrdinalIgnoreCase));
                    string code = person == null ? "" : IdentifierArray.Where(id =>
                        Value(id, "display_name") == name).Select(id => Value(id, "code")).FirstOrDefault() ?? "";
                    if (!string.IsNullOrEmpty(code) && _orangeCodes.Contains(NormalizeCode(code)))
                        cell.Style.ForeColor = Color.FromArgb(255, 140, 0);
                    else if (_purpleCodes.Contains(NormalizeCode(code)) || _excludedChronologicalNames.Contains(name))
                        cell.Style.ForeColor = Color.Purple;
                    else if (_blueCodes.Contains(NormalizeCode(code)))
                        cell.Style.ForeColor = Color.RoyalBlue;
                }
            }

            int consistentPeople = linkedPeople.Count(person =>
                !peopleMissingDates.Contains(person) && !peopleNeedingYoungParentReview.Contains(person));
            int peopleWithoutParentChildLinks = _people.Count - linkedPeople.Count;
            _chronologicalSummaryLabel.Text =
                $"Legend: orange = listed for verification/update; blue = date uncertainty; purple = disconnected/investigation record. Orange takes precedence.\n" +
                $"Total people: {_people.Count}\n" +
                $"Chronologically consistent: {consistentPeople} (among people with parent-child links, complete birth-year evidence, and no parent age under 15)\n" +
                $"People whose child birth year precedes a listed parent: {peopleWithReversedBirthOrder.Count}    " +
                $"People with a highly implausible parent age (0-11): {peopleWithImplausibleGaps.Count}\n" +
                $"People with an unusually young parent age (12-14) needing review: {peopleNeedingYoungParentReview.Except(peopleWithImplausibleGaps, StringComparer.OrdinalIgnoreCase).Except(peopleWithReversedBirthOrder, StringComparer.OrdinalIgnoreCase).Count()}\n" +
                $"Plausible parent-child links hidden from details (ages 17-36): {hiddenPlausibleLinks}\n" +
                $"People missing birth-year evidence on at least one link: {peopleMissingDates.Count}    " +
                $"People without parent-child links: {peopleWithoutParentChildLinks}\n" +
                $"Parent-child links assessed: {assessedLinks} of {relationships.Count}";
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

        private string ResearchReviewText(PersonRow person)
        {
            if (_document?["record_overrides"] is not JsonArray overrides) return "";
            string[] codes = IdentifierArray.Where(identifier =>
                    Value(identifier, "display_name").Equals(person.OriginalName, StringComparison.OrdinalIgnoreCase))
                .Select(identifier => Value(identifier, "code"))
                .Append(person.Tg)
                .Where(code => !string.IsNullOrWhiteSpace(code))
                .ToArray();
            var matching = overrides.Where(item => Value(item, "field") == "research_review_2026_11_18" &&
                (Value(item, "display_name").Equals(person.OriginalName, StringComparison.OrdinalIgnoreCase) ||
                 Value(item, "display_name").Equals(person.Name, StringComparison.OrdinalIgnoreCase) ||
                 codes.Contains(Value(item, "code"), StringComparer.OrdinalIgnoreCase))).ToList();
            return string.Join(Environment.NewLine + Environment.NewLine, matching.Select(item =>
                "UNVERIFIED RESEARCH LEAD (not a confirmed fact or relationship)" + Environment.NewLine +
                Value(item, "value") +
                (string.IsNullOrWhiteSpace(Value(item, "source")) ? "" : Environment.NewLine + "Source: " + Value(item, "source")) +
                (string.IsNullOrWhiteSpace(Value(item, "reason")) ? "" : Environment.NewLine + "Review note: " + Value(item, "reason"))));
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
                ["died"] = DeathInfoOf(person.Name).Value ?? "See original notes",
                ["parents"] = ToJsonArray(ParentsOf(person.Name)),
                ["spouse"] = ToJsonArray(SpousesOf(person.Name)),
                ["other_parents"] = ToJsonArray(CoparentsOf(person.Name)),
                ["children"] = ToJsonArray(ChildrenOf(person.Name)),
                ["details"] = person.Details,
                ["duplicate"] = DuplicateRecords.Any(r => Value(r, "display_name").Equals(person.OriginalName, StringComparison.OrdinalIgnoreCase)),
                ["original_name"] = person.OriginalName,
                ["record_override"] = OverrideFor(person.OriginalName)?.DeepClone(),
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

        private (string? Value, string Status) DeathInfoOf(string name)
        {
            string? provisionalDeath = Provisional(name, "death_date");
            if (!string.IsNullOrWhiteSpace(provisionalDeath)) return (provisionalDeath, "unverified");

            PersonRow? person = _people.FirstOrDefault(candidate =>
                candidate.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            string originalName = person?.OriginalName ?? name;
            var codes = IdentifierArray.Where(identifier =>
                    Value(identifier, "display_name").Equals(originalName, StringComparison.OrdinalIgnoreCase))
                .Select(identifier => Value(identifier, "code"))
                .Append(person?.Tg ?? "")
                .Where(code => !string.IsNullOrWhiteSpace(code))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            JsonNode? deathOverride = RecordOverrides.FirstOrDefault(item =>
                Value(item, "field") == "died" &&
                (Value(item, "display_name").Equals(originalName, StringComparison.OrdinalIgnoreCase) ||
                 codes.Contains(Value(item, "code"))));
            string overrideValue = Value(deathOverride, "value").Trim();
            if (!string.IsNullOrWhiteSpace(overrideValue) &&
                !overrideValue.Equals("still verifying", StringComparison.OrdinalIgnoreCase) &&
                !overrideValue.Equals("See original notes", StringComparison.OrdinalIgnoreCase))
                return (overrideValue, Flag(deathOverride, "verified") ? "verified" : "unverified");

            string? provisionalDescription = Provisional(name, "death_description");
            if (!string.IsNullOrWhiteSpace(provisionalDescription)) return (provisionalDescription, "unverified");

            string details = person?.Details ?? "";
            Match died = Regex.Match(details, @"\bDied:\s*(.*?)(?=\s+D:|[)]|$)", RegexOptions.IgnoreCase);
            if (died.Success && !string.IsNullOrWhiteSpace(died.Groups[1].Value))
                return (died.Groups[1].Value.Trim().TrimEnd(' ', '.', ';'), "from original notes");
            Match death = Regex.Match(details, @"\bD:\s*(.*)$", RegexOptions.IgnoreCase);
            if (death.Success && !string.IsNullOrWhiteSpace(death.Groups[1].Value))
                return (death.Groups[1].Value.Trim().TrimEnd(' ', '.', ';'), "from original notes");
            return (null, "");
        }

        private string? BirthDateOf(string name)
        {
            string? recordedDate = Provisional(name, "birth_date");
            if (!string.IsNullOrWhiteSpace(recordedDate)) return recordedDate;

            PersonRow? person = _people.FirstOrDefault(candidate =>
                candidate.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            string details = person?.Details ?? "";
            Match birth = Regex.Match(details, @"\bB:\s*(.*?)(?=\s+D:|$)", RegexOptions.IgnoreCase);
            if (birth.Success && !string.IsNullOrWhiteSpace(birth.Groups[1].Value)) return birth.Groups[1].Value.Trim();
            string birthDetails = BirthDetailsOnly(details);
            Match date = Regex.Match(birthDetails, @"\b\d{1,2}\s+[A-Za-z]{3,9}\.?[,]?\s+\d{4}\b|\b\d{4}\b");
            return date.Success ? date.Value : null;
        }

        private int? BirthYearOf(string name)
        {
            if (_document?["provisional_facts"] is JsonArray facts)
            {
                foreach (JsonNode? fact in facts.Where(f =>
                    Value(f, "display_name").Equals(name, StringComparison.OrdinalIgnoreCase) &&
                    Value(f, "field") == "birth_date" && Value(f, "status") == "active"))
                {
                    int? year = ExtractYear(Value(fact, "value"));
                    if (year.HasValue) return year;
                }
            }

            PersonRow? person = _people.FirstOrDefault(candidate =>
                candidate.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            return ExtractYearFromDetails(person?.Details) ?? ExtractYear(NotesOf(name));
        }

        private static string BirthDetailsOnly(string details)
        {
            int birthIndex = details.IndexOf("B:", StringComparison.OrdinalIgnoreCase);
            if (birthIndex >= 0)
            {
                int deathIndex = details.IndexOf("D:", birthIndex, StringComparison.OrdinalIgnoreCase);
                return details[(birthIndex + 2)..(deathIndex >= 0 ? deathIndex : details.Length)];
            }
            int onlyDeathIndex = details.IndexOf("D:", StringComparison.OrdinalIgnoreCase);
            return onlyDeathIndex == 0 ? "" : onlyDeathIndex > 0 ? details[..onlyDeathIndex] : details;
        }

        private static int? ExtractYearFromDetails(string? details) =>
            string.IsNullOrWhiteSpace(details) ? null : ExtractYear(BirthDetailsOnly(details));

        private static int? ExtractYear(string? text)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;
            Match match = Regex.Match(text, @"\b\d{4}\b");
            return match.Success && int.TryParse(match.Value, out int year) ? year : null;
        }

        private void familyListBox_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (familyListBox.SelectedItem is not PersonRow person) { ClearPerson(); return; }
            nameValueLabel.Text = person.Name;
            tgValueLabel.Text = string.IsNullOrEmpty(person.Tg) ? "Unassigned" : person.Tg;
            birthValueLabel.Text = BirthDateOf(person.Name) is string bd ? bd + " (recorded)" : "See original notes";
            var deathInfo = DeathInfoOf(person.Name);
            string? death = deathInfo.Value;
            string deathStatus = deathInfo.Status;
            deathValueLabel.Text = death is not null ? $"{death} ({deathStatus})" : "See original notes";
            parentsValueLabel.Text = ListOrUnknown(ParentsOf(person.Name));
            spouseValueLabel.Text = ListOrUnknown(SpousesOf(person.Name));
            if (CoparentsOf(person.Name).Any())
            {
                if (SpousesOf(person.Name).Any())
                    spouseValueLabel.Text += "  |  ";
                spouseValueLabel.Text += "Spouse: " + ListOrUnknown(CoparentsOf(person.Name));
            }
            childrenValueLabel.Text = ListOrUnknown(ChildrenOf(person.Name));
            string notes = NotesOf(person.Name);
            string researchReview = ResearchReviewText(person);
            funFactTextBox.Text = string.Join(Environment.NewLine, new[] {
                person.Details,
                notes,
                researchReview,
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
            if (_document == null) return;
            if (_isExperimental && _mainViewOwner != null)
            {
                _mainViewOwner._document = _document.DeepClone() as JsonObject;
                _mainViewOwner.RefreshPeople();
                return;
            }
            if (_experimentalView == null) return;
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
            string reviewText = ResearchReviewText(p);
            if (!string.IsNullOrWhiteSpace(reviewText)) sections.Add("\r\nUNVERIFIED RESEARCH REVIEW\r\n" + reviewText);
            if (_document["record_overrides"] is JsonArray reviewOverrides)
            {
                var personCodes = IdentifierArray.Where(identifier =>
                        Value(identifier, "display_name").Equals(p.OriginalName, StringComparison.OrdinalIgnoreCase))
                    .Select(identifier => Value(identifier, "code"))
                    .Append(p.Tg)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);
                AddSection("Research review leads (UNVERIFIED; not canonical facts)", reviewOverrides.Where(item =>
                    Value(item, "field") == "research_review_2026_11_18" &&
                    (Value(item, "display_name").Equals(p.OriginalName, StringComparison.OrdinalIgnoreCase) ||
                     personCodes.Contains(Value(item, "code")))));
            }
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

        private void EditPerson(PersonRow person, IWin32Window owner)
        {
            using var dialog = new Form
            {
                Text = "Edit Family Record", Width = 620, Height = 500,
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false, MinimizeBox = false
            };
            var nameBox = new TextBox { Left = 150, Top = 20, Width = 420, Text = person.Name };
            var tgBox = new TextBox { Left = 150, Top = 55, Width = 420, Text = person.Tg };
            var detailsBox = new TextBox { Left = 150, Top = 90, Width = 420, Height = 75, Multiline = true, ScrollBars = ScrollBars.Vertical, Text = person.Details };
            var parentsBox = new TextBox { Left = 150, Top = 180, Width = 420, Text = string.Join(", ", ParentsOf(person.Name)) };
            var spousesBox = new TextBox { Left = 150, Top = 215, Width = 420, Text = string.Join(", ", SpousesOf(person.Name).Concat(CoparentsOf(person.Name)).Distinct(StringComparer.OrdinalIgnoreCase)) };
            var childrenBox = new TextBox { Left = 150, Top = 250, Width = 420, Text = string.Join(", ", ChildrenOf(person.Name)) };
            string[] labels = { "Name:", "TG:", "Details:", "Parents:", "Spouse:", "Children:" };
            int[] tops = { 23, 58, 93, 183, 218, 253 };
            for (int i = 0; i < labels.Length; i++)
                dialog.Controls.Add(new Label { Left = 20, Top = tops[i], Width = 125, Height = 22, Text = labels[i] });
            var save = new Button { Text = "Save Changes", Left = 350, Top = 405, Width = 120, Height = 40, DialogResult = DialogResult.OK };
            var cancel = new Button { Text = "Cancel", Left = 475, Top = 405, Width = 95, Height = 40, DialogResult = DialogResult.Cancel };
            dialog.Controls.AddRange(new Control[] { nameBox, tgBox, detailsBox, parentsBox, spousesBox, childrenBox, save, cancel });
            dialog.AcceptButton = save; dialog.CancelButton = cancel;
            if (dialog.ShowDialog(owner) != DialogResult.OK) return;

            string newName = nameBox.Text.Trim(), newTg = tgBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(newName)) { MessageBox.Show(owner, "A name is required."); return; }

            JsonNode? existing = RecordOverrides.FirstOrDefault(r => Value(r, "original_name").Equals(person.OriginalName, StringComparison.OrdinalIgnoreCase));
            JsonObject overrideRecord = existing as JsonObject ?? new JsonObject { ["original_name"] = person.OriginalName };
            if (existing == null) RecordOverrides.Add(overrideRecord);
            overrideRecord["display_name"] = newName;
            overrideRecord["tg"] = newTg;
            overrideRecord["details"] = detailsBox.Text.Trim();
            overrideRecord["edited_parents"] = parentsBox.Text.Trim();
            overrideRecord["edited_spouse"] = spousesBox.Text.Trim();
            overrideRecord["edited_children"] = childrenBox.Text.Trim();

            foreach (JsonNode? relationship in RelationshipArray)
            {
                if (Value(relationship, "child").Equals(person.Name, StringComparison.OrdinalIgnoreCase)) relationship!["child"] = newName;
                if (Value(relationship, "parent").Equals(person.Name, StringComparison.OrdinalIgnoreCase)) relationship!["parent"] = newName;
            }
            foreach (JsonNode? relationship in SupplementalArray("spouse_relationships"))
            {
                if (Value(relationship, "person1").Equals(person.Name, StringComparison.OrdinalIgnoreCase)) relationship!["person1"] = newName;
                if (Value(relationship, "person2").Equals(person.Name, StringComparison.OrdinalIgnoreCase)) relationship!["person2"] = newName;
            }
            JsonNode? identifier = IdentifierArray.FirstOrDefault(id =>
                Value(id, "display_name").Equals(person.OriginalName, StringComparison.OrdinalIgnoreCase) && Value(id, "kind") == "TG");
            if (identifier != null && !string.IsNullOrWhiteSpace(newTg)) identifier!["code"] = newTg;

            for (int i = RelationshipArray.Count - 1; i >= 0; i--)
            {
                JsonNode? relationship = RelationshipArray[i];
                if (Value(relationship, "relation") == "parent" &&
                    (Value(relationship, "child").Equals(newName, StringComparison.OrdinalIgnoreCase) ||
                     Value(relationship, "parent").Equals(newName, StringComparison.OrdinalIgnoreCase)))
                    RelationshipArray.RemoveAt(i);
            }
            foreach (string parent in parentsBox.Text.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                RelationshipArray.Add(new JsonObject { ["child"] = newName, ["parent"] = parent, ["relation"] = "parent", ["confidence"] = 1.0, ["permanent"] = 0 });
            foreach (string child in childrenBox.Text.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                RelationshipArray.Add(new JsonObject { ["child"] = child, ["parent"] = newName, ["relation"] = "parent", ["confidence"] = 1.0, ["permanent"] = 0 });

            JsonArray spouses = SupplementalArray("spouse_relationships");
            for (int i = spouses.Count - 1; i >= 0; i--)
            {
                JsonNode? relationship = spouses[i];
                if (Value(relationship, "person1").Equals(newName, StringComparison.OrdinalIgnoreCase) ||
                    Value(relationship, "person2").Equals(newName, StringComparison.OrdinalIgnoreCase))
                    spouses.RemoveAt(i);
            }
            foreach (string spouse in spousesBox.Text.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                spouses.Add(new JsonObject { ["person1"] = newName, ["person2"] = spouse, ["relation"] = "spouse", ["permanent"] = 0 });

            RefreshPeople(newName);
            SynchronizeExperimentalView();
            UpdateStatus($"Saved edited record for {newName}. The original built-in record is preserved in the export override.");
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
            Action("Edit Person", () =>
            {
                if (memberList.SelectedItem is PersonRow person)
                    EditPerson(person, window);
                else MessageBox.Show(window, "Choose a family member first.");
                RefreshAboutMembers(memberList.SelectedItem is PersonRow selected ? selected.Name : null);
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
