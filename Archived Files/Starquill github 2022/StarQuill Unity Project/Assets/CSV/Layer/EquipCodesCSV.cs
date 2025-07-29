using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EquipCodesCSV
{
    
    public int equipCodesLength;


    public List<string> itemType;
    public List<int[]> layers;
    public List<List<int>> hiddenLayers;
    public List<List<int>> colorVariance;
    public List<List<int>> itemAmount;

    /*
     * constructor
     */
    public EquipCodesCSV() {
        itemType = new List<string>();
        layers = new List<int[]>();
        hiddenLayers = new List<List<int>>();
        colorVariance = new List<List<int>>();
        itemAmount = new List<List<int>>();
    }

    public EquipCodesCSV(TextAsset EquipCodes)
    {
        itemType = new List<string>();
        layers = new List<int[]>();
        hiddenLayers = new List<List<int>>();
        colorVariance = new List<List<int>>();
        itemAmount = new List<List<int>>();
        
        string[] EquipCodesArrayWithHeader = EquipCodes.text.Split(char.Parse("\n"));
        //get rid of header
        string[] EquipCodesArray = new string[EquipCodesArrayWithHeader.Length-1];

        for (int i = 1; i < EquipCodesArrayWithHeader.Length; i++) {
            EquipCodesArray[i-1] = EquipCodesArrayWithHeader[i];
        }

        equipCodesLength = EquipCodesArray.Length;

        foreach (string row in EquipCodesArray) {
        
            string[] array = row.Split(char.Parse(","));

            int layer0 = 0;int layer1 = 0;int layer2 = 0;

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

            List<int> hiddenLayerListInt = new List<int>();
            
            if (array[4] != null && array[4] != "") {
                
                string[] hiddenLayerArray = array[4].Split(char.Parse(" "));
                
                foreach (string layer in hiddenLayerArray) {
                    int layerInt = 0;
                    int.TryParse(layer,out layerInt);
                    hiddenLayerListInt.Add(layerInt);
                }
            }

            
            List<int> colorVarianceArrayInt = new List<int>();
            if (array[5] != null && array[5] != "") {  
                string[] colorVarianceArray = array[5].Split(char.Parse(" "));
                foreach (string layer in colorVarianceArray) {
                    int layerInt = 0;
                    int.TryParse(layer,out layerInt);
                    colorVarianceArrayInt.Add(layerInt);
                }
            }


            List<int> itemAmountArrayInt = new List<int>();
            if (array[6] != null && array[6] != "") {  
                string[] itemAmountArray = array[6].Split(char.Parse(" "));
                foreach (string layer in itemAmountArray) {
                    int layerInt = 0;
                    int.TryParse(layer,out layerInt);
                    itemAmountArrayInt.Add(layerInt);
                }
            }
            
            

            itemType.Add(array[0]);
            layers.Add(layerArray);
            hiddenLayers.Add(hiddenLayerListInt);
            colorVariance.Add(colorVarianceArrayInt);
            itemAmount.Add(itemAmountArrayInt);

        }


    }


    


}
