using UnityEngine;

/// <summary>
/// PCO Hardware Monitor - Unified performance monitoring
/// Orchestrates GPU, CPU, Thermal, and Battery monitoring
/// Provides composite hardware stress metric
/// </summary>
public class PCOHardwareMonitor : MonoBehaviour
{
    [Header("Monitor References")]
    public PCOGpuMu gpuMonitor;
    public PCOCpuMu cpuMonitor;
    public PCOThermalMonitor thermalMonitor;
    public PCOBatteryMonitor batteryMonitor;
    
    [Header("Composite Weights")]
    [Tooltip("GPU contribution to composite stress")]
    [Range(0f, 1f)]
    public float gpuWeight = 0.4f;
    
    [Tooltip("CPU contribution to composite stress")]
    [Range(0f, 1f)]
    public float cpuWeight = 0.3f;
    
    [Tooltip("Thermal contribution to composite stress")]
    [Range(0f, 1f)]
    public float thermalWeight = 0.2f;
    
    [Tooltip("Battery contribution to composite stress")]
    [Range(0f, 1f)]
    public float batteryWeight = 0.1f;
    
    [Header("Auto-Create Monitors")]
    [Tooltip("Automatically create missing monitors")]
    public bool autoCreateMonitors = true;
    
    [Header("Outputs")]
    [Tooltip("Composite hardware stress [0,1]")]
    [Range(0f, 1f)]
    public float muHardware = 0f;
    
    [Tooltip("Primary bottleneck identifier")]
    public HardwareBottleneck primaryBottleneck = HardwareBottleneck.None;

    public enum HardwareBottleneck
    {
        None,
        GPU,
        CPU,
        Thermal,
        Battery
    }

    void Start()
    {
        if (autoCreateMonitors)
        {
            CreateMissingMonitors();
        }
    }

    void Update()
    {
        ComputeCompositeStress();
        IdentifyBottleneck();
    }

    void CreateMissingMonitors()
    {
        if (gpuMonitor == null)
        {
            gpuMonitor = gameObject.AddComponent<PCOGpuMu>();
        }
        
        if (cpuMonitor == null)
        {
            cpuMonitor = gameObject.AddComponent<PCOCpuMu>();
        }
        
        if (thermalMonitor == null)
        {
            thermalMonitor = gameObject.AddComponent<PCOThermalMonitor>();
        }
        
        if (batteryMonitor == null)
        {
            batteryMonitor = gameObject.AddComponent<PCOBatteryMonitor>();
        }
    }

    void ComputeCompositeStress()
    {
        float totalWeight = gpuWeight + cpuWeight + thermalWeight + batteryWeight;
        
        if (totalWeight <= 0f)
        {
            muHardware = 0f;
            return;
        }
        
        float weightedSum = 0f;
        
        if (gpuMonitor != null)
            weightedSum += gpuMonitor.muGpu * gpuWeight;
        
        if (cpuMonitor != null)
            weightedSum += cpuMonitor.muCpu * cpuWeight;
        
        if (thermalMonitor != null)
            weightedSum += thermalMonitor.muThermal * thermalWeight;
        
        if (batteryMonitor != null)
            weightedSum += batteryMonitor.muPower * batteryWeight;
        
        muHardware = weightedSum / totalWeight;
    }

    void IdentifyBottleneck()
    {
        float maxStress = 0f;
        primaryBottleneck = HardwareBottleneck.None;
        
        if (gpuMonitor != null && gpuMonitor.muGpu > maxStress)
        {
            maxStress = gpuMonitor.muGpu;
            primaryBottleneck = HardwareBottleneck.GPU;
        }
        
        if (cpuMonitor != null && cpuMonitor.muCpu > maxStress)
        {
            maxStress = cpuMonitor.muCpu;
            primaryBottleneck = HardwareBottleneck.CPU;
        }
        
        if (thermalMonitor != null && thermalMonitor.muThermal > maxStress)
        {
            maxStress = thermalMonitor.muThermal;
            primaryBottleneck = HardwareBottleneck.Thermal;
        }
        
        if (batteryMonitor != null && batteryMonitor.muPower > maxStress)
        {
            maxStress = batteryMonitor.muPower;
            primaryBottleneck = HardwareBottleneck.Battery;
        }
        
        // Only consider it a bottleneck if above threshold
        if (maxStress < 0.5f)
        {
            primaryBottleneck = HardwareBottleneck.None;
        }
    }

    // Public API - Individual μ values
    public float MuGpu => gpuMonitor != null ? gpuMonitor.muGpu : 0f;
    public float MuCpu => cpuMonitor != null ? cpuMonitor.muCpu : 0f;
    public float MuThermal => thermalMonitor != null ? thermalMonitor.muThermal : 0f;
    public float MuPower => batteryMonitor != null ? batteryMonitor.muPower : 0f;
    
    // Hardware state queries
    public bool IsGpuBottlenecked => primaryBottleneck == HardwareBottleneck.GPU;
    public bool IsCpuBottlenecked => primaryBottleneck == HardwareBottleneck.CPU;
    public bool IsThermalLimited => primaryBottleneck == HardwareBottleneck.Thermal;
    public bool IsBatteryConstrained => primaryBottleneck == HardwareBottleneck.Battery;
    
    public bool IsStressed(float threshold = 0.7f) => muHardware >= threshold;
    public float Headroom => Mathf.Clamp01(1f - muHardware);
    
    // Diagnostics
    public string GetDiagnosticString()
    {
        return $"Hardware μ: {muHardware:F3}\n" +
               $"  GPU: {MuGpu:F3}\n" +
               $"  CPU: {MuCpu:F3}\n" +
               $"  Thermal: {MuThermal:F3}\n" +
               $"  Battery: {MuPower:F3}\n" +
               $"Bottleneck: {primaryBottleneck}";
    }

    // Debug visualization
    void OnGUI()
    {
        if (!Application.isEditor) return;
        
        GUILayout.BeginArea(new Rect(320, 10, 300, 300));
        GUILayout.Label("=== PCO Hardware Monitor ===");
        GUILayout.Label($"Composite μ: {muHardware:F3}");
        GUILayout.Space(10);
        
        // Individual metrics with color coding
        DrawMetric("GPU", MuGpu, gpuWeight);
        DrawMetric("CPU", MuCpu, cpuWeight);
        DrawMetric("Thermal", MuThermal, thermalWeight);
        DrawMetric("Battery", MuPower, batteryWeight);
        
        GUILayout.Space(10);
        
        if (primaryBottleneck != HardwareBottleneck.None)
        {
            GUI.color = Color.yellow;
            GUILayout.Label($"⚠ Bottleneck: {primaryBottleneck}", GUI.skin.box);
            GUI.color = Color.white;
        }
        
        // Additional info
        if (thermalMonitor != null)
        {
            GUILayout.Label($"Temp: {thermalMonitor.CurrentTemperature:F1}°C");
        }
        
        if (batteryMonitor != null)
        {
            GUILayout.Label($"Battery: {batteryMonitor.BatteryPercent:F0}%");
        }
        
        GUILayout.EndArea();
    }

    void DrawMetric(string name, float value, float weight)
    {
        Color color = value < 0.5f ? Color.green : 
                     value < 0.7f ? Color.yellow : Color.red;
        
        GUI.color = color;
        GUILayout.Label($"{name}: {value:F3} (w:{weight:F2})");
        GUI.color = Color.white;
    }
}