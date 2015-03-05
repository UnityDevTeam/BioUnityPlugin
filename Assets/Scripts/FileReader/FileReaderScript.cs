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

    }

    public void useChains(Boolean useChains)
    {
        this.UseChains = useChains;
    }

    public void readFile(String path){
        List<BioUnit> biounits = null;
        
        if (path.EndsWith("cif"))
        {
            Debug.Log("Cif support coming soon!");
        }
        else if (path.EndsWith("pdb"))
        {
            biounits = pdbFileReader.readFile(path);
            Debug.Log("Pdb File loaded!");
        }
        else
        {
            Debug.Log("Filetype not supported!");
            return;
        }

        Debug.Log("Support for multiple biounits will follow!");
        addBiounitToScene(biounits[0]);
    }

    private void addBiounitToScene(BioUnit biounit)
    {
        if (mainscript == null) mainscript = GameObject.FindObjectOfType<MainScript>();

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
