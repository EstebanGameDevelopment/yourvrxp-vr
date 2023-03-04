using System.Collections;
using System.Collections.Generic;
using yourvrexperience.Utils;
using yourvrexperience.VR;
using System;
using TMPro;
#if ENABLE_NETWORK
using yourvrexperience.Networking;
#endif

using UnityEngine;

namespace yourvrexperience.VR
{
	[RequireComponent(typeof(Collider))]
	[RequireComponent(typeof(Rigidbody))]	
	public class PlayerAvatar : MonoBehaviour, ICameraPlayer
#if ENABLE_NETWORK	
	, INetworkObject
#endif	
	{
		public const string EventPlayerAvatarHasStarted = "EventPlayerAvatarHasStarted";
		public const string EventPlayerAvatarEnableMovement = "EventPlayerAvatarEnableMovement";
		public const string EventPlayerAvatarRequestShootBullet = "EventPlayerAvatarRequestShootBullet";
		public const string EventPlayerAvatarCreateShootBullet = "EventPlayerAvatarCreateShootBullet";
		public const string EventPlayerAvatarNetworkImpact = "EventPlayerAvatarNetworkImpact";
		public const string EventPlayerAvatarNetworkUpdateLife = "EventPlayerAvatarNetworkUpdateLife";
		public const string EventPlayerAvatarNetworkSetUsername = "EventPlayerAvatarNetworkSetUsername";

		public const string TagInitSeparator = "<p>";

		[SerializeField] private GameObject Body;
		[SerializeField] private float Speed = 50;
        [SerializeField] private float Sensitivity = 7F;
		[SerializeField] private Vector3 ShiftFromCenter = new Vector3(0, 0.5f, 0);
		[SerializeField] private TextMeshProUGUI NetLife;
		[SerializeField] private TextMeshProUGUI UserName;
        		
		private float _rotationY = 0F;
		private Vector3 _forwardCamera = Vector3.zero;
		private bool _enableMovement = true;
		private Color _color;
		private int _life = 100;
		private string _nameAssetToCreate;
		private GameObject _assetToPlace;
		private Vector3 _positionPlacement;
		private Camera _camera;
		private Collider _collider;
		private Rigidbody _rigidBody;

		public GameObject GetGameObject()
		{
			return this.gameObject;
		}

		public bool IsOwner()
		{
			bool shouldBeOwner = true;
#if ENABLE_NETWORK
			shouldBeOwner = NetworkGameIDView.AmOwner();
#endif
			return shouldBeOwner;
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
#endif
		public Color PlayerColor
		{
			get {return _color;}
			set {
				_color = value;
				SetInitData(Utilities.PackColor(_color));
			}
		}

		public Vector3 PositionCamera 
		{ 
			get { return _camera.transform.position; } 
			set { _camera.transform.position = value; } 
		}
		public Vector3 ForwardCamera 
		{
			get { return _camera.transform.forward; } 
			set { _camera.transform.forward = value; } 
		}
		public Vector3 PositionBase
		{ 
			get { 
				RaycastHit hitCollision = new RaycastHit();
				Vector3 collidedFloor = RaycastingTools.GetRaycastOriginForward(this.transform.position, Vector3.down, ref hitCollision, 100, NetworkedSessionController.Instance.LayerFloor);
				return collidedFloor + new Vector3(0, transform.localScale.y, 0); 
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


		void Awake()
		{
			_collider = this.GetComponent<Collider>();
			_rigidBody = this.GetComponent<Rigidbody>();

			_collider.isTrigger = true;
			_rigidBody.useGravity = false;
			_rigidBody.isKinematic = true;
		}

		void Start()
		{
			SystemEventController.Instance.Event += OnSystemEvent;
			SystemEventController.Instance.DispatchSystemEvent(EventPlayerAvatarHasStarted, this.gameObject);
			SystemEventController.Instance.DispatchSystemEvent(CameraXRController.EventCameraPlayerReadyForCamera, this);

			bool shouldRun = true;
#if ENABLE_NETWORK			
			NetworkGameIDView.InitedEvent += OnInitDataEvent;
#if ENABLE_MIRROR			
			NetworkGameIDView.RefreshAuthority();
#endif			
			NetworkController.Instance.NetworkEvent += OnNetworkEvent;
			if (NetworkGameIDView.AmOwner())
			{
				Body.SetActive(false);
#if ENABLE_AVATAR_OCULUS
				if (NetworkGameIDView.AvatarEntity != null)
				{
					VRInputController.Instance.AvatarEntity = NetworkGameIDView.AvatarEntity;
					VRInputController.Instance.AvatarEntity.InitFirstPersonLocalAvatar(NetworkGameIDView);
				}
#endif
			}
			else
			{
#if ENABLE_AVATAR_OCULUS
				if (NetworkGameIDView.AvatarEntity != null)
				{
					NetworkGameIDView.AvatarEntity.InitThirdPersonRemoteAvatar(NetworkGameIDView);
				}
#endif
				shouldRun = false;
			}
#else
			transform.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.None;
#endif			

			if (shouldRun)
			{
				Body.SetActive(false);
			}
		}

		void OnDestroy()
		{
#if ENABLE_NETWORK						
			NetworkGameIDView.InitedEvent -= OnInitDataEvent;
			if (NetworkController.Instance != null) NetworkController.Instance.NetworkEvent -= OnNetworkEvent;
#endif			
			if (SystemEventController.Instance != null) SystemEventController.Instance.Event -= OnSystemEvent;			
		}

		public void SetInitData(string initializationData)
		{
#if ENABLE_NETWORK			
			NetworkGameIDView.InitialInstantiationData = initializationData + TagInitSeparator + NetworkedSessionController.Instance.LocalUserName;
#endif			
			_life = 100;
			if (NetLife != null) NetLife.text = _life.ToString();
		}

		public void OnInitDataEvent(string initializationData)
		{
			string[] initData = initializationData.Split(new string[] { TagInitSeparator }, StringSplitOptions.None);

			PlayerColor = Utilities.UnpackColor(initData[0]);
			Utilities.ApplyColor(Body.transform, PlayerColor);
			if (NetLife != null) NetLife.text = _life.ToString();

			string userName = initData[1];
			if (UserName != null) UserName.text = userName;
		}

		public void ActivatePhysics(bool activation, bool force = false)
		{
#if ENABLE_NETWORK			
			if (NetworkGameIDView.AmOwner())
			{
				_collider.isTrigger = !activation;
				_rigidBody.useGravity = activation;
				_rigidBody.isKinematic = !activation;
			}
			else
			{
				_collider.isTrigger = true;
				_rigidBody.useGravity = false;
				_rigidBody.isKinematic = true;				
			}
#else
			_collider.isTrigger = !activation;
			_rigidBody.useGravity = activation;
			_rigidBody.isKinematic = !activation;
#endif			
		}

#if ENABLE_NETWORK
		private void OnNetworkEvent(string nameEvent, int originNetworkID, int targetNetworkID, object[] parameters)
		{
			if (nameEvent.Equals(EventPlayerAvatarNetworkImpact))
			{
				int viewIdImpactedPlayer = (int)parameters[0];
				if (NetworkGameIDView.AmOwner() && (NetworkGameIDView.GetViewID() == viewIdImpactedPlayer))
				{
					_life -= 10;
					NetworkController.Instance.DispatchNetworkEvent(EventPlayerAvatarNetworkUpdateLife, -1, -1, viewIdImpactedPlayer, _life);
				}
			}
			if (nameEvent.Equals(EventPlayerAvatarNetworkUpdateLife))
			{
				int viewIdImpactedPlayer = (int)parameters[0];
				if (NetworkGameIDView.GetViewID() == viewIdImpactedPlayer)
				{
					_life = (int)parameters[1];
					if (NetLife != null) NetLife.text = _life.ToString();
				}
			}
			if (nameEvent.Equals(EventPlayerAvatarNetworkSetUsername))
			{
				int viewIdPlayerNameChanged = (int)parameters[0];
				if (NetworkGameIDView.GetViewID() == viewIdPlayerNameChanged)
				{
					if (UserName != null) UserName.text = (string)parameters[1];
				}
			}
		}
#endif		

		private void OnSystemEvent(string nameEvent, object[] parameters)
		{
			if (nameEvent.Equals(CameraXRController.EventCameraResponseToPlayer))
			{
				_camera = (Camera)parameters[0];
			}
			if (nameEvent.Equals(EventPlayerAvatarEnableMovement))
			{
				_enableMovement = (bool)parameters[0];
			}
			if (nameEvent.Equals(ScreenBuilder.EventScreenBuilderBuildObject))
			{
				bool shouldCreateAsset = true;
#if ENABLE_NETWORK					
				shouldCreateAsset = NetworkGameIDView.AmOwner();
#endif				
				if (shouldCreateAsset)
				{
					ItemMultiObjectEntry itemToCreate = (ItemMultiObjectEntry)parameters[0];
					_nameAssetToCreate = (string)itemToCreate.Objects[2];
					_assetToPlace = AssetBundleController.Instance.CreateGameObject(_nameAssetToCreate);
					_assetToPlace.transform.parent = NetworkedSessionController.Instance.Level.transform;
				}
			}		
			if (nameEvent.Equals(SystemEventController.EventSystemEventControllerDontDestroyOnLoad))	
			{
				DontDestroyOnLoad(this.gameObject);
			}
		}

		private void CheckUserInput()
		{
			bool shouldRun = true;			
#if ENABLE_NETWORK
			shouldRun = NetworkGameIDView.AmOwner();
#endif			
			if (shouldRun)
			{
#if (ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR)
				Vector3 positionCurrentController = VRInputController.Instance.VRController.CurrentController.transform.position;
				Vector3 forwardCurrentController = VRInputController.Instance.VRController.CurrentController.transform.forward;

				RaycastHit hitPositionData = new RaycastHit();
				Vector3 collidedPositionRaycast = RaycastingTools.GetRaycastOriginForward(positionCurrentController, forwardCurrentController, ref hitPositionData, 100);
				if (collidedPositionRaycast != Vector3.zero)
				{
					NetworkedSessionController.Instance.RaycastPositionBall.transform.position = collidedPositionRaycast;
				}
#else
				Vector3 positionCurrentController = Camera.main.transform.position;
				Vector3 forwardCurrentController = Camera.main.transform.forward;
#endif

				if (NetworkedSessionController.Instance.AppInputController.ActionPrimaryDown())
				{
					string packedColor = Utilities.PackColor(_color);

					if (NetworkedSessionController.Instance.AppInputController.ActionSecondary())
					{
						// CODE TO TEST THE CREATION OF A BULLET
#if ENABLE_NETWORK
						string packedPosition = Utilities.SerializeVector3(positionCurrentController);
						string packedForward = Utilities.SerializeVector3(forwardCurrentController);
						NetworkController.Instance.DispatchNetworkEvent(EventPlayerAvatarRequestShootBullet, -1, -1, NetworkController.Instance.UniqueNetworkID, packedColor, packedPosition, packedForward);
#else
						NetworkedSessionController.Instance.CreateBullet(-1,-1, _color, positionCurrentController, forwardCurrentController);
#endif						
					}
					else
					{
						if (_assetToPlace != null)
						{
							GameObject.Destroy(_assetToPlace);
							_assetToPlace = null;
#if ENABLE_NETWORK							
							NetworkController.Instance.DispatchNetworkEvent(NetworkedSessionController.EventNetworkedBasicSessionRequestCreateNewAsset, -1, -1, _nameAssetToCreate, _positionPlacement);
#else
							NetworkedSessionController.Instance.CreateNewAsset(_nameAssetToCreate, _positionPlacement);
#endif							
						}
						else
						{
							SystemEventController.Instance.DispatchSystemEvent(NetworkedSessionController.EventNetworkedBasicSessionUserInputTriggered);
						}
					}
				}
			}

			if (NetLife != null)
			{
#if (ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR)
				NetLife.transform.parent.LookAt(VRInputController.Instance.VRController.HeadController.transform.position);
#else
				NetLife.transform.parent.LookAt(Camera.main.transform.position);
#endif
			}

			if (UserName != null)
			{
#if (ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR)
				UserName.transform.parent.LookAt(VRInputController.Instance.VRController.HeadController.transform.position);
#else
				UserName.transform.parent.LookAt(Camera.main.transform.position);
#endif
			}

			if (_assetToPlace != null)
			{
				RaycastHit hitData = new RaycastHit();
#if (ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR)
				Vector3 positionController = VRInputController.Instance.VRController.CurrentController.transform.position;
				Vector3 forwardController = VRInputController.Instance.VRController.CurrentController.transform.forward;
				_positionPlacement = RaycastingTools.GetRaycastOriginForward(positionController, forwardController, ref hitData, 10000, NetworkedSessionController.Instance.LayerFloor);
#else
				_positionPlacement = RaycastingTools.GetMouseCollisionPoint(Camera.main, ref hitData, NetworkedSessionController.Instance.LayerFloor);
#endif
				if (_positionPlacement != Vector3.zero)
				{
					_assetToPlace.transform.position = _positionPlacement;
				}
			}
		}

		private void Move()
        {
			float axisVertical = Input.GetAxis("Vertical");
			float axisHorizontal = Input.GetAxis("Horizontal");
			Vector3 forward = axisVertical * Camera.main.transform.forward * Speed * Time.deltaTime;
			Vector3 lateral = axisHorizontal * Camera.main.transform.right * Speed * Time.deltaTime;
			Vector3 increment = forward + lateral;
			increment.y = 0;
			transform.GetComponent<Rigidbody>().MovePosition(transform.position + increment);
        }

        private void RotateCamera()
        {
			float rotationX = Camera.main.transform.transform.localEulerAngles.y + Input.GetAxis("Mouse X") * Sensitivity;
			_rotationY = _rotationY + Input.GetAxis("Mouse Y") * Sensitivity;
			_rotationY = Mathf.Clamp(_rotationY, -60, 60);
			Quaternion rotation = Quaternion.Euler(-_rotationY, rotationX, 0);
			_forwardCamera = rotation * Vector3.forward;
			this.transform.forward = new Vector3(_forwardCamera.x, 0, _forwardCamera.z);
        }

		public void Logic()
		{
			CheckUserInput();

#if !(ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR)
			if (_enableMovement)
			{
				bool runLogic = true;
#if ENABLE_NETWORK					
				runLogic = NetworkGameIDView.AmOwner();
#endif
				if (runLogic)
				{
					Move();
					RotateCamera();
					Camera.main.transform.position = this.transform.position + ShiftFromCenter;
					Camera.main.transform.forward = _forwardCamera;
				}
			}
#endif			
		}
	}
}
