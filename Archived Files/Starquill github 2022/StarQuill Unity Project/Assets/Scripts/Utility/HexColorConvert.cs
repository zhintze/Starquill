using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HexColorConvert
{
    public static Color32 Parse(string colorSource) {
        Color32 newCol;
        if (colorSource.Length < 6) {
            Debug.Log("colorSource error: "+colorSource);
            return newCol = new Color32(0,0,0,255);
        }
        byte intR = byte.Parse(colorSource.Substring(0,2), System.Globalization.NumberStyles.HexNumber);
        byte intG = byte.Parse(colorSource.Substring(2,2), System.Globalization.NumberStyles.HexNumber);
        byte intB = byte.Parse(colorSource.Substring(4,2), System.Globalization.NumberStyles.HexNumber);
        
        return newCol = new Color32(intR,intG,intB,255);
    }
}
