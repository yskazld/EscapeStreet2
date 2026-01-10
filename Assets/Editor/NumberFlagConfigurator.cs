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
/// 数字パス系のUIを、指定フォルダ内の ScriptableObject／スプライトに差し替えるエディタユーティリティ。
/// 既存の Stage9PasswordConfigurator と同等の機能をより汎用的な名称で提供します。
/// </summary>
public static class NumberFlagConfigurator
{
	private const string DefaultStage9Folder = "Assets/Resources/ObjectData/Stage9";
	private const string DefaultNumbersFolder = "Assets/Resources/Numbers_Image";

	[MenuItem("Tools/Number Flag/Assign Password Assets")]
	public static void AssignStage9Assets()
	{
		var configs = UnityEngine.Object.FindObjectsOfType<PasswordButtonGroupConfig>(true);
		if (configs.Length == 0)
		{
			EditorUtility.DisplayDialog("Number Flag Setup", "PasswordButtonGroupConfig がシーン内にありません。ButtonBase 直下の 1〜5 それぞれに追加し、フォルダを指定してください。", "OK");
			return;
		}

		bool didChange = false;
		foreach (var config in configs.OrderBy(cfg => GetHierarchyPath(cfg.transform)))
		{
			var contextName = GetHierarchyPath(config.transform);
			var buttonIndex = DetermineButtonIndex(config);
			if (buttonIndex <= 0)
			{
				Debug.LogWarning($"[Number Flag Setup] {contextName}: ボタン番号が判断できません。PasswordButtonGroupConfig の Button Index を設定してください。");
				continue;
			}

			var flagFolderPath = ResolveFlagFolderPath(config.FlagAssetFolder, buttonIndex);
			var spriteFolderPath = ResolveFolderPath(config.NumberImageFolder, DefaultNumbersFolder);

			if (!AssetDatabase.IsValidFolder(flagFolderPath))
			{
				EditorUtility.DisplayDialog("Number Flag Setup", $"{contextName} で指定したフラグ用フォルダが見つかりません: {flagFolderPath}", "OK");
				continue;
			}
			if (!AssetDatabase.IsValidFolder(spriteFolderPath))
			{
				EditorUtility.DisplayDialog("Number Flag Setup", $"{contextName} で指定した数字画像フォルダが見つかりません: {spriteFolderPath}", "OK");
				continue;
			}

			if (!TryLoadButtonAssets(contextName, buttonIndex, flagFolderPath, spriteFolderPath, out var assets))
			{
				continue;
			}

			var buttonContainer = FindButtonContainer(config.transform);
			if (buttonContainer == null)
			{
				Debug.LogWarning($"[Number Flag Setup] {contextName}: Button 内に 0〜9 の子オブジェクトが見つかりません。Hierarchy の構成を確認してください。");
			}
			else if (ConfigureDigits(buttonContainer, assets, contextName))
			{
				didChange = true;
			}

			var autoReset = FindAutoResetTransform(config.transform);
			if (autoReset == null)
			{
				Debug.LogWarning($"[Number Flag Setup] {contextName}: AutoReset オブジェクトが見つかりません。");
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
			EditorUtility.DisplayDialog("Number Flag Setup", "パズル設定を更新しました。", "OK");
		}
		else
		{
			EditorUtility.DisplayDialog("Number Flag Setup", "変更はありませんでした。", "OK");
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
				Debug.LogWarning($"[Number Flag Setup] {contextName}: 数字 {digit} 用の条件アセットが見つからないためスキップしました。");
				continue;
			}

			var objectBase = child.GetComponent<ObjectBase>();
			if (objectBase == null)
			{
				Debug.LogWarning($"[Number Flag Setup] {contextName}: {child.name} に ObjectBase が見つかりません。");
				continue;
			}

			var serializedObject = new SerializedObject(objectBase);
			var conditionList = serializedObject.FindProperty("_clicktConditionList");
			if (conditionList != null &&
			    (conditionList.arraySize != 1 || conditionList.GetArrayElementAtIndex(0).objectReferenceValue != condition))
			{
				Undo.RecordObject(objectBase, "Assign Number Flag digit condition");
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
					Debug.LogWarning($"[Number Flag Setup] {contextName}: {child.name} 配下に Image コンポーネントがありません。");
				}
				else
				{
					var targetImage = images[0];
					if (images.Length > 1)
					{
						Debug.LogWarning($"[Number Flag Setup] {contextName}: {child.name} 配下に Image が複数ありました。先頭の {targetImage.name} に差し替えています。");
					}

					if (targetImage.sprite != sprite)
					{
						Undo.RecordObject(targetImage, "Assign Number Flag digit sprite");
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
			Debug.LogWarning($"[Number Flag Setup] {contextName}: AutoReset 内に AutoPlayObject が見つかりません。");
			return false;
		}

		bool didChange = false;
		var serializedObject = new SerializedObject(autoPlay);

		if (assets.PassResetAsset == null || assets.OverConditionAsset == null)
		{
			Debug.LogWarning($"[Number Flag Setup] {contextName}: AutoReset に必要なアセット(PassReset/OverCondition)が不足しているためスキップします。");
			return false;
		}

		var objectDataList = serializedObject.FindProperty("_objectDataList");
		if (objectDataList != null &&
		    (objectDataList.arraySize != 1 || objectDataList.GetArrayElementAtIndex(0).objectReferenceValue != assets.PassResetAsset))
		{
			Undo.RecordObject(autoPlay, "Assign Number Flag auto reset asset");
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
				Undo.RecordObject(autoPlay, "Assign Number Flag auto reset condition");
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

	private static bool ConfigureButton(Transform configTransform, ButtonAssets assets, string contextName)
	{
		var buttonBase = configTransform.GetComponentInParent<ObjectBase>(true);
		if (buttonBase == null)
		{
			Debug.LogWarning($"[Number Flag Setup] {contextName}: ButtonBase に ObjectBase が見つかりません。");
			return false;
		}

		if (assets.PassAddAsset == null)
		{
			Debug.LogWarning($"[Number Flag Setup] {contextName}: Pass_Add アセットが不足しているためスキップします。");
			return false;
		}

		var serializedObject = new SerializedObject(buttonBase);
		var objectDataList = serializedObject.FindProperty("_objectDataList");
		if (objectDataList == null)
		{
			Debug.LogWarning($"[Number Flag Setup] {contextName}: ButtonBase の ObjectDataList が見つかりません。");
			return false;
		}

		if (objectDataList.arraySize != 1 || objectDataList.GetArrayElementAtIndex(0).objectReferenceValue != assets.PassAddAsset)
		{
			Undo.RecordObject(buttonBase, "Assign Number Flag button add asset");
			objectDataList.arraySize = 1;
			objectDataList.GetArrayElementAtIndex(0).objectReferenceValue = assets.PassAddAsset;
			serializedObject.ApplyModifiedProperties();
			EditorUtility.SetDirty(buttonBase);
			return true;
		}

		return false;
	}

	private static Transform FindButtonContainer(Transform configTransform)
	{
		return configTransform.parent?.Find("Button");
	}

	private static Transform FindAutoResetTransform(Transform configTransform)
	{
		return configTransform.parent?.Find("AutoReset");
	}

	private static int DetermineButtonIndex(PasswordButtonGroupConfig config)
	{
		if (config.ButtonIndex > 0)
		{
			return config.ButtonIndex;
		}

		if (int.TryParse(config.name, out var indexFromName))
		{
			return indexFromName;
		}

		return 0;
	}

	private static string GetHierarchyPath(Transform transform)
	{
		var stack = new Stack<string>();
		var current = transform;
		while (current != null)
		{
			stack.Push(current.name);
			current = current.parent;
		}
		return string.Join("/", stack);
	}

	private static string ResolveFolderPath(UnityEngine.Object folderAsset, string fallbackPath)
	{
		if (folderAsset == null)
		{
			return fallbackPath;
		}

		var path = AssetDatabase.GetAssetPath(folderAsset);
		return string.IsNullOrEmpty(path) ? fallbackPath : path;
	}

	private static string ResolveFlagFolderPath(UnityEngine.Object folderAsset, int buttonIndex)
	{
		var fallbackPath = Path.Combine(DefaultStage9Folder, $"Button{buttonIndex}");
		if (folderAsset == null)
		{
			return fallbackPath;
		}

		var path = AssetDatabase.GetAssetPath(folderAsset);
		if (string.IsNullOrEmpty(path))
		{
			return fallbackPath;
		}

		var buttonFolderName = $"Button{buttonIndex}";
		var currentFolderName = Path.GetFileName(path);
		if (string.Equals(currentFolderName, buttonFolderName, StringComparison.OrdinalIgnoreCase))
		{
			return path;
		}

		var nestedButtonPath = Path.Combine(path, buttonFolderName);
		if (AssetDatabase.IsValidFolder(nestedButtonPath))
		{
			return nestedButtonPath;
		}

		return path;
	}

	private static bool TryLoadButtonAssets(string contextName, int buttonIndex, string flagFolderPath, string spriteFolderPath, out ButtonAssets assets)
	{
		assets = new ButtonAssets();

		var conditionAssets = AssetDatabase.FindAssets("t:ObjectConditionFlag", new[] { flagFolderPath })
			.Select(guid => AssetDatabase.LoadAssetAtPath<ObjectConditionFlag>(AssetDatabase.GUIDToAssetPath(guid)))
			.Where(x => x != null)
			.ToArray();
		var overAssets = conditionAssets.Where(x => x.name.StartsWith("Over", StringComparison.OrdinalIgnoreCase)).ToArray();
		var passReset = AssetDatabase.LoadAssetAtPath<ObjectAssetFlagControl>(Path.Combine(flagFolderPath, $"Pass{buttonIndex} 0.asset"));
		var passAdd = AssetDatabase.LoadAssetAtPath<ObjectAssetAddFlagControl>(Path.Combine(flagFolderPath, $"Pass{buttonIndex}_Add.asset"));

		assets.PassResetAsset = passReset;
		assets.PassAddAsset = passAdd;
		assets.OverConditionAsset = overAssets.FirstOrDefault();

		if (overAssets.Length == 0)
		{
			Debug.LogWarning($"[Number Flag Setup] {contextName}: Over 系 ConditionFlag が見つかりません。必要なら追加してください。");
		}
		if (passReset == null)
		{
			Debug.LogWarning($"[Number Flag Setup] {contextName}: Pass{buttonIndex} 0.asset が見つかりません。必要なら追加してください。");
		}
		if (passAdd == null)
		{
			Debug.LogWarning($"[Number Flag Setup] {contextName}: Pass{buttonIndex}_Add.asset が見つかりません。必要なら追加してください。");
		}

		foreach (var condition in conditionAssets)
		{
			if (!condition.name.StartsWith($"Pass{buttonIndex}-", StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}

			var suffix = condition.name.Substring(condition.name.IndexOf('-') + 1);
			if (int.TryParse(suffix, out var digit))
			{
				assets.DigitConditions[digit] = condition;
			}
		}

		var digitSprites = AssetDatabase.FindAssets("t:Sprite", new[] { spriteFolderPath })
			.Select(guid => AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GUIDToAssetPath(guid)))
			.Where(x => x != null)
			.ToArray();
		foreach (var sprite in digitSprites)
		{
			if (int.TryParse(sprite.name, out var digit))
			{
				assets.DigitSprites[digit] = sprite;
			}
		}

		return true;
	}

	private class ButtonAssets
	{
		public readonly Dictionary<int, ObjectConditionFlag> DigitConditions = new Dictionary<int, ObjectConditionFlag>();
		public readonly Dictionary<int, Sprite> DigitSprites = new Dictionary<int, Sprite>();
		public ObjectConditionFlag OverConditionAsset;
		public ObjectAssetFlagControl PassResetAsset;
		public ObjectAssetAddFlagControl PassAddAsset;
	}
}
