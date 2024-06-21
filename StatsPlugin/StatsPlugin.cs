using CommonPluginHelpers;
using MySqlConnector;
using NinjaBot_DC;
using PluginBase;
using Serilog;
using StatsPlugin.PluginHelper;

namespace StatsPlugin;

// ReSharper disable once ClassNeverInstantiated.Global
public class StatsPlugin : DefaultPlugin
{
    public static MySqlConnectionHelper MySqlConnectionHelper { get; private set; } = null!;


    public override void OnLoad()
    {
        if (ReferenceEquals(PluginDirectory, null))
        {
            OnUnload();
            return;
        }

        var config = Worker.LoadAssemblyConfig(Path.Combine(PluginDirectory,"config.json"), GetType().Assembly, EnvironmentVariablePrefix);
        MySqlConnectionHelper = new MySqlConnectionHelper(EnvironmentVariablePrefix, config, Name);


        //Nullable warning suppressed, check for null is not needed, because it is checked above.
        Directory.CreateDirectory(PluginDirectory); 

        var tableStrings = new[]
        {
            "CREATE TABLE IF NOT EXISTS StatsChannelsIndex (GuildId BIGINT, CategoryChannelId BIGINT, MemberCountChannelId BIGINT, TeamCountChannelId BIGINT, BotCountChannelId BIGINT)",
            "CREATE TABLE IF NOT EXISTS StatsChannelCustomNamesIndex (GuildId BIGINT, ChannelHandle TEXT, CustomName TEXT)",
            "CREATE TABLE IF NOT EXISTS StatsChannelLinkedRolesIndex (EntryId INTEGER PRIMARY KEY AUTO_INCREMENT, GuildId BIGINT, RoleId BIGINT, RoleHandle TEXT)"
        };
        
        try
        {
            var connectionString = MySqlConnectionHelper.GetMySqlConnectionString();
            var connection = new MySqlConnection(connectionString);
            connection.Open();
            MySqlConnectionHelper.InitializeTables(tableStrings,connection);
            connection.Close();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex,"Canceling the Startup of {PluginName} Plugin!", Name);
            return;
        }
        var slashCommands = Worker.GetServiceSlashCommandsExtension();
        
        slashCommands.RegisterCommands<SlashCommandModule>();

        Task.Run(async () =>
        {
            await RefreshServerStats.Execute(Worker.GetServiceDiscordClient());
        });
        
        Log.Information("[Stats Plugin] Plugin Loaded!");
    }

    public override void OnUnload()
    {
        Log.Information("[Stats Plugin] Plugin Unloaded!");
    }
}