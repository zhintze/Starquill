using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ColorCodesCSV
{

    
    public string[] mainColorArray;
    public string[] eyeColorArray;
    public string[] equipmentColorArray;
    public string[] skinColorArray;
    public string[] weaponColorArray;

    public string[][] terrainColorsArray;
    public string[] biomCoastColorArray;
    public string[] biomSnowColorArray;


    public ColorCodesCSV(TextAsset mainColorsAsset, TextAsset eyeColorsAsset, TextAsset equipmentColorsAsset, TextAsset skinColorsAsset, TextAsset weaponColorsAsset,
                         TextAsset terrainColorsAsset) {

        
        mainColorArray = mainColorsAsset.text.Split(char.Parse("\n"));
        eyeColorArray = eyeColorsAsset.text.Split(char.Parse("\n"));
        equipmentColorArray = equipmentColorsAsset.text.Split(char.Parse("\n"));
        skinColorArray = skinColorsAsset.text.Split(char.Parse("\n"));
        weaponColorArray = weaponColorsAsset.text.Split(char.Parse("\n"));

        Debug.Log("Terrain Colors not active");
        /*terrainColorsArray = new string[terrainColorsAsset.Length][];
        for(int i = 0; i < terrainColorsAsset.Length; i++) {
            terrainColorsArray[i] = terrainColorsAsset[i].text.Split(char.Parse("\n"));
        }*/



    }

}
