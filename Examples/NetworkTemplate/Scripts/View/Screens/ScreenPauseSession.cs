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
	public class ScreenPauseSession : BaseScreenView, IScreenView
	{
		public const string ScreenName = "ScreenPauseSession";

		[SerializeField] private Button buttonResume;
		[SerializeField] private Button buttonNextLevel;
		[SerializeField] private Button buttonDisconnect;

		public override string NameScreen 
		{ 
			get { return ScreenName; }
		}

		public override void Initialize(params object[] parameters)
		{
			base.Initialize(parameters);

			_content.Find("Title").GetComponent<TextMeshProUGUI>().text = LanguageController.Instance.GetText("screen.pause.title");

			buttonResume.onClick.AddListener(OnButtonResume);
			buttonResume.transform.Find("Text").GetComponent<TextMeshProUGUI>().text = LanguageController.Instance.GetText("screen.pause.resume");

			buttonNextLevel.onClick.AddListener(OnButtonNextLevel);
			buttonNextLevel.transform.Find("Text").GetComponent<TextMeshProUGUI>().text = LanguageController.Instance.GetText("screen.pause.next.level");

			buttonDisconnect.onClick.AddListener(OnButtonDisconnect);
#if ENABLE_NETWORK
			buttonDisconnect.transform.Find("Text").GetComponent<TextMeshProUGUI>().text = LanguageController.Instance.GetText("screen.config.disconnect");
#else			
			buttonDisconnect.transform.Find("Text").GetComponent<TextMeshProUGUI>().text = LanguageController.Instance.GetText("screen.config.exit.to.menu");
#endif			
			UIEventController.Instance.Event += OnUIEvent;

#if ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR
			VRInputController.Instance.DispatchVREvent(VRInputController.EventVRInputControllerEnableLocomotion, false);
#endif						
		}

		private void OnButtonResume()
		{
			UIEventController.Instance.DispatchUIEvent(NetworkedSessionController.EventNetworkedBasicSessionResumeInSessionState);
		}

		private void OnButtonNextLevel()
		{
			int nextLevel = (NetworkedSessionController.Instance.CurrentLevel + 1) % NetworkedSessionController.Instance.GetTotalLevels();
			SystemEventController.Instance.DispatchSystemEvent(NetworkedSessionController.EventNetworkedBasicSessionLoadLevel, nextLevel);
		}
		

		private void OnButtonDisconnect()
		{
			string titleWarning = LanguageController.Instance.GetText("text.warning");
			string textAskToExit = LanguageController.Instance.GetText("screen.main.do.you.want.to.disconnect");
			ScreenInformationView.CreateScreenInformation(ScreenInformationView.ScreenConfirmation, this.gameObject, titleWarning, textAskToExit);
		}

		public override void Destroy()
		{
#if ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR
			VRInputController.Instance.DispatchVREvent(VRInputController.EventVRInputControllerEnableLocomotion, true);
#endif			
			if (UIEventController.Instance != null) UIEventController.Instance.Event -= OnUIEvent;
			base.Destroy();
		}

		private void OnUIEvent(string nameEvent, object[] parameters)
		{
			if (nameEvent.Equals(ScreenInformationView.EventScreenInformationResponse))
			{
				if (this.gameObject == (GameObject)parameters[0])
				{
					ScreenInformationResponses userResponse = (ScreenInformationResponses)parameters[1];
					if (userResponse == ScreenInformationResponses.Confirm)
					{
						UIEventController.Instance.DispatchUIEvent(NetworkedSessionController.EventNetworkedBasicSessionResumeDisconnect);
					}
				}
			}
		}
	}
}