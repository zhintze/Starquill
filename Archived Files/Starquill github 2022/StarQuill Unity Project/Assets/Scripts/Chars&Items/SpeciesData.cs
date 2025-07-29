using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpeciesData
{

    public string name;
    public float xScale;
    public float yScale;
    public string backArm;
	public string legs;
	public string body;
	public string head;
	public string frontArm;
    public string eyes;
    public string nose;
    public string mouth;
    public string ears;
    public string facialHair;
    public string faceDetail;
    public string hair;
    public string[] otherBodyPartsArray;
    public string[] skinColorArray;
    public string[] itemRestrictionsArray;
    public List<List<string>> skinVarianceArray;
    


    public SpeciesData(string _Name, float x, float y, string _BackArm, string _Legs, string _Body, string _Head, string _FrontArm, string _Eyes, 
                            string _Nose, string _Mouth, string _Ears, string _FacialHair, string _FaceDetail, string _Hair, string[] _OtherParts, string[] _SkinColors, string[] _ItemRestrictions, List<List<string>> _skinVarianceArray) {

        name = _Name;
        xScale = x;
        yScale = y;
        backArm = _BackArm;
		legs = _Legs;
		body = _Body;
		head = _Head;
		frontArm = _FrontArm;
        eyes = _Eyes;
        nose = _Nose;
        mouth = _Mouth;
        ears = _Ears;
        facialHair = _FacialHair;
        hair = _Hair;
        faceDetail = _FaceDetail;
        otherBodyPartsArray = _OtherParts;
        skinColorArray = _SkinColors;
        itemRestrictionsArray = _ItemRestrictions;
        skinVarianceArray = _skinVarianceArray;

    }




    

    
}
