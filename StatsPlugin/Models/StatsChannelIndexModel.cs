using Dapper.Contrib.Extensions;

namespace StatsPlugin.Models;

[Table("StatsChannelsIndex")]
public record StatsChannelIndexModel
{
    [ExplicitKey]
    public ulong GuildId { get; set; }
    
    public ulong CategoryChannelId { get; set; }
    
    public ulong MemberCountChannelId { get; set; }
    
    public ulong TeamCountChannelId { get; set; }
    
    public ulong BotCountChannelId { get; set; }
}