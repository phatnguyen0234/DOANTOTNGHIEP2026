using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(CraftingRecipe))]
public class CraftingRecipeEditor : Editor
{
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

    //ham ve ma tran cong thuc 3x3
    private void DrawRecipeGrid(SerializedProperty ingredientsProp)
    {
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Recipe Shape (3x3 Grid)", EditorStyles.boldLabel);

        if (ingredientsProp.arraySize != 9)
        {
            ingredientsProp.arraySize = 9;
        }

        for (int y = 0; y < 3; y++)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            for (int x = 0; x < 3; x++)
            {
                int index = y * 3 + x;
                SerializedProperty element = ingredientsProp.GetArrayElementAtIndex(index);
                SerializedProperty itemProp = element.FindPropertyRelative("item");
                SerializedProperty amountProp = element.FindPropertyRelative("amount");

                EditorGUILayout.BeginVertical("box", GUILayout.Width(100), GUILayout.Height(40));
                EditorGUILayout.PropertyField(itemProp, GUIContent.none);

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Qty:", GUILayout.Width(30));
                EditorGUILayout.PropertyField(amountProp, GUIContent.none, GUILayout.Width(50));
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.EndVertical();
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.Space(10);
    }
}