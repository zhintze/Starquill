using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;


public class TownSceneLogic : MonoBehaviour
{
    GameData gameData;

    public GameObject ApproachBackdrop;
    public GameObject TownBackdrop;
    public GameObject InteriorBackdrop;
    	public Animator encounterAnim;



    public GameObject partyMembersObject;
    PlayerTownController playerTownController;

    public GameObject TownsfolkHolder;

    public GameObject InhabitantPrefab;



    public GameObject LocationUI;

    public TMPro.TextMeshProUGUI TextBoxText;

    public TMPro.TextMeshProUGUI LocationTitle;

    public LocationData localTownData;

    public GameObject buttonOk;
    public GameObject buttonAccept;
    public GameObject buttonDecline;
    bool isTextQuestAccept;


    public GameObject npcPosition;
    public GameObject buttonLeave;


    [HideInInspector] public List<GameObject> NPCInhabitantObjects; 

    [HideInInspector] public GameObject npcHostileObject;

    [HideInInspector] public bool isTextActive;



    private TypewriterUI typeWriterScript;


    [HideInInspector] public Vector3 partyScaleApproach;
    [HideInInspector] public Vector3 partyScaleTown;
    [HideInInspector] public Vector3 partyPosApproach;
    [HideInInspector] public Vector3 partyPosTown;

    [HideInInspector] public bool isApproaching;

    void Awake() {
        LeanTween.reset();
        partyScaleApproach = new Vector3(.75f,.75f,.75f);
        partyScaleTown = new Vector3(1.5f,1.5f,1.5f);
        partyPosApproach = new Vector3(600f,114f,0);
        partyPosTown = new Vector3(-200f,114f,0);
    }

    void Start()
    {
        


        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        gameData = GameData.localData;

        playerTownController = partyMembersObject.GetComponent<PlayerTownController>();

        localTownData = gameData.currentLocationData;
        
///////////------------ Test Town Data
        //localTownData = new LocationData(false,new Vector2(0,2),20);

        LocationTitle.text = "Location: "+localTownData.name;

        Camera.main.clearFlags = CameraClearFlags.SolidColor;
        Camera.main.backgroundColor = HexColorConvert.Parse(gameData.colorCodes.skinColorArray[Random.Range(10,15)]);

        ExitText();

        NPCInhabitantObjects = new List<GameObject>();

        typeWriterScript = TextBoxText.GetComponent<TypewriterUI>();



        //init npcs
        if (localTownData.isHostile == false) {
            //town
            //initialize inhabitants
            if (localTownData.NPCInhabitants.Count > 0) {
                foreach (NPCData npc in localTownData.NPCInhabitants) {
                    AddInhabitant(npc);
                }
                
            }
            

        } else {

        }
        StartCoroutine("TownsfolkSpawnSlightDelay");


        ApproachScreen();


    }


    void Update() {
        
        if (typeWriterScript.isTextDisplayComplete == true) {
            if (isTextQuestAccept == true) {
                isTextQuestAccept = false;
                buttonAccept.SetActive(true);
                buttonDecline.SetActive(true);
            } else {
                buttonOk.SetActive(true);
            }
            
            typeWriterScript.isTextDisplayComplete = false;
        }
        if (isApproaching == true) {
            if (partyMembersObject.transform.GetChild(0).localPosition.x >= 190) {
                encounterAnim.gameObject.SetActive(true);
		        StartCoroutine("EncounterReverse");
                isApproaching = false;
                TownScreen();
            } else if (partyMembersObject.transform.GetChild(0).localPosition.x <= -400) {
                encounterAnim.gameObject.SetActive(true);
                StartCoroutine("EncounterReverse");
                isApproaching = false;
                ExitLocation();
            }
        }
    }





    IEnumerator TownsfolkSpawnSlightDelay() {
        yield return new WaitForSeconds(.05f);
        TownsfolkHolder.SetActive(false);
    }





    //////////////////////////////////
    //   Screen Launch Functions    //
    //////////////////////////////////
    void ApproachScreen() {
        isApproaching = true;
        ApproachBackdrop.SetActive(true);
        TownBackdrop.SetActive(false);
        InteriorBackdrop.SetActive(false);
        buttonLeave.SetActive(false);
        LocationTitle.gameObject.SetActive(false);
    }


    void TownScreen() {
        isApproaching = false;
        ApproachBackdrop.SetActive(false);
        TownBackdrop.SetActive(true);
        InteriorBackdrop.SetActive(false);
        buttonLeave.SetActive(true);
        LocationTitle.gameObject.SetActive(true);

        partyMembersObject.transform.localScale = partyScaleTown;
        partyMembersObject.transform.position = partyPosTown;
        partyMembersObject.transform.GetChild(0).transform.localPosition = new Vector3(40,3.3f,0);


        TownsfolkHolder.SetActive(true);

        

        //check to see if this locationData is currently a trigger in a quest
        

        //if no inhabitants or no prev step data, add narrated


        /*if (localTownData.stepData != null) {
            Debug.Log("activate next step of quest");
            NextStep();

            if (localTownData.stepData.npcHostile != null) {
                npcHostileObject = new GameObject();
                npcHostileObject.name = "villain";
                npcHostileObject.transform.SetParent(encounterMembersObject.transform,false);
        
                CharacterObject characterObject = npcHostileObject.AddComponent<CharacterObject>();
                characterObject.CreateCharacterObject(npcHostileObject,localTownData.stepData.npcHostile.data,true);
                npcHostileObject.transform.eulerAngles = new Vector3(0,180,0);
            } else {
                //if npcHostile is null, plot has not been made yet
            }
        }*/


        //add housing

    }


    void InteriorScreen() {

    }
    /////////////////////////////////////
    //   END Screen Launch Functions   //
    /////////////////////////////////////









    //////////////////////////////////
    //    NPC Inhabitant Functions  //
    //////////////////////////////////

    public void AddInhabitant(NPCData npc) {
        //add inhabitants and one as a quest giver
        GameObject NPCDisplay = Instantiate(InhabitantPrefab);
        NPCDisplay.GetComponent<NPCTownController>().townLogic = this;
        NPCDisplay.name = "CharacterDisplay";
        NPCDisplay.transform.SetParent(npcPosition.transform,false);

        //----!!!! change to random position
        int RanX = Random.Range(50,800);
        //int RanY = Random.Range(70,70);

        NPCDisplay.transform.localPosition = new Vector3(RanX,60,0);

        GameObject NPCObject = NPCDisplay.transform.Find("NPCObject").gameObject;
        CharacterObject characterObject = NPCObject.AddComponent<CharacterObject>();
        characterObject.CreateCharacterObject(NPCObject,npc.data,true);
        NPCObject.transform.eulerAngles = new Vector3(0,0,0);

        NPCInhabitantObjects.Add(NPCDisplay);
        
    }

    public void CreateInhabitantButton(GameObject inhabitant) {
        //-- cycle through all possible NPCinhabitants and assign the one needed
        int indexOfInhabitant = 0;
        foreach (GameObject npcObject in NPCInhabitantObjects) {
            if (inhabitant == npcObject) {
                indexOfInhabitant = NPCInhabitantObjects.IndexOf(npcObject);
                break;
            }
        }

        //-- Check if NPC has text
        NPCData currentNPC = localTownData.NPCInhabitants[indexOfInhabitant];
        Transform child;
        Button button;
        if ((currentNPC.questGiverData != null || currentNPC.speech != "" || currentNPC.shout != "") && currentNPC.isTalkButtonLive == false) {
            currentNPC.isTalkButtonLive = true;
            child = inhabitant.transform.Find("NPCButton");
                
            buttonAnimAppear(child.gameObject);
            button = child.GetComponent<Button>();
            button.onClick.RemoveAllListeners();
        } else {
            return;
        }

        //-- Assign NPC speech/shout/quest text to a talk button
        if (currentNPC.questGiverData == null) {
            if (currentNPC.speech != "") {
                button.onClick.AddListener(delegate{NPCLaunchSpeech(currentNPC,button.gameObject);});

            } else if (currentNPC.shout != "") {
                //shouts are inactive buttons just text
                button.GetComponentInChildren<TMPro.TMP_Text>().text = currentNPC.shout;
                button.enabled = false;
            }
        } else {
            if (currentNPC.questGiverData.isQuestGiven == false) {
                //give quest
                button.onClick.AddListener(delegate{NPCLaunchNewQuest(currentNPC,button.gameObject);});

            } else if (currentNPC.questGiverData.isQuestGiven == true) {
                //quest is given already, so use After Given Text
                button.onClick.AddListener(delegate{NPCLaunchGivenQuest(currentNPC,button.gameObject);});
            }
        }
        
        
    }


    public void RemoveInhabitantButton(GameObject inhabitant) {
        int indexOfInhabitant = 0;
        foreach (GameObject npcObject in NPCInhabitantObjects) {
            if (inhabitant == npcObject) {
                indexOfInhabitant = NPCInhabitantObjects.IndexOf(npcObject);
                break;
            }
        }
        NPCData currentNPC = localTownData.NPCInhabitants[indexOfInhabitant];

        if ((currentNPC.questGiverData != null || currentNPC.speech != "" || currentNPC.shout != "") && currentNPC.isTalkButtonLive == true) {
            currentNPC.isTalkButtonLive = false;
            Transform child = inhabitant.transform.Find("NPCButton");
            StartCoroutine(buttonAnimDisappear(child.gameObject));
        }
        
    }
    //////////////////////////////////
    // END NPC Inhabitant Functions //
    //////////////////////////////////











    ///////////////////////////////////////
    //  Text and QuestGiver Functions    //
    ///////////////////////////////////////

    void NPCLaunchSpeech(NPCData npcData,GameObject button) {
        TextBoxText.gameObject.SetActive(true);
        TextBoxText.text = npcData.speech;
        typeWriterScript.npcSpeaker = npcData;
        button.SetActive(false);
        EnterText();
    }      

    void NPCLaunchNewQuest(NPCData npcData,GameObject button) {
        npcData.questGiverData.PromptQuest();
        localTownData.stepData = npcData.questGiverData.newQuest.stepList[0];
        TextBoxText.gameObject.SetActive(true);
        TextBoxText.text = npcData.questGiverData.newQuest.stepList[0].text;
        typeWriterScript.npcSpeaker = npcData;

        isTextQuestAccept = true;
        buttonAccept.GetComponent<Button>().onClick.AddListener(delegate{ButtonPressedAccept(npcData);});
        buttonDecline.GetComponent<Button>().onClick.AddListener(delegate{ButtonPressedDecline(npcData);});
        
        button.SetActive(false);
        EnterText();
    }


    void NPCLaunchGivenQuest(NPCData npcData,GameObject button) {
        TextBoxText.gameObject.SetActive(true);
        TextBoxText.text = npcData.questGiverData.newQuest.afterQuestGivenText;
        typeWriterScript.npcSpeaker = npcData;
        
        button.SetActive(false);
        EnterText();
        
    }


    void NPCLaunchDeclinedQuestText(NPCData npcData) {
        ExitText();
        TextBoxText.gameObject.SetActive(true);
        TextBoxText.text = npcData.questGiverData.newQuest.declinedQuestText;
        typeWriterScript.npcSpeaker = npcData;
        EnterText();
        
    }


    void NextStep() {
        //Debug.Log(localTownData.stepData.text);
        //Debug.Log(localTownData.stepData.stepType);
        //StepData newStep = localTownData.stepData.NextStep();
        localTownData.stepData.isComplete = true;
        localTownData.stepData.parentQuest.currentStep++;
        StepData newStep = new StepData(localTownData.stepData.parentQuest.currentStep,localTownData.stepData.parentQuest,localTownData);
        localTownData.stepData.parentQuest.stepList.Add(newStep);

       // Debug.Log(newStep.text);
        //Debug.Log(newStep.stepType);
        localTownData.stepData = newStep;
        TextBoxText.gameObject.SetActive(true);
        TextBoxText.text = localTownData.stepData.text;
        //Debug.Log(localTownData.stepData.text);
        //Debug.Log(localTownData.stepData.stepType);
        EnterText();


         if (npcHostileObject != null && (newStep.stepType == "conclusion" || newStep.stepType == "engagement")) {
            Destroy(npcHostileObject);
        }

        

    }

    void EnterText() {
        isTextActive = true;
        LocationUI.SetActive(true);
        TextAnimAppear(LocationUI);
        typeWriterScript.StartText();
    }

    

    void ExitText() {
        //this assumes stepdata is only stored at locations for triggers
        isTextActive = false;
        LocationUI.SetActive(false);
        buttonOk.SetActive(false);
        buttonAccept.SetActive(false);
        buttonDecline.SetActive(false);
    }


    void TextAnimAppear(GameObject textUI) {
        float time = .3f;
        textUI.gameObject.SetActive(true);
        textUI.transform.LeanScale(new Vector3(1f,1f,textUI.transform.position.z),time).setEaseOutQuad();
    }

    IEnumerator TextAnimDisappear(GameObject textUI) {
        float time = .3f;
        textUI.transform.LeanScale(new Vector3(0f,0f,textUI.transform.position.z),time).setEaseOutQuad();
        yield return new WaitForSeconds(time);
        textUI.SetActive(false);
    }

    ///////////////////////////////////////
    // END Text and QuestGiver Functions //
    /////////////////////////////////////// 








    //////////////////////////////
    //   Encounter Animations   //
    //////////////////////////////   
    IEnumerator EncounterAnim() {
        encounterAnim.SetTrigger("EncounterTriggered");
		yield return new WaitForSeconds(.6f);
		encounterAnim.gameObject.SetActive(false);

	}
    IEnumerator EncounterReverse() {
        encounterAnim.SetTrigger("EncounterReverse");
		yield return new WaitForSeconds(.6f);
		encounterAnim.gameObject.SetActive(false);
        

	}
    ////////////////////////////////
    //  END Encounter Animations  //
    ////////////////////////////////






    //////////////////////
    //      Buttons     //
    //////////////////////
    public void ButtonPressedOk() {
        typeWriterScript.isTextDisplayComplete = false;
        ExitText();
    }
    public void ButtonPressedAccept(NPCData npc) {
        npc.questGiverData.ActivateQuest();
        typeWriterScript.isTextDisplayComplete = false;
        ExitText();
        
    }
    public void ButtonPressedDecline(NPCData npc) {
        typeWriterScript.isTextDisplayComplete = false;
        ExitText();
        NPCLaunchDeclinedQuestText(npc);
    }

    public void ExitLocation() {
        
        gameData.mapArray[gameData.currentMap].entryPosition = new Vector2(localTownData.tile.x,localTownData.tile.y);
        gameData.mapArray[gameData.currentMap].entryPosWithinTile = new Vector3(localTownData.posInTile.x,localTownData.height,localTownData.posInTile.z);
        gameData.currentLocationData = null;
        
        SceneManager.LoadScene("OverWorld3D");
    }


    void buttonAnimAppear(GameObject button) {
        float time = .2f;
        button.gameObject.SetActive(true);
        button.transform.LeanScale(new Vector3(.5f,.5f,button.transform.position.z),time).setEaseOutQuad();
    }

    IEnumerator buttonAnimDisappear(GameObject button) {
        float time = .2f;
        button.transform.LeanScale(new Vector3(0f,0f,button.transform.position.z),time).setEaseOutQuad();
        yield return new WaitForSeconds(time);
        button.SetActive(false);
    }
    //////////////////////
    //    END Buttons   //
    //////////////////////

    








    
}
