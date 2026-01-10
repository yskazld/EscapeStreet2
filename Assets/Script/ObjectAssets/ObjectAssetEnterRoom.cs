using ObjectAssets;
using UnityEngine;

/// <summary>
/// 入室をさせるオブジェクト
/// </summary>
[CreateAssetMenu(fileName = "入室.asset", menuName = "Escape/Object/EnterRoom")]
[System.Serializable]
public class ObjectAssetEnterRoom : ObjectAssetBase
{
	public int RoomID;
}
