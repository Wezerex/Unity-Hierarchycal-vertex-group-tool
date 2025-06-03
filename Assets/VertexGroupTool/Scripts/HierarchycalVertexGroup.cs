using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Reflection;
using System;

[CreateAssetMenu(fileName = "VertexGroup", menuName = "VertexGroup")]
public class HierarchycalVertexGroup : ScriptableObject
{
    public VertexGroup rootGroup;  
}

[System.Serializable]
public class VertexGroup
{
    public string id = System.Guid.NewGuid().ToString(); 
    public string name;

    public List<Vector3> vertices;
    public List<int> vertexIndices;
    public List<int> triangles;        
    public List<int> localTriangles;   

    public List<Vector2> uvs;          
    public List<Vector3> normals;      
    public List<Color> colors;

    public VertexGroup parent; 
    public List<VertexGroup> children = new List<VertexGroup>();

    public VertexGroup FindGroup(string groupName)
    {
        if (name == groupName) return this;
        foreach (var child in children)
        {
            var result = child.FindGroup(groupName);
            if (result != null) return result;
        }
        return null;
    }
}

public class VertexGroupSelectorAttribute : PropertyAttribute
{
    public string sourceGroupField; 

    public VertexGroupSelectorAttribute(string sourceGroupField)
    {
        this.sourceGroupField = sourceGroupField;
    }
}

[CustomPropertyDrawer(typeof(VertexGroupSelectorAttribute))]
public class VertexGroupSelectorDrawer : PropertyDrawer
{
    private List<VertexGroup> groupList = new List<VertexGroup>();
    private List<string> groupPaths = new List<string>();

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        UnityEngine.Object targetObject = property.serializedObject.targetObject;
        FieldInfo fieldInfo = targetObject.GetType().GetField(property.name);
        if (fieldInfo == null)
        {
            EditorGUI.LabelField(position, label.text, "Field not found.");
            return;
        }

        VertexGroupReference reference = fieldInfo.GetValue(targetObject) as VertexGroupReference;
        if (reference == null)
        {
            EditorGUI.LabelField(position, label.text, "Invalid reference.");
            return;
        }

        SerializedProperty sourceGroupProperty = property.serializedObject.FindProperty(((VertexGroupSelectorAttribute)attribute).sourceGroupField);
        if (sourceGroupProperty == null || sourceGroupProperty.objectReferenceValue == null)
        {
            EditorGUI.LabelField(position, label.text, "Assign a HierarchycalVertexGroup first.");
            return;
        }

        HierarchycalVertexGroup hvg = sourceGroupProperty.objectReferenceValue as HierarchycalVertexGroup;
        if (hvg == null || hvg.rootGroup == null)
        {
            EditorGUI.LabelField(position, label.text, "Invalid HierarchycalVertexGroup.");
            return;
        }

        reference.Initialize(hvg);

        groupList.Clear();
        groupPaths.Clear();
        GenerateDropdownList(hvg.rootGroup, "", 0);

        VertexGroup currentGroup = reference.Value;
        int selectedIndex = Mathf.Max(0, groupList.IndexOf(currentGroup));

        int newIndex = EditorGUI.Popup(position, label.text, selectedIndex, groupPaths.ToArray());

        if (newIndex >= 0 && newIndex < groupList.Count)
        {
            reference.Value = groupList[newIndex];
            fieldInfo.SetValue(targetObject, reference); 

            property.serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(property.serializedObject.targetObject);
        }
    }

    private void GenerateDropdownList(VertexGroup group, string currentPath, int depth)
    {
        if (group == null) return;

        string fullPath = string.IsNullOrEmpty(currentPath) ? group.name : currentPath + "/" + group.name;
        groupPaths.Add(new string(' ', depth * 2) + group.name);
        groupList.Add(group);

        foreach (var child in group.children)
        {
            GenerateDropdownList(child, fullPath, depth + 1);
        }
    }
}

[Serializable]
public class VertexGroupReference
{
    [SerializeField] private string selectedGroupPath;

    private HierarchycalVertexGroup hvg; 
    private VertexGroup cachedGroup; 

    public void Initialize(HierarchycalVertexGroup hierarchy)
    {
        if (hvg != hierarchy) 
        {
            hvg = hierarchy;
            cachedGroup = null; 
        }
    }

    public VertexGroup Value
    {
        get
        {
            if (cachedGroup == null && hvg != null)
            {
                cachedGroup = FindGroupByPath(hvg.rootGroup, selectedGroupPath);
            }
            return cachedGroup;
        }
        set
        {
            if (value != null)
            {
                selectedGroupPath = GetGroupPath(value);
                cachedGroup = value;
            }
        }
    }

    private VertexGroup FindGroupByPath(VertexGroup root, string path)
    {
        if (string.IsNullOrEmpty(path)) return null;
        string[] parts = path.Split('/');
        return FindGroupByParts(root, parts, 0);
    }

    private VertexGroup FindGroupByParts(VertexGroup current, string[] parts, int index)
    {
        if (current == null || index >= parts.Length) return null;
        if (current.name == parts[index])
        {
            if (index == parts.Length - 1) return current;
            foreach (var child in current.children)
            {
                var found = FindGroupByParts(child, parts, index + 1);
                if (found != null) return found;
            }
        }
        return null;
    }

    private string GetGroupPath(VertexGroup group)
    {
        if (group.parent == null) return group.name;
        return GetGroupPath(group.parent) + "/" + group.name;
    }
}
