﻿using System;
using System.Text.Json.Serialization;
using vcrossing.Code.Data;
using vcrossing.Code.Items;
using vcrossing.Code.Player;

namespace vcrossing.Code.Persistence;

public partial class BaseCarriable : PersistentItem, IPickupable
{
	[JsonInclude] public int Durability { get; set; }

	/* public override string GetName()
	{
		return $"{GetItemData().Name} ({Durability})";
	} */

	public override string GetTooltip()
	{
		return base.GetTooltip() + $"\nDurability: {Durability}%";
	}

	/*public override void GetLinkData( WorldNodeLink nodeLink )
	{
		base.GetLinkData( nodeLink );

		if ( nodeLink.Node is Carriable.BaseCarriable carriable )
		{
			Durability = carriable.Durability;
		}
		else
		{
			Logger.Warn( $"Node {nodeLink.Node} is not a Carriable.BaseCarriable" );
		}
	}*/


	public override void GetNodeData( Node3D node )
	{
		base.GetNodeData( node );

		if ( node is Carriable.BaseCarriable carriable )
		{
			Logger.Info( $"Getting durability {carriable.Durability}" );
			Durability = carriable.Durability;
		}
		else
		{
			Logger.Warn( $"Node {node} is not a Carriable.BaseCarriable" );
		}
	}

	/*public override Carriable.BaseCarriable CreateCarry()
	{
		var carriable = base.CreateCarry();
		carriable.Durability = Durability;
		return carriable;
	}*/

	public override Carriable.BaseCarriable Create()
	{
		if ( ItemData == null ) throw new Exception( $"ItemData is null for {ItemDataPath}" );
		if ( ItemData.CarryScene == null )
		{
			throw new Exception( $"Carry scene not found for {ItemDataPath}" );
		}

		var scene = ItemData.CarryScene.Instantiate<Carriable.BaseCarriable>();
		scene.ItemDataPath = ItemDataPath;
		scene.SceneFilePath = ItemData.CarryScene.ResourcePath;
		SetNodeData( scene );
		return scene;
	}

	public override void SetNodeData( Node3D node )
	{
		base.SetNodeData( node );

		if ( node is Carriable.BaseCarriable carriable )
		{
			Logger.Info( $"Setting durability {Durability}" );
			carriable.Durability = Durability;
		}
		else
		{
			Logger.Warn( $"Node {node} is not a Carriable.BaseCarriable" );
		}
	}

	public void OnPickup( PlayerController player )
	{
		throw new System.NotImplementedException();
	}

	public override void Initialize()
	{
		base.Initialize();
		Durability = (ItemData as ToolData)?.MaxDurability ?? 100;
	}
}
