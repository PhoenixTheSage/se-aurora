// Aurora Borealis — volumetric raymarch through a spherical shell segment over planet poles.
// Technique based on Roy Theunissen's "difference clouds" aurora (see Docs/Plan.md §1).
// Drawn by Anomaly FullscreenPassRegistry (IsolatedAdd → LBuffer).

#include <AnomalyFullscreen.hlsli>

static const float NoiseThreshold = 0.25;
static const float NoisePush = 4.0;
static const float Dither = 1.0;
static const float HeightVariation = 0.6;
static const float PatchTiling1 = 1.1;
static const float PatchTiling2 = 0.9;
static const float PatchFeather = 0.12;

#define CenterInner AnomalyPassUniform0
#define PoleOuter AnomalyPassUniform1
#define Tangent1 AnomalyPassUniform2.xyz
#define BandSinLo AnomalyPassUniform2.w
#define BandSinHi AnomalyPassUniform3.x
#define BandFeather AnomalyPassUniform3.y
#define NoiseTiling1 AnomalyPassUniform3.z
#define NoiseTiling2 AnomalyPassUniform3.w
#define ScrollOffsets AnomalyPassUniform4
#define ColumnOffset AnomalyPassUniform5.xy
#define ColumnTiling AnomalyPassUniform5.z
#define MasterIntensity AnomalyPassUniform5.w
#define StepCount AnomalyPassUniform6.x
#define FadeFactor AnomalyPassUniform6.y
#define PatchThreshold AnomalyPassUniform6.z
#define GroundLight AnomalyPassUniform6.w
#define PatchScroll AnomalyPassUniform7
#define Contrast AnomalyPassUniform8.x
#define NightOnly AnomalyPassUniform8.y
#define PlanetRadius AnomalyPassUniform8.z
#define PerlinTex AnomalyPackSrv0
#define ColorRamp AnomalyPackSrv1

// Must match Config.MaxRaymarchSteps. Transparent ConsumeWork draws this
// before Scheduler.Done publishes this-frame linear depth. Bound so a
// garbage StepCount cannot TDR.
#define AURORA_MAX_STEPS 96

float2 RaySphere(float3 origin, float3 dir, float3 center, float radius)
{
    float3 oc = origin - center;
    float b = dot(oc, dir);
    float c = dot(oc, oc) - radius * radius;
    float disc = b * b - c;
    if (disc < 0)
        return float2(-1, -1);
    float s = sqrt(disc);
    return float2(-b - s, -b + s);
}

float CurtainNoise(float2 uv1, float2 uv2)
{
    float a = PerlinTex.SampleLevel(AnomalyWrapSampler, uv1, 0).r;
    float b = PerlinTex.SampleLevel(AnomalyWrapSampler, uv2, 0).g;
    float noise = abs(a - b);
    noise = (noise - NoiseThreshold) * NoisePush + NoiseThreshold;
    return 1 - saturate(noise);
}

float BandMask(float sinLat)
{
    float feather = BandFeather;
    return smoothstep(BandSinLo - feather, BandSinLo + feather, sinLat)
         * (1 - smoothstep(BandSinHi - feather, BandSinHi + feather, sinLat));
}

float3 UnpackGbufferNormal(float2 enc)
{
    float2 fenc = enc * 4 - 2;
    float f = dot(fenc, fenc);
    if (f >= 3.99)
        return float3(0, 0, 1);
    float g = sqrt(saturate(1 - f / 4));
    float3 n;
    n.xy = fenc * g;
    n.z = 1 - f / 2;
    return n;
}

void WriteColor(inout float4 output, float3 color, float hitT)
{
    if (!all(isfinite(color)) || !isfinite(hitT))
    {
        output = 0;
        return;
    }
    output = float4(color, max(hitT, 0));
}

float PatchMask(float2 uvBase)
{
    float a = PerlinTex.SampleLevel(AnomalyWrapSampler, uvBase * PatchTiling1 + PatchScroll.xy, 0).r;
    float b = PerlinTex.SampleLevel(AnomalyWrapSampler, uvBase * PatchTiling2 + PatchScroll.zw, 0).g;
    return smoothstep(PatchThreshold - PatchFeather, PatchThreshold + PatchFeather, (a + b) * 0.5);
}

float Hash21(float2 p)
{
    p = frac(p * float2(123.34, 456.21));
    p += dot(p, p + 45.32);
    return frac(p.x * p.y);
}

// Anomaly extras store InvViewAt0 rows (VRage TransformNormal). Keen's
// Frame CB uploads the transpose for mul(M, v). Expanding the rows here
// stays independent of pack_matrix; mul(M, v) used the transpose and
// made the shell track camera yaw/pitch.
float3 ViewToWorld(float3 view)
{
    return AnomalyCameraToWorld[0].xyz * view.x
         + AnomalyCameraToWorld[1].xyz * view.y
         + AnomalyCameraToWorld[2].xyz * view.z;
}

// NightOnly used to be camera zenith × sun only. A terminator view still
// IsolatedAdd'd the near-side shell over the sunlit disk — SDR LBuffer
// read that as a fullscreen lift, not curtains. Occult per sample.
float NightAt(float3 posCamRel, float3 center, float radius)
{
    if (NightOnly < 0.5)
        return 1.0;
    return 1.0 - AnomalySunVisibility(posCamRel, center, max(radius, 1.0));
}

void __pixel_shader(float4 pos : SV_Position, float2 uv : TEXCOORD0, out float4 output : SV_Target0)
{
    float intensity = MasterIntensity * FadeFactor;
    if (intensity <= 0)
        discard;

    float2 ndc = float2(uv.x * 2 - 1, 1 - uv.y * 2);
    float2 scale = max(AnomalyProjScale, 1e-6);
    float3 viewRay = float3(ndc.x / scale.x, ndc.y / scale.y, -1);
    float3 rayDir = normalize(ViewToWorld(viewRay));

    float innerR = CenterInner.w;
    float outerR = PoleOuter.w;
    float3 center = CenterInner.xyz;
    float3 pole = PoleOuter.xyz;
    float3 tangent2 = cross(pole, Tangent1);
    float planetR = PlanetRadius > 1.0 ? PlanetRadius : innerR;

    float2 outerT = RaySphere(0, rayDir, center, outerR);
    if (outerT.y <= 0)
        discard;
    float tMin = max(outerT.x, 0);
    float tMax = outerT.y;

    float viewZ = AnomalyLinearDepth.SampleLevel(AnomalyPointSampler, uv, 0);
    float sceneDist = 0;
    bool hasScene = viewZ > 1e-3 && isfinite(viewZ);
    if (hasScene)
    {
        sceneDist = length(viewRay) * viewZ;
        tMax = min(tMax, sceneDist);
    }

    float3 ground = 0;
    if (GroundLight > 0 && hasScene)
    {
        float3 q = rayDir * sceneDist - center;
        float rq = length(q);
        if (rq >= 1e-5 && rq < innerR)
        {
            float3 up = q / rq;
            float glow = BandMask(abs(dot(up, pole)));
            if (glow > 0)
            {
                float2 uvGround = float2(dot(up, Tangent1), dot(up, tangent2));
                // The curtain pattern skips the contrast exponent: the ground sees the
                // whole sky dome, so its light is softer than the curtains themselves.
                glow *= PatchMask(uvGround);
                glow *= CurtainNoise(uvGround * NoiseTiling1 + ScrollOffsets.xy,
                                     uvGround * NoiseTiling2 + ScrollOffsets.zw);
            }
            if (glow > 0)
            {
                float3 albedo = AnomalyGBuffer0.SampleLevel(AnomalyPointSampler, uv, 0).rgb
                    * (1 - AnomalyGBuffer2.SampleLevel(AnomalyPointSampler, uv, 0).r);
                float3 normal = normalize(ViewToWorld(UnpackGbufferNormal(
                    AnomalyGBuffer1.SampleLevel(AnomalyPointSampler, uv, 0).xy)));
                float skyVisibility = saturate(dot(normal, up) * 0.5 + 0.5);
                float3 glowColor = ColorRamp.SampleLevel(AnomalyLinearSampler, float2(0.15, 0.5), 0).rgb;
                float night = NightAt(rayDir * sceneDist, center, planetR);
                ground = albedo * glowColor * (glow * skyVisibility * GroundLight * night);
            }
        }
    }

    if (tMax <= tMin)
    {
        WriteColor(output, ground * intensity, hasScene ? sceneDist : 0);
        return;
    }

    float seg0Start = tMin, seg0End = tMax;
    float seg1Start = 0, seg1End = 0;

    float2 innerT = RaySphere(0, rayDir, center, innerR);
    if (innerT.y > tMin && innerT.x < tMax)
    {
        seg0End = clamp(innerT.x, tMin, tMax);
        seg1Start = clamp(innerT.y, tMin, tMax);
        seg1End = tMax;
    }

    float len0 = max(seg0End - seg0Start, 0);
    float len1 = max(seg1End - seg1Start, 0);
    float marchLength = len0 + len1;
    if (marchLength <= 0)
    {
        WriteColor(output, ground * intensity, hasScene ? sceneDist : 0);
        return;
    }

    float shellThickness = max(outerR - innerR, 1e-5);
    // Grazing chords through a planet-sized sphere can be tens of km.
    // Cap so AURORA_MAX_STEPS samples cannot walk an unbounded interval.
    marchLength = min(marchLength, shellThickness * 8.0);
    if (len0 > marchLength)
    {
        len0 = marchLength;
        len1 = 0;
    }
    else
        len1 = min(len1, marchLength - len0);

    float stepBudget = (float)clamp((int)StepCount, 0, AURORA_MAX_STEPS);
    // Per-ray tMin stays large on grazing chords while the camera sits in
    // the shell. Camera-to-shell is uniform so a close spectator cheapens
    // every pixel. AnomalyMarchSteps owns SafetyScale (do not floor it).
    float camToVolume = abs(length(center) - outerR);
    int steps = AnomalyMarchSteps(stepBudget, 4, AURORA_MAX_STEPS,
        camToVolume, 0.0, max(shellThickness * 2.0, 1e-5));
    if (steps <= 0)
    {
        WriteColor(output, ground * intensity, hasScene ? sceneDist : 0);
        return;
    }
    float stepLen = marchLength / (float)steps;
    // Cranley-Patterson along the ray so a stable 4-step slam still
    // covers the interval across frames (DLSS / packed hit t). Do not
    // hash the frame into the pixel seed — that strobes when steps drop.
    float jitter = frac(Hash21(pos.xy) + float(AnomalyLightingFrameIndex & 1023u) * 0.61803398875)
                 * Dither;
    float3 accum = 0;
    float hitT = 0;
    float hitWeight = 0;

    [loop]
    for (int i = 0; i < AURORA_MAX_STEPS; i++)
    {
        if (i >= steps)
            break;
        float s = (i + jitter) * stepLen;
        float t = (s < len0) ? (seg0Start + s) : (seg1Start + (s - len0));

        float3 p = rayDir * t - center;
        float r = length(p);
        if (r < 1e-5)
            continue;
        float3 dir = p / r;

        float bandMask = BandMask(abs(dot(dir, pole)));
        if (bandMask <= 0)
            continue;

        float night = NightAt(rayDir * t, center, planetR);
        if (night <= 0)
            continue;

        float2 uvBase = float2(dot(dir, Tangent1), dot(dir, tangent2));
        float2 uv1 = uvBase * NoiseTiling1 + ScrollOffsets.xy;
        float2 uv2 = uvBase * NoiseTiling2 + ScrollOffsets.zw;

        float patchMask = PatchMask(uvBase);
        if (patchMask <= 0)
            continue;

        float curtain = CurtainNoise(uv1, uv2);
        if (curtain <= 0)
            continue;

        float4 columnNoise = PerlinTex.SampleLevel(AnomalyWrapSampler, uvBase * ColumnTiling + ColumnOffset, 0);
        float columnHeight = lerp(1 - HeightVariation, 1, columnNoise.b);
        float columnShift = columnNoise.a * HeightVariation * 0.5;

        float h = (r - innerR) / shellThickness;
        float hRemapped = (h - columnShift) / max(columnHeight, 1e-3);
        if (hRemapped < 0 || hRemapped > 1)
            continue;

        float4 ramp = ColorRamp.SampleLevel(AnomalyLinearSampler, float2(hRemapped, 0.5), 0);
        // The contrast exponent leaves a fully lit curtain at 1 and pushes everything
        // below it down, so raising it darkens the haze between the curtains without
        // dimming their cores. Brightness is then the intensity's job alone.
        float emission = pow(curtain, Contrast);
        float w = ramp.a * emission * bandMask * patchMask * night;
        accum += ramp.rgb * w;
        hitT += t * w;
        hitWeight += w;
    }

    if (hitWeight > 1e-6)
        hitT /= hitWeight;
    else if (hasScene)
        hitT = sceneDist;

    // IsolatedAdd is emission. 1-exp(-optical) saturates in-band pixels to
    // ~Intensity and the merge lifts LBuffer (a fullscreen brighten on SDR).
    // Stay optically thin so curtain contrast survives; Reinhard shoulders
    // grazing chords (marchLength is capped at 8× thickness).
    float3 optical = accum * (stepLen / shellThickness);
    float3 color = intensity * optical;
    color = color / (1.0 + color);
    WriteColor(output, color + ground * intensity, hitT);
}
