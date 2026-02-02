using UnityEngine;

[System.Serializable]
public class WordData
{
   [SerializeField] private string questionData;
   [SerializeField] private string answerData;

   public string Question => questionData;
   public string Answer => answerData;
}
