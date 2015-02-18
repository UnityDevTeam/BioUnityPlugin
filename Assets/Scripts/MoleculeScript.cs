using UnityEngine;

enum MolState
{
    Null = -1,           // Molecule will not be displayed
    Normal = 0,          // Molecule will be displayed with normal color
    Highlighted = 1      // Molecule will be displayed with highlighted color
};

public class MoleculeScript : MonoBehaviour
{
    [HideInInspector]
    [SerializeField]
    public int Id;

    //[HideInInspector]
    [SerializeField]
    public int Type;

    [HideInInspector]
    [SerializeField]
    public int State;

    [HideInInspector]
    [SerializeField]
    public Color Color;
}
