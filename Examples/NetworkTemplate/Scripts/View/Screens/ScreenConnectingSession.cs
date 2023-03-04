using System;
using System.Collections;
using System.Collections.Generic;
using yourvrexperience.Utils;
using UnityEngine;
using UnityEngine.UI;

namespace yourvrexperience.VR
{
	public class ScreenConnectingSession : BaseScreenView, IScreenView
	{
		public const string ScreenName = "ScreenConnectingSession";

		public override string NameScreen 
		{ 
			get { return ScreenName; }
		}
	}
}