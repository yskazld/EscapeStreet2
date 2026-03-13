using System.Collections;
using System.Collections.Generic;
using Save;
using Stage;
using UI.Dialog;
using UI.Room;
using UnityEngine;
using UnityEngine.UI;

namespace Story
{
	/// <summary>
	/// タイトルから開始したときの導入 + チュートリアルを流す
	/// </summary>
	public class IntroSequenceController : MonoBehaviour
	{
		[SerializeField] private int _roomIdStreetNormal = 40;
		[SerializeField] private int _roomIdStreetFog = 50;
		[SerializeField] private int _roomIdSign = 60;
		[SerializeField] private float _roomIdSignHoldSeconds = 3f;
		[SerializeField] private int _roomIdBookshopEntrance = 70;
		[SerializeField] private int _roomIdBookshop = 0;
		[SerializeField] private int _stage1ClearDialogRoomId = 1;
		[SerializeField] private int _key4TutorialRoomId = 321;

		[SerializeField] private int _buttonPromptRoomId = 1;
		[SerializeField] private List<int> _zoomRoomIds = new List<int> { 1, 1011, 201, 202, 203 };

		[SerializeField] private Image _smokeOverlay;
		[SerializeField] [Range(0f, 1f)] private float _smokeAlpha = 0.35f;
		[SerializeField] private float _smokeFadeInSeconds = 0.6f;
		[SerializeField] private float _smokeHoldSeconds = 0.6f;
		[SerializeField] private float _smokeFadeOutSeconds = 0.6f;

		[SerializeField] private Image _fadeOverlay;
		[SerializeField] private float _fadeToWhiteSeconds = 0.8f;
		[SerializeField] private float _fadeWhiteHoldSeconds = 0.4f;
		[SerializeField] private float _fadeToBlackSeconds = 0.2f;
		[SerializeField] private float _fadeBlackHoldSeconds = 0.5f;
		[SerializeField] private float _fadeOutSeconds = 0.8f;

		private StageManager _stageManager;
		private RoomUI _roomUI;
		private DialogManager _dialogManager;
		private SaveManager _saveManager;

		private bool _introRunning;
		private bool _waitingForMove;
		private bool _waitingForZoom;
		private bool _waitingForButtonPrompt;
		private bool _hasVisitedZoomRoom;
		private bool _stageClearMessageShown;
		private bool _key4TutorialShown;
		private bool _key4TutorialRunning;
		private bool _hadKey4;
		private bool _key4StateInitialized;

		private void OnEnable()
		{
			var gameManager = GameManager.GetInstance();
			if (gameManager != null)
			{
				_stageManager = gameManager.StageManagerInstance;
				_roomUI = FindObjectOfType<RoomUI>();
				_dialogManager = DialogManager.GetInstance();
				_saveManager = gameManager.SaveManagerInstance;
			}

			if (_stageManager != null)
			{
				_stageManager.OnEnterRoom += HandleEnterRoom;
				_stageManager.OnUpdateFlag += HandleUpdateFlag;
				_stageManager.OnUpdateItem += HandleUpdateItem;
			}
		}

		private void OnDisable()
		{
			if (_stageManager != null)
			{
				_stageManager.OnEnterRoom -= HandleEnterRoom;
				_stageManager.OnUpdateFlag -= HandleUpdateFlag;
				_stageManager.OnUpdateItem -= HandleUpdateItem;
			}
		}

		private void Start()
		{
			StartCoroutine(RunWhenReady());
		}

		private IEnumerator RunWhenReady()
		{
			while (GameManager.GetInstance() == null)
			{
				yield return null;
			}

			var gameManager = GameManager.GetInstance();
			_stageManager = gameManager.StageManagerInstance;
			_roomUI = FindObjectOfType<RoomUI>();
			while (DialogManager.GetInstance() == null)
			{
				yield return null;
			}
			_dialogManager = DialogManager.GetInstance();
			_saveManager = gameManager.SaveManagerInstance;

			if (_stageManager != null)
			{
				_stageManager.OnEnterRoom -= HandleEnterRoom;
				_stageManager.OnUpdateFlag -= HandleUpdateFlag;
				_stageManager.OnUpdateItem -= HandleUpdateItem;
				_stageManager.OnEnterRoom += HandleEnterRoom;
				_stageManager.OnUpdateFlag += HandleUpdateFlag;
				_stageManager.OnUpdateItem += HandleUpdateItem;
			}

			if (_saveManager == null)
			{
				yield break;
			}

			InitializeKey4TutorialState();

			if (_saveManager.SaveDataInstance.GetFlagNum(SaveData.SaveFlag.INTRO_SEQUENCE_DONE) > 0)
			{
				yield return ShowReturningDialogIfNeeded();
				yield break;
			}
			yield return RunIntroSequence();
		}

		private IEnumerator ShowReturningDialogIfNeeded()
		{
			if (_saveManager == null)
			{
				yield break;
			}

			var saveData = _saveManager.SaveDataInstance;
			if (saveData != null)
			{
				if (saveData.GetFlagNum(SaveData.SaveFlag.STAGE_13_CLEAR) > 0 ||
					saveData.GetFlagNum(SaveData.SaveFlag.ENDING_SEQUENCE_DONE) > 0)
				{
					yield break;
				}
			}

			SetInputEnabled(false);
			yield return ShowDialogAndWait("よし戻ってきた！！\nこれからまた謎解き開始だ！\n早く家に帰らないと！！");
			SetInputEnabled(true);
		}

		private IEnumerator RunIntroSequence()
		{
			_introRunning = true;
			SetInputEnabled(false);

			_stageManager.EnterRoom(_roomIdStreetNormal);
			yield return ShowDialogSequence(
                "おかあさんに頼まれて、\n商店街へ買い物に来た。",
                "いつも通りの、見慣れた\n景色のはずだったけど、\nなにか変だ！",
                "いや、気にしても\n仕方がない！", 
                "今日はサッカーの日本代表の試合があるんだ。",
                "夜までには、絶対帰らないと！！！！"
            );

			yield return PlaySmoke();
			yield return ShowDialogAndWait("……あれ？なんだか、霧が出てきた？");


            yield return PlayWhiteToBlackFade();
            _stageManager.EnterRoom(_roomIdStreetFog);
            yield return ShowDialogAndWait("……ここは……？さっきまでと、雰囲気が違う……");


			yield return ShowDialogAndWait("霧が濃くて、遠くがよく見えない……");

			_stageManager.EnterRoom(_roomIdSign);
			if (_roomIdSignHoldSeconds > 0f)
			{
				yield return new WaitForSeconds(_roomIdSignHoldSeconds);
			}
			yield return ShowDialogAndWait("……こんな看板、あったっけ？");
			yield return ShowDialogSequence(
				"看板の文字：",
				"「脱出したければ、各店の謎を解いてみろ！！」"
			);
			yield return ShowDialogAndWait("脱出……？冗談、だよね……？", 2f);
			yield return ShowDialogAndWait("……でも、\n戻り道が見えない。\n仕方ない、\n進むしかなさそうだ。");

			_stageManager.EnterRoom(_roomIdBookshopEntrance);
			yield return ShowDialogAndWait("本屋だけ、扉が開いてる……\nとりあえず、入ってみよう。");

			_stageManager.EnterRoom(_roomIdBookshop);
			yield return ShowDialogAndWait("……中は普通の本屋みたいだ。");
			yield return ShowDialogAndWait("画面の端に矢印があるな。\n押すと、部屋を移動できそうだ。");

			_saveManager.SaveDataInstance.SetFlagNum(SaveData.SaveFlag.INTRO_SEQUENCE_DONE, 1);
			_saveManager.Save();

			_introRunning = false;
			_waitingForMove = true;
			SetInputEnabled(true);
		}

		private void HandleEnterRoom(int roomId)
		{
			if (_introRunning)
			{
				return;
			}

			if (_waitingForMove && roomId != _roomIdBookshop)
			{
				_waitingForMove = false;
				StartCoroutine(ShowMoveTutorial());
				return;
			}

			if (_waitingForZoom && _zoomRoomIds.Contains(roomId))
			{
				_waitingForZoom = false;
				_hasVisitedZoomRoom = true;
				StartCoroutine(ShowZoomTutorial(roomId));
				return;
			}

			if (_waitingForButtonPrompt && _hasVisitedZoomRoom && roomId == _buttonPromptRoomId)
			{
				_waitingForButtonPrompt = false;
				StartCoroutine(ShowButtonTutorial());
			}
		}

		private void HandleUpdateFlag()
		{
			if (_stageClearMessageShown || _saveManager == null)
			{
				return;
			}

			if (_saveManager.SaveDataInstance.GetFlagNum(SaveData.SaveFlag.STAGE_1_CLEAR) > 0)
			{
				var currentRoom = _saveManager.SaveDataInstance.GetNowRoom();
				if (currentRoom != _stage1ClearDialogRoomId)
				{
					return;
				}
				_stageClearMessageShown = true;
				StartCoroutine(ShowStageClearDialog());
			}
		}

		private void HandleUpdateItem()
		{
			if (_key4TutorialRunning || _saveManager == null)
			{
				return;
			}

			if (!_key4StateInitialized)
			{
				InitializeKey4TutorialState();
			}

			if (_key4TutorialShown)
			{
				return;
			}

			var saveData = _saveManager.SaveDataInstance;
			if (saveData == null)
			{
				return;
			}

			var hasKey4Now = HasKey4();
			var gotKey4ThisTime = hasKey4Now && !_hadKey4;
			_hadKey4 = hasKey4Now;

			if (!gotKey4ThisTime)
			{
				return;
			}

			if (saveData.GetNowRoom() != _key4TutorialRoomId)
			{
				return;
			}

			if (saveData.GetFlagNum(SaveData.SaveFlag.STAGE4_KEY4_TUTORIAL_SHOWN) > 0)
			{
				_key4TutorialShown = true;
				return;
			}

			StartCoroutine(ShowKey4Tutorial());
		}

		private void InitializeKey4TutorialState()
		{
			if (_saveManager == null || _saveManager.SaveDataInstance == null)
			{
				return;
			}

			_key4TutorialShown = _saveManager.SaveDataInstance.GetFlagNum(SaveData.SaveFlag.STAGE4_KEY4_TUTORIAL_SHOWN) > 0;
			_hadKey4 = HasKey4();
			_key4StateInitialized = true;
		}

		private bool HasKey4()
		{
			var items = _saveManager?.SaveDataInstance?.GetItemNum();
			if (items == null)
			{
				return false;
			}
			return items.Exists(item => item == SaveData.ItemKind.KEY_4);
		}

		private IEnumerator ShowMoveTutorial()
		{
			yield return ShowDialogAndWait("なるほど、\n場所が変わった。", 2f);
			_waitingForZoom = true;
		}

		private IEnumerator ShowZoomTutorial(int roomId)
		{
			yield return ShowDialogAndWait("気になるところを\nタップすると、\n拡大できるみたいだ。");
			yield return ShowDialogAndWait("これでじっくり\n調べられるな！");

			if (roomId == _buttonPromptRoomId)
			{
				yield return ShowButtonTutorial();
			}
			else
			{
				_waitingForButtonPrompt = true;
			}
		}

		private IEnumerator ShowButtonTutorial()
		{
			yield return ShowDialogAndWait("ボタンがある……押したら、\n何か起こるかも？");
		}

		private IEnumerator ShowStageClearDialog()
		{
			yield return ShowDialogSequence(
                "よし、扉が開いた！\n少しずつ進めば、\n外に出られるかもしれない。",
                "今日は日本代表の試合があるんだ！！\n夜までには、絶対帰らないと！"
			);
		}

		private IEnumerator ShowKey4Tutorial()
		{
			if (_saveManager == null)
			{
				yield break;
			}

			_key4TutorialRunning = true;
			SetInputEnabled(false);
			yield return ShowDialogSequence(
				"どうやら、いたるところにアイテムがあるらしい！",
				"アイテムを使う時は、\n画面下のアイテム画像をクリックして\n枠が赤くなったら、\n使いたい場所をクリックすると使えそうだ！",
				"この鍵をどこかで使えばいいのかな？"
			);
			SetInputEnabled(true);

			_key4TutorialShown = true;
			_saveManager.SaveDataInstance.SetFlagNum(SaveData.SaveFlag.STAGE4_KEY4_TUTORIAL_SHOWN, 1);
			_saveManager.Save();
			_key4TutorialRunning = false;
		}

		private void SetInputEnabled(bool isEnabled)
		{
			if (_stageManager != null)
			{
				_stageManager.SetInputEnabled(isEnabled);
			}
			if (_roomUI != null)
			{
				_roomUI.SetInputEnabled(isEnabled);
			}
		}

		private IEnumerator ShowDialogAndWait(string text, float delayAfter = 0f)
		{
			if (_dialogManager == null)
			{
				yield break;
			}

			while (_dialogManager.IsDialogEnable())
			{
				yield return null;
			}

			var dialog = _dialogManager.CreateDialog(text);
			var closed = false;
			dialog.OnClose += () => { closed = true; };
			while (!closed)
			{
				yield return null;
			}
			if (delayAfter > 0f)
			{
				yield return new WaitForSeconds(delayAfter);
			}
		}

		private IEnumerator ShowDialogSequence(params string[] lines)
		{
			if (lines == null || lines.Length == 0)
			{
				yield break;
			}

			for (int i = 0; i < lines.Length; i++)
			{
				yield return ShowDialogAndWait(lines[i]);
			}
		}

		private IEnumerator PlaySmoke()
		{
			if (_smokeOverlay == null)
			{
				yield break;
			}

			if (!_smokeOverlay.gameObject.activeSelf)
			{
				_smokeOverlay.gameObject.SetActive(true);
			}
			yield return FadeImage(_smokeOverlay, 0f, _smokeAlpha, _smokeFadeInSeconds);
			if (_smokeHoldSeconds > 0f)
			{
				yield return new WaitForSeconds(_smokeHoldSeconds);
			}
			yield return FadeImage(_smokeOverlay, _smokeAlpha, 0f, _smokeFadeOutSeconds);
			_smokeOverlay.gameObject.SetActive(false);
		}

		private IEnumerator PlayWhiteToBlackFade()
		{
			if (_fadeOverlay == null)
			{
				yield break;
			}

			if (!_fadeOverlay.gameObject.activeSelf)
			{
				_fadeOverlay.gameObject.SetActive(true);
			}
			_fadeOverlay.color = new Color(1f, 1f, 1f, 0f);
			yield return FadeImage(_fadeOverlay, 0f, 1f, _fadeToWhiteSeconds);
			if (_fadeWhiteHoldSeconds > 0f)
			{
				yield return new WaitForSeconds(_fadeWhiteHoldSeconds);
			}

			yield return FadeColor(_fadeOverlay, Color.white, Color.black, _fadeToBlackSeconds);
			if (_fadeBlackHoldSeconds > 0f)
			{
				yield return new WaitForSeconds(_fadeBlackHoldSeconds);
			}
			yield return FadeImage(_fadeOverlay, 1f, 0f, _fadeOutSeconds);
			_fadeOverlay.gameObject.SetActive(false);
		}

		private static IEnumerator FadeImage(Image image, float fromAlpha, float toAlpha, float duration)
		{
			if (image == null)
			{
				yield break;
			}

			if (duration <= 0f)
			{
				var color = image.color;
				color.a = toAlpha;
				image.color = color;
				yield break;
			}

			var elapsed = 0f;
			var colorStart = image.color;
			colorStart.a = fromAlpha;
			image.color = colorStart;

			while (elapsed < duration)
			{
				elapsed += Time.deltaTime;
				var t = Mathf.Clamp01(elapsed / duration);
				var color = image.color;
				color.a = Mathf.Lerp(fromAlpha, toAlpha, t);
				image.color = color;
				yield return null;
			}
		}

		private static IEnumerator FadeColor(Image image, Color from, Color to, float duration)
		{
			if (image == null)
			{
				yield break;
			}

			if (duration <= 0f)
			{
				image.color = to;
				yield break;
			}

			var elapsed = 0f;
			from.a = 1f;
			to.a = 1f;
			image.color = from;

			while (elapsed < duration)
			{
				elapsed += Time.deltaTime;
				var t = Mathf.Clamp01(elapsed / duration);
				image.color = Color.Lerp(from, to, t);
				yield return null;
			}
		}
	}
}
