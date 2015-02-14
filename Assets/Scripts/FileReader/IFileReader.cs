using UnityEngine;
using UnityEditor;


interface IFileReader
{
    BioUnit readFile(string pathToFile);
}

