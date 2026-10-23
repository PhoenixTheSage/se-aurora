using System;
using System.Reflection;
using VRageMath;

namespace ClientPlugin.Aurora;

static class AnomalyScene
{
    public static Vector3D Camera() => new Vector3D(Read("CameraX"), Read("CameraY"), Read("CameraZ"));

    public static Vector3 SunLightDirection() => new Vector3(ReadF("SunX"), ReadF("SunY"), ReadF("SunZ"));

    public static double FrameSeconds()
    {
        // A shared render clock is not exposed yet. Keep the blocked animation
        // static on Vulkan without loading or consulting the DX11 renderer.
        if (!ClientPlugin.Anomaly.AnomalyRenderer.AllowDx11Device)
            return 0d;
        var type = Type.GetType("VRageRender.MyCommon, VRage.Render11");
        var frame = type?.GetProperty("FrameTime", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
        var seconds = frame?.GetType().GetProperty("Seconds")?.GetValue(frame);
        return seconds == null ? 0d : Convert.ToDouble(seconds);
    }

    static double Read(string name)
    {
        var value = Property(name);
        return value == null ? 0d : Convert.ToDouble(value);
    }

    static float ReadF(string name)
    {
        var value = Property(name);
        return value == null ? 0f : Convert.ToSingle(value);
    }

    static object Property(string name)
    {
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            Type type;
            try { type = assembly.GetType("ClientPlugin.Backend.SceneView", false); }
            catch { continue; }
            if (type == null)
                continue;
            return type.GetProperty(name, BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
        }

        return null;
    }
}
