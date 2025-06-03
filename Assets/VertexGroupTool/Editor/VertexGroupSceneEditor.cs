using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

[CustomEditor(typeof(MeshFilter))]
public class VertexGroupSceneEditor : Editor
{
    private static HierarchycalVertexGroup selectedGroup;
    private static bool addMode = true;
    private static MeshFilter activeMeshFilter;

    private int selectedIndex = 0;
    private List<VertexGroup> groupList = new List<VertexGroup>();
    private List<string> groupNames = new List<string>();
    private VertexGroup activeVertexGroup;

    private float selectionRadius = .5f;

    private bool edit = false;
    private bool initialized = false;

    private static CancellationTokenSource cancelSource;

    private void OnEnable()
    {
        LoadEditorPrefs();
        SceneView.duringSceneGui += CustomOnSceneGUI;
        SceneView.duringSceneGui += DrawVertices;
        activeMeshFilter = target as MeshFilter;

        StartTicking();
    }

    private void OnDisable()
    {
        SaveEditorPrefs();
        SceneView.duringSceneGui -= CustomOnSceneGUI;
        SceneView.duringSceneGui -= DrawVertices;
        initialized = false;

        StopTicking();
    }

    private void StartTicking()
    {
        if (edit) return;
        edit = true;
        cancelSource = new CancellationTokenSource();
        TickLoop(cancelSource.Token);
    }

    private void StopTicking()
    {
        if (!edit) return;
        cancelSource.Cancel();
        edit = false;
    }

    private async void TickLoop(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            await Task.Delay(16); // ~60 FPS

            if (edit && activeMeshFilter != null && activeVertexGroup != null)
            {
                UpdateSelectionRay();
            }
        }
    }

    Vector3 lastHit;
    Vector3 sceneMousePos;

    private bool mouseHeld = false;
    private bool mouseClicked = false;

    private void UpdateSelectionRay()
    {
        Ray ray = HandleUtility.GUIPointToWorldRay(sceneMousePos);
        Vector3? hitPoint = GetMeshSurfacePoint(ray);

        if (hitPoint.HasValue)
        {
            lastHit = hitPoint.Value;

            if (mouseHeld || mouseClicked)
            {
                var selected = FindVerticesInRange(activeMeshFilter, lastHit, selectionRadius);
                ModifyVertexGroup(activeVertexGroup, selected, addMode);
            }

            SceneView.RepaintAll(); // trigger wire sphere draw
        }
    }

    private void DrawWireSphere(Vector3 position, float radius, Color color)
    {
        Handles.color = color;

        // Draw 3 orthogonal circles
        Handles.DrawWireDisc(position, Vector3.up, radius);    // XY plane
        Handles.DrawWireDisc(position, Vector3.right, radius); // YZ plane
        Handles.DrawWireDisc(position, Vector3.forward, radius); // XZ plane
    }

    private void LoadEditorPrefs()
    {
        string guid = EditorPrefs.GetString("SelectedVertexGroupGUID", "");
        if (!string.IsNullOrEmpty(guid))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            selectedGroup = AssetDatabase.LoadAssetAtPath<HierarchycalVertexGroup>(path);
        }
    }

    private void SaveEditorPrefs()
    {
        if (selectedGroup != null)
        {
            string path = AssetDatabase.GetAssetPath(selectedGroup);
            string guid = AssetDatabase.AssetPathToGUID(path);
            EditorPrefs.SetString("SelectedVertexGroupGUID", guid);
        }
    }

    private void GenerateDropdownList(VertexGroup rootGroup, int depth)
    {
        if (rootGroup == null) return;

        string indent = new string(' ', depth * 2);
        groupNames.Add(indent + rootGroup.name);
        groupList.Add(rootGroup);

        foreach (var child in rootGroup.children)
        {
            GenerateDropdownList(child, depth + 1);
        }
    }

    private void DrawVertices(SceneView sceneView)
    {
        if (edit)
        {
            DrawVertexPoints(activeMeshFilter, activeVertexGroup);
            if (lastHit != Vector3.zero)
            {
                DrawWireSphere(lastHit, selectionRadius, Color.cyan);
            }
            if (edit && Event.current != null)
            {
                Event e = Event.current;

                // Handle click/drag logic
                if (e.type == EventType.MouseDown && e.button == 0)
                    mouseClicked = true;

                if (e.type == EventType.MouseDrag && e.button == 0)
                    mouseHeld = true;

                if (e.type == EventType.MouseUp && e.button == 0)
                {
                    mouseHeld = false;
                    mouseClicked = false;
                }

                sceneMousePos = e.mousePosition;

                // Prevent default box selection behavior
                if ((e.type == EventType.MouseDown || e.type == EventType.MouseDrag) && e.button == 0)
                {
                    e.Use(); // Consume the event
                }
            }
        }
    }

    private void CustomOnSceneGUI(SceneView sceneView)
    {
        if (!selectedGroup) edit = false;
        else edit = true;

        if (!edit)
        {
            GUILayout.BeginArea(new Rect(10, Screen.height - 20, 250, 220), EditorStyles.helpBox);
            selectedGroup = (HierarchycalVertexGroup)EditorGUILayout.ObjectField("Vertex Group", selectedGroup, typeof(HierarchycalVertexGroup), false);
            GUILayout.EndArea();
            return;
        }

        if(!initialized)
        {
            SetStyle();
            initialized = true;
        }

        Handles.BeginGUI();
        GUILayout.BeginArea(new Rect(10, Screen.height - 300, 250, 220), EditorStyles.helpBox);

        GUILayout.Label("Vertex Group Editor", EditorStyles.boldLabel);

        selectedGroup = (HierarchycalVertexGroup)EditorGUILayout.ObjectField("Vertex Group", selectedGroup, typeof(HierarchycalVertexGroup), false);

        if (selectedGroup == null || selectedGroup.rootGroup == null)
        {
            GUILayout.Label("Select a Vertex Group", EditorStyles.helpBox);
            GUILayout.EndArea();
            Handles.EndGUI();
            return;
        }

        if (activeMeshFilter == null || activeMeshFilter.gameObject != Selection.activeGameObject)
        {
            activeMeshFilter = Selection.activeGameObject?.GetComponent<MeshFilter>() ?? target as MeshFilter;
            if (activeMeshFilter == null)
            {
                Handles.EndGUI();
                return;
            }
        }

        if (activeMeshFilter.sharedMesh == null)
        {
            Debug.LogWarning("MeshFilter has no mesh assigned!");
            Handles.EndGUI();
            return;
        }

        groupList.Clear();
        groupNames.Clear();
        GenerateDropdownList(selectedGroup.rootGroup, 0);

        selectedIndex = EditorGUILayout.Popup("Active Group", selectedIndex, groupNames.ToArray());
        activeVertexGroup = groupList[selectedIndex];

        GUILayout.Space(10);

        GUILayout.Label("Group Name:");
        string newName = EditorGUILayout.TextField(activeVertexGroup.name);

        if (newName != activeVertexGroup.name)
        {
            activeVertexGroup.name = newName;
            EditorUtility.SetDirty(selectedGroup);
            SceneView.RepaintAll(); 
        }

        GUILayout.Space(10);

        if (GUILayout.Button("Create New Group"))
        {
            CreateNewGroup(activeVertexGroup);
            SceneView.RepaintAll(); 
        }

        if (GUILayout.Button("Remove Group"))
        {
            RemoveGroup(activeVertexGroup);
            SceneView.RepaintAll(); 
        }

        GUILayout.Space(10);

        EditorGUILayout.BeginHorizontal();
        GUI.enabled = activeVertexGroup != null;

        if (GUILayout.Toggle(addMode, "Add Mode", "Button"))
            addMode = true;
        if (GUILayout.Toggle(!addMode, "Remove Mode", "Button"))
            addMode = false;

        GUI.enabled = true;
        EditorGUILayout.EndHorizontal();

        GUILayout.Space(10);

        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("Selection Radius");
        selectionRadius = DrawDynamicSlider(selectionRadius);
        EditorGUILayout.EndHorizontal();

        GUILayout.EndArea();
        Handles.EndGUI();
    }

    private void CreateNewGroup(VertexGroup parentGroup)
    {
        VertexGroup newGroup = new VertexGroup
        {
            name = "New Group",
            id = System.Guid.NewGuid().ToString()
        };

        parentGroup.children.Add(newGroup);
        newGroup.parent = parentGroup;

        EditorUtility.SetDirty(selectedGroup);
        Debug.Log($"Created new group under {parentGroup.name}");
    }

    private void RemoveGroup(VertexGroup groupToRemove)
    {
        if (groupToRemove.parent != null)
        {
            groupToRemove.parent.children.Remove(groupToRemove);
        }

        Debug.Log($"Removed {groupToRemove.name} and its children.");
        EditorUtility.SetDirty(selectedGroup);
    }

    private void DrawVertexPoints(MeshFilter meshFilter, VertexGroup currentGroup)
    {
        if (meshFilter == null || meshFilter.sharedMesh == null || currentGroup == null)
            return;

        Mesh mesh = meshFilter.sharedMesh;
        Vector3[] vertices = mesh.vertices;

        HashSet<int> parentGroupVertices = currentGroup.parent != null
            ? new HashSet<int>(currentGroup.parent.vertexIndices)
            : new HashSet<int>();

        List<int> allVertexIndices = new List<int>();
        for (int i = 0; i < vertices.Length; i++) allVertexIndices.Add(i);

        HashSet<int> currentGroupVertices = new HashSet<int>(currentGroup.vertexIndices);
        List<int> otherIndices = allVertexIndices.FindAll(i => !currentGroupVertices.Contains(i) && !parentGroupVertices.Contains(i));
        BillboardVertexRenderer.DrawVertexIndices(meshFilter, otherIndices, Color.black, 0.015f);

        BillboardVertexRenderer.DrawVertexIndices(meshFilter, parentGroupVertices, Color.red, 0.02f);

        BillboardVertexRenderer.DrawVertexIndices(meshFilter, currentGroupVertices, Color.green, 0.025f);
    }

    public List<int> FindVerticesInRange(MeshFilter meshFilter, Vector3 selectionPoint, float selectionRadius)
    {
        Mesh mesh = meshFilter.sharedMesh;
        Vector3[] vertices = mesh.vertices;
        List<int> selectedVertices = new List<int>();

        float selectionRadiusSqr = selectionRadius * selectionRadius; 

        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3 worldPos = meshFilter.transform.TransformPoint(vertices[i]);
            if ((worldPos - selectionPoint).sqrMagnitude <= selectionRadiusSqr)
            {
                selectedVertices.Add(i);
            }
        }
        return selectedVertices;
    }

    public void ModifyVertexGroup(VertexGroup currentGroup, List<int> vertexIndices, bool isAdding)
    {
        if (vertexIndices.Count == 0) return;

        if (isAdding)
        {
            foreach (int index in vertexIndices)
            {
                if (!currentGroup.vertexIndices.Contains(index))
                    currentGroup.vertexIndices.Add(index);
            }
        }
        else
        {
            currentGroup.vertexIndices.RemoveAll(index => vertexIndices.Contains(index));
        }

        EditorUtility.SetDirty(selectedGroup);
    }

    private bool updateMinMax = true;
    private Vector2 minMax;
    private GUIStyle sliderStyle;
    private GUIStyle thumbStyle;

    private float DrawDynamicSlider(float value, float minFactor = 0.5f, float maxFactor = 2f)
    {
        if (value == minMax.x || value == minMax.y) updateMinMax = true;

        if (updateMinMax)
        {
            minMax.x = value * minFactor;
            minMax.y = value * maxFactor;
            updateMinMax = false;
        }
        
        value = GUILayout.HorizontalSlider(value, minMax.x, minMax.y, sliderStyle, thumbStyle);

        return value; 
    }

    private void SetStyle()
    {
        sliderStyle = new GUIStyle(GUI.skin.horizontalScrollbar);
        sliderStyle.fixedHeight = 5;
        sliderStyle.margin = new RectOffset(0, 0, 8, -3);

        thumbStyle = new GUIStyle(GUI.skin.horizontalSliderThumb);
        thumbStyle.fixedWidth = 10;
        thumbStyle.fixedHeight = 10;
        thumbStyle.margin = new RectOffset(0, 0, -3, 0);
    }

    private Vector3? GetMeshSurfacePoint(Ray ray)
    {
        Mesh mesh = activeMeshFilter.sharedMesh;
        Transform t = activeMeshFilter.transform;

        int[] tris = mesh.triangles;
        Vector3[] verts = mesh.vertices;

        float closestDistance = float.MaxValue;
        Vector3? closestHit = null;

        for (int i = 0; i < tris.Length; i += 3)
        {
            Vector3 v0 = t.TransformPoint(verts[tris[i]]);
            Vector3 v1 = t.TransformPoint(verts[tris[i + 1]]);
            Vector3 v2 = t.TransformPoint(verts[tris[i + 2]]);

            if (IntersectRayTriangle(ray, v0, v1, v2, out Vector3 hit))
            {
                float dist = Vector3.Distance(ray.origin, hit);
                if (dist < closestDistance)
                {
                    closestDistance = dist;
                    closestHit = hit;
                }
            }
        }

        return closestHit;
    }

    private static bool IntersectRayTriangle(Ray ray, Vector3 v0, Vector3 v1, Vector3 v2, out Vector3 hit)
    {
        hit = Vector3.zero;
        Vector3 e1 = v1 - v0;
        Vector3 e2 = v2 - v0;

        Vector3 p = Vector3.Cross(ray.direction, e2);
        float det = Vector3.Dot(e1, p);

        if (Mathf.Abs(det) < 0.0001f) return false;

        float invDet = 1f / det;
        Vector3 t = ray.origin - v0;
        float u = Vector3.Dot(t, p) * invDet;
        if (u < 0 || u > 1) return false;

        Vector3 q = Vector3.Cross(t, e1);
        float v = Vector3.Dot(ray.direction, q) * invDet;
        if (v < 0 || u + v > 1) return false;

        float d = Vector3.Dot(e2, q) * invDet;
        if (d < 0) return false;

        hit = ray.origin + ray.direction * d;
        return true;
    }
}
