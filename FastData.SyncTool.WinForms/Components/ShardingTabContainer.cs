using System.Drawing;
using System.Windows.Forms;
using FastData.SyncTool.WinForms.Services;

namespace FastData.SyncTool.WinForms.Components
{
    /// <summary>
    /// 分表同步 Tab 容器 UserControl
    /// 内嵌子 TabControl，包含：分表配置、数据同步、数据导入、数据操作
    /// </summary>
    public partial class ShardingTabContainer : UserControl
    {
        private readonly LogService _logService;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="logService">日志服务实例</param>
        public ShardingTabContainer(LogService logService)
        {
            _logService = logService ?? new LogService();
            InitializeComponent();
        }

        /// <summary>
        /// 初始化 UI 控件
        /// </summary>
        private void InitializeComponent()
        {
            BackColor = Color.White;
            Dock = DockStyle.Fill;

            var shardingTaskService = new ShardingTaskService(_logService);

            var subTabControl = new TabControl
            {
                Dock = DockStyle.Fill,
                DrawMode = TabDrawMode.Normal,
                Font = new Font("Microsoft YaHei UI", 9F)
            };

            // 分表配置
            var configTab = new TabPage("分表配置");
            configTab.BackColor = Color.White;
            configTab.Controls.Add(new ShardingConfigVisualizer { Dock = DockStyle.Fill });
            subTabControl.TabPages.Add(configTab);

            // 数据同步
            var syncTab = new TabPage("数据同步");
            syncTab.BackColor = Color.White;
            syncTab.Controls.Add(new ShardingSyncControl(shardingTaskService, _logService) { Dock = DockStyle.Fill });
            subTabControl.TabPages.Add(syncTab);

            // 数据导入
            var importTab = new TabPage("数据导入");
            importTab.BackColor = Color.White;
            importTab.Controls.Add(new ShardingImportControl { Dock = DockStyle.Fill });
            subTabControl.TabPages.Add(importTab);

            // 数据操作
            var dataTab = new TabPage("数据操作");
            dataTab.BackColor = Color.White;
            dataTab.Controls.Add(new ShardingDataControl { Dock = DockStyle.Fill });
            subTabControl.TabPages.Add(dataTab);

            Controls.Add(subTabControl);
        }
    }
}