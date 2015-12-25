using System.Windows.Forms;

namespace Launcher
{
    public partial class LauncherWindow : Form
    {
        public LauncherWindow()
        {
            InitializeComponent();

            FormClosed += OnClose;
        }

        private void OnClose(object sender, FormClosedEventArgs args)
        {
            Application.Exit();
        }
    }
}
