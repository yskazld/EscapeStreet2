using Save;
using UnityEngine;

namespace Stage.Object
{
    public class ArrangePuzzleController : ObjectBase
    {
        [SerializeField] private Transform[] _slots;

        private int _firstSelectedIndex = -1;

        public void Swap(int fromIndex, int toIndex)
        {
            // 並べ替え処理 (例: 子オブジェクトの入れ替え)
            var temp = _slots[fromIndex];
            _slots[fromIndex] = _slots[toIndex];
            _slots[toIndex] = temp;
            UpdateSlots();
            TryClear();
        }

        public void OnSlotClicked(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= _slots.Length)
            {
                Debug.LogWarning($"[ArrangePuzzle] Invalid slot index {slotIndex}");
                return;
            }

            if (_firstSelectedIndex < 0)
            {
                _firstSelectedIndex = slotIndex;
                return;
            }

            if (_firstSelectedIndex == slotIndex)
            {
                _firstSelectedIndex = -1;
                return;
            }

            Swap(_firstSelectedIndex, slotIndex);
            _firstSelectedIndex = -1;
        }

        private void UpdateSlots()
        {
            for (var i = 0; i < _slots.Length; i++)
            {
                _slots[i].SetSiblingIndex(i);
            }
        }

        private void TryClear()
        {
            if (!IsCorrectOrder())
            {
                return;
            }

            var save = GameManager.GetInstance().SaveManagerInstance.SaveDataInstance;
            if (save.GetFlagNum(SaveData.SaveFlag.STAGE_2_CLEAR) > 0)
            {
                return;
            }

            save.SetFlagNum(SaveData.SaveFlag.STAGE_2_CLEAR, 1);
            var stageManager = GameManager.GetInstance().StageManagerInstance;
            stageManager.NotifyStageClear(SaveData.SaveFlag.STAGE_2_CLEAR);
            stageManager.UpdateNowRoom();
        }

        private bool IsCorrectOrder()
        {
            // 子並びが「赤→青→黄→緑」か判定するなど
            return true;
        }
    }
}
