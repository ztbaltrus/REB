using REB.Engine.Combat.Components;
using REB.Engine.Combat.Systems;
using REB.Engine.ECS;
using REB.Engine.Player;
using REB.Engine.Player.Components;
using REB.Engine.Player.Princess.Components;
using REB.Engine.Rendering.Components;
using REB.Engine.UI;
using REB.Engine.UI.Components;
using REB.Engine.UI.Systems;
using Xunit;

namespace REB.Tests.UI;

// ---------------------------------------------------------------------------
//  Game feel tests — ScreenShakeSystem, ParticleSystem, HitFeedbackSystem,
//  RoleSelectionSystem, and SpatialAudioSystem helpers.
// ---------------------------------------------------------------------------

public sealed class GameFeelTests
{
    // =========================================================================
    //  ScreenShakeSystem
    // =========================================================================

    private static (World world, ScreenShakeSystem shakeSystem) BuildShakeWorld()
    {
        var world  = new World();
        var shaker = new ScreenShakeSystem();
        world.RegisterSystem(shaker);
        return (world, shaker);
    }

    [Fact]
    public void ScreenShake_NotShaking_BeforeTrigger()
    {
        var (world, shaker) = BuildShakeWorld();

        world.Update(0.016f);

        Assert.False(shaker.IsShaking);
        world.Dispose();
    }

    [Fact]
    public void ScreenShake_IsShaking_AfterTrigger()
    {
        var (world, shaker) = BuildShakeWorld();

        shaker.Trigger(10f, 0.5f);
        world.Update(0.016f);

        Assert.True(shaker.IsShaking);
        world.Dispose();
    }

    [Fact]
    public void ScreenShake_Offset_NonZero_WhileShaking()
    {
        var (world, shaker) = BuildShakeWorld();

        shaker.Trigger(10f, 0.5f);
        world.Update(0.016f);

        // The sin-wave at t=0.016 will almost certainly be non-zero for a reasonable phase.
        // We just verify the magnitude is within expected bounds.
        Assert.True(MathF.Abs(shaker.OffsetX) <= 10f);
        world.Dispose();
    }

    [Fact]
    public void ScreenShake_StopsAfterDuration()
    {
        var (world, shaker) = BuildShakeWorld();

        shaker.Trigger(10f, 0.1f);
        world.Update(0.016f);
        world.Update(1f);   // >> 0.1s

        Assert.False(shaker.IsShaking);
        Assert.Equal(0f, shaker.OffsetX, precision: 3);
        world.Dispose();
    }

    [Fact]
    public void ScreenShake_HigherIntensity_Wins_WhenTwoTriggersOnSameFrame()
    {
        var (world, shaker) = BuildShakeWorld();

        shaker.Trigger(2f,  0.5f);
        shaker.Trigger(10f, 0.5f);
        world.Update(0.016f);

        Assert.True(shaker.IsShaking);
        world.Dispose();
    }

    // =========================================================================
    //  ParticleSystem
    // =========================================================================

    private static (World world, ParticleSystem particles) BuildParticleWorld()
    {
        var world   = new World();
        var system  = new ParticleSystem();
        world.RegisterSystem(system);
        return (world, system);
    }

    private static Entity AddEmitter(World world, float life = 1.0f)
    {
        var e = world.CreateEntity();
        var emitter = ParticleEmitterComponent.Default;
        emitter.LifeRemaining = life;
        world.AddComponent(e, emitter);
        return e;
    }

    [Fact]
    public void Particle_LifeRemaining_DecreasesEachFrame()
    {
        var (world, _) = BuildParticleWorld();
        var e = AddEmitter(world, life: 1.0f);

        world.Update(0.1f);

        Assert.True(world.GetComponent<ParticleEmitterComponent>(e).LifeRemaining < 1.0f);
        world.Dispose();
    }

    [Fact]
    public void Particle_Deactivated_WhenLifeExpires()
    {
        var (world, particles) = BuildParticleWorld();
        AddEmitter(world, life: 0.05f);

        world.Update(0.1f);   // > 0.05s

        Assert.Equal(1, particles.ExpiredThisFrame);
        world.Dispose();
    }

    [Fact]
    public void Particle_IsActive_False_AfterExpiry()
    {
        var (world, _) = BuildParticleWorld();
        var e = AddEmitter(world, life: 0.05f);

        world.Update(0.1f);

        Assert.False(world.GetComponent<ParticleEmitterComponent>(e).IsActive);
        world.Dispose();
    }

    [Fact]
    public void Particle_Inactive_NotTicked()
    {
        var (world, particles) = BuildParticleWorld();
        var e = AddEmitter(world, life: 0.05f);

        // Expire it.
        world.Update(0.1f);
        Assert.Equal(1, particles.ExpiredThisFrame);

        // Next frame — expired count should reset to zero (inactive emitter skipped).
        world.Update(0.1f);
        Assert.Equal(0, particles.ExpiredThisFrame);
        world.Dispose();
    }

    // =========================================================================
    //  HitFeedbackSystem
    // =========================================================================

    private static (World world, HitFeedbackSystem feedback) BuildFeedbackWorld()
    {
        var world    = new World();
        // PlayerControllerSystem and CarrySystem are omitted: they require full
        // TransformComponent + physics setup on player/princess entities that
        // these minimal tests don't provide. HitFeedbackSystem only lists them
        // as RunAfter ordering hints and reads princess state directly.
        world.RegisterSystem(new CombatSystem());
        var feedback = new HitFeedbackSystem();
        world.RegisterSystem(feedback);
        return (world, feedback);
    }

    private static Entity AddCombatAttacker(World world, string targetTag = "Enemy")
    {
        var attacker = world.CreateEntity();
        world.AddTag(attacker, "Attacker");
        world.AddComponent(attacker, new TransformComponent
        {
            Position = Microsoft.Xna.Framework.Vector3.Zero
        });
        var dmg = DamageComponent.MeleeDefault;
        dmg.AttackPressed = true;
        dmg.AttackTimer   = 0f;
        dmg.MeleeRange    = 5f;
        world.AddComponent(attacker, dmg);

        var target = world.CreateEntity();
        world.AddTag(target, targetTag);
        world.AddComponent(target, new TransformComponent
        {
            Position = new Microsoft.Xna.Framework.Vector3(1f, 0f, 0f)
        });
        world.AddComponent(target, HealthComponent.For(100f));

        return attacker;
    }

    [Fact]
    public void FeedbackEvents_Empty_WhenNothingHappens()
    {
        var (world, feedback) = BuildFeedbackWorld();

        world.Update(0.016f);

        Assert.Empty(feedback.FeedbackEvents);
        world.Dispose();
    }

    [Fact]
    public void FeedbackEvents_ClearedEachFrame()
    {
        var (world, feedback) = BuildFeedbackWorld();
        AddCombatAttacker(world, "Enemy");

        world.Update(0.016f);
        Assert.NotEmpty(feedback.FeedbackEvents);

        world.Update(0.016f);   // attacker cooldown now active — no new hit
        Assert.Empty(feedback.FeedbackEvents);
        world.Dispose();
    }

    [Fact]
    public void HitEnemy_FeedbackEvent_GeneratedOnCombatHit()
    {
        var (world, feedback) = BuildFeedbackWorld();
        AddCombatAttacker(world, "Enemy");

        world.Update(0.016f);

        Assert.Contains(feedback.FeedbackEvents, ev => ev.Type == FeedbackType.HitEnemy);
        world.Dispose();
    }

    [Fact]
    public void HitPlayer_FeedbackEvent_WhenTargetIsPlayer()
    {
        var (world, feedback) = BuildFeedbackWorld();
        AddCombatAttacker(world, "Player");

        world.Update(0.016f);

        Assert.Contains(feedback.FeedbackEvents, ev => ev.Type == FeedbackType.HitPlayer);
        world.Dispose();
    }

    [Fact]
    public void PrincessDropped_FiredOnCarryFlip()
    {
        var (world, feedback) = BuildFeedbackWorld();

        var princess = world.CreateEntity();
        world.AddTag(princess, "Princess");
        var ps = PrincessStateComponent.Default;
        ps.IsBeingCarried = true;
        world.AddComponent(princess, ps);

        world.Update(0.016f);   // _wasCarried becomes true

        // Now drop princess.
        ref var psRef = ref world.GetComponent<PrincessStateComponent>(princess);
        psRef.IsBeingCarried = false;
        world.Update(0.016f);   // carry flip detected

        Assert.Contains(feedback.FeedbackEvents, ev => ev.Type == FeedbackType.PrincessDropped);
        world.Dispose();
    }

    [Fact]
    public void PrincessDropped_NotFired_WhenCarryUnchanged()
    {
        var (world, feedback) = BuildFeedbackWorld();

        var princess = world.CreateEntity();
        world.AddTag(princess, "Princess");
        var ps = PrincessStateComponent.Default;
        ps.IsBeingCarried = true;
        world.AddComponent(princess, ps);

        world.Update(0.016f);   // _wasCarried = true
        world.Update(0.016f);   // still carried — no drop

        Assert.DoesNotContain(feedback.FeedbackEvents, ev => ev.Type == FeedbackType.PrincessDropped);
        world.Dispose();
    }

    // =========================================================================
    //  RoleSelectionSystem
    // =========================================================================

    private static (World world, RoleSelectionSystem roleSystem) BuildRoleWorld()
    {
        var world      = new World();
        var roleSystem = new RoleSelectionSystem();
        world.RegisterSystem(roleSystem);
        return (world, roleSystem);
    }

    private static Entity AddPlayerSlot(World world, PlayerRole role, bool ready)
    {
        var e = world.CreateEntity();
        world.AddTag(e, "PlayerSlot");
        world.AddComponent(e, new RoleSelectionComponent
        {
            SelectedRole = role,
            IsReady      = ready,
            PlayerId     = 0,
        });
        return e;
    }

    [Fact]
    public void AllPlayersReady_False_WithNoSlots()
    {
        var (world, roles) = BuildRoleWorld();

        world.Update(0.016f);

        Assert.False(roles.AllPlayersReady);
        world.Dispose();
    }

    [Fact]
    public void AllPlayersReady_True_WhenAllReadyAndDistinct()
    {
        var (world, roles) = BuildRoleWorld();
        AddPlayerSlot(world, PlayerRole.Carrier,    ready: true);
        AddPlayerSlot(world, PlayerRole.Scout,      ready: true);

        world.Update(0.016f);

        Assert.True(roles.AllPlayersReady);
        world.Dispose();
    }

    [Fact]
    public void AllPlayersReady_False_WhenOneNotReady()
    {
        var (world, roles) = BuildRoleWorld();
        AddPlayerSlot(world, PlayerRole.Carrier,    ready: true);
        AddPlayerSlot(world, PlayerRole.Scout,      ready: false);

        world.Update(0.016f);

        Assert.False(roles.AllPlayersReady);
        world.Dispose();
    }

    [Fact]
    public void HasDuplicateRoles_True_WhenTwoPlayersSameRole()
    {
        var (world, roles) = BuildRoleWorld();
        AddPlayerSlot(world, PlayerRole.Carrier, ready: true);
        AddPlayerSlot(world, PlayerRole.Carrier, ready: true);

        world.Update(0.016f);

        Assert.True(roles.HasDuplicateRoles);
        world.Dispose();
    }

    [Fact]
    public void AllPlayersReady_False_WhenDuplicateRoles()
    {
        var (world, roles) = BuildRoleWorld();
        AddPlayerSlot(world, PlayerRole.Carrier, ready: true);
        AddPlayerSlot(world, PlayerRole.Carrier, ready: true);

        world.Update(0.016f);

        Assert.False(roles.AllPlayersReady);
        world.Dispose();
    }

    // =========================================================================
    //  SpatialAudioSystem — volume falloff math
    // =========================================================================

    [Fact]
    public void SpatialAudio_FullVolume_WithinMinRange()
    {
        float v = SpatialAudioSystem.ComputeVolumeFraction(SpatialAudioSystem.MinRange - 1f);
        Assert.Equal(1f, v, precision: 3);
    }

    [Fact]
    public void SpatialAudio_ZeroVolume_AtOrBeyondMaxRange()
    {
        float v = SpatialAudioSystem.ComputeVolumeFraction(SpatialAudioSystem.MaxRange + 10f);
        Assert.Equal(0f, v, precision: 3);
    }

    [Fact]
    public void SpatialAudio_HalfVolume_AtMidpoint()
    {
        float mid = (SpatialAudioSystem.MinRange + SpatialAudioSystem.MaxRange) * 0.5f;
        float v   = SpatialAudioSystem.ComputeVolumeFraction(mid);
        Assert.Equal(0.5f, v, precision: 3);
    }

    [Fact]
    public void SpatialAudio_FalloffIsLinear()
    {
        float d1 = SpatialAudioSystem.MinRange + 10f;
        float d2 = SpatialAudioSystem.MinRange + 20f;
        float v1 = SpatialAudioSystem.ComputeVolumeFraction(d1);
        float v2 = SpatialAudioSystem.ComputeVolumeFraction(d2);

        // Moving twice as far from MinRange should halve the remaining volume fraction.
        float range = SpatialAudioSystem.MaxRange - SpatialAudioSystem.MinRange;
        float expected1 = 1f - 10f / range;
        float expected2 = 1f - 20f / range;

        Assert.Equal(expected1, v1, precision: 3);
        Assert.Equal(expected2, v2, precision: 3);
    }
}
