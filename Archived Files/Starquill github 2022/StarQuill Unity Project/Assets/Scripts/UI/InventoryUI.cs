using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class InventoryUI : MonoBehaviour
{
    GameData gameData;

    public GameObject TrailerButtons;
    public GameObject TrailerText;
    public Animator d12Animator;
    public Animator d8Animator;
    public Animator d20Animator;
    public TMPro.TextMeshProUGUI diceText;
    public GameObject TextResult;

    public GameObject UIinventoryButton;
    public GameObject UIJournalButton;

    public GameObject UIinventoryScreen;
    public GameObject ScreenEquipment;
    public GameObject ScreenParty;
    public GameObject ScreenJournal;


    public GameObject subScreen_Equipment_Equipped;
    public GameObject subScreen_Equipment_Inventory;
    bool isSubScreen_Equipment_Equipped_Active;
    bool isSubScreen_Equipment_Inventory_Active;
    bool isMainScreen_Equipment_Active;

    //public OverworldController overworldController;

    public GameObject PartyMember1Display;
    public GameObject PartyMember2Display;
    public GameObject PartyMember3Display;
    public GameObject PartyMember4Display;
    GameObject PartyMember1Image;
    GameObject PartyMember2Image;
    GameObject PartyMember3Image;
    GameObject PartyMember4Image;

    //Equipment Screen Variables
    public GameObject Equipment_Inventory_ListContent;
    public GameObject Equipment_Inventory_ListButton;
    public GameObject buttonHead;
    public GameObject buttonTorso;
    public GameObject buttonArms;
    public GameObject buttonFeet;
    public GameObject buttonLegs;
    public GameObject buttonMisc1;
    public GameObject buttonMisc2;
    public GameObject buttonMisc3;
    public GameObject buttonMisc4;
    public GameObject buttonMainHand;
    public GameObject buttonOffHand;


    public GameObject buttonSubMisc1;
    public GameObject buttonSubMisc2;
    public GameObject buttonSubMisc3;
    public GameObject buttonSubMisc4;

    EquippableItemData itemToEquipOnLeft;
    EquippableItemData itemToEquipOnRight;
    string selectedSlotType;

    public GameObject EquipmentButtonUnequip;
    public GameObject EquipmentButtonEquip;
    public GameObject EquipmentButtonSwap;
    public GameObject EquipmentButtonEquipToMisc;
    public GameObject EquipmentButtonBack;


    ///Journal Vars
    public GameObject JournalEntryPrefab;
    public GameObject JournalEntryScrollView;
    public GameObject JournalSelectedScrollView;
    QuestData selectedQuest;



    int currentPartyMemberNumber;
    float delayClickParty;
    bool isDelayClickPartyActive;


    Vector3 activeQuestVector;


    public Button buttonParty;
    public Button buttonEquipment;
    public Button buttonStats;
    public Button buttonItems;
    public Button buttonJournal;
    public Button buttonBack;

    string buttonColorEquipment = "B7C3D1";
    string buttonColorParty = "FFFFFF";
    string buttonColorJournal = "C5D9CB";




////////////////////////// 3D ADDITIONS

    public bool is3D;


////////////////////////// 3D ADDITIONS


    void Awake() {
        LeanTween.reset();
    }

    //----- initialize
    void Start() {
        gameData = GameData.localData;
        if (is3D == false) {
            UIinventoryButton.gameObject.SetActive(true);
            UIJournalButton.gameObject.SetActive(true);
        }
        UIinventoryScreen.gameObject.SetActive(false);


        //delayClickParty = .5f;
        
    }


    


    //----- destroy character display objects and recreate
    public void RefreshDisplayCharacter(bool partyLeaderOnly, bool isEquipmentMenu) {

        //party member 1
        if (PartyMember1Image != null) {
            Destroy(PartyMember1Image.gameObject); 
            PartyMember1Image = null;
        }
        PartyMember1Image = new GameObject();
        PartyMember1Image.name = "PartyMember1Image";
        PartyMember1Image.transform.SetParent(PartyMember1Display.transform,false);
        CharacterObject characterObject = PartyMember1Image.AddComponent<CharacterObject>();

        if (isEquipmentMenu == true) {
            characterObject.CreateCharacterObject(PartyMember1Image,gameData.partyData[currentPartyMemberNumber],true);
        } else {
            characterObject.CreateCharacterObject(PartyMember1Image,gameData.partyData[0],true);
        }

        //do not destroy/recreate other members if true
        if (partyLeaderOnly ==  true) {
            return;
        }

        //party member 2
        if (PartyMember2Image != null) {
            Destroy(PartyMember2Image.gameObject); 
            PartyMember2Image = null;
        }
        PartyMember2Image = new GameObject();
        PartyMember2Image.name = "PartyMember2Image";
        PartyMember2Image.transform.SetParent(PartyMember2Display.transform,false);
        CharacterObject characterObject2 = PartyMember2Image.AddComponent<CharacterObject>();
        characterObject2.CreateCharacterObject(PartyMember2Image,gameData.partyData[1],true);

        //party member 2
        if (PartyMember3Image != null) {
            Destroy(PartyMember3Image.gameObject); 
            PartyMember3Image = null;
        }
        PartyMember3Image = new GameObject();
        PartyMember3Image.name = "PartyMember3Image";
        PartyMember3Image.transform.SetParent(PartyMember3Display.transform,false);
        CharacterObject characterObject3 = PartyMember3Image.AddComponent<CharacterObject>();
        characterObject3.CreateCharacterObject(PartyMember3Image,gameData.partyData[2],true);

        //party member 3
        if (PartyMember4Image != null) {
            Destroy(PartyMember4Image.gameObject); 
            PartyMember4Image = null;
        }
        PartyMember4Image = new GameObject();
        PartyMember4Image.name = "PartyMember4Image";
        PartyMember4Image.transform.SetParent(PartyMember4Display.transform,false);
        CharacterObject characterObject4 = PartyMember4Image.AddComponent<CharacterObject>();
        characterObject4.CreateCharacterObject(PartyMember4Image,gameData.partyData[3],true);
        
    }


    //Always First Opening of Inventory
    //----- UIinventoryButton is the only button using this
     public void ButtonInventoryOpen() {
        
        gameData.isGamePaused = true;

        UIinventoryButton.gameObject.SetActive(false);
        UIJournalButton.gameObject.SetActive(false);
        UIinventoryScreen.gameObject.SetActive(true);
        ButtonParty();
        isSubScreen_Equipment_Inventory_Active = false;
        isSubScreen_Equipment_Equipped_Active = false;
        itemToEquipOnLeft = null;
        itemToEquipOnRight = null;


        if (is3D == true) {
            Cursor.lockState = CursorLockMode.None;
		    Cursor.visible = true;
            Time.timeScale = 0;
        }




    }

    public void ButtonJournalShortcut() {
        ButtonInventoryOpen();
        ButtonJournal();
    }



    //----- used to close submenus and main menu based on booleans
    public void ButtonInventoryClose() {

        if (isSubScreen_Equipment_Inventory_Active == true && isSubScreen_Equipment_Equipped_Active == true) {
            CloseSubScreenEquipmentInventory();
            CloseSubScreenEquipmentEquipped();

        } else if (isSubScreen_Equipment_Inventory_Active == true) {
            
            CloseSubScreenEquipmentInventory();

        } else if (isSubScreen_Equipment_Equipped_Active == true) {
            CloseSubScreenEquipmentEquipped();
            

        } else if (isMainScreen_Equipment_Active == true) {
            isMainScreen_Equipment_Active = false;
            ButtonParty();
            
           
        } else {
            //close menu
            gameData.isGamePaused = false;

            /*if (is3D == false) {
                overworldController.gameObject.transform.eulerAngles = new Vector3(0, 0, 0);
                overworldController.RefreshDisplayCharacterParty();
            } else if (is3D == true) {
                GameObject player = Camera.main.GetComponent<CameraController>().target.gameObject;
		        GameObject weaponObject = player.transform.GetChild(0).GetChild(0).gameObject;
                weaponObject.GetComponent<WeaponController>().UpdateWeapons();

            }*/
            
            if (is3D == false) {
                UIinventoryButton.gameObject.SetActive(true);
                UIJournalButton.gameObject.SetActive(true);
            }
            

            UIinventoryScreen.gameObject.SetActive(false);
            ScreenParty.gameObject.SetActive(false);
            ScreenEquipment.gameObject.SetActive(false);

            if (is3D == true) {
                Cursor.lockState = CursorLockMode.Locked;
		        Cursor.visible = false;
                Time.timeScale = 1;
            }
        }

    }

    void CloseSubScreenEquipmentInventory() {
        subScreen_Equipment_Inventory.SetActive(false);
        isSubScreen_Equipment_Inventory_Active = false;
        itemToEquipOnRight = null;
        if (isSubScreen_Equipment_Equipped_Active == false) {
            itemToEquipOnLeft = null;
        }

    }

    void CloseSubScreenEquipmentEquipped() {
        subScreen_Equipment_Equipped.SetActive(false);
        isSubScreen_Equipment_Equipped_Active = false;
        PartyMember1Display.SetActive(true);
        FillEquippmentInventoryScrollview("");
        itemToEquipOnLeft = null;
        if (isSubScreen_Equipment_Inventory_Active == false) {
            itemToEquipOnRight = null;
        }
        

    }

    //----- MAIN PARTY BUTTON
    public void ButtonParty() {
        ChangeButtonColorAndFlourish("party");
        ScreenEquipment.gameObject.SetActive(false);
        ScreenParty.gameObject.SetActive(true);
        ScreenJournal.SetActive(false);
        isMainScreen_Equipment_Active = false;
        RefreshDisplayCharacter(false,false);


    }

    //----- MAIN EQUIPMENT BUTTON
    public void ButtonEquipment() {
        ChangeButtonColorAndFlourish("equipment");
        ScreenEquipment.SetActive(true);
        ScreenParty.SetActive(false);
        ScreenJournal.SetActive(false);
        isMainScreen_Equipment_Active = true;
        CloseSubScreenEquipmentEquipped();
        CloseSubScreenEquipmentInventory();

        currentPartyMemberNumber = 0;

        FillEquippmentInventoryScrollview("");
        FillAllEquippedButtons();

    }

    //----- MAIN STATS BUTTON
    public void ButtonStats() {

    }

    //----- MAIN CONSUMABLE ITEMS BUTTON
    public void ButtonItems() {

    }

    //----- MAIN JOURNAL BUTTON
    public void ButtonJournal() {
        ChangeButtonColorAndFlourish("journal");
        ScreenEquipment.SetActive(false);
        ScreenParty.SetActive(false);
        ScreenJournal.SetActive(true);

        FillJournalEntryScrollview();
        if (gameData.activeQuest != null) {
            OpenJournalEntry(gameData.activeQuest);
        }
        
    }


    //----- SWITCH PARTY LEADER BUTTONS -----//
    public void ButtonSwitchParty2() {
        if (isDelayClickPartyActive == false) {
            //StartCoroutine("DelayClickParty");
            if (gameData.partyData[1] != null) {
            CharacterData tempPlaceHolder = gameData.partyData[1];
            gameData.partyData[1] = gameData.partyData[0];
            gameData.partyData[0] = tempPlaceHolder;
            }
            RefreshDisplayCharacter(false,false);
        }
    }

    public void ButtonSwitchParty3() {
        if (isDelayClickPartyActive == false) {
            //StartCoroutine("DelayClickParty");
            if (gameData.partyData[2] != null) {
            CharacterData tempPlaceHolder = gameData.partyData[2];
            gameData.partyData[2] = gameData.partyData[0];
            gameData.partyData[0] = tempPlaceHolder;
            }
            RefreshDisplayCharacter(false,false);
        }
    }

    public void ButtonSwitchParty4() {
        if (isDelayClickPartyActive == false) {
            //StartCoroutine("DelayClickParty");
            if (gameData.partyData[3] != null) {
            CharacterData tempPlaceHolder = gameData.partyData[3];
            gameData.partyData[3] = gameData.partyData[0];
            gameData.partyData[0] = tempPlaceHolder;
            }
            RefreshDisplayCharacter(false,false);
        }
    }
    //----- END SWITCH PARTY LEADER BUTTONS -----//



    public void ButtonEquipmentSwitchPartyLeft() {
        
        currentPartyMemberNumber -= 1;
        if (currentPartyMemberNumber < 0) {
            currentPartyMemberNumber = gameData.partyData.Count - 1;
        }
        
        RefreshDisplayCharacter(true,true);
        FillAllEquippedButtons();
        
    }

    public void ButtonEquipmentSwitchPartyRight() {
        currentPartyMemberNumber += 1;
        if (currentPartyMemberNumber >= gameData.partyData.Count) {
            currentPartyMemberNumber = 0;
        }
        
        RefreshDisplayCharacter(true,true);
        FillAllEquippedButtons();
        
    }

    public void ChangeButtonColorAndFlourish(string buttonType) {
        string colorString = "";
        if (buttonType == "party") {
            buttonParty.transform.GetChild(1).gameObject.SetActive(true);
            buttonEquipment.transform.GetChild(1).gameObject.SetActive(false);
            buttonStats.transform.GetChild(1).gameObject.SetActive(false);
            buttonItems.transform.GetChild(1).gameObject.SetActive(false);
            buttonJournal.transform.GetChild(1).gameObject.SetActive(false);
            colorString = buttonColorParty;

        } else if (buttonType == "equipment") {
            buttonParty.transform.GetChild(1).gameObject.SetActive(false);
            buttonEquipment.transform.GetChild(1).gameObject.SetActive(true);
            buttonStats.transform.GetChild(1).gameObject.SetActive(false);
            buttonItems.transform.GetChild(1).gameObject.SetActive(false);
            buttonJournal.transform.GetChild(1).gameObject.SetActive(false);
            colorString = buttonColorEquipment;

        } else if (buttonType == "stats") {
            buttonParty.transform.GetChild(1).gameObject.SetActive(false);
            buttonEquipment.transform.GetChild(1).gameObject.SetActive(false);
            buttonStats.transform.GetChild(1).gameObject.SetActive(true);
            buttonItems.transform.GetChild(1).gameObject.SetActive(false);
            buttonJournal.transform.GetChild(1).gameObject.SetActive(false);
        } else if (buttonType == "items") {
            buttonParty.transform.GetChild(1).gameObject.SetActive(false);
            buttonEquipment.transform.GetChild(1).gameObject.SetActive(false);
            buttonStats.transform.GetChild(1).gameObject.SetActive(false);
            buttonItems.transform.GetChild(1).gameObject.SetActive(true);
            buttonJournal.transform.GetChild(1).gameObject.SetActive(false);
        } else if (buttonType == "journal") {
            buttonParty.transform.GetChild(1).gameObject.SetActive(false);
            buttonEquipment.transform.GetChild(1).gameObject.SetActive(false);
            buttonStats.transform.GetChild(1).gameObject.SetActive(false);
            buttonItems.transform.GetChild(1).gameObject.SetActive(false);
            buttonJournal.transform.GetChild(1).gameObject.SetActive(true);
            colorString = buttonColorJournal;

        }   

        buttonParty.image.color = HexColorConvert.Parse(colorString);
        buttonBack.image.color = HexColorConvert.Parse(colorString);
        buttonEquipment.image.color = HexColorConvert.Parse(colorString);
        buttonStats.image.color = HexColorConvert.Parse(colorString);
        buttonItems.image.color = HexColorConvert.Parse(colorString);
        buttonJournal.image.color = HexColorConvert.Parse(colorString);

        

    }


    //----- fills the equipped buttons on left side of the screen
    public void FillAllEquippedButtons() {
        
        FillEquippedButton(buttonHead,gameData.partyData[currentPartyMemberNumber].EquipSlotHead);
        FillEquippedButton(buttonTorso,gameData.partyData[currentPartyMemberNumber].EquipSlotTorso);
        FillEquippedButton(buttonArms,gameData.partyData[currentPartyMemberNumber].EquipSlotArms);
        FillEquippedButton(buttonLegs,gameData.partyData[currentPartyMemberNumber].EquipSlotLegs);
        FillEquippedButton(buttonFeet,gameData.partyData[currentPartyMemberNumber].EquipSlotFeet);
        FillEquippedButton(buttonMisc1,gameData.partyData[currentPartyMemberNumber].EquipSlotMisc1);
        FillEquippedButton(buttonMisc2,gameData.partyData[currentPartyMemberNumber].EquipSlotMisc2);
        FillEquippedButton(buttonMisc3,gameData.partyData[currentPartyMemberNumber].EquipSlotMisc3);
        FillEquippedButton(buttonMisc4,gameData.partyData[currentPartyMemberNumber].EquipSlotMisc4);
        FillEquippedButton(buttonMainHand,gameData.partyData[currentPartyMemberNumber].EquipSlotMainHand);
        FillEquippedButton(buttonOffHand,gameData.partyData[currentPartyMemberNumber].EquipSlotOffHand);
    }

    //----- fills a single equipped button
    void FillEquippedButton(GameObject equippedButton, EquippableItemData item) {
        GameObject buttonText = equippedButton.transform.GetChild(0).gameObject;
        GameObject buttonItemImage = equippedButton.transform.GetChild(1).gameObject;
        
        //if button contains item images, destroy them
        if (buttonItemImage.transform.childCount > 0) {
            foreach (Transform child in buttonItemImage.transform) {
                GameObject.Destroy(child.gameObject);
            }
        }
        
        //if item is not empty, remove text and add item image
        
        if (item != null) {
            
            //hide text
            buttonText.SetActive(false); 
            
            //remove additional EquippableItemObject components
            if (buttonItemImage.GetComponent<EquippableItemObject>() != null) {
                Destroy(buttonItemImage.GetComponent<EquippableItemObject>());
                foreach (Transform child in buttonItemImage.transform.transform) {
                    GameObject.Destroy(child.gameObject); 
                }
            }
            //add new EquippableItemObject component
            EquippableItemObject equippableObject = buttonItemImage.AddComponent<EquippableItemObject>();
            equippableObject.CreateEquippableItemObject(buttonItemImage,null,item,true,false);
            equippableObject.transform.localPosition = new Vector3(57,48,0);
            equippableObject.transform.localScale = new Vector3(1,1,0);
            ResizeItemImage(equippableObject.transform,item.type,false);
            

        } else {
            //show text
            buttonText.SetActive(true);

        }

        //add button functionality regardless if empty or not
        equippedButton.GetComponent<Button>().onClick.RemoveAllListeners();
        equippedButton.GetComponent<Button>().onClick.AddListener(delegate{OnClickEquippedButton(item,buttonText.GetComponentInChildren<TMPro.TextMeshProUGUI>().text);});
    }


    //----- fills inventory scrollview
    public void FillEquippmentInventoryScrollview(string sortType) {
        //List<EquippableItemData> gameData.inventoryEquippable = gameData.PartyInventoryEquippable();
        //gameData.SortInventoryEquippable(sortType);
        List<EquippableItemData> sortedList = gameData.SortInventoryEquippable(sortType);

        //destroy any pre-existing buttons
       foreach (Transform child in Equipment_Inventory_ListContent.transform) {
                GameObject.Destroy(child.gameObject); 
        }

        //keeps track of what button in the row we are on
        int rowCount = 0;
        int columnCount = 0;
        float columnSpacing = -150;
        float rowSpacing = 150;

        //iterate gameData.inventoryEquippable and create buttons for each
        for (int i = 0; i < sortedList.Count; i++) {
            EquippableItemData itemData = sortedList[i];

            //create object and store var for button component
            GameObject newObject = Instantiate(Equipment_Inventory_ListButton,Equipment_Inventory_ListContent.transform, false);
            Button newButton = newObject.transform.GetComponent<Button>();
            newObject.transform.position = new Vector3(newObject.transform.position.x+70,newObject.transform.position.y-70,0);
            
            //move position to a row of 4
            if (rowCount == 0) {
                newObject.transform.position = new Vector3(newObject.transform.position.x+rowSpacing*rowCount,newObject.transform.position.y+columnSpacing*columnCount,0);
                rowCount = 1;
            } else if (rowCount == 1) {
                newObject.transform.position = new Vector3(newObject.transform.position.x+rowSpacing*rowCount,newObject.transform.position.y+columnSpacing*columnCount,0);
                rowCount = 2;
            } else if (rowCount == 2) {
                newObject.transform.position = new Vector3(newObject.transform.position.x+rowSpacing*rowCount,newObject.transform.position.y+columnSpacing*columnCount,0);
                rowCount = 0;
                columnCount++;
                RectTransform rt = Equipment_Inventory_ListContent.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(458.2293f, 150*columnCount+150);
            }

            
            
            //set display text for button
            newButton.GetComponentInChildren<TMPro.TextMeshProUGUI>().text = itemData.name;

            //check for item display image and destroy if pre-existing
            if (newObject.transform.GetChild(1).gameObject.GetComponent<EquippableItemObject>() != null) {
                Destroy(newObject.transform.GetChild(1).gameObject.GetComponent<EquippableItemObject>());
                foreach (Transform child in newObject.transform.GetChild(1).gameObject.transform.transform) {
                    GameObject.Destroy(child.gameObject); 
                }
            }

            //create new item display image
            EquippableItemObject equippableObject = newObject.transform.GetChild(1).gameObject.AddComponent<EquippableItemObject>();
            equippableObject.CreateEquippableItemObject(newObject.transform.GetChild(1).gameObject,null,itemData,true,false);
            ResizeItemImage(equippableObject.transform,itemData.type,false);

            //add button functionality
            newButton.GetComponent<Button>().onClick.RemoveAllListeners();
            newButton.GetComponent<Button>().onClick.AddListener(delegate{OpenEquipInventoryDetails(itemData);});
            
        }
        
    }



    //----- equipped button is clicked
    public void OnClickEquippedButton(EquippableItemData data,string slotType) {
        //cancel button click if wrong slot matched with inventory
        

        if (isSubScreen_Equipment_Inventory_Active == true) {
            if ((slotType == "Head" && itemToEquipOnRight.type[0] != 'h') || (slotType == "Torso" && itemToEquipOnRight.type[0] != 't') || (slotType == "Hands" && itemToEquipOnRight.type[0] != 'a')
            || (slotType == "Legs" && itemToEquipOnRight.type[0] != 'l') || (slotType == "Feet" && itemToEquipOnRight.type[0] != 'f') 
            || (slotType == "Main Hand" && ((itemToEquipOnRight.type == "w08" || itemToEquipOnRight.type == "w09") || itemToEquipOnRight.type[0] != 'w')) || (slotType == "Off Hand" && itemToEquipOnRight.type[0] != 'w')
            ) {
                return;
            }

            if ((slotType == "Off Hand" && itemToEquipOnRight.type[0] == 'w' && itemToEquipOnRight.type != "w08" && itemToEquipOnRight.type != "w09")) {
                slotType = "Main Hand";
            }
        }

        

        //activate subScreen Equipment Equipped and store item data
        subScreen_Equipment_Equipped.SetActive(true);
        isSubScreen_Equipment_Equipped_Active = true;
        PartyMember1Display.SetActive(false);
        itemToEquipOnLeft = data;


        //deactivate the Sub Misc Buttons
        if (slotType == "Misc") {
            DisplaySubMiscSlots(true);
        } else {
            DisplaySubMiscSlots(false);
        }
        

        GameObject selectedItemImage = subScreen_Equipment_Equipped.transform.GetChild(2).GetChild(0).gameObject;
        GameObject selectedItemSlotText = subScreen_Equipment_Equipped.transform.GetChild(2).GetChild(1).gameObject;
        GameObject selectedItemName = subScreen_Equipment_Equipped.transform.GetChild(2).GetChild(2).gameObject;


        //Destroy any remaining item images
        if (selectedItemImage.GetComponent<EquippableItemObject>() != null) {
            Destroy(selectedItemImage.GetComponent<EquippableItemObject>());
            foreach (Transform child in selectedItemImage.transform.transform) {
                GameObject.Destroy(child.gameObject); 
            }
        }

        //if item is not null, fill image and unequip button
        if (data != null) {
            EquipmentButtonUnequip.SetActive(true);
            selectedItemSlotText.SetActive(false);
            selectedItemName.GetComponent<TMPro.TextMeshProUGUI>().text = data.name; //text name

            EquippableItemObject equippableObject = selectedItemImage.AddComponent<EquippableItemObject>();
            equippableObject.CreateEquippableItemObject(selectedItemImage,null,data,true,false);
            equippableObject.transform.localPosition = new Vector3(0,0,0);
            equippableObject.transform.localScale = new Vector3(2,2,0);
            ResizeItemImage(equippableObject.transform,data.type,true);

            EquipmentButtonUnequip.GetComponent<Button>().onClick.RemoveAllListeners();
            EquipmentButtonUnequip.GetComponent<Button>().onClick.AddListener(delegate{UnEquipAndCloseSubTabs(itemToEquipOnLeft);});

        //if item is null, fill slot text and dont include unequip button
        } else {
            EquipmentButtonUnequip.SetActive(false);
            EquipmentButtonUnequip.GetComponent<Button>().onClick.RemoveAllListeners();
            selectedItemName.GetComponent<TMPro.TextMeshProUGUI>().text = "Empty Slot"; //text name
            selectedItemSlotText.SetActive(true);
            if (slotType == "Head") {
                selectedItemSlotText.GetComponent<TMPro.TextMeshProUGUI>().text = "Head Slot";
            } else if (slotType == "Torso") {
                selectedItemSlotText.GetComponent<TMPro.TextMeshProUGUI>().text = "Torso Slot";
            } else if (slotType == "Hands") {
                selectedItemSlotText.GetComponent<TMPro.TextMeshProUGUI>().text = "Hands Slot";
            } else if (slotType == "Legs") {
                selectedItemSlotText.GetComponent<TMPro.TextMeshProUGUI>().text = "Legs Slot";
            } else if (slotType == "Feet") {
                selectedItemSlotText.GetComponent<TMPro.TextMeshProUGUI>().text = "Feet Slot";
            } else if (slotType == "Main Hand") {
                selectedItemSlotText.GetComponent<TMPro.TextMeshProUGUI>().text = "Main Hand Slot";
            } else if (slotType == "Off Hand") {
                selectedItemSlotText.GetComponent<TMPro.TextMeshProUGUI>().text = "Off Hand Slot";
            } else if (slotType == "Misc") {
                selectedItemSlotText.GetComponent<TMPro.TextMeshProUGUI>().text = "Misc Slot";
            } else {
                Debug.Log("slotType String Failure");
            }
            
        }
        

        

        if (isSubScreen_Equipment_Inventory_Active == false) {
            //sort inventory based on type
            FillEquippmentInventoryScrollview(slotType);

        } else if (isSubScreen_Equipment_Inventory_Active == true) {
            BothSubEquipmentMenusActive();
            
        }
    }

    

    public void OpenEquipInventoryDetails(EquippableItemData data) {
        //Debug.Log("ItemData: "+data.name);


        subScreen_Equipment_Inventory.SetActive(true);
        isSubScreen_Equipment_Inventory_Active = true;
        itemToEquipOnRight = data;
        EquipmentButtonSwap.SetActive(false);


        if (data != null) {
            GameObject objectInQuestion = subScreen_Equipment_Inventory.transform.GetChild(2).GetChild(0).gameObject;
            subScreen_Equipment_Inventory.transform.GetChild(2).GetChild(1).gameObject.GetComponent<TMPro.TextMeshProUGUI>().text = data.name; //text name

            if (objectInQuestion.GetComponent<EquippableItemObject>() != null) {
                Destroy(objectInQuestion.GetComponent<EquippableItemObject>());
                foreach (Transform child in objectInQuestion.transform.transform) {
                    GameObject.Destroy(child.gameObject); 
                }
            }
            EquippableItemObject equippableObject = objectInQuestion.AddComponent<EquippableItemObject>();
            equippableObject.CreateEquippableItemObject(objectInQuestion,null,data,true,false);
            equippableObject.transform.localPosition = new Vector3(0,0,0);
            equippableObject.transform.localScale = new Vector3(2,2,0);
            ResizeItemImage(equippableObject.transform,data.type,true);
            
        } else {
            Debug.Log("OpenEquipInventoryDetails Error: Inventory Item Data Cannot Be Null");
            
            return;
        }
        EquipmentButtonEquip.GetComponent<Button>().onClick.RemoveAllListeners();
        EquipmentButtonEquipToMisc.GetComponent<Button>().onClick.RemoveAllListeners();

        //if equipped sub menu left side is closed
        if (isSubScreen_Equipment_Equipped_Active == false) {
            EquipmentButtonEquip.SetActive(true);
            EquipmentButtonBack.SetActive(true);
            EquipmentButtonEquip.GetComponentInChildren<TMPro.TextMeshProUGUI>().text = "Equip To Main";
            EquipmentButtonEquipToMisc.SetActive(true);
            EquipmentButtonEquipToMisc.GetComponentInChildren<TMPro.TextMeshProUGUI>().text = "Equip To Misc";

            //EQUIP TO MAIN SLOT
            //head
            if (data.type[0] == 'h' && gameData.partyData[currentPartyMemberNumber].EquipSlotHead == null) {
                EquipmentButtonEquip.GetComponent<Button>().onClick.AddListener(delegate{EquipAndCloseSubTabs(data,false);});
                EquipmentButtonEquip.GetComponentInChildren<TMPro.TextMeshProUGUI>().text = "Equip To Head";
            } else if (data.type[0] == 'h' && gameData.partyData[currentPartyMemberNumber].EquipSlotHead != null) {
                EquipmentButtonEquip.GetComponent<Button>().onClick.AddListener(delegate{OnClickEquippedButton(gameData.partyData[currentPartyMemberNumber].EquipSlotHead,"OpenEquipInventoryDetails Error");});
                EquipmentButtonEquip.GetComponentInChildren<TMPro.TextMeshProUGUI>().text = "Equip To Head";
            //torso
            } else if (data.type[0] == 't' && gameData.partyData[currentPartyMemberNumber].EquipSlotTorso == null) {
                EquipmentButtonEquip.GetComponent<Button>().onClick.AddListener(delegate{EquipAndCloseSubTabs(data,false);});
                EquipmentButtonEquip.GetComponentInChildren<TMPro.TextMeshProUGUI>().text = "Equip To Torso";
            } else if (data.type[0] == 't' && gameData.partyData[currentPartyMemberNumber].EquipSlotTorso != null) {
                EquipmentButtonEquip.GetComponent<Button>().onClick.AddListener(delegate{OnClickEquippedButton(gameData.partyData[currentPartyMemberNumber].EquipSlotTorso,"OpenEquipInventoryDetails Error");});
                EquipmentButtonEquip.GetComponentInChildren<TMPro.TextMeshProUGUI>().text = "Equip To Torso";
            //hands
            } else if (data.type[0] == 'a' && gameData.partyData[currentPartyMemberNumber].EquipSlotArms == null) {
                EquipmentButtonEquip.GetComponent<Button>().onClick.AddListener(delegate{EquipAndCloseSubTabs(data,false);});
                EquipmentButtonEquip.GetComponentInChildren<TMPro.TextMeshProUGUI>().text = "Equip To Hands";
            } else if (data.type[0] == 'a' && gameData.partyData[currentPartyMemberNumber].EquipSlotArms != null) {
                EquipmentButtonEquip.GetComponent<Button>().onClick.AddListener(delegate{OnClickEquippedButton(gameData.partyData[currentPartyMemberNumber].EquipSlotArms,"OpenEquipInventoryDetails Error");});
                EquipmentButtonEquip.GetComponentInChildren<TMPro.TextMeshProUGUI>().text = "Equip To Hands";
            //legs
            } else if (data.type[0] == 'l' && gameData.partyData[currentPartyMemberNumber].EquipSlotLegs == null) {
                EquipmentButtonEquip.GetComponent<Button>().onClick.AddListener(delegate{EquipAndCloseSubTabs(data,false);});
                EquipmentButtonEquip.GetComponentInChildren<TMPro.TextMeshProUGUI>().text = "Equip To Legs";
            } else if (data.type[0] == 'l' && gameData.partyData[currentPartyMemberNumber].EquipSlotLegs != null) {
                EquipmentButtonEquip.GetComponent<Button>().onClick.AddListener(delegate{OnClickEquippedButton(gameData.partyData[currentPartyMemberNumber].EquipSlotLegs,"OpenEquipInventoryDetails Error");});
                EquipmentButtonEquip.GetComponentInChildren<TMPro.TextMeshProUGUI>().text = "Equip To Legs";
            //feet
            } else if (data.type[0] == 'f' && gameData.partyData[currentPartyMemberNumber].EquipSlotFeet == null) {
                EquipmentButtonEquip.GetComponent<Button>().onClick.AddListener(delegate{EquipAndCloseSubTabs(data,false);});
                EquipmentButtonEquip.GetComponentInChildren<TMPro.TextMeshProUGUI>().text = "Equip To Feet";
            } else if (data.type[0] == 'f' && gameData.partyData[currentPartyMemberNumber].EquipSlotFeet != null) {
                EquipmentButtonEquip.GetComponent<Button>().onClick.AddListener(delegate{OnClickEquippedButton(gameData.partyData[currentPartyMemberNumber].EquipSlotFeet,"OpenEquipInventoryDetails Error");});
                EquipmentButtonEquip.GetComponentInChildren<TMPro.TextMeshProUGUI>().text = "Equip To Feet";
            //main hand
            } else if (data.type[0] == 'w' && data.type != "w08" && data.type != "w09" && gameData.partyData[currentPartyMemberNumber].EquipSlotMainHand == null) {
                EquipmentButtonEquip.GetComponent<Button>().onClick.AddListener(delegate{EquipAndCloseSubTabs(data,false);});
                EquipmentButtonEquip.GetComponentInChildren<TMPro.TextMeshProUGUI>().text = "Equip Main Hand";

            } else if (data.type[0] == 'w' && data.type != "w08" && data.type != "w09" && gameData.partyData[currentPartyMemberNumber].EquipSlotMainHand != null) {
                EquipmentButtonEquip.GetComponent<Button>().onClick.AddListener(delegate{OnClickEquippedButton(gameData.partyData[currentPartyMemberNumber].EquipSlotMainHand,"OpenEquipInventoryDetails Error");});
                EquipmentButtonEquip.GetComponentInChildren<TMPro.TextMeshProUGUI>().text = "Equip Main Hand";
           
            //off hand
            } else if (data.type[0] == 'w' && gameData.partyData[currentPartyMemberNumber].EquipSlotOffHand == null) {
                EquipmentButtonEquip.GetComponent<Button>().onClick.AddListener(delegate{EquipAndCloseSubTabs(data,false);});
                EquipmentButtonEquip.GetComponentInChildren<TMPro.TextMeshProUGUI>().text = "Equip Off Hand";
            
            } else if (data.type[0] == 'w' && gameData.partyData[currentPartyMemberNumber].EquipSlotOffHand != null) {
                EquipmentButtonEquip.GetComponent<Button>().onClick.AddListener(delegate{OnClickEquippedButton(gameData.partyData[currentPartyMemberNumber].EquipSlotOffHand,"OpenEquipInventoryDetails Error");});
                EquipmentButtonEquip.GetComponentInChildren<TMPro.TextMeshProUGUI>().text = "Equip Off Hand";
            
            } else if (data.type[0] == 'm') {
                EquipmentButtonEquip.SetActive(false);
            }

            //if weapon turn equip to misc into equip to off hand
            if (data.type[0] == 'w' && data.isTwoHanded == false && data.type != "w08" && data.type != "w09" && gameData.partyData[currentPartyMemberNumber].EquipSlotMainHand != null) {

                EquipmentButtonEquipToMisc.GetComponentInChildren<TMPro.TextMeshProUGUI>().text = "Equip Off Hand";
                if (gameData.partyData[currentPartyMemberNumber].EquipSlotOffHand == null) {
                    EquipmentButtonEquipToMisc.GetComponent<Button>().onClick.RemoveAllListeners();
                    EquipmentButtonEquipToMisc.GetComponent<Button>().onClick.AddListener(delegate{EquipAndCloseSubTabs(data,false);});
                } else {
                    EquipmentButtonEquipToMisc.GetComponent<Button>().onClick.RemoveAllListeners();
                    EquipmentButtonEquipToMisc.GetComponent<Button>().onClick.AddListener(delegate{OnClickEquippedButton(gameData.partyData[currentPartyMemberNumber].EquipSlotOffHand,"Off Hand");});
                }
                //EquipmentButtonEquip.GetComponent<Button>().onClick.RemoveAllListeners();
                //EquipmentButtonEquip.GetComponent<Button>().onClick.AddListener(delegate{OnClickEquippedButton(gameData.partyData[currentPartyMemberNumber].EquipSlotMainHand,"OpenEquipInventoryDetails Error");});
                

            } else if (data.isTwoHanded == true || data.type == "w08" || data.type == "w09" || (data.type[0] == 'w' && gameData.partyData[currentPartyMemberNumber].EquipSlotMainHand == null)) {

                EquipmentButtonEquipToMisc.SetActive(false);
            //EQUIP TO MISC
            //if no misc slots are open we open up misc1
            } else {
                if (gameData.partyData[currentPartyMemberNumber].EquipSlotMisc1 != null && gameData.partyData[currentPartyMemberNumber].EquipSlotMisc2 != null && gameData.partyData[currentPartyMemberNumber].EquipSlotMisc3 != null && gameData.partyData[currentPartyMemberNumber].EquipSlotMisc4 != null) {
                    EquipmentButtonEquipToMisc.GetComponent<Button>().onClick.RemoveAllListeners();
                    EquipmentButtonEquipToMisc.GetComponent<Button>().onClick.AddListener(delegate{OnClickEquippedButton(gameData.partyData[currentPartyMemberNumber].EquipSlotMisc1,"Misc");});
                    //otherwise we equip to the next empty misc slot
                } else {
                    EquipmentButtonEquipToMisc.GetComponent<Button>().onClick.RemoveAllListeners();
                    EquipmentButtonEquipToMisc.GetComponent<Button>().onClick.AddListener(delegate{EquipAndCloseSubTabs(data,true);});
                }
            }
            
                
            

        //if equipped sub menu left side is open
        } else if (isSubScreen_Equipment_Equipped_Active == true) {
            BothSubEquipmentMenusActive();

        }

        
        
    }


    public void EquipmentButtonBackPressed() {
        CloseSubScreenEquipmentInventory();

    }


    public void BothSubEquipmentMenusActive() {

        if (itemToEquipOnLeft == null) {
            EquipmentButtonSwap.SetActive(false);
            EquipmentButtonEquipToMisc.SetActive(false);
            EquipmentButtonEquip.SetActive(true);

            EquipmentButtonEquip.GetComponentInChildren<TMPro.TextMeshProUGUI>().text = "Equip";
            EquipmentButtonEquip.GetComponent<Button>().onClick.RemoveAllListeners();
            EquipmentButtonEquip.GetComponent<Button>().onClick.AddListener(delegate{EquipAndCloseSubTabs(itemToEquipOnRight,false);});

        } else {
            EquipmentButtonSwap.SetActive(true);
            EquipmentButtonEquip.SetActive(false);
            EquipmentButtonEquipToMisc.SetActive(false);

            EquipmentButtonSwap.GetComponent<Button>().onClick.RemoveAllListeners();
            EquipmentButtonSwap.GetComponent<Button>().onClick.AddListener(delegate{SwapAndCloseSubTabs(itemToEquipOnRight,itemToEquipOnLeft);});
        }

        
        
    }


    public void DisplaySubMiscSlots(bool display) {
        if (display == true) {
            buttonSubMisc1.gameObject.SetActive(true);
            buttonSubMisc2.gameObject.SetActive(true);
            buttonSubMisc3.gameObject.SetActive(true);
            buttonSubMisc4.gameObject.SetActive(true);
            FillEquippedButton(buttonSubMisc1.gameObject,gameData.partyData[currentPartyMemberNumber].EquipSlotMisc1);
            FillEquippedButton(buttonSubMisc2.gameObject,gameData.partyData[currentPartyMemberNumber].EquipSlotMisc2);
            FillEquippedButton(buttonSubMisc3.gameObject,gameData.partyData[currentPartyMemberNumber].EquipSlotMisc3);
            FillEquippedButton(buttonSubMisc4.gameObject,gameData.partyData[currentPartyMemberNumber].EquipSlotMisc4);
        } else {
            buttonSubMisc1.gameObject.SetActive(false);
            buttonSubMisc2.gameObject.SetActive(false);
            buttonSubMisc3.gameObject.SetActive(false);
            buttonSubMisc4.gameObject.SetActive(false);
        }
        
    }


    public void EquipAndCloseSubTabs(EquippableItemData data, bool isMiscOnly) {
        if (isMiscOnly == true) {
            if (gameData.partyData[currentPartyMemberNumber].TryEquipMiscOnly(data) == false) {
                Debug.Log("Failed to equip: "+data.type+" "+data.spriteAddresses[0]);
            } 
        } else {
            if (gameData.partyData[currentPartyMemberNumber].TryEquip(data) == false) {
                Debug.Log("Failed to equip: "+data.type+" "+data.spriteAddresses[0]);
            } 
        }
        
        
        CloseSubScreenEquipmentInventory();
        if (isSubScreen_Equipment_Equipped_Active == true) {
            CloseSubScreenEquipmentEquipped();
        }
            
        RefreshDisplayCharacter(true,true);
        FillAllEquippedButtons();
        FillEquippmentInventoryScrollview("");


    }


    public void UnEquipAndCloseSubTabs(EquippableItemData data) {

        

        gameData.partyData[currentPartyMemberNumber].UnEquip(data);
        CloseSubScreenEquipmentEquipped();
        CloseSubScreenEquipmentInventory();
        

        RefreshDisplayCharacter(true,true);
        FillAllEquippedButtons();
        FillEquippmentInventoryScrollview("");

        



    }


    public void SwapAndCloseSubTabs(EquippableItemData dataToEquip,EquippableItemData dataToRemove) {

        //if mainhand is empty and you are swapping with off hand, unequip offhand and equip mainhand
        if (dataToEquip.type[0] == 'w' && dataToEquip.type != "w08" && dataToEquip.type != "w09" && gameData.partyData[currentPartyMemberNumber].EquipSlotMainHand == null) {
            if (gameData.partyData[currentPartyMemberNumber].TryEquip(dataToEquip) == false) {
                Debug.Log("Failed to swap with empty main hand");
            } else {
                gameData.partyData[currentPartyMemberNumber].UnEquip(dataToRemove);
            }
            

        } else if (gameData.partyData[currentPartyMemberNumber].TryEquipReplace(dataToEquip,dataToRemove) == false) {
                Debug.Log("Failed to replace: "+dataToEquip.type+" "+dataToEquip.spriteAddresses[0]);
        } 

        CloseSubScreenEquipmentInventory();
        CloseSubScreenEquipmentEquipped();
        
        

        RefreshDisplayCharacter(true,true);
        FillAllEquippedButtons();
        FillEquippmentInventoryScrollview("");


    }


    void ResizeItemImage(Transform itemTransform, string itemType, bool isSubScreen) {
        /*float subMultiplier = 1;
        if (isSubScreen == true) {
            subMultiplier = 2;
        }

        //head but not glasses
        if (itemType[0] == 'h' && itemType != "hd11") {
            itemTransform.localPosition = new Vector3(itemTransform.localPosition.x,itemTransform.localPosition.y - 20*subMultiplier,itemTransform.localPosition.z);
        }
        if (itemType == "hd11") {
            itemTransform.localPosition = new Vector3(itemTransform.localPosition.x,itemTransform.localPosition.y - 15*subMultiplier,itemTransform.localPosition.z);
            itemTransform.localScale = new Vector3(itemTransform.localScale.x+.5f,itemTransform.localScale.y+.5f,itemTransform.localScale.z);
        }
        if (itemType[0] == 't' && itemType != "tr11" && itemType != "tr12") {
            itemTransform.localPosition = new Vector3(itemTransform.localPosition.x,itemTransform.localPosition.y + 15*subMultiplier,itemTransform.localPosition.z);
            itemTransform.localScale = new Vector3(itemTransform.localScale.x+0.5f,itemTransform.localScale.y+0.5f,itemTransform.localScale.z);
        }
        //not bracelets not wrist jewelry
        if (itemType[0] == 'a' && itemType != "ar04" && itemType != "ar05") {
            itemTransform.localPosition = new Vector3(itemTransform.localPosition.x,itemTransform.localPosition.y + 15*subMultiplier,itemTransform.localPosition.z);
            itemTransform.localScale = new Vector3(itemTransform.localScale.x+1f,itemTransform.localScale.y+1f,itemTransform.localScale.z);
        }
        if (itemType[0] == 'l') {
            if (isSubScreen == true) {
                subMultiplier = 1.5f;
            }
            itemTransform.localPosition = new Vector3(itemTransform.localPosition.x,itemTransform.localPosition.y + 50*subMultiplier,itemTransform.localPosition.z);
            itemTransform.localScale = new Vector3(itemTransform.localScale.x+1f,itemTransform.localScale.y+1f,itemTransform.localScale.z);
        }
        if (itemType[0] == 'f') {
            if (isSubScreen == true) {
                subMultiplier = 1.5f;
            }
            itemTransform.localPosition = new Vector3(itemTransform.localPosition.x,itemTransform.localPosition.y + 60*subMultiplier,itemTransform.localPosition.z);
            itemTransform.localScale = new Vector3(itemTransform.localScale.x+1f,itemTransform.localScale.y+1f,itemTransform.localScale.z);
        }

        //scarfs
        if (itemType == "mc07") {
            itemTransform.localPosition = new Vector3(itemTransform.localPosition.x+5*subMultiplier,itemTransform.localPosition.y,itemTransform.localPosition.z);
            itemTransform.localScale = new Vector3(itemTransform.localScale.x+1f,itemTransform.localScale.y+1f,itemTransform.localScale.z);
        }
        //amulets
        if (itemType == "mc08" || itemType == "mc09") {
            itemTransform.localScale = new Vector3(itemTransform.localScale.x+2f,itemTransform.localScale.y+2f,itemTransform.localScale.z);
        }
        //belts
        if (itemType == "mc10" || itemType == "mc11") {
            itemTransform.localPosition = new Vector3(itemTransform.localPosition.x,itemTransform.localPosition.y + 15*subMultiplier,itemTransform.localPosition.z);
            itemTransform.localScale = new Vector3(itemTransform.localScale.x+1.5f,itemTransform.localScale.y+1.5f,itemTransform.localScale.z);
        }
        //earring
        if (itemType == "mc05") {
            itemTransform.localPosition = new Vector3(itemTransform.localPosition.x,itemTransform.localPosition.y - 15*subMultiplier,itemTransform.localPosition.z);
            itemTransform.localScale = new Vector3(itemTransform.localScale.x+2f,itemTransform.localScale.y+2f,itemTransform.localScale.z);
        }
        //ring
        if (itemType == "mc06" || itemType == "ar04" || itemType == "ar05") {
            itemTransform.localPosition = new Vector3(itemTransform.localPosition.x+15*subMultiplier,itemTransform.localPosition.y + 30*subMultiplier,itemTransform.localPosition.z);
            itemTransform.localScale = new Vector3(itemTransform.localScale.x+2f,itemTransform.localScale.y+2f,itemTransform.localScale.z);
        }
        //pauldrens
        if (itemType == "mc01") {
            itemTransform.localScale = new Vector3(itemTransform.localScale.x+1f,itemTransform.localScale.y+1f,itemTransform.localScale.z);
        }
        //capes
        if (itemType == "mc04") {
            itemTransform.localPosition = new Vector3(itemTransform.localPosition.x,itemTransform.localPosition.y + 15*subMultiplier,itemTransform.localPosition.z);
        }*/


    }





    //--------------- JOURNAL ----------------//

    public void FillJournalEntryScrollview() {

        //destroy any pre-existing buttons
       foreach (Transform child in JournalEntryScrollView.transform) {
                GameObject.Destroy(child.gameObject); 
        }

        //iterate gameData.inventoryEquippable and create buttons for each
        for (int i = 0; i < gameData.partyQuestData.Count; i++) {

            QuestData questData = gameData.partyQuestData[i];

            //create object and store var for button component
            GameObject newObject = Instantiate(JournalEntryPrefab);
            newObject.transform.SetParent(JournalEntryScrollView.transform,false);
            newObject.transform.localPosition = new Vector3(newObject.transform.localPosition.x,newObject.transform.position.y,0);
            Button newButton = newObject.transform.GetComponent<Button>();

            
            //set display text for button
            newButton.GetComponentInChildren<TMPro.TextMeshProUGUI>().text = "Quest";


            //add button functionality
            newButton.GetComponent<Button>().onClick.RemoveAllListeners();
            newButton.GetComponent<Button>().onClick.AddListener(delegate{OpenJournalEntry(questData);});


            if (questData == gameData.activeQuest) {
                newObject.transform.GetChild(1).GetComponent<Image>().color = Color.green;
            } else {
                newObject.transform.GetChild(1).GetComponent<Image>().color = Color.black;
            }
            
        }


        
    }


    void OpenJournalEntry(QuestData questData) {

        selectedQuest = questData;
        string questString = "";
        JournalSelectedScrollView.GetComponentInChildren<TMPro.TextMeshProUGUI>().text = questString;

        foreach (StepData step in selectedQuest.stepList) {
            questString += step.text +"\n";
        }
        LocationData destination = questData.stepList[questData.currentStep].locationDestination;
        /*questString += "go to "+destination.name+" at ("+destination.xPos+","+destination.yPos+").\nYou are at ("+gameData.currentTileX+","+gameData.currentTileY+").";*/
        questString = questString.Replace("\r", "");
        JournalSelectedScrollView.GetComponentInChildren<TMPro.TextMeshProUGUI>().text = questString;
        
    }



    public void ButtonJournalSetActive() {
        if (selectedQuest != null) {
            gameData.activeQuest = selectedQuest;
            FillJournalEntryScrollview();
            
        }
        
    }


    public void ButtonJournalForget() {
        if (selectedQuest != null) {
            int indexOfQuest = 0;
            for (int i = 0; i < gameData.partyQuestData.Count; i++) {
                if (gameData.partyQuestData[i] == selectedQuest) {
                    indexOfQuest = gameData.partyQuestData.IndexOf(selectedQuest);
                }
            }
            gameData.partyQuestData.RemoveAt(indexOfQuest);
            JournalSelectedScrollView.GetComponentInChildren<TMPro.TextMeshProUGUI>().text = "";
            FillJournalEntryScrollview();
        }
        
    }




    
    void Update() {

        if (gameData.isGamePaused == true) {
            Cursor.lockState = CursorLockMode.None;
		    Cursor.visible = true;
        }


        if (Input.GetKeyDown("tab")) {
            if (gameData.isGamePaused == false) {
                ButtonInventoryOpen();
            } else {
                ButtonInventoryClose();
            }

        }

        if (Input.GetKeyDown("t")) {
            TrailerButtons.SetActive(true);

        }

        if (Input.GetKeyDown("y")) {
            StartCoroutine("trailerRollDice");
            
        }



        /*if (gameData.activeQuest != null) {
            
            
            
            float distance = Vector3.Distance(activeQuestVector, overworldController.transform.GetChild(0).position);
            Debug.Log("distance between location: "+distance);
            if (distance > 600) {
                
            } else {

            }
            //UIQuestCursor.transform = new Vector3();

            //fromPosition.localPosition = Vector3.Lerp(startPos, toPosition, counter / duration);
        }*/
    }



    IEnumerator trailerRollDice() {
        TrailerText.SetActive(true);
        
        yield return new WaitForSeconds(2);
        d12Animator.gameObject.SetActive(true);
        d8Animator.gameObject.SetActive(true);
        d20Animator.gameObject.SetActive(true);
        float time = .75f;

        float diceEndPos = -100;
        /*d12Animator.gameObject.transform.LeanMoveLocalY(-3.34f-100+dicePos,time).setEaseOutExpo();
        d8Animator.gameObject.transform.LeanMoveLocalY(60.5f-100+dicePos,time).setEaseOutExpo();
        d20Animator.gameObject.transform.LeanMoveLocalY(-100+dicePos,time).setEaseOutExpo();
        diceText.gameObject.transform.LeanMoveLocalY(diceEndPos-200,time).setEaseOutExpo();*/


        time = .95f;
        d12Animator.gameObject.SetActive(true);
        d12Animator.SetTrigger("d12");
        d12Animator.gameObject.transform.LeanMoveLocal(new Vector3(-36.33f,-3.34f-100+diceEndPos,0),time).setEaseOutQuad();

        d8Animator.gameObject.SetActive(true);
        d8Animator.SetTrigger("d8");
        d8Animator.gameObject.transform.LeanMoveLocal(new Vector3(-13,60.5f-100+diceEndPos,0),time).setEaseOutQuad().delay = .02f;

        d20Animator.gameObject.SetActive(true);
        d20Animator.SetTrigger("d20");
        d20Animator.gameObject.transform.LeanMoveLocal(new Vector3(50,-100+diceEndPos,0),time).setEaseOutQuad().delay = .05f;

        diceText.gameObject.transform.LeanScale(new Vector3(3,3,0),.3f).setEaseInOutElastic().delay = time;


        
        TextResult.gameObject.transform.LeanScale(new Vector3(3,3,0),.3f).setEaseOutExpo().delay = time*2.25f;

        yield return new WaitForSeconds(time*2.25f);
        TrailerText.SetActive(false);

    }







    IEnumerator DelayClickParty() {
        isDelayClickPartyActive = true;
        yield return new WaitForSeconds(delayClickParty);
        isDelayClickPartyActive = false;
    }



    public void ButtonQuitApp() {
        Application.Quit();
    }



   
    
}
