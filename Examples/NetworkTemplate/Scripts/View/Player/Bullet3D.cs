using System.Collections;
using System.Collections.Generic;
using yourvrexperience.Utils;
using UnityEngine;
#if ENABLE_NETWORK
using yourvrexperience.Networking;
#endif

namespace yourvrexperience.VR
{
	public class Bullet3D : MonoBehaviour
	{
		public const float SpeedBullet = 50;
		public const float LifeBullet = 4;

		public MeshRenderer Background;

		private int _id = -1;
		private int _owner = -1;
		private Vector3 _direction = Vector3.zero;

		private float _timer = 0;

		public int Id 
		{
			get { return _id; }
		}
		public int Owner 
		{
			get { return _owner; }
		}

		public void Initialize(int id, int owner, Vector3 position, Vector3 forward, Color color)
		{
			_id = id;
#if ENABLE_NETWORK							
			_owner = owner;			
#else
			_owner = 0;
#endif			
			this.transform.position = position;
			_direction = forward;
			Background.material.color = color;
		}

		void OnTriggerEnter(Collider other)
		{
			PlayerAvatar avatarCollided = other.gameObject.GetComponent<PlayerAvatar>();
			if (avatarCollided != null)
			{
#if ENABLE_NETWORK				
				if (avatarCollided.NetworkGameIDView.GetOwnerID() != _owner)
				{
					if (NetworkController.Instance.UniqueNetworkID != _owner)
					{
						NetworkController.Instance.DispatchNetworkEvent(PlayerAvatar.EventPlayerAvatarNetworkImpact, -1, -1, avatarCollided.NetworkGameIDView.GetViewID(), _id);
					}
				}
#endif				
			}
		}

		void Update()
		{
			if (_owner != -1)
			{
				_timer += Time.deltaTime;
				if (_timer < LifeBullet)
				{
					Vector3 increment = _direction * SpeedBullet * Time.deltaTime;
					this.transform.position += increment;
				}
				else
				{
					_owner = -1;
					GameObject.Destroy(this.gameObject);
				}
			}
		}
	}

}
