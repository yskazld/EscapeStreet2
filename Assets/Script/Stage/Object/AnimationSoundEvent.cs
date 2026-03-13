using UnityEngine;
using ObjectAssets;
using Stage;

/// <summary>
/// Animation EventからSEを鳴らすための簡易コンポーネント
/// </summary>
public class AnimationSoundEvent : MonoBehaviour
{
    [SerializeField] private ObjectAssetSound soundAsset;
    [SerializeField] private float volume = 0.4f;
    [SerializeField] private float pitch = 1.0f;
    [SerializeField] private int roomId = -1;
    [SerializeField] private bool stopWhenNotInRoom = true;

    private int _resolvedRoomId = -1;

    private void Awake()
    {
        if (roomId > 0)
        {
            _resolvedRoomId = roomId;
            return;
        }

        var room = GetComponentInParent<RoomManager>();
        if (room != null)
        {
            _resolvedRoomId = room.ID;
        }
    }

    private void OnEnable()
    {
        var gameManager = GameManager.GetInstance();
        if (gameManager != null && gameManager.StageManagerInstance != null)
        {
            gameManager.StageManagerInstance.OnEnterRoom += HandleEnterRoom;
        }
    }

    private void OnDisable()
    {
        var gameManager = GameManager.GetInstance();
        if (gameManager != null && gameManager.StageManagerInstance != null)
        {
            gameManager.StageManagerInstance.OnEnterRoom -= HandleEnterRoom;
        }
    }

    private void HandleEnterRoom(int newRoomId)
    {
        if (!stopWhenNotInRoom || _resolvedRoomId <= 0 || soundAsset == null)
        {
            return;
        }

        if (newRoomId != _resolvedRoomId)
        {
            var soundManager = SoundManager.GetInstance();
            if (soundManager != null)
            {
                soundManager.Stop(soundAsset.Sound);
            }
        }
    }

    private bool IsCurrentRoom()
    {
        if (_resolvedRoomId <= 0)
        {
            return true;
        }

        var gameManager = GameManager.GetInstance();
        if (gameManager == null || gameManager.SaveManagerInstance == null)
        {
            return false;
        }

        var saveData = gameManager.SaveManagerInstance.SaveDataInstance;
        if (saveData == null)
        {
            return false;
        }

        return saveData.GetNowRoom() == _resolvedRoomId;
    }

    public void PlaySound()
    {
        if (!IsCurrentRoom())
        {
            return;
        }

        if (soundAsset == null)
        {
            Debug.LogWarning($"[AnimationSoundEvent] soundAsset is not set on {name}.");
            return;
        }

        var soundManager = SoundManager.GetInstance();
        if (soundManager != null)
        {
            soundManager.Play(soundAsset.Sound, pitch, volume);
        }
        else
        {
            Debug.LogWarning($"[AnimationSoundEvent] SoundManager is not ready. Skip playing sound on {name}.");
        }
    }
}
