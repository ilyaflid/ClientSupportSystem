using Microsoft.AspNetCore.DataProtection.KeyManagement;
using System.Linq;

namespace ClientSupportService.Host
{
    public class ConfigurationSettingsProvider : IClientSupportServiceConfiguration
    {
        private const string workingHoursStartTimeKey = "WorkingHours:StartTime";
        private const string workingHoursEndTimeKey = "WorkingHours:EndTime";
        private const string regularAgentsSectionKey = "Agents:RegularAgents";
        private const string additionalAgentsSectionKey = "Agents:AdditionalAgents";
        private const string maximumConcurrencyPerAgentKey = "MaximumConcurrencyPerAgent";
        private const string sessionTimeoutKey = "SessionTimeout";

        private const string agentIdKey = "Id";
        private const string regularAgentLevelKey = "Level";
        private const string regularAgentShiftStartTimeKey = "ShiftStartTime";
        private const string regularAgentShiftTimeInHoursKey = "ShiftTimeInHours";
        private const string regularAgentTeamKey = "Team";

        public TimeOnly WorkingHoursStartTime { get; private set; }
        public TimeOnly WorkingHoursEndTime { get; private set; }
        public int MaximumConcurrencyPerAgent { get; private set; }
        public TimeSpan SessionTimeout { get; private set; }
        public List<RegularAgent> RegularAgents { get; private set; }
        public List<AdditionalAgent> AdditionalAgents { get; private set; }

        public ConfigurationSettingsProvider(ConfigurationManager configuration)
        {
            var regularAgentsSection = GetConfigurationSection(configuration, regularAgentsSectionKey); 
            var additionalAgentsSection = GetConfigurationSection(configuration, additionalAgentsSectionKey);

            WorkingHoursStartTime = GetConfigurationValue<TimeOnly>(configuration, workingHoursStartTimeKey);
            WorkingHoursEndTime = GetConfigurationValue<TimeOnly>(configuration, workingHoursEndTimeKey);
            MaximumConcurrencyPerAgent = GetConfigurationValue<int>(configuration, maximumConcurrencyPerAgentKey);
            SessionTimeout = GetConfigurationValue<TimeSpan>(configuration, sessionTimeoutKey);

            RegularAgents = regularAgentsSection.GetChildren().Select(o => new RegularAgent(
                GetConfigurationValue<int>(o, agentIdKey),
                GetConfigurationValue<TimeOnly>(o, regularAgentShiftStartTimeKey),
                TimeSpan.FromHours(GetConfigurationValue<int>(o, regularAgentShiftTimeInHoursKey)),
                GetConfigurationValue<AgentLevel>(o, regularAgentLevelKey),
                GetConfigurationValue<string>(o, regularAgentTeamKey)
                )).ToList();

            

            AdditionalAgents = additionalAgentsSection.GetChildren().Select(o => new AdditionalAgent(
                GetConfigurationValue<int>(o, agentIdKey),
                WorkingHoursStartTime,
                WorkingHoursEndTime
                )).ToList();
        }

        private T GetConfigurationValue<T>(IConfiguration configuration, string key)
        {
            return configuration.GetValue<T>(key)
                ?? throw new Exception($"The key \"{key}\" was not found in the configuration");
        }
        private IConfigurationSection GetConfigurationSection(IConfiguration configuration, string key)
        {
            return configuration.GetSection(key)
                ?? throw new Exception($"The key \"{key}\" was not found in the configuration");
        }
    }
}
