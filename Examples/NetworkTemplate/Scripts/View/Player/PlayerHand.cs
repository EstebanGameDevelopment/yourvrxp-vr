using System.Collections;
using System.Collections.Generic;
using yourvrexperience.Utils;
using yourvrexperience.VR;
using TMPro;
#if ENABLE_NETWORK
using yourvrexperience.Networking;
using System;
#endif

using UnityEngine;

namespace yourvrexperience.VR
{
	[RequireComponent(typeof(Collider))]
	[RequireComponent(typeof(Rigidbody))]	
	public class PlayerHand : MonoBehaviour
#if ENABLE_NETWORK	
	, INetworkObject
#endif	
	{
		public const string EventPlayerHandHasStarted = "EventPlayerHandHasStarted";
		public const string EventPlayerHandDestroyedAvatar = "EventPlayerHandDestroyedAvatar";

		[SerializeField] private GameObject Mesh;
		[SerializeField] private XR_HAND Hand;
		
		private Color _color;
		private PlayerAvatar _player;

		public PlayerAvatar Player
		{
			get { return _player; }
			set { _player = value; }
		}

#if ENABLE_NETWORK
		private NetworkObjectID _networkGameID;
		public NetworkObjectID NetworkGameIDView
		{
			get
			{
				if (_networkGameID == null)
				{
					if (this != null)
					{
						_networkGameID = GetComponent<NetworkObjectID>();
					}
				}
				return _networkGameID;
			}
		}

		public Color PlayerColor
		{
			get {return _color;}
			set { _color = value; 
				SetInitData(Utilities.PackColor(_color));
			}
		}
		public string NameNetworkPrefab 
		{
			get { return null; }
		}

		public string NameNetworkPath 
		{
			get { return null; }
		}
		public bool LinkedToCurrentLevel
		{
			get { return false; }
		}

		void Start()
		{
			SystemEventController.Instance.Event += OnSystemEvent;

			NetworkGameIDView.InitedEvent += OnInitDataEvent;
#if ENABLE_MIRROR			
			NetworkGameIDView.RefreshAuthority();
#endif			

			if (NetworkGameIDView.AmOwner())
			{
				SystemEventController.Instance.DispatchSystemEvent(EventPlayerHandHasStarted, this);

				Mesh.SetActive(false);
				
#if ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR
				VRInputController.Instance.DispatchVREvent(VRInputController.EventVRInputControllerLinkWithHand, this.gameObject, Hand);
#endif
			}
		}

		void OnDestroy()
		{
			NetworkGameIDView.InitedEvent -= OnInitDataEvent;
			if (SystemEventController.Instance != null) SystemEventController.Instance.Event -= OnSystemEvent;
		}

		public void SetInitData(string initializationData)
		{
			NetworkGameIDView.InitialInstantiationData = initializationData;
		}

		public void OnInitDataEvent(string initializationData)
		{
			PlayerColor = Utilities.UnpackColor(initializationData);
			Utilities.ApplyColor(Mesh.transform, PlayerColor);
		}

		public void ActivatePhysics(bool activation, bool force = false)
		{
		}

		private void OnSystemEvent(string nameEvent, object[] parameters)
		{
			if (nameEvent.Equals(EventPlayerHandDestroyedAvatar))
			{
				PlayerAvatar playerDestroyed = (PlayerAvatar)parameters[0];
				if (_player == playerDestroyed)
				{
					GameObject.Destroy(this.gameObject);
				}
			}
			if (nameEvent.Equals(SystemEventController.EventSystemEventControllerDontDestroyOnLoad))	
			{
				DontDestroyOnLoad(this.gameObject);
			}			
		}
#endif
	}
}
