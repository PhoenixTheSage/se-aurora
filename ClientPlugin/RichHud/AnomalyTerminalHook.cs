using System;
using System.Reflection;
using ClientPlugin.Settings;
using VRage.Utils;
using VRageMath;

namespace ClientPlugin.RichHud;

internal static class AnomalyTerminalHook
{
    public const string RegistryTypeName = "ClientPlugin.RichHud.TerminalConfigRegistry";
    public const string FolderTitle = "Aurora Borealis";
    public const string SettingsPage = "Settings";

    static readonly object Gate = new();
    static bool installed;

    public static bool TryInstall()
    {
        lock (Gate)
        {
            if (installed)
                return true;

            var page = RequestPage();
            if (page == null)
                return false;

            Populate(page);
            installed = true;
            MyLog.Default.WriteLine($"{Plugin.Name}: Rich HUD page under Anomaly Shaders / {FolderTitle} / {SettingsPage}");
            return true;
        }
    }

    static object RequestPage()
    {
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            if (assembly == null || assembly == typeof(AnomalyTerminalHook).Assembly)
                continue;

            Type registry;
            try
            {
                registry = assembly.GetType(RegistryTypeName, false, false);
            }
            catch
            {
                continue;
            }

            if (registry == null)
                continue;

            var requestFolder = registry.GetMethod("RequestFolderPage", BindingFlags.Public | BindingFlags.Static,
                null, new[] { typeof(string), typeof(string) }, null);
            try
            {
                return requestFolder?.Invoke(null, new object[] { FolderTitle, SettingsPage });
            }
            catch (Exception e)
            {
                MyLog.Default.Warning($"{Plugin.Name}: RequestFolderPage failed: {e.Message}");
                return null;
            }
        }

        return null;
    }

    static void Populate(object page)
    {
        var t = page.GetType();
        Invoke(t, page, "Category", "Aurora");
        Invoke(t, page, "Checkbox", "Enabled",
            (Func<bool>)(() => Config.Current.Enabled),
            (Action<bool>)(v => Set(() => Config.Current.Enabled = v)),
            "Master switch for the aurora effect");
        Invoke(t, page, "Slider", "Intensity", 0f, 1f,
            (Func<float>)(() => Config.Current.Intensity),
            (Action<float>)(v => Set(() => Config.Current.Intensity = v)),
            "Artistic brightness of the aurora (SDR and HDR)", 0.01f);
        Invoke(t, page, "Slider", "HDR lift", 1f, 16f,
            (Func<float>)(() => Config.Current.HdrLift),
            (Action<float>)(v => Set(() => Config.Current.HdrLift = v)),
            "Extra emission when an HDR Display pack is live (AfterUpscale). 1 = same as SDR; ignored without a Display tenant.", 0.5f);
        Invoke(t, page, "Slider", "Contrast", 1f, 6f,
            (Func<float>)(() => Config.Current.Contrast),
            (Action<float>)(v => Set(() => Config.Current.Contrast = v)),
            "Separation between the bright curtain cores and the haze between them; higher is punchier, 2 is the soft look of earlier versions", 0.1f);
        Invoke(t, page, "Slider", "Ground light", 0f, 1f,
            (Func<float>)(() => Config.Current.GroundLight),
            (Action<float>)(v => Set(() => Config.Current.GroundLight = v)),
            "Aurora light tinting the terrain below (ambient glow, most visible on snow)", 0.01f);
        Invoke(t, page, "Dropdown", "Quality", typeof(AuroraQuality),
            (Func<object>)(() => Config.Current.Quality),
            (Action<object>)(v => Set(() => Config.Current.Quality = (AuroraQuality)v)),
            "Raymarching quality (number of volume samples per pixel)");

        Invoke(t, page, "Category", "Colors");
        Invoke(t, page, "Dropdown", "Color preset", typeof(AuroraColorPreset),
            (Func<object>)(() => Config.Current.ColorPreset),
            (Action<object>)(v => Set(() => Config.Current.ColorPreset = (AuroraColorPreset)v)),
            "Named gradient, or Custom to keep the pickers. Editing a picker switches to Custom.");
        Invoke(t, page, "Color", "Bottom color",
            (Func<Color>)ActiveBottom,
            (Action<Color>)(v => SetCustomColor(true, v)),
            "Bright lower edge. Shown from the active preset; editing switches to Custom.");
        Invoke(t, page, "Color", "Top color",
            (Func<Color>)ActiveTop,
            (Action<Color>)(v => SetCustomColor(false, v)),
            "Fading upper tail. Shown from the active preset; editing switches to Custom.");

        Invoke(t, page, "Category", "Placement");
        Invoke(t, page, "Slider", "Latitude center", 45f, 85f,
            (Func<float>)(() => Config.Current.LatitudeCenter),
            (Action<float>)(v => Set(() => Config.Current.LatitudeCenter = v)),
            "Latitude of the center of the aurora band (degrees, both hemispheres)", 1f);
        Invoke(t, page, "Slider", "Latitude width", 4f, 48f,
            (Func<float>)(() => Config.Current.LatitudeWidth),
            (Action<float>)(v => Set(() => Config.Current.LatitudeWidth = v)),
            "Width of the aurora band (degrees of latitude)", 1f);
        Invoke(t, page, "Slider", "Magnetic axis tilt", 0f, 45f,
            (Func<float>)(() => Config.Current.MagneticAxisTilt),
            (Action<float>)(v => Set(() => Config.Current.MagneticAxisTilt = v)),
            "Tilt of the magnetic axis from the rotation axis (degrees); tilted away from the sun", 1f);
        Invoke(t, page, "Slider", "Altitude min", 0f, 1f,
            (Func<float>)(() => Config.Current.AltitudeMin),
            (Action<float>)(v => Set(() => Config.Current.AltitudeMin = v)),
            "Bottom of the aurora shell (0 = surface, 1 = top of atmosphere)", 0.01f);
        Invoke(t, page, "Slider", "Altitude max", 0.05f, 1f,
            (Func<float>)(() => Config.Current.AltitudeMax),
            (Action<float>)(v => Set(() => Config.Current.AltitudeMax = v)),
            "Top of the aurora shell (0 = surface, 1 = top of atmosphere)", 0.01f);
        Invoke(t, page, "Slider", "Pattern density", 0.25f, 4f,
            (Func<float>)(() => Config.Current.PatternDensity),
            (Action<float>)(v => Set(() => Config.Current.PatternDensity = v)),
            "How many curtains fit across the polar cap; lower is sparser with larger structures", 0.05f);
        Invoke(t, page, "Slider", "Coverage", 0.05f, 1f,
            (Func<float>)(() => Config.Current.Coverage),
            (Action<float>)(v => Set(() => Config.Current.Coverage = v)),
            "Fraction of the aurora lit at any one time", 0.01f);
        Invoke(t, page, "Slider", "Fade start", 1f, 100f,
            (Func<float>)(() => Config.Current.FadeStartFactor),
            (Action<float>)(v => Set(() => Config.Current.FadeStartFactor = v)),
            "Distance from the planet where the aurora starts to fade (atmosphere radii)", 0.1f);
        Invoke(t, page, "Slider", "Fade end", 1f, 100f,
            (Func<float>)(() => Config.Current.FadeEndFactor),
            (Action<float>)(v => Set(() => Config.Current.FadeEndFactor = v)),
            "Distance from the planet where the aurora becomes fully invisible", 0.1f);

        Invoke(t, page, "Category", "Animation");
        Invoke(t, page, "Slider", "Animation speed", 0f, 12f,
            (Func<float>)(() => Config.Current.AnimationSpeed),
            (Action<float>)(v => Set(() => Config.Current.AnimationSpeed = v)),
            "Speed of the curtain movement", 0.1f);
        Invoke(t, page, "Checkbox", "Night only",
            (Func<bool>)(() => Config.Current.NightOnly),
            (Action<bool>)(v => Set(() => Config.Current.NightOnly = v)),
            "Show the aurora only on the night side of the planet");
    }

    static Color ActiveBottom()
    {
        Config.Current.GetGradientColors(out var bottom, out _);
        return new Color(bottom);
    }

    static Color ActiveTop()
    {
        Config.Current.GetGradientColors(out _, out var top);
        return new Color(top);
    }

    static void SetCustomColor(bool bottom, Color value)
    {
        Set(() =>
        {
            if (bottom)
                Config.Current.BottomColor = value;
            else
                Config.Current.TopColor = value;
            Config.Current.ColorPreset = AuroraColorPreset.Custom;
        });
    }

    static void Set(Action apply)
    {
        apply();
        ConfigStorage.Save(Config.Current);
    }

    static void Invoke(Type type, object instance, string name, params object[] args)
    {
        const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance;
        foreach (var method in type.GetMethods(flags))
        {
            if (method.Name != name || method.IsGenericMethodDefinition)
                continue;
            var parameters = method.GetParameters();
            if (args.Length > parameters.Length)
                continue;
            if (args.Length < parameters.Length)
            {
                var optional = true;
                for (var j = args.Length; j < parameters.Length; j++)
                {
                    if (!parameters[j].IsOptional && !parameters[j].HasDefaultValue)
                    {
                        optional = false;
                        break;
                    }
                }

                if (!optional)
                    continue;
            }
            var match = true;
            for (var i = 0; i < args.Length; i++)
            {
                var value = args[i];
                var expected = parameters[i].ParameterType;
                if (value == null)
                {
                    if (expected.IsValueType && Nullable.GetUnderlyingType(expected) == null)
                    {
                        match = false;
                        break;
                    }

                    continue;
                }

                if (!expected.IsInstanceOfType(value))
                {
                    match = false;
                    break;
                }
            }

            if (!match)
                continue;
            method.Invoke(instance, PadDefaults(method, args));
            return;
        }

        MyLog.Default.Warning($"{Plugin.Name}: Anomaly terminal missing {name}");
    }

    static object[] PadDefaults(MethodInfo method, object[] args)
    {
        var parameters = method.GetParameters();
        if (args.Length == parameters.Length)
            return args;
        var padded = new object[parameters.Length];
        for (var i = 0; i < parameters.Length; i++)
            padded[i] = i < args.Length ? args[i] : parameters[i].DefaultValue;
        return padded;
    }
}
