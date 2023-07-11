using Sandbox;

namespace MyGame;

public partial class Pistol : Weapon
{
	public override string ModelPath => "weapons/rust_pistol/rust_pistol.vmdl";
	public override string ViewModelPath => "weapons/rust_pistol/v_rust_pistol.vmdl";


	[Net, Local, Predicted]
	private bool Flicked { get; set; } = false;

	[ClientRpc]
	protected virtual void ShootEffects()
	{
		Game.AssertClient();

		Particles.Create( "particles/pistol_muzzleflash.vpcf", EffectEntity, "muzzle" );
		Particles.Create( "particles/explosion/barrel_explosion/explosion_flash_02.vpcf", EffectEntity, "muzzle");

		Pawn.SetAnimParameter( "b_attack", true );
		ViewModelEntity?.SetAnimParameter( "fire", true );
	}

	[ClientRpc]
	protected virtual void FlickEffects()
	{
		Game.AssertClient();
		if (this.Flicked) {
			Pawn.PlaySound( "sounds/flick_close.sound" );
		} else {
			Pawn.PlaySound( "sounds/flick_open.sound" );
		}
		Particles.Create( "particles/testy.vpcf", EffectEntity, "muzzle");
		//Particles.Create( "particles/lighter_particle.vpcf", EffectEntity, "muzzle");
		//Particles.Create( "particles/example/int_from_model_example/int_from_model_example.vpcf", EffectEntity, "muzzle");
	}

	public override void PrimaryAttack()
	{
		ShootEffects();
		Pawn.PlaySound( "rust_pistol.shoot" );
		ShootBullet( 0.1f, 100, 20, 1 );
	}

	public override void SecondaryAttack()
	{
		FlickEffects();
		Flicked = !Flicked;
	}

	protected override void Animate()
	{
		Pawn.SetAnimParameter( "holdtype", (int)CitizenAnimationHelper.HoldTypes.Pistol );
	}
}
