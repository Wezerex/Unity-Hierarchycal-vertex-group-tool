# Hierarchical Vertex Group Editor for Unity

This Unity Editor tool allows you to create, manage, and edit **hierarchical vertex groups** directly in the SceneView. It provides an intuitive way to select and organize vertices from a mesh using a live viewport selection tool with real-time feedback.

---

## ✨ Features

- 🔹 Create nested (parent/child) vertex groups
- 🔹 Name and rename groups in a custom dropdown hierarchy
- 🔹 Select or remove vertices from a group using a radius-based selector
- 🔹 Visualize selected vertices and selection feedback in the SceneView
- 🔹 Efficient GPU instanced rendering for vertex feedback
- 🔹 Clean separation between logic, GUI, and rendering

---

## 🧰 Usage

### 1. **Create a Hierarchy Asset**
- Right-click in the Project window → `Create > VertexGroup`
- This creates a new `HierarchycalVertexGroup` ScriptableObject

### 2. **Assign a Mesh**
- Select a GameObject with a `MeshFilter` in the scene
- Open the **Vertex Group Editor** in the SceneView GUI
- Assign your hierarchy asset to the editor field

### 3. **Edit Groups**
- Select a group from the dropdown
- Use `Create New Group` to add child groups
- Use `Remove Group` to delete a group and its children
- Rename groups directly in the UI

### 4. **Select Vertices**
- Toggle `Edit` mode
- Hover and click in the SceneView to **add** or **remove** vertices
- Use the dynamic slider to adjust the selection radius
- Vertices are color-coded:
  - ✅ **Green** = current group
  - 🔴 **Red** = parent group
  - ⚫ **Black** = other mesh vertices

---

## 🧪 Technical Details

- Uses `Graphics.RenderMeshInstanced` with a custom shader for performant vertex rendering
- Scene interaction driven by `SceneView.duringSceneGui` and a tick-based update loop
- Selection logic uses ray-mesh triangle intersection (not colliders)
- Selection feedback drawn using a procedural wireframe sphere with `Handles.DrawWireDisc`

---

## 🔧 Requirements

- Unity 2021.3 LTS or later
- Works in **Edit Mode**
- Does **not** require URP or HDRP — compatible with built-in pipeline

---

## 📂 Folder Structure
VertexGroupTool/
├── Editor/
│ ├── VertexGroupSceneEditor.cs
│ └── BillboardVertexRenderer.cs
├── Shaders/
│ └── BillboardVertex.shader
├── Scripts/
│ └── HierarchycalVertexGroup.cs

---

## 📜 License

MIT License — free to use and modify in personal and commercial projects.

---

## 🙏 Credits

Developed by Wezerex  
Billboarding shader and selection logic inspired by standard Unity Editor tooling.

