using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class CharacterObject : MonoBehaviour
{

    public CharacterData data;
    GameObject baseDisplayObject;
    GameObject parent;

    


    public void CreateCharacterObject(GameObject parentObject,CharacterData characterData,bool isUI) {
        data = characterData;
        parent = parentObject;
        parent.transform.localScale = new Vector3(data.speciesData.xScale,data.speciesData.yScale,1);
        
        

        //load all body parts
        for (int i = 0; i < data.bodyPartSpriteAddresses.Count; i++) {
            
            

            //create object
            GameObject newBodyPart = new GameObject();
            newBodyPart.name = data.bodyPartSpriteAddresses[i]; //just for unity name debugging
            newBodyPart.transform.SetParent(parent.transform,false);

            //load sprite
            if (isUI == true) {
                newBodyPart.AddComponent<Image>();
                Image objRenderer = newBodyPart.GetComponent<Image>();
                //newBodyPart.transform.SetSiblingIndex(data.bodyPartLayers[i]);
                StartCoroutine(LoadImage(objRenderer,data.bodyPartSpriteAddresses[i]));
                //set color
                objRenderer.color = HexColorConvert.Parse(data.bodyPartSpriteColors[i]);

            } else {
                
                newBodyPart.AddComponent<SpriteRenderer>();
                SpriteRenderer objRenderer = newBodyPart.GetComponent<SpriteRenderer>();

                //set sorting layer
                objRenderer.sortingOrder = data.bodyPartLayers[i];
                StartCoroutine(LoadSprite(objRenderer,data.bodyPartSpriteAddresses[i]));
                //set color
                objRenderer.color = HexColorConvert.Parse(data.bodyPartSpriteColors[i]);
            }

            
             
            
        }

        


        //load equipment
        List<EquippableItemData> EquippedItems = data.GetListOfEquippedItems();
        List<EquippableItemData> ItemsToCompare = data.GetListOfEquippedItems();
        List<int> currentItemHiddenLayers = new List<int>();

        for (int i = 0; i < EquippedItems.Count; i++) {
        
            //check for duplicates and invalid image stacking
            bool isDuplicate = false;
            for (int j = 0; j < ItemsToCompare.Count; j++) {
                bool itemDataIsHat = (EquippedItems[i].type == "hd01" || EquippedItems[i].type == "hd02" || EquippedItems[i].type == "hd03" || EquippedItems[i].type == "hd04" || EquippedItems[i].type == "hd05" || EquippedItems[i].type == "hd06" || EquippedItems[i].type == "hd07" || EquippedItems[i].type == "hd08");
                bool ItemToCompareIsHat = (ItemsToCompare[j].type == "hd01" || ItemsToCompare[j].type == "hd02" || ItemsToCompare[j].type == "hd03" || ItemsToCompare[j].type == "hd04" || ItemsToCompare[j].type == "hd05" || ItemsToCompare[j].type == "hd06" || ItemsToCompare[j].type == "hd07" || ItemsToCompare[j].type == "hd08");
                
                bool itemDataIsEyeWear = (EquippedItems[i].type == "hd10" || EquippedItems[i].type == "hd09");
                bool ItemToCompareIsEyeWear = (ItemsToCompare[j].type == "hd10" || ItemsToCompare[j].type == "hd09");
                
                bool itemDataIsFoot = (EquippedItems[i].type == "fe01" || EquippedItems[i].type == "fe02");
                bool ItemToCompareIsFoot = (ItemsToCompare[j].type == "fe01" || ItemsToCompare[j].type == "fe02");
                
                bool itemDataIsWepOnBack = (EquippedItems[i].type == "mc02" || EquippedItems[i].type == "mc03");
                bool ItemToCompareIsWepOnBack = (ItemsToCompare[j].type == "mc02" || ItemsToCompare[j].type == "mc03");
                
                bool itemDataIsBelt = (EquippedItems[i].type == "mc10" || EquippedItems[i].type == "mc11");
                bool ItemToCompareIsBelt = (ItemsToCompare[j].type == "mc10" || ItemsToCompare[j].type == "mc11");
                
                bool itemDataIsCloak = (EquippedItems[i].type == "tr11" || EquippedItems[i].type == "tr12");
                bool ItemToCompareIsCloak = (ItemsToCompare[j].type == "tr11" || ItemsToCompare[j].type == "tr12");
                
                bool itemDataIsShirt = (EquippedItems[i].type == "tr01" || EquippedItems[i].type == "tr02" || EquippedItems[i].type == "tr03" || EquippedItems[i].type == "tr04");
                bool ItemToCompareIsShirt = (ItemsToCompare[j].type == "tr01" || ItemsToCompare[j].type == "tr02" || ItemsToCompare[j].type == "tr03" || ItemsToCompare[j].type == "tr04");

                if (ItemsToCompare[j].isBeingDisplayed == true && ((EquippedItems[i].type == ItemsToCompare[j].type && EquippedItems[i].type[0] != 'w') || (itemDataIsHat && ItemToCompareIsHat) || (itemDataIsEyeWear && ItemToCompareIsEyeWear)
                    || (itemDataIsFoot && ItemToCompareIsFoot) || (itemDataIsWepOnBack && ItemToCompareIsWepOnBack) || (itemDataIsBelt && ItemToCompareIsBelt)
                    || (itemDataIsCloak && ItemToCompareIsCloak) || (itemDataIsShirt && ItemToCompareIsShirt))) {

                        isDuplicate = true;
                        
                        

                }
                

            }
        
            if (isDuplicate == false) {
                EquippedItems[i].isBeingDisplayed = true;
                ItemsToCompare[i].isBeingDisplayed = true;

                bool isRestricted = false;
                foreach (string restriction in data.speciesData.itemRestrictionsArray) {
                    if (EquippedItems[i].type == restriction) {
                        isRestricted = true;
                    }
                }
                if (isRestricted == false) {
                    EquippableItemObject equippableObject = parent.AddComponent<EquippableItemObject>();
                    equippableObject.CreateEquippableItemObject(parent,data,EquippedItems[i],isUI,false);

                    foreach (int hiddenLayer in EquippedItems[i].itemHiddenLayers) {
                        currentItemHiddenLayers.Add(hiddenLayer);
                    }
                }
                
                
            }
            

        }


        

        
        HideLayersBasedOnEquipment(currentItemHiddenLayers);
        
        SortUILayers();
        

    }





    



    void HideLayersBasedOnEquipment(List<int> hiddenLayerArray) {
        List<EquippableItemData> EquippedItems = data.GetListOfEquippedItems();
        
        foreach (EquippableItemData itemData in EquippedItems) {
            foreach (int hiddenLayer in hiddenLayerArray) {

                //for sprites
                SpriteRenderer[] childrenArray = parent.gameObject.GetComponentsInChildren<SpriteRenderer>();
                    foreach (SpriteRenderer renderer in childrenArray) {
                        if (renderer.sortingOrder == hiddenLayer) {
                            renderer.gameObject.SetActive(false);
                        }
                }

                //for UI images
                Image[] childrenArray2 = parent.gameObject.GetComponentsInChildren<Image>();
                    foreach (Image renderer in childrenArray2) {
                        
                        string cutObjString;
                        string[] objString;

                        if (renderer.gameObject.name[0] == 'w') {
                             cutObjString = renderer.gameObject.name.Substring(renderer.gameObject.name.Length-12);
                             objString = cutObjString.Split(char.Parse("-"));
                            
                        } else {
                            cutObjString = renderer.gameObject.name.Substring(renderer.gameObject.name.Length-7);
                            objString = cutObjString.Split(char.Parse("."));
                            
                        }

                        int objLayer = 0;
                        int.TryParse(objString[0],out objLayer);
                        if (objLayer == hiddenLayer) {
                            renderer.gameObject.SetActive(false);
                        }
                }

                
            }
        }

        foreach(EquippableItemData ItemsToCompare in data.GetListOfEquippedItems()) {
            ItemsToCompare.isBeingDisplayed = false;
        }
    
    }

    

    



    void SortUILayers() {
        //yield return new WaitForSeconds(.5f);
        for (int i = 0; i < 50; i++) {
            Transform[] objectList = this.transform.GetComponentsInChildren<Transform>();
            Transform[] identicalObjectList = this.transform.GetComponentsInChildren<Transform>();
        
            foreach (Transform obj in objectList) {
                foreach (Transform otherObj in identicalObjectList) {
                    if (obj.name != "CharacterDisplay" && otherObj.name != "CharacterDisplay" &&
                        obj.name != "Canvas" && otherObj.name != "Canvas") {
                        string cutObjString;
                        string[] objString;
                        string cutOtherObjString;
                        string[] otherObjString;

                        if (obj.name[0] == 'w') {
                             cutObjString = obj.name.Substring(obj.name.Length-12);
                             objString = cutObjString.Split(char.Parse("-"));
                            
                        } else {
                            cutObjString = obj.name.Substring(obj.name.Length-7);
                            objString = cutObjString.Split(char.Parse("."));
                            
                        }
                        if (otherObj.name[0] == 'w') {
                            cutOtherObjString = otherObj.name.Substring(otherObj.name.Length-12);
                            otherObjString = cutOtherObjString.Split(char.Parse("-"));
                        } else {
                            cutOtherObjString = otherObj.name.Substring(otherObj.name.Length-7);
                            otherObjString = cutOtherObjString.Split(char.Parse("."));
                        }
                
                        

                        int layer1 = 0;
                        int layer2 = 0;
                        int.TryParse(objString[0],out layer1);
                        int.TryParse(otherObjString[0],out layer2);
                    
                        //Debug.Log("obj.name: "+objString[0] + " and otherObj.name: "+otherObjString[0]);
                        if (obj.transform.GetSiblingIndex() > otherObj.transform.GetSiblingIndex() && layer1 < layer2) {
                            obj.transform.SetSiblingIndex(otherObj.transform.GetSiblingIndex() - 1);
                        }
                    }
                }
            }
        }



///////////////////makeshift weapon object holder for animation
        GameObject weaponHolder = new GameObject();
        weaponHolder.name = "weaponHolder";
        weaponHolder.transform.position = this.transform.position;
        weaponHolder.transform.SetParent(this.transform);
        Transform[] objectList2 = this.transform.GetComponentsInChildren<Transform>();
        foreach (Transform obj in objectList2) {
            if (obj.name[0] == 'w') {
                obj.transform.SetParent(weaponHolder.transform);
            }
        }
        
        

    }


    IEnumerator LoadSprite(SpriteRenderer objRenderer, string address) {
        AsyncOperationHandle<Sprite> handle = Addressables.LoadAssetAsync<Sprite>(address);
        yield return handle;
        if(handle.Result != null) {
            objRenderer.sprite = handle.Result;
        }
            

    }

    IEnumerator LoadImage(Image objRenderer, string address) {
        AsyncOperationHandle<Sprite> handle = Addressables.LoadAssetAsync<Sprite>(address);
        yield return handle;
        if(handle.Result != null) {
            objRenderer.sprite = handle.Result;
        }
            

    }

    
}
