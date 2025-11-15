using UnityEngine;
using UnityEngine.UI;
using Stage.Object;

namespace Stage.Object
{
    [RequireComponent(typeof(Button))]
    public class ArrangePuzzleSlot : MonoBehaviour
    {
        [SerializeField] private int _slotIndex;
        [SerializeField] private ArrangePuzzleController _controller;

        private void Awake()
        {
            var button = GetComponent<Button>();
            button.onClick.AddListener(() =>
            {
                _controller.OnSlotClicked(_slotIndex);
            });
        }
    }
}
