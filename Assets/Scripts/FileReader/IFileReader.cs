using UnityEngine;
using UnityEditor;
using System.Collections.Generic;


interface IFileReader
{
    List<BioUnit> readFile(string pathToFile);
}

