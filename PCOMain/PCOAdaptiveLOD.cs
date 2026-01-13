using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// PCO Adaptive LOD Controller
/// Responds to GPU pressure (μ_gpu) by adjusting visual quality
/// Predictive system that prevents frame drops before they occur
/// </summary>
public class PCOAdaptiveLOD : MonoBehaviour
{
    [Header("PCO Integration")]
    public PCOGpuMonitor gpuMonitor;
    public PCOCrownManager crownManager;
    
    [Header("Particle System Control")]
    [Tooltip("All phase particle systems to manage")]
    public PhaseParticleSystem[] particleSystems;
    
    [Tooltip("Max particles when comfortable")]
    public int maxParticlesComfort = 2000;
    
    [Tooltip("Max particles when stressed")]
    public int maxParticlesStressed = 200;
    
    [Header("Shader LOD")]
    [Tooltip("Enable shader LOD control")]
    public bool controlShaderLOD = true;
    
    [Tooltip("Shader LOD when comfortable")]
    public int shaderLODComfort = 600;
    
    [Tooltip("Shader LOD when stressed")]
    public int shaderLODStressed = 200;
    
    [Header("Render Scale (Quest FFR)")]
    [Tooltip("Enable dynamic render scale")]
    public bool controlRenderScale = true;
    
    [Tooltip("Render scale when comfortable")]
    [Range(0.5f, 1.5f)]
    public float renderScaleComfort = 1.0f;
    
    [Tooltip("Render scale when stressed")]
    [Range(0.5f, 1.5f)]
    public float renderScaleStressed = 0.7f;
    
    [Header("Thresholds")]
    [Tooltip("μ_gpu threshold for starting adaptation")]
    [Range(0.5f, 0.9f)]
    public float adaptationThreshold = 0.7f;
    
    [Tooltip("Crown C6 threshold for emergency reduction")]
    [Range(0.8f, 1.0f)]
    public float emergencyThreshold = 0.9f;
    
    [Header("Response Speed")]
    [Tooltip("How quickly to reduce quality (higher = faster)")]
    [Range(0.1f, 1f)]
    public float reductionSpeed = 0.3f;
    
    [Tooltip("How quickly to restore quality (slower = smoother)")]
    [Range(0.01f, 0.3f)]
    public float restorationSpeed = 0.05f;
    
    // State tracking
    private float currentParticleBudget = 1f;
    private float currentShaderLOD = 1f;
    private float currentRenderScale = 1f;
    
    // Particle emission baselines (cached to prevent exponential decay)
    private System.Collections.Generic.Dictionary<PhaseParticleSystem, float> emissionBaselines = 
        new System.Collections.Generic.Dictionary<PhaseParticleSystem, float>();
    
    // Smoothed μ_gpu (critical damping)
    private float smoothedMuGpu;
    private const float MU_GPU_SMOOTH = 0.15f;
    
    // Render scale quantization (Quest-safe)
    private float lastRenderScale = -1f;
    private const float RENDER_SCALE_EPS = 0.03f;
    
    // Hysteresis to prevent oscillation
    private float lastAdaptationTime;
    private const float MIN_ADAPTATION_INTERVAL = 0.5f;

    void Start()
    {
        // Initialize to comfortable state
        currentParticleBudget = 1f;
        currentShaderLOD = 1f;
        currentRenderScale = 1f;
        
        // Cache baseline emission rates to prevent exponential decay
        foreach (var ps in particleSystems)
        {
            if (ps != null)
                emissionBaselines[ps] = ps.baseEmissionRate;
        }
    }

    void Update()
    {
        if (gpuMonitor == null) return;
        
        // Critically damp μ_gpu to prevent spikes
        float rawMuGpu = Mathf.Clamp01(gpuMonitor.muGpu);
        smoothedMuGpu = Mathf.Lerp(smoothedMuGpu, rawMuGpu, MU_GPU_SMOOTH);
        
        // Get current stress levels
        float c6Proximity = crownManager != null ? crownManager.CrownProximity[5] : 0f;
        
        // Determine target quality level
        float targetQuality = ComputeTargetQuality(smoothedMuGpu, c6Proximity);
        
        // Adaptive response speed (faster when reducing, slower when restoring)
        float speed = targetQuality < GetCurrentQuality() ? reductionSpeed : restorationSpeed;
        
        // Apply adaptations with hysteresis
        if (Time.time - lastAdaptationTime >= MIN_ADAPTATION_INTERVAL)
        {
            ApplyAdaptations(targetQuality, speed);
            lastAdaptationTime = Time.time;
        }
    }

    float ComputeTargetQuality(float muGpu, float c6Proximity)
    {
        // Normal operation - respond to GPU pressure
        float quality = 1f;
        
        if (muGpu > adaptationThreshold)
        {
            // Linear reduction above threshold
            float overshoot = (muGpu - adaptationThreshold) / (1f - adaptationThreshold);
            quality = 1f - overshoot;
        }
        
        // Emergency override if approaching Crown C6 limit
        if (c6Proximity > emergencyThreshold)
        {
            float emergencyFactor = (c6Proximity - emergencyThreshold) / (1f - emergencyThreshold);
            quality = Mathf.Min(quality, 1f - emergencyFactor * 0.5f);
        }
        
        return Mathf.Clamp01(quality);
    }

    float GetCurrentQuality()
    {
        // Average of all quality metrics
        return (currentParticleBudget + currentShaderLOD + currentRenderScale) / 3f;
    }

    void ApplyAdaptations(float targetQuality, float speed)
    {
        // Particle budget
        currentParticleBudget = Mathf.Lerp(currentParticleBudget, targetQuality, speed);
        UpdateParticleSystems(currentParticleBudget);
        
        // Shader LOD
        if (controlShaderLOD)
        {
            currentShaderLOD = Mathf.Lerp(currentShaderLOD, targetQuality, speed);
            UpdateShaderLOD(currentShaderLOD);
        }
        
        // Render scale (Quest FFR)
        if (controlRenderScale)
        {
            currentRenderScale = Mathf.Lerp(currentRenderScale, targetQuality, speed);
            UpdateRenderScale(currentRenderScale);
        }
    }

    void UpdateParticleSystems(float quality)
    {
        int targetMax = Mathf.RoundToInt(
            Mathf.Lerp(maxParticlesStressed, maxParticlesComfort, quality)
        );
        
        foreach (var ps in particleSystems)
        {
            if (ps == null) continue;
            
            var sys = ps.GetComponent<ParticleSystem>();
            var main = sys.main;
            main.maxParticles = targetMax;
            
            // Use cached baseline to prevent exponential decay
            if (emissionBaselines.TryGetValue(ps, out float baseRate))
            {
                ps.baseEmissionRate = Mathf.Lerp(
                    baseRate * 0.25f,
                    baseRate,
                    quality
                );
            }
        }
    }

    void UpdateShaderLOD(float quality)
    {
        int lod = Mathf.RoundToInt(
            Mathf.Lerp(shaderLODStressed, shaderLODComfort, quality)
        );
        
        Shader.globalMaximumLOD = lod;
    }

    void UpdateRenderScale(float quality)
    {
        float scale = Mathf.Lerp(renderScaleStressed, renderScaleComfort, quality);
        
        // Quantize to 0.05 steps (Quest prefers discrete values)
        scale = Mathf.Round(scale / 0.05f) * 0.05f;
        
        // Only update if change is significant (avoid stalls)
        if (Mathf.Abs(scale - lastRenderScale) < RENDER_SCALE_EPS)
            return;
        
        lastRenderScale = scale;
        
        // Quest-specific: Use XR render scale
        #if UNITY_XR_ENABLED
        UnityEngine.XR.XRSettings.eyeTextureResolutionScale = scale;
        #endif
    }

    // Public API for external systems
    public float CurrentQualityLevel => GetCurrentQuality();
    public bool IsAdapting => GetCurrentQuality() < 0.95f;
    
    public void ForceQualityLevel(float quality)
    {
        quality = Mathf.Clamp01(quality);
        currentParticleBudget = quality;
        currentShaderLOD = quality;
        currentRenderScale = quality;
        
        ApplyAdaptations(quality, 1f); // Immediate
    }
    
    public void ResetToComfort()
    {
        ForceQualityLevel(1f);
    }

    // Debug visualization
    void OnGUI()
    {
        if (!Application.isEditor || gpuMonitor == null) return;
        
        GUILayout.BeginArea(new Rect(10, 420, 300, 200));
        GUILayout.Label("=== PCO Adaptive LOD ===");
        GUILayout.Label($"μ_gpu (raw): {gpuMonitor.muGpu:F3}");
        GUILayout.Label($"μ_gpu (smooth): {smoothedMuGpu:F3}");
        GUILayout.Label($"Quality: {GetCurrentQuality():F2}");
        GUILayout.Label($"Particles: {currentParticleBudget:F2}");
        GUILayout.Label($"Shader LOD: {Shader.globalMaximumLOD}");
        GUILayout.Label($"Render Scale: {lastRenderScale:F2}");
        
        if (IsAdapting)
        {
            GUILayout.Label("⚠ ADAPTING", GUI.skin.box);
        }
        
        GUILayout.EndArea();
    }
}