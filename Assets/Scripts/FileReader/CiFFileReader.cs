using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System;
using System.Linq;

class CifFileReader : IFileReader
{
    private List<Vector4[]> atomsSeperatedByChains;
    private List<Quaternion> subunitRotations;
    private List<Vector3> subunitPositions;

    public List<BioUnit> readFile(string pathToFile)
    {
        return null;
    }
}
