using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

[ExecuteInEditMode]
class FileReaderScript : MonoBehaviour
{
    private IFileReader pdbFileReader = new PdbFileReader();
    
    private IFileReader cifFileReader = new CifFileReader();
    
    public void readFile(String path){
        BioUnit biounit;
        
        if (path.EndsWith("cif"))
        {
            Debug.Log("Cif support coming soon!");
        }
        else if (path.EndsWith("pdb"))
        {
            biounit = pdbFileReader.readFile(path);
            Debug.Log("Pdb File loaded!");
        }
        else
        {
            Debug.Log("Filetype not supported!");
        }
    }
}
