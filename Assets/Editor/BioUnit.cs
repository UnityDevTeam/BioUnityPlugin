using UnityEngine;
using System.Collections.Generic;
using UnityEditor;

class BioUnit
{
    private List<Vector4[]> chains;
    private List<Quaternion> quarternions;

    public BioUnit(List<Vector4[]> chains, List<Quaternion> quarternions)
    {
        this.chains = chains;
        this.quarternions = quarternions;
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

    public List<Quaternion> allQuarternions()
    {
        return quarternions;
    }

    public Quaternion quarternionOfOneChain(int chainIndex)
    {
        return quarternions[chainIndex];
    }
}

