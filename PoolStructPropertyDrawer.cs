using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(CustomDictionnary))]
public class PoolStructPropertyDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

        var indent = EditorGUI.indentLevel;
        EditorGUI.indentLevel = 0;
        
        var poolItemRect = new Rect(position.x, position.y, position.width/2, position.height);
        var preInstanceAmountRect = new Rect(position.x + position.width/2, position.y, position.width/2, position.height);

        EditorGUI.PropertyField(poolItemRect, property.FindPropertyRelative("poolItem"), GUIContent.none);
        EditorGUI.PropertyField(preInstanceAmountRect, property.FindPropertyRelative("preInstanceAmount"), GUIContent.none);

        EditorGUI.indentLevel = indent;
        EditorGUI.EndProperty();
    }
}
