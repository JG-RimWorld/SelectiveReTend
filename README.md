# Selective ReTend

Selective ReTend is an independent RimWorld 1.6 mod inspired by the idea behind
ReTend. It lets the player order a doctor to improve an existing treatment while
controlling priorities, medicine use, and the number of attempts.

## Default behaviour

1. Infections: enabled, 70% target, best allowed medicine.
2. Immunizable diseases: enabled, 60% target, best allowed medicine.
3. Injuries: disabled, 60% target, weakest medicine reasonably capable of
   reaching the target.

All three categories can be enabled or disabled independently.

The default maximum is one attempt per order. Zero means unlimited attempts,
subject to medicine availability and mathematical reachability.

## Balance safeguards

- Hediffs with `disappearsAtTotalTendQuality >= 0` are never eligible.
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

RimWorld 1.6's native `HediffDef.isInfection` is recognized automatically.

## Build

From `Source`:

```bash
dotnet build -c Release
```

The project uses `Krafs.Rimworld.Ref` and writes the compiled assembly to
`1.6/Assemblies`.

## Credits

Concept inspired by ReTend by Temmie3754 and MrKev. No source code from ReTend
is included.
