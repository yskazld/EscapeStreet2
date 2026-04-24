using System.Collections;
using Save;
using Stage;
using UI.Dialog;
using UI.Room;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace Story
{
	/// <summary>
	/// ステージ13クリア後のエンディング演出
	/// </summary>
	public class EndingSequenceController : MonoBehaviour
	{
		[SerializeField] private int _endingExitRoomId = 900;
		[SerializeField] private int _clockRoomId = 910;

		[SerializeField] private Image _fadeOverlay;
		[SerializeField] private float _fadeOutSeconds = 0.4f;
		[SerializeField] private float _fadeInSeconds = 0.6f;
		[SerializeField] private float _runFadeOutSeconds = 0.5f;
		[SerializeField] private Color _fadeOutColor = Color.black;
		[SerializeField] private Color _fadeInColor = Color.white;

		[SerializeField] private Image _smokeOverlay;
		[SerializeField] [Range(0f, 1f)] private float _smokeAlpha = 0.35f;
		[SerializeField] private float _smokeFadeOutSeconds = 0.6f;

		[SerializeField] private bool _playDoorSe = true;
		[SerializeField] private SoundManager.SOUND_TYPE _doorOpenSe = SoundManager.SOUND_TYPE.OpenDoor;
		[SerializeField] private float _doorSeVolume = 0.4f;
		[SerializeField] private bool _playClockSe = true;
		[SerializeField] private SoundManager.SOUND_TYPE _clockSe = SoundManager.SOUND_TYPE.Select;
		[SerializeField] private float _clockSeVolume = 0.4f;
		[SerializeField] private bool _playRunSe;
		[SerializeField] private SoundManager.SOUND_TYPE _runSe = SoundManager.SOUND_TYPE.MoveRoom;
		[SerializeField] private float _runSeVolume = 0.4f;

		[SerializeField] private int _endingBgmIndex = -1;
		[SerializeField] private float _endingBgmFadeSeconds = 0.4f;

		[SerializeField] private bool _useAfterglowLine;
		[SerializeField] private string _afterglowLine = "霧なんて、もうこりごりだ……！";
		[SerializeField] private string _afterglowLineEnglish = "I've had more than enough of this fog...!";

		[SerializeField] private string _clearSubtitle = "日本代表戦、間に合う！今日は勝つぞ！！\n帰ったら、おかあさんにも不思議な体験を説明しないと……！";
		[SerializeField] private string _clearSubtitleEnglish = "I'll make it in time for the Japan match! We're going to win today!!\nWhen I get home, I need to tell Mom about this strange experience too...!";
		[SerializeField] private float _gameClearFontSize = 90f;
		[SerializeField] private bool _gameClearAutoSize;

		private StageManager _stageManager;
		private RoomUI _roomUI;
		private DialogManager _dialogManager;
		private SaveManager _saveManager;

		private bool _endingRunning;
		private bool _endingShown;

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
			}
		}

		private void OnDisable()
		{
			if (_stageManager != null)
			{
				_stageManager.OnEnterRoom -= HandleEnterRoom;
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
				_stageManager.OnEnterRoom += HandleEnterRoom;
			}

			if (_saveManager == null)
			{
				yield break;
			}

			var currentRoom = _saveManager.SaveDataInstance.GetNowRoom();
			if (currentRoom == _endingExitRoomId)
			{
				StartCoroutine(PlayEndingSequence());
			}
		}

		private void HandleEnterRoom(int roomId)
		{
			if (_endingRunning || _saveManager == null)
			{
				return;
			}

			if (roomId == _endingExitRoomId)
			{
				StartCoroutine(PlayEndingSequence());
			}
		}

		private IEnumerator PlayEndingSequence()
		{
			if (_endingRunning)
			{
				yield break;
			}

			_endingRunning = true;
			SetInputEnabled(false);

			if (_playDoorSe)
			{
				PlaySe(_doorOpenSe, _doorSeVolume);
			}
			yield return ShowDialogAndWait(Localize("……これで最後のはず！", "...This should be the last one!"));

			if (_stageManager != null)
			{
				_stageManager.EnterRoom(_endingExitRoomId);
			}

			if (_endingBgmIndex >= 0 && BGMManager.Instance != null)
			{
				BGMManager.Instance.Play(_endingBgmIndex, _endingBgmFadeSeconds);
			}

			yield return ShowDialogAndWait(Localize("やっと……出られた！！", "Finally... I'm out!!"));

			if (_playClockSe)
			{
				PlaySe(_clockSe, _clockSeVolume);
			}
			yield return ShowDialogAndWait(Localize("時計を見てみよう！\n……まだ17:30だ！", "Let me check the clock!\n...It's only 5:30 PM!"));
			yield return ShowDialogAndWait(Localize("今から帰れば、サッカーの試合間に合う！！", "If I head home now, I'll make it in time for the soccer match!!"));

			if (_playRunSe)
			{
				PlaySe(_runSe, _runSeVolume);
			}
			yield return FadeOverlay(_fadeInColor, 0f, 0f);

			if (_useAfterglowLine && !string.IsNullOrEmpty(_afterglowLine))
			{
				yield return ShowDialogAndWait(Localize(_afterglowLine, _afterglowLineEnglish));
			}

			yield return ShowDialogAndWait(Localize(_clearSubtitle, _clearSubtitleEnglish));
			yield return ShowDialogAndWaitWithFontSize(Localize("GAME\nCLEAR！！", "GAME\nCLEAR!!"), _gameClearFontSize, _gameClearAutoSize);

			SceneManager.LoadScene("title");

			SetInputEnabled(true);

			_endingRunning = false;
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

		private void PlaySe(SoundManager.SOUND_TYPE se, float volume)
		{
			var soundManager = SoundManager.GetInstance();
			if (soundManager == null)
			{
				return;
			}
			soundManager.Play(se, 1f, volume);
		}

		private string Localize(string japanese, string english)
		{
			if (_saveManager != null && _saveManager.SaveDataInstance != null &&
				_saveManager.SaveDataInstance.GetLanguage() == SaveData.LANGUAGE.ENGLISH)
			{
				return english;
			}

			return japanese;
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
				yield return new WaitForSecondsRealtime(delayAfter);
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

		private IEnumerator ShowDialogAndWaitWithFontSize(string text, float fontSize, bool autoSize)
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
			dialog.SetFontSize(fontSize, autoSize);
			var closed = false;
			dialog.OnClose += () => { closed = true; };
			while (!closed)
			{
				yield return null;
			}
		}

		private IEnumerator FadeOverlay(Color color, float toAlpha, float duration)
		{
			if (_fadeOverlay == null)
			{
				yield break;
			}

			if (!_fadeOverlay.gameObject.activeSelf)
			{
				_fadeOverlay.gameObject.SetActive(true);
			}

			color.a = _fadeOverlay.color.a;
			_fadeOverlay.color = color;

			yield return FadeImage(_fadeOverlay, _fadeOverlay.color.a, toAlpha, duration, color);
			if (Mathf.Approximately(toAlpha, 0f))
			{
				_fadeOverlay.gameObject.SetActive(false);
			}
		}

		private IEnumerator ClearSmoke()
		{
			if (_smokeOverlay == null)
			{
				yield break;
			}

			if (!_smokeOverlay.gameObject.activeSelf)
			{
				_smokeOverlay.gameObject.SetActive(true);
			}

			var color = _smokeOverlay.color;
			color.a = _smokeAlpha;
			_smokeOverlay.color = color;
			yield return FadeImage(_smokeOverlay, _smokeAlpha, 0f, _smokeFadeOutSeconds, color);
			_smokeOverlay.gameObject.SetActive(false);
		}

		private static IEnumerator FadeImage(Image image, float fromAlpha, float toAlpha, float duration, Color baseColor)
		{
			if (image == null)
			{
				yield break;
			}

			if (duration <= 0f)
			{
				baseColor.a = toAlpha;
				image.color = baseColor;
				yield break;
			}

			var elapsed = 0f;
			baseColor.a = fromAlpha;
			image.color = baseColor;

			while (elapsed < duration)
			{
				elapsed += Time.unscaledDeltaTime;
				var t = Mathf.Clamp01(elapsed / duration);
				var color = baseColor;
				color.a = Mathf.Lerp(fromAlpha, toAlpha, t);
				image.color = color;
				yield return null;
			}
		}
	}
}
