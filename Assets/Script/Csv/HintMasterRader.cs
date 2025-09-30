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
			ReadData();
		}

		/// <summary>
		/// csvデータ読み込み
		/// </summary>
		private void ReadData()
		{
			TextAsset textCsvAsset;
			textCsvAsset = Resources.Load(string.Format("Master/{0}", _fileName)) as TextAsset;
			var csv = ConvertCSV(textCsvAsset.ToString());
			foreach (var columns in csv)
			{
				string[] csvArr = columns;

				//データを設定する
				int index = 0;
				//ヒント文言
				string[] hintMsgs = csvArr[index].Split('|');
				index++;
				//答え文言
				string[] answerMsgs = csvArr[index].Split('|');
				index++;
				//フラグ
				Save.SaveData.SaveFlag flag =
					(Save.SaveData.SaveFlag)Enum.Parse(typeof(Save.SaveData.SaveFlag), csvArr[index]);
				index++;

				//ヒントデータ格納
				HintMasterDataList.Add(new HintMasterData(hintMsgs, answerMsgs, flag));
			}

			textCsvAsset = null;
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

