using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OfficialDSStatus.Configuration;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Commands;
using SwiftlyS2.Shared.Events;
using SwiftlyS2.Shared.GameEventDefinitions;
using SwiftlyS2.Shared.Misc;

namespace OfficialDSStatus.Services;

public class OfficialDSStatusService
{
    private readonly ISwiftlyCore Core;
    private readonly IOptionsMonitor<OfficialDSStatusConfig> _config;
    private OfficialDSStatusConfig? _fallbackConfig;

    public OfficialDSStatusService(ISwiftlyCore core, IOptionsMonitor<OfficialDSStatusConfig> config)
    {
        Core = core;
        _config = config;
        core.Registrator.Register(this);

        Core.Event.OnMapLoad += OnMapLoadHandler;

        Core.GameEvent.HookPost<EventRoundStart>((@event) =>
        {
            var cfg = GetActiveConfig();
            if (cfg.Enabled && cfg.SetValveDSOnRoundStart)
            {
                ApplyValveDSStatus();
            }
            return HookResult.Continue;
        });

        ApplyValveDSStatus();
    }

    private void OnMapLoadHandler(IOnMapLoadEvent @event)
    {
        var config = GetActiveConfig();
        if (!config.Enabled || !config.SetValveDSOnMapLoad) return;

        Core.Scheduler.DelayBySeconds(1.0f, () => ApplyValveDSStatus());
    }

    public OfficialDSStatusConfig GetActiveConfig()
    {
        var current = _config.CurrentValue;
        if (current != null) return current;

        if (_fallbackConfig == null)
        {
            _fallbackConfig = LoadConfigFromFile();
        }

        return _fallbackConfig ?? new OfficialDSStatusConfig();
    }

    private OfficialDSStatusConfig LoadConfigFromFile()
    {
        try
        {
            string[] possiblePaths = new[]
            {
                Path.Combine(Directory.GetCurrentDirectory(), "configs", "plugins", "OfficialDSStatus", "config.jsonc"),
                Path.Combine(Directory.GetCurrentDirectory(), "configs", "OfficialDSStatus", "config.jsonc"),
                Path.Combine(Core.PluginPath, "configs", "config.jsonc"),
                Path.Combine(Core.PluginPath, "resources", "config.jsonc"),
                Path.Combine(Core.PluginPath, "config.jsonc"),
            };

            foreach (var path in possiblePaths)
            {
                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path);
                    var options = new JsonSerializerOptions
                    {
                        ReadCommentHandling = JsonCommentHandling.Skip,
                        AllowTrailingCommas = true,
                        PropertyNameCaseInsensitive = true
                    };

                    var cfg = JsonSerializer.Deserialize<OfficialDSStatusConfig>(json, options);
                    if (cfg != null) return cfg;
                }
            }
        }
        catch (Exception ex)
        {
            Core.Logger.LogError(ex, "[OfficialDSStatus] Error reading config directly from file.");
        }

        return new OfficialDSStatusConfig();
    }

    public void ApplyValveDSStatus()
    {
        var config = GetActiveConfig();
        if (!config.Enabled) return;

        try
        {
            var gameRules = Core.EntitySystem.GetGameRules();
            if (gameRules != null && gameRules.IsValid)
            {
                gameRules.IsValveDS = true;

                if (config.DebugLog)
                {
                    Core.Logger.LogInformation("[OfficialDSStatus] Successfully set server status to Official DS (m_bIsValveDS = true).");
                }
            }
        }
        catch (Exception ex)
        {
            Core.Logger.LogError(ex, "[OfficialDSStatus] Error setting Valve DS status on GameRules.");
        }
    }

    [Command("officialdsstatus_reload", permission: "@admin/root")]
    [CommandAlias("sw_officialdsstatus_reload")]
    public void Command_Reload(ICommandContext context)
    {
        _fallbackConfig = null;
        ApplyValveDSStatus();

        string msg = "[LIME][OfficialDSStatus][DEFAULT] Конфигурация успешно перезагружена!";
        if (context.IsSentByPlayer && context.Sender != null)
        {
            context.Sender.SendChat(msg);
        }
        else
        {
            context.Reply("[OfficialDSStatus] Configuration successfully reloaded!");
        }
    }
}
