using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


/*
    Location Object Class
    ======================
    Represents an Object on the 3d World map that the player may "approach" and "enter".
    Displays the locationApproachButton from the InventoryUI and feeds it the LocationObject instance's locationData 
    when Player tag collides with this object's Collider Trigger.
*/

public class LocationObject : MonoBehaviour
{
    
    public LocationData locationData;

    GameObject locationButton;
    LocationHandler locationHandler;
    

    void Start() {
        locationButton = GameObject.FindGameObjectWithTag("LocationHandler");
        locationHandler = locationButton.GetComponent<LocationHandler>();
        
    }



    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Player") {
            if (locationHandler != null && locationHandler.isApproachable == false) {
                locationHandler.EnterRadius(locationData);

            } else {
                StartCoroutine("DelayedAttempt");
            }
            
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.tag == "Player") {
            locationHandler.LeaveRadius();
        }
    }


    //OnTriggerEnter may have triggered before the button was created, so the attempt is delayed before trying again.
    IEnumerator DelayedAttempt() {
        yield return new WaitForSeconds(1);
        if (locationHandler != null && locationHandler.isApproachable == false) {
            locationHandler.EnterRadius(locationData);
        }
    }



}
