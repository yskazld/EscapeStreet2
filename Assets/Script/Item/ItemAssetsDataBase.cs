using System;
using System.Collections.Generic;
using Save;
using UnityEngine;

namespace Item
{
    /// <summary>
    /// アイテム情報を管理
    /// </summary>
    public class ItemAssetsDataBase
    {
        /// <summary>
        /// すべてのアイテム情報
        /// Dictionaryで管理するので重複するIDを登録するとエラーになる
        /// </summary>
        private Dictionary<SaveData.ItemKind,ItemAssetData> _itemAssetData = new Dictionary<SaveData.ItemKind, ItemAssetData>();
        public void Init()
        {
            //ItemDataフォルダに入ったファイルを読む
            var itemDataList = Resources.LoadAll("ItemData") ;
            foreach (var data in itemDataList)
            {
                var itemData = data as ItemAssetData;
                try
                {
                    _itemAssetData.Add(itemData.ItemKindData,itemData);
                }
                catch (Exception e)
                {
                    Debug.LogError("おそらく重複したIDを登録した"+itemData.ItemKindData +" " +e);
                }
            }
        }

        /// <summary>
        /// アイテムの名前を取得する
        /// </summary>
        /// <param name="kind"></param>
        /// <returns></returns>
        public string GetName(SaveData.ItemKind kind)
        {
            return _itemAssetData[kind].Name;
        }

        /// <summary>
        /// このアイテムの合成素材となるアイテムを取得する
        /// </summary>
        /// <param name="kind"></param>
        /// <returns></returns>
        public SaveData.ItemKind GetUnionItem(SaveData.ItemKind kind)
        {
            return _itemAssetData[kind].UnionItemKind;
        }
        
        /// <summary>
        /// 合成結果
        /// </summary>
        /// <param name="kind"></param>
        /// <returns></returns>
        public SaveData.ItemKind GetUnionItemResult(SaveData.ItemKind kind)
        {
            return _itemAssetData[kind].UnionResultsItemKind;
        }

        
    }
}


