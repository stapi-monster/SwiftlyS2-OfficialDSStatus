using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OfficialDSStatus.Configuration;
using OfficialDSStatus.Services;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Plugins;

namespace OfficialDSStatus;

public partial class OfficialDSStatusPlugin : BasePlugin
{
    private ServiceProvider? _serviceProvider;
    private OfficialDSStatusService? _service;

    public OfficialDSStatusPlugin(ISwiftlyCore core) : base(core)
    {
    }

    public override void Load(bool hotReload)
    {
        Core.Configuration
            .InitializeJsonWithModel<OfficialDSStatusConfig>("config.jsonc", "OfficialDSStatus")
            .Configure(builder =>
            {
                string pluginDir = Core.PluginPath;
                string baseDir = Directory.GetCurrentDirectory();

                string[] paths = new[]
                {
                    Path.Combine(baseDir, "configs", "plugins", "OfficialDSStatus", "config.jsonc"),
                    Path.Combine(baseDir, "configs", "OfficialDSStatus", "config.jsonc"),
                    Path.Combine(pluginDir, "configs", "config.jsonc"),
                    Path.Combine(pluginDir, "resources", "config.jsonc"),
                    Path.Combine(pluginDir, "config.jsonc")
                };

                string? validPath = paths.FirstOrDefault(File.Exists);
                if (validPath != null)
                {
                    builder.AddJsonFile(validPath, optional: false, reloadOnChange: true);
                }
                else
                {
                    builder.AddJsonFile("config.jsonc", optional: true, reloadOnChange: true);
                }
            });

        ServiceCollection services = new();

        services
            .AddSwiftly(Core)
            .AddSingleton<OfficialDSStatusService>()
            .AddOptionsWithValidateOnStart<OfficialDSStatusConfig>()
            .BindConfiguration("OfficialDSStatus");

        _serviceProvider = services.BuildServiceProvider();
        _service = _serviceProvider.GetRequiredService<OfficialDSStatusService>();
    }

    public override void Unload()
    {
    }
}
