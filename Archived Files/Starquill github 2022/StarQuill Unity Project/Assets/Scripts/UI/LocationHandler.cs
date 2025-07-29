using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine;


public class LocationHandler : MonoBehaviour
{
    [HideInInspector] public LocationData locationData;
    public Animator encounterAnim;
    [HideInInspector] public bool isApproachable;

    void Awake() {
        isApproachable = false;
    }

    void Update() {
        if (isApproachable == true && GameData.localData.isGamePaused == false) {
            if (Input.GetKeyDown("return")) {
                Approach();
                isApproachable = false;
            }
        }
    }

    public void EnterRadius(LocationData _locationData) {
        locationData = _locationData;
        isApproachable = true;
        transform.GetChild(0).gameObject.SetActive(true);
        transform.GetChild(0).gameObject.GetComponent<TMPro.TMP_Text>().text = "Press Enter To Approach "+locationData.name;
    }


    public void Approach() {
        encounterAnim.gameObject.SetActive(true);
        encounterAnim.SetTrigger("EncounterTriggered");

        Debug.Log("open location");
        //SETUP EXIT POSITION
        //GameStateData.localGameStateData.mapArray[GameStateData.localGameStateData.mapCurrent].exitPosition = new Vector2(GameStateData.localGameStateData.currentTileX,GameStateData.localGameStateData.currentTileY);
        GameData.localData.currentLocationData = locationData;
        StartCoroutine("LoadTown");
    }

    public void LeaveRadius() {
        isApproachable = false;
        transform.GetChild(0).gameObject.SetActive(false);
        locationData = null;
    }



    IEnumerator LoadTown() {
        yield return new WaitForSeconds(.8f);
        SceneManager.LoadScene("Town");
    }
}
