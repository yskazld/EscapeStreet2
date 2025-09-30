using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace UI.Dialog
{
    public class YesNoDialog : DialogBase
    {

        /// <summary>
        /// YESボタン
        /// </summary>
        [SerializeField] private Button _yesButton;

        /// <summary>
        /// NOボタン
        /// </summary>
        [SerializeField] private Button _noButton;

        public Action OnYes;
        public Action OnNo;

        private void Start()
        {
            if (_yesButton != null)
            {
                _yesButton.onClick.AddListener(() =>
                {
                    OnYes();
                    Close();
                });
            }

            if (_noButton != null)
            {
                _noButton.onClick.AddListener(() =>
                {
                    OnNo();
                    Close();
                });
            }
        }
    }
}
