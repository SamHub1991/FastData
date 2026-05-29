using FastData.Database;
using FastData.Tooling.CodeGeneration;
using FastData.Tooling.Database;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace FastData.ModelGenerator.WinForms
{
    public class MainForm : Form
    {
        private readonly TabControl tabs = new TabControl { Dock = DockStyle.Fill };
        private readonly EnhancedCodeGenerator _generator = new EnhancedCodeGenerator();
        private IList<DatabaseTable> _tables = new List<DatabaseTable>();

        // Tab 1: 连接管理
        private readonly ListBox connListBox = new ListBox();
        private readonly TextBox connNameBox = new TextBox();
        private readonly ComboBox connProviderBox = new ComboBox();
        private readonly TextBox connStringBox = new TextBox();
        private readonly Button connSaveBtn = new Button();
        private readonly Button connDeleteBtn = new Button();
        private readonly Button connTestBtn = new Button();
        private readonly Button connLoadBtn = new Button();
        private readonly Label connStatusLabel = new Label();

        // Tab 2-3-4 共享的数据库连接选择
        private readonly ComboBox dbProviderBox = new ComboBox();
        private readonly TextBox dbConnStrBox = new TextBox();
        private readonly Button dbTestBtn = new Button();
        private readonly Button dbLoadTablesBtn = new Button();
        private readonly Button dbManageBtn = new Button();
        private readonly CheckBox includeViewsBox = new CheckBox { Text = "包含视图", Checked = true, AutoSize = true };

        // Tab 2: Model 生成
        private readonly TextBox modelNamespaceBox = new TextBox();
        private readonly TextBox modelOutDirBox = new TextBox();
        private readonly ListBox modelTableList = new ListBox();
        private readonly TextBox modelSearchBox = new TextBox();
        private readonly TextBox modelPreviewBox = new TextBox();
        private readonly Button modelPreviewBtn = new Button();
        private readonly Button modelGenerateBtn = new Button();

        // Tab 3: XML Map 生成
        private readonly TextBox xmlNamespaceBox = new TextBox();
        private readonly TextBox xmlOutDirBox = new TextBox();
        private readonly ListBox xmlTableList = new ListBox();
        private readonly TextBox xmlSearchBox = new TextBox();
        private readonly TextBox xmlPreviewBox = new TextBox();
        private readonly Button xmlPreviewBtn = new Button();
        private readonly Button xmlGenerateBtn = new Button();

        // Tab 4: 代码生成
        private readonly TextBox codeNamespaceBox = new TextBox();
        private readonly TextBox codeOutDirBox = new TextBox();
        private readonly ListBox codeTableList = new ListBox();
        private readonly TextBox codeSearchBox = new TextBox();
        private readonly TextBox codePreviewBox = new TextBox();

        // Tab 5: JSON 转 Model
        private readonly TextBox jsonInputBox = new TextBox();
        private readonly TextBox jsonClassNameBox = new TextBox();
        private readonly TextBox jsonNamespaceBox = new TextBox();
        private readonly TextBox jsonOutputBox = new TextBox();
        private readonly Button jsonConvertBtn = new Button();
        private readonly Button jsonLoadFileBtn = new Button();
        private readonly CheckBox jsonNullableBox = new CheckBox { Text = "可空类型", Checked = false, AutoSize = true };

        // Tab 6: API 代码生成
        private readonly TextBox apiBaseUrlBox = new TextBox();
        private readonly TextBox apiEndpointBox = new TextBox();
        private readonly ComboBox apiMethodBox = new ComboBox();
        private readonly ComboBox apiAuthTypeBox = new ComboBox();
        private readonly TextBox apiTokenBox = new TextBox();
        private readonly TextBox apiContentTypeBox = new TextBox();
        private readonly TextBox apiRequestBodyBox = new TextBox();
        private readonly TextBox apiJsonResponseBox = new TextBox();
        private readonly TextBox apiClassNameBox = new TextBox();
        private readonly TextBox apiNamespaceBox = new TextBox();
        private readonly TextBox apiOutputBox = new TextBox();
        private readonly Button apiGenerateBtn = new Button();
        private readonly CheckBox apiGenRequestBox = new CheckBox { Text = "生成请求", Checked = true, AutoSize = true };
        private readonly CheckBox apiGenResponseBox = new CheckBox { Text = "生成响应 Model", Checked = true, AutoSize = true };
        private readonly CheckBox apiGenServiceBox = new CheckBox { Text = "生成 Service", Checked = true, AutoSize = true };

        // 代码生成 - 功能选项
        private readonly CheckBox optModel = new CheckBox { Text = "Model", Checked = true, AutoSize = true };
        private readonly CheckBox optXmlMap = new CheckBox { Text = "XML Map", Checked = true, AutoSize = true };
        private readonly CheckBox optRepository = new CheckBox { Text = "Repository", Checked = true, AutoSize = true };
        private readonly CheckBox optService = new CheckBox { Text = "Service", Checked = true, AutoSize = true };
        private readonly CheckBox optController = new CheckBox { Text = "Controller", Checked = true, AutoSize = true };
        private readonly CheckBox optDemo = new CheckBox { Text = "Demo(示例)", Checked = true, AutoSize = true };
        private readonly CheckBox optInterface = new CheckBox { Text = "接口", Checked = true, AutoSize = true };
        private readonly CheckBox optCache = new CheckBox { Text = "缓存", Checked = true, AutoSize = true };
        private readonly CheckBox optQueue = new CheckBox { Text = "消息队列", Checked = false, AutoSize = true };
        private readonly CheckBox optPagination = new CheckBox { Text = "分页", Checked = true, AutoSize = true };
        private readonly CheckBox optTransaction = new CheckBox { Text = "事务", Checked = true, AutoSize = true };
        private readonly CheckBox optSharding = new CheckBox { Text = "分表", Checked = false, AutoSize = true };
        private readonly CheckBox optSync = new CheckBox { Text = "数据同步", Checked = false, AutoSize = true };
        private readonly CheckBox optRawSql = new CheckBox { Text = "原生SQL", Checked = true, AutoSize = true };
        private readonly CheckBox optMapSql = new CheckBox { Text = "XML Map SQL", Checked = true, AutoSize = true };
        private readonly CheckBox optReadme = new CheckBox { Text = "README", Checked = true, AutoSize = true };

        private readonly Button codePreviewBtn = new Button();
        private readonly Button codeGenerateBtn = new Button();
        private readonly ComboBox codeTypeBox = new ComboBox();

        private List<DbConnectionConfig> _connections = new List<DbConnectionConfig>();

        public MainForm()
        {
            Text = "FastData 代码生成工具 v2.0";
            Width = 1100;
            Height = 750;
            StartPosition = FormStartPosition.CenterScreen;

            BuildTabConnection();
            BuildTabModel();
            BuildTabXmlMap();
            BuildTabCodeGeneration();
            BuildTabJsonToModel();
            BuildTabApiGenerator();

            tabs.SelectedIndexChanged += (s, e) => RefreshAllTableLists();
            Controls.Add(tabs);

            LoadSavedConnections();
        }

        // =============== Tab 1: 连接管理 ===============
        private void BuildTabConnection()
        {
            var page = new TabPage("连接管理");
            var panel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 6, Padding = new Padding(10) };

            panel.Controls.Add(new Label { Text = "已保存的连接:", Dock = DockStyle.Fill, Font = new System.Drawing.Font("Microsoft YaHei", 10, System.Drawing.FontStyle.Bold) }, 0, 0);
            panel.SetColumnSpan(panel.GetControlFromPosition(0, 0), 2);

            connListBox.Dock = DockStyle.Fill;
            connListBox.SelectedIndexChanged += ConnListBox_SelectedIndexChanged;
            panel.Controls.Add(connListBox, 0, 1);
            panel.SetRowSpan(panel.GetControlFromPosition(0, 1), 2);

            var editPanel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 4, Padding = new Padding(5) };
            editPanel.Controls.Add(new Label { Text = "名称:" }, 0, 0);
            connNameBox.Dock = DockStyle.Fill;
            editPanel.Controls.Add(connNameBox, 1, 0);

            editPanel.Controls.Add(new Label { Text = "Provider:" }, 0, 1);
            connProviderBox.DropDownStyle = ComboBoxStyle.DropDownList;
            connProviderBox.Items.AddRange(DatabaseProviderMappings.AllProviderNames);
            connProviderBox.SelectedIndex = 0;
            editPanel.Controls.Add(connProviderBox, 1, 1);

            editPanel.Controls.Add(new Label { Text = "连接字符串:" }, 0, 2);
            connStringBox.Dock = DockStyle.Fill;
            connStringBox.Multiline = true;
            connStringBox.Height = 80;
            editPanel.Controls.Add(connStringBox, 1, 2);

            var btnPanel = new FlowLayoutPanel { Dock = DockStyle.Fill };
            connSaveBtn.Text = "保存连接";
            connSaveBtn.Click += ConnSaveBtn_Click;
            btnPanel.Controls.Add(connSaveBtn);

            connDeleteBtn.Text = "删除连接";
            connDeleteBtn.Click += ConnDeleteBtn_Click;
            btnPanel.Controls.Add(connDeleteBtn);

            connTestBtn.Text = "测试连接";
            connTestBtn.Click += ConnTestBtn_Click;
            btnPanel.Controls.Add(connTestBtn);

            connLoadBtn.Text = "加载到其他页面";
            connLoadBtn.Click += ConnLoadBtn_Click;
            btnPanel.Controls.Add(connLoadBtn);

            editPanel.Controls.Add(btnPanel, 0, 3);
            editPanel.SetColumnSpan(btnPanel, 2);

            panel.Controls.Add(editPanel, 1, 1);
            panel.SetRowSpan(panel.GetControlFromPosition(1, 1), 2);

            connStatusLabel.Dock = DockStyle.Fill;
            connStatusLabel.ForeColor = System.Drawing.Color.Green;
            panel.Controls.Add(connStatusLabel, 0, 3);
            panel.SetColumnSpan(connStatusLabel, 2);

            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            panel.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
            panel.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));

            page.Controls.Add(panel);
            tabs.TabPages.Add(page);
        }

        private void LoadSavedConnections()
        {
            _connections = ConnectionConfigManager.LoadConnections();
            connListBox.Items.Clear();
            foreach (var c in _connections)
                connListBox.Items.Add($"{c.Name} [{c.Provider}]");
            if (_connections.Count > 0)
                connStatusLabel.Text = $"已加载 {_connections.Count} 个连接";
        }

        private void ConnListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (connListBox.SelectedIndex < 0 || connListBox.SelectedIndex >= _connections.Count) return;
            var c = _connections[connListBox.SelectedIndex];
            connNameBox.Text = c.Name;
            connProviderBox.Text = c.Provider;
            connStringBox.Text = c.ConnectionString;
        }

        private void ConnSaveBtn_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(connNameBox.Text) || string.IsNullOrWhiteSpace(connStringBox.Text))
            {
                MessageBox.Show("名称和连接字符串不能为空");
                return;
            }
            ConnectionConfigManager.AddConnection(new DbConnectionConfig
            {
                Name = connNameBox.Text.Trim(),
                Provider = connProviderBox.Text,
                ConnectionString = connStringBox.Text.Trim()
            });
            LoadSavedConnections();
            connStatusLabel.Text = $"连接 \"{connNameBox.Text}\" 已保存";
        }

        private void ConnDeleteBtn_Click(object sender, EventArgs e)
        {
            if (connListBox.SelectedIndex < 0) return;
            var name = _connections[connListBox.SelectedIndex].Name;
            if (MessageBox.Show($"确认删除 \"{name}\"?", "确认", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                ConnectionConfigManager.DeleteConnection(name);
                LoadSavedConnections();
            }
        }

        private void ConnTestBtn_Click(object sender, EventArgs e)
        {
            Cursor = Cursors.WaitCursor;
            try
            {
                MetadataReaderFactory.Create(new DatabaseConnectionOptions
                {
                    Provider = connProviderBox.Text,
                    ConnectionString = connStringBox.Text
                }).TestConnection();
                connStatusLabel.Text = "连接测试成功!";
            }
            catch (Exception ex)
            {
                connStatusLabel.Text = "连接测试失败: " + ex.Message;
            }
            finally { Cursor = Cursors.Default; }
        }

        private void ConnLoadBtn_Click(object sender, EventArgs e)
        {
            if (connListBox.SelectedIndex < 0) return;
            var c = _connections[connListBox.SelectedIndex];
            dbProviderBox.Text = c.Provider;
            dbConnStrBox.Text = c.ConnectionString;
            tabs.SelectedIndex = 1;
        }

        // =============== 共享数据库连接区域 ===============
        private Panel BuildSharedDbPanel()
        {
            var p = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 7, RowCount = 1, Height = 32 };
            p.Controls.Add(new Label { Text = "Provider:", TextAlign = System.Drawing.ContentAlignment.MiddleRight, Dock = DockStyle.Fill }, 0, 0);
            dbProviderBox.DropDownStyle = ComboBoxStyle.DropDownList;
            dbProviderBox.Items.AddRange(DatabaseProviderMappings.AllProviderNames);
            dbProviderBox.SelectedIndex = 0;
            p.Controls.Add(dbProviderBox, 1, 0);

            p.Controls.Add(new Label { Text = "连接:", TextAlign = System.Drawing.ContentAlignment.MiddleRight, Dock = DockStyle.Fill }, 2, 0);
            dbConnStrBox.Dock = DockStyle.Fill;
            p.Controls.Add(dbConnStrBox, 3, 0);

            dbTestBtn.Text = "测试";
            dbTestBtn.Click += (s, e) => TestDbConnection();
            p.Controls.Add(dbTestBtn, 4, 0);

            dbLoadTablesBtn.Text = "加载表";
            dbLoadTablesBtn.Click += (s, e) => LoadDbTables();
            p.Controls.Add(dbLoadTablesBtn, 5, 0);

            dbManageBtn.Text = "连接管理";
            dbManageBtn.Click += (s, e) => { new ConnectionManagerForm().ShowDialog(this); LoadSavedConnections(); };
            p.Controls.Add(dbManageBtn, 6, 0);

            p.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 60));
            p.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
            p.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 50));
            p.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            p.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 60));
            p.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80));
            p.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100));
            return p;
        }

        private void TestDbConnection()
        {
            Cursor = Cursors.WaitCursor;
            try
            {
                MetadataReaderFactory.Create(new DatabaseConnectionOptions
                {
                    Provider = dbProviderBox.Text,
                    ConnectionString = dbConnStrBox.Text
                }).TestConnection();
                connStatusLabel.Text = "连接成功";
            }
            catch (Exception ex) { connStatusLabel.Text = "失败: " + ex.Message; }
            finally { Cursor = Cursors.Default; }
        }

        private void LoadDbTables()
        {
            Cursor = Cursors.WaitCursor;
            try
            {
                _tables = MetadataReaderFactory.Create(new DatabaseConnectionOptions
                {
                    Provider = dbProviderBox.Text,
                    ConnectionString = dbConnStrBox.Text
                }).GetTables();
                RefreshAllTableLists();
                connStatusLabel.Text = $"已加载 {_tables.Count} 个表/视图";
            }
            catch (Exception ex) { connStatusLabel.Text = "加载失败: " + ex.Message; }
            finally { Cursor = Cursors.Default; }
        }

        private void RefreshAllTableLists()
        {
            var keyword = codeSearchBox.Text ?? "";
            var incViews = includeViewsBox.Checked;
            string[][] lists = { new string[0], new string[0], new string[0] };
            var boxes = new[] { modelTableList, xmlTableList, codeTableList };
            var searchBoxes = new[] { modelSearchBox, xmlSearchBox, codeSearchBox };
            for (int i = 0; i < 3; i++)
            {
                boxes[i].Items.Clear();
                var kw = searchBoxes[i].Text ?? "";
                foreach (var t in _tables)
                {
                    if ((!string.IsNullOrEmpty(kw) && t.FullName.IndexOf(kw, StringComparison.OrdinalIgnoreCase) < 0)) continue;
                    if (!incViews && t.IsView) continue;
                    boxes[i].Items.Add(t.FullName);
                }
            }
        }

        // =============== Tab 2: Model 生成 ===============
        private void BuildTabModel()
        {
            var page = new TabPage("Model 生成");
            var panel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 6, Padding = new Padding(10) };

            panel.Controls.Add(BuildSharedDbPanel(), 0, 0);
            panel.SetColumnSpan(panel.GetControlFromPosition(0, 0), 2);

            panel.Controls.Add(includeViewsBox, 0, 1);
            panel.SetColumnSpan(includeViewsBox, 2);

            var optPanel = new FlowLayoutPanel { Dock = DockStyle.Fill };
            optPanel.Controls.Add(new Label { Text = "命名空间:", TextAlign = System.Drawing.ContentAlignment.MiddleRight, AutoSize = true });
            modelNamespaceBox.Text = "FastData.Generated.Models";
            modelNamespaceBox.Width = 200;
            optPanel.Controls.Add(modelNamespaceBox);
            optPanel.Controls.Add(new Label { Text = "输出:", TextAlign = System.Drawing.ContentAlignment.MiddleRight, AutoSize = true });
            modelOutDirBox.Text = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Output", "Models");
            modelOutDirBox.Width = 250;
            optPanel.Controls.Add(modelOutDirBox);
            panel.Controls.Add(optPanel, 0, 2);
            panel.SetColumnSpan(optPanel, 2);

            panel.Controls.Add(new Label { Text = "选择表:", Dock = DockStyle.Fill }, 0, 3);
            var rightPanelModel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2 };
            rightPanelModel.Controls.Add(new Label { Text = "搜索:", Dock = DockStyle.Bottom }, 0, 0);
            modelSearchBox.Dock = DockStyle.Fill;
            modelSearchBox.TextChanged += (s, e) => RefreshAllTableLists();
            rightPanelModel.Controls.Add(modelSearchBox, 0, 1);
            panel.Controls.Add(rightPanelModel, 1, 3);

            modelTableList.Dock = DockStyle.Fill;
            modelTableList.SelectionMode = SelectionMode.MultiExtended;
            panel.Controls.Add(modelTableList, 0, 4);
            panel.SetRowSpan(panel.GetControlFromPosition(0, 4), 2);

            var modelBtnPanel = new FlowLayoutPanel { Dock = DockStyle.Bottom };
            modelPreviewBtn.Text = "预览";
            modelPreviewBtn.Click += (s, e) => PreviewModel();
            modelBtnPanel.Controls.Add(modelPreviewBtn);
            modelGenerateBtn.Text = "生成";
            modelGenerateBtn.Click += (s, e) => GenerateModel();
            modelBtnPanel.Controls.Add(modelGenerateBtn);
            panel.Controls.Add(modelBtnPanel, 1, 5);

            modelPreviewBox.Dock = DockStyle.Fill;
            modelPreviewBox.Multiline = true;
            modelPreviewBox.ScrollBars = ScrollBars.Both;
            modelPreviewBox.Font = new System.Drawing.Font("Consolas", 10);
            modelPreviewBox.ReadOnly = true;
            panel.Controls.Add(modelPreviewBox, 1, 4);
            panel.SetRowSpan(panel.GetControlFromPosition(1, 4), 1);

            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 35));
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 35));
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));
            panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));

            page.Controls.Add(panel);
            tabs.TabPages.Add(page);
        }

        private void PreviewModel()
        {
            if (modelTableList.SelectedItem == null) return;
            var table = FindTable(modelTableList.SelectedItem.ToString());
            if (table == null) return;
            Cursor = Cursors.WaitCursor;
            try
            {
                var columns = GetColumns(table.FullName);
                modelPreviewBox.Text = new ModelCodeGenerator().Generate(modelNamespaceBox.Text, table, columns);
            }
            catch (Exception ex) { modelPreviewBox.Text = "错误: " + ex.Message; }
            finally { Cursor = Cursors.Default; }
        }

        private void GenerateModel()
        {
            if (modelTableList.SelectedItems.Count == 0) { MessageBox.Show("请选择表"); return; }
            Directory.CreateDirectory(modelOutDirBox.Text);
            var gen = new ModelCodeGenerator();
            var reader = CreateReader();
            int count = 0;
            foreach (var item in modelTableList.SelectedItems)
            {
                var table = FindTable(item.ToString());
                if (table == null) continue;
                var code = gen.Generate(modelNamespaceBox.Text, table, reader.GetColumns(table.FullName));
                File.WriteAllText(Path.Combine(modelOutDirBox.Text, table.Name + ".cs"), code);
                count++;
            }
            MessageBox.Show($"生成完成! {count} 个文件 -> {modelOutDirBox.Text}");
        }

        // =============== Tab 3: XML Map 生成 ===============
        private void BuildTabXmlMap()
        {
            var page = new TabPage("XML Map 生成");
            var panel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 6, Padding = new Padding(10) };

            panel.Controls.Add(new Label { Text = "使用 Tab1 的连接管理设置数据库连接", Dock = DockStyle.Fill, ForeColor = System.Drawing.Color.Gray }, 0, 0);
            panel.SetColumnSpan(panel.GetControlFromPosition(0, 0), 2);

            var optPanel = new FlowLayoutPanel { Dock = DockStyle.Fill };
            optPanel.Controls.Add(new Label { Text = "命名空间:", TextAlign = System.Drawing.ContentAlignment.MiddleRight, AutoSize = true });
            xmlNamespaceBox.Text = "FastData.Generated";
            xmlNamespaceBox.Width = 200;
            optPanel.Controls.Add(xmlNamespaceBox);
            optPanel.Controls.Add(new Label { Text = "输出:", TextAlign = System.Drawing.ContentAlignment.MiddleRight, AutoSize = true });
            xmlOutDirBox.Text = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Output", "XmlMaps");
            xmlOutDirBox.Width = 250;
            optPanel.Controls.Add(xmlOutDirBox);
            panel.Controls.Add(optPanel, 0, 1);
            panel.SetColumnSpan(optPanel, 2);

            panel.Controls.Add(new Label { Text = "选择表:", Dock = DockStyle.Fill }, 0, 2);
            xmlSearchBox.Dock = DockStyle.Fill;
            xmlSearchBox.TextChanged += (s, e) => RefreshAllTableLists();
            panel.Controls.Add(xmlSearchBox, 1, 2);

            xmlTableList.Dock = DockStyle.Fill;
            xmlTableList.SelectionMode = SelectionMode.MultiExtended;
            panel.Controls.Add(xmlTableList, 0, 3);
            panel.SetRowSpan(panel.GetControlFromPosition(0, 3), 3);

            var btnPanel = new FlowLayoutPanel { Dock = DockStyle.Bottom };
            xmlPreviewBtn.Text = "预览";
            xmlPreviewBtn.Click += (s, e) => PreviewXml();
            btnPanel.Controls.Add(xmlPreviewBtn);
            xmlGenerateBtn.Text = "生成";
            xmlGenerateBtn.Click += (s, e) => GenerateXml();
            btnPanel.Controls.Add(xmlGenerateBtn);
            panel.Controls.Add(btnPanel, 1, 3);

            xmlPreviewBox.Dock = DockStyle.Fill;
            xmlPreviewBox.Multiline = true;
            xmlPreviewBox.ScrollBars = ScrollBars.Both;
            xmlPreviewBox.Font = new System.Drawing.Font("Consolas", 10);
            xmlPreviewBox.ReadOnly = true;
            panel.Controls.Add(xmlPreviewBox, 1, 4);
            panel.SetRowSpan(panel.GetControlFromPosition(1, 4), 2);

            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 35));
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            panel.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
            panel.RowStyles.Add(new RowStyle(SizeType.Percent, 50));

            page.Controls.Add(panel);
            tabs.TabPages.Add(page);
        }

        private void PreviewXml()
        {
            if (xmlTableList.SelectedItem == null) return;
            var table = FindTable(xmlTableList.SelectedItem.ToString());
            if (table == null) return;
            Cursor = Cursors.WaitCursor;
            try
            {
                var columns = GetColumns(table.FullName);
                xmlPreviewBox.Text = new XmlMapSqlGenerator().Generate(xmlNamespaceBox.Text, table, columns);
            }
            catch (Exception ex) { xmlPreviewBox.Text = "错误: " + ex.Message; }
            finally { Cursor = Cursors.Default; }
        }

        private void GenerateXml()
        {
            if (xmlTableList.SelectedItems.Count == 0) { MessageBox.Show("请选择表"); return; }
            Directory.CreateDirectory(xmlOutDirBox.Text);
            var gen = new XmlMapSqlGenerator();
            var reader = CreateReader();
            int count = 0;
            foreach (var item in xmlTableList.SelectedItems)
            {
                var table = FindTable(item.ToString());
                if (table == null) continue;
                var xml = gen.Generate(xmlNamespaceBox.Text, table, reader.GetColumns(table.FullName));
                File.WriteAllText(Path.Combine(xmlOutDirBox.Text, table.Name + ".xml"), xml);
                count++;
            }
            MessageBox.Show($"生成完成! {count} 个文件 -> {xmlOutDirBox.Text}");
        }

        // =============== Tab 4: 代码生成 ===============
        private void BuildTabCodeGeneration()
        {
            var page = new TabPage("代码生成");
            var panel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 5, Padding = new Padding(10) };

            panel.Controls.Add(BuildSharedDbPanel(), 0, 0);
            panel.SetColumnSpan(panel.GetControlFromPosition(0, 0), 2);

            // 功能选项
            var featPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = true, AutoSize = true };
            featPanel.Controls.Add(new Label { Text = "文件:", Font = new System.Drawing.Font("Microsoft YaHei", 9, System.Drawing.FontStyle.Bold), AutoSize = true });
            featPanel.Controls.Add(optModel);
            featPanel.Controls.Add(optXmlMap);
            featPanel.Controls.Add(optRepository);
            featPanel.Controls.Add(optService);
            featPanel.Controls.Add(optController);
            featPanel.Controls.Add(optDemo);
            featPanel.Controls.Add(optInterface);
            featPanel.Controls.Add(optReadme);
            featPanel.Controls.Add(new Label { Text = "  |  ", AutoSize = true });
            featPanel.Controls.Add(new Label { Text = "功能:", Font = new System.Drawing.Font("Microsoft YaHei", 9, System.Drawing.FontStyle.Bold), AutoSize = true });
            featPanel.Controls.Add(optCache);
            featPanel.Controls.Add(optQueue);
            featPanel.Controls.Add(optPagination);
            featPanel.Controls.Add(optTransaction);
            featPanel.Controls.Add(optSharding);
            featPanel.Controls.Add(optSync);
            featPanel.Controls.Add(optRawSql);
            featPanel.Controls.Add(optMapSql);
            panel.Controls.Add(featPanel, 0, 1);
            panel.SetColumnSpan(featPanel, 2);

            // 命名空间和输出
            var optRow = new FlowLayoutPanel { Dock = DockStyle.Fill };
            optRow.Controls.Add(new Label { Text = "命名空间:", TextAlign = System.Drawing.ContentAlignment.MiddleRight, AutoSize = true });
            codeNamespaceBox.Text = "FastData.Generated";
            codeNamespaceBox.Width = 200;
            optRow.Controls.Add(codeNamespaceBox);
            optRow.Controls.Add(new Label { Text = "输出:", TextAlign = System.Drawing.ContentAlignment.MiddleRight, AutoSize = true });
            codeOutDirBox.Text = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Output", "CodeGen");
            codeOutDirBox.Width = 250;
            optRow.Controls.Add(codeOutDirBox);
            optRow.Controls.Add(includeViewsBox);
            panel.Controls.Add(optRow, 0, 2);
            panel.SetColumnSpan(optRow, 2);

            // 表选择 + 预览
            var leftPanel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2 };
            codeSearchBox.Dock = DockStyle.Fill;
            codeSearchBox.TextChanged += (s, e) => RefreshAllTableLists();
            leftPanel.Controls.Add(codeSearchBox, 0, 0);
            codeTableList.Dock = DockStyle.Fill;
            codeTableList.SelectionMode = SelectionMode.MultiExtended;
            codeTableList.SelectedIndexChanged += (s, e) => AutoPreviewCode();
            leftPanel.Controls.Add(codeTableList, 0, 1);
            panel.Controls.Add(leftPanel, 0, 3);
            panel.SetRowSpan(panel.GetControlFromPosition(0, 3), 2);

            codePreviewBox.Dock = DockStyle.Fill;
            codePreviewBox.Multiline = true;
            codePreviewBox.ScrollBars = ScrollBars.Both;
            codePreviewBox.Font = new System.Drawing.Font("Consolas", 9);
            codePreviewBox.ReadOnly = true;
            panel.Controls.Add(codePreviewBox, 1, 3);

            var actPanel = new FlowLayoutPanel { Dock = DockStyle.Bottom };
            actPanel.Controls.Add(new Label { Text = "预览类型:", TextAlign = System.Drawing.ContentAlignment.MiddleRight, AutoSize = true });
            codeTypeBox.DropDownStyle = ComboBoxStyle.DropDownList;
            codeTypeBox.Items.AddRange(new[] { "Repository", "Service", "Controller", "Demo", "Model", "XML Map" });
            codeTypeBox.SelectedIndex = 0;
            codeTypeBox.SelectedIndexChanged += (s, e) => AutoPreviewCode();
            actPanel.Controls.Add(codeTypeBox);
            codePreviewBtn.Text = "预览";
            codePreviewBtn.Click += (s, e) => AutoPreviewCode();
            actPanel.Controls.Add(codePreviewBtn);
            codeGenerateBtn.Text = "生成并导出";
            codeGenerateBtn.Font = new System.Drawing.Font("Microsoft YaHei", 10, System.Drawing.FontStyle.Bold);
            codeGenerateBtn.Click += (s, e) => GenerateCode();
            actPanel.Controls.Add(codeGenerateBtn);
            panel.Controls.Add(actPanel, 1, 4);

            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 35));
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 60));
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 35));
            panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));

            page.Controls.Add(panel);
            tabs.TabPages.Add(page);
        }

        private void AutoPreviewCode()
        {
            if (codeTableList.SelectedItem == null) return;
            Cursor = Cursors.WaitCursor;
            try
            {
                var table = FindTable(codeTableList.SelectedItem.ToString());
                if (table == null) return;
                var columns = GetColumns(table.FullName);
                _generator.Options = GetGeneratorOptions();
                codePreviewBox.Text = codeTypeBox.SelectedIndex switch
                {
                    0 => _generator.Options.GenerateRepository ? _generator.GenerateRepository(codeNamespaceBox.Text, table, columns) : "未勾选 Repository",
                    1 => _generator.Options.GenerateService ? _generator.GenerateService(codeNamespaceBox.Text, table) : "未勾选 Service",
                    2 => _generator.Options.GenerateController ? _generator.GenerateController(codeNamespaceBox.Text, table) : "未勾选 Controller",
                    3 => _generator.Options.GenerateDemo ? _generator.GenerateDemo(codeNamespaceBox.Text, table, columns) : "未勾选 Demo",
                    4 => _generator.Options.GenerateModel ? _generator.GenerateModel(codeNamespaceBox.Text, table, columns) : "未勾选 Model",
                    5 => _generator.Options.GenerateXmlMap ? _generator.GenerateXmlMap(codeNamespaceBox.Text, table, columns) : "未勾选 XML Map",
                    _ => "请选择预览类型"
                };
            }
            catch (Exception ex) { codePreviewBox.Text = "错误: " + ex.Message; }
            finally { Cursor = Cursors.Default; }
        }

        private void GenerateCode()
        {
            if (codeTableList.SelectedItems.Count == 0) { MessageBox.Show("请选择表"); return; }
            var opts = GetGeneratorOptions();
            _generator.Options = opts;
            Directory.CreateDirectory(codeOutDirBox.Text);
            var reader = CreateReader();
            int totalFiles = 0;

            foreach (var item in codeTableList.SelectedItems)
            {
                var table = FindTable(item.ToString());
                if (table == null) continue;
                var columns = reader.GetColumns(table.FullName);
                var result = _generator.GenerateComplete(codeNamespaceBox.Text, table, columns);
                var pascalName = Pascal(table.Name);

                if (opts.GenerateModel && !string.IsNullOrEmpty(result.ModelCode))
                { var d = Path.Combine(codeOutDirBox.Text, "Models"); Directory.CreateDirectory(d);
                  File.WriteAllText(Path.Combine(d, table.Name + ".cs"), result.ModelCode); totalFiles++; }
                if (opts.GenerateXmlMap && !string.IsNullOrEmpty(result.XmlMapCode))
                { var d = Path.Combine(codeOutDirBox.Text, "XmlMaps"); Directory.CreateDirectory(d);
                  File.WriteAllText(Path.Combine(d, table.Name + ".xml"), result.XmlMapCode); totalFiles++; }
                if (opts.GenerateRepository && !string.IsNullOrEmpty(result.RepositoryCode))
                { var d = Path.Combine(codeOutDirBox.Text, "Repositories"); Directory.CreateDirectory(d);
                  File.WriteAllText(Path.Combine(d, pascalName + "Repository.cs"), result.RepositoryCode); totalFiles++; }
                if (opts.GenerateService && !string.IsNullOrEmpty(result.ServiceCode))
                { var d = Path.Combine(codeOutDirBox.Text, "Services"); Directory.CreateDirectory(d);
                  File.WriteAllText(Path.Combine(d, pascalName + "Service.cs"), result.ServiceCode); totalFiles++; }
                if (opts.GenerateController && !string.IsNullOrEmpty(result.ControllerCode))
                { var d = Path.Combine(codeOutDirBox.Text, "Controllers"); Directory.CreateDirectory(d);
                  File.WriteAllText(Path.Combine(d, pascalName + "Controller.cs"), result.ControllerCode); totalFiles++; }
                if (opts.GenerateDemo && !string.IsNullOrEmpty(result.DemoCode))
                { var d = Path.Combine(codeOutDirBox.Text, "Demo"); Directory.CreateDirectory(d);
                  File.WriteAllText(Path.Combine(d, pascalName + "Demo.cs"), result.DemoCode); totalFiles++; }
                if (opts.GenerateReadme && !string.IsNullOrEmpty(result.ReadmeCode))
                { var d = Path.Combine(codeOutDirBox.Text, "Docs"); Directory.CreateDirectory(d);
                  File.WriteAllText(Path.Combine(d, pascalName + "_README.md"), result.ReadmeCode); totalFiles++; }
            }
            MessageBox.Show($"生成完成! 共 {totalFiles} 个文件\n输出目录: {codeOutDirBox.Text}", "成功");
        }

        private GeneratorOptions GetGeneratorOptions()
        {
            return new GeneratorOptions
            {
                GenerateModel = optModel.Checked,
                GenerateXmlMap = optXmlMap.Checked,
                GenerateRepository = optRepository.Checked,
                GenerateService = optService.Checked,
                GenerateController = optController.Checked,
                GenerateDemo = optDemo.Checked,
                GenerateReadme = optReadme.Checked,
                GenerateCache = optCache.Checked,
                GenerateQueue = optQueue.Checked,
                GenerateWithInterface = optInterface.Checked,
                GenerateWithPagination = optPagination.Checked,
                GenerateWithTransaction = optTransaction.Checked,
                GenerateWithSharding = optSharding.Checked,
                GenerateWithSync = optSync.Checked,
                GenerateRawSql = optRawSql.Checked,
                GenerateMapSql = optMapSql.Checked
            };
        }

        // =============== 辅助方法 ===============
        private DatabaseConnectionOptions GetDbOptions()
        {
            return new DatabaseConnectionOptions
            {
                Provider = dbProviderBox.Text,
                ConnectionString = dbConnStrBox.Text
            };
        }

        private IDatabaseMetadataReader CreateReader() => MetadataReaderFactory.Create(GetDbOptions());

        private IList<DatabaseColumn> GetColumns(string fullName) => CreateReader().GetColumns(fullName);

        private DatabaseTable FindTable(string fullName)
        {
            foreach (var t in _tables)
                if (t.FullName == fullName) return t;
            return null;
        }

        private static string Pascal(string value)
        {
            if (string.IsNullOrEmpty(value)) return "Model";
            var sb = new System.Text.StringBuilder();
            var upper = true;
            foreach (var ch in value)
            {
                if (!char.IsLetterOrDigit(ch)) { upper = true; continue; }
                sb.Append(upper ? char.ToUpperInvariant(ch) : ch);
                upper = false;
            }
            return sb.Length == 0 || char.IsDigit(sb[0]) ? "Model" : sb.ToString();
        }

        // =============== Tab 5: JSON 转 Model ===============
        private void BuildTabJsonToModel()
        {
            var page = new TabPage("JSON 转 Model");
            var panel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 6, Padding = new Padding(10) };

            var headerLabel = new Label { Text = "粘贴 JSON，自动生成 C# Model 类", Dock = DockStyle.Fill, Font = new System.Drawing.Font("Microsoft YaHei", 10, System.Drawing.FontStyle.Bold) };
            panel.Controls.Add(headerLabel, 0, 0);
            panel.SetColumnSpan(headerLabel, 2);

            // 配置区域
            var configPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = true };
            configPanel.Controls.Add(new Label { Text = "类名:", TextAlign = System.Drawing.ContentAlignment.MiddleRight, AutoSize = true });
            jsonClassNameBox.Text = "RootObject";
            jsonClassNameBox.Width = 150;
            configPanel.Controls.Add(jsonClassNameBox);
            configPanel.Controls.Add(new Label { Text = "命名空间:", TextAlign = System.Drawing.ContentAlignment.MiddleRight, AutoSize = true });
            jsonNamespaceBox.Text = "FastData.Generated.Models";
            jsonNamespaceBox.Width = 200;
            configPanel.Controls.Add(jsonNamespaceBox);
            configPanel.Controls.Add(jsonNullableBox);
            panel.Controls.Add(configPanel, 0, 1);
            panel.SetColumnSpan(configPanel, 2);

            // JSON 输入
            panel.Controls.Add(new Label { Text = "JSON 输入:", Dock = DockStyle.Fill }, 0, 2);
            var btnPanel = new FlowLayoutPanel { Dock = DockStyle.Fill };
            jsonLoadFileBtn.Text = "加载 JSON 文件";
            jsonLoadFileBtn.Click += JsonLoadFileBtn_Click;
            btnPanel.Controls.Add(jsonLoadFileBtn);
            panel.Controls.Add(btnPanel, 1, 2);

            jsonInputBox.Dock = DockStyle.Fill;
            jsonInputBox.Multiline = true;
            jsonInputBox.ScrollBars = ScrollBars.Both;
            jsonInputBox.Font = new System.Drawing.Font("Consolas", 10);
            panel.Controls.Add(jsonInputBox, 0, 3);
            panel.SetRowSpan(jsonInputBox, 2);

            // 输出
            panel.Controls.Add(new Label { Text = "C# Model 输出:", Dock = DockStyle.Fill }, 1, 3);
            var actionPanel = new FlowLayoutPanel { Dock = DockStyle.Fill };
            jsonConvertBtn.Text = "转换";
            jsonConvertBtn.Font = new System.Drawing.Font("Microsoft YaHei", 10, System.Drawing.FontStyle.Bold);
            jsonConvertBtn.Click += JsonConvertBtn_Click;
            actionPanel.Controls.Add(jsonConvertBtn);
            panel.Controls.Add(actionPanel, 1, 4);

            jsonOutputBox.Dock = DockStyle.Fill;
            jsonOutputBox.Multiline = true;
            jsonOutputBox.ScrollBars = ScrollBars.Both;
            jsonOutputBox.Font = new System.Drawing.Font("Consolas", 10);
            jsonOutputBox.ReadOnly = true;
            panel.Controls.Add(jsonOutputBox, 1, 5);

            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 35));
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            panel.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            panel.RowStyles.Add(new RowStyle(SizeType.Percent, 50));

            page.Controls.Add(panel);
            tabs.TabPages.Add(page);
        }

        private void JsonLoadFileBtn_Click(object sender, EventArgs e)
        {
            using var dlg = new OpenFileDialog { Filter = "JSON 文件|*.json|所有文件|*.*" };
            if (dlg.ShowDialog() == DialogResult.OK)
                jsonInputBox.Text = File.ReadAllText(dlg.FileName);
        }

        private void JsonConvertBtn_Click(object sender, EventArgs e)
        {
            Cursor = Cursors.WaitCursor;
            try
            {
                var converter = new JsonToModelConverter();
                jsonOutputBox.Text = converter.Convert(jsonInputBox.Text, jsonClassNameBox.Text, jsonNamespaceBox.Text);
            }
            catch (Exception ex)
            {
                jsonOutputBox.Text = "转换失败：" + ex.Message;
            }
            finally
            {
                Cursor = Cursors.Default;
            }
        }

        // =============== Tab 6: API 代码生成 ===============
        private void BuildTabApiGenerator()
        {
            var page = new TabPage("API 代码生成");
            var panel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 7, Padding = new Padding(10) };

            var headerLabel = new Label { Text = "输入 API 信息，生成 HttpClient 调用代码和 Model", Dock = DockStyle.Fill, Font = new System.Drawing.Font("Microsoft YaHei", 10, System.Drawing.FontStyle.Bold) };
            panel.Controls.Add(headerLabel, 0, 0);
            panel.SetColumnSpan(headerLabel, 2);

            // 配置区域
            var configPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = true };
            configPanel.Controls.Add(new Label { Text = "命名空间:", AutoSize = true });
            apiNamespaceBox.Text = "FastData.Generated.ApiClients";
            apiNamespaceBox.Width = 200;
            configPanel.Controls.Add(apiNamespaceBox);
            configPanel.Controls.Add(new Label { Text = "类名:", AutoSize = true });
            apiClassNameBox.Text = "ApiClient";
            apiClassNameBox.Width = 120;
            configPanel.Controls.Add(apiClassNameBox);
            configPanel.Controls.Add(apiGenRequestBox);
            configPanel.Controls.Add(apiGenResponseBox);
            configPanel.Controls.Add(apiGenServiceBox);
            panel.Controls.Add(configPanel, 0, 1);
            panel.SetColumnSpan(configPanel, 2);

            // API 基本信息
            var apiPanel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 2 };
            apiPanel.Controls.Add(new Label { Text = "Base URL:", Dock = DockStyle.Fill }, 0, 0);
            apiBaseUrlBox.Dock = DockStyle.Fill;
            apiBaseUrlBox.Text = "https://api.example.com";
            apiPanel.Controls.Add(apiBaseUrlBox, 1, 0);

            apiPanel.Controls.Add(new Label { Text = "Endpoint:", Dock = DockStyle.Fill }, 2, 0);
            apiEndpointBox.Dock = DockStyle.Fill;
            apiEndpointBox.Text = "/api/v1/users";
            apiPanel.Controls.Add(apiEndpointBox, 3, 0);

            apiPanel.Controls.Add(new Label { Text = "Method:", Dock = DockStyle.Fill }, 0, 1);
            apiMethodBox.DropDownStyle = ComboBoxStyle.DropDownList;
            apiMethodBox.Items.AddRange(new[] { "GET", "POST", "PUT", "DELETE", "PATCH" });
            apiMethodBox.SelectedIndex = 0;
            apiMethodBox.Dock = DockStyle.Fill;
            apiPanel.Controls.Add(apiMethodBox, 1, 1);

            apiPanel.Controls.Add(new Label { Text = "Content-Type:", Dock = DockStyle.Fill }, 2, 1);
            apiContentTypeBox.Dock = DockStyle.Fill;
            apiContentTypeBox.Text = "application/json";
            apiPanel.Controls.Add(apiContentTypeBox, 3, 1);
            panel.Controls.Add(apiPanel, 0, 2);
            panel.SetColumnSpan(apiPanel, 2);

            // 认证
            var authPanel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1 };
            authPanel.Controls.Add(new Label { Text = "认证:", Dock = DockStyle.Fill }, 0, 0);
            apiAuthTypeBox.DropDownStyle = ComboBoxStyle.DropDownList;
            apiAuthTypeBox.Items.AddRange(new[] { "None", "Bearer", "JWT", "API Key (Header)", "Token (Header)", "Basic Auth" });
            apiAuthTypeBox.SelectedIndex = 0;
            apiAuthTypeBox.Dock = DockStyle.Fill;
            apiAuthTypeBox.SelectedIndexChanged += (s, e) => apiTokenBox.Enabled = apiAuthTypeBox.SelectedIndex > 0;
            authPanel.Controls.Add(apiAuthTypeBox, 1, 0);
            apiTokenBox.Dock = DockStyle.Fill;
            apiTokenBox.Enabled = false;
            apiTokenBox.Text = "输入 Token 或 username:password";
            authPanel.Controls.Add(apiTokenBox, 2, 0);
            panel.Controls.Add(authPanel, 0, 3);
            panel.SetColumnSpan(authPanel, 2);

            // 请求体
            panel.Controls.Add(new Label { Text = "请求体 (POST/PUT):", Dock = DockStyle.Fill }, 0, 4);
            apiRequestBodyBox.Dock = DockStyle.Fill;
            apiRequestBodyBox.Multiline = true;
            apiRequestBodyBox.ScrollBars = ScrollBars.Both;
            apiRequestBodyBox.Font = new System.Drawing.Font("Consolas", 9);
            apiRequestBodyBox.Height = 60;
            panel.Controls.Add(apiRequestBodyBox, 1, 4);

            // 响应 JSON
            panel.Controls.Add(new Label { Text = "响应 JSON (用于生成 Model):", Dock = DockStyle.Fill }, 0, 5);
            apiJsonResponseBox.Dock = DockStyle.Fill;
            apiJsonResponseBox.Multiline = true;
            apiJsonResponseBox.ScrollBars = ScrollBars.Both;
            apiJsonResponseBox.Font = new System.Drawing.Font("Consolas", 9);
            apiJsonResponseBox.Height = 60;
            panel.Controls.Add(apiJsonResponseBox, 1, 5);

            // 输出
            panel.Controls.Add(new Label { Text = "生成的代码:", Dock = DockStyle.Fill }, 0, 6);
            var genPanel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 2 };
            apiGenerateBtn.Text = "生成";
            apiGenerateBtn.Font = new System.Drawing.Font("Microsoft YaHei", 10, System.Drawing.FontStyle.Bold);
            apiGenerateBtn.Click += ApiGenerateBtn_Click;
            genPanel.Controls.Add(apiGenerateBtn, 0, 0);
            apiOutputBox.Dock = DockStyle.Fill;
            apiOutputBox.Multiline = true;
            apiOutputBox.ScrollBars = ScrollBars.Both;
            apiOutputBox.Font = new System.Drawing.Font("Consolas", 9);
            apiOutputBox.ReadOnly = true;
            genPanel.SetColumnSpan(apiOutputBox, 2);
            genPanel.SetRowSpan(apiOutputBox, 2);
            genPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 35));
            genPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            panel.Controls.Add(genPanel, 1, 6);

            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 60));
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 35));
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 60));
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 60));
            panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            page.Controls.Add(panel);
            tabs.TabPages.Add(page);
        }

        private void ApiGenerateBtn_Click(object sender, EventArgs e)
        {
            Cursor = Cursors.WaitCursor;
            try
            {
                var generator = new ApiClientGenerator
                {
                    Config = new ApiClientConfig
                    {
                        AuthType = apiAuthTypeBox.SelectedIndex switch
                        {
                            1 => "Bearer",
                            2 => "JWT",
                            3 => "ApiKeyHeader",
                            4 => "CustomHeaderToken",
                            5 => "BasicAuth",
                            _ => "None"
                        },
                        AuthToken = apiAuthTypeBox.SelectedIndex > 0 ? apiTokenBox.Text : "",
                        Namespace = apiNamespaceBox.Text,
                        GenerateRequest = apiGenRequestBox.Checked,
                        GenerateResponse = apiGenResponseBox.Checked,
                        GenerateService = apiGenServiceBox.Checked
                    }
                };

                var result = generator.Generate(
                    baseUrl: apiBaseUrlBox.Text,
                    endpoint: apiEndpointBox.Text,
                    method: apiMethodBox.Text,
                    contentType: apiContentTypeBox.Text,
                    requestBody: apiRequestBodyBox.Text,
                    jsonResponse: apiJsonResponseBox.Text,
                    className: apiClassNameBox.Text);

                var output = new StringBuilder();
                if (!string.IsNullOrEmpty(result.RequestCode))
                {
                    output.AppendLine("// ============== API 客户端 ==============");
                    output.AppendLine(result.RequestCode);
                    output.AppendLine();
                }
                if (!string.IsNullOrEmpty(result.ResponseCode))
                {
                    output.AppendLine("// ============== 响应 Model ==============");
                    output.AppendLine(result.ResponseCode);
                    output.AppendLine();
                }
                if (!string.IsNullOrEmpty(result.ServiceCode))
                {
                    output.AppendLine("// ============== Service ==============");
                    output.AppendLine(result.ServiceCode);
                }

                apiOutputBox.Text = output.ToString();
            }
            catch (Exception ex)
            {
                apiOutputBox.Text = "生成失败：" + ex.Message;
            }
            finally
            {
                Cursor = Cursors.Default;
            }
        }
    }
}