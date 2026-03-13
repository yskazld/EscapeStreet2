using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Csv
{
	/// <summary>
	/// ヒントのcsvを読み込む
	/// </summary>
	public class HintMasterReader
	{
		private string _fileName = "";

		public List<HintMasterData> HintMasterDataList { get; private set; } = new List<HintMasterData>();

		private enum CSVParserState
		{
			BEGIN_FIELD,
			PLAIN,
			QUOTE,
			END_FIELD,
		};

		public HintMasterReader(string fileName)
		{
			_fileName = fileName;
		}

		/// <summary>
		/// csvデータ読み込み
		/// </summary>
		public void Load()
		{
			HintMasterDataList.Clear();
			var textCsvAsset = Resources.Load<TextAsset>(string.Format("Master/{0}", _fileName));
			if (textCsvAsset == null)
			{
				Debug.LogError($"[HintMasterReader] Resources/Master/{_fileName} が見つかりません。");
				return;
			}
			var csv = ConvertCSV(textCsvAsset.text);
			foreach (var columns in csv)
			{
				string[] csvArr = columns;
				if (csvArr == null || csvArr.Length < 3)
				{
					Debug.LogWarning($"[HintMasterReader] CSVの列数が不足しています。Expected>=3, Actual={csvArr?.Length ?? 0}");
					continue;
				}

				//データを設定する
				int index = 0;
				//ヒント文言
				string[] hintMsgs = csvArr[index].Split('|');
				index++;
				//答え文言
				string[] answerMsgs = csvArr[index].Split('|');
				index++;
				//フラグ
				var flagText = csvArr[index]?.Trim();
				if (string.IsNullOrEmpty(flagText))
				{
					Debug.LogWarning("[HintMasterReader] CSVのフラグ列が空です。行をスキップします。");
					continue;
				}
				if (!Enum.TryParse(flagText, out Save.SaveData.SaveFlag flag))
				{
					Debug.LogWarning($"[HintMasterReader] 不正なフラグ名: {flagText}. 行をスキップします。");
					continue;
				}
				index++;

				//ヒントデータ格納
				HintMasterDataList.Add(new HintMasterData(hintMsgs, answerMsgs, flag));
			}

		}

		/// <summary>
		/// csvを文字列に変換
		/// </summary>
		/// <param name="csvText"></param>
		/// <returns></returns>
		private List<string[]> ConvertCSV(string csvText)
		{
			StringReader reader = new StringReader(csvText);
			List<string[]> csvDatas = new List<string[]>();
			bool isTop = false;
			// reader.Peaekが-1になるまで
			while (reader.Peek() != -1)
			{
				// 一行ずつ読み込み
				string line = reader.ReadLine();
				//先頭は飛ばす
				if (!isTop)
				{
					isTop = true;
					continue;
				} 
				// , 区切りでリストに追加
				csvDatas.Add(line.Split(',')); 
			}
			return csvDatas;
		}
	}
}

