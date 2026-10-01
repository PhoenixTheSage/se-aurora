using System;
using System.Collections.Generic;
using System.Reflection;
using ClientPlugin.Aurora;
using VRage.Utils;

namespace ClientPlugin.Anomaly;

/// <summary>
/// Reflection-only bind to Anomaly. No compile-time reference.
/// </summary>
internal static class AnomalyBridge
{
    public const string PackId = "aurora.borealis";
    public const string PassId = "aurora.borealis.curtain";
    public const string UniformPassId = "aurora.borealis.uniforms";
    public const string NoiseName = "aurora.noise";
    public const string RampName = "aurora.ramp";

    const string PackRegistryType = "ClientPlugin.Shaders.ShaderPackRegistry";
    const string FullscreenType = "ClientPlugin.Shaders.FullscreenPassRegistry";
    const string OwnedPassType = "ClientPlugin.Shaders.OwnedPassRegistry";
    const string CatalogType = "ClientPlugin.Buffers.BufferCatalog";
    const string PublishedType = "ClientPlugin.Buffers.PublishedBuffer";

    static readonly object Gate = new();
    static bool registered;
    static Func<string, float[], bool> setUniforms;
    static Func<string, bool, bool> setEnabled;
    static bool? lastEnabled;
    static AuroraTexture publishedNoiseTexture;
    static AuroraTexture publishedRampTexture;
    public static float VelocityDistanceScale { get; private set; } = 1f;
    static PropertyInfo hasDisplayTenant;
    static MethodInfo catalogPublish;
    static MethodInfo catalogUnpublish;
    static object noisePublished;
    static object rampPublished;
    static MethodInfo publishedPublish;

    /// <summary>
    /// Anomaly AfterUpscale tenant set <c>TemporalPolicy.Display</c>.
    /// Live each get — HDR may register after this pack.
    /// </summary>
    public static bool HasDisplayTenant
    {
        get
        {
            PropertyInfo prop;
            lock (Gate)
            {
                if (hasDisplayTenant == null)
                    ProbeDisplayTenantUnlocked();
                prop = hasDisplayTenant;
            }

            if (prop == null)
                return false;
            try
            {
                return prop.GetValue(null) is true;
            }
            catch
            {
                return false;
            }
        }
    }

    public static bool TryRegisterPack(string root)
    {
        if (string.IsNullOrWhiteSpace(root))
            return false;

        lock (Gate)
        {
            if (registered)
                return true;

            try
            {
                return TryRegisterPackUnlocked(root);
            }
            catch (Exception e)
            {
                MyLog.Default.Error($"{Plugin.Name}: Anomaly pack register failed: {e}");
                return false;
            }
        }
    }

    public static bool PublishTextures() => AuroraTextures.WithTextures(PublishTexturePair);

    static bool PublishTexturePair(AuroraTexture noise, AuroraTexture ramp)
    {
        lock (Gate)
        {
            if (!ReferenceEquals(publishedNoiseTexture, noise))
            {
                if (!PublishOne(NoiseName, noisePublished, noise))
                    return false;
                publishedNoiseTexture = noise;
            }
            if (!ReferenceEquals(publishedRampTexture, ramp))
            {
                if (!PublishOne(RampName, rampPublished, ramp))
                    return false;
                publishedRampTexture = ramp;
            }
            return true;
        }
    }

    public static bool SetUniforms(float[] values)
    {
        Func<string, float[], bool> method;
        lock (Gate)
            method = setUniforms;
        return method != null && method(PassId, values);
    }

    public static bool SetPassEnabled(bool enabled)
    {
        lock (Gate)
        {
            if (setEnabled == null)
                return false;
            if (lastEnabled == enabled)
                return true;
            if (!setEnabled(PassId, enabled))
                return false;
            lastEnabled = enabled;
            return true;
        }
    }

    static bool PublishOne(string name, object published, AuroraTexture texture)
    {
        if (published == null || publishedPublish == null || catalogPublish == null || texture == null)
            return false;
        var native = texture.Resource != null ? texture.Resource.NativePointer : IntPtr.Zero;
        publishedPublish.Invoke(published, new object[]
        {
            texture,
            native,
            texture.Size.X,
            texture.Size.Y,
        });
        return catalogPublish.Invoke(null, new object[] { PackId, name, published }) is true;
    }

    static void OnResolutionChanged()
    {
        // These textures have fixed dimensions; DRS must not retire them or their pixels.
        lock (Gate)
        {
            publishedNoiseTexture = null;
            publishedRampTexture = null;
            lastEnabled = null;
        }
        AuroraRenderer.ResetFailure();
    }

    public static void ReleaseTextures()
    {
        AuroraTextures.Invalidate(UnpublishTextures);
        AuroraRenderer.ResetFailure();
    }

    static void UnpublishTextures()
    {
        lock (Gate)
        {
            publishedNoiseTexture = null;
            publishedRampTexture = null;
            lastEnabled = null;
            try
            {
                catalogUnpublish?.Invoke(null, new object[] { PackId, NoiseName });
                catalogUnpublish?.Invoke(null, new object[] { PackId, RampName });
            }
            catch
            {
                // Anomaly already tearing down.
            }
        }
    }

    static void ProbeDisplayTenantUnlocked()
    {
        if (hasDisplayTenant != null)
            return;
        foreach (var assembly in SafeAssemblies())
        {
            Type owned;
            try
            {
                owned = assembly.GetType(OwnedPassType, false, false);
            }
            catch
            {
                continue;
            }

            hasDisplayTenant = owned?.GetProperty("HasDisplayTenant", BindingFlags.Public | BindingFlags.Static);
            if (hasDisplayTenant != null)
                return;
        }
    }

    static bool TryRegisterPackUnlocked(string root)
    {
        Type packType = null;
        Type fullscreen = null;
        Type owned = null;
        Type catalog = null;
        Type published = null;
        foreach (var assembly in SafeAssemblies())
        {
            packType ??= assembly.GetType(PackRegistryType, false, false);
            fullscreen ??= assembly.GetType(FullscreenType, false, false);
            owned ??= assembly.GetType(OwnedPassType, false, false);
            catalog ??= assembly.GetType(CatalogType, false, false);
            published ??= assembly.GetType(PublishedType, false, false);
        }

        if (packType == null)
        {
            MyLog.Default.Warning($"{Plugin.Name}: Anomaly ShaderPackRegistry not found");
            return false;
        }

        packType.GetMethod("Register", BindingFlags.Public | BindingFlags.Static)
            ?.Invoke(null, new object[] { PackId, root });

        setUniforms = BindStatic<Func<string, float[], bool>>(fullscreen, "SetUniforms", typeof(string), typeof(float[]));
        setEnabled = BindStatic<Func<string, bool, bool>>(fullscreen, "SetEnabled", typeof(string), typeof(bool));
        hasDisplayTenant = owned?.GetProperty("HasDisplayTenant", BindingFlags.Public | BindingFlags.Static);
        catalogPublish = catalog?.GetMethod("Publish", BindingFlags.Public | BindingFlags.Static);
        catalogUnpublish = catalog?.GetMethod("Unpublish", BindingFlags.Public | BindingFlags.Static);
        if (published != null)
        {
            noisePublished = Activator.CreateInstance(published);
            rampPublished = Activator.CreateInstance(published);
            publishedPublish = BindInstance(
                published,
                "Publish",
                typeof(object),
                typeof(IntPtr),
                typeof(int),
                typeof(int));
        }

        // Opt in only when the host accepts the matched decode scale. Older Anomaly
        // builds keep the original raw-metre alpha contract and all current RGB.
        var setDistanceScale = BindStatic<Func<string, float, bool>>(fullscreen,
            "SetVelocityDistanceScale", typeof(string), typeof(float));
        VelocityDistanceScale = setDistanceScale != null && setDistanceScale(PassId, 1000f) ? 1000f : 1f;

        catalog?.GetMethod("RegisterLifetime", BindingFlags.Public | BindingFlags.Static)
            ?.Invoke(null, new object[]
            {
                PackId,
                (Action)OnResolutionChanged,
                (Action)ReleaseTextures,
            });

        var register = FindOwnedRegister(owned);
        register?.Invoke(null, new object[]
        {
            UniformPassId,
            "AfterAtmosphere",
            0,
            0,
            (Action<object>)(_ => AuroraRenderer.PushUniforms()),
            0,
        });

        registered = true;
        MyLog.Default.WriteLine($"{Plugin.Name}: registered Anomaly pack '{PackId}'");
        return true;
    }

    static T BindStatic<T>(Type type, string name, params Type[] args) where T : class
    {
        var method = type?.GetMethod(name, BindingFlags.Public | BindingFlags.Static, null, args, null);
        return method == null ? null : (T)(object)Delegate.CreateDelegate(typeof(T), method);
    }

    static MethodInfo BindInstance(Type type, string name, params Type[] args)
    {
        if (type == null || string.IsNullOrEmpty(name))
            return null;
        try
        {
            return type.GetMethod(
                name,
                BindingFlags.Public | BindingFlags.Instance,
                null,
                args,
                null);
        }
        catch (AmbiguousMatchException)
        {
            return null;
        }
    }

    static MethodInfo FindOwnedRegister(Type owned)
    {
        if (owned == null)
            return null;
        foreach (var method in owned.GetMethods(BindingFlags.Public | BindingFlags.Static))
        {
            if (method.Name != "Register" || method.IsGenericMethodDefinition)
                continue;
            var p = method.GetParameters();
            if (p.Length == 6 &&
                p[0].ParameterType == typeof(string) &&
                p[1].ParameterType == typeof(string) &&
                p[5].ParameterType == typeof(int))
                return method;
        }

        return null;
    }

    static IEnumerable<Assembly> SafeAssemblies()
    {
        Assembly[] assemblies;
        try
        {
            assemblies = AppDomain.CurrentDomain.GetAssemblies();
        }
        catch
        {
            yield break;
        }

        foreach (var assembly in assemblies)
        {
            if (assembly != null && assembly != typeof(AnomalyBridge).Assembly)
                yield return assembly;
        }
    }
}
