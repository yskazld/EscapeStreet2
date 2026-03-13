using System;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Serialization;


namespace UI.Dialog
{
	/// <summary>
	/// ダイアログの管理します
	/// </summary>
	public class DialogManager : MonoBehaviour
	{
		private static DialogManager _instance;
		private List<DialogBase> _dialogList = new List<DialogBase>();
		[SerializeField] private DialogBase _originalDialogBase;
		[SerializeField] private DialogBase _originalHintDialogBase;
		[SerializeField] private ItemDialog _originalItemDialog;
		[SerializeField] private YesNoDialog _originalYesNoDialogBase;
		
		/// <summary>
		/// 初期化処理
		/// </summary>
		public void Init()
		{
			_instance = this;
		}

		public static DialogManager GetInstance()
		{
			return _instance;
		}

		/// <summary>
		/// ダイアログを生み出す
		/// </summary>
		/// <param name="text"></param>
		/// <returns></returns>
		public DialogBase CreateDialog(string text)
		{
			DialogBase dialog = Instantiate(_originalDialogBase, this.transform, true);
			dialog.SetText(text);
			var transform1 = dialog.transform.transform;
			transform1.localPosition = new Vector3(0, 0, 0);
			transform1.localScale = new Vector3(1, 1, 1);
			dialog.gameObject.SetActive(true);
			AddDialog(dialog);
			return dialog;
		}

		/// <summary>
		/// ヒント用ダイアログを生み出す
		/// </summary>
		/// <param name="text"></param>
		/// <returns></returns>
		public DialogBase CreateHintDialog(string text)
		{
			var source = _originalHintDialogBase;
			if (source == null)
			{
				source = Resources.Load<DialogBase>("UI/DialogBaseHint");
			}
			if (source == null)
			{
				source = _originalDialogBase;
			}

			DialogBase dialog = Instantiate(source, this.transform, true);
			dialog.SetText(text);
			var transform1 = dialog.transform.transform;
			transform1.localPosition = new Vector3(0, 0, 0);
			transform1.localScale = new Vector3(1, 1, 1);
			dialog.gameObject.SetActive(true);
			AddDialog(dialog);
			return dialog;
		}
		
		/// <summary>
		/// アイテム拡大ダイアログを出す
		/// </summary>
		/// <param name="text"></param>
		/// <param name="kind"></param>
		/// <param name="onClose"></param>
		/// <returns></returns>
		public ItemDialog CreateItemDialog(string text, Save.SaveData.ItemKind kind,Action onClose)
		{
			ItemDialog dialog = Instantiate(_originalItemDialog, this.transform, true);
			dialog.SetText(text);
			dialog.SettingItem(kind);
			var transform1 = dialog.transform.transform;
			transform1.localPosition = new Vector3(0, 0, 0);
			transform1.localScale = new Vector3(1, 1, 1);
			dialog.gameObject.SetActive(true);
			AddDialog(dialog);
			return dialog;
		}

		
		/// <summary>
		/// YesNo選択ができるダイアログを生み出す
		/// </summary>
		/// <param name="text"></param>
		/// <returns></returns>
		public YesNoDialog CreateYesNoDialog(string text)
		{
			var dialog = Instantiate(_originalYesNoDialogBase, this.transform, true);
			dialog.SetText(text);
			var transform1 = dialog.transform.transform;
			transform1.localPosition = new Vector3(0, 0, 0);
			transform1.localScale = new Vector3(1, 1, 1);
			dialog.gameObject.SetActive(true);
			AddDialog(dialog);
			return dialog;
		}

		/// <summary>
		/// ダイアログリストにダイアログを追加
		/// </summary>
		/// <param name="dialog"></param>
		private void AddDialog(DialogBase dialog)
		{
			_dialogList.Add(dialog);

			dialog.OnClose += () =>
			{
				_dialogList.Remove(dialog);
			};
		}
		
		/// <summary>
		/// すべてのダイアログを消す
		/// </summary>
		public void DialogClear()
		{
			for (int i = 0; i < _dialogList.Count; i++)
			{
				_dialogList[i].Close();
			}
			_dialogList.Clear();
		}

		/// <summary>
		/// ダイアログが一個以上存在しているか
		/// </summary>
		/// <returns></returns>
		public bool IsDialogEnable()
		{
			return _dialogList.Count >= 1;
		}
	}
}
