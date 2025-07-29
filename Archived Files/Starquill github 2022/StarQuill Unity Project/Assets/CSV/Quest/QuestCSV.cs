using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class QuestCSV
{
    /*
[]
greeting
supplication
intro
outro
promise of reward
engagement
conclusion
get reward
reference
quest item purpose
plot
threat
action

()
reward
location

quest item
npc neutral
npc hostile
npc friendly
quest giver

{}
trigger
passive
interaction
    */

    public List<string> introNPCTalk;
    public List<string> introNPCForced;
    public List<string> introNarratedObservation;
    public List<string> introNarratedSerendipitous;

    public List<string> ReferenceLocationTrigger;
    public List<string> ReferenceLocationPassive;
    public List<string> ReferenceNPCTrigger;
    public List<string> ReferenceNPCPassive;

    //first row 
    public List<List<string>> Plot;
    public List<List<string>> Engagement;
    public List<List<string>> Conclusion;
    public List<List<string>> Action;

    public List<string> Outro;
    public List<string> Greeting;
    public List<string> Threat;
    public List<string> GetReward;
    public List<string> QuestItemPurpose;
    public List<string> Supplication;
    public List<string> QuestItem;
    public List<string> PromiseOfReward;


    public QuestCSV(TextAsset IntroCSV, TextAsset OutroCSV, TextAsset PromiseOfRewardCSV, TextAsset EngagementCSV, TextAsset ConclusionCSV, 
     TextAsset GetRewardCSV,  TextAsset ReferenceCSV,  TextAsset QuestItemPurposeCSV, TextAsset QuestItemCSV, TextAsset ActionCSV,  TextAsset PlotCSV,  TextAsset ThreatCSV, TextAsset GreetingCSV, TextAsset SupplicationCSV)
    {

        //_--- INTRO processing ----//
        string[] IntroArrayWithHeader = IntroCSV.text.Split(char.Parse("\n"));
        //remove header
        string[] IntroArrayNoHeader = new string[IntroArrayWithHeader.Length-1];
        for (int i = 1; i < IntroArrayWithHeader.Length; i++) {IntroArrayNoHeader[i-1] = IntroArrayWithHeader[i];}

        introNPCTalk = new List<string>();
        introNPCForced = new List<string>();
        introNarratedObservation = new List<string>();
        introNarratedSerendipitous = new List<string>();

        foreach(string row in IntroArrayNoHeader) {
            string[] column = row.Split(char.Parse(","));
            if (column[0] != "") {
                string newText = column[0].Replace('_',',');
                introNPCTalk.Add(newText);
            }
            if (column[1] != "") {
                string newText = column[1].Replace('_',',');
                introNPCForced.Add(newText);
            }
            if (column[2] != "") {
                string newText = column[2].Replace('_',',');
                introNarratedObservation.Add(newText);
            }
            if (column[3] != "") {
                string newText = column[3].Replace('_',',');
                introNarratedSerendipitous.Add(newText);
            }
            
        }
        //---- INTRO processing ----//


        //---- REFERENCE processing ----//
        string[] ReferenceArrayWithHeader = ReferenceCSV.text.Split(char.Parse("\n"));
        //remove header
        string[] ReferenceArrayNoHeader = new string[ReferenceArrayWithHeader.Length-1];
        for (int i = 1; i < ReferenceArrayWithHeader.Length; i++) {ReferenceArrayNoHeader[i-1] = ReferenceArrayWithHeader[i];}

        ReferenceLocationTrigger = new List<string>();
        ReferenceLocationPassive = new List<string>();
        ReferenceNPCTrigger = new List<string>();
        ReferenceNPCPassive = new List<string>();

        foreach(string row in ReferenceArrayNoHeader) {
            string[] column = row.Split(char.Parse(","));
            
            if (column[0] != "") {
                string newText = column[0].Replace('_',',');
                ReferenceLocationTrigger.Add(newText);
            }
            if (column[1] != "") {
                string newText = column[1].Replace('_',',');
                ReferenceLocationPassive.Add(newText);
            }
            if (column[2] != "") {
                string newText = column[2].Replace('_',',');
                ReferenceNPCTrigger.Add(newText);
            }
            if (column[3] != "") {
                string newText = column[3].Replace('_',',');
                ReferenceNPCPassive.Add(newText);
            }
            
        }
        //---- REFERENCE processing ----//




        

        



        //---- PLOT processing ----//
        //first row is DS36 description to match
        string[] PlotCSVArray = PlotCSV.text.Split(char.Parse("\n"));

        Plot = new List<List<string>>();

        foreach(string row in PlotCSVArray) {
            string[] column = row.Split(char.Parse(","));
            List<string> subPlot = new List<string>();
            foreach (string text in column) {
                string newText = text.Replace('_',',');
                subPlot.Add(newText);
            }
            Plot.Add(subPlot);
            
        }
        //---- PLOT processing ----//

        //---- ENGAGEMENT processing ----//
        //first row is DS36 description to match
        string[] EngagementCSVArray = EngagementCSV.text.Split(char.Parse("\n"));

        Engagement = new List<List<string>>();

        foreach(string row in EngagementCSVArray) {
            string[] column = row.Split(char.Parse(","));
            List<string> subEngagement = new List<string>();
            foreach (string text in column) {
                string newText = text.Replace('_',',');
                subEngagement.Add(newText);
            }
            Engagement.Add(subEngagement);
            
        }
        //---- ENGAGEMENT processing ----//

        //---- CONCLUSION processing ----//
        //first row is DS36 description to match
        string[] ConclusionCSVArray = ConclusionCSV.text.Split(char.Parse("\n"));

        Conclusion = new List<List<string>>();

        foreach(string row in ConclusionCSVArray) {
            string[] column = row.Split(char.Parse(","));
            List<string> subConclusion = new List<string>();
            foreach (string text in column) {
                string newText = text.Replace('_',',');
                subConclusion.Add(newText);
            }
            Conclusion.Add(subConclusion);
            
        }
        //---- CONCLUSION processing ----//

        //---- ACTION processing ----//
        //first row is DS36 description to match
        string[] ActionCSVArray = ActionCSV.text.Split(char.Parse("\n"));

        Action = new List<List<string>>();

        foreach(string row in ActionCSVArray) {
            string[] column = row.Split(char.Parse(","));
            List<string> subAction = new List<string>();
            foreach (string text in column) {
                string newText = text.Replace('_',',');
                subAction.Add(newText);
            }
            Action.Add(subAction);
            
        }
        //---- ACTION processing ----//






        //---- OUTRO processing ----//
        string[] OutroArray = OutroCSV.text.Split(char.Parse("\n"));
        Outro = new List<string>();

        foreach (string text in OutroArray) {
            string newText = text.Replace('_',',');
            Outro.Add(newText);
        }
        //---- OUTRO processing ----//


        //---- GET REWARD processing ----//
        string[] GetRewardArray = GetRewardCSV.text.Split(char.Parse("\n"));
        GetReward = new List<string>();

        foreach (string text in GetRewardArray) {
            string newText = text.Replace('_',',');
            GetReward.Add(newText);
        }
        //---- GET REWARD processing ----//

        //---- QUEST ITEM PURPOSE processing ----//
        string[] QuestItemPurposeArray = QuestItemPurposeCSV.text.Split(char.Parse("\n"));
        QuestItemPurpose = new List<string>();

        foreach (string text in QuestItemPurposeArray) {
            string newText = text.Replace('_',',');
            QuestItemPurpose.Add(newText);
        }
        //---- QUEST ITEM PURPOSE processing ----//

        //---- GREETING processing ----//
        string[] GreetingArray = GreetingCSV.text.Split(char.Parse("\n"));
        Greeting = new List<string>();

        foreach (string text in GreetingArray) {
            string newText = text.Replace('_',',');
            Greeting.Add(newText);
        }
        //---- GREETING processing ----//

        //---- THREAT processing ----//
        string[] ThreatArray = ThreatCSV.text.Split(char.Parse("\n"));
        Threat = new List<string>();

        foreach (string text in ThreatArray) {
            string newText = text.Replace('_',',');
            Threat.Add(newText);
        }
        //---- THREAT processing ----//


        //---- SUPPLICATION processing ----//
        string[] SupplicationArray = SupplicationCSV.text.Split(char.Parse("\n"));
        Supplication = new List<string>();

        foreach (string text in SupplicationArray) {
            string newText = text.Replace('_',',');
            Supplication.Add(newText);
        }
        //---- SUPPLICATION processing ----//


        //---- QuestItem processing ----//
        string[] QuestItemArray = QuestItemCSV.text.Split(char.Parse("\n"));
        QuestItem = new List<string>();

        foreach (string text in QuestItemArray) {
            string newText = text.Replace('_',',');
            QuestItem.Add(newText);
        }
        //---- QuestItem processing ----//



        //---- PROMISE OF REWARD processing ----//
        string[] PromiseOfRewardArray = PromiseOfRewardCSV.text.Split(char.Parse("\n"));
        PromiseOfReward = new List<string>();

        foreach (string text in PromiseOfRewardArray) {
            string newText = text.Replace('_',',');
            PromiseOfReward.Add(newText);
        }
        //---- PROMISE OF REWARD processing ----//

    }






    
}
