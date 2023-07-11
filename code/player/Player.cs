using Sandbox;
using SWB_Base;
using SWB_Player;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace MyGame;

public partial class Player : PlayerBase
{
    public bool SupressPickupNotices { get; set; }
    TimeSince timeSinceDropped;

    public ClothingContainer Clothing = new();
	public bool Flicked { get; set; } = false;
    TimeSince timeSinceFlick;
    public List<Particles> LighterParticles {get; set;} = new List<Particles>();

    public Player() : base()
    {
        //Inventory = new ExampleInventory(this);
        Inventory = new InventoryBase(this);
    }

    public Player(IClient client) : this()
    {
        // Load clothing from client data
        Clothing.LoadFromClient(client);
    }

    public override void Respawn()
    {
        base.Respawn();

        SetModel("models/citizen/citizen.vmdl");
        Clothing.DressEntity(this);

        Controller = new PlayerWalkController();
        Animator = new PlayerBaseAnimator();
        CameraMode = new FirstPersonCamera();

        EnableAllCollisions = true;
        EnableDrawing = true;
        EnableHideInFirstPerson = true;
        EnableShadowInFirstPerson = true;

        Health = 100;

        ClearAmmo();

        // Give weapons and ammo
        SupressPickupNotices = true;

        Inventory.Add(new SWB_WEAPONS.DEAGLE());

        GiveAmmo(AmmoTypes.Pistol, 20);

        SupressPickupNotices = false;

        SwitchToBestWeapon();
    }

    public override void Simulate(IClient cl)
    {
        base.Simulate(cl);

        TickPlayerUse();

        if (Input.Pressed(InputButtonHelper.View))
        {
            if (CameraMode is ThirdPersonCamera)
            {
                CameraMode = new FirstPersonCamera();
            }
            else
            {
                CameraMode = new ThirdPersonCamera();
            }
        }

        if (Input.Pressed(InputButtonHelper.Drop))
        {
            var dropped = Inventory.DropActive();
            if (dropped != null)
            {
                if (dropped.PhysicsGroup != null)
                {
                    dropped.PhysicsGroup.Velocity = Velocity + (this.EyeRotation.Forward + this.EyeRotation.Up) * 300;
                }

                timeSinceDropped = 0;
                SwitchToBestWeapon();
            }
        }

        if (this.ActiveChild is WeaponBase) {
            bool flicked = Input.Pressed(InputButtonHelper.Flashlight);
            bool isFlicking = Input.Down(InputButtonHelper.Flashlight);
            bool flickReleased = Input.Released(InputButtonHelper.Flashlight);

            var WeaponEntity = this.ActiveChild as WeaponBase;
            var LighterEntity = WeaponEntity.LighterModel;

            if (flicked) {
                timeSinceFlick = 0;
                this.PlaySound( "sounds/flick_open.sound" );
                Particles.Create("particles/lighter_flint.vpcf", LighterEntity, "flint");
            }

            if (isFlicking) {
                if (timeSinceFlick > .15 && LighterParticles.Count == 0) {
                    var LighterParticle1 = Particles.Create("particles/lighter_particle.vpcf", LighterEntity, "flame");
                    LighterParticles.Add(LighterParticle1);
                    var LighterParticle3 = Particles.Create("particles/lighter_fire.vpcf", LighterEntity, "flame");
                    LighterParticles.Add(LighterParticle3);
                }
            }

            if (flickReleased && LighterParticles.Count > 0) {
                LighterParticles.ForEach( lp => {
                    if (lp != null) lp.Destroy(false);
                });
                LighterParticles.Clear();
                this.PlaySound( "sounds/flick_close.sound" );
            }

            // if (Flicked) {
            //     LighterParticles.ForEach( lp => {
            //         if (lp != null) lp.Destroy(false);
            //     });
            // } else {
            //     // Particles.Create( "particles/testy.vpcf", this, "head");
            //     // var LighterParticle1 = Particles.Create("particles/lighter_particle.vpcf", LighterEntity, "flame");
            //     // LighterParticles.Add(LighterParticle1);
            //     // // LighterParticle2 = Particles.Create( "particles/emitslight.vpcf", LighterEntity, "head");
            //     // // var LighterParticle3 = Particles.Create( "particles/example/int_from_model_example/int_from_model_example.vpcf", LighterEntity, "flame");
            //     // var LighterParticle3 = Particles.Create("particles/lighter_fire.vpcf", LighterEntity, "flame");
            //     // LighterParticles.Add(LighterParticle3);
            //     Particles.Create("particles/lighter_flint.vpcf", LighterEntity, "flint");
            // }
        }

        SimulateActiveChild(cl, ActiveChild);

        //
        // If the current weapon is out of ammo and we last fired it over half a second ago
        // lets try to switch to a better wepaon
        //
        if (ActiveChild is WeaponBase weapon && !weapon.IsUsable() && weapon.TimeSincePrimaryAttack > 0.5f && weapon.TimeSinceSecondaryAttack > 0.5f)
        {
            SwitchToBestWeapon();
        }
    }

    public override void StartTouch(Entity other)
    {
        if (timeSinceDropped < 1) return;

        base.StartTouch(other);
    }

    public override void OnKilled()
    {
        base.OnKilled();

        CameraMode = new DeathCamera();
        var attacker = LastAttacker as PlayerBase;

        if (attacker != null && LastDamage.Weapon is WeaponBase weapon && GameManager.Current is MyGame game)
        {
            //game.UI.AddKillfeedEntry(To.Everyone, attacker.Client.SteamId, attacker.Client.Name, Client.SteamId, Client.Name, weapon.Icon);
        }

        if (ActiveChild is WeaponBase activeWep && activeWep.DropWeaponOnDeath)
            Inventory.DropActive();

        Inventory.DeleteContents();

        BecomeRagdollOnClient(LastDamage.Force, LastDamage.BoneIndex);

        Controller = null;
        //CameraMode = new SpectateRagdollCamera();

        EnableAllCollisions = false;
        EnableDrawing = false;
    }

    public void SwitchToBestWeapon()
    {
        var best = Children.Select(x => x as WeaponBase)
            .Where(x => x.IsValid() && x.IsUsable())
            .OrderByDescending(x => x.BucketWeight)
            .FirstOrDefault();

        if (best == null) return;

        ActiveChild = best;
    }

    public override void TakeDamage(DamageInfo info)
    {
        base.TakeDamage(info);

        if (info.Attacker is PlayerBase attacker && attacker != this)
        {
            TookDamage(To.Single(this), info.Weapon.IsValid() ? info.Weapon.Position : info.Attacker.Position);
        }
    }

    [ClientRpc]
    public virtual void TookDamage(Vector3 pos)
    {
        //DamageIndicator.Current?.OnHit(pos);
    }
}