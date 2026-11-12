//Copyright (c) November 6, 2026 Jeremiah Burke O'Neal

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


namespace Pedigree
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support.
        /// </summary>
        private void InitializeComponent()
        {
            rootTableLayout = new TableLayoutPanel();
            sharedNotesPanel = new Panel();
            sharedNotesLabel = new Label();
            sharedNotesTextBox = new TextBox();
            tabControl = new TabControl();
            mainTabPage = new TabPage();
            experimentalTabPage = new TabPage();
            treeTabPage = new TabPage();
            updateTabPage = new TabPage();
            friendsTabPage = new TabPage();
            treeView = new TreeView();
            friendsTableLayout = new TableLayoutPanel();
            friendsNoticeLabel = new Label();
            friendsInstructionsTextBox = new TextBox();
            friendsButtonsFlowLayout = new FlowLayoutPanel();
            friendsImportButton = new Button();
            friendsExportButton = new Button();
            friendsSampleButton = new Button();
            friendsTreeView = new TreeView();
            updateTableLayout = new TableLayoutPanel();
            updateNoticeLabel = new Label();
            updateInstructionsTextBox = new TextBox();
            updateButtonsFlowLayout = new FlowLayoutPanel();
            updateImportButton = new Button();
            updateExportButton = new Button();
            experimentalTextLabel = new Label();
            mainTableLayout = new TableLayoutPanel();
            headerPanel = new Panel();
            subtitleLabel = new Label();
            titleLabel = new Label();
            contentTableLayout = new TableLayoutPanel();
            memberPanel = new GroupBox();
            memberButtonsFlowLayout = new FlowLayoutPanel();
            addPersonButton = new Button();
            connectButton = new Button();
            familyListBox = new ListBox();
            searchLabel = new Label();
            searchTextBox = new TextBox();
            rightTableLayout = new TableLayoutPanel();
            detailsPanel = new GroupBox();
            detailsTableLayout = new TableLayoutPanel();
            childrenValueLabel = new Label();
            spouseValueLabel = new Label();
            parentsValueLabel = new Label();
            deathValueLabel = new Label();
            birthValueLabel = new Label();
            tgValueLabel = new Label();
            childrenLabel = new Label();
            spouseLabel = new Label();
            parentsLabel = new Label();
            deathLabel = new Label();
            birthLabel = new Label();
            tgLabel = new Label();
            nameLabel = new Label();
            nameValueLabel = new Label();
            funFactLabel = new Label();
            funFactTextBox = new TextBox();
            exportsPanel = new GroupBox();
            exportInnerTableLayout = new TableLayoutPanel();
            exportButtonsFlowLayout = new FlowLayoutPanel();
            importButton = new Button();
            exportPdfButton = new Button();
            exportJsonButton = new Button();
            aboutButton = new Button();
            exportStyleTextBox = new TextBox();
            footerPanel = new Panel();
            statusLabel = new Label();
            rootTableLayout.SuspendLayout();
            sharedNotesPanel.SuspendLayout();
            tabControl.SuspendLayout();
            mainTabPage.SuspendLayout();
            experimentalTabPage.SuspendLayout();
            updateTabPage.SuspendLayout();
            updateTableLayout.SuspendLayout();
            updateButtonsFlowLayout.SuspendLayout();
            friendsTabPage.SuspendLayout();
            friendsTableLayout.SuspendLayout();
            friendsButtonsFlowLayout.SuspendLayout();
            mainTableLayout.SuspendLayout();
            headerPanel.SuspendLayout();
            contentTableLayout.SuspendLayout();
            memberPanel.SuspendLayout();
            memberButtonsFlowLayout.SuspendLayout();
            rightTableLayout.SuspendLayout();
            detailsPanel.SuspendLayout();
            detailsTableLayout.SuspendLayout();
            exportsPanel.SuspendLayout();
            exportInnerTableLayout.SuspendLayout();
            exportButtonsFlowLayout.SuspendLayout();
            footerPanel.SuspendLayout();
            SuspendLayout();
            // 
            // mainTableLayout
            // 
            mainTableLayout.ColumnCount = 1;
            mainTableLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            mainTableLayout.Controls.Add(headerPanel, 0, 0);
            mainTableLayout.Controls.Add(contentTableLayout, 0, 1);
            mainTableLayout.Controls.Add(footerPanel, 0, 2);
            mainTableLayout.Dock = DockStyle.Fill;
            mainTableLayout.Location = new Point(0, 0);
            mainTableLayout.Name = "mainTableLayout";
            mainTableLayout.RowCount = 3;
            mainTableLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 95F));
            mainTableLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            mainTableLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 46F));
            mainTableLayout.Size = new Size(1692, 983);
            mainTableLayout.TabIndex = 0;
            // 
            // rootTableLayout
            // 
            rootTableLayout.ColumnCount = 1;
            rootTableLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            rootTableLayout.Controls.Add(sharedNotesPanel, 0, 0);
            rootTableLayout.Controls.Add(tabControl, 0, 1);
            rootTableLayout.Dock = DockStyle.Fill;
            rootTableLayout.Location = new Point(0, 0);
            rootTableLayout.Name = "rootTableLayout";
            rootTableLayout.RowCount = 2;
            rootTableLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 72F));
            rootTableLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            rootTableLayout.Size = new Size(1692, 983);
            rootTableLayout.TabIndex = 0;
            // sharedNotesPanel
            // 
            sharedNotesPanel.Controls.Add(sharedNotesTextBox);
            sharedNotesPanel.Controls.Add(sharedNotesLabel);
            sharedNotesPanel.Dock = DockStyle.Fill;
            sharedNotesPanel.Location = new Point(3, 3);
            sharedNotesPanel.Name = "sharedNotesPanel";
            sharedNotesPanel.Padding = new Padding(12, 8, 12, 8);
            sharedNotesPanel.Size = new Size(1686, 66);
            sharedNotesPanel.TabIndex = 0;
            // 
            // sharedNotesLabel
            // 
            sharedNotesLabel.AutoSize = true;
            sharedNotesLabel.Font = new Font("Segoe UI", 10F, FontStyle.Bold, GraphicsUnit.Point);
            sharedNotesLabel.Location = new Point(12, 20);
            sharedNotesLabel.Name = "sharedNotesLabel";
            sharedNotesLabel.Size = new Size(105, 28);
            sharedNotesLabel.TabIndex = 0;
            sharedNotesLabel.Text = "Shared Notes:";
            // 
            // sharedNotesTextBox
            // 
            sharedNotesTextBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            sharedNotesTextBox.Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);
            sharedNotesTextBox.Location = new Point(130, 11);
            sharedNotesTextBox.Multiline = true;
            sharedNotesTextBox.Name = "sharedNotesTextBox";
            sharedNotesTextBox.ScrollBars = ScrollBars.Vertical;
            sharedNotesTextBox.Size = new Size(1542, 46);
            sharedNotesTextBox.TabIndex = 1;
            // 
            // tabControl
            // 
            tabControl.Controls.Add(mainTabPage);
            tabControl.Controls.Add(experimentalTabPage);
            tabControl.Controls.Add(treeTabPage);
            tabControl.Controls.Add(updateTabPage);
            tabControl.Controls.Add(friendsTabPage);
            tabControl.Dock = DockStyle.Fill;
            tabControl.Location = new Point(0, 0);
            tabControl.Name = "tabControl";
            tabControl.SelectedIndex = 0;
            tabControl.Size = new Size(1692, 983);
            tabControl.TabIndex = 0;
            // 
            // mainTabPage
            // 
            mainTabPage.Controls.Add(mainTableLayout);
            mainTabPage.Location = new Point(4, 34);
            mainTabPage.Name = "mainTabPage";
            mainTabPage.Padding = new Padding(3);
            mainTabPage.Size = new Size(1684, 945);
            mainTabPage.TabIndex = 0;
            mainTabPage.Text = "Main";
            mainTabPage.UseVisualStyleBackColor = true;
            // 
            // experimentalTabPage
            // 
            experimentalTabPage.Location = new Point(4, 34);
            experimentalTabPage.Name = "experimentalTabPage";
            experimentalTabPage.Padding = new Padding(3);
            experimentalTabPage.Size = new Size(1684, 945);
            experimentalTabPage.TabIndex = 1;
            experimentalTabPage.Text = "Experimental";
            experimentalTabPage.UseVisualStyleBackColor = true;
            // 
            // treeTabPage
            // 
            treeTabPage.Controls.Add(treeView);
            treeTabPage.Location = new Point(4, 34);
            treeTabPage.Name = "treeTabPage";
            treeTabPage.Padding = new Padding(3);
            treeTabPage.Size = new Size(1684, 945);
            treeTabPage.TabIndex = 2;
            treeTabPage.Text = "Tree";
            treeTabPage.UseVisualStyleBackColor = true;
            // 
            // treeView
            // 
            treeView.Dock = DockStyle.Fill;
            treeView.Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);
            treeView.Location = new Point(3, 3);
            treeView.Name = "treeView";
            treeView.Size = new Size(1678, 939);
            treeView.TabIndex = 0;
            // 
            // updateTabPage
            // 
            updateTabPage.Controls.Add(updateTableLayout);
            updateTabPage.Location = new Point(4, 34);
            updateTabPage.Name = "updateTabPage";
            updateTabPage.Padding = new Padding(3);
            updateTabPage.Size = new Size(1684, 945);
            updateTabPage.TabIndex = 3;
            updateTabPage.Text = "ChatGPT Updates";
            updateTabPage.UseVisualStyleBackColor = true;
            // 
            // updateTableLayout
            // 
            updateTableLayout.ColumnCount = 1;
            updateTableLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            updateTableLayout.Controls.Add(updateNoticeLabel, 0, 0);
            updateTableLayout.Controls.Add(updateInstructionsTextBox, 0, 1);
            updateTableLayout.Controls.Add(updateButtonsFlowLayout, 0, 2);
            updateTableLayout.Dock = DockStyle.Fill;
            updateTableLayout.RowCount = 3;
            updateTableLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 90F));
            updateTableLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            updateTableLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 72F));
            updateTableLayout.Padding = new Padding(20);
            updateTableLayout.Name = "updateTableLayout";
            // 
            // updateNoticeLabel
            // 
            updateNoticeLabel.Dock = DockStyle.Fill;
            updateNoticeLabel.Font = new Font("Segoe UI", 11F, FontStyle.Bold, GraphicsUnit.Point);
            updateNoticeLabel.Text = "READ-ONLY FAMILY TREE\r\nChanges to the data below will not change the names in the Pedigree program. Use the import format to request additions and relationship updates.";
            updateNoticeLabel.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // updateInstructionsTextBox
            // 
            updateInstructionsTextBox.Dock = DockStyle.Fill;
            updateInstructionsTextBox.Multiline = true;
            updateInstructionsTextBox.ReadOnly = true;
            updateInstructionsTextBox.ScrollBars = ScrollBars.Both;
            updateInstructionsTextBox.Font = new Font("Consolas", 10F, FontStyle.Regular, GraphicsUnit.Point);
            updateInstructionsTextBox.Name = "updateInstructionsTextBox";
            // 
            // updateButtonsFlowLayout
            // 
            updateButtonsFlowLayout.Dock = DockStyle.Fill;
            updateButtonsFlowLayout.FlowDirection = FlowDirection.LeftToRight;
            updateButtonsFlowLayout.Controls.Add(updateImportButton);
            updateButtonsFlowLayout.Controls.Add(updateExportButton);
            // 
            // updateImportButton
            // 
            updateImportButton.Font = new Font("Segoe UI", 10F, FontStyle.Bold, GraphicsUnit.Point);
            updateImportButton.Size = new Size(220, 50);
            updateImportButton.Text = "Import ChatGPT JSON";
            updateImportButton.Click += updateImportButton_Click;
            // 
            // updateExportButton
            // 
            updateExportButton.Font = new Font("Segoe UI", 10F, FontStyle.Bold, GraphicsUnit.Point);
            updateExportButton.Size = new Size(220, 50);
            updateExportButton.Text = "Export ChatGPT JSON";
            updateExportButton.Click += updateExportButton_Click;
            // 
            // friendsTabPage
            // 
            friendsTabPage.Controls.Add(friendsTableLayout);
            friendsTabPage.Location = new Point(4, 34);
            friendsTabPage.Name = "friendsTabPage";
            friendsTabPage.Padding = new Padding(3);
            friendsTabPage.Size = new Size(1684, 945);
            friendsTabPage.TabIndex = 4;
            friendsTabPage.Text = "Friends";
            friendsTabPage.UseVisualStyleBackColor = true;
            // 
            // friendsTableLayout
            // 
            friendsTableLayout.ColumnCount = 1;
            friendsTableLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            friendsTableLayout.Controls.Add(friendsNoticeLabel, 0, 0);
            friendsTableLayout.Controls.Add(friendsInstructionsTextBox, 0, 1);
            friendsTableLayout.Controls.Add(friendsButtonsFlowLayout, 0, 2);
            friendsTableLayout.Controls.Add(friendsTreeView, 0, 3);
            friendsTableLayout.Dock = DockStyle.Fill;
            friendsTableLayout.Name = "friendsTableLayout";
            friendsTableLayout.Padding = new Padding(20);
            friendsTableLayout.RowCount = 4;
            friendsTableLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 55F));
            friendsTableLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 180F));
            friendsTableLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 70F));
            friendsTableLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            // 
            // friendsNoticeLabel
            // 
            friendsNoticeLabel.Dock = DockStyle.Fill;
            friendsNoticeLabel.Font = new Font("Segoe UI", 11F, FontStyle.Bold, GraphicsUnit.Point);
            friendsNoticeLabel.Text = "FRIENDS FAMILY TREE\r\nBP.json is a separate family package. Tree links identify existing people without changing their names.";
            friendsNoticeLabel.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // friendsInstructionsTextBox
            // 
            friendsInstructionsTextBox.Dock = DockStyle.Fill;
            friendsInstructionsTextBox.Font = new Font("Consolas", 10F, FontStyle.Regular, GraphicsUnit.Point);
            friendsInstructionsTextBox.Multiline = true;
            friendsInstructionsTextBox.ReadOnly = true;
            friendsInstructionsTextBox.ScrollBars = ScrollBars.Both;
            friendsInstructionsTextBox.Name = "friendsInstructionsTextBox";
            // 
            // friendsButtonsFlowLayout
            // 
            friendsButtonsFlowLayout.Controls.Add(friendsImportButton);
            friendsButtonsFlowLayout.Controls.Add(friendsExportButton);
            friendsButtonsFlowLayout.Controls.Add(friendsSampleButton);
            friendsButtonsFlowLayout.Dock = DockStyle.Fill;
            friendsButtonsFlowLayout.FlowDirection = FlowDirection.LeftToRight;
            // 
            // friendsImportButton
            // 
            friendsImportButton.Font = new Font("Segoe UI", 10F, FontStyle.Bold, GraphicsUnit.Point);
            friendsImportButton.Size = new Size(210, 50);
            friendsImportButton.Text = "Import BP.json";
            friendsImportButton.Click += friendsImportButton_Click;
            // 
            // friendsExportButton
            // 
            friendsExportButton.Font = new Font("Segoe UI", 10F, FontStyle.Bold, GraphicsUnit.Point);
            friendsExportButton.Size = new Size(210, 50);
            friendsExportButton.Text = "Write BP.json";
            friendsExportButton.Click += friendsExportButton_Click;
            // 
            // friendsSampleButton
            // 
            friendsSampleButton.Font = new Font("Segoe UI", 10F, FontStyle.Bold, GraphicsUnit.Point);
            friendsSampleButton.Size = new Size(250, 50);
            friendsSampleButton.Text = "Create BP-sample.json";
            friendsSampleButton.Click += friendsSampleButton_Click;
            // 
            // friendsTreeView
            // 
            friendsTreeView.Dock = DockStyle.Fill;
            friendsTreeView.Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);
            friendsTreeView.Name = "friendsTreeView";
            // 
            // experimentalTextLabel
            // 
            experimentalTextLabel.Dock = DockStyle.Fill;
            experimentalTextLabel.Font = new Font("Segoe UI", 11F, FontStyle.Regular, GraphicsUnit.Point);
            experimentalTextLabel.Location = new Point(3, 3);
            experimentalTextLabel.Name = "experimentalTextLabel";
            experimentalTextLabel.Padding = new Padding(20);
            experimentalTextLabel.Size = new Size(1678, 939);
            experimentalTextLabel.TabIndex = 0;
            experimentalTextLabel.Text = "To remove this text, tell the agent that we're using process 2. The agent should add any future changes to this tab";
            experimentalTextLabel.TextAlign = ContentAlignment.TopLeft;
            // 
            // headerPanel
            // 
            headerPanel.Controls.Add(subtitleLabel);
            headerPanel.Controls.Add(titleLabel);
            headerPanel.Dock = DockStyle.Fill;
            headerPanel.Location = new Point(3, 3);
            headerPanel.Name = "headerPanel";
            headerPanel.Padding = new Padding(20, 12, 20, 12);
            headerPanel.Size = new Size(1686, 89);
            headerPanel.TabIndex = 0;
            // 
            // subtitleLabel
            // 
            subtitleLabel.AutoSize = true;
            subtitleLabel.Font = new Font("Segoe UI", 11F, FontStyle.Regular, GraphicsUnit.Point);
            subtitleLabel.Location = new Point(25, 49);
            subtitleLabel.Name = "subtitleLabel";
            subtitleLabel.Size = new Size(703, 30);
            subtitleLabel.TabIndex = 1;
            subtitleLabel.Text = "A simple family history organizer with a friendly classroom project style";
            // 
            // titleLabel
            // 
            titleLabel.AutoSize = true;
            titleLabel.Font = new Font("Segoe UI", 18F, FontStyle.Bold, GraphicsUnit.Point);
            titleLabel.Location = new Point(19, 6);
            titleLabel.Name = "titleLabel";
            titleLabel.Size = new Size(248, 48);
            titleLabel.TabIndex = 0;
            titleLabel.Text = "My Family Tree";
            // 
            // contentTableLayout
            // 
            contentTableLayout.ColumnCount = 2;
            contentTableLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 390F));
            contentTableLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            contentTableLayout.Controls.Add(memberPanel, 0, 0);
            contentTableLayout.Controls.Add(rightTableLayout, 1, 0);
            contentTableLayout.Dock = DockStyle.Fill;
            contentTableLayout.Location = new Point(3, 98);
            contentTableLayout.Name = "contentTableLayout";
            contentTableLayout.Padding = new Padding(14, 8, 14, 8);
            contentTableLayout.RowCount = 1;
            contentTableLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            contentTableLayout.Size = new Size(1686, 836);
            contentTableLayout.TabIndex = 1;
            // 
            // memberPanel
            // 
            memberPanel.Controls.Add(memberButtonsFlowLayout);
            memberPanel.Controls.Add(familyListBox);
            memberPanel.Controls.Add(searchLabel);
            memberPanel.Controls.Add(searchTextBox);
            memberPanel.Dock = DockStyle.Fill;
            memberPanel.Font = new Font("Segoe UI", 12F, FontStyle.Bold, GraphicsUnit.Point);
            memberPanel.Location = new Point(17, 11);
            memberPanel.Name = "memberPanel";
            memberPanel.Padding = new Padding(14);
            memberPanel.Size = new Size(384, 810);
            memberPanel.TabIndex = 0;
            memberPanel.TabStop = false;
            memberPanel.Text = "Family Members";
            // 
            // memberButtonsFlowLayout
            // 
            memberButtonsFlowLayout.Controls.Add(addPersonButton);
            memberButtonsFlowLayout.Controls.Add(connectButton);
            memberButtonsFlowLayout.Dock = DockStyle.Bottom;
            memberButtonsFlowLayout.FlowDirection = FlowDirection.TopDown;
            memberButtonsFlowLayout.Location = new Point(14, 672);
            memberButtonsFlowLayout.Name = "memberButtonsFlowLayout";
            memberButtonsFlowLayout.Size = new Size(356, 124);
            memberButtonsFlowLayout.TabIndex = 3;
            // 
            // addPersonButton
            // 
            addPersonButton.Font = new Font("Segoe UI", 10F, FontStyle.Bold, GraphicsUnit.Point);
            addPersonButton.Location = new Point(3, 3);
            addPersonButton.Name = "addPersonButton";
            addPersonButton.Size = new Size(330, 50);
            addPersonButton.TabIndex = 0;
            addPersonButton.Text = "Add Person";
            addPersonButton.UseVisualStyleBackColor = true;
            addPersonButton.Click += addPersonButton_Click;
            // 
            // connectButton
            // 
            connectButton.Font = new Font("Segoe UI", 10F, FontStyle.Bold, GraphicsUnit.Point);
            connectButton.Location = new Point(3, 59);
            connectButton.Name = "connectButton";
            connectButton.Size = new Size(330, 50);
            connectButton.TabIndex = 1;
            connectButton.Text = "Connect Family Members";
            connectButton.UseVisualStyleBackColor = true;
            connectButton.Click += connectButton_Click;
            // 
            // familyListBox
            // 
            familyListBox.Dock = DockStyle.Top;
            familyListBox.Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);
            familyListBox.FormattingEnabled = true;
            familyListBox.ItemHeight = 28;
            familyListBox.Location = new Point(14, 104);
            familyListBox.Name = "familyListBox";
            familyListBox.Size = new Size(356, 564);
            familyListBox.TabIndex = 2;
            familyListBox.SelectedIndexChanged += familyListBox_SelectedIndexChanged;
            // 
            // searchLabel
            // 
            searchLabel.AutoSize = true;
            searchLabel.Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);
            searchLabel.Location = new Point(18, 39);
            searchLabel.Name = "searchLabel";
            searchLabel.Size = new Size(145, 28);
            searchLabel.TabIndex = 0;
            searchLabel.Text = "Find a person:";
            // 
            // searchTextBox
            // 
            searchTextBox.Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);
            searchTextBox.Location = new Point(18, 67);
            searchTextBox.Name = "searchTextBox";
            searchTextBox.Size = new Size(342, 34);
            searchTextBox.TabIndex = 1;
            searchTextBox.TextChanged += searchTextBox_TextChanged;
            // 
            // rightTableLayout
            // 
            rightTableLayout.ColumnCount = 1;
            rightTableLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            rightTableLayout.Controls.Add(detailsPanel, 0, 0);
            rightTableLayout.Controls.Add(exportsPanel, 0, 1);
            rightTableLayout.Dock = DockStyle.Fill;
            rightTableLayout.Location = new Point(407, 11);
            rightTableLayout.Name = "rightTableLayout";
            rightTableLayout.RowCount = 2;
            rightTableLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 58F));
            rightTableLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 42F));
            rightTableLayout.Size = new Size(1262, 810);
            rightTableLayout.TabIndex = 1;
            // 
            // detailsPanel
            // 
            detailsPanel.Controls.Add(detailsTableLayout);
            detailsPanel.Dock = DockStyle.Fill;
            detailsPanel.Font = new Font("Segoe UI", 12F, FontStyle.Bold, GraphicsUnit.Point);
            detailsPanel.Location = new Point(3, 3);
            detailsPanel.Name = "detailsPanel";
            detailsPanel.Padding = new Padding(14);
            detailsPanel.Size = new Size(1256, 463);
            detailsPanel.TabIndex = 0;
            detailsPanel.TabStop = false;
            detailsPanel.Text = "Person Card";
            // 
            // detailsTableLayout
            // 
            detailsTableLayout.ColumnCount = 2;
            detailsTableLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 215F));
            detailsTableLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            detailsTableLayout.Controls.Add(childrenValueLabel, 1, 6);
            detailsTableLayout.Controls.Add(spouseValueLabel, 1, 5);
            detailsTableLayout.Controls.Add(parentsValueLabel, 1, 4);
            detailsTableLayout.Controls.Add(deathValueLabel, 1, 3);
            detailsTableLayout.Controls.Add(birthValueLabel, 1, 2);
            detailsTableLayout.Controls.Add(tgValueLabel, 1, 1);
            detailsTableLayout.Controls.Add(childrenLabel, 0, 6);
            detailsTableLayout.Controls.Add(spouseLabel, 0, 5);
            detailsTableLayout.Controls.Add(parentsLabel, 0, 4);
            detailsTableLayout.Controls.Add(deathLabel, 0, 3);
            detailsTableLayout.Controls.Add(birthLabel, 0, 2);
            detailsTableLayout.Controls.Add(tgLabel, 0, 1);
            detailsTableLayout.Controls.Add(nameLabel, 0, 0);
            detailsTableLayout.Controls.Add(nameValueLabel, 1, 0);
            detailsTableLayout.Controls.Add(funFactLabel, 0, 7);
            detailsTableLayout.Controls.Add(funFactTextBox, 1, 7);
            detailsTableLayout.Dock = DockStyle.Fill;
            detailsTableLayout.Location = new Point(14, 46);
            detailsTableLayout.Name = "detailsTableLayout";
            detailsTableLayout.RowCount = 8;
            detailsTableLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44F));
            detailsTableLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44F));
            detailsTableLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44F));
            detailsTableLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44F));
            detailsTableLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44F));
            detailsTableLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44F));
            detailsTableLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44F));
            detailsTableLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            detailsTableLayout.Size = new Size(1228, 403);
            detailsTableLayout.TabIndex = 0;
            // 
            // childrenValueLabel
            // 
            childrenValueLabel.AutoSize = true;
            childrenValueLabel.Dock = DockStyle.Fill;
            childrenValueLabel.Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);
            childrenValueLabel.Location = new Point(218, 264);
            childrenValueLabel.Name = "childrenValueLabel";
            childrenValueLabel.Size = new Size(1007, 44);
            childrenValueLabel.TabIndex = 13;
            childrenValueLabel.Text = "";
            childrenValueLabel.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // spouseValueLabel
            // 
            spouseValueLabel.AutoSize = true;
            spouseValueLabel.Dock = DockStyle.Fill;
            spouseValueLabel.Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);
            spouseValueLabel.Location = new Point(218, 220);
            spouseValueLabel.Name = "spouseValueLabel";
            spouseValueLabel.Size = new Size(1007, 44);
            spouseValueLabel.TabIndex = 11;
            spouseValueLabel.Text = "";
            spouseValueLabel.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // parentsValueLabel
            // 
            parentsValueLabel.AutoSize = true;
            parentsValueLabel.Dock = DockStyle.Fill;
            parentsValueLabel.Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);
            parentsValueLabel.Location = new Point(218, 176);
            parentsValueLabel.Name = "parentsValueLabel";
            parentsValueLabel.Size = new Size(1007, 44);
            parentsValueLabel.TabIndex = 9;
            parentsValueLabel.Text = "";
            parentsValueLabel.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // deathValueLabel
            // 
            deathValueLabel.AutoSize = true;
            deathValueLabel.Dock = DockStyle.Fill;
            deathValueLabel.Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);
            deathValueLabel.Location = new Point(218, 132);
            deathValueLabel.Name = "deathValueLabel";
            deathValueLabel.Size = new Size(1007, 44);
            deathValueLabel.TabIndex = 7;
            deathValueLabel.Text = "";
            deathValueLabel.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // birthValueLabel
            // 
            birthValueLabel.AutoSize = true;
            birthValueLabel.Dock = DockStyle.Fill;
            birthValueLabel.Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);
            birthValueLabel.Location = new Point(218, 88);
            birthValueLabel.Name = "birthValueLabel";
            birthValueLabel.Size = new Size(1007, 44);
            birthValueLabel.TabIndex = 5;
            birthValueLabel.Text = "";
            birthValueLabel.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // tgValueLabel
            // 
            tgValueLabel.AutoSize = true;
            tgValueLabel.Dock = DockStyle.Fill;
            tgValueLabel.Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);
            tgValueLabel.Location = new Point(218, 44);
            tgValueLabel.Name = "tgValueLabel";
            tgValueLabel.Size = new Size(1007, 44);
            tgValueLabel.TabIndex = 3;
            tgValueLabel.Text = "";
            tgValueLabel.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // childrenLabel
            // 
            childrenLabel.AutoSize = true;
            childrenLabel.Dock = DockStyle.Fill;
            childrenLabel.Font = new Font("Segoe UI", 10F, FontStyle.Bold, GraphicsUnit.Point);
            childrenLabel.Location = new Point(3, 264);
            childrenLabel.Name = "childrenLabel";
            childrenLabel.Size = new Size(209, 44);
            childrenLabel.TabIndex = 12;
            childrenLabel.Text = "Children";
            childrenLabel.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // spouseLabel
            // 
            spouseLabel.AutoSize = true;
            spouseLabel.Dock = DockStyle.Fill;
            spouseLabel.Font = new Font("Segoe UI", 10F, FontStyle.Bold, GraphicsUnit.Point);
            spouseLabel.Location = new Point(3, 220);
            spouseLabel.Name = "spouseLabel";
            spouseLabel.Size = new Size(209, 44);
            spouseLabel.TabIndex = 10;
            spouseLabel.Text = "Spouse";
            spouseLabel.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // parentsLabel
            // 
            parentsLabel.AutoSize = true;
            parentsLabel.Dock = DockStyle.Fill;
            parentsLabel.Font = new Font("Segoe UI", 10F, FontStyle.Bold, GraphicsUnit.Point);
            parentsLabel.Location = new Point(3, 176);
            parentsLabel.Name = "parentsLabel";
            parentsLabel.Size = new Size(209, 44);
            parentsLabel.TabIndex = 8;
            parentsLabel.Text = "Parents";
            parentsLabel.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // deathLabel
            // 
            deathLabel.AutoSize = true;
            deathLabel.Dock = DockStyle.Fill;
            deathLabel.Font = new Font("Segoe UI", 10F, FontStyle.Bold, GraphicsUnit.Point);
            deathLabel.Location = new Point(3, 132);
            deathLabel.Name = "deathLabel";
            deathLabel.Size = new Size(209, 44);
            deathLabel.TabIndex = 6;
            deathLabel.Text = "Died";
            deathLabel.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // birthLabel
            // 
            birthLabel.AutoSize = true;
            birthLabel.Dock = DockStyle.Fill;
            birthLabel.Font = new Font("Segoe UI", 10F, FontStyle.Bold, GraphicsUnit.Point);
            birthLabel.Location = new Point(3, 88);
            birthLabel.Name = "birthLabel";
            birthLabel.Size = new Size(209, 44);
            birthLabel.TabIndex = 4;
            birthLabel.Text = "Born";
            birthLabel.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // tgLabel
            // 
            tgLabel.AutoSize = true;
            tgLabel.Dock = DockStyle.Fill;
            tgLabel.Font = new Font("Segoe UI", 10F, FontStyle.Bold, GraphicsUnit.Point);
            tgLabel.Location = new Point(3, 44);
            tgLabel.Name = "tgLabel";
            tgLabel.Size = new Size(209, 44);
            tgLabel.TabIndex = 2;
            tgLabel.Text = "TG Number";
            tgLabel.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // nameLabel
            // 
            nameLabel.AutoSize = true;
            nameLabel.Dock = DockStyle.Fill;
            nameLabel.Font = new Font("Segoe UI", 10F, FontStyle.Bold, GraphicsUnit.Point);
            nameLabel.Location = new Point(3, 0);
            nameLabel.Name = "nameLabel";
            nameLabel.Size = new Size(209, 44);
            nameLabel.TabIndex = 0;
            nameLabel.Text = "Name";
            nameLabel.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // nameValueLabel
            // 
            nameValueLabel.AutoSize = true;
            nameValueLabel.Dock = DockStyle.Fill;
            nameValueLabel.Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);
            nameValueLabel.Location = new Point(218, 0);
            nameValueLabel.Name = "nameValueLabel";
            nameValueLabel.Size = new Size(1007, 44);
            nameValueLabel.TabIndex = 1;
            nameValueLabel.Text = "";
            nameValueLabel.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // funFactLabel
            // 
            funFactLabel.AutoSize = true;
            funFactLabel.Dock = DockStyle.Fill;
            funFactLabel.Font = new Font("Segoe UI", 10F, FontStyle.Bold, GraphicsUnit.Point);
            funFactLabel.Location = new Point(3, 308);
            funFactLabel.Name = "funFactLabel";
            funFactLabel.Size = new Size(209, 95);
            funFactLabel.TabIndex = 14;
            funFactLabel.Text = "Fun Fact";
            funFactLabel.TextAlign = ContentAlignment.TopLeft;
            // 
            // funFactTextBox
            // 
            funFactTextBox.Dock = DockStyle.Fill;
            funFactTextBox.Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);
            funFactTextBox.Location = new Point(218, 311);
            funFactTextBox.Multiline = true;
            funFactTextBox.Name = "funFactTextBox";
            funFactTextBox.ReadOnly = true;
            funFactTextBox.ScrollBars = ScrollBars.Vertical;
            funFactTextBox.Size = new Size(1007, 89);
            funFactTextBox.TabIndex = 15;
            // 
            // exportsPanel
            // 
            exportsPanel.Controls.Add(exportInnerTableLayout);
            exportsPanel.Dock = DockStyle.Fill;
            exportsPanel.Font = new Font("Segoe UI", 12F, FontStyle.Bold, GraphicsUnit.Point);
            exportsPanel.Location = new Point(3, 472);
            exportsPanel.Name = "exportsPanel";
            exportsPanel.Padding = new Padding(14);
            exportsPanel.Size = new Size(1256, 335);
            exportsPanel.TabIndex = 1;
            exportsPanel.TabStop = false;
            exportsPanel.Text = "Exports and Project Notes";
            // 
            // exportInnerTableLayout
            // 
            exportInnerTableLayout.ColumnCount = 1;
            exportInnerTableLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            exportInnerTableLayout.Controls.Add(exportButtonsFlowLayout, 0, 0);
            exportInnerTableLayout.Controls.Add(exportStyleTextBox, 0, 1);
            exportInnerTableLayout.Dock = DockStyle.Fill;
            exportInnerTableLayout.Location = new Point(14, 46);
            exportInnerTableLayout.Name = "exportInnerTableLayout";
            exportInnerTableLayout.RowCount = 2;
            exportInnerTableLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 70F));
            exportInnerTableLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            exportInnerTableLayout.Size = new Size(1228, 275);
            exportInnerTableLayout.TabIndex = 0;
            // 
            // exportButtonsFlowLayout
            // 
            exportButtonsFlowLayout.Controls.Add(importButton);
            exportButtonsFlowLayout.Controls.Add(exportPdfButton);
            exportButtonsFlowLayout.Controls.Add(exportJsonButton);
            exportButtonsFlowLayout.Controls.Add(aboutButton);
            exportButtonsFlowLayout.Dock = DockStyle.Fill;
            exportButtonsFlowLayout.Location = new Point(3, 3);
            exportButtonsFlowLayout.Name = "exportButtonsFlowLayout";
            exportButtonsFlowLayout.Size = new Size(1222, 64);
            exportButtonsFlowLayout.TabIndex = 0;
            // 
            // importButton
            // 
            importButton.Font = new Font("Segoe UI", 10F, FontStyle.Bold, GraphicsUnit.Point);
            importButton.Location = new Point(3, 3);
            importButton.Name = "importButton";
            importButton.Size = new Size(190, 50);
            importButton.TabIndex = 0;
            importButton.Text = "Import JSON";
            importButton.UseVisualStyleBackColor = true;
            importButton.Click += importButton_Click;
            // 
            // exportPdfButton
            // 
            exportPdfButton.Font = new Font("Segoe UI", 10F, FontStyle.Bold, GraphicsUnit.Point);
            exportPdfButton.Location = new Point(199, 3);
            exportPdfButton.Name = "exportPdfButton";
            exportPdfButton.Size = new Size(190, 50);
            exportPdfButton.TabIndex = 1;
            exportPdfButton.Text = "Export PDF";
            exportPdfButton.UseVisualStyleBackColor = true;
            exportPdfButton.Click += exportPdfButton_Click;
            // 
            // exportJsonButton
            // 
            exportJsonButton.Font = new Font("Segoe UI", 10F, FontStyle.Bold, GraphicsUnit.Point);
            exportJsonButton.Location = new Point(395, 3);
            exportJsonButton.Name = "exportJsonButton";
            exportJsonButton.Size = new Size(190, 50);
            exportJsonButton.TabIndex = 2;
            exportJsonButton.Text = "Export Everything";
            exportJsonButton.UseVisualStyleBackColor = true;
            exportJsonButton.Click += exportJsonButton_Click;
            // 
            // aboutButton
            // 
            aboutButton.Font = new Font("Segoe UI", 10F, FontStyle.Bold, GraphicsUnit.Point);
            aboutButton.Location = new Point(591, 3);
            aboutButton.Name = "aboutButton";
            aboutButton.Size = new Size(190, 50);
            aboutButton.TabIndex = 3;
            aboutButton.Text = "About";
            aboutButton.UseVisualStyleBackColor = true;
            aboutButton.Click += aboutButton_Click;
            // 
            // exportStyleTextBox
            // 
            exportStyleTextBox.Dock = DockStyle.Fill;
            exportStyleTextBox.Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);
            exportStyleTextBox.Location = new Point(3, 73);
            exportStyleTextBox.Multiline = true;
            exportStyleTextBox.Name = "exportStyleTextBox";
            exportStyleTextBox.ReadOnly = true;
            exportStyleTextBox.ScrollBars = ScrollBars.Vertical;
            exportStyleTextBox.Size = new Size(1222, 199);
            exportStyleTextBox.TabIndex = 1;
            // 
            // footerPanel
            // 
            footerPanel.Controls.Add(statusLabel);
            footerPanel.Dock = DockStyle.Fill;
            footerPanel.Location = new Point(3, 940);
            footerPanel.Name = "footerPanel";
            footerPanel.Padding = new Padding(18, 10, 18, 8);
            footerPanel.Size = new Size(1686, 40);
            footerPanel.TabIndex = 2;
            // 
            // statusLabel
            // 
            statusLabel.AutoSize = true;
            statusLabel.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
            statusLabel.Location = new Point(18, 10);
            statusLabel.Name = "statusLabel";
            statusLabel.Size = new Size(0, 25);
            statusLabel.TabIndex = 0;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(10F, 25F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1692, 983);
            Controls.Add(rootTableLayout);
            MinimumSize = new Size(1300, 850);
            Name = "Form1";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "My Family Tree";
            Load += Form1_Load;
            rootTableLayout.ResumeLayout(false);
            sharedNotesPanel.ResumeLayout(false);
            sharedNotesPanel.PerformLayout();
            tabControl.ResumeLayout(false);
            mainTabPage.ResumeLayout(false);
            experimentalTabPage.ResumeLayout(false);
            updateTabPage.ResumeLayout(false);
            updateTableLayout.ResumeLayout(false);
            updateTableLayout.PerformLayout();
            updateButtonsFlowLayout.ResumeLayout(false);
            friendsTabPage.ResumeLayout(false);
            friendsTableLayout.ResumeLayout(false);
            friendsTableLayout.PerformLayout();
            friendsButtonsFlowLayout.ResumeLayout(false);
            mainTableLayout.ResumeLayout(false);
            headerPanel.ResumeLayout(false);
            headerPanel.PerformLayout();
            contentTableLayout.ResumeLayout(false);
            memberPanel.ResumeLayout(false);
            memberPanel.PerformLayout();
            memberButtonsFlowLayout.ResumeLayout(false);
            rightTableLayout.ResumeLayout(false);
            detailsPanel.ResumeLayout(false);
            detailsTableLayout.ResumeLayout(false);
            detailsTableLayout.PerformLayout();
            exportsPanel.ResumeLayout(false);
            exportInnerTableLayout.ResumeLayout(false);
            exportInnerTableLayout.PerformLayout();
            exportButtonsFlowLayout.ResumeLayout(false);
            footerPanel.ResumeLayout(false);
            footerPanel.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private TableLayoutPanel mainTableLayout;
        private TableLayoutPanel rootTableLayout;
        private Panel sharedNotesPanel;
        private Label sharedNotesLabel;
        private TextBox sharedNotesTextBox;
        private TabControl tabControl;
        private TabPage mainTabPage;
        private TabPage experimentalTabPage;
        private TabPage treeTabPage;
        private TabPage updateTabPage;
        private TabPage friendsTabPage;
        private TreeView treeView;
        private TableLayoutPanel friendsTableLayout;
        private Label friendsNoticeLabel;
        private TextBox friendsInstructionsTextBox;
        private FlowLayoutPanel friendsButtonsFlowLayout;
        private Button friendsImportButton;
        private Button friendsExportButton;
        private Button friendsSampleButton;
        private TreeView friendsTreeView;
        private TableLayoutPanel updateTableLayout;
        private Label updateNoticeLabel;
        private TextBox updateInstructionsTextBox;
        private FlowLayoutPanel updateButtonsFlowLayout;
        private Button updateImportButton;
        private Button updateExportButton;
        private Label experimentalTextLabel;
        private Panel headerPanel;
        private Label titleLabel;
        private Label subtitleLabel;
        private TableLayoutPanel contentTableLayout;
        private GroupBox memberPanel;
        private Label searchLabel;
        private TextBox searchTextBox;
        private ListBox familyListBox;
        private FlowLayoutPanel memberButtonsFlowLayout;
        private Button addPersonButton;
        private Button connectButton;
        private TableLayoutPanel rightTableLayout;
        private GroupBox detailsPanel;
        private TableLayoutPanel detailsTableLayout;
        private Label nameLabel;
        private Label nameValueLabel;
        private Label tgLabel;
        private Label tgValueLabel;
        private Label birthLabel;
        private Label birthValueLabel;
        private Label deathLabel;
        private Label deathValueLabel;
        private Label parentsLabel;
        private Label parentsValueLabel;
        private Label spouseLabel;
        private Label spouseValueLabel;
        private Label childrenLabel;
        private Label childrenValueLabel;
        private Label funFactLabel;
        private TextBox funFactTextBox;
        private GroupBox exportsPanel;
        private TableLayoutPanel exportInnerTableLayout;
        private FlowLayoutPanel exportButtonsFlowLayout;
        private Button importButton;
        private Button exportPdfButton;
        private Button exportJsonButton;
        private Button aboutButton;
        private TextBox exportStyleTextBox;
        private Panel footerPanel;
        private Label statusLabel;
    }
}
