using UnityEngine;
using UnityEngine.XR;

/// <summary>
/// PCO CPU Monitor - Converts CPU timing into μ_cpu
/// Monitors main thread frame time as proxy for CPU load
/// </summary>
public class PCOCpuMu : MonoBehaviour
{
    [Header("CPU Load Configuration")]
    [Tooltip("Safety margin multiplier (1.0 = full budget, 0.8 = conservative)")]
    [Range(0.5f, 1.5f)]
    public float safetyMargin = 1.0f;

    [Tooltip("Low-pass filter coefficient (lower = smoother)")]
    [Range(0.01f, 0.3f)]
    public float smoothing = 0.15f;

    [Header("Output")]
    [Tooltip("Normalized CPU load [0,1]")]
    public float muCpu { get; private set; }

    private float targetFrameMs;
    private float smoothed;

    void Start()
    {
        float hz = XRDevice.refreshRate;
        if (hz <= 0f) hz = 72f;
        targetFrameMs = (1f / hz) * 1000f;
        
        Debug.Log($"PCOCpuMu: Target frame time {targetFrameMs:F2}ms");
    }

    void Update()
    {
        // CPU frame time in milliseconds
        float cpuMs = Time.deltaTime * 1000f;
        
        // Normalize against frame budget with safety margin
        float raw = cpuMs / (targetFrameMs * safetyMargin);
        raw = Mathf.Clamp01(raw);

        // Apply exponential smoothing
        smoothed = Mathf.Lerp(smoothed, raw, smoothing);
        muCpu = smoothed;
    }
    
    // Public API
    public float TargetFrameMs => targetFrameMs;
    public float CurrentFrameMs => Time.deltaTime * 1000f;
    public float CpuHeadroom => Mathf.Clamp01(1f - muCpu);
    
    public bool IsCpuStressed(float threshold = 0.7f)
    {
        return muCpu >= threshold;
    }
}