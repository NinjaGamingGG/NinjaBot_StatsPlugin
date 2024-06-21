using System.Diagnostics.CodeAnalysis;
using Dapper;
using Dapper.Contrib.Extensions;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Net.Models;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using MySqlConnector;
using Serilog;
using StatsPlugin.Models;
using StatsPlugin.PluginHelper;

namespace StatsPlugin;

[SlashCommandGroup("stats", "Stats Plugin Commands")]
[SlashRequirePermissions(Permissions.Administrator)]
// ReSharper disable once ClassNeverInstantiated.Global
public class SlashCommandModule : ApplicationCommandModule
{
    [SlashCommand("setup", "Setup for Stats Channel")]
    [SuppressMessage("Performance", "CA1822:Member als statisch markieren")]
    public async Task SetupChannelCommand(InteractionContext ctx)
    {
        await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

        var guild = ctx.Guild;
        var newCategory = await guild.CreateChannelCategoryAsync(@"· • ●  📊 Stats 📊 ● • ·");

        await newCategory.ModifyAsync(NewEditModel);
        
        var memberCountChannel = await guild.CreateChannelAsync("╔😎～Mitglieder:", ChannelType.Voice, newCategory);
        var botCountChannel = await guild.CreateChannelAsync("╠🤖～Bot Count:", ChannelType.Voice, newCategory);
        var teamCountChannel = await guild.CreateChannelAsync("╚🥷～Teammitglieder:", ChannelType.Voice, newCategory);
        
        var statsChannelModel = new StatsChannelIndexModel()
        {
            GuildId = guild.Id, 
            CategoryChannelId = newCategory.Id, 
            MemberCountChannelId = memberCountChannel.Id, 
            BotCountChannelId = botCountChannel.Id, 
            TeamCountChannelId = teamCountChannel.Id
        };


        var connectionString = StatsPlugin.MySqlConnectionHelper.GetMySqlConnectionString();
        try
        {
            await using var mySqlConnection = new MySqlConnection(connectionString);
            await mySqlConnection.OpenAsync();
            var hasUpdated = await mySqlConnection.UpdateAsync(statsChannelModel);
            await mySqlConnection.CloseAsync();
            
            if (hasUpdated == false)
                await mySqlConnection.InsertAsync(statsChannelModel);
            await mySqlConnection.CloseAsync();
        }
        catch (MySqlException ex)
        {
            Log.Error(ex,"Error while inserting new Stats Plugin Guild config into database");
            return;
        }
        
        await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Done!"));
        return;

        void NewEditModel(ChannelEditModel editModel)
        {
            editModel.PermissionOverwrites = new List<DiscordOverwriteBuilder>()
            {
                new DiscordOverwriteBuilder(guild.EveryoneRole)
                    .Allow(Permissions.AccessChannels)
                    .Deny(Permissions.SendMessages)
                    .Deny(Permissions.UseVoice)
                    .Deny(Permissions.SendMessages)
                    .Deny(Permissions.CreatePublicThreads)
                    .Deny(Permissions.CreatePrivateThreads)
                    .Deny(Permissions.ManageThreads)
                    .For(guild.EveryoneRole)
            };
        }
    }

    [SlashCommand("Link-Channel", "Links Stats Channel")]
    [SuppressMessage("Performance", "CA1822:Member als statisch markieren")]
    public async Task LinkChannelCommand(InteractionContext ctx, [Option("Channel", "Target Channel to Link")] DiscordChannel channel, 
        [Option("Channel-Handle", "Handle of the Channel you want to Link")]
ChannelHandleEnum channelHandle = ChannelHandleEnum.NoChannel  )
    {
        await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);
        int hasUpdated;

        var connectionString = StatsPlugin.MySqlConnectionHelper.GetMySqlConnectionString();

        try
        {
            await using var mySqlConnection = new MySqlConnection(connectionString);
            await mySqlConnection.OpenAsync();

            var channelHandleInDb = DatabaseHandleHelper.GetChannelHandleFromEnum(channelHandle);

            if (channelHandleInDb == "NoChannel")
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Error, Invalid Channel Handle!"));
                return;
            }

            hasUpdated = await mySqlConnection.ExecuteAsync(
                "UPDATE StatsChannelsIndex SET " + channelHandleInDb + " = @ChannelId WHERE GuildId = @GuildId",
                new { ChannelId = channel.Id, GuildId = ctx.Guild.Id });

            await mySqlConnection.CloseAsync();
        }
        catch (MySqlException ex)
        {
            Log.Error(ex, "Error while Updating Database Channel Handle Link for Stats Plugin");
            return;
        }

        if (hasUpdated == 0)
        {
            await ctx.EditResponseAsync(
                new DiscordWebhookBuilder().WithContent("Error, Unable to Update Channel in Database!"));
            return;
        }

        await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Done!"));
    }

    [SlashCommand("Link-Role", "Links Stats Role")]
    [SuppressMessage("Performance", "CA1822:Member als statisch markieren")]
    public async Task LinkRoleCommand(InteractionContext ctx, [Option("role", "Target role to Link")] DiscordRole role,
        [Option("Role-Handle", "Handle of the Role you want to Link")]
        RoleHandleEnum roleHandle = RoleHandleEnum.NoRole)
    {
        await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

        int hasInserted;
        var connectionString = StatsPlugin.MySqlConnectionHelper.GetMySqlConnectionString();

        
        try
        {
            await using var mySqlConnection = new MySqlConnection(connectionString);
            await mySqlConnection.OpenAsync();

            var roleHandleInDb = DatabaseHandleHelper.GetRoleHandleFromEnum(roleHandle);

            if (roleHandleInDb == "NoRole")
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Error, Invalid Role Handle!"));
                return;
            }

            var hasUpdated = await mySqlConnection.ExecuteAsync(
                "UPDATE StatsChannelLinkedRolesIndex SET RoleHandle = @RoleHandle WHERE RoleId = @RoleId AND GuildId = @GuildId",
                new { RoleId = role.Id, GuildId = ctx.Guild.Id, RoleHandle = roleHandleInDb });

            if (hasUpdated == 1)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Done!"));
                return;
            }

            hasInserted = await mySqlConnection.ExecuteAsync(
                "INSERT INTO StatsChannelLinkedRolesIndex (GuildId, RoleId, RoleHandle) VALUES (@GuildId, @RoleId, @RoleHandle)",
                new { RoleId = role.Id, GuildId = ctx.Guild.Id, RoleHandle = roleHandleInDb });
            await mySqlConnection.CloseAsync();
        }
        catch (MySqlException ex)
        {
            Log.Error(ex, "Error while Linking Role for Stats Plugin");
            return;
        }


        if (hasInserted == 0)
        {
            await ctx.EditResponseAsync(
                new DiscordWebhookBuilder().WithContent("Error, Unable to Update Role in Database!"));
            return;
        }

        await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Done!"));
    }

    [SlashCommand("rename","Set a custom name for the specified channel")]
    [SuppressMessage("Performance", "CA1822:Member als statisch markieren")]
    public async Task RenameChannelCommand(InteractionContext ctx, [Option("Channel-Handle", "Handle of the Channel you want to Link")] ChannelHandleEnum channelHandle, [Option("Name", "New Name for the Channel")] string name)
    {
        await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);
        
        var channelHandleInDb = DatabaseHandleHelper.GetChannelHandleFromEnum(channelHandle);
        
        if (channelHandleInDb == "NoChannel")
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Error, Invalid Channel Handle!"));
            return;
        }

        var connectionString = StatsPlugin.MySqlConnectionHelper.GetMySqlConnectionString();

        try
        {
            await using var mySqlConnection = new MySqlConnection(connectionString);
        
            var hasUpdated = await mySqlConnection.ExecuteAsync("UPDATE StatsChannelCustomNamesIndex SET CustomName = @Name WHERE GuildId = @GuildId AND ChannelHandle = @ChannelHandle", new { Name = name, GuildId = ctx.Guild.Id, ChannelHandle = channelHandleInDb });
        
            if (hasUpdated == 0)
            {
                var renameRecord = new StatsChannelCustomNamesIndex()
                {
                    GuildId = ctx.Guild.Id,
                    ChannelHandle = channelHandleInDb,
                    CustomName = name
                };

                await mySqlConnection.InsertAsync(renameRecord);
                await mySqlConnection.CloseAsync();
            }
        }
        catch (MySqlException ex)
        {
            Log.Error(ex,"Error while Renaming Custom name for StatsPlugin Command");
            return;
        }
        
        await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Done!"));
    }
    
    [SlashCommand("disable","Disables the functionality on this server")]
    [SuppressMessage("Performance", "CA1822:Member als statisch markieren")]
    public async Task DisableCommand(InteractionContext ctx)
    {

        await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);
        
        var connectionString = StatsPlugin.MySqlConnectionHelper.GetMySqlConnectionString();
        int hasUpdated;
        try
        {
            await using var mySqlConnection = new MySqlConnection(connectionString);
            await mySqlConnection.OpenAsync();
            var entry = await mySqlConnection.GetAsync<StatsChannelIndexModel>(ctx.Guild.Id);
        
            if (entry == null)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("This functionality is already disabled!"));
                return;
            }

            try
            {
                await ctx.Guild.GetChannel(entry.MemberCountChannelId).DeleteAsync();
                await ctx.Guild.GetChannel(entry.BotCountChannelId).DeleteAsync();
                await ctx.Guild.GetChannel(entry.TeamCountChannelId).DeleteAsync();
                await ctx.Guild.GetChannel(entry.CategoryChannelId).DeleteAsync();
            }
            catch (Exception e)
            {
                Log.Error(e,"Unable to delete Stat-Channels on Guild:{GuildId}",ctx.Guild.Id);
            }

        
            hasUpdated = await mySqlConnection.ExecuteAsync("DELETE FROM StatsChannelsIndex WHERE GuildId = @GuildId", new { GuildId = ctx.Guild.Id });
            await mySqlConnection.OpenAsync();

        }
        catch (MySqlException ex)
        {
            Log.Fatal(ex,"Unable to delete Stats Plugin Config from Database");
            return;
        }

        
        if (hasUpdated == 0)
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Unable to disable this functionality. Please contact an bot operator!"));
        }
        
        await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Done!"));
    }
    
    [SlashCommand("unlink-role","Unlinks the role from the specified handle")]
    [SuppressMessage("Performance", "CA1822:Member als statisch markieren")]
    public async Task UnlinkRoleCommand(InteractionContext ctx,[Option("role","Role to unlink")] DiscordRole role,
        [Option("Role-Handle", "Handle of the Role you want to Link")] RoleHandleEnum roleHandle = RoleHandleEnum.NoRole)
    {

        await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);
        
        var connectionString = StatsPlugin.MySqlConnectionHelper.GetMySqlConnectionString();
        int hasUpdated;

        try
        {
            await using var mySqlConnection = new MySqlConnection(connectionString);
            await mySqlConnection.OpenAsync();
        
            if (roleHandle == RoleHandleEnum.NoRole)
            {
                hasUpdated = await mySqlConnection.ExecuteAsync("DELETE FROM StatsChannelLinkedRolesIndex WHERE GuildId = @GuildId AND RoleId = @RoleId", new { GuildId = ctx.Guild.Id, RoleId = role.Id });
            }
            else
            {
                var roleHandleInDb = DatabaseHandleHelper.GetRoleHandleFromEnum(roleHandle);
                hasUpdated = await mySqlConnection.ExecuteAsync("DELETE FROM StatsChannelLinkedRolesIndex WHERE GuildId = @GuildId AND RoleId = @RoleId AND RoleHandle = @RoleHandle", new { GuildId = ctx.Guild.Id, RoleId = role.Id, RoleHandle = roleHandleInDb });
            }
            
            await mySqlConnection.CloseAsync();

        }
        catch (MySqlException ex)
        {
            Log.Error(ex,"Error while unlinking role in Stats Plugin Config");
            throw;
        }
        

        if (hasUpdated == 0)
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Unable to disable this functionality. Please contact an bot operator!"));
            return;
        }
        
        await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Done!"));
    }

    public enum ChannelHandleEnum
    {
        [ChoiceName("Category Channel")]
        CategoryChannel,
        [ChoiceName("Member Counter Channel")]
        MemberChannel,
        [ChoiceName("Bot Counter Channel")]
        BotChannel,
        [ChoiceName("Team Counter Channel")]
        TeamChannel,
        NoChannel
    }
    
    public enum RoleHandleEnum
    {
        [ChoiceName("Team Role")]
        TeamRole,
        [ChoiceName("Bot Role")]
        BotRole,
        NoRole
    }

}