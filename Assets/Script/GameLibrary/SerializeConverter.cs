using UnityEngine;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

#if UNITY_IOS
public class SerializeConverter 
{
	// <!!> T is any struct or class marked with [Serializable]
	public static void Save<T> (string prefKey, T serializableObject) {
		System.Environment.SetEnvironmentVariable("MONO_REFLECTION_SERIALIZER", "yes");
		MemoryStream memoryStream = new MemoryStream ();
		BinaryFormatter bf = new BinaryFormatter ();
		bf.Serialize (memoryStream, serializableObject);
		string tmp = System.Convert.ToBase64String (memoryStream.ToArray ());
		PlayerPrefs.SetString ( prefKey, tmp );
		
	}
		/// <summary>
	/// クラスをバイナリに変換する
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="serializableObject"></param>
	/// <returns></returns>
	public static string ConvertClassToBinaryData<T>(T serializableObject)
	{
	
		System.Environment.SetEnvironmentVariable("MONO_REFLECTION_SERIALIZER", "yes");
		MemoryStream memoryStream = new MemoryStream ();
		BinaryFormatter bf = new BinaryFormatter ();
		bf.Serialize (memoryStream, serializableObject);
		return System.Convert.ToBase64String (memoryStream.ToArray ());
	}
	
	public static T Load<T> (string prefKey) {
		System.Environment.SetEnvironmentVariable("MONO_REFLECTION_SERIALIZER", "yes");
		if (!PlayerPrefs.HasKey(prefKey)) return default(T);
		
		BinaryFormatter bf = new BinaryFormatter();
		string serializedData = PlayerPrefs.GetString(prefKey);
		MemoryStream dataStream = new MemoryStream(System.Convert.FromBase64String(serializedData));
		T deserializedObject = (T)bf.Deserialize(dataStream);
		return deserializedObject;
	}
	
	public static bool Exist(string prefkey ) {
		return PlayerPrefs.HasKey( prefkey );
	}
}
#else
/// <summary>
/// クラス→バイナリ変換
/// バイナリ→クラス変換
/// </summary>
public class SerializeConverter
{
	/// <summary>
	/// クラスをバイナリに変換する
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="serializableObject"></param>
	/// <returns></returns>
	public static string ConvertClassToBinaryData<T>(T serializableObject)
	{
		MemoryStream memoryStream = new MemoryStream();
		BinaryFormatter bf = new BinaryFormatter();
		bf.Serialize(memoryStream, serializableObject);
		return System.Convert.ToBase64String(memoryStream.ToArray());
	}

	/// <summary>
	/// バイナリをクラスに変換
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="prefKey"></param>
	/// <returns></returns>
	public static T Load<T>(string prefKey)
	{
		BinaryFormatter bf = new BinaryFormatter();
		string serializedData = PlayerPrefs.GetString(prefKey);
		MemoryStream dataStream = new MemoryStream(System.Convert.FromBase64String(serializedData));
		T deserializedObject = (T)bf.Deserialize(dataStream);
		return deserializedObject;
	}

	public static bool Exist(string prefkey)
	{
		return PlayerPrefs.HasKey(prefkey);
	}
}
#endif