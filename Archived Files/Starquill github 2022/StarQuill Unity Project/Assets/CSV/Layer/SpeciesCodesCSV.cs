using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpeciesCodesCSV
{

    public List<SpeciesData> species;


    public SpeciesCodesCSV(TextAsset SpeciesCodes) {

        species = new List<SpeciesData>();
        string[] HumanSkinColors = GameData.localData.colorCodes.skinColorArray;


        string[] SpeciesCodesArrayWithHeader = SpeciesCodes.text.Split(char.Parse("\n"));
        //get rid of header
        string[] SpeciesCodesArray = new string[SpeciesCodesArrayWithHeader.Length-1];

        for (int i = 1; i < SpeciesCodesArrayWithHeader.Length; i++) {
            SpeciesCodesArray[i-1] = SpeciesCodesArrayWithHeader[i];
        }


        foreach (string row in SpeciesCodesArray) {
            string[] array = row.Split(char.Parse(","));

            //other body parts array
            string[] otherBodyPartsArray = null;
            if (array[15] != "") {
                otherBodyPartsArray = array[15].Split(char.Parse(" "));
                
            }

            string[] skinColors = null;
            skinColors = array[16].Split(char.Parse(" "));
            if (skinColors[0] == "human") {
                skinColors = HumanSkinColors;
            }

            string[] itemRestrictions = null;
            itemRestrictions = array[17].Split(char.Parse(" "));
            


            List<List<string>> skinVariance = null;
            List<string> skinVariance2D = null;
            if (array[18] != null && array[18] != "") {
                skinVariance = new List<List<string>>();
                skinVariance2D = new List<string>();
                Debug.Log("species has variance: "+array[0]);

                for (int i = 18; i < array.Length; i++) {
                    /*if (array[i] == null || array[i] == "" || array[i] == " ") {
                        break;
                    }*/
                    string[] subSkinVariance = null;
                    
                    if (array[i] != null && array[i] != "") {
                        skinVariance2D = new List<string>();
                        subSkinVariance = array[i].Split(char.Parse(" "));
                        foreach (string str in subSkinVariance) {
                            skinVariance2D.Add(str);
                        }
                        skinVariance.Add(skinVariance2D);
                    }
                }

                
            }
            
            
            
            
            float xScale = 1;
            float.TryParse(array[1],out xScale);
            float yScale = 1;
            float.TryParse(array[2],out yScale);
            
            species.Add(new SpeciesData(
            array[0],    //name
            xScale,          //xScale
            yScale,          //yScale
            array[3],  //backArm
            array[4],  //legs
            array[5],  //body
            array[6],  //head
            array[7],  //frontArm
            array[8],        //eyes
            array[9],        //nose
            array[10],        //mouth
            array[11],        //ears
            array[12],         //facialHair
            array[13],         //facialDetail
            array[14],         //hair
            otherBodyPartsArray, //OtherBodyParts
            skinColors, //SkinColor
            itemRestrictions, //itemRestrictions
            skinVariance //SkinVariance
            ));
            
        }

    }


    
}
