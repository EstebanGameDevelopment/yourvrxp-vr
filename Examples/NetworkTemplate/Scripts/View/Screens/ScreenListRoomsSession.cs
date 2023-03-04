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
	public class ScreenListRoomsSession : BaseScreenView, IScreenView
	{
		public const string ScreenName = "ScreenListRoomsSession";

		[SerializeField] private GameObject RoomItemPrefab;
		[SerializeField] private Button buttonJoinSession;
		[SerializeField] private Button buttonBack;

        private SlotManagerView _slotManager;
        private ItemMultiObjectEntry _selectedObject = null;

		public override string NameScreen 
		{ 
			get { return ScreenName; }
		}

		public override void Initialize(params object[] parameters)
		{
			base.Initialize(parameters);

			_content.Find("Title").GetComponent<TextMeshProUGUI>().text = LanguageController.Instance.GetText("screen.join.session.title");

#if ENABLE_NETWORK
			if (NetworkController.Instance.RoomsLobby.Count > 0)
			{
				_slotManager = _content.Find("ListItems").GetComponent<SlotManagerView>();
				List<ItemMultiObjectEntry> roomItems = new List<ItemMultiObjectEntry>();
				for (int i = 0; i < NetworkController.Instance.RoomsLobby.Count; i++)
				{
					ItemMultiObjectEntry data = new ItemMultiObjectEntry(NetworkController.Instance.RoomsLobby[i].NameRoom);
					if (_selectedObject == null) 
					{
						_selectedObject = data;
					}
					roomItems.Add(new ItemMultiObjectEntry(this.gameObject, i, data));
				}
				_slotManager.Initialize(NetworkController.Instance.RoomsLobby.Count, roomItems, RoomItemPrefab);
			}
#endif
			buttonJoinSession.onClick.AddListener(OnButtonJoinSession);
			buttonBack.onClick.AddListener(OnButtonBack);

			buttonBack.transform.Find("Text").GetComponent<TextMeshProUGUI>().text = LanguageController.Instance.GetText("text.back");
			buttonJoinSession.transform.Find("Text").GetComponent<TextMeshProUGUI>().text = LanguageController.Instance.GetText("screen.join.session.button.join");

			UIEventController.Instance.Event += OnUIEvent;
		}

		private void OnButtonJoinSession()
		{
			if (_selectedObject != null)
			{
				UIEventController.Instance.DispatchUIEvent(NetworkedSessionController.EventNetworkedBasicSessionJoinRoom, (string)_selectedObject.Objects[0]);
			}			
		}

		public override void Destroy()
		{
			base.Destroy();

			if (_content != null)
			{
				_content = null;
				UIEventController.Instance.Event -= OnUIEvent;
			}						
		}

		private void OnButtonBack()
		{
			UIEventController.Instance.DispatchUIEvent(ScreenController.EventScreenControllerDestroyScreen, this.gameObject);
		}

		private void OnUIEvent(string nameEvent, object[] parameters)
        {
            if (nameEvent.Equals(ItemRoom.EventItemRoomSelected))
            {
				if (this.gameObject == (GameObject)parameters[0])
				{
					_selectedObject = (ItemMultiObjectEntry)parameters[3];
				}
            }
        }
	}
}