# Procedural Generation Learning Curriculum
## From Beginner to Advanced: Understanding GPU-Based Planet Generation

---

## **COURSE OVERVIEW**

This curriculum will teach you procedural generation by analyzing the Solar-System project and implementing similar techniques in Astral Explorer. We'll progress from fundamental concepts to advanced GPU compute shader techniques.

**Prerequisites:** Basic Unity C# knowledge, understanding of GameObjects and Components
**Duration:** 4-6 weeks (depending on pace)
**Learning Style:** Theory → Observation → Practice → Implementation

---

## **MODULE 1: Foundations - Understanding Procedural Generation**

### **Lesson 1.1: What is Procedural Generation?**

**Learning Objectives:**
- Understand the difference between hand-crafted and procedural content
- Learn why procedural generation is useful
- Identify the core components of a procedural system

**Theory:**
Procedural generation creates content algorithmically rather than manually. For planets, this means:
- **Input:** A seed number (determines randomness)
- **Process:** Mathematical functions (noise, blending, transformations)
- **Output:** Terrain height, colors, textures

**Key Concept:** The same seed always produces the same result (deterministic randomness).

**Exercise 1.1.1 - Observation:**
1. Open the Solar-System project in Unity
2. Find a planet prefab (e.g., in `Celestial Body/Templates/Earth-Like/`)
3. Select it and examine the Inspector
4. Find the `CelestialBodyGenerator` component
5. Change the `seed` value in `body.shape` settings
6. Observe how the planet changes but remains consistent

**Exercise 1.1.2 - Analysis:**
1. Open `Celestial Body/Scripts/CelestialBodyGenerator.cs`
2. Read lines 196-255 (the `GenerateTerrainMesh` function)
3. Identify these steps in the code:
   - Creating base sphere vertices
   - Calculating heights
   - Applying heights to vertices
   - Generating shading data
   - Creating the final mesh

**Checkpoint:** Can you explain in your own words what procedural generation means? Write it down.

---

### **Lesson 1.2: The Pipeline - Data Flow**

**Learning Objectives:**
- Understand how data flows through a procedural system
- Learn the relationship between CPU and GPU in procedural generation
- Identify where each step happens in the code

**Theory:**
The procedural pipeline follows this flow:

```
1. BASE GEOMETRY (CPU)
   ↓
2. VERTEX POSITIONS → GPU BUFFER
   ↓
3. HEIGHT CALCULATION (GPU Compute Shader)
   ↓
4. HEIGHTS → CPU ARRAY
   ↓
5. APPLY HEIGHTS TO VERTICES (CPU)
   ↓
6. SHADING DATA CALCULATION (GPU Compute Shader)
   ↓
7. SHADING DATA → MESH UVs (CPU)
   ↓
8. RENDER MESH (GPU Shader)
```

**Why GPU?** Height calculations run on thousands of vertices simultaneously, which is much faster on GPU than CPU.

**Exercise 1.2.1 - Code Tracing:**
Trace through `CelestialBodyGenerator.GenerateTerrainMesh()`:

1. **Line 200:** `CreateSphereVertsAndTris(resolution)` 
   - What does this return? (Hint: check `SphereMesh.cs`)
   - This is STEP 1: Base Geometry

2. **Line 201:** `ComputeHelper.CreateStructuredBuffer<Vector3>(ref vertexBuffer, vertices)`
   - This is STEP 2: Sending data to GPU
   - What is a ComputeBuffer? (It's Unity's way of sending arrays to GPU)

3. **Line 206:** `body.shape.CalculateHeights(vertexBuffer)`
   - This is STEP 3: GPU calculation
   - Open `CelestialBodyShape.cs` lines 20-35
   - Notice it runs a compute shader and gets results back

4. **Line 227:** `vertices[i] *= height`
   - This is STEP 5: Applying heights on CPU
   - Why multiply? (The base sphere has radius 1, height scales it)

5. **Line 240:** `body.shading.GenerateShadingData(vertexBuffer)`
   - This is STEP 6: More GPU calculation
   - The results go into mesh UVs (line 241)

**Exercise 1.2.2 - Visual Debugging:**
Add this temporary code to see the pipeline in action:

```csharp
// In CelestialBodyGenerator.GenerateTerrainMesh(), after line 206:
Debug.Log($"Calculated {heights.Length} heights. Min: {heights.Min()}, Max: {heights.Max()}");
```

Run it and observe the console output. This shows you the data at each stage.

**Checkpoint:** Draw a diagram showing the data flow. Label CPU vs GPU steps.

---

### **Lesson 1.3: Understanding Noise - The Building Block**

**Learning Objectives:**
- Understand what noise functions are
- Learn the difference between random and noise
- See how noise creates natural-looking patterns

**Theory:**
**Random:** Completely unpredictable (like rolling dice)
**Noise:** Smoothly varying values that look random but are deterministic

Think of noise like a mountain range: nearby points have similar heights (smooth), but far apart points can be very different (varied).

**Types of Noise:**
1. **Simple Noise (Perlin/Simplex):** Smooth, wavy patterns
2. **Ridge Noise:** Creates sharp peaks and valleys
3. **Fractal Noise:** Multiple layers of noise at different scales

**Exercise 1.3.1 - Visualizing Noise:**
1. Open `Celestial Body/Scripts/NoiseSettings/SimpleNoiseSettings.cs`
2. Notice the parameters:
   - `numLayers`: How many noise layers to combine
   - `lacunarity`: How much each layer is scaled (usually 2 = double size)
   - `persistence`: How much each layer contributes (usually 0.5 = half strength)
   - `scale`: Overall size of the noise pattern

**Exercise 1.3.2 - Experiment:**
1. In Unity, select a planet with `EarthShape`
2. Find `continentNoise` in the Inspector
3. Change `scale` from 1 to 10 (noise becomes finer detail)
4. Change `scale` from 1 to 0.1 (noise becomes larger features)
5. Change `numLayers` from 4 to 1 (less detail)
6. Change `persistence` from 0.5 to 0.1 (softer, smoother)

**Understanding:** 
- **Scale** controls feature size (smaller scale = larger features)
- **Layers** add detail (more layers = more complexity)
- **Persistence** controls contrast (lower = smoother)

**Checkpoint:** Can you predict what will happen if you set `numLayers = 8` and `persistence = 0.8`? Test it and see if you were right.

---

## **MODULE 2: CPU-Based Implementation (Building Intuition)**

### **Lesson 2.1: Creating a Simple CPU Height Generator**

**Learning Objectives:**
- Implement height calculation on CPU first (easier to debug)
- Understand how noise values become terrain heights
- Learn to visualize procedural data

**Why Start with CPU?** 
- Easier to debug (can use `Debug.Log`)
- No shader syntax to learn yet
- Builds intuition before moving to GPU

**Exercise 2.1.1 - Create a Test Script:**
In Astral Explorer, create a new script: `Assets/PlanetGenerationScripts/Learning/SimpleHeightGenerator.cs`

```csharp
using UnityEngine;

public class SimpleHeightGenerator : MonoBehaviour
{
    [Header("Noise Settings")]
    public int seed = 0;
    public float scale = 1f;
    public int numLayers = 4;
    public float persistence = 0.5f;
    public float lacunarity = 2f;
    
    [Header("Height Settings")]
    public float heightMultiplier = 0.1f;
    
    // Simple noise function using Unity's built-in PerlinNoise
    float SimpleNoise(Vector3 position, float scale)
    {
        // PerlinNoise takes 2D coordinates, so we use X and Z
        float x = position.x * scale;
        float z = position.z * scale;
        return Mathf.PerlinNoise(x, z);
    }
    
    // Fractal noise: multiple layers combined
    float FractalNoise(Vector3 position)
    {
        float value = 0f;
        float amplitude = 1f;
        float frequency = scale;
        
        for (int i = 0; i < numLayers; i++)
        {
            // Get noise value at this layer
            float noiseValue = SimpleNoise(position, frequency);
            
            // Add to total (weighted by amplitude)
            value += noiseValue * amplitude;
            
            // Next layer: higher frequency, lower amplitude
            frequency *= lacunarity;  // Double the frequency
            amplitude *= persistence;  // Half the amplitude
        }
        
        return value;
    }
    
    // Calculate height for a point on unit sphere
    public float CalculateHeight(Vector3 pointOnUnitSphere)
    {
        // Get noise value (0 to 1 range)
        float noiseValue = FractalNoise(pointOnUnitSphere);
        
        // Convert to height (centered around 1.0, with variation)
        float height = 1f + (noiseValue - 0.5f) * heightMultiplier;
        
        return height;
    }
    
    // Test function - call this from Inspector button or Start()
    [ContextMenu("Test Height Calculation")]
    void TestHeightCalculation()
    {
        Vector3 testPoint = Vector3.up; // Point at top of sphere
        float height = CalculateHeight(testPoint);
        Debug.Log($"Point: {testPoint}, Height: {height}");
        
        // Test multiple points
        for (int i = 0; i < 10; i++)
        {
            Vector3 randomPoint = Random.onUnitSphere;
            height = CalculateHeight(randomPoint);
            Debug.Log($"Random point {i}: {randomPoint}, Height: {height}");
        }
    }
}
```

**Exercise 2.1.2 - Understanding the Code:**
1. **`SimpleNoise`:** Uses Unity's `Mathf.PerlinNoise` - a built-in 2D noise function
2. **`FractalNoise`:** Combines multiple layers:
   - Layer 1: Large features (frequency = scale)
   - Layer 2: Medium features (frequency = scale * 2)
   - Layer 3: Small features (frequency = scale * 4)
   - Each layer contributes less (amplitude decreases)
3. **`CalculateHeight`:** Converts noise (0-1) to height multiplier

**Exercise 2.1.3 - Test It:**
1. Create an empty GameObject in Astral Explorer
2. Add the `SimpleHeightGenerator` component
3. Right-click the component → "Test Height Calculation"
4. Observe the console output
5. Change `scale`, `numLayers`, `persistence` and test again

**Checkpoint:** Can you explain what `lacunarity` and `persistence` do? Write it in comments in your code.

---

### **Lesson 2.2: Applying Heights to a Mesh**

**Learning Objectives:**
- Learn how to modify mesh vertices
- Understand vertex manipulation
- Create a visible result from procedural generation

**Theory:**
A mesh is made of:
- **Vertices:** 3D points (Vector3)
- **Triangles:** Groups of 3 vertices that form faces
- **Normals:** Direction each face points (for lighting)

To create terrain, we:
1. Start with a base shape (sphere)
2. Calculate height for each vertex
3. Move vertex outward by height amount

**Exercise 2.2.1 - Create Mesh Modifier:**
Create `Assets/PlanetGenerationScripts/Learning/SimplePlanetGenerator.cs`:

```csharp
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class SimplePlanetGenerator : MonoBehaviour
{
    [Header("Generation Settings")]
    public int resolution = 50; // How many vertices per face
    public float radius = 1f;
    
    [Header("Height Settings")]
    public SimpleHeightGenerator heightGenerator;
    
    Mesh mesh;
    Vector3[] baseVertices; // Original sphere vertices
    Vector3[] modifiedVertices; // Vertices with heights applied
    
    void Start()
    {
        GeneratePlanet();
    }
    
    void GeneratePlanet()
    {
        // Get or create mesh
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        if (meshFilter.sharedMesh == null)
        {
            meshFilter.sharedMesh = new Mesh();
        }
        mesh = meshFilter.sharedMesh;
        mesh.name = "Procedural Planet";
        
        // Create base sphere (we'll use a simple icosphere)
        CreateBaseSphere();
        
        // Apply heights
        ApplyHeights();
        
        // Update mesh
        UpdateMesh();
    }
    
    void CreateBaseSphere()
    {
        // For learning, we'll create a simple sphere using Unity's primitive
        // In production, you'd use proper icosphere generation (like SphereMesh.cs)
        
        // Create vertices in a grid pattern (simplified for learning)
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        
        // Create a simple sphere by subdividing a cube
        // This is a simplified version - real icospheres are more complex
        int segments = resolution;
        
        for (int y = 0; y <= segments; y++)
        {
            for (int x = 0; x <= segments; x++)
            {
                // Create point on unit sphere
                float u = (float)x / segments;
                float v = (float)y / segments;
                
                // Convert to sphere coordinates
                float theta = u * 2 * Mathf.PI; // Longitude
                float phi = v * Mathf.PI; // Latitude
                
                Vector3 point = new Vector3(
                    Mathf.Sin(phi) * Mathf.Cos(theta),
                    Mathf.Cos(phi),
                    Mathf.Sin(phi) * Mathf.Sin(theta)
                );
                
                vertices.Add(point);
            }
        }
        
        // Create triangles
        for (int y = 0; y < segments; y++)
        {
            for (int x = 0; x < segments; x++)
            {
                int i = y * (segments + 1) + x;
                
                triangles.Add(i);
                triangles.Add(i + segments + 1);
                triangles.Add(i + 1);
                
                triangles.Add(i + 1);
                triangles.Add(i + segments + 1);
                triangles.Add(i + segments + 2);
            }
        }
        
        baseVertices = vertices.ToArray();
        mesh.vertices = baseVertices;
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();
    }
    
    void ApplyHeights()
    {
        if (heightGenerator == null)
        {
            Debug.LogWarning("No height generator assigned!");
            return;
        }
        
        modifiedVertices = new Vector3[baseVertices.Length];
        
        for (int i = 0; i < baseVertices.Length; i++)
        {
            Vector3 baseVertex = baseVertices[i];
            
            // Calculate height for this point
            float height = heightGenerator.CalculateHeight(baseVertex);
            
            // Apply height: move vertex outward
            modifiedVertices[i] = baseVertex * radius * height;
        }
    }
    
    void UpdateMesh()
    {
        mesh.vertices = modifiedVertices;
        mesh.RecalculateNormals(); // Important! Normals must be recalculated after moving vertices
        mesh.RecalculateBounds(); // Update bounding box for culling
    }
    
    // Allow regeneration in editor
    [ContextMenu("Regenerate Planet")]
    void Regenerate()
    {
        GeneratePlanet();
    }
}
```

**Exercise 2.2.2 - Test the Generator:**
1. Create a GameObject in Astral Explorer
2. Add `MeshFilter` and `MeshRenderer` components (automatic with `[RequireComponent]`)
3. Add `SimpleHeightGenerator` component
4. Add `SimplePlanetGenerator` component
5. Assign the height generator in the inspector
6. Set `resolution = 50`
7. Press Play or use "Regenerate Planet" context menu
8. You should see a procedurally generated sphere!

**Exercise 2.2.3 - Experiment:**
1. Change `heightGenerator.heightMultiplier` from 0.1 to 0.5 - see more dramatic terrain
2. Change `heightGenerator.scale` - see different feature sizes
3. Change `heightGenerator.numLayers` - see more or less detail

**Understanding:**
- `baseVertices` are points on a unit sphere (radius = 1)
- `height` is a multiplier (1.0 = no change, 1.1 = 10% taller)
- `modifiedVertices[i] = baseVertex * radius * height` scales the sphere and applies height

**Checkpoint:** Can you explain why we multiply by `radius * height`? What happens if height = 1.0?

---

### **Lesson 2.3: Comparing CPU vs GPU Approach**

**Learning Objectives:**
- Understand performance differences
- See why GPU is necessary for high-resolution planets
- Learn when to use CPU vs GPU

**Theory:**
**CPU Approach (what we just built):**
- Easy to debug
- Sequential processing (one vertex at a time)
- Limited by CPU cores (typically 4-16 threads)
- Good for: Learning, low-resolution (< 1000 vertices), debugging

**GPU Approach (Solar-System project):**
- Harder to debug (can't use Debug.Log easily)
- Parallel processing (thousands of vertices simultaneously)
- Limited by GPU cores (typically 1000+ threads)
- Good for: Production, high-resolution (10,000+ vertices), real-time generation

**Exercise 2.3.1 - Performance Test:**
Add this to `SimplePlanetGenerator`:

```csharp
void PerformanceTest()
{
    System.Diagnostics.Stopwatch sw = System.Diagnostics.Stopwatch.StartNew();
    
    // Test different resolutions
    int[] testResolutions = { 10, 50, 100, 200 };
    
    foreach (int res in testResolutions)
    {
        resolution = res;
        sw.Restart();
        GeneratePlanet();
        sw.Stop();
        
        int vertexCount = baseVertices.Length;
        Debug.Log($"Resolution: {res}, Vertices: {vertexCount}, Time: {sw.ElapsedMilliseconds}ms");
    }
}
```

Run this and observe:
- How time increases with resolution
- At what point it becomes too slow
- Why GPU is needed for high-res planets

**Exercise 2.3.2 - Compare with Solar-System:**
1. Open Solar-System project
2. Find a planet with `CelestialBodyGenerator`
3. Enable "Log Timers" in the component
4. Change resolution settings
5. Observe the generation times
6. Notice it's much faster even at high resolutions (GPU advantage)

**Key Insight:** The Solar-System project uses GPU compute shaders, which is why it can handle 300+ resolution planets smoothly.

**Checkpoint:** At what resolution does your CPU version become noticeably slow? Why do you think GPU is better?

---

## **MODULE 3: Understanding Compute Shaders**

### **Lesson 3.1: What is a Compute Shader?**

**Learning Objectives:**
- Understand what compute shaders are
- Learn the basic structure of a compute shader
- See how data flows to/from GPU

**Theory:**
A **compute shader** is a program that runs on the GPU, similar to a regular shader but:
- It doesn't render anything directly
- It processes data in parallel
- It can read and write to buffers
- It's written in HLSL (High-Level Shading Language)

**Basic Structure:**
```hlsl
#pragma kernel CSMain  // Entry point function

// Input buffer (read-only)
StructuredBuffer<float3> vertices;

// Output buffer (write-only)
RWStructuredBuffer<float> heights;

// Parameters
uint numVertices;

// Thread configuration
[numthreads(64, 1, 1)]  // 64 threads per group
void CSMain(uint id : SV_DispatchThreadID)
{
    if (id >= numVertices) return;  // Safety check
    
    // Each thread processes one vertex
    float3 pos = vertices[id];
    
    // Calculate height
    float height = CalculateHeight(pos);
    
    // Write result
    heights[id] = height;
}
```

**Key Concepts:**
- **Kernel:** The function that runs on GPU (`CSMain`)
- **Thread:** One execution of the kernel (processes one vertex)
- **Thread Group:** A batch of threads (64 in example above)
- **Dispatch:** Sending work to GPU (determines how many thread groups to run)

**Exercise 3.1.1 - Examine a Real Compute Shader:**
1. Open `Celestial Body/Scripts/Shaders/Compute/Height/EarthHeight.compute`
2. Identify these parts:
   - `#pragma kernel CSMain` (line 1) - Entry point
   - `StructuredBuffer<float3> vertices` (line 7) - Input
   - `RWStructuredBuffer<float> heights` (line 8) - Output
   - `[numthreads(512,1,1)]` (line 23) - 512 threads per group
   - `void CSMain(uint id : SV_DispatchThreadID)` (line 24) - Main function

3. Trace the logic:
   - Line 30: Gets vertex position
   - Line 32: Calculates continent shape using noise
   - Line 41: Calculates ridge noise (mountains)
   - Line 44: Blends them together
   - Line 46: Calculates final height
   - Line 50: Writes result

**Exercise 3.1.2 - Understand Threading:**
If you have 10,000 vertices and `[numthreads(512,1,1)]`:
- Each thread group processes 512 vertices
- You need: 10,000 / 512 = ~20 thread groups
- All 10,000 threads run in parallel (or in batches if GPU is busy)

**Checkpoint:** Can you explain what `SV_DispatchThreadID` represents? (Hint: it's the unique ID for each thread)

---

### **Lesson 3.2: How C# Calls Compute Shaders**

**Learning Objectives:**
- Learn how to set up compute shader execution from C#
- Understand ComputeBuffer usage
- See the complete CPU-GPU communication

**Theory:**
To run a compute shader from C#:

1. **Load the compute shader:** `ComputeShader shader = Resources.Load<ComputeShader>("MyShader");`
2. **Create buffers:** `ComputeBuffer buffer = new ComputeBuffer(count, stride);`
3. **Set data:** `buffer.SetData(array);`
4. **Set buffers on shader:** `shader.SetBuffer(kernelIndex, "bufferName", buffer);`
5. **Set parameters:** `shader.SetFloat("parameterName", value);`
6. **Dispatch:** `shader.Dispatch(kernelIndex, numGroupsX, numGroupsY, numGroupsZ);`
7. **Get results:** `buffer.GetData(resultArray);`

**Exercise 3.2.1 - Trace the Solar-System Code:**
Open `Celestial Body/Scripts/Shape/CelestialBodyShape.cs`:

1. **Line 24:** `SetShapeData();` - Sets all parameters on the shader
2. **Line 25:** `heightMapCompute.SetInt("numVertices", vertexBuffer.count);` - Sets parameter
3. **Line 25:** `heightMapCompute.SetBuffer(0, "vertices", vertexBuffer);` - Sets input buffer
4. **Line 26:** `ComputeHelper.CreateAndSetBuffer<float>(...)` - Creates output buffer
5. **Line 29:** `ComputeHelper.Run(heightMapCompute, vertexBuffer.count);` - Dispatches
6. **Line 32-33:** Gets results back

**Exercise 3.2.2 - Examine ComputeHelper:**
Open `Script Utilities/ComputeHelper.cs`:

1. **Lines 14-20:** `Run()` function calculates thread groups and dispatches
2. **Lines 52-64:** `CreateStructuredBuffer()` creates buffers
3. **Lines 87-90:** `CreateAndSetBuffer()` creates buffer and sets it on shader

**Key Function:**
```csharp
public static void Run(ComputeShader cs, int numIterationsX, ...)
{
    Vector3Int threadGroupSizes = GetThreadGroupSizes(cs, kernelIndex);
    int numGroupsX = Mathf.CeilToInt(numIterationsX / (float)threadGroupSizes.x);
    cs.Dispatch(kernelIndex, numGroupsX, numGroupsY, numGroupsZ);
}
```

This automatically calculates how many thread groups you need!

**Exercise 3.2.3 - Create Your First Compute Shader Call:**
Create `Assets/PlanetGenerationScripts/Learning/SimpleComputeHeightGenerator.cs`:

```csharp
using UnityEngine;

public class SimpleComputeHeightGenerator : MonoBehaviour
{
    public ComputeShader heightComputeShader;
    public int testVertexCount = 1000;
    
    ComputeBuffer vertexBuffer;
    ComputeBuffer heightBuffer;
    
    void Start()
    {
        TestComputeShader();
    }
    
    void TestComputeShader()
    {
        if (heightComputeShader == null)
        {
            Debug.LogError("No compute shader assigned!");
            return;
        }
        
        // 1. Create test vertices (points on unit sphere)
        Vector3[] vertices = new Vector3[testVertexCount];
        for (int i = 0; i < testVertexCount; i++)
        {
            vertices[i] = Random.onUnitSphere;
        }
        
        // 2. Create buffers
        vertexBuffer = new ComputeBuffer(testVertexCount, sizeof(float) * 3);
        heightBuffer = new ComputeBuffer(testVertexCount, sizeof(float));
        
        // 3. Set data
        vertexBuffer.SetData(vertices);
        
        // 4. Set buffers on shader
        int kernelIndex = 0; // First (and only) kernel
        heightComputeShader.SetBuffer(kernelIndex, "vertices", vertexBuffer);
        heightComputeShader.SetBuffer(kernelIndex, "heights", heightBuffer);
        heightComputeShader.SetInt("numVertices", testVertexCount);
        
        // 5. Dispatch
        int threadGroupSize = 64; // Must match [numthreads] in shader
        int numGroups = Mathf.CeilToInt(testVertexCount / (float)threadGroupSize);
        heightComputeShader.Dispatch(kernelIndex, numGroups, 1, 1);
        
        // 6. Get results
        float[] heights = new float[testVertexCount];
        heightBuffer.GetData(heights);
        
        // 7. Display results
        Debug.Log($"Computed {heights.Length} heights");
        Debug.Log($"Min: {System.Linq.Enumerable.Min(heights)}, Max: {System.Linq.Enumerable.Max(heights)}");
        
        // 8. Clean up
        ReleaseBuffers();
    }
    
    void ReleaseBuffers()
    {
        if (vertexBuffer != null) vertexBuffer.Release();
        if (heightBuffer != null) heightBuffer.Release();
    }
    
    void OnDestroy()
    {
        ReleaseBuffers();
    }
}
```

**Note:** You'll need to create a simple compute shader first (next lesson).

**Checkpoint:** Can you explain why we need to calculate `numGroups`? What happens if we dispatch too few groups?

---

### **Lesson 3.3: Writing Your First Compute Shader**

**Learning Objectives:**
- Write a basic compute shader
- Understand HLSL syntax basics
- Connect C# code to compute shader

**Theory:**
HLSL (High-Level Shading Language) is similar to C# but:
- No classes (just functions and structs)
- Different data types (`float` instead of `float`, `float3` instead of `Vector3`)
- Built-in math functions
- Special semantics (`SV_DispatchThreadID`)

**Exercise 3.3.1 - Create a Simple Compute Shader:**
In Unity, create a new Compute Shader:
1. Right-click in Project → Create → Shader → Compute Shader
2. Name it `SimpleHeight.compute`

Replace the contents with:

```hlsl
#pragma kernel CSMain

// Input: vertices on unit sphere
StructuredBuffer<float3> vertices;

// Output: height values
RWStructuredBuffer<float> heights;

// Parameters
uint numVertices;
float heightMultiplier;

// Thread configuration: 64 threads per group
[numthreads(64, 1, 1)]
void CSMain(uint id : SV_DispatchThreadID)
{
    // Safety check: don't process beyond array bounds
    if (id >= numVertices)
    {
        return;
    }
    
    // Get vertex position
    float3 pos = vertices[id];
    
    // Simple height calculation: use Y coordinate as height variation
    // (This is just for learning - real noise comes later)
    float height = 1.0 + pos.y * heightMultiplier;
    
    // Clamp to reasonable values
    height = max(0.1, min(2.0, height));
    
    // Write result
    heights[id] = height;
}
```

**Exercise 3.3.2 - Connect to C#:**
1. Create the C# script from Lesson 3.2.3
2. Assign the compute shader in Inspector
3. Set `testVertexCount = 100`
4. Run and check console output

**Exercise 3.3.3 - Add Noise:**
Now let's add simple noise. Replace the height calculation:

```hlsl
// Simple 2D noise function (simplified Perlin-style)
float noise(float2 p)
{
    return frac(sin(dot(p, float2(12.9898, 78.233))) * 43758.5453);
}

// Fractal noise (multiple octaves)
float fractalNoise(float3 pos, float scale, int layers)
{
    float value = 0.0;
    float amplitude = 1.0;
    float frequency = scale;
    
    for (int i = 0; i < layers; i++)
    {
        float2 samplePos = pos.xz * frequency;
        value += noise(samplePos) * amplitude;
        
        frequency *= 2.0;  // Double frequency (lacunarity)
        amplitude *= 0.5;  // Half amplitude (persistence)
    }
    
    return value;
}

// In CSMain, replace height calculation:
float noiseValue = fractalNoise(pos, 1.0, 4);
float height = 1.0 + (noiseValue - 0.5) * heightMultiplier;
```

**Understanding:**
- `noise()` creates pseudo-random values from coordinates
- `fractalNoise()` combines multiple layers (octaves)
- Each layer is smaller and contributes less (creates detail)

**Checkpoint:** Can you modify the noise to create larger features? (Hint: change the initial `scale` parameter)

---

## **MODULE 4: Advanced Concepts - Noise Systems**

### **Lesson 4.1: Understanding Fractal Noise in Detail**

**Learning Objectives:**
- Deeply understand how fractal noise works
- Learn about octaves, lacunarity, and persistence
- See how the Solar-System project implements it

**Theory:**
**Fractal Noise** = Multiple layers of noise at different scales

**Example with 4 layers:**
- Layer 1: Scale 1.0, Amplitude 1.0 → Large features
- Layer 2: Scale 2.0, Amplitude 0.5 → Medium features  
- Layer 3: Scale 4.0, Amplitude 0.25 → Small features
- Layer 4: Scale 8.0, Amplitude 0.125 → Fine details

**Parameters:**
- **Lacunarity:** How much scale increases each layer (usually 2.0)
- **Persistence:** How much amplitude decreases each layer (usually 0.5)
- **Num Layers:** How many octaves to combine

**Exercise 4.1.1 - Visualize Fractal Noise:**
Create a test to see each layer:

```csharp
// In your test script, visualize each layer separately
void VisualizeLayers()
{
    Vector3 testPoint = Vector3.up;
    
    for (int layer = 0; layer < 4; layer++)
    {
        float frequency = Mathf.Pow(2, layer); // 1, 2, 4, 8
        float amplitude = Mathf.Pow(0.5f, layer); // 1, 0.5, 0.25, 0.125
        
        float noiseValue = SimpleNoise(testPoint, frequency);
        float contribution = noiseValue * amplitude;
        
        Debug.Log($"Layer {layer}: Frequency={frequency}, Amplitude={amplitude}, Value={noiseValue}, Contribution={contribution}");
    }
}
```

**Exercise 4.1.2 - Examine Solar-System Implementation:**
1. Open `Celestial Body/Scripts/Shaders/Compute/Includes/FractalNoise.cginc`
2. Find the `simpleNoise` function
3. Notice it uses Simplex noise (better than Perlin for 3D)
4. See how it handles multiple layers

**Key Insight:** The Solar-System project uses **Simplex noise** instead of Perlin because:
- Better quality in 3D
- More efficient computation
- Less directional artifacts

**Exercise 4.1.3 - Implement Proper 3D Noise:**
For your compute shader, you'll need 3D noise. Here's a simplified version:

```hlsl
// 3D noise function (simplified - real implementations are more complex)
float hash(float3 p)
{
    p = frac(p * 0.3183099 + 0.1);
    p *= 17.0;
    return frac(p.x * p.y * p.z * (p.x + p.y + p.z));
}

float noise3D(float3 p)
{
    float3 i = floor(p);
    float3 f = frac(p);
    f = f * f * (3.0 - 2.0 * f);
    
    float n = i.x + i.y * 57.0 + 113.0 * i.z;
    return lerp(
        lerp(lerp(hash(n + 0.0), hash(n + 1.0), f.x),
             lerp(hash(n + 57.0), hash(n + 58.0), f.x), f.y),
        lerp(lerp(hash(n + 113.0), hash(n + 114.0), f.x),
             lerp(hash(n + 170.0), hash(n + 171.0), f.x), f.y),
        f.z);
}
```

**Checkpoint:** Can you explain why we use `f * f * (3.0 - 2.0 * f)`? (Hint: it's smooth interpolation)

---

### **Lesson 4.2: Ridge Noise and Specialized Noise Types**

**Learning Objectives:**
- Understand ridge noise (for mountains)
- Learn when to use different noise types
- See how noise types combine

**Theory:**
**Ridge Noise:** Creates sharp peaks and valleys (like mountain ranges)

**How it works:**
1. Calculate regular noise
2. Take absolute value (creates ridges)
3. Invert (1.0 - abs(noise)) to create peaks
4. Apply power function to sharpen peaks

**Exercise 4.2.1 - Examine Ridge Noise:**
1. Open `Celestial Body/Scripts/NoiseSettings/RidgeNoiseSettings.cs`
2. Notice extra parameters:
   - `power`: How sharp the peaks are
   - `gain`: Additional sharpening
   - `peakSmoothing`: Softens the peaks

3. Open `Celestial Body/Scripts/Shaders/Compute/Includes/FractalNoise.cginc`
4. Find `smoothedRidgidNoise` function
5. See how it:
   - Calculates base noise
   - Takes absolute value
   - Applies power function
   - Smooths the result

**Exercise 4.2.2 - Implement Simple Ridge Noise:**
Add to your compute shader:

```hlsl
float ridgeNoise(float3 pos, float scale, int layers)
{
    float value = 0.0;
    float amplitude = 1.0;
    float frequency = scale;
    float ridgePower = 2.0; // How sharp the peaks
    
    for (int i = 0; i < layers; i++)
    {
        float noiseValue = noise3D(pos * frequency);
        
        // Create ridge: abs(noise) creates valleys, invert to create peaks
        float ridge = 1.0 - abs(noiseValue);
        
        // Apply power to sharpen peaks
        ridge = pow(ridge, ridgePower);
        
        value += ridge * amplitude;
        
        frequency *= 2.0;
        amplitude *= 0.5;
    }
    
    return value;
}
```

**Exercise 4.2.3 - Combine Noise Types:**
In `EarthHeight.compute`, see how they combine:
- Line 32: Continent noise (smooth, large features)
- Line 41: Ridge noise (sharp peaks for mountains)
- Line 44: Mask noise (controls where mountains appear)
- Line 46: Final blend

**Key Concept:** Different noise types create different terrain features:
- **Simple Noise:** Smooth hills and valleys
- **Ridge Noise:** Sharp mountain peaks
- **Domain Warping:** Distorts other noise (creates more natural patterns)

**Checkpoint:** Can you think of a terrain feature that would need a different noise type? (Hint: craters, canyons, plateaus)

---

### **Lesson 4.3: Noise Masking and Blending**

**Learning Objectives:**
- Understand how to control where features appear
- Learn blending techniques
- See how masks create realistic terrain

**Theory:**
**Masking:** Controls where certain features appear (e.g., mountains only on continents)

**Blending Functions:**
- **Lerp:** Linear interpolation (smooth blend)
- **Smoothstep:** Smooth S-curve blend
- **SmoothMax/SmoothMin:** Smooth versions of max/min

**Exercise 4.3.1 - Examine Masking in EarthHeight:**
Open `Celestial Body/Scripts/Shaders/Compute/Height/EarthHeight.compute`:

1. **Line 32:** `continentShape` - Determines land vs ocean
2. **Line 34:** `smoothMax` - Smoothly blends ocean floor
3. **Line 37-39:** Ocean depth calculation (negative values become deeper)
4. **Line 41:** `ridgeNoise` - Mountain noise
5. **Line 44:** `mask` - Controls where mountains appear
6. **Line 46:** Final blend: `continentShape + ridgeNoise * mask`

**Understanding:**
- If `mask = 0`: No mountains (ocean areas)
- If `mask = 1`: Full mountains (mountainous areas)
- If `mask = 0.5`: Half-strength mountains (hills)

**Exercise 4.3.2 - Implement Masking:**
Add to your compute shader:

```hlsl
// In CSMain:
float continentNoise = fractalNoise(pos, 1.0, 4);
float mountainNoise = ridgeNoise(pos, 2.0, 5);

// Create mask: mountains only where continent noise is high
float mask = smoothstep(0.3, 0.7, continentNoise); // Mountains on higher land

// Blend
float finalHeight = 1.0 + continentNoise * 0.05 + mountainNoise * 0.1 * mask;
```

**Exercise 4.3.3 - Smooth Blending Functions:**
Study `Celestial Body/Scripts/Shaders/Compute/Includes/Math.cginc`:

- `smoothMax`: Smooth version of `max(a, b)`
- `smoothMin`: Smooth version of `min(a, b)`
- `Blend`: Smooth interpolation with control

These create natural-looking transitions instead of sharp edges.

**Checkpoint:** Why use `smoothstep` instead of a simple `if` statement? (Hint: think about visual quality)

---

## **MODULE 5: Implementing in Astral Explorer**

### **Lesson 5.1: Adapting Solar-System Code to Astral Explorer**

**Learning Objectives:**
- Understand how to port code between projects
- Learn what to copy vs what to adapt
- Set up the foundation in Astral Explorer

**Theory:**
Astral Explorer already has CPU-based planet generation. We'll add GPU compute shader support while keeping the existing system.

**Exercise 5.1.1 - Analyze Astral Explorer's Current System:**
1. Open `Astral Explorer/Assets/PlanetGenerationScripts/Planet.cs`
2. Notice it uses:
   - `ShapeGenerator` (CPU-based)
   - `ColorGenerator` (CPU-based)
   - `TerrainFace` (creates cube faces, not icosphere)

3. Compare with Solar-System:
   - `CelestialBodyGenerator` (GPU-based)
   - `CelestialBodyShape` (ScriptableObject with compute shader)
   - `SphereMesh` (icosphere generation)

**Key Differences:**
- **Astral Explorer:** Cube-based (6 faces), CPU noise
- **Solar-System:** Icosphere-based, GPU compute shaders

**Exercise 5.1.2 - Copy Essential Utilities:**
Copy these files from Solar-System to Astral Explorer:

1. **`Script Utilities/ComputeHelper.cs`**
   - Essential for compute shader management
   - Handles buffer creation, dispatching, cleanup

2. **`Script Utilities/PRNG.cs`**
   - Seeded random number generator
   - Used for consistent noise offsets

3. **`Celestial Body/Scripts/SphereMesh.cs`**
   - Icosphere generation (if you want to switch from cube faces)
   - Or adapt your existing `TerrainFace` system

**Exercise 5.1.3 - Create Compute Shader Structure:**
In Astral Explorer, create:

```
Assets/PlanetGenerationScripts/Compute/
  ├── Height/
  │   └── PlanetHeight.compute (your first compute shader)
  ├── Shading/
  │   └── PlanetShading.compute (for colors/biomes)
  └── Includes/
      ├── Noise.cginc (noise functions)
      └── Math.cginc (utility functions)
```

**Exercise 5.1.4 - Create ScriptableObject System:**
Create base classes similar to Solar-System:

```csharp
// Assets/PlanetGenerationScripts/ComputeShape.cs
using UnityEngine;

public abstract class ComputeShape : ScriptableObject
{
    public int seed;
    public ComputeShader heightCompute;
    
    public abstract float[] CalculateHeights(ComputeBuffer vertexBuffer);
    public abstract void ReleaseBuffers();
}
```

**Checkpoint:** What are the advantages of using ScriptableObjects for shape settings? (Hint: reusability, asset-based workflow)

---

### **Lesson 5.2: Creating Your First GPU-Based Planet**

**Learning Objectives:**
- Implement a complete GPU-based planet generator
- Integrate with Astral Explorer's existing system
- Test and debug the implementation

**Exercise 5.2.1 - Create Basic Compute Shader:**
Create `Assets/PlanetGenerationScripts/Compute/Height/PlanetHeight.compute`:

```hlsl
#pragma kernel CSMain

#include "../Includes/Noise.cginc"

StructuredBuffer<float3> vertices;
RWStructuredBuffer<float> heights;
uint numVertices;

// Noise parameters (we'll set these from C#)
float4 noiseParams[3]; // offset, layers, persistence, lacunarity, scale, elevation

[numthreads(64, 1, 1)]
void CSMain(uint id : SV_DispatchThreadID)
{
    if (id >= numVertices) return;
    
    float3 pos = vertices[id];
    
    // Simple height calculation using fractal noise
    float height = FractalNoise3D(pos, noiseParams);
    
    // Convert to height multiplier (centered around 1.0)
    heights[id] = 1.0 + (height - 0.5) * 0.1;
}
```

**Exercise 5.2.2 - Create Noise Include:**
Create `Assets/PlanetGenerationScripts/Compute/Includes/Noise.cginc`:

```hlsl
// Simplified 3D noise (you can improve this later)
float hash(float3 p)
{
    p = frac(p * 0.3183099 + 0.1);
    p *= 17.0;
    return frac(p.x * p.y * p.z * (p.x + p.y + p.z));
}

float noise3D(float3 p)
{
    float3 i = floor(p);
    float3 f = frac(p);
    f = f * f * (3.0 - 2.0 * f);
    
    float n = i.x + i.y * 57.0 + 113.0 * i.z;
    return lerp(
        lerp(lerp(hash(n + 0.0), hash(n + 1.0), f.x),
             lerp(hash(n + 57.0), hash(n + 58.0), f.x), f.y),
        lerp(lerp(hash(n + 113.0), hash(n + 114.0), f.x),
             lerp(hash(n + 170.0), hash(n + 171.0), f.x), f.y),
        f.z);
}

float FractalNoise3D(float3 pos, float4 params[3])
{
    float3 offset = params[0].xyz;
    int numLayers = (int)params[0].w;
    float persistence = params[1].x;
    float lacunarity = params[1].y;
    float scale = params[1].z;
    float elevation = params[1].w;
    
    float value = 0.0;
    float amplitude = 1.0;
    float frequency = scale;
    
    for (int i = 0; i < numLayers; i++)
    {
        float noiseValue = noise3D((pos + offset) * frequency);
        value += noiseValue * amplitude;
        
        frequency *= lacunarity;
        amplitude *= persistence;
    }
    
    return value * elevation;
}
```

**Exercise 5.2.3 - Create C# Wrapper:**
Create `Assets/PlanetGenerationScripts/GPUPlanetGenerator.cs`:

```csharp
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class GPUPlanetGenerator : MonoBehaviour
{
    [Header("Generation")]
    public int resolution = 100;
    public float radius = 1f;
    
    [Header("Compute Shader")]
    public ComputeShader heightCompute;
    
    [Header("Noise Settings")]
    public int seed = 0;
    public float noiseScale = 1f;
    public int numLayers = 4;
    public float persistence = 0.5f;
    public float lacunarity = 2f;
    public float heightMultiplier = 0.1f;
    
    Mesh mesh;
    ComputeBuffer vertexBuffer;
    ComputeBuffer heightBuffer;
    
    void Start()
    {
        GeneratePlanet();
    }
    
    void GeneratePlanet()
    {
        // Create base sphere (simplified - use your existing TerrainFace or SphereMesh)
        CreateBaseSphere();
        
        // Calculate heights on GPU
        CalculateHeightsGPU();
        
        // Apply heights
        ApplyHeights();
        
        // Update mesh
        UpdateMesh();
        
        // Cleanup
        ReleaseBuffers();
    }
    
    void CreateBaseSphere()
    {
        // Use your existing sphere generation code
        // Or adapt from SphereMesh.cs
        // For now, placeholder:
        MeshFilter mf = GetComponent<MeshFilter>();
        if (mf.sharedMesh == null)
        {
            // Create simple sphere using your existing system
        }
        mesh = mf.sharedMesh;
    }
    
    void CalculateHeightsGPU()
    {
        if (heightCompute == null)
        {
            Debug.LogError("No compute shader assigned!");
            return;
        }
        
        Vector3[] vertices = mesh.vertices;
        int vertexCount = vertices.Length;
        
        // Create buffers
        vertexBuffer = new ComputeBuffer(vertexCount, sizeof(float) * 3);
        heightBuffer = new ComputeBuffer(vertexCount, sizeof(float));
        
        // Set data
        vertexBuffer.SetData(vertices);
        
        // Set noise parameters
        PRNG prng = new PRNG(seed);
        Vector3 offset = new Vector3(prng.Value(), prng.Value(), prng.Value()) * 10000f;
        
        Vector4[] noiseParams = new Vector4[3];
        noiseParams[0] = new Vector4(offset.x, offset.y, offset.z, numLayers);
        noiseParams[1] = new Vector4(persistence, lacunarity, noiseScale, 1f);
        noiseParams[2] = Vector4.zero; // Unused for now
        
        // Set buffers and parameters
        int kernelIndex = 0;
        heightCompute.SetBuffer(kernelIndex, "vertices", vertexBuffer);
        heightCompute.SetBuffer(kernelIndex, "heights", heightBuffer);
        heightCompute.SetInt("numVertices", vertexCount);
        heightCompute.SetVectorArray("noiseParams", noiseParams);
        
        // Dispatch
        int threadGroupSize = 64;
        int numGroups = Mathf.CeilToInt(vertexCount / (float)threadGroupSize);
        heightCompute.Dispatch(kernelIndex, numGroups, 1, 1);
        
        // Get results
        float[] heights = new float[vertexCount];
        heightBuffer.GetData(heights);
        
        // Store for later use
        // (You'll need to store this in a class variable)
    }
    
    void ApplyHeights()
    {
        // Apply heights to vertices (similar to Lesson 2.2)
    }
    
    void UpdateMesh()
    {
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
    }
    
    void ReleaseBuffers()
    {
        if (vertexBuffer != null) vertexBuffer.Release();
        if (heightBuffer != null) heightBuffer.Release();
    }
    
    void OnDestroy()
    {
        ReleaseBuffers();
    }
}
```

**Checkpoint:** What errors do you expect when you first run this? How will you debug them?

---

### **Lesson 5.3: Integration and Optimization**

**Learning Objectives:**
- Integrate GPU generation with existing Astral Explorer systems
- Optimize performance
- Add LOD support

**Exercise 5.3.1 - Performance Comparison:**
1. Create a test scene with both CPU and GPU generators
2. Measure generation time at different resolutions
3. Compare:
   - CPU: `ShapeGenerator` (existing)
   - GPU: `GPUPlanetGenerator` (new)

**Expected Results:**
- CPU: Linear time increase (slower at high res)
- GPU: Much faster, scales better

**Exercise 5.3.2 - Add LOD Support:**
Adapt the LOD system from `CelestialBodyGenerator`:

```csharp
[System.Serializable]
public class LODSettings
{
    public int lod0 = 200;
    public int lod1 = 100;
    public int lod2 = 50;
    
    public int GetLODResolution(int lodLevel)
    {
        switch (lodLevel)
        {
            case 0: return lod0;
            case 1: return lod1;
            case 2: return lod2;
            default: return lod2;
        }
    }
}

// In GPUPlanetGenerator:
public LODSettings lodSettings;
Mesh[] lodMeshes;

void GenerateLODMeshes()
{
    lodMeshes = new Mesh[3];
    for (int i = 0; i < 3; i++)
    {
        int res = lodSettings.GetLODResolution(i);
        GenerateMeshAtResolution(res, ref lodMeshes[i]);
    }
}

public void SetLOD(int lodLevel)
{
    if (lodMeshes != null && lodLevel < lodMeshes.Length)
    {
        GetComponent<MeshFilter>().sharedMesh = lodMeshes[lodLevel];
    }
}
```

**Exercise 5.3.3 - Distance-Based LOD:**
Add automatic LOD switching based on camera distance:

```csharp
void Update()
{
    if (Camera.main != null)
    {
        float distance = Vector3.Distance(transform.position, Camera.main.transform.position);
        int lodLevel = CalculateLODLevel(distance);
        SetLOD(lodLevel);
    }
}

int CalculateLODLevel(float distance)
{
    // Adjust these thresholds based on your needs
    if (distance > 100f) return 2; // Lowest detail
    if (distance > 50f) return 1;  // Medium detail
    return 0; // Highest detail
}
```

**Checkpoint:** Why is LOD important for procedural planets? (Hint: think about performance and visual quality)

---

## **MODULE 6: Advanced Topics**

### **Lesson 6.1: Shading and Biomes**

**Learning Objectives:**
- Understand how shading data is generated
- Learn biome systems
- Implement color generation

**Theory:**
**Shading Data:** Additional information stored in mesh UVs (not for texture coordinates, but as data storage)

Common shading data:
- Biome type (0-1 value)
- Slope (how steep the terrain is)
- Moisture (for realistic biomes)
- Temperature (for realistic biomes)

**Exercise 6.1.1 - Examine EarthShading:**
1. Open `Celestial Body/Scripts/Shading/EarthShading.cs`
2. Notice it generates shading data in a compute shader
3. The data is stored in mesh UVs (line 241 in CelestialBodyGenerator)

4. Open `Celestial Body/Scripts/Shaders/Compute/Shading/EarthShading.compute`
5. See how it calculates:
   - Large noise (biome regions)
   - Detail noise (texture variation)
   - Warped noise (natural distortion)

**Exercise 6.1.2 - Create Simple Shading:**
Create `Assets/PlanetGenerationScripts/Compute/Shading/PlanetShading.compute`:

```hlsl
#pragma kernel CSMain

#include "../Includes/Noise.cginc"

StructuredBuffer<float3> vertices;
StructuredBuffer<float3> normals; // For slope calculation
RWStructuredBuffer<float4> shadingData;

uint numVertices;

float4 noiseParams_biome[3];
float4 noiseParams_detail[3];

[numthreads(64, 1, 1)]
void CSMain(uint id : SV_DispatchThreadID)
{
    if (id >= numVertices) return;
    
    float3 pos = vertices[id];
    float3 normal = normals[id];
    
    // Calculate biome (large-scale noise)
    float biome = FractalNoise3D(pos, noiseParams_biome);
    
    // Calculate detail (small-scale noise)
    float detail = FractalNoise3D(pos, noiseParams_detail);
    
    // Calculate slope (how steep - use normal Y component)
    float slope = 1.0 - abs(normal.y); // 0 = flat, 1 = vertical
    
    // Pack into Vector4 (stored in UVs)
    shadingData[id] = float4(biome, detail, slope, 0);
}
```

**Exercise 6.1.3 - Use Shading Data in Material:**
In your terrain shader/material, sample the UV data:

```hlsl
// In fragment shader:
float4 shadingData = tex2D(_ShadingData, input.uv);
float biome = shadingData.x;
float detail = shadingData.y;
float slope = shadingData.z;

// Blend colors based on biome and slope
float3 color = lerp(
    _GrassColor,      // Low slope
    _RockColor,       // High slope
    slope
);

color = lerp(
    color,
    _DesertColor,     // Different biome
    biome
);
```

**Checkpoint:** Why store shading data in UVs instead of calculating it in the fragment shader? (Hint: performance)

---

### **Lesson 6.2: Ocean and Water Systems**

**Learning Objectives:**
- Understand ocean level systems
- Learn how to render water
- Implement ocean shaders

**Theory:**
**Ocean System:**
1. Define ocean level (height threshold)
2. Everything below ocean level is water
3. Render water mesh/sphere at ocean level
4. Use shader to create water effects

**Exercise 6.2.1 - Examine Ocean Settings:**
1. Open `Celestial Body/Scripts/OceanSettings.cs`
2. See how it handles:
   - Ocean level (0-1, relative to min/max height)
   - Ocean material
   - Ocean radius calculation

3. In `CelestialBodyGenerator`, see:
   - Line 337-342: `GetOceanRadius()` calculates ocean size
   - Ocean is rendered as a separate sphere mesh

**Exercise 6.2.2 - Implement Simple Ocean:**
```csharp
// In GPUPlanetGenerator:
[Header("Ocean")]
public bool hasOcean = true;
[Range(0, 1)]
public float oceanLevel = 0.5f;
public Material oceanMaterial;

void GenerateOcean()
{
    if (!hasOcean) return;
    
    // Calculate ocean radius
    float minHeight = /* get from height calculation */;
    float maxHeight = /* get from height calculation */;
    float oceanRadius = Mathf.Lerp(minHeight, maxHeight, oceanLevel) * radius;
    
    // Create ocean sphere (simpler mesh, lower resolution)
    GameObject oceanObj = new GameObject("Ocean");
    oceanObj.transform.parent = transform;
    oceanObj.transform.localPosition = Vector3.zero;
    
    // Create sphere mesh for ocean
    MeshFilter mf = oceanObj.AddComponent<MeshFilter>();
    MeshRenderer mr = oceanObj.AddComponent<MeshRenderer>();
    
    // Use Unity's built-in sphere or generate simple one
    mf.sharedMesh = CreateOceanMesh(oceanRadius);
    mr.sharedMaterial = oceanMaterial;
}

Mesh CreateOceanMesh(float radius)
{
    // Create low-resolution sphere
    // (Simplified - use your sphere generation code)
    return null; // Placeholder
}
```

**Checkpoint:** Why render ocean as a separate mesh instead of part of the terrain? (Hint: different shader, different LOD needs)

---

### **Lesson 6.3: Advanced Techniques**

**Learning Objectives:**
- Learn domain warping
- Understand vertex perturbation
- Explore advanced noise combinations

**Theory:**
**Domain Warping:** Distort the input coordinates before sampling noise. Creates more natural, less repetitive patterns.

**Example:**
```hlsl
// Instead of: noise(pos)
// Use: noise(pos + warpNoise(pos) * strength)
```

This makes the noise pattern flow and curve naturally.

**Exercise 6.3.1 - Implement Domain Warping:**
Add to your noise function:

```hlsl
float WarpedNoise(float3 pos, float warpStrength)
{
    // Calculate warp offset
    float3 warp = float3(
        noise3D(pos),
        noise3D(pos + float3(100, 0, 0)),
        noise3D(pos + float3(0, 100, 0))
    );
    
    // Apply warp to position
    float3 warpedPos = pos + warp * warpStrength;
    
    // Sample noise at warped position
    return noise3D(warpedPos);
}
```

**Exercise 6.3.2 - Vertex Perturbation:**
Examine `Celestial Body/Scripts/CelestialBodyGenerator.cs` lines 209-220:

```csharp
if (body.shape.perturbVertices && body.shape.perturbCompute)
{
    // Runs a second compute shader to slightly jitter vertices
    // Makes terrain less perfectly smooth
}
```

This adds micro-detail by randomly moving vertices slightly.

**Exercise 6.3.3 - Combine Advanced Techniques:**
Create a complex terrain with:
1. Base continent noise (large features)
2. Domain-warped detail noise (natural variation)
3. Ridge noise for mountains (sharp peaks)
4. Mask to control feature placement
5. Vertex perturbation for micro-detail

**Final Project:** Create a complete planet generator with:
- GPU-based height calculation
- Multiple noise types
- Biome system
- Ocean rendering
- LOD support
- Distance-based detail

---

## **ASSESSMENT AND PRACTICE**

### **Practice Exercises:**

1. **Beginner:** Create a simple CPU-based planet with one noise layer
2. **Intermediate:** Port to GPU compute shader
3. **Advanced:** Add multiple noise types and blending
4. **Expert:** Implement full biome system with shading

### **Debugging Tips:**

1. **Compute Shader Not Running:**
   - Check kernel index matches
   - Verify buffer names match exactly
   - Check thread group size matches `[numthreads]`

2. **Wrong Results:**
   - Add debug output in C# (log min/max values)
   - Verify noise parameters are set correctly
   - Check buffer data before/after GPU

3. **Performance Issues:**
   - Profile with Unity Profiler
   - Check if buffers are being released
   - Verify LOD is working

### **Resources:**

- Unity Compute Shader Documentation
- HLSL Language Reference
- Shadertoy.com (for noise function examples)
- Book: "Real-Time Rendering" (for advanced techniques)

---

## **CONCLUSION**

You've now learned:
1. ✅ What procedural generation is and why it's useful
2. ✅ How data flows through a procedural pipeline
3. ✅ How noise functions create terrain
4. ✅ CPU vs GPU implementation approaches
5. ✅ Compute shader basics and HLSL
6. ✅ Advanced noise techniques
7. ✅ Integration with existing projects
8. ✅ Optimization and LOD systems

**Next Steps:**
- Experiment with different noise combinations
- Study the Solar-System project's advanced features
- Implement your own unique terrain features
- Share your creations and get feedback

**Remember:** Procedural generation is an art as much as a science. Experiment, iterate, and have fun creating unique worlds!

---

*This curriculum is designed to be self-paced. Take your time with each lesson, and don't move on until you understand the concepts. Good luck!*







