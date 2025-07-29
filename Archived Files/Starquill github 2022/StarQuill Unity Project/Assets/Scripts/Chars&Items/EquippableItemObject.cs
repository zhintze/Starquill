using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class EquippableItemObject : MonoBehaviour
{
    EquippableItemData data;

    public void CreateEquippableItemObject(GameObject parent,CharacterData parentData, EquippableItemData itemData, bool isUI, bool is3D) {
        data = itemData;

        for (int i = 0; i < data.spriteAddresses.Count; i++) {
            string spriteAddress = data.spriteAddresses[i];

            if (is3D == true) {
                string split1 = data.spriteAddresses[i].Substring(0,21)+"10x";
                string split2 = data.spriteAddresses[i].Substring(21,data.spriteAddresses[i].Length-21);
                spriteAddress = split1+split2;
            }

            //create layer object and set as a child
            GameObject equipmentLayerObject = new GameObject();
            equipmentLayerObject.name = itemData.spriteAddresses[i]; //just for unity name debugging

            //ensures all weapons are above all other layers??!?!?
            if (data.type[0] == 'w') {
                equipmentLayerObject.name = "w"+equipmentLayerObject.name;
            }
            
            equipmentLayerObject.transform.SetParent(parent.transform,false);

           if (isUI == true) {
                equipmentLayerObject.AddComponent<Image>();
                Image objRenderer = equipmentLayerObject.GetComponent<Image>();
            
                //set sorting order layer
                //objRenderer.g.parent.GetComponent<Canvas>().sortingOrder = data.itemLayers[i];
                equipmentLayerObject.transform.SetSiblingIndex(data.itemLayers[i]);
                //load sprite with sprite address
                StartCoroutine(LoadImage(objRenderer,spriteAddress));
                

                objRenderer.color = HexColorConvert.Parse(data.spriteColors[i]);
            } else {
                equipmentLayerObject.AddComponent<SpriteRenderer>();
                SpriteRenderer objRenderer = equipmentLayerObject.GetComponent<SpriteRenderer>();
            
                //set sorting order layer
                objRenderer.sortingOrder = data.itemLayers[i];

                //load sprite with sprite address
                StartCoroutine(LoadSprite(objRenderer,spriteAddress));

                objRenderer.color = HexColorConvert.Parse(data.spriteColors[i]);
            }
            
            
            

            //move and rotate weapon to backhand if necessary
            if (parentData != null) {
                if ((parentData.EquipSlotMainHand != null && (parentData.EquipSlotMainHand.type == "w09" || parentData.EquipSlotMainHand.type == "w08") && parentData.EquipSlotOffHand != null && parentData.EquipSlotOffHand == data) 
                || (parentData.EquipSlotMainHand != null && (parentData.EquipSlotMainHand.type != "w09" && parentData.EquipSlotMainHand.type != "w08") && parentData.EquipSlotMainHand == data))  {
                
                    if (parentData.EquipSlotMainHand.isTwoHanded == true || (parentData.EquipSlotMainHand != null && parentData.EquipSlotOffHand != null) && (parentData.EquipSlotMainHand.type == "w09" || parentData.EquipSlotMainHand.type == "w08") && (parentData.EquipSlotOffHand.type == "w09" || parentData.EquipSlotOffHand.type == "w08")) {

                    } else {
                        equipmentLayerObject.transform.localPosition = new Vector3(10f,5f,0);
                        equipmentLayerObject.transform.eulerAngles = new Vector3(0,0,40);

                        

                        //fix flails for backhand
                        if (equipmentLayerObject.name == "wAssets/Images/Weapons/w03-166-0008.png" || equipmentLayerObject.name == "wAssets/Images/Weapons/w03-166-0013.png" || equipmentLayerObject.name == "wAssets/Images/Weapons/w03-166-0014.png") {
                            equipmentLayerObject.transform.localPosition = new Vector3(22.5f,11.5f,0);
                            equipmentLayerObject.transform.eulerAngles = new Vector3(0,0,3);
                        }
                        if (equipmentLayerObject.name == "wAssets/Images/Weapons/w02-166-0005.png"|| equipmentLayerObject.name == "wAssets/Images/Weapons/w02-166-0017.png") {
                            equipmentLayerObject.transform.localPosition = new Vector3(21f,14.5f,0);
                            equipmentLayerObject.transform.eulerAngles = new Vector3(0,0,3);
                        }



                        // DEPRECATED VECTOR ANGLE FOR UI
                        /*
                        if (isUI == true) {
                            equipmentLayerObject.transform.localPosition = new Vector3(10f,5f,0);
                            equipmentLayerObject.transform.eulerAngles = new Vector3(0,0,40);
                        } else {
                            equipmentLayerObject.transform.localPosition = new Vector3(.2f,.1f,0);
                            equipmentLayerObject.transform.eulerAngles = new Vector3(0,0,40);
                        }
                        */
                        
                    
                    }

                }
            }



////////////// 3D World Settings for Weapons
            if (is3D == true) {
                equipmentLayerObject.transform.localPosition = new Vector3(0,0,0);
                equipmentLayerObject.transform.localEulerAngles = new Vector3(0,0,0);
            }


            
                
            
            

        }


    }





    IEnumerator LoadSprite(SpriteRenderer objRenderer, string address) {
        AsyncOperationHandle<Sprite> handle = Addressables.LoadAssetAsync<Sprite>(address);
        yield return handle;
        if(handle.Result != null && objRenderer != null) {
            objRenderer.sprite = handle.Result;
        }
            

    }


    IEnumerator LoadImage(Image objRenderer, string address) {
        AsyncOperationHandle<Sprite> handle = Addressables.LoadAssetAsync<Sprite>(address);
        yield return handle;
        
        if(handle.Result != null && objRenderer != null) {
            objRenderer.sprite = handle.Result;
        }
            

    }

    



    
}
