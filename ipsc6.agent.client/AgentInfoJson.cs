using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ipsc6.agent.client
{
    public class AgentInfoJson
    {
        [JsonPropertyName("powerex")]
        public string PowerExt { get; set; }

        [JsonPropertyName("agentid")]
        public int AgentId { get; set; }

        [JsonPropertyName("username")]
        public string DisplayName { get; set; }

        [JsonPropertyName("power")]
        public IEnumerable<Privilege> Power { get; set; }

        //[JsonPropertyName("svrtime")]
        //public DateTime ServerTime { get; set; }

        [JsonPropertyName("agentgroup")]
        public IList<string> GroupIdIdList { get; set; }

        [JsonPropertyName("agentgroupname")]
        public IList<string> GroupNameList { get; set; }

        [JsonPropertyName("telemode")]
        public TeleMode Telemode { get; set; }

        [JsonPropertyName("synch_interval")]
        public int SynchInterval { get; set; }

        [JsonPropertyName("udl")]
        public string Udl { get; set; }

        [JsonPropertyName("append_agentgroupids")]
        public IList<string> AppendedGroupIdList { get; set; }

        [JsonPropertyName("append_agentgroupnames")]
        public IList<string> AppendedGroupNameList { get; set; }

        [JsonPropertyName("custom")]
        public string Custom { get; set; }

    }
}
