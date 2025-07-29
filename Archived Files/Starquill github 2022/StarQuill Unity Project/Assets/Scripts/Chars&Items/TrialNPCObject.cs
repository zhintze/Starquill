using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrialNPCObject : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        //add inhabitants and one as a quest giver
        /*GameObject NPCDisplay = new GameObject();
        
        NPCDisplay.name = "CharacterDisplay";*/

        //----!!!! change to random position
        //int RanY = Random.Range(70,70);

        NPCData npc = new NPCData();
        GameObject holder = new GameObject();
        holder.transform.SetParent(this.transform,false);
        CharacterObject characterObject = holder.AddComponent<CharacterObject>();
        characterObject.CreateCharacterObject(holder,npc.data,false);

    }

}
