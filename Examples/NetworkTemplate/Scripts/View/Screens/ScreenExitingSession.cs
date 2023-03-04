using System;
using System.Collections;
using System.Collections.Generic;
using yourvrexperience.Utils;
using UnityEngine;
using UnityEngine.UI;

namespace yourvrexperience.VR
{
	public class ScreenExitingSession : BaseScreenView, IScreenView
	{
		public const string ScreenName = "ScreenExitingSession";

		public override string NameScreen 
		{ 
			get { return ScreenName; }
		}
	}
}