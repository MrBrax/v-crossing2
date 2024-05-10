﻿using Godot;
using vcrossing2.Code.Items;

namespace vcrossing2.Code.WorldBuilder;

public partial class House : Node3D
{
	
	// [Export] public string HouseId { get; set; }
	
	[Export] public string DestinationWorld { get; set; }
	
	public override void _Ready()
	{
		SpawnTrigger();
	}

	private void SpawnTrigger()
	{
		var world = GetNode<WorldManager>( "/root/Main/WorldContainer" ).ActiveWorld;

		if ( world == null ) throw new System.Exception( "World not found." );

		var entrancePosition = world.WorldToItemGrid( Position + new Vector3( 0, 0, 1 ) );

		var trigger = world.SpawnPlacedItem<AreaTrigger>( GD.Load<ItemData>( "res://items/misc/area_trigger.tres" ),
			entrancePosition,
			World.ItemPlacement.Floor, World.ItemRotation.North );

		trigger.DestinationWorld = DestinationWorld;
		trigger.DestinationExit = "entrance";

		GD.Print( $"Spawned trigger at {entrancePosition}" );
	}
}