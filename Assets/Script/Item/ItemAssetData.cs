using Save;
using UnityEngine;

namespace Item
{
    [System.Serializable]
    [CreateAssetMenu(fileName = "アイテム情報.asset", menuName = "Escape/ItemData")]
    //アイテムの情報を記載
    public class ItemAssetData  : ScriptableObject
    {
        /// <summary>
        /// このアイテムの種類を設定
        /// </summary>
        public SaveData.ItemKind ItemKindData;
        /// <summary>
        /// このアイテムの名前
        /// </summary>
        public string Name;
        
        /// <summary>
        /// 合成できるアイテム
        /// </summary>
        public SaveData.ItemKind UnionItemKind;
        
        /// <summary>
        /// 合成結果のアイテム
        /// </summary>
        public SaveData.ItemKind UnionResultsItemKind;

    }
}
