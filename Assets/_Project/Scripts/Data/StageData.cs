using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "StageData", menuName = "Scriptable Objects/StageData")]
public class StageData : ScriptableObject
{
    [Tooltip("1번 방부터 보스 방까지 순서대로 프리팹을 넣어주세요.")]
    public List<GameObject> roomSequence;
}
