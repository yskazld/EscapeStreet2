using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace UI.Debug
{
	/// <summary>
	/// デバック用
	/// 現状は言語切り替えボタンのみを入れています。
	/// </summary>
	public class DebugUI : MonoBehaviour
	{
		[SerializeField] private Button _saveDeleteButton;
		[SerializeField] private Button _languageChangeButton;
		[SerializeField] private TextMeshProUGUI _languageText;

		void Start()
		{
			_saveDeleteButton.onClick.AddListener(() =>
			{
				var save = GameManager.GetInstance().SaveManagerInstance;
				save.InitializeSaveData();
				save.Save();
				//mainシーンに遷移
				SceneManager.LoadScene("Main");
			});
			//言語切り替え
			_languageChangeButton.onClick.AddListener(() =>
			{
				var save = GameManager.GetInstance().SaveManagerInstance;
				var nowLanguage = save.SaveDataInstance.GetLanguage();
				if(nowLanguage == Save.SaveData.LANGUAGE.JAPAN)
				{
					nowLanguage = Save.SaveData.LANGUAGE.ENGLISH;
				}
				else
				if (nowLanguage == Save.SaveData.LANGUAGE.ENGLISH)
				{
					nowLanguage = Save.SaveData.LANGUAGE.JAPAN;
				}
				save.SaveDataInstance.SetLanguage(nowLanguage);
				UpdateLanguageText();
			});
			UpdateLanguageText();
		}

		/// <summary>
		/// テキスト更新
		/// </summary>
		private void UpdateLanguageText()
		{
			var save = GameManager.GetInstance().SaveManagerInstance;
			var nowLanguage = save.SaveDataInstance.GetLanguage();
			_languageText.text = nowLanguage.ToString();
		}
	}
}