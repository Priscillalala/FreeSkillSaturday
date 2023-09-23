using System;
using Unity;
using UnityEngine;
using RoR2;
using System.Collections;
using HG;
using RoR2.Achievements;
using System.Collections.Generic;
using UnityEngine.Networking;

namespace FreeItemFriday.Achievements
{
	public class CompleteMultiplayerUnknownEndingAchievement : BaseAchievement
	{
		public override void OnInstall()
		{
			base.OnInstall();
			base.SetServerTracked(true);
		}

		public override void OnUninstall()
		{
			base.OnUninstall();
		}

		public class ServerAchievement : BaseServerAchievement
		{
			public override void OnInstall()
			{
				base.OnInstall();
				Run.onServerGameOver += this.OnServerGameOver;
			}

			public override void OnUninstall()
			{
				base.OnInstall();
				Run.onServerGameOver -= this.OnServerGameOver;
			}

			public void OnServerGameOver(Run run, GameEndingDef gameEndingDef)
			{
				if ((gameEndingDef == RoR2Content.GameEndings.ObliterationEnding || gameEndingDef == RoR2Content.GameEndings.LimboEnding) && RoR2Application.isInMultiPlayer)
				{
					base.Grant();
				}
			}
		}
	}
}

