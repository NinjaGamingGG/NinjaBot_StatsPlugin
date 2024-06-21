using Dapper.Contrib.Extensions;

namespace StatsPlugin.Models;

[Table("StatsChannelLinkedRoleIndex")]
public record StatsChannelLinkedRoleIndex
{
    [ExplicitKey]
    public int EntryId { get; set; }
    
    public ulong GuildId { get; set; }
    
    public ulong RoleId { get; set; }
    
    public string? RoleHandle { get; set; }
}