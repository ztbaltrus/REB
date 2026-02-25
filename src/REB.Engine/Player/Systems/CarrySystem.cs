using Microsoft.Xna.Framework;
using REB.Engine.ECS;
using REB.Engine.Player.Components;
using REB.Engine.Player.Princess;
using REB.Engine.Player.Princess.Components;
using REB.Engine.Rendering.Components;

namespace REB.Engine.Player.Systems;

/// <summary>
/// Manages picking up, dropping, and handing off the princess between carriers.
/// Also drives the princess's mood/health decay and struggle bursts.
/// <para>Pipeline per frame:</para>
/// <list type="number">
///   <item>Pick-up: uncarried player presses Interact near the princess.</item>
///   <item>Drop: carrier presses Drop.</item>
///   <item>Handoff: carrier presses Interact near another carrier.</item>
///   <item>Position update: snap princess to carrier's carry offset.</item>
///   <item>Mood update: drain health, tier mood, trigger struggles.</item>
/// </list>
/// </summary>
[RunAfter(typeof(PlayerControllerSystem))]
public sealed class CarrySystem : GameSystem
{
    // Health thresholds that define mood tiers.
    private const float ThresholdNervous  = 75f;
    private const float ThresholdUpset    = 50f;
    private const float ThresholdFurious  = 25f;

    // How long each struggle burst lasts (seconds).
    private const float StruggleDuration  = 1.5f;

    public override void Update(float deltaTime)
    {
        // Handoff must run before pick-up so that a carrier pressing Interact
        // attempts to hand off first (while IsCarrying=true); only then, if no
        // handoff target is found, can an un-carrying player pick up the princess.
        ProcessHandoff();
        ProcessPickUp();
        ProcessDrop();
        UpdateCarried(deltaTime);
    }

    // =========================================================================
    //  Pick-up
    // =========================================================================

    private void ProcessPickUp()
    {
        Entity princess = FindPrincess();
        if (!World.IsAlive(princess)) return;
        if (!World.HasComponent<PrincessStateComponent>(princess)) return;

        ref var ps         = ref World.GetComponent<PrincessStateComponent>(princess);
        if (ps.IsBeingCarried) return;

        var princessPos = World.GetComponent<TransformComponent>(princess).Position;

        foreach (var carrier in
            World.Query<CarryComponent, PlayerInputComponent, TransformComponent>())
        {
            ref var carry  = ref World.GetComponent<CarryComponent>(carrier);
            ref var pinput = ref World.GetComponent<PlayerInputComponent>(carrier);

            if (!pinput.InteractPressed || carry.IsCarrying) continue;

            var tf   = World.GetComponent<TransformComponent>(carrier);
            float dist = Vector3.Distance(tf.Position, princessPos);
            if (dist > carry.InteractRange) continue;

            carry.IsCarrying    = true;
            carry.CarriedEntity = princess;
            ps.IsBeingCarried   = true;
            ps.CarrierEntity    = carrier;
            break;  // only one carrier can pick up per frame
        }
    }

    // =========================================================================
    //  Drop
    // =========================================================================

    private void ProcessDrop()
    {
        foreach (var carrier in World.Query<CarryComponent, PlayerInputComponent>())
        {
            ref var carry  = ref World.GetComponent<CarryComponent>(carrier);
            ref var pinput = ref World.GetComponent<PlayerInputComponent>(carrier);

            if (!carry.IsCarrying || !pinput.DropPressed) continue;
            DropPrincess(carrier, ref carry);
        }
    }

    private void DropPrincess(Entity carrier, ref CarryComponent carry)
    {
        if (World.IsAlive(carry.CarriedEntity))
        {
            ref var ps    = ref World.GetComponent<PrincessStateComponent>(carry.CarriedEntity);
            ps.IsBeingCarried = false;
            ps.CarrierEntity  = Entity.Null;
        }
        carry.IsCarrying    = false;
        carry.CarriedEntity = Entity.Null;
    }

    // =========================================================================
    //  Handoff
    // =========================================================================

    private void ProcessHandoff()
    {
        // Find the active carrier.
        Entity activeCarrier = Entity.Null;
        foreach (var e in World.Query<CarryComponent>())
        {
            if (World.GetComponent<CarryComponent>(e).IsCarrying) { activeCarrier = e; break; }
        }
        if (!World.IsAlive(activeCarrier)) return;

        ref var activeCarry  = ref World.GetComponent<CarryComponent>(activeCarrier);
        ref var activePinput = ref World.GetComponent<PlayerInputComponent>(activeCarrier);
        if (!activePinput.InteractPressed) return;

        var activeTf = World.GetComponent<TransformComponent>(activeCarrier);

        foreach (var receiver in
            World.Query<CarryComponent, PlayerInputComponent, TransformComponent>())
        {
            if (receiver == activeCarrier) continue;

            ref var recCarry = ref World.GetComponent<CarryComponent>(receiver);
            if (recCarry.IsCarrying) continue;

            var   recTf = World.GetComponent<TransformComponent>(receiver);
            float dist  = Vector3.Distance(activeTf.Position, recTf.Position);
            if (dist > activeCarry.InteractRange) continue;

            // Transfer the carried entity to the receiver.
            recCarry.IsCarrying    = true;
            recCarry.CarriedEntity = activeCarry.CarriedEntity;

            if (World.IsAlive(activeCarry.CarriedEntity))
            {
                ref var ps    = ref World.GetComponent<PrincessStateComponent>(
                                    activeCarry.CarriedEntity);
                ps.CarrierEntity = receiver;
            }

            activeCarry.IsCarrying    = false;
            activeCarry.CarriedEntity = Entity.Null;
            break;
        }
    }

    // =========================================================================
    //  Carried entity update (position + mood)
    // =========================================================================

    private void UpdateCarried(float deltaTime)
    {
        foreach (var carrier in World.Query<CarryComponent, TransformComponent>())
        {
            var carry = World.GetComponent<CarryComponent>(carrier);
            if (!carry.IsCarrying || !World.IsAlive(carry.CarriedEntity)) continue;

            var carrierTf = World.GetComponent<TransformComponent>(carrier);

            // Snap princess to carry position above the carrier.
            ref var princessTf = ref World.GetComponent<TransformComponent>(carry.CarriedEntity);
            princessTf.Position = carrierTf.Position + Vector3.Up * carry.CarryOffsetY;

            // Update mood.
            if (World.HasComponent<PrincessStateComponent>(carry.CarriedEntity))
            {
                ref var ps = ref World.GetComponent<PrincessStateComponent>(carry.CarriedEntity);
                UpdateMood(ref ps, carrier, deltaTime);
            }
        }
    }

    private void UpdateMood(ref PrincessStateComponent ps, Entity carrier, float deltaTime)
    {
        float decay = ps.MoodDecayRate;

        // Negotiator role halves the decay rate.
        if (World.HasComponent<RoleComponent>(carrier))
        {
            var role = World.GetComponent<RoleComponent>(carrier);
            if (role.Role == PlayerRole.Negotiator) decay *= 0.5f;
        }

        ps.Health = MathHelper.Clamp(ps.Health - decay * deltaTime, 0f, 100f);

        // Derive mood tier from current health.
        ps.MoodLevel = ps.Health switch
        {
            >= ThresholdNervous => PrincessMoodLevel.Calm,
            >= ThresholdUpset   => PrincessMoodLevel.Nervous,
            >= ThresholdFurious => PrincessMoodLevel.Upset,
            _                   => PrincessMoodLevel.Furious,
        };

        // Trigger a struggle burst when health drops into Furious territory.
        if (ps.MoodLevel == PrincessMoodLevel.Furious && !ps.IsStruggling)
        {
            ps.IsStruggling = true;
            ps.StruggleTimer = StruggleDuration;
        }

        if (ps.IsStruggling)
        {
            ps.StruggleTimer -= deltaTime;
            if (ps.StruggleTimer <= 0f)
                ps.IsStruggling = false;
        }
    }

    // =========================================================================
    //  Helper
    // =========================================================================

    private Entity FindPrincess()
    {
        foreach (var e in World.GetEntitiesWithTag("Princess"))
            return e;
        return Entity.Null;
    }
}
