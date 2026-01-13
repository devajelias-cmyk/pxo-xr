using UnityEngine;

/// <summary>
/// PCO Thermal Monitor (Drift-Based) - Infers thermal stress from hardware drift
/// Watches for CPU/GPU performance degradation as thermal indicator
/// Alternative to direct temperature sensing
/// </summary>
public class PCOThermalMu : MonoBehaviour
{
    [Header("Drift Detection")]
    [Tooltip("Rate at which thermal stress rises when drift detected")]
    [Range(0.001f, 0.05f)]
    public float riseRate = 0.01f;

    [Tooltip("Rate at which thermal stress decays when stable")]
    [Range(0.001f, 0.05f)]
    public float decayRate = 0.005f;
    
    [Tooltip("Drift threshold to trigger thermal rise")]
    [Range(0.05f, 0.3f)]
    public float driftThreshold = 0.15f;

    [Header("Monitor References")]
    public PCOCpuMu cpuMu;
    public PCOGpuMu gpuMu;

    [Header("Output")]
    [Tooltip("Inferred thermal stress [0,1]")]
    public float muThermal { get; private set; }

    private float baselineCpu;
    private float baselineGpu;
    private bool baselineSet;

    void Update()
    {
        // Establish baseline on first frame
        if (!baselineSet)
        {
            if (cpuMu != null && gpuMu != null)
            {
                baselineCpu = cpuMu.muCpu;
                baselineGpu = gpuMu.muGpu;
                baselineSet = true;
            }
            return;
        }

        // Check for performance drift (thermal throttling indicator)
        float drift = 0f;
        
        if (cpuMu != null)
            drift += (cpuMu.muCpu - baselineCpu);
        
        if (gpuMu != null)
            drift += (gpuMu.muGpu - baselineGpu);

        // If hardware performance is degrading, infer thermal stress
        if (drift > driftThreshold)
        {
            muThermal = Mathf.Clamp01(muThermal + riseRate * Time.deltaTime);
        }
        else
        {
            // Performance stable - thermal stress decays
            muThermal = Mathf.Clamp01(muThermal - decayRate * Time.deltaTime);
        }
    }
    
    // Public API
    public float CurrentDrift
    {
        get
        {
            if (!baselineSet || cpuMu == null || gpuMu == null)
                return 0f;
            
            return (cpuMu.muCpu - baselineCpu) + (gpuMu.muGpu - baselineGpu);
        }
    }
    
    public bool IsThrottling => CurrentDrift > driftThreshold;
    public float ThermalHeadroom => Mathf.Clamp01(1f - muThermal);
    
    /// <summary>
    /// Reset baseline to current performance level
    /// Useful after loading screens or major scene changes
    /// </summary>
    public void ResetBaseline()
    {
        if (cpuMu != null && gpuMu != null)
        {
            baselineCpu = cpuMu.muCpu;
            baselineGpu = gpuMu.muGpu;
            baselineSet = true;
            Debug.Log("PCOThermalMu: Baseline reset");
        }
    }
}