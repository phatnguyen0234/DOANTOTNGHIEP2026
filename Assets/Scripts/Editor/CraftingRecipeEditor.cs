using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(CraftingRecipe))]
public class CraftingRecipeEditor : Editor
{
    private readonly bool[] useGroupMode = new bool[9];
    private bool initializedModes = false;

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        SerializedProperty property = serializedObject.GetIterator();

        bool enterChildren = true;
        while (property.NextVisible(enterChildren))
        {
            enterChildren = false;

            if (property.name == "requiredIngredients")
            {
                DrawRecipeGrid(property);
            }
            else
            {
                if (property.name == "m_Script")
                {
                    GUI.enabled = false;
                    EditorGUILayout.PropertyField(property, true);
                    GUI.enabled = true;
                }
                else
                {
                    EditorGUILayout.PropertyField(property, true);
                }
            }
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawRecipeGrid(SerializedProperty ingredientsProp)
    {
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Recipe Shape (3x3 Grid)", EditorStyles.boldLabel);

        if (ingredientsProp.arraySize != 9)
        {
            ingredientsProp.arraySize = 9;
        }

        if (!initializedModes)
        {
            for (int i = 0; i < 9; i++)
            {
                SerializedProperty el = ingredientsProp.GetArrayElementAtIndex(i);
                SerializedProperty grp = el.FindPropertyRelative("ingredientGroup");
                useGroupMode[i] = grp != null && grp.objectReferenceValue != null;
            }
            initializedModes = true;
        }

        Color defaultBg = GUI.backgroundColor;

        for (int y = 0; y < 3; y++)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            for (int x = 0; x < 3; x++)
            {
                int index = y * 3 + x;
                SerializedProperty element = ingredientsProp.GetArrayElementAtIndex(index);
                SerializedProperty itemProp = element.FindPropertyRelative("item");
                SerializedProperty groupProp = element.FindPropertyRelative("ingredientGroup");
                SerializedProperty amountProp = element.FindPropertyRelative("amount");

                if (groupProp.objectReferenceValue != null)
                {
                    useGroupMode[index] = true;
                }

                GUI.backgroundColor = useGroupMode[index] ? new Color(0.75f, 0.92f, 1f, 1f) : defaultBg;
                EditorGUILayout.BeginVertical("box", GUILayout.Width(116), GUILayout.Height(46));
                GUI.backgroundColor = defaultBg;

                if (useGroupMode[index])
                {
                    EditorGUILayout.PropertyField(groupProp, GUIContent.none);
                    if (groupProp.objectReferenceValue != null)
                    {
                        itemProp.objectReferenceValue = null;
                    }
                }
                else
                {
                    EditorGUILayout.PropertyField(itemProp, GUIContent.none);
                    if (itemProp.objectReferenceValue != null)
                    {
                        groupProp.objectReferenceValue = null;
                    }
                }

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Qty:", GUILayout.Width(26));
                EditorGUILayout.PropertyField(amountProp, GUIContent.none, GUILayout.Width(36));

                string modeLabel = useGroupMode[index] ? "Group" : "Item";
                if (GUILayout.Button(modeLabel, EditorStyles.miniButton, GUILayout.Width(44)))
                {
                    useGroupMode[index] = !useGroupMode[index];
                    if (useGroupMode[index])
                        itemProp.objectReferenceValue = null;
                    else
                        groupProp.objectReferenceValue = null;
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.EndVertical();
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.Space(10);
    }
}