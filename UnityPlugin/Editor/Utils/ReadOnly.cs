using UnityEditor;
using UnityEngine;

namespace UniVCC
{
    public class ShowOnlyAttribute : PropertyAttribute { }

    [CustomPropertyDrawer(typeof(ShowOnlyAttribute))]
    public class ShowOnlyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            GUI.enabled = false;
            EditorGUI.LabelField(position, property.stringValue);
            GUI.enabled = true;
        }
    }
}