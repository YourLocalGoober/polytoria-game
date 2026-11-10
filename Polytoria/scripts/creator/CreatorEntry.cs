// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Godot;
using Polytoria.Client.Settings.Appliers;
using Polytoria.Creator.Managers;
using Polytoria.Creator.Settings;
using Polytoria.Creator.Utils;
using Polytoria.Datamodel.Creator;
using Polytoria.Shared;
using Polytoria.Shared.AssetLoaders;
using Polytoria.Shared.Settings;
using System.Collections.Generic;
using System.IO;

namespace Polytoria.Creator;

public partial class CreatorEntry : Node
{
	public const int CreatorPort = 24220;

	public async override void _EnterTree()
	{
		Dictionary<string, string> cmdargs = Globals.ReadCmdArgs();
		cmdargs.TryGetValue("token", out string? launchToken);

		CreatorService creatorService = new();
		AddChild(creatorService);

		CreatorSettingsService creatorSettingsService = new()
		{
			Name = "CreatorSettingsService"
		};
		AddChild(creatorSettingsService, true, InternalMode.Front);
		creatorSettingsService.Init();

		AssetLoader.Singleton.MaxConcurrentRequests = creatorSettingsService.Get<int>(SharedSettingKeys.Advanced.AssetQueue);

		creatorSettingsService.AddChild(new GraphicsSettingsApplier { Name = GraphicsSettingsApplier.NodeName, Settings = creatorSettingsService }, true, InternalMode.Front);

		GetViewport().GuiEmbedSubwindows = true;

		if (Globals.IsMobileBuild)
		{
			DisplayServer.WindowSetMode(DisplayServer.WindowMode.Windowed);
			DisplayServer.ScreenSetOrientation(DisplayServer.ScreenOrientation.Landscape);
			GetTree().Root.ContentScaleMode = Window.ContentScaleModeEnum.CanvasItems;
			GetTree().Root.ContentScaleAspect = Window.ContentScaleAspectEnum.Expand;
			GetTree().Root.ContentScaleFactor = 2.25f;
			AddChild(new Polytoria.Shared.Misc.TouchLongPressToRightClick { Name = "TouchLongPressToRightClick" });
			AddChild(new Polytoria.Creator.UI.TouchSplitResize { Name = "TouchSplitResize" });
			UnbindStickFromUI();
			InsetTopBar();

			if (Engine.HasSingleton("PolytoriaPermissions"))
			{
				GodotObject perms = Engine.GetSingleton("PolytoriaPermissions");
				if (!(bool)perms.Call("hasAllFilesAccess"))
				{
					perms.Call("requestAllFilesAccess");
				}
			}
		}

		// Open project
		cmdargs.TryGetValue("proj", out string? creatorFilePath);
		if (creatorFilePath != null)
		{
			_ = CreatorService.Singleton.CreateNewSession(creatorFilePath);
		}

		// Import legacy world cmd arguments
		cmdargs.TryGetValue("liin", out string? legacyImportIn);
		cmdargs.TryGetValue("liout", out string? legacyImportOut);

		if (legacyImportIn != null && legacyImportOut != null)
		{
			_ = ProjectManager.ImportLegacyWorld(legacyImportIn, legacyImportOut, new() { MainWorld = "main.poly", ProjectName = new DirectoryInfo(legacyImportOut).Name });
		}

		// Login creator with token
		if (launchToken != null)
		{
			await PolyCreatorAPI.LoginWithToken(launchToken);
		}
	}

	private const float TopBarInset = 24f;

	private void InsetTopBar()
	{
		PanelContainer? menu = GetNodeOrNull<PanelContainer>("GUI/Menu");
		if (menu?.GetThemeStylebox("panel")?.Duplicate() is not StyleBox box) return;
		box.ContentMarginLeft = Mathf.Max(box.ContentMarginLeft, 0) + TopBarInset;
		box.ContentMarginRight = Mathf.Max(box.ContentMarginRight, 0) + TopBarInset;
		menu.AddThemeStyleboxOverride("panel", box);
	}

	private static void UnbindStickFromUI()
	{
		foreach (string action in new[] { "ui_up", "ui_down", "ui_left", "ui_right" })
		{
			if (!InputMap.HasAction(action)) continue;
			foreach (InputEvent e in InputMap.ActionGetEvents(action))
			{
				if (e is InputEventJoypadMotion) InputMap.ActionEraseEvent(action, e);
			}
		}
	}
}
