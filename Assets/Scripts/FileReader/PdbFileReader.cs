using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System;
using System.Linq;

class PdbFileReader : IFileReader
{
    private List<Vector4[]> atomsSeperatedByChains;
    private List<Quaternion> subunitRotations;
    private List<Vector3> subunitPositions;
    private String searchRegexForRotationMatrices = @"^REMARK\s350\s\s\sBIOMT";

    private String[] linesOfFile;

    public BioUnit readFile(string pathToFile)
    {
        if (!File.Exists(pathToFile)) throw new Exception("Pdb file not found");

        initializeVariables();

        readLinesOfFile(pathToFile);
        loadRotationsAndPositions();
        loadAtoms();

        return new BioUnit(atomsSeperatedByChains, subunitRotations, subunitPositions, readNameOutOfPath(pathToFile));
    }

    private String readNameOutOfPath(String path)
    {
        String[] splittedPath = path.Split(new Char[]{'/'});
        String fileNameWithFiletype = splittedPath[splittedPath.Length - 1];
        String filename = fileNameWithFiletype.Substring(0, fileNameWithFiletype.Length - 4);

        Debug.Log("Filename: " + filename);

        return filename;
    }

    private void initializeVariables()
    {
        atomsSeperatedByChains = new List<Vector4[]>();
        subunitPositions = new List<Vector3>();
        subunitRotations = new List<Quaternion>();
    }

    private void readLinesOfFile(String path){
        linesOfFile = File.ReadAllLines(path);
    }

    private Boolean checkLine(String line)
    {
        return System.Text.RegularExpressions.Regex.IsMatch(line, searchRegexForRotationMatrices);
    }

    private void loadRotationsAndPositions(){
        Debug.Log("Begin loading of Rotations and Positions!");
        int i = 0;
        Matrix4x4 matrix = Matrix4x4.identity;
        Vector3 position = new Vector3();
        
        foreach (var line in linesOfFile)
        {

            if (checkLine(line))
            {
                var split = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
//                Debug.Log("Row split in " + split.Length + " parts");
                var values = split.Where(s => s.Contains(".")).ToList();
//                Debug.Log("Amount of values: " + values.Count);

                matrix[i,0] = float.Parse(values[0]);
                matrix[i,1] = float.Parse(values[1]);
                matrix[i,2] = float.Parse(values[2]);
                position[i] = float.Parse(values[3]);

                i++;
            }

            if (i == 3)
            {
                //store position of Subunit
                subunitPositions.Add(position);
                //Calculate quarternion and store it
                Debug.Log("Rotationmatrix: " + matrix);
                Debug.Log("Quaternion: " + Helper.RotationMatrixToQuaternion(matrix));
                subunitRotations.Add(Helper.RotationMatrixToQuaternion(matrix));

                //reset variables
                i = 0;
                position = new Vector3();
            }
        }

        FallbackIfRemarkIsMissing();

        Debug.Log("Biounit consists of " + subunitRotations.Count + " subunits.");
    }

    private void FallbackIfRemarkIsMissing()
    {
        if (subunitPositions.Count == 0)
        {
            Debug.Log("No remark for biounit greation was found! Using fallback values.");
            subunitPositions.Add(Vector3.zero);
            subunitRotations.Add(Quaternion.identity);
        }
    }

    private void loadAtoms(){
        List<List<Vector4>> atoms = new List<List<Vector4>>();
        List<Vector4> currentChain = new List<Vector4>();
        Boolean newChain = false;

        foreach (var line in linesOfFile)
        {
            if (line.StartsWith("ATOM"))
            {
                if (newChain)
                {
                    currentChain = new List<Vector4>();
                    newChain = false;
                }

                var split = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                var position = split.Where(s => s.Contains(".")).ToList();
                var symbol = Array.IndexOf(Atom.AtomSymbols, split[2][0].ToString());
                if (symbol < 0) throw new Exception("Symbol not found");

                var atom = new Vector4(float.Parse(position[0]), float.Parse(position[1]), float.Parse(position[2]), symbol);
                currentChain.Add(atom);
            }

            if (line.StartsWith("TER"))
            {
                Debug.Log("TER found -> terminating chain with " + currentChain.Count + " atoms!");
                atoms.Add(currentChain);
                newChain = true;
            }
        }

        writeAtomsIntoAtomsSeperatedByChains(atoms);
    }

    private void writeAtomsIntoAtomsSeperatedByChains(List<List<Vector4>> atoms){
        int i = 1;
        int count = 0;

        foreach(var atomsOfOnChain in atoms){
            Debug.Log("Chain " + i + " consists of " + atomsOfOnChain.Count + " atoms!");
            count = count + atomsOfOnChain.Count; 

            i++;
            atomsSeperatedByChains.Add(atomsOfOnChain.ToArray());
        }
        Debug.Log("One subunit consists of " + count +" atoms!");
    }
}
