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
	public class ScreenConfigurationSession : BaseScreenView, IScreenView
	{
		public const string ScreenName = "ScreenConfigurationSession";

		public const string EventScreenConfigChangeLocomotion = "EventScreenConfigChangeLocomotion";

		[SerializeField] private Button buttonLocomotionLeft;
		[SerializeField] private Button buttonLocomotionRight;
		[SerializeField] private TextMeshProUGUI leftHandInfo;
		[SerializeField] private TextMeshProUGUI rightHandInfo;
		[SerializeField] private Button buttonExit;
		[SerializeField] private Button buttonDisconnect;
		[SerializeField] private Button buttonSettings;
		[SerializeField] private Button buttonBuild;
		[SerializeField] private TextMeshProUGUI userName;

		private LocomotionMode _leftHand;
		private LocomotionMode _rightHand;

		public override string NameScreen 
		{ 
			get { return ScreenName; }
		}

		public override void Initialize(params object[] parameters)
		{
			base.Initialize(parameters);

			_content.Find("Title").GetComponent<TextMeshProUGUI>().text = LanguageController.Instance.GetText("screen.config.title");
			_content.Find("UsernameTitle").GetComponent<TextMeshProUGUI>().text = LanguageController.Instance.GetText("screen.main.menu.username");

			GameObject leftHandContainer = _content.Find("Left_Hand").gameObject;
			GameObject rightHandContainer = _content.Find("Right_Hand").gameObject;

			leftHandContainer.transform.Find("Title").GetComponent<TextMeshProUGUI>().text = LanguageController.Instance.GetText("screen.config.left.hand");
			rightHandContainer.transform.Find("Title").GetComponent<TextMeshProUGUI>().text = LanguageController.Instance.GetText("screen.config.right.hand");

#if ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR						
			_leftHand = VRInputController.Instance.LocomotionLeftHand;
			_rightHand = VRInputController.Instance.LocomotionRightHand;

			buttonLocomotionLeft.onClick.AddListener(OnLocomotionLeft);
			buttonLocomotionRight.onClick.AddListener(OnLocomotionRight);
			leftHandInfo.text = LanguageController.Instance.GetText("locomotion.hand." + _leftHand.ToString());
			rightHandInfo.text = LanguageController.Instance.GetText("locomotion.hand." + _rightHand.ToString());
#else
			leftHandContainer.SetActive(false);
			rightHandContainer.SetActive(false);
#endif			

			buttonExit.onClick.AddListener(OnButtonExit);
			buttonExit.transform.Find("Text").GetComponent<TextMeshProUGUI>().text = LanguageController.Instance.GetText("text.exit");

			buttonDisconnect.onClick.AddListener(OnButtonDisconnect);
#if ENABLE_NETWORK
			buttonDisconnect.transform.Find("Text").GetComponent<TextMeshProUGUI>().text = LanguageController.Instance.GetText("screen.config.disconnect");
#else			
			buttonDisconnect.transform.Find("Text").GetComponent<TextMeshProUGUI>().text = LanguageController.Instance.GetText("screen.config.exit.to.menu");
#endif			

			buttonSettings.onClick.AddListener(OnButtonSettings);
			buttonSettings.transform.Find("Text").GetComponent<TextMeshProUGUI>().text = LanguageController.Instance.GetText("screen.main.menu.settings");

			buttonBuild.onClick.AddListener(OnButtonBuild);
			buttonBuild.transform.Find("Text").GetComponent<TextMeshProUGUI>().text = LanguageController.Instance.GetText("screen.config.build");

			userName.text = NetworkedSessionController.Instance.LocalUserName;
			
			UIEventController.Instance.Event += OnUIEvent;

#if ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR	
			VRInputController.Instance.DispatchVREvent(VRInputController.EventVRInputControllerEnableLocomotion, false);
#endif						
		}

		private void OnButtonBuild()
		{
			ScreenController.Instance.CreateScreen(ScreenBuilderSession.ScreenNameSession, false, true);
		}

		private void OnButtonSettings()
		{
			ScreenController.Instance.CreateScreen(ScreenSettingsSession.ScreenName, false, true, NetworkedSessionController.Instance.LocalUserName);
		}

		private void OnButtonDisconnect()
		{
			string titleWarning = LanguageController.Instance.GetText("text.warning");
			string textAskToExit = LanguageController.Instance.GetText("screen.main.do.you.want.to.disconnect");
			ScreenInformationView.CreateScreenInformation(ScreenInformationView.ScreenConfirmation, this.gameObject, titleWarning, textAskToExit);
		}

		private void OnLocomotionRight()
		{
			_rightHand++;
			if ((int)_rightHand > 3) _rightHand = 0;
			rightHandInfo.text = LanguageController.Instance.GetText("locomotion.hand." + _rightHand.ToString());
#if ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR
			VRInputController.Instance.DispatchVREvent(EventScreenConfigChangeLocomotion, true, _rightHand);
#endif			
		}

		private void OnLocomotionLeft()
		{
			_leftHand++;
			if ((int)_leftHand > 3) _leftHand = 0;
			leftHandInfo.text = LanguageController.Instance.GetText("locomotion.hand." + _leftHand.ToString());
#if ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR
			VRInputController.Instance.DispatchVREvent(EventScreenConfigChangeLocomotion, false, _leftHand);
#endif			
		}

		private void OnButtonExit()
		{
			UIEventController.Instance.DispatchUIEvent(NetworkedSessionController.EventNetworkedBasicSessionResumeInSessionState);
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
			if (nameEvent.Equals(ScreenSettingsSession.EventScreenSettingsSessionName))
			{
				userName.text = (string)parameters[0];
#if ENABLE_NETWORK				
				if (NetworkController.Instance.IsInRoom)
				{
					NetworkController.Instance.DispatchNetworkEvent(PlayerAvatar.EventPlayerAvatarNetworkSetUsername, -1, -1, NetworkedSessionController.Instance.LocalPlayer.NetworkGameIDView.GetViewID(), userName.text);
				}
#endif				
			}
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