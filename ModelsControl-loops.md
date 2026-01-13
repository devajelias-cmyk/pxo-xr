# Control Loops in PCO-XR

## Introduction

At the core of PCO-XR is the idea that XR comfort can be treated as a control system. Multiple feedback loops operate simultaneously to maintain user comfort and system stability. These loops can be conceptualized without specifying a particular engine or SDK.

## Reference Loops

### 1. Vestibular-Visual Alignment Loop
- Measures phase difference between expected motion (vestibular/proprioceptive) and visual motion
- Corrects acceleration and rotation signals to maintain alignment
- Acts to prevent vection-induced discomfort

### 2. Confidence (μ) Monitoring Loop
- Tracks user model confidence in predicting system behavior
- Low μ triggers slower acceleration, smaller rotations, or additional cues
- High μ allows higher gains and more dynamic motion

### 3. Crown Constraints Enforcement
- Hard boundaries on acceleration, jerk, latency, and motion amplitude
- Prevents system from exceeding thresholds likely to induce discomfort
- Overrides loop outputs when necessary

## Observability
- Each loop depends on measurable or estimable signals
- Some axes (μ, perceived coupling) may only be inferred
- Measurement limitations are part of ongoing refinement
