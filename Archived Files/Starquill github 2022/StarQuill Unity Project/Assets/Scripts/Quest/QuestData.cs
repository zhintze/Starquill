using System.Collections;
using System.Collections.Generic;
using UnityEngine;



// quest giver doesnt work and has no habitat

public class QuestData
{
    GameData gameData;

    public List<StepData> stepList;
    public List<LocationData> stepLocationList;

    public int currentStep;
    public QuestGiverData questGiver;
    public string questItem;
    public string afterQuestGivenText;
    public string declinedQuestText;

    //activate interaction


    public QuestData(QuestGiverData questGiven, LocationData locationData) {
        gameData = GameData.localData;

        stepList = new List<StepData>();
        stepLocationList = new List<LocationData>();

        questGiver = questGiven;

        currentStep = 0;
        stepList.Add(new StepData(currentStep,this,locationData));


    }



    


    
}
