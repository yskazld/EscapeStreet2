namespace Csv
{
	/// <summary>
	/// ヒントの内容を格納している
	/// </summary>
	public class HintMasterData
	{
		/// <summary>
		/// ヒント文字
		/// </summary>
		private string[] _messages;

		/// <summary>
		/// 答え
		/// </summary>
		private string[] _answerMessages;

		/// <summary>
		/// 表示のための条件フラグ
		/// このフラグがオフなら表示をする
		/// </summary>
		public Save.SaveData.SaveFlag Flag { get; private set; }

		public HintMasterData(string[] hintMessages, string[] answerMessages, Save.SaveData.SaveFlag flag)
		{
			_messages = hintMessages;
			_answerMessages = answerMessages;
			Flag = flag;
		}

		public string GetMessage(int language,bool isAnswer)
		{
			if (isAnswer)
			{
				return _answerMessages[language];
			}
			return _messages[language];
		}
	}
}
