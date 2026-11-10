// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Godot;
using Polytoria.Shared;
using System.Linq;

namespace Polytoria.Creator.UI;

public sealed partial class TouchSplitResize : Node
{
	private const int GrabThickness = 28;
	private static readonly Color HighlightColor = new(0.35f, 0.6f, 1f, 0.55f);

	private ColorRect? _highlight;

	public override void _Ready()
	{
		GetTree().NodeAdded += OnNodeAdded;
		Hook(GetTree().Root);
	}

	public override void _ExitTree()
	{
		GetTree().NodeAdded -= OnNodeAdded;
		ClearHighlight();
		Globals.DockResizing = false;
	}

	private void OnNodeAdded(Node node)
	{
		if (node is SplitContainer sc) Setup(sc);
	}

	private void Hook(Node node)
	{
		if (node is SplitContainer sc) Setup(sc);
		foreach (Node child in node.GetChildren())
		{
			Hook(child);
		}
	}

	private void Setup(SplitContainer sc)
	{
		if (sc.HasMeta("touch_split")) return;
		sc.SetMeta("touch_split", true);
		sc.AddThemeConstantOverride("minimum_grab_thickness", GrabThickness);
		sc.DragStarted += () => OnDragStarted(sc);
		sc.DragEnded += OnDragEnded;
	}

	private void OnDragStarted(SplitContainer sc)
	{
		Globals.DockResizing = true;
		if (Globals.IsMobileBuild) Input.VibrateHandheld(20);

		ClearHighlight();
		Control? dragger = FindDragger(sc);
		if (dragger == null) return;

		_highlight = new ColorRect
		{
			Color = HighlightColor,
			MouseFilter = Control.MouseFilterEnum.Ignore,
		};
		dragger.AddChild(_highlight, @internal: InternalMode.Back);
		_highlight.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
	}

	private void OnDragEnded()
	{
		Globals.DockResizing = false;
		ClearHighlight();
	}

	private static Control? FindDragger(SplitContainer sc)
	{
		var regular = sc.GetChildren().ToHashSet();
		Control? best = null;
		float bestDist = float.MaxValue;
		foreach (Node child in sc.GetChildren(true))
		{
			if (regular.Contains(child) || child is not Control c || !c.Visible) continue;
			Rect2 rect = c.GetGlobalRect();
			float dist = rect.GetCenter().DistanceTo(c.GetGlobalMousePosition());
			if (rect.Grow(GrabThickness).HasPoint(c.GetGlobalMousePosition()) && dist < bestDist)
			{
				best = c;
				bestDist = dist;
			}
		}
		return best;
	}

	private void ClearHighlight()
	{
		if (_highlight != null && IsInstanceValid(_highlight)) _highlight.QueueFree();
		_highlight = null;
	}
}
