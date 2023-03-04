using System;
using System.Collections;
using System.Collections.Generic;
using yourvrexperience.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace yourvrexperience.VR
{
	public class ScreenJoinSession : BaseScreenView, IScreenView
	{
		public const string ScreenName = "ScreenJoinSession";

		[SerializeField] private CustomInput hostSession;
		[SerializeField] private CustomInput roomNameSession;
		[SerializeField] private Button buttonJoinSession;
		[SerializeField] private Button buttonBack;

		public override string NameScreen 
		{ 
			get { return ScreenName; }
		}

		public override void Initialize(params object[] parameters)
		{
			base.Initialize(parameters);

			_content.Find("Title").GetComponent<TextMeshProUGUI>().text = LanguageController.Instance.GetText("screen.join.session.title");
			
			hostSession.text = NetworkedSessionController.Instance.GetHostName();
			roomNameSession.text = NetworkedSessionController.Instance.GetRoomName();

			hostSession.OnFocusEvent += OnHostFocusEvent;
			roomNameSession.OnFocusEvent += OnRoomNameFocusEvent;

			_content.Find("NameHostTitle").GetComponent<TextMeshProUGUI>().text = LanguageController.Instance.GetText("screen.create.session.host");

			Transform roomNameTitle = _content.Find("NameRoomTitle");
			roomNameTitle.GetComponent<TextMeshProUGUI>().text = LanguageController.Instance.GetText("screen.create.session.name.room");
#if ENABLE_MIRROR || ENABLE_NETCODE
			roomNameTitle.gameObject.SetActive(false);
			roomNameSession.gameObject.SetActive(false);
#endif

			buttonJoinSession.onClick.AddListener(OnButtonJoinSession);
			buttonBack.onClick.AddListener(OnButtonBack);

			buttonBack.transform.Find("Text").GetComponent<TextMeshProUGUI>().text = LanguageController.Instance.GetText("text.back");
			buttonJoinSession.transform.Find("Text").GetComponent<TextMeshProUGUI>().text = LanguageController.Instance.GetText("screen.join.session.button.join");

			UIEventController.Instance.Event += OnUIEvent;
		}

		private void OnRoomNameFocusEvent()
		{
#if ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR
			_content.gameObject.SetActive(false);
			ScreenController.Instance.CreateScreen(ScreenVRKeyboardView.ScreenName, false, true,  roomNameSession.gameObject, roomNameSession.text, 10);
#endif			
		}

		private void OnHostFocusEvent()
		{
#if ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR
			_content.gameObject.SetActive(false);
			ScreenController.Instance.CreateScreen(ScreenVRKeyboardView.ScreenName, false, true,  hostSession.gameObject, hostSession.text, 40);
#endif			
		}

		private void OnButtonJoinSession()
		{
			UIEventController.Instance.DispatchUIEvent(NetworkedSessionController.EventNetworkedBasicSessionConnect, false, hostSession.text, roomNameSession.text);
		}

		public override void Destroy()
		{
			base.Destroy();

			if (UIEventController.Instance != null) UIEventController.Instance.Event -= OnUIEvent;
		}

		private void OnButtonBack()
		{
			UIEventController.Instance.DispatchUIEvent(ScreenController.EventScreenControllerDestroyScreen, this.gameObject);
		}
		
		private void OnUIEvent(string nameEvent, object[] parameters)
		{
			if (nameEvent.Equals(ScreenVRKeyboardView.EventScreenVRKeyboardSetNewText))
			{
				if (hostSession.gameObject == (GameObject)parameters[0])
				{
					hostSession.text = (string)parameters[1];
				}				
				if (roomNameSession.gameObject == (GameObject)parameters[0])
				{
					roomNameSession.text = (string)parameters[1];
				}				
			}
		}
	}
}