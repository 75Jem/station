using Content.Server._Goobstation.Blob.Components;
using Content.Shared._Goobstation.Blob;
using Content.Server.GameTicking;
using Content.Shared._Goobstation.Blob.Components;
using Content.Shared.FixedPoint;
using Content.Shared.Popups;

namespace Content.Server._Goobstation.Blob;

public sealed class BlobResourceSystem : EntitySystem
{
    [Dependency] private readonly BlobCoreSystem _blobCoreSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    private EntityQuery<BlobTileComponent> _blobTile;
    private EntityQuery<BlobCoreComponent> _blobCore;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BlobResourceComponent, BlobSpecialGetPulseEvent>(OnPulsed);
        SubscribeLocalEvent<BlobResourceComponent, BlobNodePulseEvent>(OnPulsed);
        SubscribeLocalEvent<RoundEndTextAppendEvent>(OnRoundEnd);
        _blobTile = GetEntityQuery<BlobTileComponent>();
        _blobCore = GetEntityQuery<BlobCoreComponent>();
    }

    private void OnPulsed<T>(EntityUid uid, BlobResourceComponent component, T args)
    {
        if (!_blobTile.TryComp(uid, out var blobTileComponent) || blobTileComponent.Core == null)
            return;

        if (!_blobCore.TryComp(blobTileComponent.Core, out var blobCoreComponent) ||
            blobCoreComponent.Observer == null)
            return;

        _popup.PopupEntity(Loc.GetString("blob-get-resource", ("point", component.PointsPerPulsed)),
            uid,
            blobCoreComponent.Observer.Value,
            PopupType.Large);

        var points = component.PointsPerPulsed;

        if (blobCoreComponent.CurrentChem == BlobChemType.RegenerativeMateria)
        {
            points += 1;
        }

        if (_blobCoreSystem.ChangeBlobPoint(blobTileComponent.Core.Value, points))
        {
            _popup.PopupClient(Loc.GetString("blob-get-resource", ("point", points)),
                uid,
                blobCoreComponent.Observer.Value,
                PopupType.Large);
        }
    }
    /// <summary>
    /// On round end makes all the blobs resource nodes generate 100 points each pulse.
    /// </summary>
    /// <param name="args"></param>
    private void OnRoundEnd(RoundEndTextAppendEvent args)
    {
        var query = EntityQueryEnumerator<BlobResourceComponent>();
        while(query.MoveNext(out var resource))
            resource.PointsPerPulsed = 100;
    }
}
