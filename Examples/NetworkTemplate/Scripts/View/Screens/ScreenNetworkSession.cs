using System;
using System.Collections;
using System.Collections.Generic;
using yourvrexperience.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


namespace yourvrexperience.VR
{
	public class ScreenNetworkSession : BaseScreenView, IScreenView
	{
		public const string ScreenName = "ScreenNetworkSession";

		[SerializeField] private Button buttonHost;
		[SerializeField] private Button buttonConnect;
		[SerializeField] private Button buttonSettings;
		[SerializeField] private TextMeshProUGUI userName;
		[SerializeField] private Button buttonExit;

		public override string NameScreen 
		{ 
			get { return ScreenName; }
		}

		public override void Initialize(params object[] parameters)
		{
			base.Initialize(parameters);

			_content.Find("Text").GetComponent<TextMeshProUGUI>().text = LanguageController.Instance.GetText("screen.main.menu.title");
			_content.Find("UsernameTitle").GetComponent<TextMeshProUGUI>().text = LanguageController.Instance.GetText("screen.main.menu.username");

			userName.text = NetworkedSessionController.Instance.LocalUserName;

			buttonHost.onClick.AddListener(OnButtonHost);
			buttonConnect.onClick.AddListener(OnButtonConnect);
			buttonSettings.onClick.AddListener(OnButtonSettings);
			buttonExit.onClick.AddListener(OnButtonExit);
#if ENABLE_NETWORK		
			buttonHost.transform.Find("Text").GetComponent<TextMeshProUGUI>().text = LanguageController.Instance.GetText("screen.main.menu.host.session");
			buttonConnect.transform.Find("Text").GetComponent<TextMeshProUGUI>().text = LanguageController.Instance.GetText("screen.main.menu.connect.to.session");
#else
			buttonHost.transform.Find("Text").GetComponent<TextMeshProUGUI>().text = LanguageController.Instance.GetText("screen.main.single.user");
			buttonConnect.gameObject.SetActive(false);
#endif			
			buttonSettings.transform.Find("Text").GetComponent<TextMeshProUGUI>().text = LanguageController.Instance.GetText("screen.main.menu.settings");
			buttonExit.transform.Find("Text").GetComponent<TextMeshProUGUI>().text = LanguageController.Instance.GetText("screen.main.menu.exit");

			UIEventController.Instance.Event += OnUIEvent;
		}

		public override void Destroy()
		{
			base.Destroy();

			if (UIEventController.Instance != null) UIEventController.Instance.Event -= OnUIEvent;
		}

		private void OnButtonExit()
		{
			string titleWarning = LanguageController.Instance.GetText("text.warning");
			string textAskToExit = LanguageController.Instance.GetText("screen.main.do.you.want.to.exit");
			ScreenInformationView.CreateScreenInformation(ScreenInformationView.ScreenConfirmation, this.gameObject, titleWarning, textAskToExit);
		}

		private void OnButtonSettings()
		{
			ScreenController.Instance.CreateForwardScreen(ScreenSettingsSession.ScreenName, NetworkedSessionController.Instance.DistanceMenus, false, true, NetworkedSessionController.Instance.LocalUserName);
		}

		private void OnButtonHost()
		{
#if !ENABLE_NETWORK
			UIEventController.Instance.DispatchUIEvent(NetworkedSessionController.EventNetworkedBasicSessionConnect);
#else			
			ScreenController.Instance.CreateForwardScreen(ScreenCreateSession.ScreenName, NetworkedSessionController.Instance.DistanceMenus, false, true);
#endif			
		}

		private void OnButtonConnect()
		{
#if ENABLE_MIRROR || ENABLE_NETCODE
			ScreenController.Instance.CreateForwardScreen(ScreenJoinSession.ScreenName, NetworkedSessionController.Instance.DistanceMenus, false, true);
#else
			SystemEventController.Instance.DispatchSystemEvent(NetworkedSessionController.EventNetworkedBasicSessionConnectToLobby);
#endif
		}

		private void DelayedQuit()
		{
			Application.Quit();
		}

		private void OnUIEvent(string nameEvent, object[] parameters)
		{
			if (nameEvent.Equals(ScreenSettingsSession.EventScreenSettingsSessionName))
			{
				userName.text = (string)parameters[0];
			}
			if (nameEvent.Equals(ScreenInformationView.EventScreenInformationResponse))
			{
				if (this.gameObject == (GameObject)parameters[0])
				{
					ScreenInformationResponses userResponse = (ScreenInformationResponses)parameters[1];
					if (userResponse == ScreenInformationResponses.Confirm)
					{
						string titleInfo = LanguageController.Instance.GetText("text.info");
						string textNowExiting = LanguageController.Instance.GetText("screen.main.now.exiting");
						ScreenInformationView.CreateScreenInformation(ScreenInformationView.ScreenLoading, this.gameObject, titleInfo, textNowExiting);
						Invoke("DelayedQuit", 2);
					}
				}
			}
		}
	}
}