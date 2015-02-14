using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

public class MoleculeManagerWindow : EditorWindow {

    string pathToFile = string.Empty;
    int selected = 1;

    // Add menu item named "My Window" to the Window menu
    [MenuItem("Window/MoleculeManager")]
    public static void ShowWindow()
    {
        //Show existing window instance. If one doesn't exist, make one.
        EditorWindow.GetWindow(typeof(MoleculeManagerWindow));
    }

    private static void space()
    {
        GUILayout.Space(10);
    }

    void OnGUI()
    {
        /*GUILayout.Label("Import properties", EditorStyles.boldLabel);

        space();

        GUILayout.Label("How much should be loaded?");
        var radioButtonText = new string[] { "only one symmetrical unit", "asymetrical unit", "biounit"};
        selected = GUILayout.SelectionGrid(selected, radioButtonText, 3, EditorStyles.radioButton);

        space();*/

        GUILayout.Label("Add Molecule", EditorStyles.boldLabel);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Open File"))
        {
            pathToFile = EditorUtility.OpenFilePanel("Open FILE", "", "pdb");
        }
        if (GUILayout.Button("Download File"))
        {
            //Todo
        }
        GUILayout.EndHorizontal();

        space();

        GUILayout.Label("Remove Molecules", EditorStyles.boldLabel);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Remove All"))
        {
            //Todo
        }
        GUILayout.EndHorizontal();
    }    
}
