# Learning Scripts - Procedural Generation Tutorial

This folder contains educational scripts for learning procedural generation step-by-step.

## Getting Started

1. **Read the Curriculum:** Open `PROCEDURAL_GENERATION_CURRICULUM.md` in the parent folder
2. **Follow the Lessons:** Work through each module in order
3. **Use These Scripts:** Each script corresponds to a lesson

## Scripts Overview

### Module 2: CPU-Based Implementation

- **`SimpleHeightGenerator.cs`** (Lesson 2.1)
  - Demonstrates noise functions on CPU
  - Shows how fractal noise works
  - Easy to debug and understand
  - **Usage:** Add to GameObject, adjust parameters, use context menu "Test Height Calculation"

### Module 3: Compute Shaders

- **`SimpleComputeHeightGenerator.cs`** (Lesson 3.2)
  - Shows how to call compute shaders from C#
  - Demonstrates the CPU-GPU pipeline
  - **Usage:** Assign a compute shader, run test, observe results

## Next Steps

After completing the lessons, you'll create:
- Your own compute shaders
- GPU-based planet generators
- Advanced noise systems

## Tips

- **Don't skip ahead:** Each lesson builds on the previous one
- **Experiment:** Change parameters and see what happens
- **Debug:** Use Debug.Log to understand data flow
- **Compare:** Look at the Solar-System project code as reference

## Troubleshooting

**Compute shader not working?**
- Check that buffer names match exactly (case-sensitive!)
- Verify thread group size matches `[numthreads]` in shader
- Make sure kernel index is correct (usually 0)

**Getting wrong results?**
- Log min/max values to verify data range
- Check that parameters are being set correctly
- Verify noise functions are working as expected

**Performance issues?**
- Profile with Unity Profiler
- Check buffer cleanup (memory leaks)
- Verify you're releasing buffers properly

Good luck learning procedural generation!







