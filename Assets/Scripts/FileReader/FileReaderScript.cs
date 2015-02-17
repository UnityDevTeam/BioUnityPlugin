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

    private MainScript mainscript;

    private Boolean UseChains = false;

    void Start()
    {
        mainscript = GameObject.FindObjectOfType<MainScript>();
    }

    public void useChains(Boolean useChains)
    {
        this.UseChains = useChains;
    }

    public void readFile(String path){
        BioUnit biounit = null;
        
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
            return;
        }

        addBiounitToScene(biounit);
    }

    private void addBiounitToScene(BioUnit biounit)
    {
        if (UseChains)
        {
            Debug.Log("Biounit will be added to Scene.");
            mainscript.AddBiounit(biounit);
        }
        else
        {
            Debug.Log("Biounit will be added to Scene. Chains suppressed");
            mainscript.AddBiounitIgnoreChains(biounit);
        }
    }
}
