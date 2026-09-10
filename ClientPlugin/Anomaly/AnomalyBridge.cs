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
    static MethodInfo setUniforms;
    static MethodInfo setEnabled;
    static MethodInfo catalogPublish;
    static MethodInfo catalogUnpublish;
    static object noisePublished;
    static object rampPublished;
    static MethodInfo publishedPublish;

    public static bool TryRegisterPack(string root)
    {
        if (string.IsNullOrWhiteSpace(root))
            return false;

        lock (Gate)
        {
            if (registered)
                return true;

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

            setUniforms = fullscreen?.GetMethod("SetUniforms", BindingFlags.Public | BindingFlags.Static);
            setEnabled = fullscreen?.GetMethod("SetEnabled", BindingFlags.Public | BindingFlags.Static);
            catalogPublish = catalog?.GetMethod("Publish", BindingFlags.Public | BindingFlags.Static);
            catalogUnpublish = catalog?.GetMethod("Unpublish", BindingFlags.Public | BindingFlags.Static);
            if (published != null)
            {
                noisePublished = Activator.CreateInstance(published);
                rampPublished = Activator.CreateInstance(published);
                publishedPublish = published.GetMethod("Publish", BindingFlags.Public | BindingFlags.Instance);
            }

            catalog?.GetMethod("RegisterLifetime", BindingFlags.Public | BindingFlags.Static)
                ?.Invoke(null, new object[]
                {
                    PackId,
                    (Action)OnLifetimeChanged,
                    (Action)OnLifetimeChanged,
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
    }

    public static void PublishTextures()
    {
        lock (Gate)
        {
            PublishOne(NoiseName, noisePublished, AuroraTextures.Noise);
            PublishOne(RampName, rampPublished, AuroraTextures.Ramp);
        }
    }

    public static void SetUniforms(float[] values)
    {
        MethodInfo method;
        lock (Gate)
            method = setUniforms;
        try
        {
            method?.Invoke(null, new object[] { PassId, values });
        }
        catch (Exception e)
        {
            MyLog.Default.Error($"{Plugin.Name}: SetUniforms failed: {e.Message}");
            RenderTraceBind.Dump("Aurora.SetUniforms", e);
        }
    }

    public static void SetPassEnabled(bool enabled)
    {
        MethodInfo method;
        lock (Gate)
            method = setEnabled;
        try
        {
            method?.Invoke(null, new object[] { PassId, enabled });
        }
        catch (Exception e)
        {
            MyLog.Default.Error($"{Plugin.Name}: SetEnabled failed: {e.Message}");
        }
    }

    static void PublishOne(string name, object published, AuroraTexture texture)
    {
        if (published == null || publishedPublish == null || catalogPublish == null || texture == null)
            return;
        try
        {
            var native = texture.Resource != null ? texture.Resource.NativePointer : IntPtr.Zero;
            publishedPublish.Invoke(published, new object[]
            {
                texture,
                native,
                texture.Size.X,
                texture.Size.Y,
            });
            catalogPublish.Invoke(null, new object[] { PackId, name, published });
        }
        catch (Exception e)
        {
            MyLog.Default.Error($"{Plugin.Name}: catalog publish '{name}' failed: {e.Message}");
        }
    }

    static void OnLifetimeChanged()
    {
        AuroraTextures.Invalidate();
        lock (Gate)
        {
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
