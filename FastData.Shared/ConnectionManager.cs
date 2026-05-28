using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
#if NETFRAMEWORK
using System.Web.Script.Serialization;
#else
using System.Text.Json;
#endif

namespace FastData.Shared
{
    public class DbConnectionConfig
    {
        public string Name { get; set; }
        public string Provider { get; set; }
        public string ConnectionString { get; set; }
        public DateTime CreatedTime { get; set; }
        public DateTime LastTestTime { get; set; }
    }

    public static class ConnectionConfigManager
    {
        private static readonly string ConfigFileName = "db_connections.json";

        public static string GetConfigPath()
        {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ConfigFileName);
        }

        public static List<DbConnectionConfig> LoadConnections()
        {
            var configPath = GetConfigPath();
            if (!File.Exists(configPath))
                return new List<DbConnectionConfig>();

            try
            {
                var content = File.ReadAllText(configPath);
#if NETFRAMEWORK
                var serializer = new JavaScriptSerializer();
                return serializer.Deserialize<List<DbConnectionConfig>>(content) ?? new List<DbConnectionConfig>();
#else
                return JsonSerializer.Deserialize<List<DbConnectionConfig>>(content) ?? new List<DbConnectionConfig>();
#endif
            }
            catch
            {
                return new List<DbConnectionConfig>();
            }
        }

        public static void SaveConnections(List<DbConnectionConfig> configs)
        {
            var configPath = GetConfigPath();
#if NETFRAMEWORK
            var serializer = new JavaScriptSerializer();
            var content = serializer.Serialize(configs);
#else
            var content = JsonSerializer.Serialize(configs, new JsonSerializerOptions { WriteIndented = true });
#endif
            File.WriteAllText(configPath, content);
        }

        public static void AddConnection(DbConnectionConfig config)
        {
            var configs = LoadConnections();
            var existing = configs.FirstOrDefault(c => c.Name == config.Name);
            if (existing != null)
            {
                existing.Provider = config.Provider;
                existing.ConnectionString = config.ConnectionString;
                existing.LastTestTime = DateTime.Now;
            }
            else
            {
                config.CreatedTime = DateTime.Now;
                config.LastTestTime = DateTime.Now;
                configs.Add(config);
            }
            SaveConnections(configs);
        }

        public static void DeleteConnection(string name)
        {
            var configs = LoadConnections();
            var config = configs.FirstOrDefault(c => c.Name == name);
            if (config != null)
            {
                configs.Remove(config);
                SaveConnections(configs);
            }
        }

        public static DbConnectionConfig GetConnection(string name)
        {
            var configs = LoadConnections();
            return configs.FirstOrDefault(c => c.Name == name);
        }
    }

    public class ConnectionManagerForm : Form
    {
        private readonly DataGridView connectionGrid = new DataGridView();
        private readonly TextBox nameBox = new TextBox();
        private readonly ComboBox providerBox = new ComboBox();
        private readonly TextBox connectionStringBox = new TextBox();
        private readonly Button saveButton = new Button();
        private readonly Button deleteButton = new Button();
        private readonly Button testButton = new Button();
        private readonly Button selectButton = new Button();
        private readonly TextBox logBox = new TextBox();

        public DbConnectionConfig SelectedConnection { get; private set; }
        public bool ConnectionSelected { get; private set; }

        public ConnectionManagerForm()
        {
            Text = "数据库连接管理";
            Width = 800;
            Height = 600;
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            BuildLayout();
            LoadConnections();
        }

        private void BuildLayout()
        {
            var mainPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 6,
                Padding = new Padding(10)
            };

            // 连接列表
            mainPanel.Controls.Add(new Label { Text = "已保存连接:", Dock = DockStyle.Fill }, 0, 0);
            connectionGrid.Dock = DockStyle.Fill;
            connectionGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            connectionGrid.MultiSelect = false;
            connectionGrid.ReadOnly = true;
            connectionGrid.AllowUserToAddRows = false;
            connectionGrid.Columns.Add("Name", "名称");
            connectionGrid.Columns.Add("Provider", "Provider");
            connectionGrid.Columns.Add("ConnectionString", "连接字符串");
            connectionGrid.Columns.Add("LastTestTime", "最后测试");
            connectionGrid.Columns["ConnectionString"].Width = 300;
            connectionGrid.SelectionChanged += ConnectionGrid_SelectionChanged;
            mainPanel.Controls.Add(connectionGrid, 0, 1);
            mainPanel.SetColumnSpan(connectionGrid, 2);

            // 编辑区域
            var editPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 3
            };

            editPanel.Controls.Add(new Label { Text = "连接名称:", Dock = DockStyle.Fill }, 0, 0);
            nameBox.Dock = DockStyle.Fill;
            editPanel.Controls.Add(nameBox, 1, 0);

            editPanel.Controls.Add(new Label { Text = "Provider:", Dock = DockStyle.Fill }, 0, 1);
            providerBox.DropDownStyle = ComboBoxStyle.DropDownList;
            providerBox.Items.AddRange(new[] { "System.Data.SqlClient", "MySql.Data.MySqlClient", "System.Data.SQLite", "Npgsql" });
            providerBox.SelectedIndex = 0;
            editPanel.Controls.Add(providerBox, 1, 1);

            editPanel.Controls.Add(new Label { Text = "连接字符串:", Dock = DockStyle.Fill }, 0, 2);
            connectionStringBox.Dock = DockStyle.Fill;
            connectionStringBox.Multiline = true;
            connectionStringBox.Height = 60;
            editPanel.Controls.Add(connectionStringBox, 1, 2);

            mainPanel.Controls.Add(editPanel, 0, 2);
            mainPanel.SetColumnSpan(editPanel, 2);

            // 按钮
            var buttonPanel = new FlowLayoutPanel { Dock = DockStyle.Fill };
            saveButton.Text = "保存";
            saveButton.Click += SaveButton_Click;
            buttonPanel.Controls.Add(saveButton);

            deleteButton.Text = "删除";
            deleteButton.Click += DeleteButton_Click;
            buttonPanel.Controls.Add(deleteButton);

            testButton.Text = "测试连接";
            testButton.Click += TestButton_Click;
            buttonPanel.Controls.Add(testButton);

            selectButton.Text = "选择此连接";
            selectButton.Click += SelectButton_Click;
            buttonPanel.Controls.Add(selectButton);

            mainPanel.Controls.Add(buttonPanel, 0, 3);
            mainPanel.SetColumnSpan(buttonPanel, 2);

            // 日志
            logBox.Dock = DockStyle.Fill;
            logBox.Multiline = true;
            logBox.ReadOnly = true;
            logBox.ScrollBars = ScrollBars.Vertical;
            mainPanel.Controls.Add(logBox, 0, 4);
            mainPanel.SetColumnSpan(logBox, 2);

            Controls.Add(mainPanel);
        }

        private void ConnectionGrid_SelectionChanged(object sender, EventArgs e)
        {
            if (connectionGrid.SelectedRows.Count > 0)
            {
                var row = connectionGrid.SelectedRows[0];
                nameBox.Text = row.Cells["Name"].Value?.ToString();
                providerBox.Text = row.Cells["Provider"].Value?.ToString();
                connectionStringBox.Text = row.Cells["ConnectionString"].Value?.ToString();
            }
        }

        private void LoadConnections()
        {
            connectionGrid.Rows.Clear();
            var configs = ConnectionConfigManager.LoadConnections();
            foreach (var config in configs)
            {
                connectionGrid.Rows.Add(
                    config.Name,
                    config.Provider,
                    config.ConnectionString,
                    config.LastTestTime.ToString("yyyy-MM-dd HH:mm:ss")
                );
            }
        }

        private void SaveButton_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(nameBox.Text))
            {
                MessageBox.Show("请输入连接名称");
                return;
            }

            if (string.IsNullOrWhiteSpace(connectionStringBox.Text))
            {
                MessageBox.Show("请输入连接字符串");
                return;
            }

            var config = new DbConnectionConfig
            {
                Name = nameBox.Text.Trim(),
                Provider = providerBox.Text,
                ConnectionString = connectionStringBox.Text
            };

            ConnectionConfigManager.AddConnection(config);
            LoadConnections();
            Log($"连接 \"{config.Name}\" 已保存");
        }

        private void DeleteButton_Click(object sender, EventArgs e)
        {
            if (connectionGrid.SelectedRows.Count == 0)
            {
                MessageBox.Show("请先选择要删除的连接");
                return;
            }

            var name = connectionGrid.SelectedRows[0].Cells["Name"].Value?.ToString();
            if (string.IsNullOrEmpty(name))
                return;

            if (MessageBox.Show($"确定要删除连接 \"{name}\" 吗？", "确认", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                ConnectionConfigManager.DeleteConnection(name);
                LoadConnections();
                Log($"连接 \"{name}\" 已删除");
            }
        }

        private void TestButton_Click(object sender, EventArgs e)
        {
            // 测试连接逻辑
            Log("测试连接功能待实现");
        }

        private void SelectButton_Click(object sender, EventArgs e)
        {
            if (connectionGrid.SelectedRows.Count == 0)
            {
                MessageBox.Show("请先选择一个连接");
                return;
            }

            var name = connectionGrid.SelectedRows[0].Cells["Name"].Value?.ToString();
            SelectedConnection = ConnectionConfigManager.GetConnection(name);
            ConnectionSelected = true;
            DialogResult = DialogResult.OK;
            Close();
        }

        private void Log(string message)
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss");
            logBox.AppendText($"[{timestamp}] {message}{Environment.NewLine}");
        }
    }
}
