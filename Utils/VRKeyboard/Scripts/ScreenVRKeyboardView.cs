using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using yourvrexperience.Utils;
using static TMPro.TMP_InputField;

namespace yourvrexperience.VR
{
    public class ScreenVRKeyboardView : BaseScreenView, IScreenView
    {
        public const string ScreenName = "ScreenVRKeyboardView";

        public const string EventScreenVRKeyboardSetNewText = "EventScreenVRKeyboardSetNewText";
        public const string EventScreenVRKeyboardConfirmInput = "EventScreenVRKeyboardConfirmInput";

		private GameObject _target;
        private KeyboardManager _keyboardManager;
        private TMP_InputField _inputField;
        private ContentType _typeContent = ContentType.Standard;

        public override void Initialize(params object[] parameters)
        {
            base.Initialize(parameters);

            _keyboardManager = _content.GetComponentInChildren<KeyboardManager>();
			_target = (GameObject)parameters[0];
            if (parameters[1] is TMP_InputField)
            {
                _inputField = (TMP_InputField)parameters[1];
                _keyboardManager.inputText.text = _inputField.text;
                _typeContent = _inputField.contentType;
                if (parameters.Length > 3)
                {
                    _typeContent = (ContentType)parameters[3];
                } 
            }
            else
            {
                _keyboardManager.inputText.text = (string)parameters[1];
                _typeContent = ContentType.Standard;
                if (parameters.Length > 3)
                {
                    _typeContent = (ContentType)parameters[3];
                }                
            }
            _keyboardManager.maxInputLength = (int)parameters[2];
            _keyboardManager.Initialize(_typeContent);
            UIEventController.Instance.Event += OnUIEvent;
		}

        void OnDestroy()
        {
            Destroy();
        }

        public override void Destroy()
        {
            if (_keyboardManager != null)
            {
                _keyboardManager.Destroy();
                _keyboardManager = null;

                base.Destroy();

                UIEventController.Instance.Event -= OnUIEvent;

				UIEventController.Instance.DispatchUIEvent(ScreenController.EventScreenControllerDestroyScreen, this.gameObject);
            }
        }

        private void OnUIEvent(string nameEvent, params object[] parameters)
		{
            if (nameEvent.Equals(EventScreenVRKeyboardConfirmInput))
            {
                string finalText = _keyboardManager.inputText.text;
                Destroy();
                GameObject.Destroy(this.gameObject);
                UIEventController.Instance.DispatchUIEvent(EventScreenVRKeyboardSetNewText, _target, finalText);
            }
		}
    }
}