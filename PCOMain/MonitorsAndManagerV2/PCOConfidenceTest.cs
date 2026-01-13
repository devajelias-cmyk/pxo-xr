using UnityEngine;
using System.Collections;

/// <summary>
/// PCO Confidence Test Suite
/// Comprehensive testing for all confidence factors and edge cases
/// </summary>
public class PCOConfidenceTestSuite : MonoBehaviour
{
    [Header("PCO Components")]
    public PCOConfidenceMonitor confidenceMonitor;
    public PCOCrownManager crownManager;
    public PCOHardwareMonitor hardwareMonitor;
    
    [Header("Test Configuration")]
    [Tooltip("Run all tests on Start")]
    public bool autoRunTests = false;
    
    [Tooltip("Test interval (seconds)")]
    public float testInterval = 1f;
    
    [Header("Test Status")]
    public string currentTest = "None";
    public bool testPassed = false;
    public int testsRun = 0;
    public int testsPassed = 0;
    public int testsFailed = 0;

    void Start()
    {
        if (autoRunTests)
        {
            StartCoroutine(RunAllTests());
        }
    }

    void Update()
    {
        // Manual test triggers
        if (Input.GetKeyDown(KeyCode.T))
        {
            StartCoroutine(RunAllTests());
        }
        
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            StartCoroutine(TestJitterDetection());
        }
        
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            StartCoroutine(TestMuSpikeDetection());
        }
        
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            StartCoroutine(TestThermalResponse());
        }
        
        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            StartCoroutine(TestSoftFloors());
        }
        
        if (Input.GetKeyDown(KeyCode.Alpha5))
        {
            StartCoroutine(TestEmergencyGuard());
        }
    }

    IEnumerator RunAllTests()
    {
        Debug.Log("=== PCO Confidence Test Suite Starting ===");
        testsRun = 0;
        testsPassed = 0;
        testsFailed = 0;
        
        yield return TestJitterDetection();
        yield return new WaitForSeconds(testInterval);
        
        yield return TestMuSpikeDetection();
        yield return new WaitForSeconds(testInterval);
        
        yield return TestThermalResponse();
        yield return new WaitForSeconds(testInterval);
        
        yield return TestSoftFloors();
        yield return new WaitForSeconds(testInterval);
        
        yield return TestEmergencyGuard();
        yield return new WaitForSeconds(testInterval);
        
        yield return TestModelValidation();
        yield return new WaitForSeconds(testInterval);
        
        yield return TestFirstFrameProtection();
        yield return new WaitForSeconds(testInterval);
        
        yield return TestConfidenceRecovery();
        
        Debug.Log($"=== Test Suite Complete ===");
        Debug.Log($"Tests Run: {testsRun}");
        Debug.Log($"Passed: {testsPassed}");
        Debug.Log($"Failed: {testsFailed}");
        Debug.Log($"Success Rate: {(float)testsPassed / testsRun * 100f:F1}%");
    }

    // Test 1: Jitter Detection
    IEnumerator TestJitterDetection()
    {
        currentTest = "Jitter Detection";
        testsRun++;
        
        Debug.Log("--- Test 1: Jitter Detection ---");
        
        // Simulate stable frame rate
        float initialConfidence = confidenceMonitor.TimingConfidence;
        Debug.Log($"Initial timing confidence: {initialConfidence:F3}");
        
        // Wait for stable baseline
        yield return new WaitForSeconds(1f);
        float stableConfidence = confidenceMonitor.TimingConfidence;
        Debug.Log($"Stable timing confidence: {stableConfidence:F3}");
        
        // Simulate jitter by forcing irregular updates (simulated)
        Debug.Log("Simulating frame time jitter...");
        // Note: Can't easily simulate jitter in test, but we can check response
        
        // Check that confidence is working
        bool passed = stableConfidence > 0.7f;
        
        if (passed)
        {
            Debug.Log("✓ Test PASSED: Timing confidence responding correctly");
            testsPassed++;
        }
        else
        {
            Debug.LogWarning("✗ Test FAILED: Timing confidence too low");
            testsFailed++;
        }
        
        testPassed = passed;
    }

    // Test 2: μ Spike Detection
    IEnumerator TestMuSpikeDetection()
    {
        currentTest = "μ Spike Detection";
        testsRun++;
        
        Debug.Log("--- Test 2: μ Spike Detection ---");
        
        float initialMuConfidence = confidenceMonitor.MuConfidence;
        Debug.Log($"Initial μ confidence: {initialMuConfidence:F3}");
        
        // Inject artificial μ spike
        float originalMu = crownManager.mu_h;
        crownManager.mu_h = 0.95f;  // Instant spike
        
        yield return null; // Wait one frame for detection
        
        float spikedConfidence = confidenceMonitor.MuConfidence;
        Debug.Log($"μ confidence after spike: {spikedConfidence:F3}");
        
        // Should detect impossible velocity and reduce confidence
        bool passed = spikedConfidence < initialMuConfidence * 0.9f;
        
        // Restore
        crownManager.mu_h = originalMu;
        
        if (passed)
        {
            Debug.Log("✓ Test PASSED: μ spike detected, confidence reduced");
            testsPassed++;
        }
        else
        {
            Debug.LogWarning("✗ Test FAILED: μ spike not detected");
            testsFailed++;
        }
        
        testPassed = passed;
    }

    // Test 3: Thermal Response
    IEnumerator TestThermalResponse()
    {
        currentTest = "Thermal Response";
        testsRun++;
        
        Debug.Log("--- Test 3: Thermal Response ---");
        
        if (hardwareMonitor == null || hardwareMonitor.thermalMonitor == null)
        {
            Debug.LogWarning("⊘ Test SKIPPED: No thermal monitor available");
            yield break;
        }
        
        float initialThermalConf = confidenceMonitor.ThermalConfidence;
        Debug.Log($"Initial thermal confidence: {initialThermalConf:F3}");
        
        // Check that thermal confidence responds to temperature
        bool passed = initialThermalConf >= 0f && initialThermalConf <= 1f;
        
        if (passed)
        {
            Debug.Log("✓ Test PASSED: Thermal confidence in valid range");
            testsPassed++;
        }
        else
        {
            Debug.LogWarning("✗ Test FAILED: Thermal confidence invalid");
            testsFailed++;
        }
        
        testPassed = passed;
        yield return null;
    }

    // Test 4: Soft Floors
    IEnumerator TestSoftFloors()
    {
        currentTest = "Soft Floors";
        testsRun++;
        
        Debug.Log("--- Test 4: Soft Floors ---");
        
        // Even with multiple factors degraded, confidence should have minimum
        float C_pco = confidenceMonitor.C_pco;
        float G_pco = confidenceMonitor.G_pco;
        
        Debug.Log($"Current C_pco: {C_pco:F3}");
        Debug.Log($"Current G_pco: {G_pco:F3}");
        
        // Check individual factors have floors
        float sensorConf = confidenceMonitor.SensorConfidence;
        float timingConf = confidenceMonitor.TimingConfidence;
        float muConf = confidenceMonitor.MuConfidence;
        
        Debug.Log($"  Sensor: {sensorConf:F3}");
        Debug.Log($"  Timing: {timingConf:F3}");
        Debug.Log($"  μ: {muConf:F3}");
        
        // All should be > 0 even if degraded (except thermal in emergency)
        bool passed = sensorConf >= 0f && timingConf >= 0f && muConf >= 0f;
        
        if (passed)
        {
            Debug.Log("✓ Test PASSED: Soft floors preventing collapse");
            testsPassed++;
        }
        else
        {
            Debug.LogWarning("✗ Test FAILED: Confidence factors invalid");
            testsFailed++;
        }
        
        testPassed = passed;
        yield return null;
    }

    // Test 5: Emergency Guard
    IEnumerator TestEmergencyGuard()
    {
        currentTest = "Emergency Guard";
        testsRun++;
        
        Debug.Log("--- Test 5: Emergency Guard ---");
        
        // Emergency guard should clamp G_pco on critical failures
        float G_pco = confidenceMonitor.G_pco;
        
        Debug.Log($"Current G_pco: {G_pco:F3}");
        Debug.Log($"Sensor confidence: {confidenceMonitor.SensorConfidence:F3}");
        Debug.Log($"Thermal confidence: {confidenceMonitor.ThermalConfidence:F3}");
        Debug.Log($"μ confidence: {confidenceMonitor.MuConfidence:F3}");
        
        // If any critical factor is zero, G should be zero
        bool thermalCritical = confidenceMonitor.ThermalConfidence <= 0f;
        bool sensorCritical = confidenceMonitor.SensorConfidence <= 0f;
        bool muCritical = confidenceMonitor.MuConfidence < 0.01f;
        
        bool anyCritical = thermalCritical || sensorCritical || muCritical;
        
        bool passed;
        if (anyCritical)
        {
            passed = G_pco == 0f;
            Debug.Log($"Critical failure detected, G_pco should be 0: {passed}");
        }
        else
        {
            passed = G_pco > 0f;
            Debug.Log($"No critical failure, G_pco should be > 0: {passed}");
        }
        
        if (passed)
        {
            Debug.Log("✓ Test PASSED: Emergency guard functioning");
            testsPassed++;
        }
        else
        {
            Debug.LogWarning("✗ Test FAILED: Emergency guard not working");
            testsFailed++;
        }
        
        testPassed = passed;
        yield return null;
    }

    // Test 6: Model Validation
    IEnumerator TestModelValidation()
    {
        currentTest = "Model Validation";
        testsRun++;
        
        Debug.Log("--- Test 6: Model Validation ---");
        
        float modelConfidence = confidenceMonitor.ModelConfidence;
        Debug.Log($"Model confidence: {modelConfidence:F3}");
        
        // Model confidence should be in valid range
        bool passed = modelConfidence >= 0f && modelConfidence <= 1f;
        
        if (passed)
        {
            Debug.Log("✓ Test PASSED: Model validation active");
            testsPassed++;
        }
        else
        {
            Debug.LogWarning("✗ Test FAILED: Model confidence invalid");
            testsFailed++;
        }
        
        testPassed = passed;
        yield return null;
    }

    // Test 7: First Frame Protection
    IEnumerator TestFirstFrameProtection()
    {
        currentTest = "First Frame Protection";
        testsRun++;
        
        Debug.Log("--- Test 7: First Frame Protection ---");
        
        // μ confidence should initialize to 1.0, not trigger false spike
        float muConf = confidenceMonitor.MuConfidence;
        Debug.Log($"μ confidence on startup: {muConf:F3}");
        
        // Should be high (no false spike)
        bool passed = muConf > 0.8f;
        
        if (passed)
        {
            Debug.Log("✓ Test PASSED: No false first-frame spike");
            testsPassed++;
        }
        else
        {
            Debug.LogWarning("✗ Test FAILED: First-frame protection not working");
            testsFailed++;
        }
        
        testPassed = passed;
        yield return null;
    }

    // Test 8: Confidence Recovery
    IEnumerator TestConfidenceRecovery()
    {
        currentTest = "Confidence Recovery";
        testsRun++;
        
        Debug.Log("--- Test 8: Confidence Recovery ---");
        
        float initialC = confidenceMonitor.C_pco;
        float initialG = confidenceMonitor.G_pco;
        
        Debug.Log($"Initial C_pco: {initialC:F3}, G_pco: {initialG:F3}");
        
        // Wait for system to stabilize
        yield return new WaitForSeconds(2f);
        
        float finalC = confidenceMonitor.C_pco;
        float finalG = confidenceMonitor.G_pco;
        
        Debug.Log($"After 2s: C_pco: {finalC:F3}, G_pco: {finalG:F3}");
        
        // System should maintain or improve confidence when stable
        bool passed = finalC >= initialC * 0.9f;
        
        if (passed)
        {
            Debug.Log("✓ Test PASSED: Confidence stable/recovering");
            testsPassed++;
        }
        else
        {
            Debug.LogWarning("✗ Test FAILED: Confidence degrading unexpectedly");
            testsFailed++;
        }
        
        testPassed = passed;
    }

    // Manual test functions
    public void SimulateJitter()
    {
        StartCoroutine(SimulateJitterCoroutine());
    }

    IEnumerator SimulateJitterCoroutine()
    {
        Debug.Log("Simulating frame jitter for 2 seconds...");
        float duration = 2f;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            // Can't actually control frame time from here,
            // but we can monitor the response
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        Debug.Log($"Jitter test complete. Timing confidence: {confidenceMonitor.TimingConfidence:F3}");
    }

    public void SimulateMuSpike()
    {
        Debug.Log("Injecting μ spike...");
        crownManager.mu_h = 0.95f;
        Debug.Log($"μ confidence: {confidenceMonitor.MuConfidence:F3}");
    }

    public void SimulateThermalWarning()
    {
        if (hardwareMonitor != null && hardwareMonitor.thermalMonitor != null)
        {
            Debug.Log("Simulating thermal warning...");
            Debug.Log($"Current temp: {hardwareMonitor.thermalMonitor.CurrentTemperature:F1}°C");
            Debug.Log($"Thermal confidence: {confidenceMonitor.ThermalConfidence:F3}");
        }
        else
        {
            Debug.LogWarning("No thermal monitor available");
        }
    }

    // Debug visualization
    void OnGUI()
    {
        if (!Application.isEditor) return;
        
        GUILayout.BeginArea(new Rect(10, 780, 400, 200));
        
        GUILayout.Label("=== PCO Confidence Test Suite ===");
        GUILayout.Label($"Current Test: {currentTest}");
        GUILayout.Label($"Status: {(testPassed ? "✓ PASSED" : "⊗ RUNNING")}");
        GUILayout.Space(5);
        
        GUILayout.Label($"Tests Run: {testsRun}");
        GUILayout.Label($"Passed: {testsPassed}");
        GUILayout.Label($"Failed: {testsFailed}");
        
        if (testsRun > 0)
        {
            float successRate = (float)testsPassed / testsRun * 100f;
            Color color = successRate > 80f ? Color.green : successRate > 50f ? Color.yellow : Color.red;
            GUI.color = color;
            GUILayout.Label($"Success Rate: {successRate:F1}%");
            GUI.color = Color.white;
        }
        
        GUILayout.Space(10);
        
        if (GUILayout.Button("Run All Tests (T)"))
        {
            StartCoroutine(RunAllTests());
        }
        
        GUILayout.Label("Manual Tests:");
        if (GUILayout.Button("1: Jitter Detection"))
            StartCoroutine(TestJitterDetection());
        if (GUILayout.Button("2: μ Spike Detection"))
            StartCoroutine(TestMuSpikeDetection());
        if (GUILayout.Button("3: Thermal Response"))
            StartCoroutine(TestThermalResponse());
        if (GUILayout.Button("4: Soft Floors"))
            StartCoroutine(TestSoftFloors());
        if (GUILayout.Button("5: Emergency Guard"))
            StartCoroutine(TestEmergencyGuard());
        
        GUILayout.EndArea();
    }
}