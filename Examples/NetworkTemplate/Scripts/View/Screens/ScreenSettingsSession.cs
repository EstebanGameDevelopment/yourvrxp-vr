using System;
using System.Collections;
using System.Collections.Generic;
using yourvrexperience.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace yourvrexperience.VR
{
	public class ScreenSettingsSession : BaseScreenView, IScreenView
	{
		public const string ScreenName = "ScreenSettingsSession";

		public const string EventScreenSettingsSessionName = "EventScreenSettingsSessionName";

		[SerializeField] private CustomInput userName;
		[SerializeField] private Button buttonBack;

		public override string NameScreen 
		{ 
			get { return ScreenName; }
		}

		public override void Initialize(params object[] parameters)
		{
			base.Initialize(parameters);

			userName.text = (string)parameters[0];

			_content.Find("Title").GetComponent<TextMeshProUGUI>().text = LanguageController.Instance.GetText("screen.settings.title");
			_content.Find("UsernameTitle").GetComponent<TextMeshProUGUI>().text = LanguageController.Instance.GetText("screen.main.menu.username");

			userName.OnFocusEvent += OnFocusEvent;

			buttonBack.onClick.AddListener(OnButtonBack);
			buttonBack.transform.Find("Text").GetComponent<TextMeshProUGUI>().text = LanguageController.Instance.GetText("text.back");

			UIEventController.Instance.Event += OnUIEvent;
		}

		private void OnFocusEvent()
		{
#if ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR
			_content.gameObject.SetActive(false);
			ScreenController.Instance.CreateScreen(ScreenVRKeyboardView.ScreenName, false, true,  userName.gameObject, userName.text, 10);
#endif			
		}

		private void OnButtonBack()
		{
			UIEventController.Instance.DispatchUIEvent(EventScreenSettingsSessionName, userName.text);
			UIEventController.Instance.DispatchUIEvent(ScreenController.EventScreenControllerDestroyScreen, this.gameObject);
		}

		public override void Destroy()
		{
			base.Destroy();

			if (UIEventController.Instance != null) UIEventController.Instance.Event -= OnUIEvent;
		}

		private void OnUIEvent(string nameEvent, object[] parameters)
		{
			if (nameEvent.Equals(ScreenVRKeyboardView.EventScreenVRKeyboardSetNewText))
			{
				if (userName.gameObject == (GameObject)parameters[0])
				{
					userName.text = (string)parameters[1];
				}				
			}
		}
	}
}