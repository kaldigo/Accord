# Dancer Auto Mode precasting

Out of combat, Dancer's hands-free Auto Mode dispatch now requires the player's actual hard target to be a living, targetable enemy before either the single-target or AoE rotation is evaluated. Party members, friendly NPCs, dead or untargetable enemies, and no target do not authorize automatic precasting. Automatic target selection and a previous rotation target cannot substitute for that selection.

The old flow could evaluate a rotation using an automatic/previous target, then execute self-targeted dances even when the player had selected an ally. The guard applies before rotation evaluation, including the out-of-combat buff-bypass path, simple/advanced rotations and automatic targeting modes.

In-combat targeting, other jobs, manual dance buttons and normal cooldown/status checks retain their existing behaviour. This is a Wrath Combo engine fix; Accord settings and hotbars do not need changes.

Validation compiles the actual DPS dispatch method with simulated game/rotation boundaries and checks 80 combinations of job, combat state, selected target, manual/automatic targeting and single-target/AoE dispatch. A hostile automatic override is deliberately present even for friendly/no-target cases. Normal build, client compatibility and release packaging checks also run. Native in-game acceptance remains separate.
