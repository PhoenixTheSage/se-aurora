using System;
using ClientPlugin.Anomaly;
using VRageMath;
using VRageRender;

namespace ClientPlugin.Aurora;

/// <summary>
/// Game/render-thread uniforms for the Anomaly AfterAtmosphere fullscreen program.
/// Does not compile or draw; Anomaly owns the pass.
/// </summary>
public static class AuroraRenderer
{
    static volatile AuroraSnapshot snapshot;

    public static void Publish(AuroraSnapshot value)
    {
        snapshot = value;
    }

    public static void PushUniforms()
    {
        var snap = snapshot;
        var config = Config.Current;
        if (snap == null || !config.Enabled)
        {
            AnomalyBridge.SetPassEnabled(false);
            AnomalyBridge.SetUniforms(new float[32]);
            return;
        }

        float fadeFactor = ComputeNightFactor(snap, config) * ComputeDistanceFade(snap);
        if (fadeFactor <= 0f)
        {
            AnomalyBridge.SetPassEnabled(false);
            AnomalyBridge.SetUniforms(new float[32]);
            return;
        }

        AuroraTextures.EnsureCreated(config);
        AnomalyBridge.PublishTextures();
        AnomalyBridge.SetPassEnabled(true);
        AnomalyBridge.SetUniforms(PackConstants(snap, config, fadeFactor));
    }

    static float ComputeNightFactor(AuroraSnapshot snap, Config config)
    {
        if (!config.NightOnly)
            return 1f;

        var up = (Vector3)Vector3D.Normalize(
            MyRender11.Environment.Matrices.CameraPosition - snap.PlanetCenter);
        var dirToSun = -MyRender11.Environment.Data.EnvironmentLight.SunLightDirection;
        float elevation = up.Dot(dirToSun);
        return MathHelper.Clamp((0.05f - elevation) / 0.20f, 0f, 1f);
    }

    static float ComputeDistanceFade(AuroraSnapshot snap)
    {
        float distance = (float)(MyRender11.Environment.Matrices.CameraPosition - snap.PlanetCenter).Length();
        if (distance <= snap.FadeStartDistance)
            return 1f;
        return MathHelper.Clamp(
            (snap.FadeEndDistance - distance) / Math.Max(snap.FadeEndDistance - snap.FadeStartDistance, 1f),
            0f, 1f);
    }

    static Vector3 ComputeMagneticAxis(Vector3 pole, float tiltDegrees)
    {
        if (tiltDegrees <= 0f)
            return pole;

        var dirToSun = -MyRender11.Environment.Data.EnvironmentLight.SunLightDirection;
        var sunPerp = dirToSun - pole * pole.Dot(dirToSun);
        float length = sunPerp.Length();
        if (length < 1e-3f)
            return pole;
        sunPerp /= length;

        float tilt = MathHelper.ToRadians(tiltDegrees);
        return pole * (float)Math.Cos(tilt) - sunPerp * (float)Math.Sin(tilt);
    }

    static float[] PackConstants(AuroraSnapshot snap, Config config, float fadeFactor)
    {
        var centerRel = (Vector3)(snap.PlanetCenter - MyRender11.Environment.Matrices.CameraPosition);

        var pole = ComputeMagneticAxis(snap.PoleAxis, config.MagneticAxisTilt);
        var reference = Math.Abs(pole.X) < 0.9f ? Vector3.UnitX : Vector3.UnitY;
        var tangent1 = Vector3.Normalize(Vector3.Cross(pole, reference));

        float halfWidth = config.LatitudeWidth * 0.5f;
        float latLo = MathHelper.ToRadians(MathHelper.Clamp(config.LatitudeCenter - halfWidth, 0f, 89.5f));
        float latHi = MathHelper.ToRadians(MathHelper.Clamp(config.LatitudeCenter + halfWidth, 1f, 89.9f));
        float sinLo = (float)Math.Sin(latLo);
        float sinHi = (float)Math.Sin(latHi);
        float feather = Math.Max((sinHi - sinLo) * 0.25f, 1e-4f);

        float density = Math.Max(config.PatternDensity, 0.01f);
        float tiling1 = 9f * density;
        float tiling2 = 10.5f * density;
        const float columnScale = 0.37f;

        double rate1X = 0.005 * density;
        double rate1Y = 0.002 * density;
        double rate2X = -0.0035 * density;
        double rate2Y = 0.0025 * density;

        double t = MyCommon.FrameTime.Seconds * config.AnimationSpeed;
        float Frac(double v) => (float)(v - Math.Floor(v));
        float coverage = MathHelper.Clamp(config.Coverage, 0f, 1f);
        float patchThreshold = 0.9f - 0.8f * coverage;

        return new[]
        {
            centerRel.X, centerRel.Y, centerRel.Z, snap.InnerRadius,
            pole.X, pole.Y, pole.Z, snap.OuterRadius,
            tangent1.X, tangent1.Y, tangent1.Z, sinLo,
            sinHi, feather, tiling1, tiling2,
            Frac(t * rate1X), Frac(t * rate1Y), Frac(t * rate2X), Frac(t * rate2Y),
            Frac(t * rate1X * columnScale), Frac(t * rate1Y * columnScale), tiling1 * columnScale,
            config.Intensity * snap.DensityFactor,
            Math.Min(config.StepCount, Config.MaxRaymarchSteps), fadeFactor, patchThreshold, config.GroundLight,
            Frac(t * 0.0016), Frac(t * -0.0007), Frac(t * -0.0011), Frac(t * 0.0009),
            // Uniform8.x = curtain contrast (original GroundParams.y).
            Math.Max(config.Contrast, 1f), 0f, 0f, 0f,
        };
    }
}
