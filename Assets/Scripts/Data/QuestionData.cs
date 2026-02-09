using System.Collections.Generic;

[System.Serializable]
public class LevelData
{
    public List<QuestionData> questions;
}

[System.Serializable]
public class QuestionData
{
    public int id;
    public string question;
    public string answer;
}
