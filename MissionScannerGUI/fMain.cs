using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MissionScannerGUI
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

        // Backend exe is assumed to sit next to our own exe.
        private static string BackendPath()
        {
            string here = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? string.Empty;
            return Path.Combine(here, "MissionScanner.exe");
        }

        private void btnBrowseFolder_Click(object sender, EventArgs e)
        {
            using (var dlg = new FolderBrowserDialog())
            {
                dlg.Description = "Select folder containing Outpost 2 mission DLLs";
                if (Directory.Exists(txtFolder.Text)) dlg.SelectedPath = txtFolder.Text;
                if (dlg.ShowDialog(this) == DialogResult.OK) txtFolder.Text = dlg.SelectedPath;
            }
        }

        private async void btnScan_Click(object sender, EventArgs e)
        {
            string backend = BackendPath();
            string folder = txtFolder.Text.Trim();

            btnScan.Enabled = false;
            grid.Rows.Clear();
            lblStatus.Text = "Scanning " + folder + " ...";
            Cursor = Cursors.WaitCursor;
            try
            {
                Backend.Result result = await Task.Run(() => Backend.Run(backend, folder));

                if (!string.IsNullOrEmpty(result.Error))
                {
                    lblStatus.Text = "Error.";
                    MessageBox.Show(this, result.Error, "Mission Scanner GUI",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                foreach (var r in result.Rows)
                {
                    grid.Rows.Add(r.DllName, r.Type, r.Players, r.UnitOnly, r.MapName, r.TechtreeName, r.Description);
                }

                if (result.Rows.Count == 0)
                {
                    lblStatus.Text = "No mission DLLs found.";
                }
                else
                {
                    lblStatus.Text = "Scan finished. " + result.Rows.Count + " mission DLLs.";
                }
            }
            finally
            {
                Cursor = Cursors.Default;
                btnScan.Enabled = true;
            }
        }

        private void statusStrip_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {

        }
    }
}
