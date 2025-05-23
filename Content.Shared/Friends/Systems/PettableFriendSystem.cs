using Content.Shared.Chemistry.Components;
using Content.Shared.Friends.Components;
using Content.Shared.Interaction.Events;
using Content.Shared.NPC.Components;
using Content.Shared.NPC.Systems;
using Content.Shared.Popups;
using Content.Shared.Timing;
using Content.Shared._Shitmed.Spawners.EntitySystems; // Shitmed Change

namespace Content.Shared.Friends.Systems;

public sealed class PettableFriendSystem : EntitySystem
{
    [Dependency] private readonly NpcFactionSystem _factionException = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly UseDelaySystem _useDelay = default!;

    private EntityQuery<FactionExceptionComponent> _exceptionQuery;
    private EntityQuery<UseDelayComponent> _useDelayQuery;

    public override void Initialize()
    {
        base.Initialize();

        _exceptionQuery = GetEntityQuery<FactionExceptionComponent>();
        _useDelayQuery = GetEntityQuery<UseDelayComponent>();

        SubscribeLocalEvent<PettableFriendComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<PettableFriendComponent, GotRehydratedEvent>(OnRehydrated);
        SubscribeLocalEvent<PettableFriendComponent, SpawnerSpawnedEvent>(OnSpawned); // Shitmed Change
    }

    private void OnUseInHand(Entity<PettableFriendComponent> ent, ref UseInHandEvent args)
    {
        var (uid, comp) = ent;
        var user = args.User;
        if (args.Handled || !_exceptionQuery.TryGetComponent(uid, out var exceptionComp))
            return;

        if (_useDelayQuery.TryGetComponent(uid, out var useDelay) && !_useDelay.TryResetDelay((uid, useDelay), true))
            return;

        var exception = (uid, exceptionComp);
        if (_factionException.IsIgnored(exception, user))
        {
            _popup.PopupClient(Loc.GetString(comp.FailureString, ("target", uid)), user, user);
            return;
        }

        // you have made a new friend :)
        _popup.PopupClient(Loc.GetString(comp.SuccessString, ("target", uid)), user, user);
        _factionException.IgnoreEntity(exception, user);
        args.Handled = true;
    }

    private void OnRehydrated(Entity<PettableFriendComponent> ent, ref GotRehydratedEvent args)
    {
        // can only pet before hydrating, after that the fish cannot be negotiated with
        if (!TryComp<FactionExceptionComponent>(ent, out var comp))
            return;

        _factionException.IgnoreEntities(args.Target, comp.Ignored);
    }

    // Shitmed Change
    private void OnSpawned(Entity<PettableFriendComponent> ent, ref SpawnerSpawnedEvent args)
    {
        if (!TryComp<FactionExceptionComponent>(ent, out var comp))
            return;

        _factionException.IgnoreEntities(args.Entity, comp.Ignored);
    }
}
