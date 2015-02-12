using UnityEngine;
using System.Collections;
using UnityEditor;

// Debug function to instanciate molecules in edit mode
public static class MyMenuCommands
{
    [MenuItem("My Commands/Add molecule")]
    static void FirstCommand()
    {
        GameObject.Find("Main Script").GetComponent<SimpleScript>().FireEvent();
    }
}

public class SimpleScript : MainScript
{
    public void FireEvent()
    {
        // Add a new molecule in the scene at position 0,0,0
        this.AddMoleculeInstance("2RH1", new Vector3(0, 10, 0), Quaternion.identity);
    }

    // Use this for initialization
    void Start()
    {

    }
	
	// Update is called once per frame
	void Update ()
	{
	    // Update the base class
        this.UpdateRoutine();
	}
}