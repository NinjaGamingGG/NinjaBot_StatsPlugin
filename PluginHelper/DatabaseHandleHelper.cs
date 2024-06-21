namespace StatsPlugin.PluginHelper;

public static class DatabaseHandleHelper
{
    public static string GetChannelHandleFromEnum(SlashCommandModule.ChannelHandleEnum handle)
    {

        switch (handle)
        {
            case SlashCommandModule.ChannelHandleEnum.CategoryChannel:
                return "CategoryChannelId";
            
            case SlashCommandModule.ChannelHandleEnum.MemberChannel:
                return "MemberCountChannelId";

            case SlashCommandModule.ChannelHandleEnum.BotChannel:
                return "BotCountChannelId";
            
            case SlashCommandModule.ChannelHandleEnum.TeamChannel:
                return "TeamCountChannelId";
            
            case SlashCommandModule.ChannelHandleEnum.NoChannel:
            default:
                return "NoChannel";

        }
    }
    
    public static string GetRoleHandleFromEnum(SlashCommandModule.RoleHandleEnum handle)
    {

        switch (handle)
        {
            case SlashCommandModule.RoleHandleEnum.TeamRole:
                return "TeamRole";

            case SlashCommandModule.RoleHandleEnum.BotRole:
                return "BotRole";

            case SlashCommandModule.RoleHandleEnum.NoRole:
            default:
                return "NoRole";

        }
    }
    
}