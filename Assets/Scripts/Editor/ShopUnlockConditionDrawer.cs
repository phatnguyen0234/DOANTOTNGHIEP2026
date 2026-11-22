using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(ShopUnlockCondition))]
public class ShopUnlockConditionDrawer : PropertyDrawer
{
    private static readonly GUIContent DaysLabel = new GUIContent(
        "Required Days Played",
        "Minimum in-game days elapsed required before this item unlocks."
    );

    private static readonly GUIContent MaterialLabel = new GUIContent(
        "Required Material",
        "Material item the player must obtain before this item unlocks."
    );

    private static readonly GUIContent HolidayLabel = new GUIContent(
        "Holiday Event ID",
        "Leave empty to allow ANY holiday, or enter an eventId from TimeLightingConfig (e.g., 'lunar_new_year', 'mid_autumn')."
    );

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        SerializedProperty conditionTypeProp = property.FindPropertyRelative("conditionType");
        ShopUnlockConditionType conditionType = (ShopUnlockConditionType)conditionTypeProp.enumValueIndex;

        float lineHeight = EditorGUIUtility.singleLineHeight;
        float spacing = EditorGUIUtility.standardVerticalSpacing;

        // Only 1 line when 'None' is selected; 2 lines when a condition requires a parameter
        return conditionType == ShopUnlockConditionType.None
            ? lineHeight
            : (lineHeight * 2f) + spacing;
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        SerializedProperty conditionTypeProp = property.FindPropertyRelative("conditionType");
        float lineHeight = EditorGUIUtility.singleLineHeight;
        float spacing = EditorGUIUtility.standardVerticalSpacing;

        // Line 1: Draw the dropdown directly under the 'Unlock Condition' label (no extra nested foldout)
        Rect dropdownRect = new Rect(position.x, position.y, position.width, lineHeight);
        EditorGUI.PropertyField(dropdownRect, conditionTypeProp, label);

        // Line 2: Draw only the relevant setting field for the selected condition
        ShopUnlockConditionType conditionType = (ShopUnlockConditionType)conditionTypeProp.enumValueIndex;
        if (conditionType != ShopUnlockConditionType.None)
        {
            Rect detailRect = new Rect(
                position.x,
                position.y + lineHeight + spacing,
                position.width,
                lineHeight
            );

            EditorGUI.indentLevel++;

            switch (conditionType)
            {
                case ShopUnlockConditionType.MinDaysPlayed:
                    EditorGUI.PropertyField(
                        detailRect,
                        property.FindPropertyRelative("requiredDaysPlayed"),
                        DaysLabel
                    );
                    break;

                case ShopUnlockConditionType.RequiredMaterialObtained:
                    EditorGUI.PropertyField(
                        detailRect,
                        property.FindPropertyRelative("requiredMaterial"),
                        MaterialLabel
                    );
                    break;

                case ShopUnlockConditionType.HolidayOnly:
                    EditorGUI.PropertyField(
                        detailRect,
                        property.FindPropertyRelative("requiredHolidayEventId"),
                        HolidayLabel
                    );
                    break;
            }

            EditorGUI.indentLevel--;
        }

        EditorGUI.EndProperty();
    }
}