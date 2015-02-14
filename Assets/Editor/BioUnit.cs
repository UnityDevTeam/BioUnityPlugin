using UnityEngine;
using System.Collections.Generic;
using UnityEditor;

class BioUnit
{
    private List<Vector4[]> chains;
    private List<Quaternion> rotations;
    private List<Vector3> positions;

    public BioUnit(List<Vector4[]> chains, List<Quaternion> rotations, List<Vector3> positions)
    {
        this.chains = chains;
        this.rotations = rotations;
        this.positions = positions;
    }

    public Vector4[] allAtomPositions()
    {
        //Calculate Array size
        int size = 0;
        foreach (var array in chains)
        {
            size = size + array.Length;
        }

        //Create and fill atoms Array
        int position = 0;
        Vector4[] atoms = new Vector4[size];
        foreach (var array in chains)
        {
            foreach (var atom in array)
            {
                atoms[position] = atom;
                position++;
            }
        }

        return atoms;
    }

    public Vector4[] atomsOfOneChain(int chainIndex){
        return chains[chainIndex];
    }

    public List<Vector4[]> atomPositionsSeperatedByChain()
    {
        return chains;
    }

    public List<Quaternion> allRotations()
    {
        return rotations;
    }

    public Quaternion rotationOfSubunitWithIndex(int subunitIndex)
    {
        return rotations[subunitIndex];
    }

    public List<Vector3> allPositions()
    {
        return positions;
    }

    public Vector3 positionOfSubunitWithIndex(int subunitIndex)
    {
        return positions[subunitIndex];
    }
}

