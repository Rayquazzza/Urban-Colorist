using System;
using System.Collections.Generic;
using System.Reflection;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UIElements;
#endif
using UnityEngine;
using System.Linq;
using UnityEngine.UIElements;

public class MMInspectorGroupData
{
    public bool GroupIsOpen;
    public InspectorGroupAttribute GroupAttribute;
    public List<SerializedProperty> PropertiesList = new List<SerializedProperty>();
    public HashSet<string> GroupHashSet = new HashSet<string>();
    public Color GroupColor;

    public void ClearGroup()
    {
        GroupAttribute = null;
        GroupHashSet.Clear();
        PropertiesList.Clear();
    }
}

// 1. MODIFICATION : Accepter ScriptableObject ET MonoBehaviour
[CanEditMultipleObjects]
[CustomEditor(typeof(UnityEngine.Object), true)]
public class MonoBehaviourUITKEditor : Editor
{
    public StyleSheet EditorStyleSheet;

    public bool DrawerInitialized;
    public Dictionary<string, MMInspectorGroupData> GroupData;
    public List<SerializedProperty> PropertiesList;
    private bool _requiresConstantRepaint;
    private bool _requiresConstantRepaintOnlyWhenPlaying;

    // 2. MODIFICATION : Utiliser UnityEngine.Object au lieu de MonoBehaviour
    private UnityEngine.Object _targetObject;
    private bool _targetObjectIsNotNull;

    protected bool _shouldDrawBase = true;
    protected string _targetTypeName;
    private string[] _mmHiddenPropertiesToHide;
    private bool _hasMMHiddenProperties = false;

    protected virtual void OnEnable()
    {
        if (EditorStyleSheet == null)
        {
            string[] guids = AssetDatabase.FindAssets("MonoBehaviourUITKEditorStyleSheet t:StyleSheet");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                EditorStyleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(path);
            }
        }
    }

    public override bool RequiresConstantRepaint()
    {
        if (_requiresConstantRepaintOnlyWhenPlaying)
        {
            // 3. MODIFICATION : Vérification de la propriété '.enabled' uniquement si c'est un MonoBehaviour
            bool isEnabled = true;
            if (_targetObject is MonoBehaviour mb)
            {
                isEnabled = mb.enabled;
            }

            return Application.isPlaying && _targetObjectIsNotNull && isEnabled;
        }
        else
        {
            return _requiresConstantRepaint;
        }
    }

    protected virtual void Initialization()
    {
        if (DrawerInitialized && PropertiesList != null)
        {
            return;
        }

        // Bloquer l'exécution si le type sélectionné n'est ni MonoBehaviour ni ScriptableObject (ex: Material, Texture)
        if (!(target is MonoBehaviour) && !(target is ScriptableObject))
        {
            _shouldDrawBase = true;
            return;
        }

        _shouldDrawBase = true;
        GroupData = new Dictionary<string, MMInspectorGroupData>();
        PropertiesList = new List<SerializedProperty>();
        _targetTypeName = target.GetType().Name;

        _targetObject = target;
        if (_targetObject != null)
        {
            _targetObjectIsNotNull = true;
        }

        _requiresConstantRepaint = serializedObject.targetObject.GetType().GetCustomAttribute<RequiresConstantRepaintAttribute>() != null;
        _requiresConstantRepaintOnlyWhenPlaying = serializedObject.targetObject.GetType().GetCustomAttribute<RequiresConstantRepaintOnlyWhenPlayingAttribute>() != null;

        List<FieldInfo> fieldInfoList;
        InspectorGroupAttribute previousGroupAttribute = default;
        int fieldInfoLength = MonoBehaviourFieldInfo.GetFieldInfo(target, out fieldInfoList);

        for (int i = 0; i < fieldInfoLength; i++)
        {
            InspectorGroupAttribute group = Attribute.GetCustomAttribute(fieldInfoList[i], typeof(InspectorGroupAttribute)) as InspectorGroupAttribute;
            MMInspectorGroupData groupData;
            if (group == null)
            {
                if (previousGroupAttribute != null && previousGroupAttribute.GroupAllFieldsUntilNextGroupAttribute)
                {
                    _shouldDrawBase = false;
                    if (!GroupData.TryGetValue(previousGroupAttribute.GroupName, out groupData))
                    {
                        GroupData.Add(previousGroupAttribute.GroupName, new MMInspectorGroupData
                        {
                            GroupAttribute = previousGroupAttribute,
                            GroupHashSet = new HashSet<string> { fieldInfoList[i].Name },
                            GroupColor = Colors.GetColorAt(previousGroupAttribute.GroupColorIndex)
                        });
                    }
                    else
                    {
                        groupData.GroupColor = Colors.GetColorAt(previousGroupAttribute.GroupColorIndex);
                        groupData.GroupHashSet.Add(fieldInfoList[i].Name);
                    }
                }

                continue;
            }

            previousGroupAttribute = group;

            if (!GroupData.TryGetValue(group.GroupName, out groupData))
            {
                bool fallbackOpenState = true;
                if (group.ClosedByDefault) { fallbackOpenState = false; }
                bool groupIsOpen = EditorPrefs.GetBool(string.Format($"{group.GroupName}{fieldInfoList[i].Name}{target.GetInstanceID()}"), fallbackOpenState);
                GroupData.Add(group.GroupName, new MMInspectorGroupData
                {
                    GroupAttribute = group,
                    GroupColor = Colors.GetColorAt(previousGroupAttribute.GroupColorIndex),
                    GroupHashSet = new HashSet<string> { fieldInfoList[i].Name },
                    GroupIsOpen = groupIsOpen
                });
            }
            else
            {
                groupData.GroupHashSet.Add(fieldInfoList[i].Name);
                groupData.GroupColor = Colors.GetColorAt(previousGroupAttribute.GroupColorIndex);
            }
        }

        SerializedProperty iterator = serializedObject.GetIterator();

        if (iterator.NextVisible(true))
        {
            do
            {
                FillPropertiesList(iterator);
            } while (iterator.NextVisible(false));
        }
        DrawerInitialized = true;
    }

    public void FillPropertiesList(SerializedProperty serializedProperty)
    {
        bool shouldClose = false;

        foreach (KeyValuePair<string, MMInspectorGroupData> pair in GroupData)
        {
            if (pair.Value.GroupHashSet.Contains(serializedProperty.name))
            {
                SerializedProperty property = serializedProperty.Copy();
                shouldClose = true;
                pair.Value.PropertiesList.Add(property);
                break;
            }
        }

        if (!shouldClose)
        {
            SerializedProperty property = serializedProperty.Copy();
            PropertiesList.Add(property);
        }
    }

    public override VisualElement CreateInspectorGUI()
    {
        Initialization();

        VisualElement root = new VisualElement();
        if (EditorStyleSheet != null)
        {
            root.styleSheets.Add(EditorStyleSheet);
        }

        if (PropertiesList == null || PropertiesList.Count == 0 || _shouldDrawBase)
        {
            VisualElement defaultInspector = new VisualElement();
            InspectorElement.FillDefaultInspector(defaultInspector, serializedObject, this);
            root.Add(defaultInspector);
            return root;
        }

        // Draw the script field
        SerializedProperty scriptProperty = serializedObject.FindProperty("m_Script");
        if (scriptProperty != null)
        {
            PropertyField scriptField = new PropertyField(scriptProperty);
            scriptField.SetEnabled(false);
            root.Add(scriptField);
        }

        foreach (KeyValuePair<string, MMInspectorGroupData> pair in GroupData)
        {
            DrawGroup(pair.Value, root);
        }

        serializedObject.ApplyModifiedProperties();

        return root;
    }

    protected virtual void DrawGroup(MMInspectorGroupData groupData, VisualElement root)
    {
        Foldout foldout = new Foldout();
        foldout.text = groupData.GroupAttribute.GroupName;
        foldout.value = groupData.GroupIsOpen;
        foldout.AddToClassList("mm-foldout");
        foldout.style.borderLeftColor = groupData.GroupColor;
        foldout.viewDataKey = target.name + "-" + _targetTypeName + groupData.GroupAttribute.GroupName;
        root.Add(foldout);

        var toggleElement = foldout.Q<Toggle>();
        if (toggleElement != null)
        {
            toggleElement.AddToClassList("mm-foldout-toggle");
        }

        for (int i = 0; i < groupData.PropertiesList.Count; i++)
        {
            DrawChild(i, foldout, root);
        }

        void DrawChild(int index, Foldout f, VisualElement r)
        {
            if ((_hasMMHiddenProperties) && (_mmHiddenPropertiesToHide.Contains(groupData.PropertiesList[index].name)))
            {
                return;
            }
            PropertyField field = new PropertyField(groupData.PropertiesList[index]);
            field.label = ObjectNames.NicifyVariableName(groupData.PropertiesList[index].name);
            field.tooltip = groupData.PropertiesList[index].tooltip;
            f.Add(field);
        }
    }
}