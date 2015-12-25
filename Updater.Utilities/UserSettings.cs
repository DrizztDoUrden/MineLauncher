using System.Configuration;

namespace Updater.Utilities
{
    public abstract class UserSettings
    {
        private Configuration _cfg;
        private AppSettingsSection _section;

        #region Cfg reading

        private static Configuration GetConfig(string path)
        {
            var configFileMap = new ExeConfigurationFileMap();
            configFileMap.ExeConfigFilename = path;
            var retVal = ConfigurationManager.OpenMappedExeConfiguration(configFileMap, ConfigurationUserLevel.None);
            return retVal;
        }

        private static AppSettingsSection GetAppSettings(Configuration config, string sectionName)
        {
            AppSettingsSection section;
            var rawSection = config.Sections[sectionName];

            if (rawSection == null)
            {
                section = new AppSettingsSection();
                section.SectionInformation.AllowExeDefinition = ConfigurationAllowExeDefinition.MachineToLocalUser;
                config.Sections.Add(sectionName, section);
            }
            else
                section = (AppSettingsSection)rawSection;

            return section;
        }

        #endregion

        #region Cfg parameters getting

        protected string GetValue(string name) => _section.Settings[name]?.Value;

        protected void SetValue(string name, string value)
        {
            if (_section.Settings[name] == null)
                _section.Settings.Add(name, value);
            else
                _section.Settings[name].Value = value;
        }

        #endregion

        protected UserSettings(string file, string section)
        {
            _cfg = GetConfig(file);
            _section = GetAppSettings(_cfg, section);
        }

        public void Save() => _cfg.Save();
    }
}
