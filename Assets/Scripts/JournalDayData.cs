using UnityEngine;

[CreateAssetMenu(fileName = "JournalDay01", menuName = "Shortwaves/Journal Day Data")]
public class JournalDayData : ScriptableObject
{
    public int DayNumber = 1;

    [TextArea(4, 10)]
    public string PreAnomalyThoughts = "";

    [TextArea(4, 10)]
    public string PostAnomalyThoughts = "";
}
