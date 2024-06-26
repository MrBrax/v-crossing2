﻿using System.Threading.Tasks;
using vcrossing.Code.Data;
using vcrossing.Code.Save;
using vcrossing.Code.Ui;
using Array = Godot.Collections.Array;

namespace vcrossing.Code;

public partial class WorldManager : Node3D
{
	[Export] public World ActiveWorld { get; set; }

	[Signal]
	public delegate void WorldUnloadEventHandler( World world );

	[Signal]
	public delegate void WorldLoadedEventHandler( World world );

	public string CurrentWorldDataPath { get; set; }

	public bool IsLoading;

	public Array LoadingProgress { get; set; } = new Array();

	public override async void _Ready()
	{
		if ( !IsInstanceValid( ActiveWorld ) )
		{
			await LoadWorld( "res://world/worlds/island.tres" );

			GetNode<Fader>( "/root/Main/UserInterface/Fade" ).FadeOut();
		}

		WorldLoaded += ( World world ) => GetNode<SettingsSaveData>( "/root/SettingsSaveData" ).ApplySettings();

		/*WorldLoaded += ( world ) =>
		{
			SetupNewWorld();
		};*/
	}

	private void SetLoadingScreen( bool visible, string text = "" )
	{
		// TODO: make loading screen a class
		GetNode<PanelContainer>( "/root/Main/UserInterface/LoadingScreen" ).Visible = visible;
		GetNode<Label>( "/root/Main/UserInterface/LoadingScreen/MarginContainer/LoadingLabel" ).Text = text;
	}


	/// <summary>
	/// Loads a world from the specified world data path asynchronously.
	/// TODO: load persistent data asynchronously
	/// </summary>
	/// <param name="worldDataPath">The path to the world data.</param>
	/// <returns>A task representing the asynchronous operation. The task result is true if the world was loaded successfully, false otherwise.</returns>
	public async Task<bool> LoadWorld( string worldDataPath )
	{
		if ( IsLoading )
		{
			Logger.LogError( "WorldManager", "Already loading a world." );
			return false;
		}

		SetLoadingScreen( true, $"Loading {worldDataPath}..." );

		// wait for loading screen to show
		await ToSignal( GetTree(), SceneTree.SignalName.ProcessFrame );

		if ( ActiveWorld != null )
		{
			EmitSignal( SignalName.WorldUnload, ActiveWorld );
			ActiveWorld.Unload();
			ActiveWorld.QueueFree();
			ActiveWorld = null;
		}

		// WorldChanged?.Invoke();

		Logger.Info( "WorldManager", "Waiting for old world to be freed." );
		await ToSignal( GetTree(), SceneTree.SignalName.ProcessFrame );

		Logger.Info( "WorldManager", "Waited for old world to be freed, hopefully it's gone now." );

		// clear loaded resources
		Loader.ClearLoadedResources();

		CurrentWorldDataPath = worldDataPath;

		if ( ResourceLoader.HasCached( CurrentWorldDataPath ) )
		{
			Logger.Info( "WorldManager", "Loading world data from cache." );
			var resource = Loader.LoadResource<WorldData>( CurrentWorldDataPath );
			if ( resource is WorldData worldData )
			{
				SetupNewWorld( worldData );
			}
			else
			{
				Logger.LogError( "WorldManager", $"Failed to load world data: {CurrentWorldDataPath}" );
				IsLoading = false;
				SetLoadingScreen( false );
			}

			return true;
		}

		Logger.Info( "WorldManager", "Loading world data threaded..." );
		var error = ResourceLoader.LoadThreadedRequest( CurrentWorldDataPath );
		if ( error != Error.Ok )
		{
			Logger.LogError( "WorldManager", $"Failed to load world data: {CurrentWorldDataPath} ({error})" );
			IsLoading = false;
			SetLoadingScreen( false );
			return false;
		}

		Logger.Info( "WorldManager", $"World data loading response: {error}" );
		IsLoading = true;

		// wait for the world to load
		await ToSignal( this, SignalName.WorldLoaded );

		Logger.Info( "WorldManager", "World loaded." );

		SetLoadingScreen( false );

		return true;

	}

	public override void _Process( double delta )
	{
		base._Process( delta );

		if ( !IsLoading )
		{
			return;
		}

		// check if world data is loaded
		if ( !string.IsNullOrEmpty( CurrentWorldDataPath ) && !IsInstanceValid( ActiveWorld ) )
		{
			var status = ResourceLoader.LoadThreadedGetStatus( CurrentWorldDataPath, LoadingProgress );
			if ( status == ResourceLoader.ThreadLoadStatus.Loaded )
			{
				var resource = ResourceLoader.LoadThreadedGet( CurrentWorldDataPath );
				if ( resource is WorldData worldData )
				{
					SetupNewWorld( worldData );
				}
			}
			else if ( status == ResourceLoader.ThreadLoadStatus.Failed )
			{
				Logger.LogError( "WorldManager", $"Failed to load world data: {CurrentWorldDataPath}" );
				IsLoading = false;
				SetLoadingScreen( true, "Failed to load world data." );
			}
			else if ( status == ResourceLoader.ThreadLoadStatus.InvalidResource )
			{
				Logger.LogError( "WorldManager", $"Invalid resource: {CurrentWorldDataPath}" );
				IsLoading = false;
				SetLoadingScreen( true, "Invalid resource." );
			}
			else
			{
				// Logger.Info( "World data not loaded yet." );
				if ( LoadingProgress.Count > 0 )
				{
					var progress = (float)LoadingProgress[0] * 100f;
					SetLoadingScreen( true, $"Loading {CurrentWorldDataPath} ({progress:0.0}%)" );
				}
			}
		}
	}



	private async void SetupNewWorld( WorldData worldData )
	{
		/*if ( worldData == null )
		{
			throw new System.Exception( "World data is null." );
			return;
		}

		if ( worldData.WorldScene == null )
		{
			throw new System.Exception( "World scene is null." );
			return;
		}*/

		Logger.Info( "WorldManager", "Loading new world." );

		ActiveWorld = worldData.WorldScene.Instantiate<World>();
		ActiveWorld.WorldId = worldData.WorldId;
		ActiveWorld.WorldName = worldData.WorldName;
		ActiveWorld.WorldPath = worldData.ResourcePath;
		ActiveWorld.GridWidth = worldData.Width;
		ActiveWorld.GridHeight = worldData.Height;
		ActiveWorld.UseAcres = worldData.UseAcres;

		Logger.Info( "WorldManager", "Adding new world to scene." );
		AddChild( ActiveWorld );

		// Logger.Info( "WorldManager", "Checking terrain." );
		// ActiveWorld.CheckTerrain();

		Logger.Info( "WorldManager", "Setup interior collisions." );
		ActiveWorld.SetupInteriorCollisions();

		Logger.Info( "WorldManager", "Loading editor placed items." );
		ActiveWorld.LoadEditorPlacedItems();

		Logger.Info( "WorldManager", "Loading world data." );
		await ActiveWorld.Load();

		Logger.Info( "WorldManager", "Load interiors." );
		ActiveWorld.LoadInteriors();

		Logger.Info( "WorldManager", "Activate classes." );
		ActiveWorld.ActivateClasses();

		Logger.Info( "WorldManager", "World loaded." );
		IsLoading = false;
		GetNode<PanelContainer>( "/root/Main/UserInterface/LoadingScreen" ).Hide();
		// WorldLoaded?.Invoke( ActiveWorld );
		// WorldLoaded
		EmitSignal( SignalName.WorldLoaded, ActiveWorld );
	}
}
