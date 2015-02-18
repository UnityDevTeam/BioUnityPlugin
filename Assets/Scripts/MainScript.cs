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

    /*****/

    [NonSerialized]
    private int _previousSelectedMoleculeIndex = -1;

    [NonSerialized]
    private MoleculeScript _selectedMolecule = null;

    // Reference to the renderer script attached to the camera
    private MoleculeDisplayScript _moleculeDisplayScript = null;
    
    [NonSerialized]
    private List<Color> _moleculeColors = new List<Color>();
    
    [NonSerialized]
    private List<string> _moleculeNames = new List<string>();

    [NonSerialized]
    private List<GameObject> _gameObjects = new List<GameObject>();

    private String chainPostfix = "-chain-";

    void OnEnable()
    {
        foreach (var go in gameObject.GetComponentsInChildren<MoleculeScript>())
        {
            GameObject.DestroyImmediate(go.gameObject);
        }
    }

    public void AddBiounitIgnoreChains(BioUnit biounit)
    {
        AddMoleculeTypeIfNotAlreadyPresent(biounit, false, getRandomColor());
        AddMoleculeInstancesForBiounit(biounit, false);
    }

    public void AddBiounit(BioUnit biounit)
    {
        AddMoleculeTypeIfNotAlreadyPresent(biounit, true, getRandomColor());
        AddMoleculeInstancesForBiounit(biounit, true);
    }

    private void AddMoleculeInstancesForBiounit(BioUnit biounit, Boolean useChains)
    {
        String name = biounit.Name;
        var rotations = biounit.allRotations();
        var positions = biounit.allPositions();

        if (positions.Count != rotations.Count) throw new Exception("Rotation- and positioncounts don't match!");

        int amountOfSubunits = rotations.Count;

        if (useChains)
        {
            AddMoleculeInstancesForBiounitUseChains(name + chainPostfix, positions, rotations, amountOfSubunits, biounit.AmountOfChains);
        }
        else
        {
            AddMoleculeInstancesForBiounit(name, positions, rotations, amountOfSubunits);
        }
    }

    private void AddMoleculeInstancesForBiounit(String name, List<Vector3> positions, List<Quaternion> rotations, int amountOfSubunits)
    {
        for (int i = 0; i < amountOfSubunits; i++)
        {
            AddMoleculeInstance(name, positions[i], rotations[i]);
        }
    }

    private void AddMoleculeInstancesForBiounitUseChains(String name, List<Vector3> positions, List<Quaternion> rotations, int amountOfSubunits, int amountOfChains)
    {
        for (int i = 0; i < amountOfSubunits; i++)
        {
            for (int j = 1; j <= amountOfChains; j++)
            {
                AddMoleculeInstance(name+j, positions[i], rotations[i]);
            }
        }
    }

    private void AddMoleculeTypeIfNotAlreadyPresent(BioUnit biounit, Boolean withChains, Color color)
    {

        Debug.Log("Check if the biounit subunit already present!");

        String namePostfix = "";
        if (withChains)
        {
            namePostfix = chainPostfix + "1";
        }

        if (HasMoleculeType(biounit.Name + namePostfix)) return;

        Debug.Log("Add subunit typs!");

        if (withChains)
        {
            addMoleculeTypeWithChains(biounit);
            return;
        }
        AddMoleculeType(biounit.Name, color, biounit.allAtomPositions());
        return;
    }

    private Color getRandomColor()
    {
        return new Color(Random.Range(0.0f, 1.0f), Random.Range(0.0f, 1.0f), Random.Range(0.0f, 1.0f));
    }

    private void addMoleculeTypeWithChains(BioUnit biounit)
    {
        int i = 1;
        foreach (var chainAtoms in biounit.atomPositionsSeperatedByChain())
        {
            AddMoleculeType(biounit.Name + chainPostfix + i, getRandomColor(), chainAtoms);

            i++;
        }
    }

    private int GetUniqueId()
    {
        if (_gameObjects.Count == 0) return 0;

        int uniqueId = 0;

        // Push the molecules information into the buffers
        for (int i = 0; i < _gameObjects.Count; i++)
        {
            var go = _gameObjects[i];
            var molecule = go.GetComponent<MoleculeScript>();
            uniqueId = Mathf.Max(molecule.Id, uniqueId);
        }

        return uniqueId + 1;
    }

    public bool HasMoleculeType(string pdbName)
    {
        return _moleculeNames.IndexOf(pdbName) >= 0;
    }

    public void AddMoleculeType(string pdbName, Color color, Vector4[] atoms)
    {
        // If molecule type is not present => add new type to the system
        if (HasMoleculeType(pdbName)) throw new Exception("Molecule type already exists");

       _moleculeColors.Add(color);
       _moleculeNames.Add(pdbName);
       _moleculeDisplayScript.AddMoleculeType(atoms);
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
        molecule.MainScript = this;
        molecule.Id = uniqueId;
        molecule.Type =  _moleculeNames.IndexOf(pdbName);
        molecule.State = (int)MolState.Normal;
        molecule.Color = _moleculeColors[molecule.Type];

        // Add game object to the list
        _gameObjects.Add(gameObject);

        Debug.Log("New molecule instance: " + gameObject.name);
    }

    public void NotifyDestroy(GameObject gameObject)
    {
        if (_selectedMolecule != null && gameObject.GetComponent<MoleculeScript>().Id == _selectedMolecule.Id) _selectedMolecule = null;

        _gameObjects.Remove(gameObject);
        Debug.Log("Remove molecule instance: " + gameObject.name);
    }

    // The update routine
	protected void UpdateRoutine()
	{
        // Fetch reference to the display script
        if (_moleculeDisplayScript == null) _moleculeDisplayScript = GameObject.Find("Main Camera").GetComponent<MoleculeDisplayScript>();
        
	    if (_moleculeDisplayScript.SelectedMolecule != _previousSelectedMoleculeIndex)
	    {
	        SelectMolecule(_moleculeDisplayScript.SelectedMolecule);
	        _previousSelectedMoleculeIndex = _moleculeDisplayScript.SelectedMolecule;
	    }

        // Send data to the renderer
	    SendMoleculeDataToRenderer();         
	}

    public void SelectMolecule(int moleculeIndex)
    {
        //Debug.Log("Set selection to: " + moleculeIndex);

        if (_moleculeDisplayScript.SelectedMolecule >= 0 && _moleculeDisplayScript.SelectedMolecule < _gameObjects.Count)
        {
            var molecule = _gameObjects[moleculeIndex].GetComponent<MoleculeScript>();

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

    void SendMoleculeDataToRenderer()
    {
        // Molecule data to be transfered to the renderer, do not modify
        var positions = new Vector4[_gameObjects.Count];
        var rotations = new Vector4[_gameObjects.Count];
        var states = new int[_gameObjects.Count];
        var types = new int[_gameObjects.Count];
        var colors = _moleculeColors.ToArray();

        // Push the molecules information into the buffers
        for (int i = 0; i < _gameObjects.Count; i++)
        {
            var go = _gameObjects[i];
            var molecule = go.GetComponent<MoleculeScript>();

            positions[i] = go.transform.position*Scale;
            rotations[i] = Helper.QuanternionToVector4(go.transform.rotation);
            states[i] = molecule.State;
            types[i] = molecule.Type;
        }

        // Send mol information to the renderer
        _moleculeDisplayScript.UpdateMoleculeData(positions, rotations, types, states, colors, Scale, ShowAtomColors);
    }
}
