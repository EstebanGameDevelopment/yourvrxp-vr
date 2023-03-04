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
	[RequireComponent(typeof(PatrolWaypoints))]
	[RequireComponent(typeof(Collider))]
	[RequireComponent(typeof(Rigidbody))]	
	public class RobotSphere : 
#if ENABLE_NETWORK	
	NetworkPrefab, INetworkObject
#else
	MonoBehaviour
#endif	
	{
		public const string EventRobotSphereTakeControl = "EventRobotSphereTakeControl";
		public const string EventRobotSphereReleaseControl = "EventRobotSphereReleaseControl";
		public const string EventRobotSphereReleasedControlConfirmed = "EventRobotSphereReleasedControlConfirmed";

		public const string TriggerAnimationIdle = "Idle";
		public const string TriggerAnimationWalk = "Walk";

		[SerializeField] private AnimatorSystem BodyAnimation;
		[SerializeField] private float Speed = 50;

		private PatrolWaypoints _patrolWaypoints;
		private bool _enabled = true;
        		
#if ENABLE_NETWORK					
		protected override void Start()
		{
			base.Start();

			if (!IsInLevel)
			{
				bool shouldActivate = true;
#if ENABLE_NETWORK
#if ENABLE_MIRROR
				if (NetworkController.Instance.IsServer)
				{
					NetworkGameIDView.RefreshAuthority();
				}				
#endif			
				NetworkGameIDView.InitedEvent += OnInitDataEvent;
				shouldActivate = NetworkGameIDView.AmOwner();
#endif			
				_patrolWaypoints = this.GetComponent<PatrolWaypoints>();
				_patrolWaypoints.MoveEvent += OnMoveEvent;
				_patrolWaypoints.StandEvent += OnStandEvent;
				if (!_patrolWaypoints.Activated)				
				{
					_patrolWaypoints.ActivatePatrol();
				}
			}
		}

		protected override void OnDestroy()
		{
			base.OnDestroy();

			if (_patrolWaypoints != null)
			{
				_patrolWaypoints.MoveEvent -= OnMoveEvent;
				_patrolWaypoints.StandEvent -= OnStandEvent;
			}
			if (!IsInLevel)
			{
#if ENABLE_NETWORK						
				NetworkGameIDView.InitedEvent -= OnInitDataEvent;
#endif			
			}
		}
#else
		void Start()
		{
			_patrolWaypoints = this.GetComponent<PatrolWaypoints>();
			_patrolWaypoints.MoveEvent += OnMoveEvent;
			_patrolWaypoints.StandEvent += OnStandEvent;
			if (!_patrolWaypoints.Activated)
			{
				_patrolWaypoints.ActivatePatrol();
			}
		}	

		void OnDestroy()
		{
			if (_patrolWaypoints != null)
			{
				_patrolWaypoints.MoveEvent -= OnMoveEvent;
				_patrolWaypoints.StandEvent -= OnStandEvent;
			}
		}			
#endif

		private void OnStandEvent()
		{
			BodyAnimation.ChangeAnimation(TriggerAnimationIdle);
		}

		private void OnMoveEvent()
		{
			BodyAnimation.ChangeAnimation(TriggerAnimationWalk);
		}

		public void SetInitData(string initializationData)
		{
#if ENABLE_NETWORK			
			NetworkGameIDView.InitialInstantiationData = initializationData;
#endif			
		}

		public void ActivatePhysics(bool activation, bool force = false)
		{
#if ENABLE_NETWORK			
			NetworkRigidBody.useGravity = activation;
			NetworkRigidBody.isKinematic = !activation;
			NetworkCollider.isTrigger = !activation;
#endif			
		}

		public void OnInitDataEvent(string initializationData)
		{
		}

		private bool ShouldRun()
		{
			bool shouldRun = true;
#if ENABLE_NETWORK			
			shouldRun = NetworkGameIDView.AmOwner();
#endif			
			return shouldRun;
		}

#if ENABLE_NETWORK
		protected override void OnNetworkEvent(string nameEvent, int originNetworkID, int targetNetworkID, object[] parameters)
		{
			base.OnNetworkEvent(nameEvent, originNetworkID, targetNetworkID, parameters);

			if (nameEvent.Equals(EventRobotSphereTakeControl))
			{
				int viewID = (int)parameters[0];
				if (viewID == NetworkGameIDView.GetViewID())
				{
					_enabled = false;
				}
			}
			if (nameEvent.Equals(EventRobotSphereReleaseControl))
			{
				int viewID = (int)parameters[0];
				if (viewID == NetworkGameIDView.GetViewID())
				{
					_enabled = true;
					SystemEventController.Instance.DispatchSystemEvent(EventRobotSphereReleasedControlConfirmed);
				}
			}			
		}
#endif		

		public void ToggleControl()
		{
			if (_enabled)
			{
				_enabled = false;
				_patrolWaypoints.DeactivatePatrol();
				BodyAnimation.ChangeAnimation(TriggerAnimationIdle);
#if ENABLE_NETWORK
				NetworkGameIDView.RequestAuthority();
				NetworkController.Instance.DispatchNetworkEvent(EventRobotSphereTakeControl, -1, -1, NetworkGameIDView.GetViewID());
#endif				
			}
			else
			{
				if (ShouldRun())
				{
					_enabled = true;
					_patrolWaypoints.ActivatePatrol();
					BodyAnimation.ChangeAnimation(TriggerAnimationWalk);
#if ENABLE_NETWORK					
					NetworkController.Instance.DispatchNetworkEvent(EventRobotSphereReleaseControl, -1, -1, NetworkGameIDView.GetViewID());
#else
					SystemEventController.Instance.DispatchSystemEvent(EventRobotSphereReleasedControlConfirmed);
#endif					
				}
			}
		}

		private void MoveToPosition()
		{
			RaycastHit hitData = new RaycastHit();
#if (ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR)
			Vector3 positionController = VRInputController.Instance.VRController.CurrentController.transform.position;
			Vector3 forwardController = VRInputController.Instance.VRController.CurrentController.transform.forward;
			Vector3 positionPlacement = RaycastingTools.GetRaycastOriginForward(positionController, forwardController, ref hitData, 10000, NetworkedSessionController.Instance.LayerFloor);
#else
			Vector3 positionPlacement = RaycastingTools.GetMouseCollisionPoint(Camera.main, ref hitData, NetworkedSessionController.Instance.LayerFloor);
#endif

			if (ShouldRun())
			{
				this.transform.position = positionPlacement + new Vector3(0,1,0);
			}
		}

		void Update()
		{
			if (_enabled)
			{
				if (ShouldRun())
				{
					if (_patrolWaypoints != null)
					{
						_patrolWaypoints.UpdateLogic();
					}					
				}				
			}
			else
			{
				MoveToPosition();
			}
		}
	}
}
