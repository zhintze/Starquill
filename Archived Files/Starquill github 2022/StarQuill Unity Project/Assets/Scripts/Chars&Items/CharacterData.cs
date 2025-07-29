using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterData
{
	GameData gameData;


	public string name;

	public SpeciesData speciesData;
	public string skinColor;
	public string hairColor;
	public string eyeColor;

	//variables for creating the actual Object Image
    public List<int> bodyPartLayers;
    public List<string> bodyPartSpriteAddresses;
    public List<string> bodyPartSpriteColors;

	//public List<ConsumableItemData> InventoryConsumable;

    public EquippableItemData EquipSlotHead;
    public EquippableItemData EquipSlotTorso;
    public EquippableItemData EquipSlotArms;
    public EquippableItemData EquipSlotLegs;
    public EquippableItemData EquipSlotFeet;
    public EquippableItemData EquipSlotMisc1;
    public EquippableItemData EquipSlotMisc2;
    public EquippableItemData EquipSlotMisc3;
    public EquippableItemData EquipSlotMisc4;

    public EquippableItemData EquipSlotMainHand;
    public EquippableItemData EquipSlotOffHand;



	List<List<string>> skinVarianceGroups;
	List<string> skinVarianceColor;

	

	//heraldry and lineage data
	//journal data

	//RPG stats
	public int strength;
	public int dexterity;
	public int constitution;
    public int intelligence;
	public int wisdom;
	public int charisma;

    public SkillData[] skills;

	public string armorClass;
	public int armorPoints;

	public int health;
	public int magic;


	//RPG Modified Stats (constantly updating stats based off of the Base Stats and any current effects or status that are affecting the LocalPlayer)
	public int strengthModified;
	public int dexterityModified;
	public int constitutionModified;
	public int wisdomModified;
	public int intelligenceModified;
	public int charismaModified;

	public int armorPointsModified;

	public int healthModified;
	public int magicModified;



    int beardLayer;
    int eyeLayer;

	
	public CharacterData (SpeciesData species, int str, int dex, int con, int wis, int intel, int cha)
    {
		gameData = GameData.localData;

        //used to set color of beard/eye
////////// !!-- specific file beards are currently not active to avoid the issue with catfolk beard
        beardLayer = 122;
        eyeLayer = 84;

		speciesData = species;
		strength = str;
		dexterity = dex;
		constitution = con;
		wisdom = wis;
		intelligence = intel;
		charisma = cha;

        InitSkills();

		
		skinColor = species.skinColorArray[Random.Range(0,species.skinColorArray.Length)];
		
        //change to gameData.hairEyeColors;
		hairColor = gameData.colorCodes.mainColorArray[Random.Range(0,gameData.colorCodes.mainColorArray.Length)];
		eyeColor = gameData.colorCodes.eyeColorArray[Random.Range(0,gameData.colorCodes.eyeColorArray.Length)];


		/*
			colors and layers first have to be separated into different lists
			then we
			add multiple layers to a list and puts it into varianceGroups
			and all colors enter a new list so a random one can be selected
		*/
		skinVarianceGroups = new List<List<string>>();
		skinVarianceColor  = new List<string>();
        

        //Debug.Log("HENLO");
        if (speciesData.skinVarianceArray != null) {
            //Debug.Log("speciesData.skinVarianceArray.Count: "+speciesData.skinVarianceArray.Count);
            for (int i = 0; i < speciesData.skinVarianceArray.Count; i++) {
			    List<string> varianceGroup = new List<string>();
			    List<string> colorGroupForRandomizer = new List<string>();

                

			    for (int j = 0; j < speciesData.skinVarianceArray[i].Count; j++) {
				    //layer
				    if (speciesData.skinVarianceArray[i][j].Length < 4) {
				    varianceGroup.Add(speciesData.skinVarianceArray[i][j]);
				    //color hex
				    } else {
					    colorGroupForRandomizer.Add(speciesData.skinVarianceArray[i][j]);
				    }
			    }
                if (colorGroupForRandomizer.Count == 0) {
                    continue;
                }
			    skinVarianceColor.Add(colorGroupForRandomizer[Random.Range(0, colorGroupForRandomizer.Count)]);
                //skinVarianceColor.Add(colorGroupForRandomizer[0]);
			    skinVarianceGroups.Add(varianceGroup);
                /*foreach (string var in varianceGroup) {
                    Debug.Log("color:"+skinVarianceColor[skinVarianceColor.Count-1] + " for layer: "+var);
                }*/
                /*string debugGroups = "layers: ";
                string debugColors = "colors: ";
                foreach (string var in varianceGroup) {
                    debugGroups = debugGroups + var + " ";
                }
                foreach (string var in colorGroupForRandomizer) {
                    debugColors = debugColors + var + " ";
                }
                Debug.Log(debugGroups);
                Debug.Log(debugColors);*/

		    }
        }
		


		initializeBodyImages();
        defaultStatsRoll();
		
    }


    void InitSkills() {
        skills = new SkillData[36];
        //strength
        skills[0] = new SkillData( 0, "athletics", 0, new int[] {0});
        skills[1] = new SkillData( 1, "momentum", 0, new int[] {0});
        skills[2] = new SkillData( 2, "intimidation", 0, new int[] {0});
        skills[3] = new SkillData( 3, "training", 0, new int[] {0});
        skills[4] = new SkillData( 4, "propulsion", 0, new int[] {0});
        skills[5] = new SkillData( 5, "brawn", 0, new int[] {0});
        //dexterity
        skills[6] = new SkillData( 6, "acrobatics", 1, new int[] {0});
        skills[7] = new SkillData( 7, "stealth", 1, new int[] {0});
        skills[8] = new SkillData( 8, "sleight of hand", 1, new int[] {0});
        skills[9] = new SkillData( 9, "reflexes", 1, new int[] {0});
        skills[10] = new SkillData( 10, "balance", 1, new int[] {0});
        skills[11] = new SkillData( 11, "precision", 1, new int[] {0});
        //constitution
        skills[12] = new SkillData( 6, "endurance", 2, new int[] {0});
        skills[13] = new SkillData( 7, "willpower", 2, new int[] {0});
        skills[14] = new SkillData( 8, "armor expertise", 2, new int[] {0});
        skills[15] = new SkillData( 9, "tolerance", 2, new int[] {0});
        skills[16] = new SkillData( 10, "survival", 2, new int[] {0});
        skills[17] = new SkillData( 11, "immunity", 2, new int[] {0});
        //intelligence
        skills[18] = new SkillData( 18, "investigation", 3, new int[] {0});
        skills[19] = new SkillData( 19, "arcana", 3, new int[] {0});
        skills[20] = new SkillData( 20, "history", 3, new int[] {0});
        skills[21] = new SkillData( 21, "science", 3, new int[] {0});
        skills[22] = new SkillData( 22, "mechanics", 3, new int[] {0});
        skills[23] = new SkillData( 23, "mathematics", 3, new int[] {0});
        //wisdom
        skills[24] = new SkillData( 24, "perception", 4, new int[] {0});
        skills[25] = new SkillData( 25, "insight", 4, new int[] {0});
        skills[26] = new SkillData( 26, "intuition", 4, new int[] {0});
        skills[27] = new SkillData( 27, "faith", 4, new int[] {0});
        skills[28] = new SkillData( 28, "beast kinship", 4, new int[] {0});
        skills[29] = new SkillData( 29, "nature affinity", 4, new int[] {0});
        //charisma
        skills[30] = new SkillData( 30, "persuasion", 5, new int[] {0});
        skills[31] = new SkillData( 31, "deception", 5, new int[] {0});
        skills[32] = new SkillData( 32, "performance", 5, new int[] {0});
        skills[33] = new SkillData( 33, "artistry", 5, new int[] {0});
        skills[34] = new SkillData( 34, "civility", 5, new int[] {0});
        skills[35] = new SkillData( 35, "allure", 5, new int[] {0});

    }



    void defaultStatsRoll() {
        /*
            main attributes
            lvl 1 cost 1
            2 costs 1
            3 cost 1
            4 is max and cost 2
        */
        //roll 3d4 for points
        int pointsToSpend1 = Random.Range(1,5);
        int pointsToSpend2 = Random.Range(1,5);
        int pointsToSpend3 = Random.Range(1,5);
        int pointsToSpend = pointsToSpend1+ pointsToSpend2 +pointsToSpend3;
        int randomAttribute;

        int infiniteLoopCatch = 0;

        do {
            bool pointsApplied = false;
            do {
                randomAttribute = Random.Range(0,6);
                //Debug.Log("pointsToSpend: "+pointsToSpend);
                pointsApplied = tryAddAttribute(randomAttribute,pointsToSpend, out pointsToSpend);
                
            } while (pointsApplied == false);

            infiniteLoopCatch++;
            if (infiniteLoopCatch > 20000) {
                Debug.Log("Error: defaultStatsRoll infiniteLoopCatch");
                pointsToSpend = -1;
            }

        } while (pointsToSpend > 0);
        
        
    }

    bool tryAddAttribute(int attribute,int pointsIn, out int pointsToSpend) {
        bool isSuccessful = false;
        int chosenAttribute;
        pointsToSpend = pointsIn;

        if (attribute == 0) {
            chosenAttribute = strength;
        } else if (attribute == 1) {
            chosenAttribute = dexterity;
        } else if (attribute == 2) {
            chosenAttribute = constitution;
        } else if (attribute == 3) {
            chosenAttribute = intelligence;
        } else if (attribute == 4) {
            chosenAttribute = wisdom;
        } else {
            chosenAttribute = charisma;
            
        }
 
        if (chosenAttribute <= 3 && pointsToSpend > 0) {
            chosenAttribute++;
            pointsToSpend = pointsToSpend - 1;
            isSuccessful = true;
        } else if (chosenAttribute == 4 && pointsToSpend > 2) {
            chosenAttribute++;
            pointsToSpend = pointsToSpend - 2;
            isSuccessful = true;
        }


        if (attribute == 0) {
            strength = chosenAttribute;
        } else if (attribute == 1) {
            dexterity = chosenAttribute;
        } else if (attribute == 2) {
            constitution = chosenAttribute;
        } else if (attribute == 3) {
            intelligence = chosenAttribute;
        } else if (attribute == 4) {
            wisdom = chosenAttribute;
        } else {
            charisma = chosenAttribute;
            
        }
        
        return isSuccessful;
    }



	//facedetail and facialhair chances are hardcoded
	void initializeBodyImages() {
		bodyPartLayers = new List<int>();
    	bodyPartSpriteAddresses = new List<string>();
    	bodyPartSpriteColors = new List<string>();

		
        
        processBodyPart(speciesData.eyes);
        processBodyPart(speciesData.ears);
        processBodyPart(speciesData.mouth);
        processBodyPart(speciesData.nose);

        
        processBodyPart(speciesData.hair);


        processBodyPart(speciesData.legs);
        processBodyPart(speciesData.body);
        processBodyPart(speciesData.head);
        processBodyPart(speciesData.backArm);
        processBodyPart(speciesData.frontArm);

        //if numeric it is required, else it is modular chance
        if (speciesData.facialHair.Length > 0 && (speciesData.facialHair.Substring(0,1) == "0" || speciesData.facialHair.Substring(0,1) == "1" ||  speciesData.facialHair.Substring(0,1) == "2" ||  speciesData.facialHair.Substring(0,1) == "3" ||
            speciesData.facialHair.Substring(0,1) == "4" ||  speciesData.facialHair.Substring(0,1) == "5" ||  speciesData.facialHair.Substring(0,1) == "6" ||  speciesData.facialHair.Substring(0,1) == "7" ||
            speciesData.facialHair.Substring(0,1) == "8" ||  speciesData.facialHair.Substring(0,1) == "9")) {
                processBodyPart(speciesData.facialHair);
        } else if (Random.Range(0,3) == 0) {
            processBodyPart(speciesData.facialHair);
        }

        //if numeric it is required, else it is modular chance
        if (speciesData.faceDetail.Length > 0 && (speciesData.faceDetail.Substring(0,1) == "0" || speciesData.faceDetail.Substring(0,1) == "1" ||  speciesData.faceDetail.Substring(0,1) == "2" ||  speciesData.faceDetail.Substring(0,1) == "3" ||
            speciesData.faceDetail.Substring(0,1) == "4" ||  speciesData.faceDetail.Substring(0,1) == "5" ||  speciesData.faceDetail.Substring(0,1) == "6" ||  speciesData.faceDetail.Substring(0,1) == "7" ||
            speciesData.faceDetail.Substring(0,1) == "8" ||  speciesData.faceDetail.Substring(0,1) == "9")) {
                processBodyPart(speciesData.faceDetail);
        } else if (Random.Range(0,3) == 0) {
            processBodyPart(speciesData.faceDetail);
        }


        if (speciesData.otherBodyPartsArray != null) {
            foreach (string piece in speciesData.otherBodyPartsArray) {
                processBodyPart(piece);
            }
        }
        


		


	}


	void processBodyPart(string piece) {
        
        /*
        first check if string is empty, if its empty no image is created
        check first char of string for alpha or numeric 
        if alpha it is either modular, single selected modular file, or multiple modulars separated by ','
        if numeric it is a single species file
        */

        //check for multiple modular vs single selected modular file
        bool checkForMultipleModularFormat = false;
        if (piece.Contains(" ")) {
            checkForMultipleModularFormat = true;
        }

        if (piece == "") {
            //Do nothing


        //Numeric = single species file
        } else if (piece.Substring(0,1) == "0" || piece.Substring(0,1) == "1" ||  piece.Substring(0,1) == "2" ||  piece.Substring(0,1) == "3" ||
            piece.Substring(0,1) == "4" ||  piece.Substring(0,1) == "5" ||  piece.Substring(0,1) == "6" ||  piece.Substring(0,1) == "7" ||
            piece.Substring(0,1) == "8" ||  piece.Substring(0,1) == "9") {

            SingularSpeciesFileLoader(piece,1);

        //Alpha, not modular, not multi-modular = single modular file
        } else if (piece.Length > 4 && checkForMultipleModularFormat == false) {
            
            SingularSpeciesFileLoader(piece,2);
    
        //Alpha modular
        } else {
            ModularFeatureLoader(piece);
        }

	}


	void SingularSpeciesFileLoader(string piece, int splitNum) {
        //set address and load image
        string spriteAddress = "Assets/Images/Species/"+piece+".png";
		bodyPartSpriteAddresses.Add(spriteAddress);


        //set layer
        string[] layer = piece.Split('-');
        int actualLayer = 0;
        int.TryParse(layer[splitNum],out actualLayer);
		bodyPartLayers.Add(actualLayer);

        bool isColorSet = false;
        //cycle through skinVarianceGroups and see if color should be assigned to layer
        for (int i = 0; i < skinVarianceGroups.Count; i++) {
            for (int j = 0; j < skinVarianceGroups[i].Count; j++) {
                int varianceGroup = 0;
                int.TryParse(skinVarianceGroups[i][j],out varianceGroup);
                if (actualLayer == varianceGroup) {
                    isColorSet = true;
                    //Set Color Here
                    bodyPartSpriteColors.Add(skinVarianceColor[i]);
                    return;
                } 
            }
        }
        

        if (isColorSet == false) {
            
            //check for facial hair
            if (actualLayer == beardLayer) {
				bodyPartSpriteColors.Add(hairColor);
            } else if (actualLayer == eyeLayer) {
				bodyPartSpriteColors.Add(eyeColor);
            } else {
                bodyPartSpriteColors.Add(skinColor);
            }
        }


        
        
    }



    void ModularFeatureLoader(string modularFeatureList) {
        string[] modularFeatures = modularFeatureList.Split(' ');

        //------------ Complex Randomizer ------------//
        /*
            Feature Randomizer combines all types included in the species list and combines them for fair randomization
            i.e. 
                species allows both hair and hair with hairtop (h01,h02)
                if there are 8 h01 files and 2 h02 files, this allows the randomization to be out of the total 10 instead of a 50% chance of either type
        */
        List<string> mfType = new List<string>();
        List<int> mfCount = new List<int>();
        List<int[]> mfLayers = new List<int[]>();

        //in case there are multiple feature choices like different types of hair h01,h02 etc
        foreach (string feature in modularFeatures) {
            int matchedFeature = 0;
            for (int i = 0; i < gameData.featureCodes.itemType.Count; i++) {
                if (feature == gameData.featureCodes.itemType[i]) {
                    matchedFeature = i;
                    mfType.Add(feature);
                    mfCount.Add(gameData.featureCodes.itemAmount[matchedFeature]);
                    //Debug.Log("feature: "+feature+" amount: "+(gameData.featureCodes.itemAmount[matchedFeature]).ToString());
                    mfLayers.Add(gameData.featureCodes.layers[matchedFeature]);
                    break;
                }
            }
        }

        int ultimateCount = 0;
        foreach (int count in mfCount) {
            ultimateCount += count;
        }

        int randomSelection = 0;
        int trueSelection = 0;
        int ListSelected = 0;

        randomSelection = Random.Range(0,ultimateCount);
        trueSelection = randomSelection;
        ListSelected = 0;

        if (ultimateCount > mfCount[0]) {
            for(int i = 0; i < mfCount.Count; i++) {
                if (trueSelection >= mfCount[i]) {
                    trueSelection -= mfCount[i];
                    ListSelected++;
                } else {
                    break;
                }
            }
        }
            
        
        //------------ Complex Randomizer END ------------//



        //----------- Loads Selected Image -----------//
        string selectionString = trueSelection.ToString();
        while (selectionString.Length < 4) {
            selectionString = "0"+selectionString;
        }

        foreach (int layer in mfLayers[ListSelected]) {
			bodyPartLayers.Add(layer);

            string strLayer = layer.ToString();
            while (strLayer.Length < 3) {
                strLayer = "0"+strLayer;
            }
            string fullname = mfType[ListSelected]+"-"+selectionString+"-"+strLayer;
            
            //set address
            string spriteAddress = "Assets/Images/Species/"+fullname+".png";
			bodyPartSpriteAddresses.Add(spriteAddress);

            

            //face detail
            
            if (mfType[ListSelected] == "f06") {
                int chosenColorInt = Random.Range(0,gameData.colorCodes.equipmentColorArray.Length);
				bodyPartSpriteColors.Add(gameData.colorCodes.equipmentColorArray[chosenColorInt]);
            //eyes
            } else if (layer == eyeLayer/*mfType[ListSelected] == "f01"*/) {
                bodyPartSpriteColors.Add(eyeColor);
            //hair
            } else if (mfType[ListSelected] == "h01" || mfType[ListSelected] == "h02" || mfType[ListSelected] == "h03" || mfType[ListSelected] == "h04" || layer == beardLayer/*mfType[ListSelected] == "f05"*/) {
                bodyPartSpriteColors.Add(hairColor);
            } else {
                bodyPartSpriteColors.Add(skinColor);
            }
            
            
            
        }



    }



    public bool TryEquip(EquippableItemData itemData) {
        bool isSuccess = false;

       //Debug.Log("tryEquip: "+itemData.type);

        //check species restrictions
        /*foreach (string restriction in speciesData.itemRestrictionsArray) {
            if (itemData.type == restriction) {
                //Debug.Log(itemData.type + " restricted on "+speciesData.name);
                return isSuccess;
            }
        }*/

        //check for available matching slot
        if (itemData.type[0] == 'h' && EquipSlotHead == null) {
            EquipSlotHead = itemData;
            isSuccess = true;
        } else if (itemData.type[0] == 't' && EquipSlotTorso == null) {
            EquipSlotTorso = itemData;
            isSuccess = true;
        } else if (itemData.type[0] == 'a' && EquipSlotArms == null) {
            EquipSlotArms = itemData;
            isSuccess = true;
        } else if (itemData.type[0] == 'l' && EquipSlotLegs == null) {
            EquipSlotLegs = itemData;
            isSuccess = true;
        } else if (itemData.type[0] == 'f' && EquipSlotFeet == null) {
            EquipSlotFeet = itemData;
            isSuccess = true;
        } else if (itemData.type[0] == 'w' && EquipSlotMainHand == null && itemData.type != "w08" && itemData.type != "w09") {
            EquipSlotMainHand = itemData;
            isSuccess = true;
        } else if (itemData.type[0] == 'w' && (EquipSlotMainHand == null || (EquipSlotMainHand != null && EquipSlotMainHand.isTwoHanded == false)) && itemData.isTwoHanded == false) {
            EquipSlotOffHand = itemData;
            isSuccess = true;
        } else if (itemData.type[0] != 'w') {
            if (EquipSlotMisc1 == null) {
                EquipSlotMisc1 = itemData;
                isSuccess = true;
            } else if (EquipSlotMisc2 == null) {
                EquipSlotMisc2 = itemData;
                isSuccess = true;
            } else if (EquipSlotMisc3 == null) {
                EquipSlotMisc3 = itemData;
                isSuccess = true;
            } else if (EquipSlotMisc4 == null) {
                EquipSlotMisc4 = itemData;
                isSuccess = true;
            }
        }

        if (isSuccess == true) {
            EquippableItemData itemFound = null;
            if (gameData.inventoryEquippable != null) {
                foreach (EquippableItemData item in gameData.inventoryEquippable) {
                    if (item.uniqueID == itemData.uniqueID) {
                        itemFound = item;
                    }
                }
                gameData.inventoryEquippable.Remove(itemFound);
            }
            
           
        }

        /*Debug.Log("---------------------");
        Debug.Log("Equipped Head: "+EquipSlotHead);
        Debug.Log("Equipped Torso: "+EquipSlotTorso);
        Debug.Log("Equipped Arms: "+EquipSlotArms);
        Debug.Log("Equipped Legs: "+EquipSlotLegs);
        Debug.Log("Equipped Feet: "+EquipSlotFeet);
        Debug.Log("Equipped Misc1: "+EquipSlotMisc1);
        Debug.Log("Equipped Misc2: "+EquipSlotMisc2);
        Debug.Log("Equipped Misc3: "+EquipSlotMisc3);
        Debug.Log("Equipped Misc4: "+EquipSlotMisc4);
        Debug.Log("Equipped MainHand: "+EquipSlotMainHand);
        Debug.Log("Equipped OffHand: "+EquipSlotOffHand);
        Debug.Log("---------------------");*/


        return isSuccess;

    }

    public bool TryEquipMiscOnly(EquippableItemData itemData) {
        bool isSuccess = false;

       //Debug.Log("tryEquip: "+itemData.type);

        //check species restrictions
        /*foreach (string restriction in speciesData.itemRestrictionsArray) {
            if (itemData.type == restriction) {
                //Debug.Log(itemData.type + " restricted on "+speciesData.name);
                return isSuccess;
            }
        }*/

        
        if (EquipSlotMisc1 == null) {
            EquipSlotMisc1 = itemData;
            isSuccess = true;
        } else if (EquipSlotMisc2 == null) {
            EquipSlotMisc2 = itemData;
            isSuccess = true;
        } else if (EquipSlotMisc3 == null) {
            EquipSlotMisc3 = itemData;
            isSuccess = true;
        } else if (EquipSlotMisc4 == null) {
            EquipSlotMisc4 = itemData;
            isSuccess = true;
        }
        

        if (isSuccess == true) {
            EquippableItemData itemFound = null;
            foreach (EquippableItemData item in gameData.inventoryEquippable) {
                if (item.uniqueID == itemData.uniqueID) {
                    itemFound = item;
                }
            }
            gameData.inventoryEquippable.Remove(itemFound);
        }

        /*Debug.Log("---------------------");
       Debug.Log("Equipped Head: "+EquipSlotHead);
        Debug.Log("Equipped Torso: "+EquipSlotTorso);
        Debug.Log("Equipped Arms: "+EquipSlotArms);
        Debug.Log("Equipped Legs: "+EquipSlotLegs);
        Debug.Log("Equipped Feet: "+EquipSlotFeet);
        Debug.Log("Equipped Misc1: "+EquipSlotMisc1);
        Debug.Log("Equipped Misc2: "+EquipSlotMisc2);
        Debug.Log("Equipped Misc3: "+EquipSlotMisc3);
        Debug.Log("Equipped Misc4: "+EquipSlotMisc4);
        Debug.Log("Equipped MainHand: "+EquipSlotMainHand);
        Debug.Log("Equipped OffHand: "+EquipSlotOffHand);
        Debug.Log("---------------------");*/
        return isSuccess;

    }


    public void UnEquip(EquippableItemData itemSlot) {
        if (EquipSlotHead == itemSlot) {
            EquipSlotHead = null;
        } else if (EquipSlotTorso == itemSlot) {
            EquipSlotTorso = null;
        } else if (EquipSlotArms == itemSlot) {
            EquipSlotArms = null;
        } else if (EquipSlotLegs == itemSlot) {
            EquipSlotLegs = null;
        } else if (EquipSlotFeet == itemSlot) {
            EquipSlotFeet = null;
        } else if (EquipSlotMisc1 == itemSlot) {
            EquipSlotMisc1 = EquipSlotMisc2;
            EquipSlotMisc2 = EquipSlotMisc3;
            EquipSlotMisc3 = EquipSlotMisc4;
            EquipSlotMisc4 = null;
        } else if (EquipSlotMisc2 == itemSlot) {
            EquipSlotMisc2 = EquipSlotMisc3;
            EquipSlotMisc3 = EquipSlotMisc4;
            EquipSlotMisc4 = null;
        } else if (EquipSlotMisc3 == itemSlot) {
            EquipSlotMisc3 = EquipSlotMisc4;
            EquipSlotMisc4 = null;
        } else if (EquipSlotMisc4 == itemSlot) {
            EquipSlotMisc4 = null;
        } else if (EquipSlotMainHand == itemSlot) {
            //moves dual weilding off hands to main hand
            if (EquipSlotOffHand != null && EquipSlotOffHand.type != "w08" && EquipSlotOffHand.type != "w09") {
                EquipSlotMainHand = EquipSlotOffHand;
                EquipSlotOffHand = null;
            } else {
                EquipSlotMainHand = null;
            }
            
        } else if (EquipSlotOffHand == itemSlot) {
            EquipSlotOffHand = null;
        }
        gameData.inventoryEquippable.Add(itemSlot);
        /*
        Debug.Log("---------------------");
        Debug.Log("Equipped Head: "+EquipSlotHead);
        Debug.Log("Equipped Torso: "+EquipSlotTorso);
        Debug.Log("Equipped Arms: "+EquipSlotArms);
        Debug.Log("Equipped Legs: "+EquipSlotLegs);
        Debug.Log("Equipped Feet: "+EquipSlotFeet);
        Debug.Log("Equipped Misc1: "+EquipSlotMisc1);
        Debug.Log("Equipped Misc2: "+EquipSlotMisc2);
        Debug.Log("Equipped Misc3: "+EquipSlotMisc3);
        Debug.Log("Equipped Misc4: "+EquipSlotMisc4);
        Debug.Log("Equipped MainHand: "+EquipSlotMainHand);
        Debug.Log("Equipped OffHand: "+EquipSlotOffHand);
        Debug.Log("---------------------");*/
        
    }

    public void UnEquipWithoutMovingMisc(EquippableItemData itemSlot) {
        if (EquipSlotHead == itemSlot) {
            EquipSlotHead = null;
        } else if (EquipSlotTorso == itemSlot) {
            EquipSlotTorso = null;
        } else if (EquipSlotArms == itemSlot) {
            EquipSlotArms = null;
        } else if (EquipSlotLegs == itemSlot) {
            EquipSlotLegs = null;
        } else if (EquipSlotFeet == itemSlot) {
            EquipSlotFeet = null;
        } else if (EquipSlotMisc1 == itemSlot) {
            EquipSlotMisc1 = null;
        } else if (EquipSlotMisc2 == itemSlot) {
            EquipSlotMisc2 = null;
        } else if (EquipSlotMisc3 == itemSlot) {
            EquipSlotMisc3 = null;
        } else if (EquipSlotMisc4 == itemSlot) {
            EquipSlotMisc4 = null;
        } else if (EquipSlotMainHand == itemSlot) {
            EquipSlotMainHand = null;
        } else if (EquipSlotOffHand == itemSlot) {
            EquipSlotOffHand = null;
        }
        gameData.inventoryEquippable.Add(itemSlot);
        /*
        Debug.Log("---------------------");
        Debug.Log("Equipped Head: "+EquipSlotHead);
        Debug.Log("Equipped Torso: "+EquipSlotTorso);
        Debug.Log("Equipped Arms: "+EquipSlotArms);
        Debug.Log("Equipped Legs: "+EquipSlotLegs);
        Debug.Log("Equipped Feet: "+EquipSlotFeet);
        Debug.Log("Equipped Misc1: "+EquipSlotMisc1);
        Debug.Log("Equipped Misc2: "+EquipSlotMisc2);
        Debug.Log("Equipped Misc3: "+EquipSlotMisc3);
        Debug.Log("Equipped Misc4: "+EquipSlotMisc4);
        Debug.Log("Equipped MainHand: "+EquipSlotMainHand);
        Debug.Log("Equipped OffHand: "+EquipSlotOffHand);
        Debug.Log("---------------------");*/
        
    }



    public bool TryEquipReplace(EquippableItemData itemData, EquippableItemData itemSlot) {
        bool isSuccess = false;

        //Debug.Log("TryEquipReplace: "+itemData.type);

        //check species restrictions
        /*foreach (string restriction in speciesData.itemRestrictionsArray) {
            if (itemData.type == restriction) {
                Debug.Log(itemData.type + " restricted on "+speciesData.name);
                return isSuccess;
            }
        }*/

        if (itemData.type[0] == 'h' && itemSlot == EquipSlotHead) {
            UnEquip(itemSlot);
            EquipSlotHead = itemData;
            isSuccess = true;

        } else if (itemData.type[0] == 't' && itemSlot == EquipSlotTorso) {
            UnEquip(itemSlot);
            EquipSlotTorso = itemData;
            isSuccess = true;

        } else if (itemData.type[0] == 'a' && itemSlot == EquipSlotArms) {
            UnEquip(itemSlot);
            EquipSlotArms = itemData;
            isSuccess = true;

        } else if (itemData.type[0] == 'l' && itemSlot == EquipSlotLegs) {
            UnEquip(itemSlot);
            EquipSlotLegs = itemData;
            isSuccess = true;

        } else if (itemData.type[0] == 'f' && itemSlot == EquipSlotFeet) {
            UnEquip(itemSlot);
            EquipSlotFeet = itemData;
            isSuccess = true;

        } else if (itemData.type[0] == 'w' && itemSlot == EquipSlotMainHand && itemData.type != "w08" && itemData.type != "w09") {
            gameData.inventoryEquippable.Add(itemSlot);
            EquipSlotMainHand = null;
            EquipSlotMainHand = itemData;
            isSuccess = true;

        } else if (itemData.type[0] == 'w' && itemSlot == EquipSlotOffHand && (EquipSlotMainHand == null || (EquipSlotMainHand != null && EquipSlotMainHand.isTwoHanded == false)) && itemData.isTwoHanded == false) {
            UnEquip(itemSlot);
            EquipSlotOffHand = itemData;
            isSuccess = true;

        } else if (itemData.type[0] != 'w') {
            if (itemSlot == EquipSlotMisc1) {
                UnEquipWithoutMovingMisc(itemSlot);
                EquipSlotMisc1 = itemData;
                isSuccess = true;

            } else if (itemSlot == EquipSlotMisc2) {
                UnEquipWithoutMovingMisc(itemSlot);
                EquipSlotMisc2 = itemData;
                isSuccess = true;

            } else if (itemSlot == EquipSlotMisc3) {
                UnEquipWithoutMovingMisc(itemSlot);
                EquipSlotMisc3 = itemData;
                isSuccess = true;

            } else if (itemSlot == EquipSlotMisc4) {
                UnEquipWithoutMovingMisc(itemSlot);
                EquipSlotMisc4 = itemData;
                isSuccess = true;

            }
        }

        if (isSuccess == true) {
            EquippableItemData itemFound = null;
            foreach (EquippableItemData item in gameData.inventoryEquippable) {
                if (item.uniqueID == itemData.uniqueID) {
                    itemFound = item;
                }
            }
            gameData.inventoryEquippable.Remove(itemFound);
        }

        /*Debug.Log("---------------------");
        Debug.Log("Equipped Head: "+EquipSlotHead);
        Debug.Log("Equipped Torso: "+EquipSlotTorso);
        Debug.Log("Equipped Arms: "+EquipSlotArms);
        Debug.Log("Equipped Legs: "+EquipSlotLegs);
        Debug.Log("Equipped Feet: "+EquipSlotFeet);
        Debug.Log("Equipped Misc1: "+EquipSlotMisc1);
        Debug.Log("Equipped Misc2: "+EquipSlotMisc2);
        Debug.Log("Equipped Misc3: "+EquipSlotMisc3);
        Debug.Log("Equipped Misc4: "+EquipSlotMisc4);
        Debug.Log("Equipped MainHand: "+EquipSlotMainHand);
        Debug.Log("Equipped OffHand: "+EquipSlotOffHand);
        Debug.Log("---------------------");*/
        return isSuccess;
    }


    public List<EquippableItemData> GetListOfEquippedItems() {
        List<EquippableItemData> EquippedItems = new List<EquippableItemData>();
        if (EquipSlotHead != null) {
            EquippedItems.Add(EquipSlotHead);
        }
        if (EquipSlotTorso != null) {
            EquippedItems.Add(EquipSlotTorso);
        }
        if (EquipSlotArms != null) {
            EquippedItems.Add(EquipSlotArms);
        }
        if (EquipSlotLegs != null) {
            EquippedItems.Add(EquipSlotLegs);
        }
        if (EquipSlotFeet != null) {
            EquippedItems.Add(EquipSlotFeet);
        }
        if (EquipSlotMisc1 != null) {
            EquippedItems.Add(EquipSlotMisc1);
        }
        if (EquipSlotMisc2 != null) {
            EquippedItems.Add(EquipSlotMisc2);
        }
        if (EquipSlotMisc3 != null) {
            EquippedItems.Add(EquipSlotMisc3);
        }
        if (EquipSlotMisc4 != null) {
            EquippedItems.Add(EquipSlotMisc4);
        }
        if (EquipSlotMainHand != null) {
            EquippedItems.Add(EquipSlotMainHand);
        }
        if (EquipSlotOffHand != null) {
            EquippedItems.Add(EquipSlotOffHand);
        }


        return EquippedItems;
    }



	public void CalculateHpMp()
    {
		//calculate health and magic based off of stats
    }



	

}
