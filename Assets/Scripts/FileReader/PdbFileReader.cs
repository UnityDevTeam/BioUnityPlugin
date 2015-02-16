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
    private String usedRemarkForBioUnitCreation = "REMARK 350";

    private String[] linesOfFile;

    public BioUnit readFile(string pathToFile)
    {
        if (!File.Exists(pathToFile)) throw new Exception("Pdb file not found");

        readLinesOfFile(pathToFile);
        loadRotationsAndPositions();
        loadAtoms();

        return new BioUnit(atomsSeperatedByChains, subunitRotations, subunitPositions);
    }

    private void readLinesOfFile(String path){
        linesOfFile = File.ReadAllLines(path);
    }

    private void loadRotationsAndPositions(){
        foreach (var line in linesOfFile)
        {
            int i = 0;
            Matrix4x4 matrix = Matrix4x4.identity;
            Vector3 position = new Vector3();

            if (line.StartsWith(usedRemarkForBioUnitCreation))
            {
                var split = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                var values = split.Where(s => s.Contains(".")).ToList();

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
                subunitRotations.Add(Helper.RotationMatrixToQuaternion(matrix));
                //reset variables
                i = 0;
                position = new Vector3();
            }
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
                    atoms.Add(currentChain);
                    newChain = false;
                }

                var split = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                var position = split.Where(s => s.Contains(".")).ToList();
                var symbol = Array.IndexOf(Atom.AtomSymbols, split[2][0].ToString());
                if (symbol < 0) throw new Exception("Symbol not found");

                var atom = new Vector4(float.Parse(position[0]), float.Parse(position[1]), float.Parse(position[2]), symbol);
                currentChain.Add(atom);
            }

            if (line.StartsWith("TER")) newChain = true;
        }

        writeAtomsIntoAtomsSeperatedByChains(atoms);
    }

    private void writeAtomsIntoAtomsSeperatedByChains(List<List<Vector4>> atoms){
        foreach(var atomsOfOnChain in atoms){
            atomsSeperatedByChains.Add(atomsOfOnChain.ToArray());
        }
    }
}
