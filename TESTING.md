# Manual test plan

Use a development save with dev mode enabled. Test first with Core only, then
repeat the compatibility cases with the normal mod list.

## Loading and settings

- The game loads with no red errors.
- Selective ReTend appears in the mod settings list.
- Defaults are infections on, diseases on, injuries off, 70/60/60 targets,
  small-improvement filter on, 10 percentage points, and one attempt.
- All settings survive save, return to menu, and reload.
- English and Spanish labels fit without clipping.

## Candidate selection and priority

- A tended wound below 60% produces no option while injuries are disabled.
- Enabling injuries makes that wound eligible.
- With an eligible infection and disease on the same pawn, the menu names the
  actual infection selected, not just its category.
- With an eligible disease and wound, the menu names the disease.
- A pawn with only flu produces a menu option naming flu and uses the disease
  target, never the infection target.
- Lung rot, blood rot, fibrous mechanites, and sensory mechanites all produce
  an option when their existing treatment is below the disease target.
- Intestinal worms and muscle parasites never produce an option.
- Permanent-tend and fully cured/immune hediffs never produce an option.

## Medicine policy

- Infection and disease choose the strongest reachable medicine permitted by
  the patient's medical-care setting.
- Lowering the patient's setting immediately lowers or removes the chosen
  medicine.
- Injury chooses no medicine, herbal, industrial, or glitterworld in that order
  according to the configured target and available attempts.
- Forbidden, unreachable, or reserved map medicine is not selected.
- Medicine already in the doctor's inventory can be used.
- One medicine is consumed for every roll, including failed rolls.
- Unused medicine picked up for a multi-attempt order returns to the doctor's
  inventory when the job ends.

## Quality and attempts

- A worse roll consumes an attempt but leaves the existing quality unchanged.
- A better roll replaces the quality.
- Re-tending does not increase the remaining treatment duration.
- One attempt performs exactly one roll.
- Two attempts stop after two rolls unless the target is reached first.
- Zero attempts repeats until the target is reached, medicine runs out, or the
  current setup cannot improve the treatment.
- Changing doctor, bed, self-tend state, medicine availability, or medical care
  during the order ends safely without a loop.

## Balance and records

- Re-tending grants no Medicine XP.
- It does not increment normal tend records, guest tending, bonding attempts, or
  quest tending signals.
- It never adds accumulated tend quality to a hediff.

## Job safety

- Self-tend is unavailable when self-tend is disabled.
- No-care patients cannot be re-tended.
- Aggressive mental-state patients are rejected consistently with vanilla
  tending.
- Drafted and undrafted doctors can use the option.
- Interrupting the job releases reservations.
- Saving during travel to medicine, during tending, and between attempts can be
  loaded without losing the target or creating a stuck job.
