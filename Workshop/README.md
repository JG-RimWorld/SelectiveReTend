# Steam Workshop publication material

## Files used automatically

- `About/Preview.png` is the 640×360 primary preview used by RimWorld's mod
  manager and Workshop publisher. It is below Steam's 1 MiB limit.
- `About/About.xml` supplies the mod name, author, version, supported game
  version, source URL and in-game description.

## Fields to enter manually

- Title: `Selective ReTend`
- Description: copy `Description.en.txt` into the English Workshop page.
  `Description.es.txt` is ready if the page is later localized into Spanish.
- Tags: select `Mod` and `1.6`, as listed in `Tags.txt`.
- Visibility for a first upload: `Unlisted` or `Friends-only` until the
  Workshop download has been tested.
- Change note: copy `ReleaseNotes-0.2.0.txt`.

The full-size `Preview-1280x720.png` is retained as artwork for the repository,
social posts or an additional Workshop image. The publisher should use
`About/Preview.png` as the primary thumbnail.

## Do not create PublishedFileId.txt in advance

RimWorld creates `About/PublishedFileId.txt` after the first successful Workshop
upload. It contains the numeric Workshop item ID. Preserve and commit that file
after publication: future uploads use it to update the same item instead of
creating a duplicate.

If a Workshop item is permanently deleted, remove the stale
`PublishedFileId.txt` before attempting a new first upload.

## Pre-publication checklist

1. Build the Release configuration and confirm that
   `1.6/Assemblies/SelectiveReTend.dll` exists.
2. Test with only Core and Selective ReTend enabled.
3. Run the cases in `TESTING.md` and check the player log for errors.
4. Confirm `About/Preview.png` appears in the in-game mod list.
5. Upload initially with restricted visibility.
6. Subscribe to or download the uploaded copy and test that exact copy.
7. Preserve `About/PublishedFileId.txt`.
8. Make the page public only after the Workshop copy passes the smoke test.
