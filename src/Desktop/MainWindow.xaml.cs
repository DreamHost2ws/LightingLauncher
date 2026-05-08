using System.Windows;
using AgLauncher.Core;

namespace AgLauncher.Desktop
{
    public partial class MainWindow : Window
    {
        private LauncherCore _launcherCore;

        public MainWindow()
        {
            InitializeComponent();
            Initialize();
        }

        private async void Initialize()
        {
            _launcherCore = new LauncherCore();
            await _launcherCore.InitializeAsync();
        }
    }
}
