namespace ClientAPI
{
    public class ConfigurationSettingsProvider
    {
        private const string hostNameKey = "Host";
        private const string createSessionPathKey = "CreateSessionPath";
        private const string prolongateSessionPathKey = "ProlongateSessionPath";

        public string Host { get; private set; }
        public string CreateSessionPath { get; private set; }    
        public string ProlongateSessionPath { get; private set; }

        public ConfigurationSettingsProvider(ConfigurationManager configurationManager)
        {
            Host = GetConfigurationValue(configurationManager, hostNameKey);
            CreateSessionPath = GetConfigurationValue(configurationManager, createSessionPathKey);
            ProlongateSessionPath = GetConfigurationValue(configurationManager, prolongateSessionPathKey);
        }

        private string GetConfigurationValue(ConfigurationManager configurationManager, string key)
        {
            return configurationManager.GetValue<string>(key) 
                ?? throw new Exception($"The key \"{key}\" was not found in the configuration");
        }
    }
}
