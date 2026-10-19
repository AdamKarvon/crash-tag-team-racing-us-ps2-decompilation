# OpenGL Renderer & Verification Exporter Guide

This document describes the modern OpenGL rendering architecture, real-time debugging helpers, and the headless asset verification pipeline implemented inside **CTTR Online**.

---

## 🎨 OpenGL Graphics Engine (`src/renderer/`)

The real-time graphics layer utilizes standard cross-platform libraries (**GLFW**, **GLAD**, and **GLM**) operating under a strict **OpenGL 3.3 Core Profile** specification.

```
+-----------------------------------------------------------+
|                      P3DLoader                            |
|  - Parse Pure3D meshes & skeletons                        |
|  - Perform PS2 -> OpenGL Coordinate Handedness Conversion |
+-----------------------------------------------------------+
                             |
                             v
+-----------------------------------------------------------+
|                     GLRenderer                            |
|  - Generate VAO, VBO, EBO buffers on GPU                  |
|  - Compile geometry & normal-visualization shaders        |
|  - Draw static/physics-transformed mesh components        |
+-----------------------------------------------------------+
       |                     |                     |
       v                     v                     v
[ cttr_vehicle_proxy.obj ] [ cttr_vehicle_preview.ppm ] [ cttr_vehicle_report.txt ]
 (Wavefront OBJ Export)      (Bresenham Wireframe)       (Geometric Metrics)
```

### 1. GPU Memory Buffering (`BufferMesh`)
Vertices and indices are transferred to the GPU via `GLRenderer::BufferMesh`:
- **VAO (Vertex Array Object)**: Manages layout states.
- **VBO (Vertex Buffer Object)**: Stores packed `Vertex` structures (Position, Normal, UV, weights, bone indices) as `GL_STATIC_DRAW`.
- **EBO (Element Buffer Object)**: Holds 32-bit indices for optimized indexed rendering calls (`glDrawElements`).

### 2. Geometry Shader Pipeline
The engine compiles a custom shader to visualize normals directly:
- **Vertex Shader**: Translates world vertices to screen coordinates:
  $$\text{Clip Space} = M_{\text{proj}} \times M_{\text{view}} \times M_{\text{model}} \times V_{\text{local}}$$
- **Fragment Shader**: Direct normal visualization:
  $$\text{RGB Color} = (\text{Normal}_{\text{normalized}} \times 0.5) + [0.5, 0.5, 0.5]$$
  This maps raw vectors from $[-1.0, 1.0]$ to color ranges $[0.0, 1.0]$. Mesh surface orientations are rendered as vibrant RGB gradients, allowing instant diagnostic identification of inverted polygon faces or reversed winding orders.

---

## 📐 PS2 to OpenGL Coordinate Handedness Inversion

The PlayStation 2 Emotion Engine graphics architecture typically operates in a **Left-Handed coordinate space** (positive Z extending forward into the screen). PC platforms (like standard OpenGL) default to a **Right-Handed coordinate space** (positive Z extending out of the screen toward the camera).

To resolve mirrored geometry, flipped normals, and reversed triangle winding orders, the `P3DLoader` performs a XY-plane reflection (Z-axis inversion) during asset buffering:

```c++
glm::vec3 P3DLoader::ConvertPS2ToGL(const glm::vec3& ps2Pos) {
    const float SCALE = 1.0f; // Scale normalization factor
    return glm::vec3(ps2Pos.x * SCALE, ps2Pos.y * SCALE, -ps2Pos.z * SCALE);
}
```

This transforms both coordinates and lighting vectors:
- **Position Reflection**: $P_{\text{GL}} = [P_{\text{PS2, X}}, P_{\text{PS2, Y}}, -P_{\text{PS2, Z}}]$
- **Normal Reflection**: $N_{\text{GL}} = [N_{\text{PS2, X}}, N_{\text{PS2, Y}}, -N_{\text{PS2, Z}}]$

---

## 🦴 Real-Time Skeletal Debug Visualizer (`DebugDraw`)

To diagnose bone orientation issues, joint mapping faults, or locator spindle attachments, `src/renderer/DebugDraw.cpp` compiles an interactive overlay directly over the meshes:

1. **Skeletal Joints**: Renders every bone's transformation origin as a **Yellow Sphere** (`0.02f` radius).
2. **Bone Links**: Draws a solid **White Line** linking child joints back to their parent coordinate nodes, representing the physical skeleton armature.
3. **Axis Tripods**: Renders three orthogonal vectors at every joint to trace rotational axes (Red = X axis, Green = Y axis, Blue = Z axis).
4. **Locators (Attachment Anchors)**: Renders vehicle locators (wheel hubs, weapon mounts, clash drivers) as **Red Spheres** (`0.05f` radius) combined with RGB axis tripods.

---

## 🖨️ Headless Verification Exporters

During pipeline execution, the engine compiles headless diagnostic data on-demand inside `verification/` to document binary preservation metrics.

### 1. `cttr_vehicle_proxy.obj`
Exports parsed meshes into a standard, readable Wavefront OBJ file. 
- Writes vertex lists (`v X Y Z`) and normal lists (`vn X Y Z`).
- Sequentially increments vertex indices when scanning multi-mesh composites:
  $$\text{Index}_{\text{Global}} = \text{Index}_{\text{Mesh}} + \text{Vertex Base}$$
- Maps face indexes to vertex and normal indices: `f V1//N1 V2//N2 V3//N3`.

### 2. `cttr_vehicle_preview.ppm` (Bresenham Software Rasterizer)
Produces a clean, high-resolution wireframe rendering ($960 \times 540$) saved directly as an uncompressed binary **P6 Portable PixMap (.ppm)**. 

To run independently of OS display drivers or GPU window context failures, the exporter implements a dedicated software-rasterizing drawing loop:
- **Automatic Camera Bounds**: Traverses all mesh vertices to determine the absolute bounding box. Calculates spatial centers and scaling ratios to center the vehicle wireframe inside the image canvas automatically.
- **Wireframe Software Rendering**: Evaluates every mesh triangle, projects coordinate points to $2\text{D}$ raster coordinates, and draws lines connecting vertices using **Bresenham's Line Algorithm**.
- **Normal Shading**: Lines are shaded using the color map of their face normals, providing a detailed wireframe map of the vehicle.

```
Bresenham Rasterization Loop:
1. Traverse Triangles (va, vb, vc)
2. Project 3D Coordinates -> 2D Screen (x, y)
3. Calculate Normal Color = Normal * 0.5 + 0.5
4. DrawLine(va, vb, color) ---\
5. DrawLine(vb, vc, color)    +---> Pixel Plotting in PPM array
6. DrawLine(vc, va, color) ---/
```

### 3. `cttr_vehicle_report.txt`
Compiles an analytical geometric census summarizing the assets.
- Counts total submeshes, vertices, and triangles.
- Records the absolute minimum and maximum floating-point bounding volumes across all submeshes:
  ```text
  CTTR Online Verification Report
  ================================
  Meshes: 1
  Vertices: 4
  Triangles: 2
  Bounds Min: -1.0000, -1.0000, 2.0000
  Bounds Max: 1.0000, 1.0000, 2.0000
  ```
- Catalogues all individual submeshes by name alongside their localized structural complexity.
