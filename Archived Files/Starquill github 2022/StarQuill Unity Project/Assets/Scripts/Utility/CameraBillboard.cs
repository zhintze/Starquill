using System.Collections;
using System.Collections.Generic;
using UnityEngine;

 public class CameraBillboard : MonoBehaviour
 {
     Transform target;

     private void LateUpdate()
     {
         //transform.forward = new Vector3(Camera.main.transform.forward.x, transform.forward.y, Camera.main.transform.forward.z);
        
        if (target == null) {
            target = GameObject.FindWithTag("Player").transform;
        }

        //int damping = 2;
        Vector3 lookPos = target.position - transform.position;
        lookPos.y = 0;

        if (lookPos != Vector3.zero) {
            Quaternion rotation = Quaternion.LookRotation(lookPos);
            transform.rotation = rotation;
        }
        //transform.rotation = Quaternion.Slerp(transform.rotation, rotation, Time.deltaTime * damping);
        
     }
 }
