using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MonsterCodesCSV
{

    public List<string> monType;
    public List<int[]> layerType;
    public List<int[]> layerAmount;

    public MonsterCodesCSV(TextAsset MonsterCodes) {
        monType = new List<string>();
        layerType = new List<int[]>();
        layerAmount = new List<int[]>();

        string[] MonsterCodesArrayWithHeader = MonsterCodes.text.Split(char.Parse("\n"));
        string[] MonsterCodesArray = new string[MonsterCodesArrayWithHeader.Length-1];

        for (int i = 1; i < MonsterCodesArrayWithHeader.Length; i++) {
            MonsterCodesArray[i-1] = MonsterCodesArrayWithHeader[i];
        }

        foreach (string row in MonsterCodesArray) {
            string[] array = row.Split(char.Parse(","));
            int layer1 = 0;
            int layer2 = 0;
            int layer3 = 0;
            int layer4 = 0;

            
            int.TryParse(array[1],out layer1);
            
            int.TryParse(array[2],out layer2);
            
            int.TryParse(array[3],out layer3);

            int.TryParse(array[4],out layer4);
            

            

            monType.Add(array[0]);
            layerType.Add(new int[]{1,2,3,4});
            layerAmount.Add(new int[]{layer1,layer2,layer3,layer4});

        }

    }
    
}
