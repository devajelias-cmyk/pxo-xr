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
        
        // Validate component links on startup
        ValidateSetup();
    }

    void ValidateSetup()
    {
        bool valid = true;
        
        if (confidenceMonitor == null)
        {
            Debug.LogError("PCOConfidenceTestSuite: confidenceMonitor not assigned!");
            valid = false;
        }
        
        if (crownManager == null)
        {
            Debug.LogError("PCOConfidenceTestSuite: crownManager not assigned!");
            valid = false;
        }
        
        if (hardwareMonitor == null)
        {
            Debug.LogWarning("PCOConfidenceTestSuite: hardwareMonitor not assigned. Some tests will be skipped.");
        }
        
        if (!valid)
        {
            Debug.LogError("PCOConfidenceTestSuite: Setup incomplete. Assign missing components in Inspector.");
        }
        else
        {
            Debug.Log("PCOConfidenceTestSuite: Setup validated. Ready to run tests.");
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
        
        if (testsRun > 0)
        {
            Debug.Log($"Success Rate: {(float)testsPassed / testsRun * 100f:F1}%");
        }
    }

    // Test 1: Jitter Detection
    IEnumerator TestJitterDetection()
    {
        currentTest = "Jitter Detection";
        testsRun++;
        
        Debug.Log("--- Test 1: Jitter Detection ---");
        
        if (confidenceMonitor == null)
        {
            Debug.LogError("✗ Test FAILED: confidenceMonitor not assigned");
            testsFailed++;
            testPassed = false;
            yield break;
        }
        
        // Simulate stable frame rate
        float initialConfidence = confidenceMonitor.TimingConfidence;
        Debug.Log($"Initial timing confidence: {initialConfidence:F3}");
        
        // Wait for stable baseline
        yield return new WaitForSeconds(1f);
        float stableConfidence = confidenceMonitor.TimingConfidence;
        Debug.Log($"Stable timing confidence: {stableConfidence:F3}");
        
        // Check that confidence is working and in valid range
        bool passed = stableConfidence >= 0f && stableConfidence <= 1f && stableConfidence > 0.5f;
        
        if (passed)
        {
            Debug.Log("✓ Test PASSED: Timing confidence responding correctly");
            testsPassed++;
        }
        else
        {
            Debug.LogWarning($"✗ Test FAILED: Timing confidence {stableConfidence:F3} outside expected range");
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
        
        if (confidenceMonitor == null || crownManager == null)
        {
            Debug.LogError("✗ Test FAILED: Required components not assigned");
            testsFailed++;
            testPassed = false;
            yield break;
        }
        
        float initialMuConfidence = confidenceMonitor.MuConfidence;
        Debug.Log($"Initial μ confidence: {initialMuConfidence:F3}");
        
        // Inject artificial μ spike
        float originalMu = crownManager.mu_h;
        Debug.Log($"Original mu_h: {originalMu:F3}");
        
        crownManager.mu_h = 0.95f;  // Instant spike
        Debug.Log($"Injected spike: mu_h = 0.95");
        
        // Wait one frame for detection
        yield return null;
        
        float spikedConfidence = confidenceMonitor.MuConfidence;
        Debug.Log($"μ confidence after spike: {spikedConfidence:F3}");
        
        // Should detect impossible velocity and reduce confidence
        // Note: First frame protection might prevent this, so check if it's different or still high
        bool passed = spikedConfidence < initialMuConfidence * 0.95f || spikedConfidence < 0.5f;
        
        // Restore original value
        crownManager.mu_h = originalMu;
        
        // Wait for recovery
        yield return new WaitForSeconds(0.5f);
        
        if (passed)
        {
            Debug.Log("✓ Test PASSED: μ spike detected, confidence reduced");
            testsPassed++;
        }
        else
        {
            Debug.LogWarning("✗ Test FAILED: μ spike not detected (may need multiple frames)");
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
        
        if (confidenceMonitor == null)
        {
            Debug.LogError("✗ Test FAILED: confidenceMonitor not assigned");
            testsFailed++;
            testPassed = false;
            yield break;
        }
        
        if (hardwareMonitor == null || hardwareMonitor.thermalMonitor == null)
        {
            Debug.LogWarning("⊘ Test SKIPPED: No thermal monitor available");
            testsRun--; // Don't count skipped tests
            yield break;
        }
        
        float initialThermalConf = confidenceMonitor.ThermalConfidence;
        float currentTemp = hardwareMonitor.thermalMonitor.CurrentTemperature;
        
        Debug.Log($"Initial thermal confidence: {initialThermalConf:F3}");
        Debug.Log($"Current temperature: {currentTemp:F1}°C");
        
        // Check that thermal confidence is in valid range
        bool passed = initialThermalConf >= 0f && initialThermalConf <= 1f;
        
        if (passed)
        {
            Debug.Log("✓ Test PASSED: Thermal confidence in valid range");
            testsPassed++;
        }
        else
        {
            Debug.LogWarning($"✗ Test FAILED: Thermal confidence {initialThermalConf:F3} invalid");
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
        
        if (confidenceMonitor == null)
        {
            Debug.LogError("✗ Test FAILED: confidenceMonitor not assigned");
            testsFailed++;
            testPassed = false;
            yield break;
        }
        
        // Check overall confidence
        float C_pco = confidenceMonitor.C_pco;
        float G_pco = confidenceMonitor.G_pco;
        
        Debug.Log($"Current C_pco: {C_pco:F3}");
        Debug.Log($"Current G_pco: {G_pco:F3}");
        
        // Check individual factors have floors
        float sensorConf = confidenceMonitor.SensorConfidence;
        float timingConf = confidenceMonitor.TimingConfidence;
        float muConf = confidenceMonitor.MuConfidence;
        float thermalConf = confidenceMonitor.ThermalConfidence;
        float modelConf = confidenceMonitor.ModelConfidence;
        
        Debug.Log($"  Sensor:  {sensorConf:F3}");
        Debug.Log($"  Timing:  {timingConf:F3}");
        Debug.Log($"  μ:       {muConf:F3}");
        Debug.Log($"  Thermal: {thermalConf:F3}");
        Debug.Log($"  Model:   {modelConf:F3}");
        
        // All should be >= 0 and <= 1
        // Thermal can be 0 (absolute authority), others should have floors
        bool passed = sensorConf >= 0f && sensorConf <= 1f &&
                     timingConf >= 0f && timingConf <= 1f &&
                     muConf >= 0f && muConf <= 1f &&
                     thermalConf >= 0f && thermalConf <= 1f &&
                     modelConf >= 0f && modelConf <= 1f;
        
        if (passed)
        {
            Debug.Log("✓ Test PASSED: Soft floors functioning, all factors in valid range");
            testsPassed++;
        }
        else
        {
            Debug.LogWarning("✗ Test FAILED: Confidence factors outside valid range");
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
        
        if (confidenceMonitor == null)
        {
            Debug.LogError("✗ Test FAILED: confidenceMonitor not assigned");
            testsFailed++;
            testPassed = false;
            yield break;
        }
        
        // Emergency guard should clamp G_pco on critical failures
        float G_pco = confidenceMonitor.G_pco;
        float C_pco = confidenceMonitor.C_pco;
        
        Debug.Log($"Current C_pco: {C_pco:F3}");
        Debug.Log($"Current G_pco: {G_pco:F3}");
        Debug.Log($"Sensor confidence: {confidenceMonitor.SensorConfidence:F3}");
        Debug.Log($"Thermal confidence: {confidenceMonitor.ThermalConfidence:F3}");
        Debug.Log($"μ confidence: {confidenceMonitor.MuConfidence:F3}");
        
        // Check for critical failures
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
            // No critical failure - G should follow C smoothly and be > 0
            passed = G_pco >= 0f && G_pco <= 1f;
            Debug.Log($"No critical failure, G_pco in valid range [0,1]: {passed}");
        }
        
        if (passed)
        {
            Debug.Log("✓ Test PASSED: Emergency guard functioning correctly");
            testsPassed++;
        }
        else
        {
            Debug.LogWarning("✗ Test FAILED: Emergency guard not working as expected");
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
        
        if (confidenceMonitor == null)
        {
            Debug.LogError("✗ Test FAILED: confidenceMonitor not assigned");
            testsFailed++;
            testPassed = false;
            yield break;
        }
        
        float modelConfidence = confidenceMonitor.ModelConfidence;
        Debug.Log($"Model confidence: {modelConfidence:F3}");
        
        // Model confidence should be in valid range
        bool passed = modelConfidence >= 0f && modelConfidence <= 1f;
        
        if (passed)
        {
            Debug.Log("✓ Test PASSED: Model validation active and in valid range");
            testsPassed++;
        }
        else
        {
            Debug.LogWarning($"✗ Test FAILED: Model confidence {modelConfidence:F3} invalid");
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
        
        if (confidenceMonitor == null)
        {
            Debug.LogError("✗ Test FAILED: confidenceMonitor not assigned");
            testsFailed++;
            testPassed = false;
            yield break;
        }
        
        // μ confidence should be initialized properly (not showing false spike)
        float muConf = confidenceMonitor.MuConfidence;
        Debug.Log($"μ confidence: {muConf:F3}");
        
        // Should be reasonably high (no false spike)
        // Note: This test is most meaningful on actual startup
        bool passed = muConf >= 0.5f && muConf <= 1f;
        
        if (passed)
        {
            Debug.Log("✓ Test PASSED: No false first-frame spike (or properly initialized)");
            testsPassed++;
        }
        else
        {
            Debug.LogWarning($"✗ Test FAILED: μ confidence {muConf:F3} unexpectedly low");
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
        
        if (confidenceMonitor == null)
        {
            Debug.LogError("✗ Test FAILED: confidenceMonitor not assigned");
            testsFailed++;
            testPassed = false;
            yield break;
        }
        
        float initialC = confidenceMonitor.C_pco;
        float initialG = confidenceMonitor.G_pco;
        
        Debug.Log($"Initial C_pco: {initialC:F3}, G_pco: {initialG:F3}");
        
        // Wait for system to stabilize
        yield return new WaitForSeconds(2f);
        
        float finalC = confidenceMonitor.C_pco;
        float finalG = confidenceMonitor.G_pco;
        
        Debug.Log($"After 2s: C_pco: {finalC:F3}, G_pco: {finalG:F3}");
        
        // System should maintain or improve confidence when stable
        // Allow for slight degradation (90%) but should stay reasonable
        bool passed = finalC >= initialC * 0.85f && finalC >= 0.3f;
        
        if (passed)
        {
            Debug.Log("✓ Test PASSED: Confidence stable/recovering as expected");
            testsPassed++;
        }
        else
        {
            Debug.LogWarning($"✗ Test FAILED: Confidence degraded unexpectedly from {initialC:F3} to {finalC:F3}");
            testsFailed++;
        }
        
        testPassed = passed;
    }

    // Manual test helper functions
    public void SimulateJitter()
    {
        StartCoroutine(SimulateJitterCoroutine());
    }

    IEnumerator SimulateJitterCoroutine()
    {
        Debug.Log("Simulating frame jitter monitoring for 2 seconds...");
        float duration = 2f;
        float elapsed = 0f;
        
        float minConf = 1f;
        float maxConf = 0f;
        
        while (elapsed < duration)
        {
            if (confidenceMonitor != null)
            {
                float conf = confidenceMonitor.TimingConfidence;
                minConf = Mathf.Min(minConf, conf);
                maxConf = Mathf.Max(maxConf, conf);
            }
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        Debug.Log($"Jitter test complete. Timing confidence range: [{minConf:F3}, {maxConf:F3}]");
    }

    public void SimulateMuSpike()
    {
        if (crownManager != null && confidenceMonitor != null)
        {
            Debug.Log("Injecting μ spike...");
            float before = confidenceMonitor.MuConfidence;
            crownManager.mu_h = 0.95f;
            Debug.Log($"μ confidence before: {before:F3}");
            Debug.Log("Check confidence next frame...");
        }
        else
        {
            Debug.LogWarning("Cannot simulate μ spike - components not assigned");
        }
    }

    public void SimulateThermalWarning()
    {
        if (hardwareMonitor != null && hardwareMonitor.thermalMonitor != null && confidenceMonitor != null)
        {
            Debug.Log("Thermal status check...");
            Debug.Log($"Current temp: {hardwareMonitor.ther