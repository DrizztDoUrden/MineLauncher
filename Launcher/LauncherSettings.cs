using Updater.Utilities;

namespace Launcher
{
    public class LauncherSettings : UserSettings
    {
        public string Version
        {
            get { return GetValue("Version"); }
            set { SetValue("Version", value); }
        }

        public string ClientPath
        {
            get { return GetValue("ClientPath"); }
            set { SetValue("ClientPath", value); }
        }

        public bool ConsoleOutput
        {
            get { return GetValue<bool>("ConsoleOutput", bool.TryParse); }
            set { SetValue("ConsoleOutput", value); }
        }

        #region Singleton

        private LauncherSettings() : base("Launcher.config", "MineLauncherServer") { }
        private static readonly object _lock = new object();
        private static LauncherSettings _default;

        public static LauncherSettings Default
        {
            get
            {
                lock (_lock)
                {
                    return _default = _default ?? new LauncherSettings();
                }
            }
        }

        #endregion
    }
}