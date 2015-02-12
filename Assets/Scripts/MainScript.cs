using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;


[ExecuteInEditMode]
[RequireComponent(typeof(Camera))]
public class MainScript : MonoBehaviour
{
    [RangeAttribute(0, 1)]
    public float Scale = 0.5f;

    public bool ShowAtomColors = false;

    /*****/

    [NonSerialized]
    private int _previousSelectedMoleculeIndex = -1;

    [NonSerialized]
    private MoleculeScript _selectedMolecule = null;

    // Reference to the renderer script attached to the camera
    private MoleculeDisplayScript _moleculeDisplayScript = null;
    
    // List of molecule names
    public List<Color> MoleculeColors = new List<Color>();

    [NonSerialized]
    private List<Color> _moleculeColors = new List<Color>();
    
    // List of molecule names
    private List<string> _moleculeNames = new List<string>();

    // List of molecule objects in the scene this must not be serialized otherwise it cannot reload the scene
    [NonSerialized]
    protected List<GameObject> GameObjects = new List<GameObject>();

    public void CheckIfColorsChanged()
    {
        if (MoleculeColors.SequenceEqual(_moleculeColors)) return;

        //Debug.Log("Color changed");
        _moleculeColors = new List<Color>(MoleculeColors);

        foreach (var molecule in GameObjects.Select(go => go.GetComponent<MoleculeScript>()))
        {
            molecule.Color = MoleculeColors[molecule.Type];
        }
    }

    int AddMoleculeType(string pdbName, Color color)
    {
        MoleculeColors.Add(color);
       _moleculeNames.Add(pdbName);
       _moleculeDisplayScript.AddMoleculeType(pdbName);

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
        if(_moleculeNames.IndexOf(pdbName) < 0)
        {
            AddMoleculeType(pdbName, new Color(UnityEngine.Random.Range(0.0f, 1.0f), UnityEngine.Random.Range(0.0f, 1.0f), UnityEngine.Random.Range(0.0f, 1.0f)));
        }

        int uniqueId = GetUniqueId();

        // Create the game object and assign position + rotation
        var gameObject = new GameObject("Molecule_" + pdbName + "_" + uniqueId);
        gameObject.transform.parent = transform;
        gameObject.transform.position = position;
        gameObject.transform.rotation = rotation;
        
        // Create Molecule component and assign attributes
        var molecule = gameObject.AddComponent<MoleculeScript>();
        molecule.MainScript = this;
        molecule.Id = uniqueId;
        molecule.Type =  _moleculeNames.IndexOf(pdbName);
        molecule.State = (int)MolState.Normal;
        molecule.Color = MoleculeColors[molecule.Type];

        // Add game object to the list
        GameObjects.Add(gameObject);

        Debug.Log("New molecule instance: " + gameObject.name);
    }

    public void NotifyDestroy(GameObject gameObject)
    {
        if (_selectedMolecule != null && gameObject.GetComponent<MoleculeScript>().Id == _selectedMolecule.Id) _selectedMolecule = null;

        GameObjects.Remove(gameObject);
        Debug.Log("Remove molecule instance: " + gameObject.name);
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
	protected void UpdateRoutine()
	{
        // Fetch reference to the display script
        if (_moleculeDisplayScript == null) _moleculeDisplayScript = GameObject.Find("Main Camera").GetComponent<MoleculeDisplayScript>();

        // If the list count is different and the children count we reload the scene
	    if (GameObjects.Count != transform.childCount) ReloadScene();

	    if (_moleculeDisplayScript.SelectedMolecule != _previousSelectedMoleculeIndex)
	    {
	        SelectMolecule(_moleculeDisplayScript.SelectedMolecule);
	        _previousSelectedMoleculeIndex = _moleculeDisplayScript.SelectedMolecule;
	    }

        // Send data to the renderer
	    SendMoleculeDataToRenderer();         
	}

    void ReloadScene()
    {
        Debug.Log("Reload scene: " + gameObject.name);

        MoleculeColors.Clear();
        GameObjects.Clear();
        _moleculeNames.Clear();

        foreach (var g in from Transform t in transform select t.gameObject)
        {
            var pdbName = g.name.Split('_')[1];

            // If molecule type is not present => add new type to the system
            if (_moleculeNames.IndexOf(pdbName) < 0)
            {
                var molecule = g.GetComponent<MoleculeScript>();
                molecule.Type = AddMoleculeType(pdbName, molecule.Color);
            }

            GameObjects.Add(g);
        }

        _moleculeColors = new List<Color>(MoleculeColors);
    }

    void SendMoleculeDataToRenderer()
    {
        // Molecule data to be transfered to the renderer, do not modify
        var positions = new Vector4[GameObjects.Count];
        var rotations = new Vector4[GameObjects.Count];
        var states = new int[GameObjects.Count];
        var types = new int[GameObjects.Count];
        var colors = MoleculeColors.ToArray();

        // Push the molecules information into the buffers
        for (int i = 0; i < GameObjects.Count; i++)
        {
            var go = GameObjects[i];
            var molecule = go.GetComponent<MoleculeScript>();

            positions[i] = go.transform.position;
            rotations[i] = Helper.QuanternionToVector4(go.transform.rotation);
            states[i] = molecule.State;
            types[i] = molecule.Type;
        }

        // Send mol information to the renderer
        _moleculeDisplayScript.UpdateMoleculeData(positions, rotations, types, states, colors, Scale, ShowAtomColors);
    }
}
