﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using Godot;
using vcrossing2.Code.Helpers;
using vcrossing2.Code.Items;
using vcrossing2.Code.Player;

namespace vcrossing2.Code;

public class WorldNodeLink
{
	[JsonIgnore] public Node3D Node;
	[JsonInclude, JsonConverter( typeof( Vector2IConverter ) )] public Vector2I GridPosition;
	[JsonInclude] public World.ItemRotation GridRotation;
	[JsonInclude] public World.ItemPlacement GridPlacement;
	[JsonInclude] public World.ItemPlacementType PlacementType;

	[JsonIgnore] public Vector2I GridSize;

	[JsonInclude] public string ItemDataPath;
	[JsonInclude] public string ItemScenePath;

	public WorldNodeLink()
	{
	}

	public WorldNodeLink( Node3D node )
	{
		Node = node;
		GetData( node );
	}

	private void GetData( Node3D node )
	{
		if ( node is WorldItem worldItem )
		{
			ItemDataPath = worldItem.ItemDataPath;
			PlacementType = worldItem.PlacementType;
		}
		else if ( node is Carriable.BaseCarriable carriable )
		{
			ItemDataPath = carriable.ItemDataPath;
			PlacementType = World.ItemPlacementType.Dropped;
		}
		else
		{
			GD.PushWarning( $"Item data path not found for {node} (unsupported type {node.GetType()})" );
		}
	}

	public bool ShouldBeSaved()
	{
		// return true;
		if ( Node is IWorldItem worldItem )
		{
			return worldItem.ShouldBeSaved();
		}
		
		return true;
	}

	public bool IsValid()
	{
		return Node != null;
	}

	public string GetName()
	{
		return Node.Name;
	}

	public void DestroyNode()
	{
		Node.QueueFree();
	}

	public override string ToString()
	{
		return $"[NL:{Node.Name} {GridPosition} {GridRotation} {GridPlacement}]";
	}

	public List<Vector2I> GetGridPositions( bool global = false )
	{
		var positions = new List<Vector2I>();

		var itemData = GetItemData();

		if ( itemData == null )
		{
			throw new Exception( $"Item data not found on {this}" );
		}

		// rotate the item based on the rotation
		var width = itemData.Width;
		var height = itemData.Height;
		var rotation = GridRotation;

		// GD.Print( $"Getting size of {itemData.Name}" );
		// GD.Print( $"Width: {width}, Height: {height}, Rotation: {rotation}" );

		if ( width == 0 || height == 0 ) throw new Exception( "Item has no size" );

		if ( width == 1 && height == 1 )
		{
			return new List<Vector2I> { global ? GridPosition : Vector2I.Zero };
		}

		if ( rotation == World.ItemRotation.North )
		{
			for ( var x = 0; x < width; x++ )
			{
				for ( var y = 0; y < height; y++ )
				{
					positions.Add( new Vector2I( x, y ) );
				}
			}
		}
		else if ( rotation == World.ItemRotation.South )
		{
			for ( var x = 0; x < width; x++ )
			{
				for ( var y = 0; y < height; y++ )
				{
					positions.Add( new Vector2I( x, y * -1 ) );
				}
			}
		}
		else if ( rotation == World.ItemRotation.East )
		{
			for ( var x = 0; x < height; x++ )
			{
				for ( var y = 0; y < width; y++ )
				{
					positions.Add( new Vector2I( x, y ) );
				}
			}
		}
		else if ( rotation == World.ItemRotation.West )
		{
			for ( var x = 0; x < height; x++ )
			{
				for ( var y = 0; y < width; y++ )
				{
					positions.Add( new Vector2I( x * -1, y ) );
				}
			}
		}

		if ( global )
		{
			positions = positions.Select( p => p + GridPosition ).ToList();
		}

		return positions;
	}

	public void OnPlayerPickUp( PlayerInteract playerInteract )
	{
		if ( !CanBePickedUp() )
		{
			GD.Print( $"Cannot pick up {GetName()}" );
			return;
		}

		var playerInventory = playerInteract.GetNode<Player.Inventory>( "../PlayerInventory" );
		playerInventory.PickUpItem( this );
	}

	private bool CanBePickedUp()
	{
		return !GetItemData().DisablePickup;
	}

	public void OnPlayerUse( PlayerInteract playerInteract, Vector2I pos )
	{
		throw new System.NotImplementedException();
	}

	public ItemData GetItemData()
	{
		return GD.Load<ItemData>( ItemDataPath );
	}
}