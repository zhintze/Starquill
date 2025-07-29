using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SkillData
{
    public int amount;
    public int positionInArray;
    public int governingAttribute;
    public string name;
    public int[] relationshipArray;
    


    public SkillData(int _positionInArray,string _name, int _governingAttribute, int[] _relationshipArray) {
        name = _name;
        positionInArray = _positionInArray;
        relationshipArray = _relationshipArray;
        governingAttribute = _governingAttribute;
    }
}
