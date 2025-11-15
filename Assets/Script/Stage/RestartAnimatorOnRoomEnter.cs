using System.Collections;
using UnityEngine;

namespace Stage
{
	/// <summary>
	/// 指定したルームに入ったタイミングで Animator を最初から再生し直す。
	/// </summary>
	[RequireComponent(typeof(Animator))]
	public class RestartAnimatorOnRoomEnter : MonoBehaviour
	{
		[SerializeField] private RoomManager _room;
		[SerializeField] private string _stateName = "EyesSequence";
		[SerializeField] private int _layerIndex = 0;
		[SerializeField] private float _normalizedStartTime = 0f;

		private Animator _animator;
		private StageManager _stageManager;
		private Coroutine _subscribeRoutine;

		private void Awake()
		{
			_animator = GetComponent<Animator>();
			if (_room == null)
			{
				_room = GetComponentInParent<RoomManager>();
			}
		}

		private void OnEnable()
		{
			BeginSubscribe();
		}

		private void OnDisable()
		{
			if (_subscribeRoutine != null)
			{
				StopCoroutine(_subscribeRoutine);
				_subscribeRoutine = null;
			}

			if (_stageManager != null)
			{
				_stageManager.OnEnterRoom -= HandleEnterRoom;
				_stageManager = null;
			}
		}

		private void BeginSubscribe()
		{
			if (!isActiveAndEnabled)
			{
				return;
			}

			if (_stageManager != null)
			{
				_stageManager.OnEnterRoom -= HandleEnterRoom;
				_stageManager.OnEnterRoom += HandleEnterRoom;
				return;
			}

			if (_subscribeRoutine == null)
			{
				_subscribeRoutine = StartCoroutine(SubscribeWhenReady());
			}
		}

		private IEnumerator SubscribeWhenReady()
		{
			while (isActiveAndEnabled)
			{
				var gameManager = GameManager.GetInstance();
				if (gameManager != null && gameManager.StageManagerInstance != null)
				{
					_stageManager = gameManager.StageManagerInstance;
					_stageManager.OnEnterRoom += HandleEnterRoom;
					_subscribeRoutine = null;
					yield break;
				}

				yield return null;
			}

			_subscribeRoutine = null;
		}

		private void HandleEnterRoom(int roomId)
		{
			if (_room == null || _animator == null)
			{
				return;
			}

			if (_room.ID != roomId)
			{
				return;
			}

			RestartAnimation();
		}

		private void RestartAnimation()
		{
			if (string.IsNullOrEmpty(_stateName))
			{
				var stateInfo = _animator.GetCurrentAnimatorStateInfo(_layerIndex);
				_animator.Play(stateInfo.fullPathHash, _layerIndex, _normalizedStartTime);
			}
			else
			{
				_animator.Play(_stateName, _layerIndex, _normalizedStartTime);
			}

			_animator.Update(0f);
		}
	}
}
