﻿using System;
using System.Text.Json.Serialization;
using vcrossing.Code.Data;
using vcrossing.Code.Items;
using vcrossing.Code.Persistence;
using vcrossing.Code.WorldBuilder;

namespace vcrossing.Code.Inventory;

public class InventorySlot<TItem> where TItem : PersistentItem
{

	[JsonInclude] public int Index { get; set; } = -1;

	[JsonInclude] public TItem _item;

	public InventorySlot( Player.Inventory inventory )
	{
		Inventory = inventory;
	}

	public InventorySlot()
	{
	}


	[JsonIgnore] public Player.Inventory Inventory { get; set; }

	[JsonIgnore] public bool HasItem => _item != null;

	public void SetItem( TItem item )
	{
		_item = item;
		Inventory.OnChange();
	}

	public TItem GetItem()
	{
		return _item;
	}

	public T GetItem<T>() where T : TItem
	{
		return (T)_item;
	}

	public void RemoveItem()
	{
		_item = null;
		Inventory.OnChange();
		// Inventory.Player.Save();
	}

	public void Drop()
	{
		Logger.Info( "Dropping item" );
		var position = Inventory.PlayerInteract.GetAimingGridPosition();
		var playerRotation =
			Inventory.World.GetItemRotationFromDirection(
				Inventory.World.Get4Direction( Inventory.PlayerModel.RotationDegrees.Y ) );

		try
		{
			// Inventory.World.SpawnDroppedItem( _item.GetItemData(), position, World.ItemPlacement.Floor, playerRotation );
			Inventory.World.SpawnPersistentNode( _item, position, playerRotation, World.ItemPlacement.Floor, true );
		}
		catch ( System.Exception e )
		{
			Logger.Info( e );
			return;
		}

		Inventory.GetNode<AudioStreamPlayer3D>( "ItemDrop" ).Play();

		// Items.Remove( item );
		RemoveItem();
		// Inventory.World.Save();

		// Inventory.GetNode<PlayerController>( "../" ).Save();
	}

	public void Place()
	{
		Logger.Info( "Placing item" );
		var aimingGridPosition = Inventory.PlayerInteract.GetAimingGridPosition();
		var playerRotation =
			Inventory.World.GetItemRotationFromDirection(
				Inventory.World.Get4Direction( Inventory.PlayerModel.RotationDegrees.Y ) );

		var floorItem = Inventory.World.GetItem( aimingGridPosition, World.ItemPlacement.Floor );

		if ( floorItem != null )
		{
			var placeableNodes = floorItem.GetPlaceableNodes();
			if ( placeableNodes.Count > 0 )
			{
				/*var nodeNearestToAimPosition = placeableNodes.MinBy( n =>
					n.GlobalPosition.DistanceTo( Inventory.World.ItemGridToWorld( aimingGridPosition ) ) );
				
				var nodeGridPosition = Inventory.World.WorldToItemGrid( nodeNearestToAimPosition.GlobalPosition );
				*/
				var onTopItem = Inventory.World.GetItem( aimingGridPosition, World.ItemPlacement.OnTop );
				if ( onTopItem != null )
				{
					Logger.Warn( "On top item already exists." );
					return;
				}

				try
				{
					Inventory.World.SpawnPersistentNode( _item, aimingGridPosition, playerRotation, World.ItemPlacement.OnTop,
						false );
				}
				catch ( System.Exception e )
				{
					Logger.LogError( e.Message );
					return;
				}

				RemoveItem();

				Inventory.GetNode<AudioStreamPlayer3D>( "ItemDrop" ).Play();

				return;
			}

			Logger.Warn( "Can't place item on this position." );
			return;
		}

		try
		{
			// Inventory.World.SpawnPlacedItem<PlacedItem>( _item.GetItemData(), position, World.ItemPlacement.Floor,
			// 	playerRotation );
			Inventory.World.SpawnPersistentNode( _item, aimingGridPosition, playerRotation, World.ItemPlacement.Floor,
				false );
		}
		catch ( System.Exception e )
		{
			Logger.LogError( e.Message );
			return;
		}

		Inventory.GetNode<AudioStreamPlayer3D>( "ItemDrop" ).Play();

		// Items.Remove( item );
		RemoveItem();
		// Inventory.World.Save();

		// Inventory.Player.Save();
	}

	public void Equip()
	{
		PersistentItem currentCarriable;
		if ( Inventory.Player.HasEquippedItem( Player.PlayerController.EquipSlot.Tool ) )
		{
			// TODO: automatically unequip current item

			currentCarriable = PersistentItem.Create( Inventory.Player.GetEquippedItem( Player.PlayerController.EquipSlot.Tool ) );

		}

		// if ( !Player.Inventory.IsInstanceValid( Inventory.Player.Equip ) ) throw new System.Exception( "Player equip node is null." );

		var itemDataPath = GetItem().ItemDataPath;

		if ( string.IsNullOrEmpty( itemDataPath ) )
		{
			throw new Exception( "Item data path is empty." );
		}

		var item = GetItem().CreateCarry();
		item.ItemDataPath = itemDataPath;
		item.Inventory = Inventory;

		Inventory.Player.ToolEquip.AddChild( item );
		// Inventory.Player.CurrentCarriable = item;
		Inventory.Player.SetEquippedItem( Player.PlayerController.EquipSlot.Tool, item );

		item.Position = Vector3.Zero;
		item.RotationDegrees = new Vector3( 0, 0, 0 );

		item.OnEquip( Inventory.Player );

		RemoveItem();
		// Inventory.Player.Save();
	}

	public void Bury()
	{
		var pos = Inventory.Player.Interact.GetAimingGridPosition();
		var floorItem = Inventory.World.GetItem( pos, World.ItemPlacement.Floor );
		if ( floorItem.Node is not Hole hole )
		{
			return;
		}

		// spawn item underground
		Inventory.World.SpawnPersistentNode( _item, pos, World.ItemRotation.North, World.ItemPlacement.Underground,
			true );

		// remove hole so it isn't obstructing the dirt that will be spawned next
		Inventory.World.RemoveItem( hole );

		// spawn dirt on top
		Inventory.World.SpawnNode( Loader.LoadResource<ItemData>( "res://items/misc/hole/buried_item.tres" ), pos,
			World.ItemRotation.North, World.ItemPlacement.Floor, false );

		RemoveItem();

		// Inventory.World.RemoveItem( floorItem );
	}

	public void SetWallpaper()
	{

		if ( _item.GetItemData() is not WallpaperData wallpaperData )
		{
			throw new System.Exception( "Item data is not a wallpaper data." );
		}

		/* var interiorSearch = Inventory.Player.World.FindChildren("*", "HouseInterior").FirstOrDefault();
		if ( interiorSearch == null )
		{
			throw new System.Exception( "Interior not found." );
		}

		var interior = interiorSearch as HouseInterior; */

		var interior = Inventory.Player.World.GetTree().GetNodesInGroup( "interior" ).FirstOrDefault() as HouseInterior;

		if ( !GodotObject.IsInstanceValid( interior ) )
		{
			throw new System.Exception( "Interior not found." );
		}

		interior.SetWallpaper( 0, wallpaperData );

		/* var wall = interior.Rooms[0].GetWall( interior );

		if ( wall == null )
		{
			throw new System.Exception( "Wall not found." );
		}

		wall.MaterialOverride = new StandardMaterial3D
		{
			AlbedoTexture = wallpaperData.Texture
		}; */

	}

	public void Eat()
	{

		if ( _item.GetItemData() is not FruitData foodData )
		{
			throw new System.Exception( "Item data is not a food data." );
		}

		Inventory.GetNode<AudioStreamPlayer3D>( "ItemEat" ).Play();

		Logger.Info( "Eating food" );

		RemoveItem();

		// Inventory.Player.Save();

	}
}
