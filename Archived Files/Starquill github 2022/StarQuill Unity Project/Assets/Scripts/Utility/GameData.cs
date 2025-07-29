 using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class GameData : MonoBehaviour
{
    public static GameData localData;
    [HideInInspector] public bool isGamePaused;
    [HideInInspector] public bool isControlRestricted;
    
    private int itemUniqueID;


    [HideInInspector] public List<CharacterData> partyData;
    [HideInInspector] public List<EquippableItemData> inventoryEquippable;

    //--- questdata may have to be both for parties and individuals
    [HideInInspector] public List<QuestData> partyQuestData;
    [HideInInspector] public QuestData activeQuest;

    

    public GameObject heightCheckObject;
    public GameObject[] locationObjectPrefabs;

    [HideInInspector] public LocationData currentLocationData;
    [HideInInspector] public List<MapData> mapArray;
    [HideInInspector] public int currentMap;


    ///////////////////
    //-- CSV FILES --//
    ///////////////////
    [Space(10)]
    [Header ("Layer csv")]
    public TextAsset csvEquipCodes;
    public TextAsset csvFeatureCodes;
    public TextAsset csvMonsterCodes;
    public TextAsset csvSpeciesCodes;

    [Header ("Color csv")]
    public TextAsset csvColorsEquipment;
    public TextAsset csvColorsEyes;
    public TextAsset csvColorsHumanSkin;
    public TextAsset csvColorsMain;
    public TextAsset csvColorsTerrain;
    public TextAsset csvColorsWeapon;

    
    [Header ("Naming csv")]
    public TextAsset  csvCityNames;
    public TextAsset  csvCityVariables;
    public TextAsset  csvDungeonNames;
    public TextAsset  csvEquipmentPrefix;
    public TextAsset  csvEquipmentSuffix;
    public TextAsset  csvEquipmentTypes;
    public TextAsset  csvMonsterName;
    public TextAsset  csvMonsterPrefix;
    public TextAsset  csvMonsterSuffix;
    public TextAsset  csvNPCNames;

    [Header ("Quest csv")]
    public TextAsset csvAction;
    public TextAsset csvBoring;
    public TextAsset csvConclusion;
    public TextAsset csvEngagement;
    public TextAsset csvGetReward;
    public TextAsset csvGreeting;
    public TextAsset csvIntro;
    public TextAsset csvOutro;
    public  TextAsset csvPlotQuestStep;
    public  TextAsset csvPlotQuestGiven;
    public TextAsset csvPromiseOfReward;
    public  TextAsset csvQuestItemPurpose;
    public TextAsset csvQuestItem;
    public  TextAsset csvReference;
    public  TextAsset csvSegue;
    public TextAsset csvSupplication;
    public  TextAsset csvThreat;


    ///////////////////////
    //-- END CSV FILES --//
    ///////////////////////

    //-- Terrain
    [Space(10)]
    [Header ("Terrain")]
    //public Sprite[][] terrainMiniMapTiles;
    public Sprite[] terrain2dBackground;
    public Material[] terrain3dSkybox;
    public Sprite[] terrainStarImage;
    public GameObject terrainStarPrefab;


    //-- Processed/Separated CSV Files (Codes)
    [HideInInspector] public EquipCodesCSV equipCodes;
    [HideInInspector] public FeatureCodesCSV featureCodes;
    [HideInInspector] public EquipmentNamingCSV equipmentNamingCodes;
    [HideInInspector] public QuestCSV questCSV;
    [HideInInspector] public ColorCodesCSV colorCodes {get; private set;}
    [HideInInspector] public MonsterCodesCSV monsterCodes;
    [HideInInspector] public SpeciesCodesCSV speciesArray; //the only CSV codes used as a complete representation of the type.




    void Awake()
    {

        if (localData == null)
        {
            DontDestroyOnLoad(gameObject);
            localData = this;

            CreateNewGameData();

        }
        else if (localData != null)
        {
            Destroy(gameObject);
        }

    }



    void CreateNewGameData() {

        isGamePaused = false;
        isControlRestricted = false;

        //-- Setup Processed/Separated CSV Files (Codes)
        equipCodes = new EquipCodesCSV(csvEquipCodes);
        featureCodes = new FeatureCodesCSV(csvFeatureCodes);
        colorCodes = new ColorCodesCSV(csvColorsMain,csvColorsEyes,csvColorsEquipment,csvColorsHumanSkin,csvColorsWeapon,csvColorsTerrain);
        equipmentNamingCodes = new EquipmentNamingCSV(csvEquipmentTypes,csvEquipmentPrefix,csvEquipmentSuffix);
        monsterCodes = new MonsterCodesCSV(csvMonsterCodes);
        speciesArray = new SpeciesCodesCSV(csvSpeciesCodes);
        Debug.Log("questCSV not reflecting new categories");
        questCSV = new QuestCSV( csvIntro, csvOutro, csvPromiseOfReward, csvEngagement, csvConclusion, csvGetReward, 
                csvReference, csvQuestItemPurpose, csvQuestItem, csvAction,  csvPlotQuestStep,  csvThreat, csvGreeting, csvSupplication);


        //////////////////////////////////
        //      Generate New Map        //
        //////////////////////////////////

        currentMap = 0;
        mapArray = new List<MapData>();
        int mapSize = 20;
        //Vector3 triangleScale = new Vector3(25,10,25);
        Vector3 triangleScale = new Vector3(5,.5f,5);
        mapArray.Add(new MapData(mapSize, triangleScale));
        



        inventoryEquippable = new List<EquippableItemData>();



        partyQuestData = new List<QuestData>();
        
        //-- Test Player --//
        partyData = new List<CharacterData>();
        CreateRandomPartyCharacter();
        CreateRandomPartyCharacter();
        CreateRandomPartyCharacter();
        CreateRandomPartyCharacter();






    }
    


    void CreateRandomPartyCharacter() {
        int selectedSpecies = UnityEngine.Random.Range(0,speciesArray.species.Count);
        
        partyData.Add(new CharacterData(speciesArray.species[selectedSpecies], 1, 1, 1, 1, 1, 1));

        CreateEquipmentStarterPackDefault(partyData[partyData.Count-1],4,5);

        //add random equippable inventory to character data
        for (int i = 0; i < UnityEngine.Random.Range(10,15); i++) {
            EquippableItemData newItem = new EquippableItemData();
            inventoryEquippable.Add(newItem);
        }

    }




    
/////////////// EQUIPMENT STARTER PACK VARIABLES
        string[] helmets;
        string[] eyes;

        string[] dress;
        string[] shirt;
        string[] pants;
        string[] shoes;
        string[] arms;
        string[] back;
        string[] ring;
        string[] neck;
        string[] belt;
        string[] earring;
        string[] shoulders;
        string[] coat;
        string[] overbelt;
        string[] armor;
        string[] weapons;
        string[] shield;

        bool dressBool;
        bool shirtBool;
        bool pantsBool;
        bool shoesBool;
        bool helmetBool;
        bool eyeBool;
        bool armsBool;
        bool backBool;
        bool ringBool;
        bool neckBool;
        bool beltBool;
        bool earringBool;
        bool shouldersBool;
        bool coatBool;
        bool overbeltBool;
        bool armorBool;
        bool weaponsBool;
        bool weaponsBool2;
        bool shieldBool;


    public void CreateEquipmentStarterPackDefault(CharacterData characterData, int minEquipped, int maxEquipped) {
        //4 5
        helmets = new string[] {"hd01","hd02","hd03","hd04","hd05","hd06","hd07","hd08","hd10"};
        eyes = new string[] {"hd09","hd11","hd12","hd13"};

        dress = new string[] {"tr05","tr06"};
        shirt = new string[] {"tr01","tr02","tr03","tr04","tr07"};
        pants = new string[] {"lg01","lg02"};
        shoes = new string[] {"fe01","fe02"};
        arms = new string[] {"ar01","ar02","ar03","ar04","ar05"};
        back = new string[] {"mc03","mc04","mc06","mc07"};
        ring = new string[] {"mc05"};
        neck = new string[] {"mc10","mc11","mc08"};
        belt = new string[] {"mc12","mc13"};
        earring = new string[] {"mc09"};
        shoulders = new string[] {"mc01","mc02","tr13"};
        coat = new string[] {"tr08","tr09"};
        overbelt = new string[] {"tr10"};
        armor = new string[] {"tr11","tr12"};
        weapons = new string[] {"w01","w02","w03","w04","w05","w06","w07"};
        shield = new string[] {"w08","w09"};

        dressBool = false;
        shirtBool = false;
        pantsBool = false;
        shoesBool = false;
        helmetBool = false;
        eyeBool = false;
        armsBool = false;
        backBool = false;
        ringBool = false;
        neckBool = false;
        beltBool = false;
        earringBool = false;
        shouldersBool = false;
        coatBool = false;
        overbeltBool = false;
        armorBool = false;
        weaponsBool = false;
        weaponsBool2 = false;
        shieldBool = false;

        int minEquipment = minEquipped;
        int maxEquipment = maxEquipped;


        CheckSlotsForPreEquipped(characterData.EquipSlotHead);
        CheckSlotsForPreEquipped(characterData.EquipSlotTorso);
        CheckSlotsForPreEquipped(characterData.EquipSlotLegs);
        CheckSlotsForPreEquipped(characterData.EquipSlotFeet);
        CheckSlotsForPreEquipped(characterData.EquipSlotArms);
        CheckSlotsForPreEquipped(characterData.EquipSlotMisc1);
        CheckSlotsForPreEquipped(characterData.EquipSlotMisc2);
        CheckSlotsForPreEquipped(characterData.EquipSlotMisc3);
        CheckSlotsForPreEquipped(characterData.EquipSlotMisc4);
        CheckSlotsForPreEquipped(characterData.EquipSlotMainHand);
        CheckSlotsForPreEquipped(characterData.EquipSlotOffHand);

         
        int randomChoice = UnityEngine.Random.Range(0,100);

        //step 1 20% chance to get one of these (tr05 tr06), then skip to step 4 
        if (randomChoice < 20 && dressBool == false && shirtBool == false) {
            randomChoice = UnityEngine.Random.Range(0,2);
            if (randomChoice == 0) {
                equipSpecificGroup(characterData,"tr05");
            } else {
                equipSpecificGroup(characterData,"tr06");
            }
        //step 2 80% chance of having one from the following categories: tr01 tr02 tr03 tr04 tr07
        } else if (randomChoice >= 20 && shirtBool == false && dressBool == false) {
                randomChoice = UnityEngine.Random.Range(0,5);
                if (randomChoice == 0) {
                    equipSpecificGroup(characterData,"tr01");
                } else if (randomChoice == 1){
                    equipSpecificGroup(characterData,"tr02");
                } else if (randomChoice == 2) {
                    equipSpecificGroup(characterData,"tr03");
                } else if (randomChoice == 3) {
                    equipSpecificGroup(characterData,"tr04");
                } else {
                    equipSpecificGroup(characterData,"tr07");
                }

                // If step 2 was used, 80% chance of having one from the following: lg01 lg02
                randomChoice = UnityEngine.Random.Range(0,2);
                if (randomChoice == 0) {
                    equipSpecificGroup(characterData,"lg01");
                } else {
                    equipSpecificGroup(characterData,"lg02");
                }
        }

        //if shirt was pre-selected reinclude pants chance
        if (shirtBool == true && pantsBool == false) {
            randomChoice = UnityEngine.Random.Range(0,2);
            if (randomChoice == 0) {
                equipSpecificGroup(characterData,"lg01");
            } else {
                equipSpecificGroup(characterData,"lg02");
            }
        }   



        //step 3 50% chance of fe01, 40% chance of fe02, 10% empty
        randomChoice = UnityEngine.Random.Range(0,100);
        if (shoesBool == false) {
            if (randomChoice >= 50) {
                equipSpecificGroup(characterData,"fe01");
            } else if (randomChoice < 50 && randomChoice >= 10) {
                equipSpecificGroup(characterData,"fe02");
            }
        }
        

        // step 4 everything else
        for (int i = 0; i < UnityEngine.Random.Range(minEquipment,maxEquipment); i++) {

            bool successfullyChosen = false;
            do {
                    randomChoice = UnityEngine.Random.Range(0,14);
                    if (randomChoice == 15) {
                        if (shieldBool == false) {
                            shieldBool = true;
                            randomChoice = UnityEngine.Random.Range(0,shield.Length);
                            equipSpecificGroup(characterData,shield[randomChoice]);
                            successfullyChosen = true;
                        }
                        
                    } else if (randomChoice == 1) {
                        if (helmetBool == false) {
                            helmetBool = true;
                            randomChoice = UnityEngine.Random.Range(0,helmets.Length);
                            equipSpecificGroup(characterData,helmets[randomChoice]);
                            successfullyChosen = true;
                        }
                    } else if (randomChoice == 2) {
                        if (eyeBool == false) {
                            eyeBool = true;
                            randomChoice = UnityEngine.Random.Range(0,eyes.Length);
                            equipSpecificGroup(characterData,eyes[randomChoice]);
                            successfullyChosen = true;
                        }
                        
                    } else if (randomChoice == 3) {
                        if (armsBool == false) {
                            armsBool = true;
                            randomChoice = UnityEngine.Random.Range(0,arms.Length);
                            equipSpecificGroup(characterData,arms[randomChoice]);
                            successfullyChosen = true;
                        }
                        
                    } else if (randomChoice == 4) {
                        if (backBool == false) {
                            backBool = true;
                            randomChoice = UnityEngine.Random.Range(0,back.Length);
                            equipSpecificGroup(characterData,back[randomChoice]);
                            successfullyChosen = true;
                        }
                        
                    } else if (randomChoice == 5) {
                        if ((weaponsBool == false && weaponsBool2 == false) || (weaponsBool == true && weaponsBool2 == false) || (weaponsBool == false && weaponsBool2 == true)) {
                            
                            
                            randomChoice = UnityEngine.Random.Range(0,weapons.Length);
                            equipSpecificGroup(characterData,weapons[randomChoice]);
                            
                            if (weapons[randomChoice] == "w07" || weapons[randomChoice] == "w05" || weapons[randomChoice] == "w04") {
                                weaponsBool = true;
                                weaponsBool2 = false;
                            } else {
                                if (weaponsBool == false && weaponsBool2 == false) {
                                    weaponsBool = true;
                                } else {
                                    weaponsBool2 = true;
                                }
                                
                            }
                            successfullyChosen = true;
                        }
                    } else if (randomChoice == 6) {
                        if (ringBool == false) {
                            ringBool = true;
                            randomChoice = UnityEngine.Random.Range(0,ring.Length);
                            equipSpecificGroup(characterData,ring[randomChoice]);
                            successfullyChosen = true;
                        }
                        
                    } else if (randomChoice == 7) {
                        if (neckBool == false) {
                            neckBool = true;
                            randomChoice = UnityEngine.Random.Range(0,neck.Length);
                            equipSpecificGroup(characterData,neck[randomChoice]);
                            successfullyChosen = true;
                        }
                        
                    } else if (randomChoice == 8) {
                        if (beltBool == false) {
                            beltBool = true;
                            randomChoice = UnityEngine.Random.Range(0,belt.Length);
                            equipSpecificGroup(characterData,belt[randomChoice]);
                            successfullyChosen = true;
                        }
                        
                    } else if (randomChoice == 9) {
                        if (earringBool == false) {
                            earringBool = true;
                            randomChoice = UnityEngine.Random.Range(0,earring.Length);
                            equipSpecificGroup(characterData,earring[randomChoice]);
                            successfullyChosen = true;
                        }
                        
                    } else if (randomChoice == 10) {
                        if (shouldersBool == false) {
                            shouldersBool = true;
                            randomChoice = UnityEngine.Random.Range(0,shoulders.Length);
                            equipSpecificGroup(characterData,shoulders[randomChoice]);
                            successfullyChosen = true;
                        }
                        
                    } else if (randomChoice == 11) {
                        if (coatBool == false) {
                            coatBool = true;
                            randomChoice = UnityEngine.Random.Range(0,coat.Length);
                            equipSpecificGroup(characterData,coat[randomChoice]);
                            successfullyChosen = true;
                        }
                        
                    } else if (randomChoice == 12) {
                        if (overbeltBool == false) {
                            overbeltBool = true;
                            randomChoice = UnityEngine.Random.Range(0,overbelt.Length);
                            equipSpecificGroup(characterData,overbelt[randomChoice]);
                            successfullyChosen = true;
                        }
                        
                    } else if (randomChoice == 13) {
                        if (armorBool == false) {
                            armorBool = true;
                            randomChoice = UnityEngine.Random.Range(0,armor.Length);
                            equipSpecificGroup(characterData,armor[randomChoice]);
                            successfullyChosen = true;
                        }
                        
                    }



                } while (successfullyChosen == false);



        }



    void CheckSlotsForPreEquipped(EquippableItemData equipSlot) {
        if (equipSlot == null) {return;}
        foreach (string item in helmets) {if (equipSlot.type == item) {helmetBool = true;}}
        foreach (string item in eyes) {if (equipSlot.type == item) {eyeBool = true;}}
        foreach (string item in dress) {if (equipSlot.type == item) {dressBool = true;}}
        foreach (string item in shirt) {if (equipSlot.type == item) {shirtBool = true;}}
        foreach (string item in pants) {if (equipSlot.type == item) {pantsBool = true;}}
        foreach (string item in shoes) {if (equipSlot.type == item) {shoesBool = true;}}
        foreach (string item in arms) {if (equipSlot.type == item) {armsBool = true;}}
        foreach (string item in back) {if (equipSlot.type == item) {backBool = true;}}
        foreach (string item in ring) {if (equipSlot.type == item) {ringBool = true;}}
        foreach (string item in neck) {if (equipSlot.type == item) {neckBool = true;}}
        foreach (string item in belt) {if (equipSlot.type == item) {beltBool = true;}}
        foreach (string item in earring) {if (equipSlot.type == item) {earringBool = true;}}
        foreach (string item in shoulders) {if (equipSlot.type == item) {shouldersBool = true;}}
        foreach (string item in coat) {if (equipSlot.type == item) {coatBool = true;}}
        foreach (string item in overbelt) {if (equipSlot.type == item) {overbeltBool = true;}}
        foreach (string item in armor) {if (equipSlot.type == item) {armorBool = true;}}
        foreach (string item in weapons) {if (equipSlot.type == item) {weaponsBool = true;}}
        foreach (string item in shield) {if (equipSlot.type == item) {shieldBool = true;}}
    }

        ////////////////////////// FORCE WEAPON TEST
        randomChoice = UnityEngine.Random.Range(0,2);
        if (randomChoice == 0) {
            if ((weaponsBool == false && weaponsBool2 == false) || (weaponsBool == true && weaponsBool2 == false) || (weaponsBool == false && weaponsBool2 == true)) {
                            
            randomChoice = UnityEngine.Random.Range(0,weapons.Length);
            equipSpecificGroup(characterData,weapons[randomChoice]);
            if (weapons[randomChoice] == "w07" || weapons[randomChoice] == "w05" || weapons[randomChoice] == "w04") {
                weaponsBool = true;
                weaponsBool2 = false;
            } else {
                if (weaponsBool == false && weaponsBool2 == false) {
                    weaponsBool = true;
                } else {
                    weaponsBool2 = true;
                }
                                
            }
            }
        }
        
        ////////////////////////// FORCE WEAPON TEST

    }






    void equipSpecificGroup(CharacterData characterData, string itemType) {
        string[] itemTypes = new string[] {itemType};
        EquippableItemData newItem = new EquippableItemData(itemTypes);
        if (characterData.TryEquip(newItem) == false) {
            newItem = new EquippableItemData(itemTypes);
        }
    }







    public List<EquippableItemData> SortInventoryEquippable(string type) {
        List<EquippableItemData> sortedList = new List<EquippableItemData>();

        if (type == "" || type == "Head" || type == "Misc") {
            foreach (EquippableItemData item in inventoryEquippable) {
            if (item.type == "hd01") {sortedList.Add(item);}
        }
        foreach (EquippableItemData item in inventoryEquippable) {
            if (item.type == "hd02") {sortedList.Add(item);}
        }
        foreach (EquippableItemData item in inventoryEquippable) {
            if (item.type == "hd03") {sortedList.Add(item);}
        }
        foreach (EquippableItemData item in inventoryEquippable) {
            if (item.type == "hd04") {sortedList.Add(item);}
        }
        foreach (EquippableItemData item in inventoryEquippable) {
            if (item.type == "hd05") {sortedList.Add(item);}
        }
        foreach (EquippableItemData item in inventoryEquippable) {
            if (item.type == "hd06") {sortedList.Add(item);}
        }
        foreach (EquippableItemData item in inventoryEquippable) {
            if (item.type == "hd07") {sortedList.Add(item);}
        }
        foreach (EquippableItemData item in inventoryEquippable) {
            if (item.type == "hd08") {sortedList.Add(item);}
        }
        foreach (EquippableItemData item in inventoryEquippable) {
            if (item.type == "hd09") {sortedList.Add(item);}
        }
        foreach (EquippableItemData item in inventoryEquippable) {
            if (item.type == "hd10") {sortedList.Add(item);}
        }
        foreach (EquippableItemData item in inventoryEquippable) {
            if (item.type == "hd11") {sortedList.Add(item);}
        }
        foreach (EquippableItemData item in inventoryEquippable) {
            if (item.type == "hd12") {sortedList.Add(item);}
        }
        foreach (EquippableItemData item in inventoryEquippable) {
            if (item.type == "hd13") {sortedList.Add(item);}
        }
        }
        

        if (type == "" || type == "Torso" || type == "Misc") {
            foreach (EquippableItemData item in inventoryEquippable) {
            if (item.type == "tr01") {sortedList.Add(item);}
        }
        foreach (EquippableItemData item in inventoryEquippable) {
            if (item.type == "tr02") {sortedList.Add(item);}
        }
        foreach (EquippableItemData item in inventoryEquippable) {
            if (item.type == "tr03") {sortedList.Add(item);}
        }
        foreach (EquippableItemData item in inventoryEquippable) {
            if (item.type == "tr04") {sortedList.Add(item);}
        }
        foreach (EquippableItemData item in inventoryEquippable) {
            if (item.type == "tr05") {sortedList.Add(item);}
        }
        foreach (EquippableItemData item in inventoryEquippable) {
            if (item.type == "tr06") {sortedList.Add(item);}
        }
        foreach (EquippableItemData item in inventoryEquippable) {
            if (item.type == "tr07") {sortedList.Add(item);}
        }
        foreach (EquippableItemData item in inventoryEquippable) {
            if (item.type == "tr08") {sortedList.Add(item);}
        }
        foreach (EquippableItemData item in inventoryEquippable) {
            if (item.type == "tr09") {sortedList.Add(item);}
        }
        foreach (EquippableItemData item in inventoryEquippable) {
            if (item.type == "tr10") {sortedList.Add(item);}
        }
        foreach (EquippableItemData item in inventoryEquippable) {
            if (item.type == "tr11") {sortedList.Add(item);}
        }
        foreach (EquippableItemData item in inventoryEquippable) {
            if (item.type == "tr12") {sortedList.Add(item);}
        }
        foreach (EquippableItemData item in inventoryEquippable) {
            if (item.type == "tr13") {sortedList.Add(item);}
        }
        }
        

        if (type == "" || type == "Hands" || type == "Misc") {
            foreach (EquippableItemData item in inventoryEquippable) {
            if (item.type == "ar01") {sortedList.Add(item);}
        }
        foreach (EquippableItemData item in inventoryEquippable) {
            if (item.type == "ar02") {sortedList.Add(item);}
        }
        foreach (EquippableItemData item in inventoryEquippable) {
            if (item.type == "ar03") {sortedList.Add(item);}
        }
        foreach (EquippableItemData item in inventoryEquippable) {
            if (item.type == "ar04") {sortedList.Add(item);}
        }
        foreach (EquippableItemData item in inventoryEquippable) {
            if (item.type == "ar05") {sortedList.Add(item);}
        }
        }
        

        if (type == "" || type == "Legs" || type == "Misc") {
            foreach (EquippableItemData item in inventoryEquippable) {
            if (item.type == "lg01") {sortedList.Add(item);}
            if (item.type == "lg02") {sortedList.Add(item);}
        }
        }
        

        if (type == "" || type == "Feet" || type == "Misc") {
            foreach (EquippableItemData item in inventoryEquippable) {
            if (item.type == "fe01") {sortedList.Add(item);}
        }
        foreach (EquippableItemData item in inventoryEquippable) {
            if (item.type == "fe02") {sortedList.Add(item);}
        }
        }
        

        if (type == "" || type == "Misc") {
            foreach (EquippableItemData item in inventoryEquippable) {
            if (item.type == "mc01") {sortedList.Add(item);}
        }
        foreach (EquippableItemData item in inventoryEquippable) {
            if (item.type == "mc02") {sortedList.Add(item);}
        }
        foreach (EquippableItemData item in inventoryEquippable) {
            if (item.type == "mc03") {sortedList.Add(item);}
        }
        foreach (EquippableItemData item in inventoryEquippable) {
            if (item.type == "mc04") {sortedList.Add(item);}
        }
        foreach (EquippableItemData item in inventoryEquippable) {
            if (item.type == "mc05") {sortedList.Add(item);}
        }
        foreach (EquippableItemData item in inventoryEquippable) {
            if (item.type == "mc06") {sortedList.Add(item);}
        }
        foreach (EquippableItemData item in inventoryEquippable) {
            if (item.type == "mc07") {sortedList.Add(item);}
        }
        foreach (EquippableItemData item in inventoryEquippable) {
            if (item.type == "mc08") {sortedList.Add(item);}
        }
        foreach (EquippableItemData item in inventoryEquippable) {
            if (item.type == "mc09") {sortedList.Add(item);}
        }
        foreach (EquippableItemData item in inventoryEquippable) {
            if (item.type == "mc10") {sortedList.Add(item);}
        }
        foreach (EquippableItemData item in inventoryEquippable) {
            if (item.type == "mc11") {sortedList.Add(item);}
        }
        foreach (EquippableItemData item in inventoryEquippable) {
            if (item.type == "mc12") {sortedList.Add(item);}
        }
        foreach (EquippableItemData item in inventoryEquippable) {
            if (item.type == "mc13") {sortedList.Add(item);}
        }
        }
        

        if (type == "" || type == "Main Hand" || type == "Off Hand") {
            foreach (EquippableItemData item in inventoryEquippable) {
            if (item.type == "w01") {sortedList.Add(item);}
        }
        foreach (EquippableItemData item in inventoryEquippable) {
            if (item.type == "w02") {sortedList.Add(item);}
        }
        foreach (EquippableItemData item in inventoryEquippable) {
            if (item.type == "w03") {sortedList.Add(item);}
        }
        foreach (EquippableItemData item in inventoryEquippable) {
            if (item.type == "w04") {sortedList.Add(item);}
        }
        foreach (EquippableItemData item in inventoryEquippable) {
            if (item.type == "w05") {sortedList.Add(item);}
        }
        foreach (EquippableItemData item in inventoryEquippable) {
            if (item.type == "w06") {sortedList.Add(item);}
        }
        foreach (EquippableItemData item in inventoryEquippable) {
            if (item.type == "w07") {sortedList.Add(item);}
            }
        }

        

        if (type == "" || type == "Off Hand") {
        foreach (EquippableItemData item in inventoryEquippable) {
            if (item.type == "w08") {sortedList.Add(item);}
        }
        foreach (EquippableItemData item in inventoryEquippable) {
            if (item.type == "w09") {sortedList.Add(item);}
        }
        }
        

        //inventoryEquippable = sortedList;
        return sortedList;
    }









    

    public int ItemUniqueID
    {
        get
        {
            int uniqueID = itemUniqueID;
            itemUniqueID++;
            return uniqueID;
        }
    }

}
