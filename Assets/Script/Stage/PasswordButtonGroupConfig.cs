using UnityEngine;

/// <summary>
/// Stage9 の数字ボタン用設定。Hierarchy 上の ButtonBase の子に付与し、参照するフォルダを指定する。
/// </summary>
[DisallowMultipleComponent]
public class PasswordButtonGroupConfig : MonoBehaviour
{
	[SerializeField] private int _buttonIndex = 0;
	[SerializeField] private UnityEngine.Object _flagAssetFolder = null;
	[SerializeField] private UnityEngine.Object _numberImageFolder = null;

	/// <summary>
	/// 1 〜 のボタン番号。0 の場合はオブジェクト名から自動推測します。
	/// </summary>
	public int ButtonIndex => _buttonIndex;

	/// <summary>
	/// フラグ関連 ScriptableObject を格納したフォルダ（DefaultAsset）を指定します。
	/// </summary>
	public UnityEngine.Object FlagAssetFolder => _flagAssetFolder;

	/// <summary>
	/// 数字画像を格納したフォルダ（DefaultAsset）を指定します。
	/// </summary>
	public UnityEngine.Object NumberImageFolder => _numberImageFolder;
}
