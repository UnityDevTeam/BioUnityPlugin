using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;


[ExecuteInEditMode]
[RequireComponent(typeof(Camera))]
public class MainScript : MonoBehaviour
{
    [RangeAttribute(0, 1)]
    public float Scale = 0.5f;

    public bool ShowAtomColors = false;
    
    // Reference to the renderer script attached to the camera (linked in the editor)
    public MoleculeDisplayScript _moleculeDisplayScript;

    /*****/

    [NonSerialized]
    private int _previousSelectedMoleculeIndex = -1;

    [NonSerialized]
    private MoleculeScript _selectedMolecule = null;
    
    [NonSerialized]
    private List<Color> _moleculeColors = new List<Color>();
    
    // List of molecule names
    [NonSerialized]
    private List<string> _moleculeNames = new List<string>();

    // List of molecule objects in the scene this must not be serialized otherwise it cannot reload the scene
    [NonSerialized]
    protected List<GameObject> GameObjects = new List<GameObject>();

    /*****/

    void OnEnable()
    {
        // If the list count is different and the children count we reload the scene
        if (GameObjects.Count != transform.childCount)
        {
            ReloadScene();
        }
    }

    void ReloadScene()
    {
        Debug.Log("Reload scene: " + gameObject.name);

        GameObjects.Clear();
        _moleculeNames.Clear();
        _moleculeColors.Clear();
        
        foreach (var go in from Transform t in transform select t.gameObject)
        {
            var molecule = go.GetComponent<MoleculeScript>();
            var pdbName = go.name.Split('_')[1];

            // Check if molecule already exists
            if (!HasMoleculeType(pdbName))
            {
                Debug.Log("Load molecule : " + pdbName);
                var atoms = PdbReader.ReadPdbFile(Application.dataPath + "/Molecules/" + pdbName + ".pdb");

                // Add new molecule type
                AddMoleculeType(pdbName, molecule.Color, atoms);
            }

            molecule.Type = GetMoleculeType(pdbName);
            GameObjects.Add(go);
        }
    }
    
    public bool HasMoleculeType(string pdbName)
    {
        return _moleculeNames.IndexOf(pdbName) >= 0;
    }

    public int GetMoleculeType(string pdbName)
    {
        return _moleculeNames.IndexOf(pdbName);
    }

    public int AddMoleculeType(string pdbName, Color color, Vector4[] atoms)
    {
        // If molecule type is not present => add new type to the system
        if (HasMoleculeType(pdbName)) throw new Exception("Molecule type already exists");

        _moleculeColors.Add(color);
        _moleculeNames.Add(pdbName);
        _moleculeDisplayScript.AddMoleculeType(atoms);

        return _moleculeNames.Count - 1;
    }

    int GetUniqueId()
    {
        if (GameObjects.Count == 0) return 0;

        int uniqueId = 0;

        // Push the molecules information into the buffers
        for (int i = 0; i < GameObjects.Count; i++)
        {
            var go = GameObjects[i];
            var molecule = go.GetComponent<MoleculeScript>();
            uniqueId = Mathf.Max(molecule.Id, uniqueId);
        }

        return uniqueId + 1;
    }

    public void AddMoleculeInstance(string pdbName, Vector3 position, Quaternion rotation)
    {
        // If molecule type is not present => add new type to the system
        if (!HasMoleculeType(pdbName)) throw new Exception("Molecule type unknown");

        int uniqueId = GetUniqueId();

        // Create the game object and assign position + rotation
        var gameObject = new GameObject("Molecule_" + pdbName + "_" + uniqueId);
        gameObject.transform.parent = transform;
        gameObject.transform.position = position;
        gameObject.transform.rotation = rotation;

        // Create Molecule component and assign attributes
        var molecule = gameObject.AddComponent<MoleculeScript>();
        molecule.Id = uniqueId;
        molecule.Type = _moleculeNames.IndexOf(pdbName);
        molecule.State = (int)MolState.Normal;
        molecule.Color = _moleculeColors[molecule.Type];

        // Add game object to the list
        GameObjects.Add(gameObject);

        Debug.Log("New molecule instance: " + gameObject.name);
    }

    public void SelectMolecule(int moleculeIndex)
    {
        //Debug.Log("Set selection to: " + moleculeIndex);

        if (_moleculeDisplayScript.SelectedMolecule >= 0 &&_moleculeDisplayScript.SelectedMolecule < GameObjects.Count)
        {
            var molecule = GameObjects[moleculeIndex].GetComponent<MoleculeScript>();

            if (_selectedMolecule == null || _selectedMolecule.Id != molecule.Id)
            {
                if (_selectedMolecule != null) _selectedMolecule.State = 1;
                _selectedMolecule = molecule;
                
                molecule.State = 2;
                Selection.objects = new Object[] { molecule.gameObject };
            }
        }
        else
        {
            if (_selectedMolecule != null) _selectedMolecule.State = 1;
            _selectedMolecule = null;

            Selection.objects = new Object[] { };
        }
    }

    // The update routine
	void Update()
	{
        if (_moleculeDisplayScript.SelectedMolecule != _previousSelectedMoleculeIndex)
	    {
	        SelectMolecule(_moleculeDisplayScript.SelectedMolecule);
	        _previousSelectedMoleculeIndex = _moleculeDisplayScript.SelectedMolecule;
	    }

        // Send data to the renderer
	    SendMoleculeDataToRenderer();         
	}
    
    void SendMoleculeDataToRenderer()
    {
        // Molecule data to be transfered to the renderer, do not modify
        var positions = new Vector4[GameObjects.Count];
        var rotations = new Vector4[GameObjects.Count];
        var states = new int[GameObjects.Count];
        var types = new int[GameObjects.Count];
        var colors = _moleculeColors.ToArray();

        // Push the molecules information into the buffers
        int count = 0;
        foreach(var go in GameObjects.ToArray())
        {
            if (go == null)
            {
                GameObjects.Remove(go);
                continue;
            }

            var molecule = go.GetComponent<MoleculeScript>();

            positions[count] = go.transform.position;
            rotations[count] = Helper.QuanternionToVector4(go.transform.rotation);
            states[count] = molecule.State;
            types[count] = molecule.Type;

            count ++;
        }

        // Send mol information to the renderer
        _moleculeDisplayScript.UpdateMoleculeData(count, positions, rotations, types, states, colors, Scale, ShowAtomColors);
    }
}
