using System;
using vcrossing.Code.Data;
using vcrossing.Code.Persistence;
using vcrossing.Code.Player;
using static vcrossing.Code.Data.ShopInventoryData;

namespace vcrossing.Code.Items;

public partial class ShopDisplay : Node3D, IUsable
{

	[Export] public string ShopId { get; set; }

	[Export] public int ItemIndex { get; set; }

	// [Export] public ItemCategoryData Category { get; set; }

	[Export] public Node3D ModelContainer { get; set; }

	[Export] public Node3D ShopSoldOutSign { get; set; }

	public ItemData CurrentItem { get; set; }

	// public bool IsBought { get; set; }

	public override void _Ready()
	{
		base._Ready();

		SpawnModel();

		GetNode<GpuParticles3D>( "Poof" ).Emitting = false;

		Logger.Warn( "Item does not have a model" );
	}

	private void SpawnModel()
	{

		ModelContainer.GetChildren().FirstOrDefault()?.QueueFree();

		if ( !HasItem || !IsInStock )
		{
			Logger.Info( $"ShopDisplay", $"Item {Item?.ItemDataPath}: {HasItem}, Stock: {IsInStock}" );
			ShopSoldOutSign.Visible = true;
			return;
		}

		ShopSoldOutSign.Visible = false;

		CurrentItem = ResourceLoader.Load<ItemData>( Item.ItemDataPath );

		if ( CurrentItem == null ) throw new Exception( "No item to spawn" );

		var itemInstance = CurrentItem.PlaceScene.Instantiate<Node3D>();

		if ( itemInstance is BaseItem baseItem )
		{
			var model = baseItem.Model;
			if ( model != null )
			{
				var modelNode = itemInstance.GetNode<Node3D>( model );
				modelNode.GetParent().RemoveChild( modelNode );
				ModelContainer.AddChild( modelNode );
				itemInstance.QueueFree();
				Logger.Info( $"Added model {modelNode.Name} to shop display" );
				return;
			}
		}
	}

	private bool HasItem
	{
		get
		{
			return Item != null;
		}
	}

	private ShopItem Item
	{
		get
		{
			var main = GetNode<MainGame>( "/root/Main" );
			var shop = main.Shops[ShopId];
			if ( ItemIndex >= shop.Items.Count )
			{
				Logger.Warn( $"Item index {ItemIndex} is out of range for shop {ShopId}" );
				return null;
			}
			var item = shop.Items[ItemIndex];
			return item;
		}
	}

	private bool IsInStock
	{
		get
		{
			return Item.Stock > 0;
		}
	}

	public bool CanUse( PlayerController player )
	{
		return HasItem && IsInStock;
	}

	public void OnUse( PlayerController player )
	{
		Logger.Info( $"Used shop display with item {CurrentItem.Name}" );

		// if ( IsBought ) return;
		// IsBought = true;

		Item.Stock--;

		var item = PersistentItem.Create( CurrentItem );
		player.Inventory.PickUpItem( item );

		GetNode<GpuParticles3D>( "Poof" ).Emitting = true;

		SpawnModel();

		GetNode<MainGame>( "/root/Main" ).SaveShops();
	}
}
