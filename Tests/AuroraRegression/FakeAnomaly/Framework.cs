using System;
using System.Collections.Generic;

namespace ClientPlugin.Shaders
{
    public static class ShaderPackRegistry { public static void Register(string id, string root) { } }
    public static class FullscreenPassRegistry
    {
        public static bool AcceptScale = true;
        public static int EnabledCalls;
        public static bool Enabled;
        public static float[] Uniforms;
        public static Exception UniformFailure;
        public static float DistanceScale;
        public static bool SetUniforms(string id, float[] values)
        {
            if (UniformFailure != null) throw UniformFailure;
            Uniforms = (float[])values.Clone(); return true;
        }
        public static bool SetEnabled(string id, bool enabled) { EnabledCalls++; Enabled = enabled; return true; }
        public static bool SetVelocityDistanceScale(string id, float scale)
        { if (!AcceptScale) return false; DistanceScale = scale; return true; }
    }
    public static class OwnedPassRegistry
    {
        public static bool HasDisplayTenant => false;
        public static Action<object> Draw;
        public static void Register(string id, string slot, int priority, int temporal, Action<object> draw, int phase) { Draw = draw; }
    }
}
namespace ClientPlugin.Buffers
{
    public sealed class PublishedBuffer
    {
        public object Srv;
        public byte[] Rgba;
        public static Exception UploadFailure;
        public void PublishRgba8(byte[] rgba, int width, int height)
        { if (UploadFailure != null) throw UploadFailure; Rgba = rgba; }
        public void Publish(object srv, IntPtr native, int width, int height) { Srv = srv; }
    }
    public static class BufferCatalog
    {
        public static int PublishCalls;
        public static Action ResolutionChanged;
        public static Action DeviceEnd;
        public static readonly Dictionary<string, PublishedBuffer> Entries = new Dictionary<string, PublishedBuffer>();
        public static bool Publish(string pack, string name, PublishedBuffer value) { PublishCalls++; Entries[name] = value; return true; }
        public static void Unpublish(string pack, string name) { Entries.Remove(name); }
        public static void RegisterLifetime(string pack, Action resolution, Action deviceEnd) { ResolutionChanged = resolution; DeviceEnd = deviceEnd; }
    }
}
namespace ClientPlugin.Backend
{
    public static class SceneView
    {
        public static double CameraX => 0;
        public static double CameraY => 0;
        public static double CameraZ => 150000;
        public static float SunX => 0;
        public static float SunY => 0;
        public static float SunZ => 1;
    }
}
