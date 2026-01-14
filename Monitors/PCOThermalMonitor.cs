using UnityEngine;

/// <summary>
/// PCO Thermal Monitor - Converts device temperature to μ_thermal
/// Anticipates thermal throttling before it occurs
/// Quest-specific implementation
/// </summary>
public class PCOThermalMonitor : MonoBehaviour
{
    [Header("Thermal Thresholds (°C)")]
    [Tooltip("Temperature considered comfortable")]
    [Range(25f, 40f)]
    public float comfortTemp = 35f;
    
    [Tooltip("Temperature at throttle threshold")]
    [Range(40f, 55f)]
    public float throttleTemp = 45f;
    
    [Header("Smoothing")]
    [Tooltip("Low-pass filter for temperature readings")]
    [Range(0.01f, 0.3f)]
    public float smoothing = 0.05f;
    
    [Header("Output")]
    [Tooltip("Normalized thermal stress [0,1]")]
    [Range(0f, 1f)]
    public float muThermal = 0f;
    
    // Internal state
    private float smoothedTemp;
    private float currentTemp;
    
    // Thermal trend tracking (predictive)
    private float lastTemp;
    private float thermalVelocity;
    private const int TREND_SAMPLES = 30; // ~0.5s at 60fps
    private int sampleCount;

    void Start()
    {
        // Initialize with ambient estimate
        smoothedTemp = comfortTemp;
        currentTemp = comfortTemp;
        lastTemp = comfortTemp;
    }

    void Update()
    {
        UpdateTemperature();
        UpdateThermalVelocity();
        ComputeMuThermal();
    }

    void UpdateTemperature()
    {
        // Quest: Use battery temperature as proxy for device thermal state
        // Android BatteryManager provides temperature in tenths of degrees C
        currentTemp = GetDeviceTemperature();
        
        // Apply exponential smoothing
        smoothedTemp = Mathf.Lerp(smoothedTemp, currentTemp, smoothing);
    }

    float GetDeviceTemperature()
    {
        // Platform-specific temperature reading
        #if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
            using (AndroidJavaObject context = activity.Call<AndroidJavaObject>("getApplicationContext"))
            {
                // Get battery manager
                using (AndroidJavaObject batteryManager = context.Call<AndroidJavaObject>("getSystemService", "batterymanager"))
                {
                    // BATTERY_PROPERTY_TEMPERATURE returns tenths of degrees C
                    int tempTenths = batteryManager.Call<int>("getIntProperty", 4); // 4 = BATTERY_PROPERTY_TEMPERATURE
                    return tempTenths / 10f;
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"PCOThermalMonitor: Could not read temperature: {e.Message}");
            return comfortTemp; // Fallback
        }
        #else
        // Editor/non-Android: simulate thermal rise based on frame time
        float simulatedRise = Time.deltaTime * 0.1f; // Slow simulated heating
        return Mathf.Min(smoothedTemp + simulatedRise, throttleTemp);
        #endif
    }

    void UpdateThermalVelocity()
    {
        // Track temperature change rate (predictive signal)
        thermalVelocity = (smoothedTemp - lastTemp) / Time.deltaTime;
        lastTemp = smoothedTemp;
        
        sampleCount++;
        if (sampleCount >= TREND_SAMPLES)
        {
            sampleCount = 0;
        }
    }

    void ComputeMuThermal()
    {
        // Base thermal stress from current temperature
        float tempStress = Mathf.InverseLerp(comfortTemp, throttleTemp, smoothedTemp);
        
        // Add predictive component from thermal velocity
        // If heating rapidly, increase μ preemptively
        float velocityFactor = Mathf.Clamp01(thermalVelocity / 2f); // 2°C/s = max concern
        
        muThermal = Mathf.Clamp01(tempStress + velocityFactor * 0.3f);
    }

    // Public API
    public float CurrentTemperature => smoothedTemp;
    public float ThermalVelocity => thermalVelocity;
    public float HeadroomPercent => Mathf.Clamp01(1f - muThermal) * 100f;
    
    public bool IsOverheating(float threshold = 0.7f)
    {
        return muThermal >= threshold;
    }
}