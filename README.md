# Selective ReTend

Current source version: **0.2.0**

Selective ReTend is an independent RimWorld 1.6 mod inspired by the idea behind
ReTend. It lets the player order a doctor to improve an existing treatment while
controlling priorities, medicine use, and the number of attempts.

![Selective ReTend preview](About/Preview.png)

## Default behaviour

1. Infections: enabled, 70% target, best allowed medicine.
2. Diseases and other treatable conditions: enabled, 60% target, best allowed
   medicine.
3. Injuries: disabled, 60% target, weakest medicine reasonably capable of
   reaching the target.

All three categories can be enabled or disabled independently.

The default maximum is one attempt per order. Zero means unlimited attempts,
subject to medicine availability and mathematical reachability.

## Balance safeguards

- Hediffs with `disappearsAtTotalTendQuality >= 0` are never eligible.
- Any other tended, non-permanent hediff with a live
  `HediffComp_TendDuration` is eligible even if it cannot develop natural
  immunity. This includes lung rot, blood rot, both vanilla mechanite
  conditions, asthma, and carcinoma.
- A re-tend replaces only the current quality when the new roll is better.
- It does not extend the existing tend duration.
- It does not grant repeatable medical XP, bonding attempts, records, or quest
  progress.
- Medicine is consumed even when the new roll is worse.

## Expected-quality calculation

For a finite number of attempts, the improvement filter uses the result that
has a 50% probability of being reached or exceeded at least once:

```text
reference = base - 0.25 + 0.5 × 0.5^(1 / attempts)
```

The result is clamped to the medicine's maximum quality.

## Compatibility extension

Modded hediffs can opt into infection priority or opt out entirely:

```xml
<modExtensions>
  <li Class="SelectiveReTend.SelectiveReTendExtension">
    <isInfection>true</isInfection>
    <exclude>false</exclude>
  </li>
</modExtensions>
```

The vanilla wound infection is recognized explicitly. RimWorld 1.6's
`HediffDef.isInfection` is intentionally not used because it also marks
ordinary diseases such as flu as infections.

## Build

From `Source`:

```bash
dotnet build -c Release
```

The project uses `Krafs.Rimworld.Ref` and writes the compiled assembly to
`1.6/Assemblies`.

## Download and installation

Download `SelectiveReTend-vX.Y.Z.zip` from the latest
[GitHub release](https://github.com/jgonc19/SelectiveReTend/releases/latest).
Extract its `SelectiveReTend` folder into RimWorld's local `Mods` directory,
then enable **Selective ReTend** in the game's mod manager.

Do not use GitHub's automatically generated **Source code** archives as the
installed mod: they do not contain the compiled DLL.

## Publishing a GitHub release

The release workflow compiles the DLL, creates an installable ZIP, validates
its structure and attaches it to a GitHub release.

1. Update `VERSION.txt`, `About/About.xml`, the project version in
   `Source/SelectiveReTend.csproj`, `CHANGELOG.md`, and the matching release
   notes in `Workshop`.
2. Commit and push those changes.
3. Create and push a tag matching `VERSION.txt`, for example:

   ```bash
   git tag -a v0.2.0 -m "Selective ReTend v0.2.0"
   git push origin v0.2.0
   ```

The workflow is stored in `.github/workflows/release.yml`. A tag whose name
does not exactly match `v` plus the contents of `VERSION.txt` is rejected.

## Credits

Concept inspired by ReTend by Temmie3754 and MrKev. No source code from ReTend
is included.

## Steam Workshop

Publication copy, tags, the full-size artwork and a checklist are kept in
[`Workshop`](Workshop/README.md). `About/Preview.png` is the image used
automatically by RimWorld's mod manager and Workshop publisher.
