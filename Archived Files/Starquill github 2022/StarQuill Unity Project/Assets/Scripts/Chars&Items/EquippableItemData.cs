using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

public class EquippableItemData
{

    GameData gameData = GameData.localData;
    
    public int uniqueID;
    public string name;
    string chosenPrefix;
    string chosenSuffix;
    public EquipmentNamingCSV namingArray;
    List<string>[] prefixList;
	List<string>[] suffixList;

    public string type;
    //variables for creating the actual Object Image
    public int[] itemLayers;
    public List<string> spriteAddresses;
    public List<string> spriteColors;
    public List<int> itemHiddenLayers;
    public bool isBeingDisplayed = false; //only used in display image loop



    public bool isTwoHanded = false;
	public bool isImprovised;
    public bool isHeavy;
	public bool isVersatile;
	public bool isDexterity;
    

    //stats
    public int rarity;
	public int level = 10;
	public int availablePoints = 0;

	public int strength;
	public int dexterity;
	public int constitution;
	public int wisdom;
	public int intelligence;
	public int charisma;

	public string armorClass; //light, medium, heavy
	public int armorPoints;

	public int health;
	public int magic;

	public int bludgeoning;
	public int piercing;
	public int slashing;










    
    
    public EquippableItemData() {
        uniqueID = gameData.ItemUniqueID;
        initializeNamingArrays();

        ItemTypeRoll(null);

        rarity = Random.Range(0, 5);
		rarityPointRoll();
        InitializeEquipmentStats();
        //NameRoll();
		name = "";

        //Debug.Log(name);
    }


    public EquippableItemData(string[] itemTypes) {
        uniqueID = gameData.ItemUniqueID;
        initializeNamingArrays();

        ItemTypeRoll(itemTypes);

        rarity = Random.Range(0, 5);
		rarityPointRoll();
        InitializeEquipmentStats();
        //NameRoll();
		name = "";

        //Debug.Log(name);

    }


    /*
        ItemTypeRoll selects the itemType, Images, and Colors
        if null, itemType is random
    */
    void ItemTypeRoll(string[] itemTypes) {

        EquipCodesCSV sortedEquipCodes;

        if (itemTypes == null) {
            sortedEquipCodes = gameData.equipCodes;
        } else {
            sortedEquipCodes = SortEquipCodes(itemTypes);
        }

        int chosenItemType = Random.Range(0,sortedEquipCodes.equipCodesLength);
        //set variables based on chosen Item Type

        type = sortedEquipCodes.itemType[chosenItemType];
        itemLayers = sortedEquipCodes.layers[chosenItemType];
        List<int> colorVariance = sortedEquipCodes.colorVariance[chosenItemType];
        List<int> itemAmount = sortedEquipCodes.itemAmount[chosenItemType];
        

        
        spriteAddresses = new List<string>();
        spriteColors = new List<string>();
        

        //weapons have each layer randomized and different sprite address
        if (type[0] == 'w') {
            //any layer NOT in colorVariance applies this color
            string mainColor = gameData.colorCodes.equipmentColorArray[Random.Range(0,gameData.colorCodes.weaponColorArray.Length)];

            //weapons cycle through randomImages PER layer
            for (int i = 0; i < itemLayers.Length; i++) {
                //select the image FOR THIS LAYER within the itemType group
                int randomImage = Random.Range(1,itemAmount[i]+1);
                string itemImage = randomImage.ToString();
                while (itemImage.Length < 4) {itemImage = "0"+itemImage;} //add 0s

                //set spriteAddress
                string strLayer = itemLayers[i].ToString();
                while (strLayer.Length < 3) {strLayer = "0"+strLayer;} //add 0s
                string fullname = type+"-"+strLayer+"-"+itemImage;

                spriteAddresses.Add("Assets/Images/Weapons/"+fullname+".png");


                //if colorVariance == layer it applies a different color
                bool isVariant = false;
                foreach (int variedLayer in colorVariance) {
                    if (variedLayer == itemLayers[i]) {
                        spriteColors.Add(gameData.colorCodes.equipmentColorArray[Random.Range(0,gameData.colorCodes.weaponColorArray.Length)]);
                        isVariant = true;
                    }
                }
                if (isVariant == false) {
                    spriteColors.Add(mainColor);
                }
            }
        } else {

            //select the image within the itemType group
            int randomImage = Random.Range(1,itemAmount[0]+1);
            string itemImage = randomImage.ToString();
            while (itemImage.Length < 4) {itemImage = "0"+itemImage;} //add 0s

            //any layer NOT in colorVariance applies this color
            string mainColor = gameData.colorCodes.equipmentColorArray[Random.Range(0,gameData.colorCodes.equipmentColorArray.Length)];


            foreach (int layer in itemLayers) {
                //set spriteAddress
                string strLayer = layer.ToString();
                while (strLayer.Length < 3) {strLayer = "0"+strLayer;} //add 0s
                string fullname = type+"-"+itemImage+"-"+strLayer;

                spriteAddresses.Add("Assets/Images/Equipment/"+fullname+".png");


                //if colorVariance == layer it applies a different color
                bool isVariant = false;
                foreach (int variedLayer in colorVariance) {
                    if (variedLayer == layer) {
                        spriteColors.Add(gameData.colorCodes.equipmentColorArray[Random.Range(0,gameData.colorCodes.equipmentColorArray.Length)]);
                        isVariant = true;
                    }
                }
                if (isVariant == false) {
                    spriteColors.Add(mainColor);
                }
            }

            

        }

        
        itemHiddenLayers = sortedEquipCodes.hiddenLayers[chosenItemType];

    }









    void InitializeEquipmentStats()
	{
		int randomStat = 0;
		int randomNumber = 0;

		List<int> statsArray = new List<int>();
		List<bool> boolArray = new List<bool>();
		statsArray.Add(strength); boolArray.Add(false);
		statsArray.Add(dexterity); boolArray.Add(false);
		statsArray.Add(constitution); boolArray.Add(false);
		statsArray.Add(wisdom); boolArray.Add(false);
		statsArray.Add(intelligence); boolArray.Add(false);
		statsArray.Add(charisma); boolArray.Add(false);
		statsArray.Add(armorPoints); boolArray.Add(false);
		statsArray.Add(health); boolArray.Add(false);
		statsArray.Add(magic); boolArray.Add(false);

		if (type[0] == 'w' && type != "w08" && type != "w09")
        {
			statsArray.Add(bludgeoning); boolArray.Add(false);
			statsArray.Add(piercing); boolArray.Add(false);
			statsArray.Add(slashing); boolArray.Add(false);

			if (type == "w04" ||type == "w05" || type == "w07") {
				isTwoHanded = true;
			}

			if (type == "w07" || type == "w06") {
				isDexterity = true;
			}
		}
		

		while (availablePoints > 0)
        {
			//reset bools if all booleans are true
			bool atLeastOneBoolIsFalse = false;
			bool atLeastOneBoolIsTrue = false;
			foreach(bool boolean in boolArray)
            {
				if (boolean == false)
                {
					atLeastOneBoolIsFalse = true;
				}
				if (boolean == true)
				{
					atLeastOneBoolIsTrue = true;
				}
			}
			if (atLeastOneBoolIsFalse == false && atLeastOneBoolIsTrue == true)
            {
				for (int i = 0; i < boolArray.Count; i++)
                {
					boolArray[i] = false;
				}
			}

			//pick a random stat that is not set to true, assign points to it, set it to true

			randomStat = Random.Range(0, statsArray.Count);
			if (boolArray[randomStat] == false)
            {
				boolArray[randomStat] = true;
				randomNumber = Random.Range(0, availablePoints + 1);
				statsArray[randomStat] += randomNumber; availablePoints -= randomNumber;

			}
        }
	}




    void rarityPointRoll()
	{
		if (rarity == 0)
		{
			availablePoints = Random.Range(3, 6);
		}
		else if (rarity == 1)
		{
			availablePoints = Random.Range(6, 10);
		}
		else if (rarity == 2)
		{
			availablePoints = Random.Range(10, 15);
		}
		else if (rarity == 3)
		{
			availablePoints = Random.Range(15, 20);
		}
		else if (rarity == 4)
		{
			availablePoints = Random.Range(20, 28);
		}
		else
		{
			Debug.Log("Equipment.cs: Rarity not assigned");
			availablePoints = 0;
		}

	}




    /*
	 * randomize name for item based off of the stats
	 */
	void NameRoll()
    {
		//find stat with the highest value
		int highestValue = 0;
		int namingInteger = 0;

		for (int i = 0; i < 2; i++)
		{
			checkStatValue(strength, 1, highestValue, namingInteger, out highestValue, out namingInteger);
			checkStatValue(dexterity, 2, highestValue, namingInteger, out highestValue, out namingInteger);
			checkStatValue(constitution, 3, highestValue, namingInteger, out highestValue, out namingInteger);
			checkStatValue(wisdom, 4, highestValue, namingInteger, out highestValue, out namingInteger);
			checkStatValue(intelligence, 5, highestValue, namingInteger, out highestValue, out namingInteger);
			checkStatValue(charisma, 6, highestValue, namingInteger, out highestValue, out namingInteger);
			checkStatValue(armorPoints, 7, highestValue, namingInteger, out highestValue, out namingInteger);
			checkStatValue(health, 8, highestValue, namingInteger, out highestValue, out namingInteger);
			checkStatValue(magic, 9, highestValue, namingInteger, out highestValue, out namingInteger);
			checkStatValue(bludgeoning, 10, highestValue, namingInteger, out highestValue, out namingInteger);
			checkStatValue(piercing, 11, highestValue, namingInteger, out highestValue, out namingInteger);
			checkStatValue(slashing, 12, highestValue, namingInteger, out highestValue, out namingInteger);

		}

		//determine if namingInteger is prefix or suffix or has both
		//probably change percentage chance based on level and rarity
		//adding the space " " at time of assigning prefix/suffix prevents extra blank spaces
		int chanceTime = Random.Range(0, 3);
		if (chanceTime == 0)
		{
			//prefix
			chosenPrefix = prefixList[namingInteger][Random.Range(0, prefixList[namingInteger].Count)] + " ";
			chosenPrefix = "";
		}
		else if (chanceTime == 1)
		{
			//suffix
			chosenSuffix = " " + suffixList[namingInteger][Random.Range(0, suffixList[namingInteger].Count)];
		}
		else if (chanceTime == 2)
		{
			//has none

		}
		else
		{
			Debug.Log("EquipmentData: prefix/suffix naming error");
		}

		//------------------------------------------ having both prefix+suffix not yet implemented --------------------------------------//


		//add name of equipment type
		string typeName = "";
		if (type[0] == 't' || type[0] == 'l')
        {
			typeName = namingArray.body[Random.Range(0, namingArray.body.Count)];
		} else if (type[0] == 'h')
		{
			typeName = namingArray.head[Random.Range(0, namingArray.head.Count)];
		}
		else if (type[0] == 'f')
		{
			typeName = namingArray.feet[Random.Range(0, namingArray.feet.Count)];
		}
		else if (type[0] == 'a')
		{
			
			typeName = namingArray.accessory[Random.Range(0, namingArray.accessory.Count)];
			
			
		}
        else if (type[0] == 'm')
		{
			
			typeName = namingArray.artifact[Random.Range(0, namingArray.artifact.Count)];
			
		}
		else if (type == "w08" ||type == "w09")
		{
			typeName = namingArray.shield[Random.Range(0, namingArray.shield.Count)];
		}
		else if (type[0] == 'w')
		{
			typeName = namingArray.lightSword[Random.Range(0, namingArray.lightSword.Count)];
		}
		else
		{
			Debug.Log("EquipmentData: namingArray type error");
		}


		//finally, combine all three strings to make the full equipment's name
		name = chosenPrefix + typeName + chosenSuffix;
	}




	/*
	 * used inside NameRoll() to determine which stat is more worthy than another for being chosen for the naming convention
	 */
	void checkStatValue(int selectedStat, int selectedStatNamingInteger, int prevHighestValue, int prevNamingInteger, out int highestValue, out int namingInteger)
	{
		//50% chance to switch namingInteger if this stat equals a previous stat
		if (selectedStat == prevHighestValue && prevNamingInteger != selectedStatNamingInteger && Random.Range(0, 2) == 0)
		{
			highestValue = health;
			namingInteger = selectedStatNamingInteger;
			//if this stat is higher than the previous stat, switch namingInteger
		}
		else if (selectedStat > prevHighestValue)
		{
			highestValue = selectedStat;
			namingInteger = selectedStatNamingInteger;
			//otherwise use previous values
		}
		else
		{
			highestValue = prevHighestValue;
			namingInteger = prevNamingInteger;
		}

	}


	/*
	 * puts all csv prefix/suffix categories into arrays
	 */
	void initializeNamingArrays()
    {
		namingArray = gameData.equipmentNamingCodes;
		prefixList = new List<string>[13];
		prefixList[0] = null;
		prefixList[1] = namingArray.strengthPrefix;
		prefixList[2] = namingArray.dexterityPrefix;
		prefixList[3] = namingArray.constitutionPrefix;
		prefixList[4] = namingArray.wisdomPrefix;
		prefixList[5] = namingArray.intelligencePrefix;
		prefixList[6] = namingArray.charismaPrefix;
		prefixList[7] = namingArray.armorPointsPrefix;
		prefixList[8] = namingArray.healthPrefix;
		prefixList[9] = namingArray.magicPrefix;
		prefixList[10] = namingArray.bludgeoningPrefix;
		prefixList[11] = namingArray.piercingPrefix;
		prefixList[12] = namingArray.slashingPrefix;


		suffixList = new List<string>[13];
		suffixList[0] = null;
		suffixList[1] = namingArray.strengthSuffix;
		suffixList[2] = namingArray.dexteritySuffix;
		suffixList[3] = namingArray.constitutionSuffix;
		suffixList[4] = namingArray.wisdomSuffix;
		suffixList[5] = namingArray.intelligenceSuffix;
		suffixList[6] = namingArray.charismaSuffix;
		suffixList[7] = namingArray.armorPointsSuffix;
		suffixList[8] = namingArray.healthSuffix;
		suffixList[9] = namingArray.magicSuffix;
		suffixList[10] = namingArray.bludgeoningSuffix;
		suffixList[11] = namingArray.piercingSuffix;
		suffixList[12] = namingArray.slashingSuffix;

	}



	









    EquipCodesCSV SortEquipCodes(string[] itemTypes) {
        EquipCodesCSV sortedEquipCodes = new EquipCodesCSV();

        foreach (string itemType in itemTypes) {
            for (int i = 0; i < gameData.equipCodes.itemType.Count; i++) {
                if (itemType == gameData.equipCodes.itemType[i]) {

                    sortedEquipCodes.itemType.Add(gameData.equipCodes.itemType[i]);
                    sortedEquipCodes.layers.Add(gameData.equipCodes.layers[i]);
                    sortedEquipCodes.hiddenLayers.Add(gameData.equipCodes.hiddenLayers[i]);
                    sortedEquipCodes.colorVariance.Add(gameData.equipCodes.colorVariance[i]);
                    sortedEquipCodes.itemAmount.Add(gameData.equipCodes.itemAmount[i]);

                }
            }
            
        }

        sortedEquipCodes.equipCodesLength = sortedEquipCodes.itemType.Count;
        
        if (sortedEquipCodes == null) {
            Debug.Log("SortEquipCodes Failed: EquippableItemData.cs");
        }
        return sortedEquipCodes;
    }

    
}
