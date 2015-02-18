using UnityEngine;
using System.Collections;
using UnityEditor;

// Debug function to instanciate molecules in edit mode
public static class MyMenuCommands
{
    [MenuItem("My Commands/Add molecule &a")]
    static void FirstCommand()
    {
        var pdbName = new[] { "2RH1", "1OKC", "2K4T", "2FP4", "2OAU", "p3" };
        GameObject.Find("Main Script").GetComponent<SimpleScript>().AddMolecule(pdbName[Random.Range(0, 6)]);
    }
}

public class SimpleScript : MainScript
{
    public void AddMolecule(string pdbName)
    {
        // Check if molecule already exists
        if (!HasMoleculeType(pdbName))
        {
            Debug.Log("Load molecule : " + pdbName);
            var atoms = PdbReader.ReadPdbFile(Application.dataPath + "/Molecules/" + pdbName + ".pdb");

            // Add new molecule type
            AddMoleculeType(pdbName, new Color(Random.Range(0.0f, 1.0f), Random.Range(0.0f, 1.0f), Random.Range(0.0f, 1.0f)), atoms);
        }

        this.AddMoleculeInstance(pdbName, new Vector3(Random.Range(-25, 25), Random.Range(-25, 25) + 25, Random.Range(-25, 25)) * 2, Random.rotation);
    }
}