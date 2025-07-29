using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class QuestGiverData
{
    
    public NPCData npcData;
    public LocationData locationData;
    public bool isReturnRequired;
    public bool isExisting;
    public QuestData newQuest;
    public bool isQuestGiven;
    public bool isQuestPrompted;


    public QuestGiverData(NPCData _npcData, LocationData _locationData, bool returnRequired) {
        npcData = _npcData;
        locationData = _locationData;
        isReturnRequired = returnRequired;
        isExisting = true;

        
    }

    public QuestGiverData(LocationData _locationData, bool returnRequired) {
        npcData = null;
        locationData = _locationData;
        isReturnRequired = returnRequired;
        isExisting = true;

    }


    public void PromptQuest() {
        if (isQuestPrompted == false) {
            isQuestPrompted = true;
            newQuest = new QuestData(this,locationData);
        }
        
    }


    public void ActivateQuest() {
        isQuestGiven = true;
        GameData.localData.partyQuestData.Add(newQuest);
        if (GameData.localData.activeQuest == null) {
            GameData.localData.activeQuest = newQuest;
        }
    }

}
