# Changelog

## 0.2.0

- The contextual menu now names the selected hediff itself (`Flu`,
  `Lung rot`, `Infection`, and so on) instead of displaying only its broad
  treatment category.
- Wound infection is the only vanilla hediff assigned infection priority.
  Modded hediffs can opt into that priority with
  `SelectiveReTendExtension.isInfection`.
- Eligibility no longer requires natural immunity gain. Any already-tended,
  non-permanent hediff with a live `HediffComp_TendDuration` can be re-tended.
  This covers lung rot, blood rot, fibrous mechanites, and sensory mechanites.
- Hediffs cured through accumulated tend quality remain excluded through the
  generic `disappearsAtTotalTendQuality` check.
- Added explicit source, assembly, and settings-screen version identifiers so
  builds can be distinguished reliably.
- Added the Workshop preview, publication descriptions, tags, release note,
  repository URL, and publication checklist.

## 0.1.0

- Initial source release.
