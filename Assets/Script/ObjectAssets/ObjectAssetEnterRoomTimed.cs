using ObjectAssets;
using UnityEngine;

/// <summary>
/// 遅延・移動時間を指定して入室させるオブジェクト
/// </summary>
[CreateAssetMenu(fileName = "EnterRoomTimed.asset", menuName = "Escape/Object/EnterRoomTimed")]
[System.Serializable]
public class ObjectAssetEnterRoomTimed : ObjectAssetBase
{
    /// <summary>
    /// 移動先の部屋ID
    /// </summary>
    public int RoomID;

    /// <summary>
    /// 実行前に待つ秒数
    /// </summary>
    public float DelaySeconds = 1f;

    /// <summary>
    /// 移動にかける秒数
    /// </summary>
    public float MoveSeconds = 1f;
}
