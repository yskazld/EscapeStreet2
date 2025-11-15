using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ObjectAssets;
using ObjectAssets.Condition;
using Stage.Object;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Stage9 のパスワード UI を、指定したフォルダ内の ScriptableObject／スプライトに差し替えるエディタユーティリティ。
/// </summary>
public static class Stage9PasswordConfigurator
{
	private const string DefaultStage9Folder = "Assets/Resources/ObjectData/Stage9";
	private const string DefaultNumbersFolder = "Assets/Resources/Numbers_Image";

	[MenuItem("Tools/Stage9/Assign Password Assets")]
	public static void AssignStage9Assets()
	{
		var configs = UnityEngine.Object.FindObjectsOfType<PasswordButtonGroupConfig>(true);
		if (configs.Length == 0)
		{
			EditorUtility.DisplayDialog("Stage9 Setup", "PasswordButtonGroupConfig がシーン内にありません。ButtonBase 直下の 1〜5 それぞれに追加し、フォルダを指定してください。", "OK");
			return;
		}

		bool didChange = false;
		foreach (var config in configs.OrderBy(cfg => GetHierarchyPath(cfg.transform)))
		{
			var contextName = GetHierarchyPath(config.transform);
			var buttonIndex = DetermineButtonIndex(config);
			if (buttonIndex <= 0)
			{
				Debug.LogWarning($"[Stage9 Setup] {contextName}: ボタン番号が判断できません。PasswordButtonGroupConfig の Button Index を設定してください。");
				continue;
			}

			var flagFolderPath = ResolveFolderPath(config.FlagAssetFolder, Path.Combine(DefaultStage9Folder, $"Button{buttonIndex}"));
			var spriteFolderPath = ResolveFolderPath(config.NumberImageFolder, DefaultNumbersFolder);

			if (!AssetDatabase.IsValidFolder(flagFolderPath))
			{
				EditorUtility.DisplayDialog("Stage9 Setup", $"{contextName} で指定したフラグ用フォルダが見つかりません: {flagFolderPath}", "OK");
				continue;
			}
			if (!AssetDatabase.IsValidFolder(spriteFolderPath))
			{
				EditorUtility.DisplayDialog("Stage9 Setup", $"{contextName} で指定した数字画像フォルダが見つかりません: {spriteFolderPath}", "OK");
				continue;
			}

			if (!TryLoadButtonAssets(contextName, buttonIndex, flagFolderPath, spriteFolderPath, out var assets))
			{
				continue;
			}

			var buttonContainer = FindButtonContainer(config.transform);
			if (buttonContainer == null)
			{
				Debug.LogWarning($"[Stage9 Setup] {contextName}: Button 内に 0〜9 の子オブジェクトが見つかりません。Hierarchy の構成を確認してください。");
			}
			else if (ConfigureDigits(buttonContainer, assets, contextName))
			{
				didChange = true;
			}

			var autoReset = FindAutoResetTransform(config.transform);
			if (autoReset == null)
			{
				Debug.LogWarning($"[Stage9 Setup] {contextName}: AutoReset オブジェクトが見つかりません。");
			}
			else if (ConfigureAutoPlay(autoReset, assets, contextName))
			{
				didChange = true;
			}

			if (ConfigureButton(config.transform, assets, contextName))
			{
				didChange = true;
			}
		}

		if (didChange)
		{
			AssetDatabase.SaveAssets();
			EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
			EditorUtility.DisplayDialog("Stage9 Setup", "Stage9 のパズル設定を更新しました。", "OK");
		}
		else
		{
			EditorUtility.DisplayDialog("Stage9 Setup", "変更はありませんでした。", "OK");
		}
	}

	private static bool ConfigureDigits(Transform buttonContainer, ButtonAssets assets, string contextName)
	{
		bool didChange = false;
		foreach (Transform child in buttonContainer)
		{
			if (child == null || !int.TryParse(child.name, out var digit))
			{
				continue;
			}

			if (!assets.DigitConditions.TryGetValue(digit, out var condition) || condition == null)
			{
				Debug.LogWarning($"[Stage9 Setup] {contextName}: 数字 {digit} 用の条件アセットが見つからないためスキップしました。");
				continue;
			}

			var objectBase = child.GetComponent<ObjectBase>();
			if (objectBase == null)
			{
				Debug.LogWarning($"[Stage9 Setup] {contextName}: {child.name} に ObjectBase が見つかりません。");
				continue;
			}

			var serializedObject = new SerializedObject(objectBase);
			var conditionList = serializedObject.FindProperty("_clicktConditionList");
			if (conditionList != null &&
			    (conditionList.arraySize != 1 || conditionList.GetArrayElementAtIndex(0).objectReferenceValue != condition))
			{
				Undo.RecordObject(objectBase, "Assign Stage9 digit condition");
				conditionList.arraySize = 1;
				conditionList.GetArrayElementAtIndex(0).objectReferenceValue = condition;
				serializedObject.ApplyModifiedProperties();
				EditorUtility.SetDirty(objectBase);
				didChange = true;
			}

			if (assets.DigitSprites.TryGetValue(digit, out var sprite) && sprite != null)
			{
				var images = child.GetComponentsInChildren<Image>(true);
				if (images.Length == 0)
				{
					Debug.LogWarning($"[Stage9 Setup] {contextName}: {child.name} 配下に Image コンポーネントがありません。");
				}
				else
				{
					var targetImage = images[0];
					if (images.Length > 1)
					{
						Debug.LogWarning($"[Stage9 Setup] {contextName}: {child.name} 配下に Image が複数ありました。先頭の {targetImage.name} に差し替えています。");
					}

					if (targetImage.sprite != sprite)
					{
						Undo.RecordObject(targetImage, "Assign Stage9 digit sprite");
						targetImage.sprite = sprite;
						EditorUtility.SetDirty(targetImage);
						didChange = true;
					}
				}
			}
		}

		return didChange;
	}

	private static bool ConfigureAutoPlay(Transform autoReset, ButtonAssets assets, string contextName)
	{
		var autoPlay = autoReset.GetComponentInChildren<AutoPlayObject>(true);
		if (autoPlay == null)
		{
			Debug.LogWarning($"[Stage9 Setup] {contextName}: AutoReset 内に AutoPlayObject が見つかりません。");
			return false;
		}

		bool didChange = false;
		var serializedObject = new SerializedObject(autoPlay);

		var objectDataList = serializedObject.FindProperty("_objectDataList");
		if (objectDataList != null &&
		    (objectDataList.arraySize != 1 || objectDataList.GetArrayElementAtIndex(0).objectReferenceValue != assets.PassResetAsset))
		{
			Undo.RecordObject(autoPlay, "Assign Stage9 auto reset asset");
			objectDataList.arraySize = 1;
			objectDataList.GetArrayElementAtIndex(0).objectReferenceValue = assets.PassResetAsset;
			didChange = true;
		}

		var conditionList = serializedObject.FindProperty("_autoPlayConsitionList");
		if (conditionList != null &&
		    (conditionList.arraySize != 1 || conditionList.GetArrayElementAtIndex(0).objectReferenceValue != assets.OverConditionAsset))
		{
			if (!didChange)
			{
				Undo.RecordObject(autoPlay, "Assign Stage9 auto reset condition");
			}
			conditionList.arraySize = 1;
			conditionList.GetArrayElementAtIndex(0).objectReferenceValue = assets.OverConditionAsset;
			didChange = true;
		}

		if (didChange)
		{
			serializedObject.ApplyModifiedProperties();
			EditorUtility.SetDirty(autoPlay);
		}

		return didChange;
	}

	private static bool ConfigureButton(Transform group, ButtonAssets assets, string contextName)
	{
		var button = FindClickableButton(group);
		if (button == null)
		{
			Debug.LogWarning($"[Stage9 Setup] {contextName}: Flag を加算する Button(ObjectBase) が見つかりません。");
			return false;
		}

		var objectBase = button.GetComponent<ObjectBase>();
		if (objectBase == null)
		{
			Debug.LogWarning($"[Stage9 Setup] {contextName}: {button.name} に ObjectBase が付いていません。");
			return false;
		}

		var serializedObject = new SerializedObject(objectBase);
		var dataList = serializedObject.FindProperty("_objectDataList");
		if (dataList == null)
		{
			return false;
		}

		if (dataList.arraySize != 1 || dataList.GetArrayElementAtIndex(0).objectReferenceValue != assets.AddAsset)
		{
			Undo.RecordObject(objectBase, "Assign Stage9 add flag asset");
			dataList.arraySize = 1;
			dataList.GetArrayElementAtIndex(0).objectReferenceValue = assets.AddAsset;
			serializedObject.ApplyModifiedProperties();
			EditorUtility.SetDirty(objectBase);
			return true;
		}

		return false;
	}

	private static Button FindClickableButton(Transform group)
	{
		var buttons = group.GetComponentsInChildren<Button>(true);
		foreach (var btn in buttons)
		{
			if (btn == null || btn.GetComponent<ObjectBase>() == null)
			{
				continue;
			}

			var name = btn.transform.name;
			if (btn.transform.parent == group ||
			    name.IndexOf("Button", StringComparison.OrdinalIgnoreCase) >= 0)
			{
				return btn;
			}
		}

		return buttons.FirstOrDefault(btn => btn != null && btn.GetComponent<ObjectBase>() != null);
	}

	private static bool TryLoadButtonAssets(string contextName, int buttonIndex, string flagFolderPath, string spriteFolderPath, out ButtonAssets assets)
	{
		assets = new ButtonAssets(buttonIndex, flagFolderPath, spriteFolderPath);

		for (int digit = 0; digit <= 9; digit++)
		{
			var condition = LoadAssetWithCandidates<ObjectConditionBase>(flagFolderPath,
				$"Pass{buttonIndex}-{digit}",
				$"Pass{buttonIndex}_{digit}",
				$"Pass{buttonIndex} {digit}",
				$"Pass{buttonIndex}{digit}");
			if (condition != null)
			{
				assets.DigitConditions[digit] = condition;
			}
			else
			{
				Debug.LogWarning($"[Stage9 Setup] {contextName}: {flagFolderPath} で Pass{buttonIndex}-{digit} に相当する条件アセットが見つかりませんでした。");
			}

			var sprite = LoadSpriteWithCandidates(spriteFolderPath,
				$"image_{digit}",
				$"Image_{digit}");
			if (sprite != null)
			{
				assets.DigitSprites[digit] = sprite;
			}
		}

		assets.PassResetAsset = LoadAssetWithCandidates<ObjectAssetBase>(flagFolderPath,
			$"Pass{buttonIndex} 0",
			$"Pass{buttonIndex}_0",
			$"Pass{buttonIndex}-0",
			"Pass 0",
			"Pass_0",
			"Pass-0");
		if (assets.PassResetAsset == null)
		{
			EditorUtility.DisplayDialog("Stage9 Setup", $"{contextName}: Pass 0 用のアセットが {flagFolderPath} 内に見つかりません。", "OK");
			return false;
		}

		assets.OverConditionAsset = LoadAssetWithCandidates<ObjectConditionBase>(flagFolderPath,
			$"Over_Pass{buttonIndex}-9",
			$"Over_Pass{buttonIndex}_9",
			$"Over_Pass{buttonIndex} 9",
			$"OverPass{buttonIndex}9");
		if (assets.OverConditionAsset == null)
		{
			EditorUtility.DisplayDialog("Stage9 Setup", $"{contextName}: Over_Pass{buttonIndex}-9 が {flagFolderPath} 内に見つかりません。", "OK");
			return false;
		}

		assets.AddAsset = LoadAssetWithCandidates<ObjectAssetBase>(flagFolderPath,
			$"Pass{buttonIndex}_Add",
			$"Pass{buttonIndex}_add",
			$"Pass{buttonIndex}-Add",
			$"Pass{buttonIndex}-add",
			$"Pass{buttonIndex}Add",
			$"Pass{buttonIndex} add");
		if (assets.AddAsset == null)
		{
			EditorUtility.DisplayDialog("Stage9 Setup", $"{contextName}: Pass{buttonIndex}_Add が {flagFolderPath} 内に見つかりません。", "OK");
			return false;
		}

		if (assets.DigitConditions.Count == 0)
		{
			EditorUtility.DisplayDialog("Stage9 Setup", $"{contextName}: 0〜9 の条件アセットが一つも取得できませんでした。フォルダ設定を確認してください。", "OK");
			return false;
		}

		return true;
	}

	private static T LoadAssetWithCandidates<T>(string folderPath, params string[] candidateNames) where T : UnityEngine.Object
	{
		foreach (var candidate in candidateNames)
		{
			if (string.IsNullOrWhiteSpace(candidate))
			{
				continue;
			}
			var directPath = Path.Combine(folderPath, candidate + ".asset");
			var asset = AssetDatabase.LoadAssetAtPath<T>(directPath);
			if (asset != null)
			{
				return asset;
			}
		}

		var normalizedCandidates = candidateNames
			.Where(name => !string.IsNullOrWhiteSpace(name))
			.Select(name => name.ToLowerInvariant())
			.ToArray();

		var guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}", new[] { folderPath });
		foreach (var guid in guids)
		{
			var path = AssetDatabase.GUIDToAssetPath(guid);
			var fileName = Path.GetFileNameWithoutExtension(path).ToLowerInvariant();
			if (normalizedCandidates.Contains(fileName))
			{
				return AssetDatabase.LoadAssetAtPath<T>(path);
			}
		}

		return null;
	}

	private static Sprite LoadSpriteWithCandidates(string folderPath, params string[] candidateNames)
	{
		foreach (var candidate in candidateNames)
		{
			if (string.IsNullOrWhiteSpace(candidate))
			{
				continue;
			}
			var directPathPng = Path.Combine(folderPath, candidate + ".png");
			var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(directPathPng);
			if (sprite != null)
			{
				return sprite;
			}

			var directPathPsd = Path.Combine(folderPath, candidate + ".psd");
			sprite = AssetDatabase.LoadAssetAtPath<Sprite>(directPathPsd);
			if (sprite != null)
			{
				return sprite;
			}
		}

		var normalizedCandidates = candidateNames
			.Where(name => !string.IsNullOrWhiteSpace(name))
			.Select(name => name.ToLowerInvariant())
			.ToArray();

		var guids = AssetDatabase.FindAssets("t:Sprite", new[] { folderPath });
		foreach (var guid in guids)
		{
			var path = AssetDatabase.GUIDToAssetPath(guid);
			var fileName = Path.GetFileNameWithoutExtension(path).ToLowerInvariant();
			if (normalizedCandidates.Contains(fileName))
			{
				return AssetDatabase.LoadAssetAtPath<Sprite>(path);
			}
		}

		return null;
	}

	private static Transform FindButtonContainer(Transform group)
	{
		foreach (Transform child in group)
		{
			if (ContainsDigitChildren(child))
			{
				return child;
			}
		}

		return group.GetComponentsInChildren<Transform>(true)
			.FirstOrDefault(t => t != group && ContainsDigitChildren(t));
	}

	private static Transform FindAutoResetTransform(Transform group)
	{
		var direct = group.Find("AutoReset");
		if (direct != null)
		{
			return direct;
		}

		return group.GetComponentsInChildren<Transform>(true)
			.FirstOrDefault(t => t != group && t.name.IndexOf("AutoReset", StringComparison.OrdinalIgnoreCase) >= 0);
	}

	private static bool ContainsDigitChildren(Transform parent)
	{
		if (parent == null)
		{
			return false;
		}

		int count = 0;
		foreach (Transform child in parent)
		{
			if (child != null && int.TryParse(child.name, out _))
			{
				count++;
			}
		}
		return count >= 3;
	}

	private static string ResolveFolderPath(UnityEngine.Object folderObject, string fallback)
	{
		if (folderObject != null)
		{
			var path = AssetDatabase.GetAssetPath(folderObject);
			if (AssetDatabase.IsValidFolder(path))
			{
				return path;
			}
			Debug.LogWarning($"[Stage9 Setup] {folderObject.name} はフォルダではありません。デフォルト ({fallback}) を使用します。");
		}
		return fallback;
	}

	private static int DetermineButtonIndex(PasswordButtonGroupConfig config)
	{
		if (config == null)
		{
			return 0;
		}

		if (config.ButtonIndex > 0)
		{
			return config.ButtonIndex;
		}

		var indexFromName = ParseTrailingNumber(config.transform.name);
		if (indexFromName > 0)
		{
			return indexFromName;
		}

		return ParseTrailingNumber(config.transform.parent != null ? config.transform.parent.name : string.Empty);
	}

	private static int ParseTrailingNumber(string text)
	{
		if (string.IsNullOrEmpty(text))
		{
			return 0;
		}

		int end = text.Length - 1;
		while (end >= 0 && char.IsDigit(text[end]))
		{
			end--;
		}

		if (end == text.Length - 1)
		{
			return 0;
		}

		var numberText = text.Substring(end + 1);
		return int.TryParse(numberText, out var result) ? result : 0;
	}

	private static string GetHierarchyPath(Transform transform)
	{
		var names = new List<string>();
		var current = transform;
		while (current != null)
		{
			names.Add(current.name);
			current = current.parent;
		}
		names.Reverse();
		return string.Join("/", names);
	}

	private sealed class ButtonAssets
	{
		public int ButtonIndex { get; }
		public string FlagFolderPath { get; }
		public string SpriteFolderPath { get; }
		public Dictionary<int, ObjectConditionBase> DigitConditions { get; } = new Dictionary<int, ObjectConditionBase>();
		public Dictionary<int, Sprite> DigitSprites { get; } = new Dictionary<int, Sprite>();
		public ObjectAssetBase PassResetAsset { get; set; }
		public ObjectConditionBase OverConditionAsset { get; set; }
		public ObjectAssetBase AddAsset { get; set; }

		public ButtonAssets(int buttonIndex, string flagFolderPath, string spriteFolderPath)
		{
			ButtonIndex = buttonIndex;
			FlagFolderPath = flagFolderPath;
			SpriteFolderPath = spriteFolderPath;
		}
	}
}
