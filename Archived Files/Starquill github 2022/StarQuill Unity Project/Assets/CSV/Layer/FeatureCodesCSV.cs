using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FeatureCodesCSV
{
    
    public int featureCodesLength;


    public List<string> itemType;
    public List<int[]> layers;
    public List<int> itemAmount;

    /*
     * constructor
     */
    public FeatureCodesCSV(TextAsset FeatureCodes)
    {
        itemType = new List<string>();
        layers = new List<int[]>();
        itemAmount = new List<int>();
        
        string[] FeatureCodesArrayWithHeader = FeatureCodes.text.Split(char.Parse("\n"));
        //get rid of header
        string[] FeatureCodesArray = new string[FeatureCodesArrayWithHeader.Length-1];

        for (int i = 1; i < FeatureCodesArrayWithHeader.Length; i++) {
            FeatureCodesArray[i-1] = FeatureCodesArrayWithHeader[i];
        }

        featureCodesLength = FeatureCodesArray.Length;

        foreach (string row in FeatureCodesArray) {
            string[] array = row.Split(char.Parse(","));
            int layer0 = 0;
            int layer1 = 0;
            int layer2 = 0;

            int.TryParse(array[1],out layer0);
            
            int.TryParse(array[2],out layer1);
            
            int.TryParse(array[3],out layer2);
            
            

            int[] layerArray;
            if (array[2] == "") {
                layerArray = new int[] {layer0};
            } else if (array[3] == "") {
                layerArray = new int[] {layer0,layer1};
            } else {
                layerArray = new int[] {layer0,layer1,layer2};
            }



            
            int itemAmountInt = 0;
            if (array[4] != null) {   
                int.TryParse(array[4],out itemAmountInt);
            }

            itemType.Add(array[0]);
            layers.Add(layerArray);
            itemAmount.Add(itemAmountInt);

        }


    }


}
