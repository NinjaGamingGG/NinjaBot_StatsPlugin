using Dapper.Contrib.Extensions;

namespace StatsPlugin.Models;

[Table("StatsChannelCustomNamesIndex")]
public record StatsChannelCustomNamesIndex
{
    [ExplicitKey]
    public ulong GuildId { get; set; }
    
    public string? ChannelHandle { get; set; }
    
    public string? CustomName { get; set; }
}