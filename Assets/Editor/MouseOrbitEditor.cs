using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(SimpleScript))]
public class SimpleScriptEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        ((MainScript)target).CheckIfColorsChanged();
    }
}