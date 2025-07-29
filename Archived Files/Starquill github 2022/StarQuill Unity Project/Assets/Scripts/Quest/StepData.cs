using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StepData {
    GameData gameData; 

    public string text;
    public LocationData locationData;
    public LocationData locationDestination;
    public NPCData npcHostile;
    public string stepType;
    public QuestData parentQuest;
    public int thisStep;
    public int prevStep;
    public bool isLocationTriggerCreationComplete;
    public bool isComplete;
    public bool activateInteraction;

    public List<LocationData> locationPassives;
    public List<NPCData> npcPassives;


   public int ds36 = 99; 


   public StepData(int stepNumber, QuestData parent, LocationData _locationData) {
        gameData = GameData.localData;
        locationPassives = new List<LocationData>();
        npcPassives = new List<NPCData>();

        locationData = _locationData;
        parentQuest = parent;
        thisStep = stepNumber;
        prevStep = stepNumber-1;

        
        if (thisStep == 0) {
            CreateExposition();
            if (parentQuest.questGiver != null) {
                CreateAfterQuestGivenText();
                CreateDeclinedQuestText();
            }

        } else if (thisStep > 0) {
            string prevType = parentQuest.stepList[prevStep].stepType;
            if (prevType == "exposition") {
                CreatePlot();
            } else if (prevType == "plot") {
                ds36 = parentQuest.stepList[prevStep].ds36;
                Debug.Log("engagement");
                CreateEngagement();
            } else if (prevType == "engagement") {
                Debug.Log("conclusion/plottwist");
                ds36 = parentQuest.stepList[prevStep].ds36;
                TryPlotTwist();
            }
        }

   }


   public void NextStep() {
       isComplete = true;
       parentQuest.currentStep++;
       //StepData newStep = new StepData(parentQuest.currentStep,parentQuest);
       //parentQuest.stepList.Add(newStep);
        //return newStep;
   }

   

   void CreateExposition() {
    int typeOfIntro = Random.Range(0,2);
    if (parentQuest.questGiver.npcData == null) {
        if (typeOfIntro == 0) {
            FillTextHoles(gameData.questCSV.introNarratedObservation[Random.Range(0,gameData.questCSV.introNarratedObservation.Count)],"exposition");
            
        } else if (typeOfIntro == 1) {
            FillTextHoles(gameData.questCSV.introNarratedSerendipitous[Random.Range(0,gameData.questCSV.introNarratedSerendipitous.Count)],"exposition");
            
        }
    } else {
        if (typeOfIntro == 0) {
            FillTextHoles(gameData.questCSV.introNPCTalk[Random.Range(0,gameData.questCSV.introNPCTalk.Count)],"exposition");

        } else if (typeOfIntro == 1) {
            FillTextHoles(gameData.questCSV.introNPCForced[Random.Range(0,gameData.questCSV.introNPCForced.Count)],"exposition");
            
        }
    }
    
   }


    void CreatePlot() {
        ds36 = Random.Range(0,gameData.questCSV.Plot.Count);
        FillTextHoles(gameData.questCSV.Plot[Random.Range(1,gameData.questCSV.Plot.Count)][ds36],"plot");

    }

    void CreateEngagement() {
        npcHostile = parentQuest.stepList[prevStep].npcHostile;
        FillTextHoles(gameData.questCSV.Engagement[Random.Range(1,gameData.questCSV.Engagement.Count)][ds36],"engagement");
    }

    void TryPlotTwist() {
        npcHostile = parentQuest.stepList[prevStep].npcHostile;
        int plotTwist = Random.Range(0,2);
        if (plotTwist == 0) {
            FillTextHoles(gameData.questCSV.Conclusion[Random.Range(1,gameData.questCSV.Conclusion.Count)][ds36],"conclusion");
        } else {
            CreatePlot();
        }
    }


    void CreateAfterQuestGivenText() {
        parentQuest.afterQuestGivenText = "Have you completed my quest yet?";
    }

    void CreateDeclinedQuestText() {
        parentQuest.declinedQuestText = "That is unfortunate. I hope you reconsider.";
    }



   public void FillTextHoles(string subjectString,string plotType) {

        bool allBracketsReplaced = false;
        bool bracketFound = false;
        string replacementString = subjectString;

        replacementString = curlyBracketReplacement(subjectString);

        //---- [PLOT brackets] ----//
            for (int i = 0; i < subjectString.Length; i++) {
                if (subjectString[i] == '[') {
                    for (int j = i; j < subjectString.Length; j++) {
                        if (subjectString[j] == ']') {

                            if (subjectString.Substring(i,j-i+1) == "[plot]") {
                                string replacement = TextReplacement(subjectString.Substring(i,j-i+1),false,false);

                                if (subjectString.Length-j-1 <= 0) {
                                    replacementString = subjectString.Substring(0,i) + replacement;
                                
                                } else {
                                    //Debug.Log(subjectString.Substring(j-i+1,subjectString.Length-j-1));
                                    replacementString = subjectString.Substring(0,i) + replacement + subjectString.Substring(j+1,subjectString.Length-j-1);
                                }

                                plotType = "plot";

                            }

                            
                        }
                    }
                }
            }
        subjectString = replacementString;
        stepType = plotType;

        replacementString = curlyBracketReplacement(subjectString);
        
        //---- [square brackets] ----//
        do {
            subjectString = replacementString;
            
            for (int i = 0; i < subjectString.Length; i++) {
                bracketFound = false;
                //hunt for next bracket
                if (subjectString[i] == '[') {
                    for (int j = i; j < subjectString.Length; j++) {
                        if (subjectString[j] == ']') { 
                            
                            //i and j now enclose what we need to replace
                            string replacement = TextReplacement(subjectString.Substring(i,j-i+1),false,false);
                            if (subjectString.Length-j-1 <= 0) {
                                replacementString = subjectString.Substring(0,i) + replacement;
                                
                            } else {
                                //Debug.Log(subjectString.Substring(j-i+1,subjectString.Length-j-1));
                                replacementString = subjectString.Substring(0,i) + replacement + subjectString.Substring(j+1,subjectString.Length-j-1);
                            }
                            
                            
                            replacementString = curlyBracketReplacement(replacementString);
                            bracketFound = true;
                            break;
                        }
                    }
                }
                if (bracketFound == true) {
                    break;
                }
            }


            if (bracketFound == false) {
                allBracketsReplaced = true;
            }

        } while (allBracketsReplaced == false);
        
        //creates location trigger if not created already
        if (stepType == "plot" || stepType == "exposition") {
            CompleteLocationTrigger();
        }
        

        subjectString = subjectString.Replace("\r", "");
        text = subjectString;
        
    }




    public string curlyBracketReplacement(string subjectString) {
        //curly brackets
        //---- {curly brackets} ----//
        bool allBracketsReplaced = false;
        bool bracketFound = false;
        string replacementString = subjectString;
        do {
            subjectString = replacementString;
            for (int i = 0; i < subjectString.Length; i++) {
                bracketFound = false;
                //hunt for next bracket
                if (subjectString[i] == '{') {
                    for (int j = i; j < subjectString.Length; j++) {
                        if (subjectString[j] == '}') { 

                            string triggerContent = "";
                            int kHolder = 0;
                            for (int k = i; k >= 0; k--) {
                                if (subjectString[k] == '[') {
                                    triggerContent = subjectString.Substring(k,i-k);
                                    //Debug.Log("triggerContent: "+triggerContent);
                                    kHolder = k;
                                    break;
                                }
                            }
                            
                            string replacement = "";
                            //create trigger or passive data
                            if (subjectString.Substring(i,j-i+1) == "{trigger}") {
                                //Debug.Log(subjectString.Substring(i,j-i+1) + " recognized as trigger");
                                replacement = TextReplacement(triggerContent,true,false);
                            } else if (subjectString.Substring(i,j-i+1) == "{passive}") {
                                //Debug.Log(subjectString.Substring(i,j-i+1) + " recognized as passive");
                                replacement = TextReplacement(triggerContent,false,true);
                            } else {
                                Debug.Log("some kind of trigger error");
                            }
                            
                            //designated preceding brackets triggers/passives and then remove curly bracket portion
                            replacementString = subjectString.Substring(0,kHolder) + replacement + subjectString.Substring(j+1,subjectString.Length-j-1);
                            bracketFound = true;
                            break;
                        }
                    }
                }
                if (bracketFound == true) {
                    break;
                }
            }
            if (bracketFound == false) {
                allBracketsReplaced = true;
            }
        } while (allBracketsReplaced == false);
        allBracketsReplaced = false;

        return(subjectString);
    }


    public string TextReplacement(string subjectString, bool isTrigger, bool isPassive) {
        string replacement = "";

        if (subjectString == "[supplication]") {
            replacement = gameData.questCSV.Supplication[Random.Range(0,gameData.questCSV.Supplication.Count)];
        } else if (subjectString == "[greeting]") {
            replacement = gameData.questCSV.Greeting[Random.Range(0,gameData.questCSV.Greeting.Count)];
        } else if (subjectString == "[threat]") {
            replacement = gameData.questCSV.Threat[Random.Range(0,gameData.questCSV.Threat.Count)];
        } else if (subjectString == "[get reward]") {
            replacement = gameData.questCSV.GetReward[Random.Range(0,gameData.questCSV.GetReward.Count)];
        } else if (subjectString == "[promise of reward]") {
            replacement = gameData.questCSV.PromiseOfReward[Random.Range(0,gameData.questCSV.PromiseOfReward.Count)];
        } else if (subjectString == "[reward]") {
            replacement = "reward";
        } else if (subjectString == "[quest item purpose]") {
            replacement = gameData.questCSV.QuestItemPurpose[Random.Range(0,gameData.questCSV.QuestItemPurpose.Count)];

        } else if (subjectString == "[quest item]") {
            if (parentQuest.questItem != null) {
                replacement = "<color=green>"+parentQuest.questItem+"</color>";
            } else {
                parentQuest.questItem = gameData.questCSV.QuestItem[Random.Range(0,gameData.questCSV.QuestItem.Count)];
                replacement = "<color=green>"+ parentQuest.questItem +"</color>";
            }
        
        } else if (subjectString == "[outro]") {
            replacement = gameData.questCSV.Outro[Random.Range(0,gameData.questCSV.Outro.Count)];
        } else if (subjectString == "[quest giver]") {
            if (parentQuest.questGiver.npcData != null) {
                replacement = parentQuest.questGiver.npcData.name;
            }
            
            
        } else if (subjectString == "[location]") {

            if (stepType == "plot" || stepType == "exposition") {
                replacement = CreateNewLocation(isTrigger,isPassive,false);
            } else if (stepType == "engagement" || stepType == "conclusion") {
                if (isTrigger == true) {
                    replacement = "<color=blue>"+parentQuest.stepList[prevStep].locationDestination.name+"</color>";
                } else {
                    replacement = "<color=grey>"+parentQuest.stepList[prevStep].locationPassives[Random.Range(0,locationPassives.Count)].name+"</grey>";
                }
                
            }

        } else if (subjectString == "[location hostile]") {
            
            if (stepType == "plot" || stepType == "exposition") {
                replacement = CreateNewLocation(isTrigger,isPassive,true);
            } else if (stepType == "engagement" || stepType == "conclusion") {
                if (isTrigger == true) {
                    replacement = "<color=blue>"+parentQuest.stepList[prevStep].locationDestination.name+"</color>";
                } else {
                    replacement = "<color=grey>"+parentQuest.stepList[prevStep].locationPassives[Random.Range(0,locationPassives.Count)].name+"</grey>";
                }
                
            }

        
        } else if (subjectString == "[npc]" || subjectString == "[npc hostile]" || subjectString == "[npc friendly]") {
            
            if (isTrigger == true) {
                CreateNPCHostile();
                replacement = "<color=red>"+npcHostile.name+"</color>";
                //Debug.Log("<color=red>"+npcHostile.name+"</color>");
            } else if(isPassive == true) {
                NPCData npcVar = CreateNPCPassive();
                replacement = "<color=grey>"+npcVar.name+"</color>";


            ///---------- !!!!! WARNING this assumes all npcs are hostile in engagement and conclusion
            } else {
                CreateNPCHostile();
                replacement = "<color=red>"+npcHostile.name+"</color>";
                //Debug.Log("<color=red>"+npcHostile.name+"</color>");
            }
            
            


        } else if (subjectString == "[plot]") {
            ds36 = Random.Range(0,gameData.questCSV.Plot.Count);

            replacement = gameData.questCSV.Plot[Random.Range(1,gameData.questCSV.Plot[ds36].Count)][ds36];
            
        } else if (subjectString == "[action]") {
            replacement = gameData.questCSV.Action[Random.Range(1,gameData.questCSV.Action[ds36].Count)][ds36];


        } else if (subjectString == "[interaction]") {
            replacement = "";
            //Debug.Log("assign interaction");
            activateInteraction = true;
        } else {
            Debug.Log("replacement error:"+subjectString);
            replacement = "replacement error";
        }


        
        return replacement;
    }




    //activates if text does not mention a location
    public void CompleteLocationTrigger() {

        

        if (isLocationTriggerCreationComplete == false) {
            int isHostileInt = Random.Range(0,2);
            bool isHostile = false;
            if (isHostileInt == 0){isHostile = true;}
            CreateNewLocation(true,false,isHostile);
        }


        CreateNPCHostile();
        

        
    }



    string CreateNewLocation(bool isTrigger,bool isPassive, bool isHostile) {
        string replacement = "";
        
        if (isTrigger == true) {
            isLocationTriggerCreationComplete = true;

            //LOOP PARTY
            bool isLocationTileChosen = false;
            int safetyCatch = 0;
            int bigCatch = 20;
            int minX = 2;
            int minZ = 2;
            int maxX = 4;
            int maxZ = 4;
            int distanceX = Random.Range(minX,maxX);
            int distanceZ = Random.Range(minZ,maxZ);
            
            do {
                
                int chosenDistanceX = (int)locationData.tile.x - distanceX;
                int chosenDistanceZ = (int)locationData.tile.y - distanceZ;

                if (chosenDistanceX >= gameData.mapArray[gameData.currentMap].xMapSize-1) {
                    chosenDistanceX -= gameData.mapArray[gameData.currentMap].xMapSize-1;
                } else if (chosenDistanceX < 0) {
                    chosenDistanceX += gameData.mapArray[gameData.currentMap].xMapSize-1;
                }
                if (chosenDistanceZ >= gameData.mapArray[gameData.currentMap].zMapSize-1) {
                    chosenDistanceZ -= gameData.mapArray[gameData.currentMap].zMapSize-1;
                } else if (chosenDistanceZ < 0) {
                    chosenDistanceZ += gameData.mapArray[gameData.currentMap].zMapSize-1;
                }

                if (gameData.mapArray[gameData.currentMap].dataTileArray[chosenDistanceX,chosenDistanceZ] != null &&
                    gameData.mapArray[gameData.currentMap].dataTileArray[chosenDistanceX,chosenDistanceZ].locationData == null) {
                    LocationData newLocation = new LocationData(true,new Vector2(chosenDistanceX,chosenDistanceZ), gameData.mapArray[gameData.currentMap].xMapSize);
                    gameData.mapArray[gameData.currentMap].dataTileArray[chosenDistanceX,chosenDistanceZ].locationData = newLocation;
                    locationDestination = newLocation;
                    Debug.Log("("+newLocation.tile.x+" , "+newLocation.tile.y+") locationDestination is currently not stored in the next step");
                    isLocationTileChosen = true;
                }

                safetyCatch++;
                if (safetyCatch > bigCatch) {
                    minX++;maxX++;minZ++;maxZ++;
                }
                if (safetyCatch > 1000) {
                    Debug.Log("Stepdata.cs: Massive Quest Location Spawn Failure");
                    isLocationTileChosen = true;
                }

            } while (isLocationTileChosen == false);

            
            

            

            /*newLocation.PlaceLocationDataBasedOnTile(gameData.currentTileX,gameData.currentTileY);
            locationDestination = newLocation;
            newLocation.stepData = this;
            replacement = "<color=blue>"+newLocation.name+"</color>";*/

        } else if(isPassive == true) {
            /*LocationData newLocation = new LocationData(isHostile,Vector2.zero);

            newLocation.PlaceLocationDataBasedOnTile(gameData.currentTileX,gameData.currentTileY);
            locationPassives.Add(newLocation);
            replacement = "<color=grey>"+newLocation.name+"</color>";*/
        }

        return replacement;
    }


    void CreateNPCHostile() {
        if (npcHostile != null) {

        } else if (stepType != "engagement") {
            npcHostile = new NPCData();
        }
    }


    NPCData CreateNPCPassive() {
        NPCData data = new NPCData();
        //currently dont get added anywhere
        npcPassives.Add(data);
        return data;
    }
   
   
}
