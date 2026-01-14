using UnityEngine;
using UnityEngine.XR;

/// <summary>
/// PCO GPU Monitor - Converts GPU timing into μ_gpu
/// Predictive load sensing that responds BEFORE frame drops
/// Quest-safe, uses only reliable XR stats
/// </summary>
public class PCOGpuMonitor : MonoBehaviour
{
    [Header("Frame Budget")]
    [Tooltip("Safety margin multiplier (1.0 = full budget, 0.8 = conservative)")]
    [Range(0.5f, 1.5f)]
    public float safetyMargin = 1.0f;
    
    [Header("Smoothing")]
    [Tooltip("Low-pass filter coefficient (lower = smoother)")]
    [Range(0.01f, 0.5f)]
    public float smoothing = 0.1f;
    
    [Header("Output")]
    [Tooltip("Normalized GPU load [0,1]")]
    [Range(0f, 1f)]
    public float muGpu = 0f;
    
    // Internal state
    private float frameBudgetMs;
    private float muGpuSmoothed;
    private float refreshRate;
    
    // Statistics tracking
    private float peakGpuTime;
    private float avgGpuTime;
    private int sampleCount;
    private const int RESET_INTERVAL = 300; // Reset stats every ~5 seconds at 60Hz

    void Start()
    {
        InitializeFrameBudget();
    }

    void InitializeFrameBudget()
    {
        // Get device refresh rate
        refreshRate = XRDevice.refreshRate;
        
        // Fallback to 72Hz if unavailable (Quest 2 default)
        if (refreshRate <= 0f)
        {
            refreshRate = 72f;
            Debug.LogWarning("PCOGpuMonitor: XRDevice.refreshRate unavailable, defaulting to 72Hz");
        }
        
        // Calculate frame budget in milliseconds
        frameBudgetMs = (1f / refreshRate) * 1000f;
        
        Debug.Log($"PCOGpuMonitor: Target {refreshRate}Hz, budget {frameBudgetMs:F2}ms");
    }

    void Update()
    {
        UpdateGpuMu();
        UpdateStatistics();
    }

    void UpdateGpuMu()
    {
        // Try to get GPU frame time from XR stats
        if (XRStats.TryGetGPUTimeLastFrame(out float gpuMs))
        {
            // Normalize against frame budget with safety margin
            float raw = gpuMs / (frameBudgetMs * safetyMargin);
            raw = Mathf.Clamp01(raw);
            
            // Apply exponential smoothing to prevent spikes
            muGpuSmoothed = Mathf.Lerp(muGpuSmoothed, raw, smoothing);
            muGpu = muGpuSmoothed;
        }
        else
        {
            // Fallback: estimate from frame delta time
            float targetDelta = 1f / refreshRate;
            float actualDelta = Time.deltaTime;
            
            float raw = actualDelta / (targetDelta * safetyMargin);
            raw = Mathf.Clamp01(raw);
            
            muGpuSmoothed = Mathf.Lerp(muGpuSmoothed, raw, smoothing);
            muGpu = muGpuSmoothed;
        }
    }

    void UpdateStatistics()
    {
        if (XRStats.TryGetGPUTimeLastFrame(out float gpuMs))
        {
            peakGpuTime = Mathf.Max(peakGpuTime, gpuMs);
            avgGpuTime = (avgGpuTime * sampleCount + gpuMs) / (sampleCount + 1);
            sampleCount++;
            
            // Reset periodically to track recent performance
            if (sampleCount >= RESET_INTERVAL)
            {
                peakGpuTime = gpuMs;
                avgGpuTime = gpuMs;
                sampleCount = 0;
            }
        }
    }

    // Public API
    public float FrameBudgetMs => frameBudgetMs;
    public float RefreshRate => refreshRate;
    public float PeakGpuTime => peakGpuTime;
    public float AverageGpuTime => avgGpuTime;
    public float CurrentGpuTime
    {
        get
        {
            XRStats.TryGetGPUTimeLastFrame(out float gpuMs);
            return gpuMs;
        }
    }
    
    // Utility: Is GPU under stress?
    public bool IsGpuStressed(float threshold = 0.7f)
    {
        return muGpu >= threshold;
    }
    
    // Utility: Headroom percentage
    public float GpuHeadroom => Mathf.Clamp01(1f - muGpu);
}