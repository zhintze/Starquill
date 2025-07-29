// Script for having a typewriter effect for UI
// Prepared by Nick Hwang (https://www.youtube.com/nickhwang)
// Want to get creative? Try a Unicode leading character(https://unicode-table.com/en/blocks/block-elements/)
// Copy Paste from page into Inpector

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TypewriterUI : MonoBehaviour
{


    [HideInInspector] public NPCData npcSpeaker;
    [HideInInspector] public bool isTextDisplayComplete;
    public GameObject HeadTextDisplayPrefab;



	Text _text;
	TMP_Text _tmpProText;
	string writer;

	[SerializeField] float delayBeforeStart = 0f;
	[SerializeField] float timeBtwChars = 0.1f;
	[SerializeField] string leadingChar = "";
	[SerializeField] bool leadingCharBeforeDelay = false;


    [SerializeField] ScrollRect scrollrect;
    [SerializeField] bool skipSpaces;
    [SerializeField] float sentenceDelay = 4;


    

    bool headSpace;
    bool headSpace2;
    bool skipText;

    private Vector2 resolution;
    //string spacingAmt = "             ";

	// Use this for initialization
	public void StartText()
	{

        //npcSpeaker = new NPCData();


        resolution = new Vector2(Screen.width, Screen.height);

		_text = GetComponent<Text>()!;
		_tmpProText = GetComponent<TMP_Text>()!;


        skipText = false;


		if (_tmpProText != null)
		{
            if (npcSpeaker != null) {
                //writer = spacingAmt+_tmpProText.text;

                //make copy so original is not altered
                NPCData npc = npcSpeaker;
                //GameObject NPCObject = transform.GetChild(0).GetChild(0).gameObject;
                if (transform.GetChild(0).GetComponentInChildren<Transform>() != null) {
                    foreach (Transform child in transform.GetChild(0).GetComponentInChildren<Transform>()) {
                        Destroy(child.gameObject);
                    }
                }
                
                GameObject NPCObject = Instantiate(HeadTextDisplayPrefab);
                NPCObject.transform.SetParent(transform.GetChild(0),false);
                
                CharacterObject characterObject = NPCObject.AddComponent<CharacterObject>();
                npc.data.speciesData.xScale = 1;
                npc.data.speciesData.yScale = 1;
                characterObject.CreateCharacterObject(NPCObject,npc.data,true);
                
                //NPCObject.transform.eulerAngles = new Vector3(0,0,0);
                NPCObject.transform.localScale = new Vector3(3f,3f,3f);

                transform.GetChild(1).GetComponent<TMP_Text>().text = npc.name;


            } else {
                transform.GetChild(0).gameObject.SetActive(false);
                
            }
            writer = _tmpProText.text;

			_tmpProText.text = "";

			StartCoroutine("TypeWriterTMP");
		}

        /*if(_text != null)
        {
			writer = _text.text;
			_text.text = "";

			StartCoroutine("TypeWriterText");
		}*/
	}


	




	IEnumerator TypeWriterTMP()
    {
        _tmpProText.text = leadingCharBeforeDelay ? leadingChar : "";

        yield return new WaitForSeconds(delayBeforeStart);

		/*foreach (char c in writer)
		{

            
			if (_tmpProText.text.Length > 0)
			{
				_tmpProText.text = _tmpProText.text.Substring(0, _tmpProText.text.Length - leadingChar.Length);
			}


           


			_tmpProText.text += c;
			_tmpProText.text += leadingChar;


            //-- Spacing for NPC Head Image --//
            if (npcSpeaker != null) {
                if (_tmpProText.textInfo.lineCount == 2 && headSpace == false) {
                    headSpace = true;
                    string newText = _tmpProText.text.Substring(0, _tmpProText.textInfo.lineInfo[1].firstCharacterIndex-1);
                    //Debug.Log(_tmpProText.text[_tmpProText.textInfo.lineInfo[1].firstCharacterIndex]);
                    _tmpProText.text = newText+"\n"+spacingAmt+_tmpProText.text.Substring(_tmpProText.textInfo.lineInfo[1].firstCharacterIndex, _tmpProText.text.Length - _tmpProText.textInfo.lineInfo[1].firstCharacterIndex);
                    //_tmpProText.text = _tmpProText.text + "     ";
                    //Debug.Log("got it: "+_tmpProText.textInfo.lineCount);
                }

                if (_tmpProText.textInfo.lineCount == 3 && headSpace2 == false) {
                    headSpace2 = true;
                    string newText = _tmpProText.text.Substring(0, _tmpProText.textInfo.lineInfo[2].firstCharacterIndex-1);
                    //Debug.Log(_tmpProText.text[_tmpProText.textInfo.lineInfo[2].firstCharacterIndex]);
                    _tmpProText.text = newText+"\n"+spacingAmt+_tmpProText.text.Substring(_tmpProText.textInfo.lineInfo[2].firstCharacterIndex, _tmpProText.text.Length - _tmpProText.textInfo.lineInfo[2].firstCharacterIndex);
                    //_tmpProText.text = _tmpProText.text + "     ";
                    //Debug.Log("got it: "+_tmpProText.textInfo.lineCount);
                }
            }
            

            //-- IMMEDIATELY SKIP TYPEWRITER ANIMATION --//
            if (skipText == true ) {
                _tmpProText.text = writer;

                Canvas.ForceUpdateCanvases();

                //-- Spacing for NPC Head Image --//
                if (npcSpeaker != null) {
                    if (_tmpProText.textInfo.lineCount >= 2) {
                        string newText = _tmpProText.text.Substring(0, _tmpProText.textInfo.lineInfo[1].firstCharacterIndex-1);
                        _tmpProText.text = newText+"\n"+spacingAmt+_tmpProText.text.Substring(_tmpProText.textInfo.lineInfo[1].firstCharacterIndex, _tmpProText.text.Length - _tmpProText.textInfo.lineInfo[1].firstCharacterIndex);
                    }

                    if (_tmpProText.textInfo.lineCount >= 3) {
                        string newText = _tmpProText.text.Substring(0, _tmpProText.textInfo.lineInfo[2].firstCharacterIndex-1);
                        _tmpProText.text = newText+"\n"+spacingAmt+_tmpProText.text.Substring(_tmpProText.textInfo.lineInfo[2].firstCharacterIndex, _tmpProText.text.Length - _tmpProText.textInfo.lineInfo[2].firstCharacterIndex);
                    }
                }

                
                //support for scrollrect version
                if (scrollrect != null) {
                    yield return new WaitForSeconds(0.001f);
                    scrollrect.verticalScrollbar.value=0f;
                    Canvas.ForceUpdateCanvases();
                }

                isTextDisplayComplete = true;
                break;
            }
            //-- END SKIP TYPEWRITER ANIMATION --//


            //support for scrollrect version
            if (scrollrect != null) {  
                scrollrect.verticalScrollbar.value=0f;
                Canvas.ForceUpdateCanvases();
            }



            if (skipSpaces == true && c == ' ') {
                yield return null;
            } else if (c == '.') {
                yield return new WaitForSeconds(timeBtwChars*sentenceDelay);
            } else {
                yield return new WaitForSeconds(timeBtwChars);
            }
			
		}*/




        for (int c = 0; c < writer.Length; c++)
		{

            
			if (_tmpProText.text.Length > 0)
			{
				_tmpProText.text = _tmpProText.text.Substring(0, _tmpProText.text.Length - leadingChar.Length);
			}


            if (writer[c] == '<') {
                for (int i = c; i < writer.Length; i++) {
                    _tmpProText.text += writer[i];
                    if (writer[i] == '>') {
                        c = i;
                        break;
                    }
                }
                continue;
            }


			_tmpProText.text += writer[c];
			_tmpProText.text += leadingChar;


            //-- Spacing for NPC Head Image --//
            /*if (npcSpeaker != null) {
                if (_tmpProText.textInfo.lineCount == 2 && headSpace == false) {
                    headSpace = true;
                    string newText = _tmpProText.text.Substring(0, _tmpProText.textInfo.lineInfo[1].firstCharacterIndex-1);
                    //Debug.Log(_tmpProText.text[_tmpProText.textInfo.lineInfo[1].firstCharacterIndex]);
                    _tmpProText.text = newText+"\n"+spacingAmt+_tmpProText.text.Substring(_tmpProText.textInfo.lineInfo[1].firstCharacterIndex, _tmpProText.text.Length - _tmpProText.textInfo.lineInfo[1].firstCharacterIndex);
                    //_tmpProText.text = _tmpProText.text + "     ";
                    //Debug.Log("got it: "+_tmpProText.textInfo.lineCount);
                }

                if (_tmpProText.textInfo.lineCount == 3 && headSpace2 == false) {
                    headSpace2 = true;
                    string newText = _tmpProText.text.Substring(0, _tmpProText.textInfo.lineInfo[2].firstCharacterIndex-1);
                    //Debug.Log(_tmpProText.text[_tmpProText.textInfo.lineInfo[2].firstCharacterIndex]);
                    _tmpProText.text = newText+"\n"+spacingAmt+_tmpProText.text.Substring(_tmpProText.textInfo.lineInfo[2].firstCharacterIndex, _tmpProText.text.Length - _tmpProText.textInfo.lineInfo[2].firstCharacterIndex);
                    //_tmpProText.text = _tmpProText.text + "     ";
                    //Debug.Log("got it: "+_tmpProText.textInfo.lineCount);
                }
            }*/
            

            //-- IMMEDIATELY SKIP TYPEWRITER ANIMATION --//
            if (skipText == true ) {
                _tmpProText.text = writer;

                Canvas.ForceUpdateCanvases();

                //-- Spacing for NPC Head Image --//
                /*if (npcSpeaker != null) {
                    if (_tmpProText.textInfo.lineCount >= 2) {
                        string newText = _tmpProText.text.Substring(0, _tmpProText.textInfo.lineInfo[1].firstCharacterIndex-1);
                        _tmpProText.text = newText+"\n"+spacingAmt+_tmpProText.text.Substring(_tmpProText.textInfo.lineInfo[1].firstCharacterIndex, _tmpProText.text.Length - _tmpProText.textInfo.lineInfo[1].firstCharacterIndex);
                    }

                    

                    if (_tmpProText.textInfo.lineCount >= 3) {
                        string newText = _tmpProText.text.Substring(0, _tmpProText.textInfo.lineInfo[2].firstCharacterIndex-1);
                        _tmpProText.text = newText+"\n"+spacingAmt+_tmpProText.text.Substring(_tmpProText.textInfo.lineInfo[2].firstCharacterIndex, _tmpProText.text.Length - _tmpProText.textInfo.lineInfo[2].firstCharacterIndex);
                    }
                }*/


                
                //support for scrollrect version
                if (scrollrect != null) {
                    yield return new WaitForSeconds(0.001f);
                    scrollrect.verticalScrollbar.value=0f;
                    Canvas.ForceUpdateCanvases();
                }

                isTextDisplayComplete = true;
                break;
            }
            //-- END SKIP TYPEWRITER ANIMATION --//


            //support for scrollrect version
            if (scrollrect != null) {  
                scrollrect.verticalScrollbar.value=0f;
                Canvas.ForceUpdateCanvases();
            }



            if (skipSpaces == true && writer[c] == ' ') {
                yield return null;
            } else if (writer[c] == '.') {
                yield return new WaitForSeconds(timeBtwChars*sentenceDelay);
            } else {
                yield return new WaitForSeconds(timeBtwChars);
            }
			
		}






		if (leadingChar != "")
		{
			_tmpProText.text = _tmpProText.text.Substring(0, _tmpProText.text.Length - leadingChar.Length);
		}

        
        isTextDisplayComplete = true;
	}

    void Update () {
        if (Input.GetKeyDown("space") && skipText == false) {
            skipText = true;
        }
    }


    /*IEnumerator TypeWriterText()
	{
		_text.text = leadingCharBeforeDelay ? leadingChar : "";

		yield return new WaitForSeconds(delayBeforeStart);

		foreach (char c in writer)
		{
			if (_text.text.Length > 0)
			{
				_text.text = _text.text.Substring(0, _text.text.Length - leadingChar.Length);
			}
			_text.text += c;
			_text.text += leadingChar;
			yield return new WaitForSeconds(timeBtwChars);
		}

		if(leadingChar != "")
        {
			_text.text = _text.text.Substring(0, _text.text.Length - leadingChar.Length);
		}
	}*/


    /*private void Update ()
    {
     if (resolution.x != Screen.width || resolution.y != Screen.height)
     {
         // do your stuff
 
         resolution.x = Screen.width;
         resolution.y = Screen.height;
         Debug.Log("heck");

         if (_tmpProText.textInfo.lineCount >= 2) {
                
                string newText = _tmpProText.text.Substring(0, _tmpProText.textInfo.lineInfo[1].firstCharacterIndex-1);
                
                Debug.Log(_tmpProText.text[_tmpProText.textInfo.lineInfo[1].firstCharacterIndex]);

                _tmpProText.text = newText+"\n      "+_tmpProText.text.Substring(_tmpProText.textInfo.lineInfo[1].firstCharacterIndex, _tmpProText.text.Length - _tmpProText.textInfo.lineInfo[1].firstCharacterIndex);
                
                //_tmpProText.text = _tmpProText.text + "     ";
                //Debug.Log("got it: "+_tmpProText.textInfo.lineCount);
            }
     }
    }*/


    
}