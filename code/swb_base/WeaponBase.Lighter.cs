using System.Collections.Generic;
using System.Threading.Tasks;
using Sandbox;

namespace SWB_Base;

public partial class WeaponBase
{

    const string ParticleFlint = "particles/lighter_flint.vpcf";
    const string ParticleFire = "particles/lighter_fire.vpcf";
    const string ParticleLight = "particles/lighter_particle.vpcf";

    /// <summary>
    /// Checks if the weapon can do the provided attack
    /// </summary>
    /// <param name="clipInfo">Attack information</param>
    /// <returns></returns>
    public virtual bool CanFlick()
    {
        // Not using this right now..
        if (IsReloading) return false;
        return true;
    }

    public virtual void SimulateFlick() 
    {
        bool flicked = Input.Pressed(InputButtonHelper.Flashlight);
        bool isFlicking = Input.Down(InputButtonHelper.Flashlight);
        bool flickReleased = Input.Released(InputButtonHelper.Flashlight);
        if (isFlicking && IsReloading ){
            flickReleased = true;
            isFlicking = false;
            flicked = false;
        }

        if (flicked) {
            timeSinceFlick = 0;
            this.PlaySound( "sounds/flick_open.sound" );
            // Particles.Create("particles/lighter_flint.vpcf", LighterModel, "flint");
            LighterEffectsSV(ParticleFlint);
        }

        if (isFlicking) {
            if (timeSinceFlick > .15 && Flicked == false) {
                // var LighterParticle1 = Particles.Create("particles/lighter_particle.vpcf", LighterModel, "flame");
                // LighterParticles.Add(LighterParticle1);
                // var LighterParticle2 = Particles.Create("particles/lighter_fire.vpcf", LighterModel, "flame");
                // LighterParticles.Add(LighterParticle2);
                Flicked = true;
                LighterEffectsSV(ParticleFire);
                LighterEffectsSV(ParticleLight);
            }
        }

        if (flickReleased && Flicked) {
            // LighterParticles.ForEach( lp => {
            //     if (lp != null) lp.Destroy(false);
            // });
            // LighterParticles.Clear();
            Flicked = false;
            RemoveLighterEffects();
            this.PlaySound( "sounds/flick_close.sound" );
        }

    }


    /// <summary>
    /// Gets the data on where to show the lighter effect
    /// Model, attachment point
    /// </summary>
    public virtual (ModelEntity, string) GetParticleEffectData(string particle)
    {
        ModelEntity effectEntity = this.Parent as MyGame.Player;
        //string attachment = "hold_L";
        string attachment = "head";
        if (CanSeeViewModel())
        {
            effectEntity = LighterModel;

            if (particle == ParticleFlint) {
                attachment = "flint";
            } else {
                attachment = "flame";
            }
        }

        return (effectEntity, attachment);
    }

    /// <summary>
    /// Networks shooting effects
    /// </summary>
    [ClientRpc]
    protected virtual void LighterEffectsSV(string particleName)
    {
        LighterEffectsCL(particleName);
    }

    /// <summary>
    /// Networks shooting effects
    /// </summary>
    [ClientRpc]
    protected virtual void RemoveLighterEffects()
    {
        if (LighterParticles.Count > 0) {
            LighterParticles.ForEach( lp => {
                if (lp != null) lp.Destroy(false);
            });
            LighterParticles.Clear();
        }
    }

    /// <summary>
    /// Handles shooting effects
    /// </summary>
    protected virtual void LighterEffectsCL(string particleName)
    {
        Game.AssertClient();

        DoLighterEffects(particleName);
    }

    protected virtual void DoLighterEffects(string particleName, float vmScale = 1f, float wmScale = 3f)
    {
        ModelEntity effectModel = GetEffectModel();
        if (effectModel == null) return;

        var isViewModel = IsLocalPawn && IsFirstPersonMode;
        (ModelEntity effectEntity, string attachment) = GetParticleEffectData(particleName);
        var particle = Particles.Create(particleName, effectEntity, attachment);
        var scale = isViewModel ? vmScale : wmScale;
        particle.Set("scale", scale);

        if (particleName == ParticleFire || particleName == ParticleLight) {
            LighterParticles.Add(particle);
        }
    }
}
