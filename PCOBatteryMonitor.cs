using UnityEngine;

/// <summary>
/// PCO Battery Monitor - Converts battery state to μ_power
/// Reduces computational load as battery drains to extend session
/// </summary>
public class PCOBatteryMonitor : MonoBehaviour
{
    [Header("Battery Thresholds")]
    [Tooltip("Battery % considered comfortable")]
    [Range(20f, 100f)]
    public float comfortLevel = 50f;
    
    [Tooltip("Battery % requiring power conservation")]
    [Range(5f, 30f)]
    public float criticalLevel = 15f;
    
    [Header("Discharge Rate Sensitivity")]
    [Tooltip("How much discharge rate affects μ")]
    [Range(0f, 1f)]
    public float drainSensitivity = 0.3f;
    
    [Header("Smoothing")]
    [Tooltip("Low-pass filter for battery readings")]
    [Range(0.01f, 0.5f)]
    public float smoothing = 0.1f;
    
    [Header("Output")]
    [Tooltip("Normalized power stress [0,1]")]
    [Range(0f, 1f)]
    public float muPower = 0f;
    
    // Internal state
    private float smoothedBatteryLevel;
    private float lastBatteryLevel;
    private float dischargeRate; // %/second
    private float timeSinceLastUpdate;
    private const float UPDATE_INTERVAL = 1f; // Check battery every second

    void Start()
    {
        // Initialize with current battery level
        smoothedBatteryLevel = SystemInfo.batteryLevel * 100f;
        lastBatteryLevel = smoothedBatteryLevel;
    }

    void Update()
    {
        timeSinceLastUpdate += Time.deltaTime;
        
        if (timeSinceLastUpdate >= UPDATE_INTERVAL)
        {
            UpdateBatteryState();
            ComputeMuPower();
            timeSinceLastUpdate = 0f;
        }
    }

    void UpdateBatteryState()
    {
        // Get current battery level (Unity reports as 0-1)
        float currentLevel = SystemInfo.batteryLevel * 100f;
        
        // Handle edge cases
        if (currentLevel < 0f)
        {
            // Device doesn't report battery or is plugged in
            currentLevel = 100f;
        }
        
        // Apply smoothing
        smoothedBatteryLevel = Mathf.Lerp(smoothedBatteryLevel, currentLevel, smoothing);
        
        // Calculate discharge rate
        float delta = lastBatteryLevel - smoothedBatteryLevel;
        dischargeRate = delta / UPDATE_INTERVAL;
        
        lastBatteryLevel = smoothedBatteryLevel;
    }

    void ComputeMuPower()
    {
        // Base power stress from battery level
        float levelStress;
        
        if (smoothedBatteryLevel > comfortLevel)
        {
            // Comfortable - no stress
            levelStress = 0f;
        }
        else if (smoothedBatteryLevel > criticalLevel)
        {
            // Declining - linear increase in stress
            levelStress = Mathf.InverseLerp(comfortLevel, criticalLevel, smoothedBatteryLevel);
        }
        else
        {
            // Critical - high stress
            levelStress = Mathf.InverseLerp(criticalLevel, 0f, smoothedBatteryLevel);
            levelStress = 0.7f + levelStress * 0.3f; // Map to [0.7, 1.0]
        }
        
        // Add discharge rate component (faster drain = more stress)
        // Normal discharge ~10%/hour = ~0.0028%/s
        float drainStress = Mathf.Clamp01(dischargeRate / 0.01f); // 0.01%/s = concern threshold
        
        // Combine level and drain
        muPower = Mathf.Clamp01(levelStress + drainStress * drainSensitivity);
        
        // Special case: Plugged in (charging)
        if (SystemInfo.batteryStatus == BatteryStatus.Charging || 
            SystemInfo.batteryStatus == BatteryStatus.Full)
        {
            muPower = 0f; // No power stress when charging
        }
    }

    // Public API
    public float BatteryPercent => smoothedBatteryLevel;
    public float DischargeRate => dischargeRate;
    public float EstimatedMinutesRemaining
    {
        get
        {
            if (dischargeRate <= 0f) return float.MaxValue;
            return (smoothedBatteryLevel / dischargeRate) / 60f;
        }
    }
    
    public bool IsCharging => SystemInfo.batteryStatus == BatteryStatus.Charging;
    public bool IsCritical => smoothedBatteryLevel <= criticalLevel;
}