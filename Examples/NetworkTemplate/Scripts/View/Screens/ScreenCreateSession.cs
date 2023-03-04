using System;
using System.Collections;
using System.Collections.Generic;
using yourvrexperience.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
#if ENABLE_NETWORK
using yourvrexperience.Networking;
#endif

namespace yourvrexperience.VR
{
	public class ScreenCreateSession : BaseScreenView, IScreenView
	{
		public const string ScreenName = "ScreenCreateSession";

		[SerializeField] private CustomInput hostSession;
		[SerializeField] private CustomInput roomNameSession;
		[SerializeField] private CustomInput numberClientsSession;
		[SerializeField] private Button buttonCreateSession;
		[SerializeField] private Button buttonBack;

		public override string NameScreen 
		{ 
			get { return ScreenName; }
		}

		public override void Initialize(params object[] parameters)
		{
			base.Initialize(parameters);

			_content.Find("Title").GetComponent<TextMeshProUGUI>().text = LanguageController.Instance.GetText("screen.create.session.title");
			
			hostSession.text = NetworkedSessionController.Instance.GetHostName();
			roomNameSession.text = NetworkedSessionController.Instance.GetRoomName();
			numberClientsSession.text = NetworkedSessionController.Instance.GetNumberClients().ToString();

			hostSession.OnFocusEvent += OnHostFocusEvent;
			roomNameSession.OnFocusEvent += OnRoomNameFocusEvent;
			numberClientsSession.OnFocusEvent += OnNumberClientsFocusEvent;

			Transform nameHostTitle = _content.Find("NameHostTitle");
			nameHostTitle.GetComponent<TextMeshProUGUI>().text = LanguageController.Instance.GetText("screen.create.session.host");
			Transform roomNameTitle = _content.Find("NameRoomTitle");
			roomNameTitle.GetComponent<TextMeshProUGUI>().text = LanguageController.Instance.GetText("screen.create.session.name.room");
#if ENABLE_MIRROR || ENABLE_NETCODE
			roomNameTitle.gameObject.SetActive(false);
			roomNameSession.gameObject.SetActive(false);
#elif ENABLE_PHOTON || ENABLE_NAKAMA
			nameHostTitle.gameObject.SetActive(false);
			hostSession.gameObject.SetActive(false);
#endif			
			_content.Find("NumberClientsTitle").GetComponent<TextMeshProUGUI>().text = LanguageController.Instance.GetText("screen.create.session.number.clients");

			buttonCreateSession.onClick.AddListener(OnButtonCreateSession);
			buttonBack.onClick.AddListener(OnButtonBack);

			buttonBack.transform.Find("Text").GetComponent<TextMeshProUGUI>().text = LanguageController.Instance.GetText("text.back");
			buttonCreateSession.transform.Find("Text").GetComponent<TextMeshProUGUI>().text = LanguageController.Instance.GetText("screen.create.session.button.start.host");

			UIEventController.Instance.Event += OnUIEvent;
		}

		private void OnNumberClientsFocusEvent()
		{
#if ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR
			_content.gameObject.SetActive(false);
			ScreenController.Instance.CreateScreen(ScreenVRKeyboardView.ScreenName, false, true,  numberClientsSession.gameObject, numberClientsSession.text, 2);
#endif			
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

		private void OnButtonCreateSession()
		{
			UIEventController.Instance.DispatchUIEvent(NetworkedSessionController.EventNetworkedBasicSessionConnect, true, hostSession.text, roomNameSession.text, int.Parse(numberClientsSession.text));
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
				if (numberClientsSession.gameObject == (GameObject)parameters[0])
				{
					numberClientsSession.text = (string)parameters[1];
				}				
			}
		}
	}
}