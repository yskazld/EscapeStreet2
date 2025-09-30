using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace UI.Title
{
	/// <summary>
	/// タイトルのUI制御　主にmainシーンへの遷移
	/// </summary>
	public class TitleUI : MonoBehaviour
	{
		/// <summary>
		/// スタートボタン
		/// </summary>
		[SerializeField] private Button _gameStartButton;
		/// <summary>
		/// 続きから
		/// </summary>
		[SerializeField] private Button _continueButton;
		
		// Start is called before the first frame update
		void Start()
		{
			//セーブクラス初期化
			var saveManagerInstance = new Save.SaveManager();
			var save = saveManagerInstance;
			//ロード
			save.LoadOrInitializeReturnInitialize();
			//ボタンを押したとき
			_gameStartButton.onClick.AddListener(() =>
			{
				//データ初期化
				save.InitializeSaveData();
				save.Save();
				//mainシーンに遷移
				SceneManager.LoadScene("main");
			});
			//ボタンを押したとき
			_continueButton.onClick.AddListener(() =>
			{
				//mainシーンに遷移
				SceneManager.LoadScene("main");
			});
			//コンティニューはセーブデータがあるときに表示
			_continueButton.gameObject.SetActive(!save.SaveDataInstance.IsFirst);
		}
	}
}
