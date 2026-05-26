using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace OP2MissionScanner
{
    public partial class fMain : Form
    {
        public fMain()
        {
            InitializeComponent();
            this.Icon = Properties.Resources.OPU;
        }

        private void fMain_Load(object sender, EventArgs e)
        {
        }

        private async void btnScan_Click(object sender, EventArgs e)
        {
            string folder = txtPath.Text.Trim();
            if (!Directory.Exists(folder))
            {
                MessageBox.Show(this, "Folder not found:\r\n" + folder, "Mission DLL Scanner",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            btnScan.Enabled = false;
            grid.Rows.Clear();
            lblStatus.Text = "Scanning " + folder;
            Cursor = Cursors.WaitCursor;
            try
            {
                List<MissionDll> missions = await Task.Run(() => ScanFolder(folder));

                if (missions.Count == 0)
                {
                    lblStatus.Text = "No DLL's found!";
                    return;
                }

                foreach (var m in missions)
                {
                    grid.Rows.Add(m.DllName, m.MapName, m.TechtreeName, m.LevelDesc);
                }

                lblStatus.Text = "Scan finished.";
            }
            finally
            {
                Cursor = Cursors.Default;
                btnScan.Enabled = true;
            }
        }

        private static List<MissionDll> ScanFolder(string folder)
        {
            return Directory.EnumerateFiles(folder, "*.dll", SearchOption.TopDirectoryOnly)
                .Select(MissionDll.TryLoad)
                .Where(m => m != null)
                .OrderBy(m => m.DllName, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private void statusStrip_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {

        }
    }
}
