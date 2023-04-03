/***
 * Author: Yunhan Li
 * Any issue please contact yunhn.lee@gmail.com
 ***/

using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System;
using yourvrexperience.Utils;

namespace yourvrexperience.VR
{
    public class KeyboardManager : MonoBehaviour
    {
        public const string EventKeyboardManagerKeycode = "EventKeyboardManagerKeycode";
        public const string EventKeyboardManagerEnter = "EventKeyboardManagerEnter";
        public const string EventKeyboardManagerBackspace = "EventKeyboardManagerBackspace";
        public const string EventKeyboardManagerClear = "EventKeyboardManagerClear";
        public const string EventKeyboardManagerCapsLock = "EventKeyboardManagerCapsLock";
        public const string EventKeyboardManagerShift = "EventKeyboardManagerShift";

        [Header("User defined")]
        [Tooltip("If the character is uppercase at the initialization")]
        public bool isUppercase = false;
        public int maxInputLength;

        [Header("UI Elements")]
        public Text inputText;

        [Header("Essentials")]
        public Transform keys;

        private string Input
        {
            get { return inputText.text; }
            set { inputText.text = value; }
        }
        private Key[] keyList;
        private bool capslockFlag;

        public void Initialize()
        {
            keyList = keys.GetComponentsInChildren<Key>();
            UIEventController.Instance.Event += OnUIEvent;
            capslockFlag = isUppercase;
            CapsLocK();
        }
        
        public void Destroy()
        {
            UIEventController.Instance.Event -= OnUIEvent;
        }

        private void CapsLocK()
        {
            foreach (var key in keyList)
            {
                if (key is Alphabet)
                {
                    key.CapsLock(capslockFlag);
                }
            }
            capslockFlag = !capslockFlag;
        }

        private void OnUIEvent(string nameEvent, object[] parameters)
        {
            if (nameEvent.Equals(EventKeyboardManagerKeycode))
            {
                if (Input.Length > maxInputLength) { return; }
                Input += (string)parameters[0];
            }
            if (nameEvent.Equals(EventKeyboardManagerEnter))
            {
                UIEventController.Instance.DispatchUIEvent(ScreenVRKeyboardView.EventScreenVRKeyboardConfirmInput);
            }
            if (nameEvent.Equals(EventKeyboardManagerBackspace))
            {
                if (Input.Length > 0)
                {
                    Input = Input.Remove(Input.Length - 1);
                }
                else
                {
                    return;
                }
            }
            if (nameEvent.Equals(EventKeyboardManagerClear))
            {
                Input = "";
            }
            if (nameEvent.Equals(EventKeyboardManagerCapsLock))
            {
                CapsLocK();
            }
            if (nameEvent.Equals(EventKeyboardManagerShift))
            {
                foreach (var key in keyList)
                {
                    if (key is Shift)
                    {
                        key.ShiftKey();
                    }
                }
            }
        }
    }
}