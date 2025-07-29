using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;




public class CharacterRandomizer : MonoBehaviour
{

    GameData gameData;
    

    int picCount = 0;
    bool isTakingPic = false;
    public GameObject buttonSwitch;
    public GameObject buttonRandomize;
    public GameObject buttonX;

    public GameObject background;
    bool isBackgroundOff;

    int newerSkinColorHolder;
    int oldSkinColorHolder;
    int fakeHairColorHolder;

    CharacterData[] newPlayer;
    int playerAmount;

    [HideInInspector] public GameObject[] RandomCharacterDisplay;
    [HideInInspector] public GameObject[] RandomCharacterDisplay2;

    //drop down vars
    public TMP_Dropdown speciesDropdown;
    int dropdownSelectedRace;

    public TMP_Dropdown itemDropdown;
    string dropdownSelectedItem;
    public TMP_Dropdown itemDropdown2;
    string dropdownSelectedItem2;
    public TMP_Dropdown itemDropdown3;
    string dropdownSelectedItem3;
    public TMP_Dropdown itemDropdown4;
    string dropdownSelectedItem4;


    public TMP_InputField minInput;
    public TMP_InputField maxInput;
    int equipAmtMin;
    int equipAmtMax;



    //single mode vars
    bool isSingleModeOn;
    public GameObject SingleModeTexts;
    public Text equipmentText0;
    public Text equipmentText1;
    public Text equipmentText2;
    public Text equipmentText3;
    public Text equipmentText4;
    public Text equipmentText5;
    public Text equipmentText6;
    public Text equipmentText7;
    public Text equipmentText8;

    public Text textEyes;
    public Text textHair;
    public Text textEars;
    public Text textNose;
    public Text textMouth;
    public Text textBeard;
    public Text textFacialDetail;



    // Start is called before the first frame update
    void Start()
    {
        gameData = GameData.localData;
        playerAmount = 18;
        newPlayer = new CharacterData[playerAmount];
        RandomCharacterDisplay = new GameObject[playerAmount];
        RandomCharacterDisplay2 = new GameObject[playerAmount];

        SpeciesDropDownInitializer();
        ItemDropDownInitializer();
        ItemDropDownInitializer2();
        ItemDropDownInitializer3();
        ItemDropDownInitializer4();

        equipAmtMin = 4;
        equipAmtMax = 5;

    }

    

    public void ButtonSwitchMode() {
        if (isSingleModeOn == false) {
            isSingleModeOn = true;
            //turn on single mode objects
            SingleModeTexts.SetActive(true);
            buttonSwitch.GetComponentInChildren<TMP_Text>().text = "Switch to Multi";
        } else {
            isSingleModeOn = false;
            SingleModeTexts.SetActive(false);
            buttonSwitch.GetComponentInChildren<TMP_Text>().text = "Switch to Single";

        }
    }







    public void ButtonQuitApp() {
        Application.Quit();
    }


    public void RandomizeCharacterButton() {
        int xCount = 0;
        float yCount = 1;
        float xSpacing = 110;
        float ySpacing = 110;


        int playerAmountTemp;
        if (isSingleModeOn == true) {
            playerAmountTemp = 1;
        } else {
            playerAmountTemp = 18;
        }


        for (int i = 0; i < playerAmount; i++) {
            //destroy old character object and recreate
            if (RandomCharacterDisplay[i] != null) {
                Destroy(RandomCharacterDisplay[i].gameObject); 
                RandomCharacterDisplay[i] = null;
                if (RandomCharacterDisplay2[i] != null) {
                    Destroy(RandomCharacterDisplay2[i].gameObject); 
                    RandomCharacterDisplay2[i] = null;
                }
            
            }
        }


        for (int i = 0; i < playerAmountTemp; i++) {
            //create new character data
            

            //// use Species dropDown
            int selectedRace;
            if (dropdownSelectedRace == 9999) {
                selectedRace = Random.Range(0,gameData.speciesArray.species.Count);
            } else {
                selectedRace = dropdownSelectedRace;
            }
            //// end Species dropDown


            

            //create player
            newPlayer[i] = null;
            newPlayer[i] = new CharacterData(gameData.speciesArray.species[selectedRace], 1, 1, 1, 1, 1, 1);



            //// item dropDown
            if (dropdownSelectedItem != "random") {
                equipSpecificGroup(newPlayer[i],dropdownSelectedItem);
            }
            if (dropdownSelectedItem2 != "random") {
                equipSpecificGroup(newPlayer[i],dropdownSelectedItem2);
            }
            if (dropdownSelectedItem3 != "random") {
                equipSpecificGroup(newPlayer[i],dropdownSelectedItem3);
            }
            if (dropdownSelectedItem4 != "random") {
                equipSpecificGroup(newPlayer[i],dropdownSelectedItem4);
            }


            if (equipAmtMin > 10) {
                equipAmtMin = 10;
            }
            if (equipAmtMax > 10) {
                equipAmtMax = 10;
            }

            if (equipAmtMin < 0) {
                equipAmtMin = 0;
            }
            if (equipAmtMax <= equipAmtMin) {
                equipAmtMax = equipAmtMin+1;
            }
            
            gameData.CreateEquipmentStarterPackDefault(newPlayer[i],equipAmtMin,equipAmtMax);



            //destroy old character object and recreate
            if (RandomCharacterDisplay[i] != null) {
                Destroy(RandomCharacterDisplay[i].gameObject); 
                RandomCharacterDisplay[i] = null;
                if (RandomCharacterDisplay2[i] != null) {
                    Destroy(RandomCharacterDisplay2[i].gameObject); 
                    RandomCharacterDisplay2[i] = null;
                }
            
            }

            
            

            RandomCharacterDisplay[i] = new GameObject();
            RandomCharacterDisplay[i].layer = 4;
            //RandomCharacterDisplay[i].transform.SetParent(this.transform);
            if (isSingleModeOn == true) {
                RandomCharacterDisplay[i].transform.localPosition = new Vector3(100+2*xSpacing,260-2*ySpacing,0);

            } else {
                RandomCharacterDisplay[i].transform.localPosition = new Vector3(100+xCount*xSpacing,260-yCount*ySpacing,0);
            }
            
            RandomCharacterDisplay2[i] = new GameObject();
            RandomCharacterDisplay2[i].layer = 4;
            RandomCharacterDisplay2[i].transform.SetParent(RandomCharacterDisplay[i].transform);
            RandomCharacterDisplay2[i].transform.localPosition = new Vector3(0,0,0);
            CharacterObject characterObject = RandomCharacterDisplay2[i].AddComponent<CharacterObject>();
            characterObject.CreateCharacterObject(RandomCharacterDisplay2[i],newPlayer[i],false);

            StartCoroutine(delayScale(RandomCharacterDisplay2[i],characterObject.data));

            foreach (Transform child in RandomCharacterDisplay2[i].GetComponentsInChildren<Transform>()) {
                child.gameObject.layer = 4;
            }

            xCount++;
            if (xCount > 5) {
                xCount = 0;
                yCount++;
            }
        }

        if (isSingleModeOn == true) {
            fillSingleModeTexts();
        }


    }

    IEnumerator delayScale(GameObject character, CharacterData characterData) {
        yield return new WaitForSeconds(.2f);
        character.transform.localScale = new Vector3(50*characterData.speciesData.xScale,50*characterData.speciesData.yScale,0);
    }




    public void EquipAmtChangedMin(string value) {
        Debug.Log("min "+minInput.text);
        int num = 0;
        int.TryParse(maxInput.text,out num);
        equipAmtMin = num;
    }

    public void EquipAmtChangedMax(string value) {
        
        Debug.Log("max "+maxInput.text);
        int num = 0;
        int.TryParse(maxInput.text,out num);
        equipAmtMax = num;
    }
    






    void equipSpecificGroup(CharacterData characterData, string itemType) {
        string[] itemTypes = new string[] {itemType};
        EquippableItemData newItem = new EquippableItemData(itemTypes);
        if (characterData.TryEquip(newItem) == false) {
            newItem = new EquippableItemData(itemTypes);
        }
    }



    
    void Update() {

        if (Input.GetKey("r")) {
            if (isTakingPic == false) {
                StartCoroutine("CaptureScreen");
            }
            
        }
    }

    IEnumerator CaptureScreen() {
        isTakingPic = true;
        buttonRandomize.SetActive(false);
        buttonSwitch.SetActive(false);
        buttonX.SetActive(false);
        ScreenCapture.CaptureScreenshot("PicFolder/CharacterPic"+picCount+".png");
        picCount++;
        yield return new WaitForSeconds(1);
        isTakingPic = false;
        buttonRandomize.SetActive(true);
        buttonSwitch.SetActive(true);
        buttonX.SetActive(true);
    }



    public void switchBackground() {

        if (isBackgroundOff == false) {
            isBackgroundOff = true;
            background.SetActive(false);
        } else {
            isBackgroundOff = false;
            background.SetActive(true);
        }
    }





    void fillSingleModeTexts() {
        List<EquippableItemData> EquippedItems = newPlayer[0].GetListOfEquippedItems();

        for (int i = 0; i < EquippedItems.Count; i++) {
            string equipText = EquippedItems[i].spriteAddresses[0];

            string[] equipTextArray = equipText.Split(char.Parse("/"));
            
            if (i == 0) {
                equipmentText0.text = equipTextArray[3];
            } else if (i == 1) {
                equipmentText1.text = equipTextArray[3];
            } else if (i == 2) {
                equipmentText2.text = equipTextArray[3];
            } else if (i == 3) {
                equipmentText3.text = equipTextArray[3];
            } else if (i == 4) {
                equipmentText4.text = equipTextArray[3];
            } else if (i == 5) {
                equipmentText5.text = equipTextArray[3];
            } else if (i == 6) {
                equipmentText6.text = equipTextArray[3];
            } else if (i == 7) {
                equipmentText7.text = equipTextArray[3];
            } else if (i == 8) {
                equipmentText8.text = equipTextArray[3];
            }

            
        }

        if (EquippedItems.Count < 8) {
            equipmentText8.text = "";
        }
        if (EquippedItems.Count < 7) {
            equipmentText7.text = "";
        }
        if (EquippedItems.Count < 6) {
            equipmentText6.text = "";
        }
        if (EquippedItems.Count < 5) {
            equipmentText5.text = "";
        }
        if (EquippedItems.Count < 4) {
            equipmentText4.text = "";
        }
        if (EquippedItems.Count < 3) {
            equipmentText3.text = "";
        }
        if (EquippedItems.Count < 2) {
            equipmentText2.text = "";
        }
        if (EquippedItems.Count < 1) {
            equipmentText1.text = "";
        }
        
        textEyes.text = newPlayer[0].bodyPartSpriteAddresses[0];
        textHair.text = newPlayer[0].bodyPartSpriteAddresses[4];
        textEars.text = newPlayer[0].bodyPartSpriteAddresses[1];
        textNose.text = newPlayer[0].bodyPartSpriteAddresses[3];
        textMouth.text = newPlayer[0].bodyPartSpriteAddresses[2];
        if (newPlayer[0].bodyPartSpriteAddresses.Count > 10 && newPlayer[0].bodyPartSpriteAddresses[10].Length > 33) {
            textBeard.text = newPlayer[0].bodyPartSpriteAddresses[10];
        } else {
            textBeard.text = "";
        }
        if (newPlayer[0].bodyPartSpriteAddresses.Count > 11 && newPlayer[0].bodyPartSpriteAddresses[11].Length > 33) {
           textFacialDetail.text = newPlayer[0].bodyPartSpriteAddresses[11];
        } else {
            textFacialDetail.text = "";
  
        }
    }





    
//////////////////////// DROP DOWN MENUS
    void SpeciesDropDownInitializer() {

        dropdownSelectedRace = 9999; //starts on random

        //species Dropdown handler
        speciesDropdown.options.Clear();
        
        List<int> speciesOptions = new List<int>();
        speciesOptions.Add(9999); //placeholder showing random
        for (int i = 0; i < gameData.speciesArray.species.Count; i++) {
            speciesOptions.Add(i);
        }
        foreach(int item in speciesOptions) {
            string chosenSpecies = "";
            if (item == 9999) {
                chosenSpecies = "random";
            } else {
                chosenSpecies = gameData.speciesArray.species[item].name;
            }
            speciesDropdown.options.Add(new TMP_Dropdown.OptionData() {text = item +"- "+chosenSpecies});
        }

        speciesDropdown.onValueChanged.AddListener(delegate {SpeciesDropdownItemSelected(speciesDropdown);});

    }


    void SpeciesDropdownItemSelected(TMP_Dropdown dropdown) {
        //assign here
        string[] splitString = dropdown.options[dropdown.value].text.Split('-');
        int speciesNum = 0;
        int.TryParse(splitString[0],out speciesNum);
        dropdownSelectedRace = speciesNum;
    }






    void ItemDropDownInitializer() {

        dropdownSelectedItem = "random"; //starts on random

        //species Dropdown handler
        itemDropdown.options.Clear();
        
        List<int> itemOptions = new List<int>();
        itemOptions.Add(9999); //placeholder showing random
        for (int i = 0; i < gameData.equipCodes.itemType.Count; i++) {
            itemOptions.Add(i);
        }
        foreach(int item in itemOptions) {
            string chosenItem = "";
            if (item == 9999) {
                chosenItem = "random";
            } else {
                chosenItem = gameData.equipCodes.itemType[item];
            }
            itemDropdown.options.Add(new TMP_Dropdown.OptionData() {text = chosenItem});
        }

        itemDropdown.onValueChanged.AddListener(delegate {ItemDropdownItemSelected(itemDropdown);});

    }


    void ItemDropdownItemSelected(TMP_Dropdown dropdown) {
        dropdownSelectedItem = dropdown.options[dropdown.value].text;
    }

    void ItemDropDownInitializer2() {

        dropdownSelectedItem2 = "random"; //starts on random

        //species Dropdown handler
        itemDropdown2.options.Clear();
        
        List<int> itemOptions = new List<int>();
        itemOptions.Add(9999); //placeholder showing random
        for (int i = 0; i < gameData.equipCodes.itemType.Count; i++) {
            itemOptions.Add(i);
        }
        foreach(int item in itemOptions) {
            string chosenItem = "";
            if (item == 9999) {
                chosenItem = "random";
            } else {
                chosenItem = gameData.equipCodes.itemType[item];
            }
            itemDropdown2.options.Add(new TMP_Dropdown.OptionData() {text = chosenItem});
        }

        itemDropdown2.onValueChanged.AddListener(delegate {ItemDropdownItemSelected2(itemDropdown2);});

    }


    void ItemDropdownItemSelected2(TMP_Dropdown dropdown) {
        dropdownSelectedItem2 = dropdown.options[dropdown.value].text;
    }


    void ItemDropDownInitializer3() {

        dropdownSelectedItem3 = "random"; //starts on random

        //species Dropdown handler
        itemDropdown3.options.Clear();
        
        List<int> itemOptions = new List<int>();
        itemOptions.Add(9999); //placeholder showing random
        for (int i = 0; i < gameData.equipCodes.itemType.Count; i++) {
            itemOptions.Add(i);
        }
        foreach(int item in itemOptions) {
            string chosenItem = "";
            if (item == 9999) {
                chosenItem = "random";
            } else {
                chosenItem = gameData.equipCodes.itemType[item];
            }
            itemDropdown3.options.Add(new TMP_Dropdown.OptionData() {text = chosenItem});
        }

        itemDropdown3.onValueChanged.AddListener(delegate {ItemDropdownItemSelected3(itemDropdown3);});

    }


    void ItemDropdownItemSelected3(TMP_Dropdown dropdown) {
        dropdownSelectedItem3 = dropdown.options[dropdown.value].text;
    }




    void ItemDropDownInitializer4() {

        dropdownSelectedItem4 = "random"; //starts on random

        //species Dropdown handler
        itemDropdown4.options.Clear();
        
        List<int> itemOptions = new List<int>();
        itemOptions.Add(9999); //placeholder showing random
        for (int i = 0; i < gameData.equipCodes.itemType.Count; i++) {
            itemOptions.Add(i);
        }
        foreach(int item in itemOptions) {
            string chosenItem = "";
            if (item == 9999) {
                chosenItem = "random";
            } else {
                chosenItem = gameData.equipCodes.itemType[item];
            }
            itemDropdown4.options.Add(new TMP_Dropdown.OptionData() {text = chosenItem});
        }

        itemDropdown4.onValueChanged.AddListener(delegate {ItemDropdownItemSelected4(itemDropdown4);});

    }


    void ItemDropdownItemSelected4(TMP_Dropdown dropdown) {
        dropdownSelectedItem4 = dropdown.options[dropdown.value].text;
    }
//////////////////////// DROP DOWN MENUS

    
}
