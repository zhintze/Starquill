using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EquipmentNamingCSV
{
    TextAsset itemNameType;
    TextAsset itemNamePrefix;
    TextAsset itemNameSuffix;

    string[] preprocessorArray;

    //types
    public List<string> lightSword;
    public List<string> shield;
    public List<string> artifact;
    public List<string> accessory;
    public List<string> body;
    public List<string> head;
    public List<string> feet;


    //prefixes
    public List<string> strengthPrefix;
    public List<string> dexterityPrefix;
    public List<string> constitutionPrefix;
    public List<string> wisdomPrefix;
    public List<string> intelligencePrefix;
    public List<string> charismaPrefix;
    public List<string> bludgeoningPrefix;
    public List<string> piercingPrefix;
    public List<string> slashingPrefix;
    public List<string> armorPointsPrefix;
    public List<string> healthPrefix;
    public List<string> magicPrefix;

    //suffixes
    public List<string> strengthSuffix;
    public List<string> dexteritySuffix;
    public List<string> constitutionSuffix;
    public List<string> wisdomSuffix;
    public List<string> intelligenceSuffix;
    public List<string> charismaSuffix;
    public List<string> bludgeoningSuffix;
    public List<string> piercingSuffix;
    public List<string> slashingSuffix;
    public List<string> armorPointsSuffix;
    public List<string> healthSuffix;
    public List<string> magicSuffix;


    /*
     * constructor
     */
    public EquipmentNamingCSV(TextAsset types, TextAsset prefixes, TextAsset suffixes)
    {
        itemNameType = types;
        itemNamePrefix = prefixes;
        itemNameSuffix = suffixes;
        LoadTextAssets();
    }


    /*
     * each row of .csv is run through this function and placed into the array passed as the argument
     */
    void ListPreprocessing(List<string> arrayToFill)
    {

        //start at 1 to ignore row description
        for (int i = 1; i < preprocessorArray.Length; i++)
        {
            if (preprocessorArray[i].StartsWith("Empty"))
            {
                //do not include value
            }
            else
            {
                arrayToFill.Add(preprocessorArray[i]);
            }
        }

    }



    /*
     * goes through each row of the .csv specifically and processes it into an array the code can utilize
     */
    void LoadTextAssets()
    {

        //--- Types ---//
        string[] itemNameTypeArray = itemNameType.text.Split(char.Parse("\n"));

        preprocessorArray = itemNameTypeArray[0].Split(char.Parse(","));
        lightSword = new List<string>();
        ListPreprocessing(lightSword);

        preprocessorArray = itemNameTypeArray[1].Split(char.Parse(","));
        shield = new List<string>();
        ListPreprocessing(shield);

        preprocessorArray = itemNameTypeArray[2].Split(char.Parse(","));
        artifact = new List<string>();
        ListPreprocessing(artifact);

        preprocessorArray = itemNameTypeArray[3].Split(char.Parse(","));
        accessory = new List<string>();
        ListPreprocessing(accessory);

        preprocessorArray = itemNameTypeArray[4].Split(char.Parse(","));
        body = new List<string>();
        ListPreprocessing(body);

        preprocessorArray = itemNameTypeArray[5].Split(char.Parse(","));
        head = new List<string>();
        ListPreprocessing(head);

        preprocessorArray = itemNameTypeArray[6].Split(char.Parse(","));
        feet = new List<string>();
        ListPreprocessing(feet);
        //--- Types END ---//



        //--- Prefixes BEGIN ---//
        string[] itemNamePrefixArray = itemNamePrefix.text.Split(char.Parse("\n"));

        preprocessorArray = itemNamePrefixArray[0].Split(char.Parse(","));
        strengthPrefix = new List<string>();
        ListPreprocessing(strengthPrefix);

        preprocessorArray = itemNamePrefixArray[1].Split(char.Parse(","));
        dexterityPrefix = new List<string>();
        ListPreprocessing(dexterityPrefix);

        preprocessorArray = itemNamePrefixArray[2].Split(char.Parse(","));
        constitutionPrefix = new List<string>();
        ListPreprocessing(constitutionPrefix);

        preprocessorArray = itemNamePrefixArray[3].Split(char.Parse(","));
        wisdomPrefix = new List<string>();
        ListPreprocessing(wisdomPrefix);

        preprocessorArray = itemNamePrefixArray[4].Split(char.Parse(","));
        intelligencePrefix = new List<string>();
        ListPreprocessing(intelligencePrefix);

        preprocessorArray = itemNamePrefixArray[5].Split(char.Parse(","));
        charismaPrefix = new List<string>();
        ListPreprocessing(charismaPrefix);

        preprocessorArray = itemNamePrefixArray[6].Split(char.Parse(","));
        bludgeoningPrefix = new List<string>();
        ListPreprocessing(bludgeoningPrefix);

        preprocessorArray = itemNamePrefixArray[7].Split(char.Parse(","));
        piercingPrefix = new List<string>();
        ListPreprocessing(piercingPrefix);

        preprocessorArray = itemNamePrefixArray[8].Split(char.Parse(","));
        slashingPrefix = new List<string>();
        ListPreprocessing(slashingPrefix);

        preprocessorArray = itemNamePrefixArray[9].Split(char.Parse(","));
        armorPointsPrefix = new List<string>();
        ListPreprocessing(armorPointsPrefix);

        /*preprocessorArray = itemNamePrefixArray[9].Split(char.Parse(","));
        armorPointsPrefix = new List<string>();
        ListPreprocessing(armorPointsPrefix);

        preprocessorArray = itemNamePrefixArray[10].Split(char.Parse(","));
        healthPrefix = new List<string>();
        ListPreprocessing(healthPrefix);

        preprocessorArray = itemNamePrefixArray[11].Split(char.Parse(","));
        magicPrefix = new List<string>();
        ListPreprocessing(magicPrefix);*/
        //--- Prefixes END ---//


        //--- Suffixes BEGIN ---//
        string[] itemNameSuffixArray = itemNameSuffix.text.Split(char.Parse("\n"));

        preprocessorArray = itemNameSuffixArray[0].Split(char.Parse(","));
        strengthSuffix = new List<string>();
        ListPreprocessing(strengthSuffix);

        preprocessorArray = itemNameSuffixArray[1].Split(char.Parse(","));
        dexteritySuffix = new List<string>();
        ListPreprocessing(dexteritySuffix);

        preprocessorArray = itemNameSuffixArray[2].Split(char.Parse(","));
        constitutionSuffix = new List<string>();
        ListPreprocessing(constitutionSuffix);

        preprocessorArray = itemNameSuffixArray[3].Split(char.Parse(","));
        wisdomSuffix = new List<string>();
        ListPreprocessing(wisdomSuffix);

        preprocessorArray = itemNameSuffixArray[4].Split(char.Parse(","));
        intelligenceSuffix = new List<string>();
        ListPreprocessing(intelligenceSuffix);

        preprocessorArray = itemNameSuffixArray[5].Split(char.Parse(","));
        charismaSuffix = new List<string>();
        ListPreprocessing(charismaSuffix);

        preprocessorArray = itemNameSuffixArray[6].Split(char.Parse(","));
        bludgeoningSuffix = new List<string>();
        ListPreprocessing(bludgeoningSuffix);

        preprocessorArray = itemNameSuffixArray[7].Split(char.Parse(","));
        piercingSuffix = new List<string>();
        ListPreprocessing(piercingSuffix);

        preprocessorArray = itemNameSuffixArray[8].Split(char.Parse(","));
        slashingSuffix = new List<string>();
        ListPreprocessing(slashingSuffix);

        preprocessorArray = itemNameSuffixArray[9].Split(char.Parse(","));
        armorPointsSuffix = new List<string>();
        ListPreprocessing(armorPointsSuffix);

        /*preprocessorArray = itemNameSuffixArray[9].Split(char.Parse(","));
        armorPointsSuffix = new List<string>();
        ListPreprocessing(armorPointsSuffix);

        preprocessorArray = itemNameSuffixArray[10].Split(char.Parse(","));
        healthSuffix = new List<string>();
        ListPreprocessing(healthSuffix);

        preprocessorArray = itemNameSuffixArray[11].Split(char.Parse(","));
        magicSuffix = new List<string>();
        ListPreprocessing(magicSuffix);*/
        //--- Suffixes END ---//


        


    }

}
