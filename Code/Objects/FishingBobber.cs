using Godot;
using vcrossing2.Code.Carriable;

namespace vcrossing2.Code.Objects;

public partial class FishingBobber : Node3D
{

	public FishingRod Rod { get; set; }

	public override void _Ready()
	{
		AddToGroup( "fishing_bobber" );

		GetNode<AudioStreamPlayer3D>( "BobberWater" ).Play();

		GetNode<AnimationPlayer>( "fish_bobber/AnimationPlayer" ).Play( "bobbing" );
	}

	public Vector3 TipBoneGlobalPosition
	{
		get
		{
			var boneTransform = GetNode<Skeleton3D>( "fish_bobber/fish_armature/Skeleton3D" ).GetBoneGlobalPose( 0 );
			var boneWithOffset = boneTransform.Origin + boneTransform.Basis.Y * 0.1f;
			return GlobalTransform.Origin + boneWithOffset;
		}
	}
}
