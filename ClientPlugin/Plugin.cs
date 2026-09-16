using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using ClientPlugin.Anomaly;
using ClientPlugin.Aurora;
using ClientPlugin.RichHud;
using ClientPlugin.Settings;
using ClientPlugin.Settings.Layouts;
using Sandbox.Graphics.GUI;
using VRage.Plugins;
using VRage.Utils;

#if !DEV_BUILD
[assembly: AssemblyVersion("1.2.0.0")]
[assembly: AssemblyFileVersion("1.2.0.0")]
#endif

namespace ClientPlugin;

// ReSharper disable once UnusedType.Global
public class Plugin : IPlugin
{
    public const string Name = "Aurora";
    public static Plugin Instance { get; private set; }
    private SettingsGenerator settingsGenerator;
    private bool updateFailed;

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    public void Init(object gameInstance)
    {
        Instance = this;
        Instance.settingsGenerator = new SettingsGenerator();
        Config.Current.PropertyChanged += OnConfigPropertyChanged;
        AnomalyTerminalHook.TryInstall();
    }

    public void Dispose()
    {
        ConfigStorage.FlushPending(true);
        AuroraRenderer.Publish(null);
        AuroraSampler.OnSessionUnloading();
        Instance = null;
    }

    public void Update()
    {
        AnomalyTerminalHook.TryInstall();
        ConfigStorage.FlushPending();
        if (updateFailed)
            return;
        try
        {
            AuroraSampler.Update();
        }
        catch (Exception e)
        {
            updateFailed = true;
            AuroraRenderer.Publish(null);
            MyLog.Default.Error($"{Name}: Update failed, disabling for this session: {e}");
        }
    }

    // ReSharper disable once UnusedMember.Global
    public void LoadAssets(IReadOnlyDictionary<string, string> assets)
    {
        if (assets != null && assets.TryGetValue("AnomalyPack", out var root))
            AnomalyBridge.TryRegisterPack(root);
        else
            MyLog.Default.Warning($"{Name}: AnomalyPack asset missing");
        AnomalyTerminalHook.TryInstall();
    }

    // ReSharper disable once UnusedMember.Global
    public void LoadAssets(string folder)
    {
        // Named AnomalyPack is registered from the dictionary overload.
    }

    private static void OnConfigPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(Config.ColorPreset):
            case nameof(Config.BottomColor):
            case nameof(Config.TopColor):
                AuroraTextures.MarkRampDirty();
                break;
        }
    }

    // ReSharper disable once UnusedMember.Global
    public void OpenConfigDialog()
    {
        Instance.settingsGenerator.SetLayout<Simple>();
        MyGuiSandbox.AddScreen(Instance.settingsGenerator.Dialog);
    }
}
