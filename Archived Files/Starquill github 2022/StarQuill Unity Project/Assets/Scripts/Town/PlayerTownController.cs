using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine;

public class PlayerTownController : MonoBehaviour
{
    GameData gameData;

    //---- created character - made for overworld testing
    GameObject PartyTeamObject;
    GameObject PartyMember1Image;
    GameObject PartyMember2Image;
    GameObject PartyMember3Image;
    GameObject PartyMember4Image;

    public TownSceneLogic townLogic;

    //movement variables
    float moveSpeed = .75f;
    private bool isMoving = false;
    private string direction;
    int currentCameraTileX;
    int currentCameraTileY;
    int cameraTileMovementLimitX;
    int cameraTileMovementLimitY;
    float cameraTileDistance;

    //encounter variables
    int movesUntilEncounter;
    bool isEncounterDisabled;



    private float partyFlipApproach = 75;
    private float partyFlipTown = 180;

    Camera mainCamera;


    bool isFacingLeft = false;

    void Start() {
        gameData = GameData.localData;

        RefreshDisplayCharacterParty();
        

    }



    public void RefreshDisplayCharacterParty() {
        isFacingLeft = false;

        if (PartyTeamObject != null) {
            Destroy(PartyTeamObject);
        }
        PartyTeamObject = new GameObject();
        PartyTeamObject.transform.SetParent(this.transform,false);

        
        PartyMember1Image = new GameObject();
        PartyMember1Image.name = "playableCharacter";
        PartyMember1Image.transform.SetParent(PartyTeamObject.transform,false);
        CharacterObject characterObject = PartyMember1Image.AddComponent<CharacterObject>();
        characterObject.CreateCharacterObject(PartyMember1Image,gameData.partyData[0],true);
        PartyMember1Image.transform.position = PartyTeamObject.transform.position;


        int charSpacingX = 80;
        int charSpacingY = 40;

        
        
        if (gameData.partyData.Count > 2 ) {
            PartyMember3Image = new GameObject();
            PartyMember3Image.name = "PartyMember3Image";
            PartyMember3Image.transform.SetParent(PartyTeamObject.transform,false);
            CharacterObject characterObject3 = PartyMember3Image.AddComponent<CharacterObject>();
            characterObject3.CreateCharacterObject(PartyMember3Image,gameData.partyData[2],true);
            
            PartyMember3Image.transform.position = new Vector3(PartyTeamObject.transform.position.x-charSpacingX,PartyTeamObject.transform.position.y-charSpacingY,PartyTeamObject.transform.position.z);
        }


        
        

        if (gameData.partyData.Count > 3) {
        PartyMember4Image = new GameObject();
        PartyMember4Image.name = "PartyMember4Image";
        PartyMember4Image.transform.SetParent(PartyTeamObject.transform,false);
        CharacterObject characterObject4 = PartyMember4Image.AddComponent<CharacterObject>();
        characterObject4.CreateCharacterObject(PartyMember4Image,gameData.partyData[3],true);
        PartyMember4Image.transform.position = new Vector3(PartyTeamObject.transform.position.x-charSpacingX*2,PartyTeamObject.transform.position.y,PartyTeamObject.transform.position.z);
        }


        if (gameData.partyData.Count > 1 ) {

            PartyMember2Image = new GameObject();
            PartyMember2Image.name = "PartyMember2Image";
            PartyMember2Image.transform.SetParent(PartyTeamObject.transform,false);
            CharacterObject characterObject2 = PartyMember2Image.AddComponent<CharacterObject>();
            characterObject2.CreateCharacterObject(PartyMember2Image,gameData.partyData[1],true);
                    PartyMember2Image.transform.position = new Vector3(PartyTeamObject.transform.position.x-charSpacingX,PartyTeamObject.transform.position.y+charSpacingY,PartyTeamObject.transform.position.z);

        }

        
        PartyTeamObject.transform.position = new Vector3(this.transform.position.x+60,this.transform.position.y+5,this.transform.position.z);

        if (townLogic.isApproaching == true) {
            this.transform.position = townLogic.partyPosApproach;
            this.transform.localScale = townLogic.partyScaleApproach;
        }

    }


    





    void Update() {
        if (gameData.isGamePaused == true || townLogic.isTextActive == true) {
            return;
        }
        if (townLogic.isApproaching == true) { 
            if (isMoving == false)
                {
                    MoveCharacter("Right");
                }
        } else {
            if (Input.GetKey("a") || Input.GetKey("left") || Input.GetKey("s")) {
                direction = "Left";
                if (isMoving == false)
                {
                    MoveCharacter(direction);
                }
            } else if (Input.GetKey("d") || Input.GetKey("right") || Input.GetKey("w")) {
                direction = "Right";
                if (isMoving == false)
                    {
                        MoveCharacter(direction);
                    }
            } 
            CheckDistanceOfNPCInhabitants();
        }
        

        
    }





    




     public void MoveCharacter(string moveDirection)
    {




        if (moveDirection == "Left")
        {

            if (isFacingLeft == false) {
                isFacingLeft = true;
                PartyTeamObject.transform.eulerAngles = new Vector3(0, 180, 0);
                if (townLogic.isApproaching == true) {
                    PartyTeamObject.transform.position = new Vector3(PartyTeamObject.transform.position.x-partyFlipApproach,PartyTeamObject.transform.position.y,PartyTeamObject.transform.position.z);
                } else {
                    PartyTeamObject.transform.position = new Vector3(PartyTeamObject.transform.position.x-partyFlipTown,PartyTeamObject.transform.position.y,PartyTeamObject.transform.position.z);
                }
            }
            
            

            isMoving = true;
            StartCoroutine(MovePartyWithSpeed(moveDirection,moveSpeed));
            StartCoroutine("AnimatedCharacterMovement");
            


        }
        else if (moveDirection == "Right")
        {

            if (isFacingLeft == true) {
                isFacingLeft = false;
                PartyTeamObject.transform.eulerAngles = new Vector3(0, 0, 0);
                if (townLogic.isApproaching == true) {
                    PartyTeamObject.transform.position = new Vector3(PartyTeamObject.transform.position.x+partyFlipApproach,PartyTeamObject.transform.position.y,PartyTeamObject.transform.position.z);
                } else {
                    PartyTeamObject.transform.position = new Vector3(PartyTeamObject.transform.position.x+partyFlipTown,PartyTeamObject.transform.position.y,PartyTeamObject.transform.position.z);
                }
            }
            

            
            isMoving = true;
            StartCoroutine(MovePartyWithSpeed(moveDirection,moveSpeed));
            StartCoroutine("AnimatedCharacterMovement");


        }
        else if (moveDirection == "Up")
        {

            
            isMoving = true;
            StartCoroutine(MovePartyWithSpeed(moveDirection,moveSpeed));
            StartCoroutine("AnimatedCharacterMovement");


        }
        else if (moveDirection == "Down")
        {
            
            

            isMoving = true;
            StartCoroutine(MovePartyWithSpeed(moveDirection,moveSpeed));
            StartCoroutine("AnimatedCharacterMovement");


        }
        else
        {
            Debug.Log("MoveCharacter() Error: not a valid swipe and/or valid tile");

        }



    }

    IEnumerator waitWalk() {
        yield return new WaitForSeconds(.4f);
        isMoving = false;
        /*if (gameData.mapArray[gameData.currentMap].dataTileArray[gameData.player_xTile,gameData.player_zTile].isGateway == true)
        {
            Debug.Log("Enter Gateway: " + gameData.mapArray[gameData.currentMap].tileArray[gameData.player_xTile,gameData.player_zTile].parentMap.name);


            EnterLocation(gameData.mapArray[gameData.mapCurrent].tileArray[gameData.currentTileX][gameData.currentTileY].locationData);

        }
        else
        {
            if (isEncounterDisabled == false)
            {
                EncounterStep();
            }

        }*/
    }


    IEnumerator MovePartyWithSpeed(string moveDirection, float duration) {
        float counter = 0;

       

        Vector3 startPos = PartyTeamObject.transform.localPosition;
        Transform fromPosition = PartyTeamObject.transform;

        Vector3 toPosition = new Vector3(startPos.x,startPos.y,0);
        float movement = 60;

        if (moveDirection == "Left") {
            toPosition = new Vector3(startPos.x-movement,startPos.y,0);

        } else if (moveDirection == "Right") {
            toPosition = new Vector3(startPos.x+movement,startPos.y,0);

        } else if (moveDirection == "Up") {
            toPosition = new Vector3(startPos.x,startPos.y+movement,0);

        } else if (moveDirection == "Down") {
            toPosition = new Vector3(startPos.x,startPos.y-movement,0);

        }

        


        while (counter < duration)
        {
            counter += Time.deltaTime;
            fromPosition.localPosition = Vector3.Lerp(startPos, toPosition, counter / duration);
           
            
            
            
            yield return null;
        }
        
        isMoving = false;
        
        

        

    }


    void CheckDistanceOfNPCInhabitants() {
        //Debug.Log("Distance: "+Vector3.Distance(PartyTeamObject.transform.position, Inhabitant.transform.position));
        
        foreach (GameObject npc in townLogic.NPCInhabitantObjects) {
             if (Vector3.Distance(PartyTeamObject.transform.position, npc.transform.position) < 220) {
                townLogic.CreateInhabitantButton(npc);

            } else if (Vector3.Distance(PartyTeamObject.transform.position, npc.transform.position) > 220) {
                townLogic.RemoveInhabitantButton(npc);
            }
        }
        
       
    }



    void EncounterStep()
    {
        movesUntilEncounter -= 1;
        if (movesUntilEncounter <= 0)
        {
            Debug.Log("WILD ENCOUNTER OCCURS");
            movesUntilEncounter = Random.Range(2, 5);
            //encounterAnim.gameObject.SetActive(true);
            //encounterAnim.SetTrigger("EncounterTriggered");
           //StartCoroutine("EncounterAnimation");

        }

    }



    /*void EnterLocation(LocationData currentLocationData) {
        
        Debug.Log("open location");
        gameData.mapArray[gameData.currentMap].entryPosition = new Vector2(gameData.player_xTile,gameData.player_zTile);
        gameData.currentLocationData = currentLocationData;
        SceneManager.LoadScene("Location");
        


    }*/




    IEnumerator AnimatedCharacterMovement() {

        
        StartCoroutine(AnimateCharacterMove(PartyMember1Image,.06f,.165f,.09f,.1575f,.09f,.1575f));

        if (gameData.partyData.Count > 0 ) {
            //yield return new WaitForSeconds(.2f);
            StartCoroutine(AnimateCharacterMove(PartyMember2Image,.09f,.1575f,.09f,.165f,.09f,.1575f));
        }

        if (gameData.partyData.Count > 1 ) {
            //yield return new WaitForSeconds(.1f);
            StartCoroutine(AnimateCharacterMove(PartyMember3Image,.09f,.15f,.09f,.1575f,.09f,.1575f));
        }

        if (gameData.partyData.Count > 2 ) {
            //yield return new WaitForSeconds(.15f);
            StartCoroutine(AnimateCharacterMove(PartyMember4Image,.105f,.165f,.09f,.1575f,.09f,.1425f));
        }
        yield return null;

    }

    IEnumerator AnimateCharacterMove(GameObject character, float duration1, float duration2, float duration3, float duration4, float duration5, float duration6) {
        float counter = 0;


       

        Vector3 startPos = character.transform.localPosition;
        Transform fromPosition = character.transform;

        Vector3 toPosition = new Vector3(startPos.x,startPos.y+10,0);


        while (counter < duration1)
        {
            counter += Time.deltaTime;
            fromPosition.localPosition = Vector3.Lerp(startPos, toPosition, counter / duration1);
            yield return null;
        }

        counter = 0;
        startPos = character.transform.localPosition;
        fromPosition = character.transform;
        toPosition = new Vector3(startPos.x,startPos.y-10,0);


        while (counter < duration2)
        {
            counter += Time.deltaTime;
            fromPosition.localPosition = Vector3.Lerp(startPos, toPosition, counter / duration2);
            yield return null;
        }

        
        counter = 0;
        startPos = character.transform.localPosition;
        fromPosition = character.transform;
        toPosition = new Vector3(startPos.x,startPos.y+10,0);


        while (counter < duration3)
        {
            counter += Time.deltaTime;
            fromPosition.localPosition = Vector3.Lerp(startPos, toPosition, counter / duration3);
            yield return null;
        }

        counter = 0;
        startPos = character.transform.localPosition;
        fromPosition = character.transform;
        toPosition = new Vector3(startPos.x,startPos.y-10,0);


        while (counter < duration4)
        {
            counter += Time.deltaTime;
            fromPosition.localPosition = Vector3.Lerp(startPos, toPosition, counter / duration4);
            yield return null;
        }

        counter = 0;
        startPos = character.transform.localPosition;
        fromPosition = character.transform;
        toPosition = new Vector3(startPos.x,startPos.y+10,0);


        while (counter < duration5)
        {
            counter += Time.deltaTime;
            fromPosition.localPosition = Vector3.Lerp(startPos, toPosition, counter / duration5);
            yield return null;
        }

        counter = 0;
        startPos = character.transform.localPosition;
        fromPosition = character.transform;
        toPosition = new Vector3(startPos.x,startPos.y-10,0);


        while (counter < duration6)
        {
            counter += Time.deltaTime;
            fromPosition.localPosition = Vector3.Lerp(startPos, toPosition, counter / duration6);
            yield return null;
        }




    }




}
