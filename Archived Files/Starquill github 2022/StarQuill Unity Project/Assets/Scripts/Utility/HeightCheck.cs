using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HeightCheck : MonoBehaviour
{

    [HideInInspector] public LocationData locationData;
    [HideInInspector] public GameObject parent;


    public void SetLocation(LocationData _locationData, GameObject _parent) {
        locationData = _locationData;
        parent = _parent;
    }

    void OnCollisionEnter(Collision collision) {
        Debug.Log("ground hit");
        if (collision.gameObject.tag == "Ground") {
            GetComponent<Rigidbody>().useGravity = false;

            if (locationData != null) {
                LocationHeightDrop(locationData);
            } else {
                BasicDrop();
            }

        }
        
    }

    void BasicDrop() {
        parent.transform.position = new Vector3(parent.transform.position.x, gameObject.transform.position.y+1, parent.transform.position.z);
		Destroy(this.gameObject);
    }



    void LocationHeightDrop(LocationData locationData) {
		//Debug.Log("height at drop: "+gameObject.transform.position.y);
		float heightAdjustment = parent.GetComponent<RectTransform>().rect.height/8;
        locationData.isHeightSet = true;
        locationData.height = gameObject.transform.position.y+heightAdjustment-1;
        parent.transform.position = new Vector3(parent.transform.position.x, locationData.height, parent.transform.position.z);
		Destroy(this.gameObject);
	}
}
