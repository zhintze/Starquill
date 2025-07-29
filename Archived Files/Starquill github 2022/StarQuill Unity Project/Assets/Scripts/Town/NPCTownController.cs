using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NPCTownController : MonoBehaviour
{
    GameData gameData;

    float moveSpeed = 1f;

    private bool isMoving = false;
    [HideInInspector] public TownSceneLogic townLogic;
    GameObject NPCObject;
    GameObject NPCButton;

    float timeBetweenMovement;


    // Start is called before the first frame update
    void Start()
    {
        timeBetweenMovement = 0;
        gameData = GameData.localData;
        NPCObject = this.transform.Find("NPCObject").gameObject;
        NPCButton = this.transform.Find("NPCButton").gameObject;
        timeBetweenMovement = Random.Range(0,3);

        
    }

    // Update is called once per frame
    void Update()
    {
        if (gameData.isGamePaused == true || (townLogic != null && townLogic.isTextActive == true)) {
            return;
        }

        if (isMoving == false) {
            
            
            timeBetweenMovement -= Time.deltaTime;
            if (timeBetweenMovement <= 0) {
                int randomDirection = Random.Range(0,2);
                timeBetweenMovement = Random.Range(3,8);

                if (randomDirection == 0) {
                    NPCObject.transform.eulerAngles = new Vector3(0,180,0);

                    NPCObject.transform.LeanMoveLocalX(NPCObject.transform.localPosition.x-15,moveSpeed);
                    NPCButton.transform.LeanMoveLocalX(NPCButton.transform.localPosition.x-15,moveSpeed);

                    WalkingBounceAnim();
                } else if (randomDirection == 1) {
                    NPCObject.transform.eulerAngles = new Vector3(0,0,0);
                    NPCObject.transform.LeanMoveLocalX(NPCObject.transform.localPosition.x+15,moveSpeed);
                    NPCButton.transform.LeanMoveLocalX(NPCButton.transform.localPosition.x+15,moveSpeed);
                    WalkingBounceAnim();
                    
                } 
            }
        }
    }

    void WalkingBounceAnim() {
        NPCObject.transform.LeanMoveLocalY(NPCObject.transform.localPosition.y+10,moveSpeed/8).setEaseOutQuad();
        NPCObject.transform.LeanMoveLocalY(NPCObject.transform.localPosition.y,moveSpeed/8).setEaseInQuad().delay = moveSpeed/8;
        NPCObject.transform.LeanMoveLocalY(NPCObject.transform.localPosition.y+10,moveSpeed/8).setEaseOutQuad().delay = moveSpeed/8 * 2;
        NPCObject.transform.LeanMoveLocalY(NPCObject.transform.localPosition.y,moveSpeed/8).setEaseInQuad().delay = moveSpeed/8 * 3;
        NPCObject.transform.LeanMoveLocalY(NPCObject.transform.localPosition.y+10,moveSpeed/8).setEaseOutQuad().delay = moveSpeed/8 * 4;
        NPCObject.transform.LeanMoveLocalY(NPCObject.transform.localPosition.y,moveSpeed/8).setEaseInQuad().delay = moveSpeed/8 * 5;
        NPCObject.transform.LeanMoveLocalY(NPCObject.transform.localPosition.y+10,moveSpeed/8).setEaseOutQuad().delay = moveSpeed/8 * 6;
        NPCObject.transform.LeanMoveLocalY(NPCObject.transform.localPosition.y,moveSpeed/8).setEaseInQuad().delay = moveSpeed/8 * 7;
    }



}
