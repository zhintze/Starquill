using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NPCData
{
    public CharacterData data;
    public string name;
    public QuestGiverData questGiverData;
    public string speech;
    public string shout;
    public bool isTalkButtonLive;


    public NPCData() {
        data = CreateRandomCharacterData();
        name = GenerateRandomName();
        speech = "";
        shout = "";
        //shout = "\"heck\"";
        //make quest giver here?
    }



    public CharacterData CreateRandomCharacterData() {
        //Debug.Log(GameData.localData.speciesArray.species.Count);
        int selectedRace = UnityEngine.Random.Range(0,GameData.localData.speciesArray.species.Count);
        
        CharacterData characterData = new CharacterData(GameData.localData.speciesArray.species[selectedRace], 1, 1, 1, 1, 1, 1);

        GameData.localData.CreateEquipmentStarterPackDefault(characterData,4,5);
        return characterData;
    }



    public string GenerateRandomName() {
        string[] NameArray = GameData.localData.csvNPCNames.text.Split(char.Parse("\n"));

        string chosenName = NameArray[Random.Range(0,NameArray.Length)];
        return chosenName;
    }
    
}
