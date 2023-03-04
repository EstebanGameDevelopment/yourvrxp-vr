using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using yourvrexperience.Utils;

namespace yourvrexperience.VR
{
    public class ScreenVRKeyboardView : BaseScreenView, IScreenView
    {
        public const string ScreenName = "ScreenVRKeyboardView";

        public const string EventScreenVRKeyboardSetNewText = "EventScreenVRKeyboardSetNewText";
        public const string EventScreenVRKeyboardConfirmInput = "EventScreenVRKeyboardConfirmInput";

		private GameObject _target;
        private KeyboardManager _keyboardManager;

        public override void Initialize(params object[] parameters)
        {
            base.Initialize(parameters);

            _keyboardManager = _content.GetComponentInChildren<KeyboardManager>();
			_target = (GameObject)parameters[0];
            _keyboardManager.inputText.text = (string)parameters[1];
            _keyboardManager.maxInputLength = (int)parameters[2];
            _keyboardManager.Initialize();

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