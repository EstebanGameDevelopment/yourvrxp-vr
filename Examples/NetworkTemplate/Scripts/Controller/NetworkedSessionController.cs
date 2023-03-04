using System;
using System.Collections;
using System.Collections.Generic;
#if ENABLE_NETWORK
using yourvrexperience.Networking;
#endif
using yourvrexperience.Utils;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace yourvrexperience.VR
{
	public class NetworkedSessionController : MonoBehaviour
	{
		public const string EventNetworkedBasicSessionExit = "EventNetworkedBasicSessionExit";
		public const string EventNetworkedBasicSessionConnect = "EventNetworkedBasicSessionConnect";
		public const string EventNetworkedBasicSessionJoinRoom = "EventNetworkedBasicSessionJoinRoom";
		public const string EventNetworkedBasicSessionSettings = "EventNetworkedBasicSessionSettings";
		public const string EventNetworkedBasicSessionChangeState = "EventNetworkedBasicSessionChangeState";
		public const string EventNetworkedBasicSessionLoadedBundleCompleted = "EventNetworkedBasicSessionLoadedBundleCompleted";
		public const string EventNetworkedBasicSessionRestorePlayerMovement = "EventNetworkedBasicSessionRestorePlayerMovement";
		public const string EventNetworkedBasicSessionRequestCreateNewAsset = "EventNetworkedBasicSessionRequestCreateNewAsset";
		public const string EventNetworkedBasicSessionResponseCreationNewAsset = "EventNetworkedBasicSessionResponseCreationNewAsset";
		public const string EventNetworkedBasicSessionConnectToLobby = "EventNetworkedBasicSessionConnectToLobby";
		public const string EventNetworkedBasicSessionUserInputTriggered = "EventNetworkedBasicSessionUserInputTriggered";
		public const string EventNetworkedBasicSessionResumeInSessionState = "EventNetworkedBasicSessionResumeInSessionState";
		public const string EventNetworkedBasicSessionResumeDisconnect = "EventNetworkedBasicSessionResumeDisconnect";
		public const string EventNetworkedBasicSessionLoadLevel = "EventNetworkedBasicSessionLoadLevel";

		public const string DisconnectScene = "DisconnectScene";

		public const string UserNameStored = "UserNameStored";
		public const string HostAddressStored = "HostAddressStored";
		public const string NameRoomStored = "NameRoomStored";
		public const string ClientNumberStored = "ClientNumberStored";

		public enum StatesNetworkApp { None = 0, MainMenu, Loading, Connecting, InSession, InPause, ExitSession }

        private static NetworkedSessionController _instance;

        public static NetworkedSessionController Instance
        {
            get
            {
                if (!_instance)
                {
                    _instance = GameObject.FindObjectOfType(typeof(NetworkedSessionController)) as NetworkedSessionController;
                }
				return _instance;
            }
        }

		[SerializeField] private string[] LevelScenes;
		[SerializeField] private GameObject[] LevelPrefab;
		[SerializeField] private GameObject PlayerDesktopAvatarPrefab;
		[SerializeField] private GameObject PlayerVRAvatarPrefab;
		[SerializeField] private GameObject PlayerHandLeftPrefab;
		[SerializeField] private GameObject PlayerHandRightPrefab;
		[SerializeField] private GameObject BulletPrefab;
		[SerializeField] private GameObject RaycastPositionPrefab;
		public Vector3 DistanceMenus = new Vector3(0, 0, 2.2f);

		private IInputController _inputController;
		private bool _hasStartedSession = false;
		private bool _isHost = false;
		private bool _changeStateRequested = false;
		private bool _initedInputController = false;
		private bool _initedScreenController = false;
		private PlayerAvatar _localPlayer;
		private List<PlayerAvatar> _players = new List<PlayerAvatar>();
		private int _counterBullet = 0;

		private GameObject _level;
		private int _currentLevel = 0;
		private int _previousLevel = -1;

		private StatesNetworkApp _state = StatesNetworkApp.None;
		private int _stateIterator = 0;
		private float _stateTimer = 0;

		private string _localUserName;
		private string _hostAddress;
		private string _roomName;
		private int _numberClients;
		private GameObject _raycastPositionBall;

		private StatesGameObject[] _stateObjects;

		private List<string> _assetBundleObjects = new List<string>() { "Car_1", "Car_2", "Bike_1", "Bike_2", "Bike_3" };
		private int _layerFloor;
		private bool _firstTimeEnterInSession = false;
		private int _uniqueAssetIDCounter = 0;
		private bool _isAdditiveLevelLoaded = false;

		private RobotSphere _robotControlled;

		public IInputController AppInputController
		{
			get { return _inputController; }
		}

		public string LocalUserName
		{
			get { return _localUserName; }
		}
		public PlayerAvatar LocalPlayer
		{
			get { return _localPlayer; }
		}
		public int LayerFloor
		{
			get { return _layerFloor; }
		}
		public GameObject Level
		{
			get { return _level; }
		}
		public GameObject RaycastPositionBall
		{
			get { return _raycastPositionBall; }
		}
		public int CurrentLevel
		{
			get { return _currentLevel; }
		}
		
		void Awake()
		{
			SystemEventController.Instance.Event += OnSystemEvent;
		}

		void Start()
		{
			_layerFloor = LayerMask.GetMask("Floor");
			_localUserName = PlayerPrefs.GetString(UserNameStored, "");
			UIEventController.Instance.Event += OnUIEvent;
		}

		void OnDestroy()
		{
#if ENABLE_NETWORK
			if (NetworkController.Instance != null) NetworkController.Instance.NetworkEvent -= OnNetworkEvent;
#endif			
			if (UIEventController.Instance != null) UIEventController.Instance.Event -= OnUIEvent;
			if (SystemEventController.Instance != null) SystemEventController.Instance.Event -= OnSystemEvent;
		}

		private void Initilization()
		{
			if (_initedScreenController && _initedInputController && _state == StatesNetworkApp.None)
			{
#if ENABLE_NETWORK
				NetworkController.Instance.Initialize();
				NetworkController.Instance.NetworkEvent += OnNetworkEvent;
#endif			
				ChangeState(StatesNetworkApp.MainMenu);
			}
		}

		public string GetHostName()
		{ 
			return PlayerPrefs.GetString(HostAddressStored, "127.0.0.1");
		}
		public void SetHostName(string hostName)
		{
			_hostAddress = hostName;
#if ENABLE_NETWORK			
#if ENABLE_MIRROR || ENABLE_NETCODE
			NetworkController.Instance.ServerAddress = _hostAddress;			
#endif
#endif			
			if (_hostAddress != null)
			{
				PlayerPrefs.SetString(HostAddressStored, _hostAddress);
			}			
		}
		public string GetRoomName()
		{
			return PlayerPrefs.GetString(NameRoomStored, "RoomName");
		}
		public void SetRoomName(string roomName)
		{
			_roomName = roomName;
			if (_roomName != null)
			{
				PlayerPrefs.SetString(NameRoomStored, _roomName);
			}	
		}
		public int GetNumberClients()
		{
			return PlayerPrefs.GetInt(ClientNumberStored, 2);	
		}
		public void SetNumberClients(int numberClients)
		{
			_numberClients = numberClients;
			PlayerPrefs.SetInt(ClientNumberStored, _numberClients);
		}

		public int GetTotalLevels()
		{
			if (LevelScenes.Length > 0)
			{
				return LevelScenes.Length;
			}
			else
			{
				return LevelPrefab.Length;
			}			
		}

		public GameObject CreateNewAsset(string nameAsset, Vector3 posAsset)
		{
			GameObject assetVehicle = AssetBundleController.Instance.CreateGameObject(nameAsset);
			assetVehicle.transform.position = posAsset;
			assetVehicle.transform.parent = _level.transform;
#if !ENABLE_NETWORK
			_stateObjects = null;
#endif
			return assetVehicle;
		}

		public bool InteractWithStatesGameObject(GameObject target)
		{
			if (_stateObjects == null)
			{
				_stateObjects = GameObject.FindObjectsOfType<StatesGameObject>();
			}
			foreach (StatesGameObject item in _stateObjects)
			{
				if (item.CheckInObject(target))
				{
					item.State = (item.State + 1) % item.StatesLength();
					return true;
				}
			}
			return false;
		}

		private void RestoreStateBeforeOpenConfiguration()
		{
			ScreenController.Instance.DestroyScreens();
#if ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR								
			VRInputController.Instance.DispatchVREvent(VRInputController.EventVRInputControllerEnableLocomotion, true);
#else
			SystemEventController.Instance.DispatchSystemEvent(PlayerAvatar.EventPlayerAvatarEnableMovement, true);
#endif			
		}

		public void CreateBullet(int bulletID, int playerOwnerID, Color color, Vector3 position, Vector3 forward)
		{
			GameObject bullet = Instantiate(BulletPrefab);
			bullet.GetComponent<Bullet3D>().Initialize(bulletID, playerOwnerID, position, forward, color);
		}

		private void UpdateLevel(int previousScene)
		{
			_stateObjects = null;
			if (LevelScenes.Length > 0)
			{
				_isAdditiveLevelLoaded = true;
#if ENABLE_NETWORK
				string namePrevious = ((previousScene != -1)?LevelScenes[previousScene]:"");
				NetworkController.Instance.LoadNewScene(LevelScenes[_currentLevel], namePrevious);
#else
				if (previousScene != -1) SceneManager.UnloadSceneAsync(LevelScenes[previousScene]);
				SceneManager.LoadScene(LevelScenes[_currentLevel], LoadSceneMode.Additive);
#endif
			}
			else
			{
				if (_level) GameObject.Destroy(_level);
				_level = Instantiate(LevelPrefab[_currentLevel]);
			}
		}

		private void OnSystemEvent(string nameEvent, object[] parameters)
		{
			if (nameEvent.Equals(SystemEventController.EventSystemEventControllerDontDestroyOnLoad))
			{
				if (Instance)
				{
					if (_raycastPositionBall != null)
					{
						DontDestroyOnLoad(_raycastPositionBall);
					}
					DontDestroyOnLoad(Instance.gameObject);
				}
			}
			if (nameEvent.Equals(ScreenController.EventScreenControllerStarted))
			{
				_initedScreenController = true;
				Initilization();
			}
			if (nameEvent.Equals(InputController.EventInputControllerHasStarted))
			{
				_initedInputController = true;
				_inputController = ((GameObject)parameters[0]).GetComponent<IInputController>();
				_inputController.Initialize();
				Initilization();
			}
			if (nameEvent.Equals(LevelContentReporter.EventLevelContentReporterStarted))
			{
				_level = (GameObject)parameters[0];
			}
			if (nameEvent.Equals(EventNetworkedBasicSessionLoadedBundleCompleted))
			{
#if ENABLE_NETWORK
				if ((_state != StatesNetworkApp.Connecting) && (_state != StatesNetworkApp.InSession))
				{
					ChangeState(StatesNetworkApp.Connecting);
					NetworkController.Instance.Connect();
				}
#else
				if (_state != StatesNetworkApp.InSession)
				{
					ScreenController.Instance.DestroyScreens();
					_currentLevel = 0;
					_previousLevel = -1;
					UpdateLevel(_previousLevel);
					ChangeState(StatesNetworkApp.InSession);
#if ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR			
					_localPlayer = (Instantiate(PlayerVRAvatarPrefab) as GameObject).GetComponent<PlayerAvatar>();
#else
					_localPlayer = (Instantiate(PlayerDesktopAvatarPrefab) as GameObject).GetComponent<PlayerAvatar>();
#endif
					_localPlayer.gameObject.transform.position = new Vector3(0, 1, 0);
				}
#endif				
			}
#if ENABLE_NETWORK			
			if (nameEvent.Equals(EventNetworkedBasicSessionConnectToLobby))
			{
				if (!NetworkController.Instance.IsInLobby)
				{
					string titleInfo = LanguageController.Instance.GetText("text.info");
					string textNowExiting = LanguageController.Instance.GetText("screen.main.connecting.to.lobby");
					ScreenInformationView.CreateScreenInformation(ScreenInformationView.ScreenLoading, this.gameObject, titleInfo, textNowExiting);
					NetworkController.Instance.Connect();
				}
				else
				{
					ScreenController.Instance.CreateForwardScreen(ScreenListRoomsSession.ScreenName, DistanceMenus, false, true);
				}		
			}
#endif			
			if (nameEvent.Equals(EventNetworkedBasicSessionRestorePlayerMovement))
			{
				SystemEventController.Instance.DispatchSystemEvent(PlayerAvatar.EventPlayerAvatarEnableMovement, true);
			}
			if (nameEvent.Equals(PlayerAvatar.EventPlayerAvatarHasStarted))
			{
				GameObject newPlayerGO = (GameObject)parameters[0];
				if (!_players.Contains(newPlayerGO.GetComponent<PlayerAvatar>()))
				{
					_players.Add(newPlayerGO.GetComponent<PlayerAvatar>());
				}
#if ENABLE_NETWORK				
				if (newPlayerGO.GetComponent<INetworkObject>().NetworkGameIDView.AmOwner())
				{
					_localPlayer = newPlayerGO.GetComponent<PlayerAvatar>();
					if (NetworkController.Instance.IsServer)
					{
						_localPlayer.PlayerColor = Color.red;
					}
					else
					{
						_localPlayer.PlayerColor = Color.green;
					}
#if ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR
					NetworkController.Instance.CreateNetworkPrefab(false, PlayerHandLeftPrefab.name, PlayerHandLeftPrefab.gameObject, "Player\\" + PlayerHandLeftPrefab.name, new Vector3(0, 0, 0), Quaternion.identity, 0);
					NetworkController.Instance.CreateNetworkPrefab(false, PlayerHandRightPrefab.name, PlayerHandRightPrefab.gameObject, "Player\\" + PlayerHandRightPrefab.name, new Vector3(0, 0, 0), Quaternion.identity, 0);
#endif					
				}

#else
				_localPlayer = newPlayerGO.GetComponent<PlayerAvatar>();
#endif				
			}	
			if (nameEvent.Equals(PlayerHand.EventPlayerHandHasStarted))
			{
				PlayerHand playerHand = (PlayerHand)parameters[0];
				playerHand.Player = _localPlayer;				
#if ENABLE_NETWORK
				playerHand.PlayerColor = _localPlayer.PlayerColor;
#endif
			}
			if (nameEvent.Equals(ScreenBuilder.EventScreenBuilderBuildObject))
			{
				RestoreStateBeforeOpenConfiguration();
			}
			if (nameEvent.Equals(EventNetworkedBasicSessionUserInputTriggered))
			{
				if (GameObject.FindObjectOfType<BaseScreenView>() == null)
				{
					if (_robotControlled != null)
					{
						if (_raycastPositionBall != null)
						{
							if (!_raycastPositionBall.activeSelf)
							{
								_raycastPositionBall.SetActive(true);
							}
						}
						_robotControlled.ToggleControl();
					}
					else
					{
						RaycastHit hitObjectData = new RaycastHit();
#if (ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR)
						Vector3 positionController = VRInputController.Instance.VRController.CurrentController.transform.position;
						Vector3 forwardController = VRInputController.Instance.VRController.CurrentController.transform.forward;
						GameObject hitObject = RaycastingTools.GetRaycastObject(positionController, forwardController, 1000, ref hitObjectData);
#else
						GameObject hitObject = RaycastingTools.GetMouseCollisionObject(Camera.main, ref hitObjectData);
#endif
						if (hitObject != null)
						{
							if (!InteractWithStatesGameObject(hitObject))
							{
								if (_robotControlled == null)
								{
									_robotControlled = hitObject.GetComponent<RobotSphere>();
									if (_robotControlled != null)
									{
										if (_raycastPositionBall != null)
										{
											if (_raycastPositionBall.activeSelf)
											{
												_raycastPositionBall.SetActive(false);
											}
										}
										_robotControlled.ToggleControl();
									}
								}
							}
						}
					}
				}
			}
			if (nameEvent.Equals(RobotSphere.EventRobotSphereReleasedControlConfirmed))
			{
				_robotControlled = null;
			}
			if (nameEvent.Equals(BaseScreenView.EventBaseScreenViewCreated))
			{
				if (_raycastPositionBall != null)
				{
					_raycastPositionBall.SetActive(false);
				}
			}
			if (nameEvent.Equals(BaseScreenView.EventBaseScreenViewDestroyed))
			{
				if (_raycastPositionBall != null)
				{
					if (ScreenController.Instance.GetTotalNumberScreens() <= 1)
					{
						_raycastPositionBall.SetActive(true);
					}					
				}
			}		
			if (nameEvent.Equals(EventNetworkedBasicSessionLoadLevel))
			{
				int nextLevel = (int)parameters[0];
				if (_currentLevel != nextLevel)
				{
					if (nextLevel < GetTotalLevels())
					{
#if ENABLE_NETWORK
						NetworkController.Instance.DispatchNetworkEvent(EventNetworkedBasicSessionLoadLevel, -1, -1, nextLevel);
#else
						_previousLevel = _currentLevel;
						_currentLevel = nextLevel;
						UpdateLevel(_previousLevel);
						UIEventController.Instance.DispatchUIEvent(NetworkedSessionController.EventNetworkedBasicSessionResumeInSessionState);
#endif							
					}					
				}
			}	
		}

		private void OnUIEvent(string nameEvent, object[] parameters)
		{
			if (nameEvent.Equals(EventNetworkedBasicSessionConnect))
			{				
#if ENABLE_NETWORK				
				_isHost = (bool)parameters[0];
				SetHostName((string)parameters[1]);
				SetRoomName((string)parameters[2]);
				if (_isHost)
				{
					SetNumberClients((int)parameters[3]);
				}
#endif			
				ChangeState(StatesNetworkApp.Loading);
			}
#if ENABLE_NETWORK							
			if (nameEvent.Equals(EventNetworkedBasicSessionJoinRoom))
			{
				_isHost = false;
				SetRoomName((string)parameters[0]);
				NetworkController.Instance.JoinRoom(_roomName);
				ChangeState(StatesNetworkApp.Loading);
			}
#endif			
			if (nameEvent.Equals(EventNetworkedBasicSessionResumeInSessionState))
			{
				if (_state == StatesNetworkApp.InPause)
				{
					ChangeState(StatesNetworkApp.InSession);
				}
				else
				{
					RestoreStateBeforeOpenConfiguration();
				}				
			}
			if (nameEvent.Equals(EventNetworkedBasicSessionResumeDisconnect))
			{
				ChangeState(StatesNetworkApp.ExitSession);
			}
			if (nameEvent.Equals(ScreenSettingsSession.EventScreenSettingsSessionName))
			{
				_localUserName = (string)parameters[0];
				PlayerPrefs.SetString(UserNameStored, _localUserName);
#if ENABLE_NETWORK				
				if (NetworkController.Instance.IsInRoom)
				{
					NetworkController.Instance.DispatchNetworkEvent(PlayerAvatar.EventPlayerAvatarNetworkSetUsername, -1, -1, _localPlayer.NetworkGameIDView.GetViewID(), _localUserName);
				}
#endif				
			}
		}

#if ENABLE_NETWORK
		protected virtual void OnNetworkEvent(string nameEvent, int originNetworkID, int targetNetworkID, object[] parameters)
		{
			if (nameEvent.Equals(NetworkController.EventNetworkControllerListRoomsConfirmedUpdated))
			{
				if (!_hasStartedSession)
				{
					_hasStartedSession = true;
					if (_isHost)
					{
						NetworkController.Instance.CreateRoom(_roomName, _numberClients);
					}
					else
					{
#if ENABLE_MIRROR || ENABLE_NETCODE
						NetworkController.Instance.JoinRoom(_roomName);
#else
						UIEventController.Instance.DispatchUIEvent(ScreenInformationView.EventScreenInformationDestroy);
						ScreenController.Instance.CreateForwardScreen(ScreenListRoomsSession.ScreenName, DistanceMenus, false, true);
#endif						
					}
				}
			}
			if (nameEvent.Equals(NetworkController.EventNetworkControllerConnectionWithRoom))
			{
				Utilities.DebugLogColor("JOINED ROOM WITH ID["+(int)parameters[0]+"]", Color.red);
				UpdateLevel(-1);
				ChangeState(StatesNetworkApp.InSession);
				if (_localPlayer == null)
				{					
#if ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR				
					NetworkController.Instance.CreateNetworkPrefab(false, PlayerVRAvatarPrefab.name, PlayerVRAvatarPrefab.gameObject, "Player\\" + PlayerVRAvatarPrefab.name, new Vector3(0, 1.04f, 0), Quaternion.identity, 0);
#else
					NetworkController.Instance.CreateNetworkPrefab(false, PlayerDesktopAvatarPrefab.name, PlayerDesktopAvatarPrefab.gameObject, "Player\\" + PlayerDesktopAvatarPrefab.name, new Vector3(0, 1.04f, 0), Quaternion.identity, 0);
#endif					
				}
			}
			if (nameEvent.Equals(NetworkController.EventNetworkControllerNewPlayerJoinedRoom))
			{
				Utilities.DebugLogColor("NEW PLAYER["+(int)parameters[0]+"] JOINED TO THE ROOM", Color.red);
			}
			if (nameEvent.Equals(NetworkController.EventNetworkControllerPlayerDisconnected))
			{
				int netIDDisconnected = -1;
				if (parameters != null)
				{
					if (parameters.Length > 0)
					{
						netIDDisconnected = (int)parameters[0];
					}
				}
				for (int i = 0; i < _players.Count; i++)
				{
					PlayerAvatar playerToDelete = _players[i];
					if (playerToDelete != null)
					{
						if (playerToDelete.NetworkGameIDView.GetOwnerID() == netIDDisconnected)
						{
							_players.RemoveAt(i);
							SystemEventController.Instance.DispatchSystemEvent(PlayerHand.EventPlayerHandDestroyedAvatar, playerToDelete);
							GameObject.Destroy(playerToDelete.gameObject);
							Utilities.DebugLogColor("PLAYER["+netIDDisconnected+"] SUCCESSFULLY DESTROYED", Color.red);
						}
					}
				}				
			}		
			if (nameEvent.Equals(EventNetworkedBasicSessionChangeState))
			{
				int newState = (int)parameters[0];
				ChangeLocalState((StatesNetworkApp)newState);
			}
			if (nameEvent.Equals(PlayerAvatar.EventPlayerAvatarRequestShootBullet))
			{
				if (NetworkController.Instance.IsServer)
				{
					int playerOwnerBullet = (int)parameters[0];
					string colorBullet = (string)parameters[1];
					string positionBullet = (string)parameters[2];
					string forwardBullet = (string)parameters[3];
					NetworkController.Instance.DispatchNetworkEvent(PlayerAvatar.EventPlayerAvatarCreateShootBullet, -1, -1, _counterBullet++, playerOwnerBullet, colorBullet, positionBullet, forwardBullet);
				}
			}
			if (nameEvent.Equals(PlayerAvatar.EventPlayerAvatarCreateShootBullet))
			{
				int bulletID = (int)parameters[0];
				int playerOwnerBullet = (int)parameters[1];
				Color colorBullet = Utilities.UnpackColor((string)parameters[2]);
				Vector3 positionBullet = Utilities.DeserializeVector3((string)parameters[3]);
				Vector3 forwardBullet = Utilities.DeserializeVector3((string)parameters[4]);
				CreateBullet(bulletID, playerOwnerBullet, colorBullet, positionBullet, forwardBullet);
			}
			if (nameEvent.Equals(PlayerAvatar.EventPlayerAvatarNetworkImpact))
			{
				int bulletID = (int)parameters[1];
				Bullet3D[] bullets = GameObject.FindObjectsOfType<Bullet3D>();
				foreach (Bullet3D bullet in bullets)
				{
					if (bullet.Id == bulletID)
					{
						bullet.transform.parent = null;
						GameObject.Destroy(bullet.gameObject);
						return;
					}
				}
			}
			if (nameEvent.Equals(NetworkController.EventNetworkControllerDisconnected))
			{
				Bullet3D[] bullets = GameObject.FindObjectsOfType<Bullet3D>();
				foreach (Bullet3D bullet in bullets)
				{
					if (bullet != null) 
					{
						GameObject.Destroy(bullet.gameObject);
					}
				}
				for (int i = 0; i < _players.Count; i++)
				{
					PlayerAvatar playerToDelete = _players[i];
					if (playerToDelete != null)
					{
						SystemEventController.Instance.DispatchSystemEvent(PlayerHand.EventPlayerHandDestroyedAvatar, playerToDelete);
						GameObject.Destroy(playerToDelete.gameObject);
					}
				}				
			}
			if (nameEvent.Equals(EventNetworkedBasicSessionRequestCreateNewAsset))
			{
				if (NetworkController.Instance.IsServer)
				{
					_uniqueAssetIDCounter++;
					string nameAssetBundle = (string)parameters[0];
					GameObject assetCreated = CreateNewAsset(nameAssetBundle, (Vector3)parameters[1]);
					if (assetCreated != null)
					{
						NetworkAssetBundleObject networkAssetBundleObjectCreated = assetCreated.AddComponent<NetworkAssetBundleObject>();
						string uniqueNetAssetBundleIdentifier = nameAssetBundle + " " + _uniqueAssetIDCounter;
						networkAssetBundleObjectCreated.Initialize(uniqueNetAssetBundleIdentifier, nameAssetBundle);
						NetworkStatesGameObject.InitializeStatesGameObject(networkAssetBundleObjectCreated.gameObject, uniqueNetAssetBundleIdentifier);
						_stateObjects = null;
					}					
				}
			}
			if (nameEvent.Equals(NetworkAssetBundleObject.EventNetworkAssetBundleObjectCreated))
			{
				if (!NetworkController.Instance.IsServer)
				{
					string uidGO = (string)parameters[0];
					string nameAsset = (string)parameters[1];
					Vector3 posGO = (Vector3)parameters[2];
					GameObject assetCreated = CreateNewAsset(nameAsset, (Vector3)parameters[2]);
					if (assetCreated != null)
					{
						NetworkAssetBundleObject networkAssetBundleObjectCreated = assetCreated.AddComponent<NetworkAssetBundleObject>();
						networkAssetBundleObjectCreated.Initialize(uidGO, nameAsset);
						NetworkStatesGameObject.InitializeStatesGameObject(networkAssetBundleObjectCreated.gameObject, uidGO);
						_stateObjects = null;
					}
				}
			}
			if (nameEvent.Equals(EventNetworkedBasicSessionLoadLevel))
			{
				int nextLevel = (int)parameters[0];
				_previousLevel = _currentLevel;
				if (_currentLevel != nextLevel)
				{
					_currentLevel = nextLevel;
					if (_currentLevel < GetTotalLevels())
					{
						DestroyNetworkLevelObjects();
						ScreenController.Instance.DestroyScreens();
						string information = LanguageController.Instance.GetText("text.info");
						string welcomeDescription = LanguageController.Instance.GetText("in.level.loading.level");
						ScreenInformationView.CreateScreenInformation(ScreenInformationView.ScreenLoading, null, information, welcomeDescription);
						Invoke("DelayedConfirmationDestroyedNetworkLevelObjects", 1);
					}
				}
			}
		}

		private void DelayedConfirmationDestroyedNetworkLevelObjects()
		{
			ScreenController.Instance.DestroyScreens();
			UpdateLevel(_previousLevel);
			UIEventController.Instance.DispatchUIEvent(NetworkedSessionController.EventNetworkedBasicSessionResumeInSessionState);
		}

		private void DestroyNetworkLevelObjects()
		{
			NetworkObjectID[] networkObjects = GameObject.FindObjectsOfType<NetworkObjectID>();
			foreach (NetworkObjectID netObjectID in networkObjects)
			{
				if ((netObjectID != null) && (netObjectID.LinkedToCurrentLevel))
				{
					string nameToDestroy = netObjectID.name;
					netObjectID.Destroy();
				}
			}
		}

		private void ChangeRemoteState(int newState)
		{
			if (!_changeStateRequested)
			{
				_changeStateRequested = true;
				NetworkController.Instance.DispatchNetworkEvent(EventNetworkedBasicSessionChangeState, NetworkController.Instance.UniqueNetworkID, -1, newState);
			}
		}
#endif		

		private void DisplayPersonalConfigScreen()
		{
			if (GameObject.FindObjectOfType<ScreenConfigurationSession>() == null)
			{
#if ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR								
				VRInputController.Instance.DispatchVREvent(VRInputController.EventVRInputControllerEnableLocomotion, false);
#else
				SystemEventController.Instance.DispatchSystemEvent(PlayerAvatar.EventPlayerAvatarEnableMovement, false);
#endif					
				ScreenController.Instance.CreateScreen(ScreenConfigurationSession.ScreenName, true, false);
			}
			else
			{
				RestoreStateBeforeOpenConfiguration();
			}
		}

		private void ChangeState(StatesNetworkApp state)
		{
#if ENABLE_NETWORK
			switch (state)
			{
				case StatesNetworkApp.InSession:
				case StatesNetworkApp.InPause:
				case StatesNetworkApp.ExitSession:
					ChangeRemoteState((int)state);
					break;

				default:
					ChangeLocalState(state);
					break;
			}
#else
			ChangeLocalState(state);
#endif			
		}

		private void ChangeLocalState(StatesNetworkApp state)
		{
			_changeStateRequested = false;
			if (_state == state) return;

			_stateIterator = 0;
			_stateTimer = 0;

			_state = state;
			switch (_state)
			{
				case StatesNetworkApp.MainMenu:
#if ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR				
					VRInputController.Instance.DispatchVREvent(VRInputController.EventVRInputControllerResetToInitial, true);
#endif					
					ScreenController.Instance.CreateForwardScreen(ScreenNetworkSession.ScreenName, DistanceMenus, true, false);
					break;
				case StatesNetworkApp.Loading:
					ScreenController.Instance.CreateForwardScreen(ScreenLoadingSession.ScreenName, DistanceMenus, true, false);
					break;
				case StatesNetworkApp.Connecting:
					ScreenController.Instance.CreateForwardScreen(ScreenConnectingSession.ScreenName, DistanceMenus, true, false);
					break;
				case StatesNetworkApp.InSession:
#if ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR				
					if (_raycastPositionBall == null)
					{
						_raycastPositionBall = Instantiate(RaycastPositionPrefab);
					}
					VRInputController.Instance.DispatchVREvent(VRInputController.EventVRInputControllerEnableLocomotion, true);
#else
					SystemEventController.Instance.DispatchSystemEvent(PlayerAvatar.EventPlayerAvatarEnableMovement, true);
#endif					
					UIEventController.Instance.DispatchUIEvent(ScreenController.EventScreenControllerDestroyAllScreens);
					if (!_firstTimeEnterInSession)
					{
						_firstTimeEnterInSession = true;
						string information = LanguageController.Instance.GetText("text.info");
						string welcomeDescription = LanguageController.Instance.GetText("in.level.welcome.instructions");
						ScreenInformationView.CreateScreenInformation(ScreenInformationView.ScreenInformation, null, information, welcomeDescription, EventNetworkedBasicSessionRestorePlayerMovement);
						SystemEventController.Instance.DelaySystemEvent(PlayerAvatar.EventPlayerAvatarEnableMovement, 0.1f, false);
					}
#if ENABLE_NETWORK					
					NetworkController.Instance.DelayNetworkEvent(NetworkController.EventNetworkControllerClientLevelReady,  0.5f, -1, -1, !NetworkController.Instance.IsServer);
#endif					
					break;
				case StatesNetworkApp.InPause:
#if ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR								
					VRInputController.Instance.DispatchVREvent(VRInputController.EventVRInputControllerEnableLocomotion, false);
#else
					SystemEventController.Instance.DispatchSystemEvent(PlayerAvatar.EventPlayerAvatarEnableMovement, false);
#endif					
					ScreenController.Instance.CreateScreen(ScreenPauseSession.ScreenName, true, false);
					break;
				case StatesNetworkApp.ExitSession:
					ScreenController.Instance.CreateScreen(ScreenExitingSession.ScreenName, true, false);
					break;
			}
		}

		void Update()
		{
			if (_stateIterator < 100) _stateIterator++;

			bool pressedPause = false;
			switch (_state)
			{
#if UNITY_EDITOR				
				case StatesNetworkApp.MainMenu:
					if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.H))
					{
						UIEventController.Instance.DispatchUIEvent(EventNetworkedBasicSessionConnect, true);
					}
					if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.J))
					{
						UIEventController.Instance.DispatchUIEvent(EventNetworkedBasicSessionConnect, false);
					}
					break;
#endif

				case StatesNetworkApp.InSession:
					foreach (PlayerAvatar player in _players)
					{
						player.Logic();
					}
					pressedPause = _inputController.ActionMenuPressed();
#if UNITY_EDITOR				
					pressedPause = pressedPause || Input.GetKeyDown(KeyCode.P);
#endif					

					if (_inputController.ActionSecondary() && pressedPause)
					{
						ChangeState(StatesNetworkApp.InPause);
					}
					else
					{
						if (pressedPause)
						{
							DisplayPersonalConfigScreen();
						}
					}
					break;

				case StatesNetworkApp.InPause:
					if (_inputController.ActionSecondary() && pressedPause)
					{
						ChangeState(StatesNetworkApp.InSession);
					}
					break;

				case StatesNetworkApp.ExitSession:
					switch (_stateIterator)
					{
						case 1:
							_stateTimer += Time.deltaTime;
							if (_stateTimer < 0.1)
							{
								_stateIterator = 0;
							}
							break;

						case 2:
							_stateTimer = 0;
#if ENABLE_NETWORK
							DestroyNetworkLevelObjects();
							NetworkController.Instance.Disconnect();
#endif
							break;

						case 3:
							_stateTimer += Time.deltaTime;
							if (_stateTimer < 0.25f)
							{
								_stateIterator = 2;
							}
							break;

						case 4:
							_stateTimer = 0;
#if ENABLE_NETWORK							
							DestroyNetworkLevelObjects();
#endif							
							if (_level != null) GameObject.Destroy(_level);
							if (LevelScenes.Length > 0)
							{
								if (_isAdditiveLevelLoaded)
								{
#if ENABLE_NETWORK
									NetworkController.Instance.LoadNewScene(NetworkController.DisconnectScene, LevelScenes[_currentLevel]);
#else
									if (LevelScenes[_currentLevel].Length > 0) SceneManager.UnloadSceneAsync(LevelScenes[_currentLevel]);
									SceneManager.LoadScene(DisconnectScene, LoadSceneMode.Additive);
#endif
								}
							}
							break;

						case 5:
							_stateTimer += Time.deltaTime;
							if (_stateTimer < 0.25f)
							{
								_stateIterator = 4;
							}
							break;

						case 6:
							_stateTimer = 0;
							if (_localPlayer != null)
							{
								GameObject.Destroy(_localPlayer.gameObject);
								_localPlayer = null;
							}
							_hasStartedSession = false;
							_isHost = false;
							_changeStateRequested = false;
							_firstTimeEnterInSession = false;
							_stateObjects = null;
							if (_raycastPositionBall != null)
							{
								GameObject.Destroy(_raycastPositionBall);
								_raycastPositionBall = null;
							}
							_currentLevel = 0;
							_previousLevel = -1;
							ChangeState(StatesNetworkApp.MainMenu);
							break;
					}
					break;
			}
		}
	}
}