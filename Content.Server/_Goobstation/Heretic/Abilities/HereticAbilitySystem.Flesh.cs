using Content.Server.Body.Components;
using Content.Shared.Body.Components;
using Content.Shared.Body.Organ;
using Content.Shared.Body.Part;
using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Content.Shared.Heretic;
using Content.Shared.Popups;
using Robust.Shared.Audio;
using Robust.Shared.Player;

namespace Content.Server.Heretic.Abilities;

public sealed partial class HereticAbilitySystem : EntitySystem
{
    private void SubscribeFlesh()
    {
        SubscribeLocalEvent<HereticComponent, EventHereticFleshSurgery>(OnFleshSurgery);
        SubscribeLocalEvent<HereticComponent, EventHereticFleshSurgeryDoAfter>(OnFleshSurgeryDoAfter);
        SubscribeLocalEvent<HereticComponent, HereticAscensionFleshEvent>(OnAscensionFlesh);
    }

    private void OnFleshSurgery(Entity<HereticComponent> ent, ref EventHereticFleshSurgery args)
    {
        if (!TryUseAbility(ent, args))
            return;

        if (HasComp<GhoulComponent>(args.Target)
        || (TryComp<HereticComponent>(args.Target, out var th) && th.CurrentPath == ent.Comp.CurrentPath))
        {
            var dargs = new DoAfterArgs(EntityManager, ent, 10f, new EventHereticFleshSurgeryDoAfter(args.Target), ent, args.Target)
            {
                BreakOnDamage = true,
                BreakOnMove = true,
                BreakOnHandChange = false,
            };
            _doafter.TryStartDoAfter(dargs);
            args.Handled = true;
            return;
        }

        // remove a random organ
        if (TryComp<BodyComponent>(args.Target, out var body))
        {
            _vomit.Vomit(args.Target, -1000, -1000); // You feel hollow!

            switch (_random.Next(0, 3))
            {
                // remove stomach
                case 0:
                    foreach (var entity in GetBodyOrganEntityComps<StomachComponent>((args.Target, body)))
                        QueueDel(entity.Owner);

                    _popup.PopupEntity(Loc.GetString("admin-smite-stomach-removal-self"), args.Target,
                        args.Target, PopupType.LargeCaution);
                    break;

                // remove random hand
                case 1:
                    var baseXform = Transform(args.Target);
                    foreach (var part in _body.GetBodyChildrenOfType(args.Target, BodyPartType.Hand, body))
                    {
                        _transform.AttachToGridOrMap(part.Id);
                        break;
                    }
                    _popup.PopupEntity(Loc.GetString("admin-smite-remove-hands-self"), args.Target, args.Target, PopupType.LargeCaution);
                    _popup.PopupCoordinates(Loc.GetString("admin-smite-remove-hands-other", ("name", args.Target)), baseXform.Coordinates,
                        Filter.PvsExcept(args.Target), true, PopupType.Medium);
                    break;

                // remove lungs
                case 2:
                    foreach (var entity in GetBodyOrganEntityComps<LungComponent>((args.Target, body)))
                        QueueDel(entity.Owner);

                    _popup.PopupEntity(Loc.GetString("admin-smite-lung-removal-self"), args.Target,
                        args.Target, PopupType.LargeCaution);
                    break;

                default:
                    break;
            }
        }

        args.Handled = true;
    }
    private void OnFleshSurgeryDoAfter(Entity<HereticComponent> ent, ref EventHereticFleshSurgeryDoAfter args)
    {
        if (args.Cancelled)
            return;

        if (args.Target == null) // shouldn't really happen. just in case
            return;

        if (!TryComp<DamageableComponent>(args.Target, out var dmg))
            return;

        // heal teammates, mostly ghouls
        _dmg.SetAllDamage((EntityUid) args.Target, dmg, 0);
        args.Handled = true;
    }
    private void OnAscensionFlesh(Entity<HereticComponent> ent, ref HereticAscensionFleshEvent args)
    {
        var urist = _poly.PolymorphEntity(ent, "EldritchHorror");
        if (urist == null)
            return;

        _aud.PlayPvs(new SoundPathSpecifier("/Audio/Animals/space_dragon_roar.ogg"), (EntityUid) urist, AudioParams.Default.AddVolume(2f));
    }

    /// <summary>
    /// Returns a list of Entity<<see cref="T"/>, <see cref="OrganComponent"/>>
    /// for each organ of the body
    /// </summary>
    /// <typeparam name="T">The component that we want to return</typeparam>
    /// <param name="entity">The body to check the organs of</param>
    /// PIRATE, HELPER method from Content.Shared.Body.Systems.SharedBodySystem.Organs.cs
    public List<Entity<T, OrganComponent>> GetBodyOrganEntityComps<T>(
        Entity<BodyComponent?> entity)
        where T : IComponent
    {
        if (!Resolve(entity, ref entity.Comp))
            return new List<Entity<T, OrganComponent>>();

        var query = GetEntityQuery<T>();
        var list = new List<Entity<T, OrganComponent>>(3);
        foreach (var organ in _body.GetBodyOrgans(entity.Owner, entity.Comp))
        {
            if (query.TryGetComponent(organ.Id, out var comp))
                list.Add((organ.Id, comp, organ.Component));
        }

        return list;
    }
}
