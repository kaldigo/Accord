using System.Collections.Generic;

namespace Accord;

/// <summary>User-facing scope of each global choice, independent of legacy pack descriptions.</summary>
internal static class JobIntents
{
    private const string Targeted = "No extra automatic support. Targeted aid, recovery and movement retain their own controls.";
    private const string Native = "No extra mechanic switch. Normal gauge, combo and accepted-phase handling remains on ST/AoE.";
    private const string Tank = "Conditional pack/tankbuster defenses on ST/AoE. Manual defense, invulnerability and targeted protection remain separate.";
    private const string Feint = "Conditional Feint on ST/AoE; retain manual timing. Existing self-recovery thresholds remain in effect.";
    private const string Healing = "Healing buttons continue their reviewed healing selectors in either mode; this switch does not turn healing off.";

    internal static readonly IReadOnlyDictionary<uint, string[]> Jobs = new Dictionary<uint, string[]>
    {
        [19] = ["Fight or Flight; native Requiescat and spell-chain follow-through.", Tank, Targeted, Native],
        [21] = ["Berserk / Inner Release; native gauge and follow-through.", Tank, Targeted, Native],
        [32] = ["Delirium / Blood Weapon and Living Shadow; a confirmed start hands the remaining buff to ST/AoE.", Tank, Targeted, Native],
        [37] = ["Bloodfest and No Mercy; resume ST/AoE after initiation for the remaining buff and cartridge sequences.", Tank, Targeted, Native],
        [24] = ["Presence of Mind; Glare IV continues from its status.", "Conditional Asylum and Temperance for incoming party damage.", Healing, "Lilies remain on the healing/damage intents. Bell placement and early release remain deliberate."],
        [28] = ["Chain Stratagem; Baneful Impaction continues from its status.", "Conditional Sacred Soil. The dedicated selector retains fairy/shield preparation; Expedient stays deliberate.", Healing, "Conditional Dissipation on healing intents. Fairy healing and Aetherflow follow their existing conditions."],
        [33] = ["Divination; Oracle continues from its status.", "Conditional Collective Unconscious and Neutral Sect for incoming party damage.", "Cards belong to class mechanics. Healing selectors, Synastry and Macrocosmos keep their distinct intents.", "Draw initiation is delegated in A. Held cards continue in either mode, including eligible allied cards from ST/AoE damage using the resolved recipient."],
        [40] = ["No additional burst initiator.", "Conditional Kerachole and Holos for incoming party damage.", "Initially assign missing Kardia through ST/AoE damage using the healing target stack. Never move an existing assignment. Kardia and Pneuma retain manual controls.", "Addersgall, Rhizomata and Eukrasia retain their existing healing/damage intent paths."],
        [20] = ["Brotherhood / Riddle of Fire, with the remaining buff on ST/AoE after initiation.", Feint, Targeted, "Perfect Balance initiation; ST/AoE completes the accepted forms and Blitz in either mode."],
        [22] = ["Lance Charge / Battle Litany, with the remaining buff on ST/AoE after initiation.", Feint, Targeted, "Geirskogul / Life initiation; accepted Life procs continue in either mode."],
        [30] = ["Mug / Dokumori and Trick Attack / Kunai's Bane. The Hide selector initiates; ST/AoE follows through.", Feint, Targeted, "Kassatsu and Ten Chi Jin initiation. Accepted mudras and the TCJ Meisui follow-up continue in either mode. Hide/Kassatsu retain prepull access."],
        [34] = ["Ikishoten; Ogi, Kaeshi and Zanshin continue through native status/resource checks.", Feint, Targeted, "Meikyo initiation; accepted Meikyo combos continue in either mode. Meditate remains deliberate."],
        [39] = ["Arcane Circle; Plentiful Harvest follows native eligibility.", "Conditional Feint and Arcane Crest on ST/AoE. Retain manual protection timing.", Targeted, "Enshroud initiation; the accepted phase and finishers continue in either mode."],
        [41] = ["Serpent's Ire; native follow-through.", Feint, Targeted, "Reawaken initiation; the accepted phase and Legacy attacks continue in either mode."],
        [23] = ["Barrage selector initiates the buff group; ST/AoE follows through even when the manual order differs from automatic initiation.", "Conditional Troubadour on ST/AoE without stacking ranged party defenses.", "Conditional Paean and Nature's Minne on ST/AoE. Both retain manual access.", "Start a song cycle in A; a manually started active cycle continues in M. Song control remains available."],
        [31] = ["Barrel / Wildfire initiation; confirmed manual Hypercharge also hands off. Both damage paths follow through under existing timing checks.", "Conditional Dismantle and Tactician on ST/AoE, with non-stacking checks. Keep the manual defense selector.", Targeted, "Heat, battery and tools retain native automation. Flamethrower remains deliberate."],
        [38] = ["Devilment initiation. Dances belong to class mechanics.", "Conditional Shield Samba on ST/AoE; cannot interrupt a dance.", "Conditional Curing Waltz on ST/AoE using existing HP thresholds. Improvisation and manual Waltz remain available.", "Standard / Technical initiation. Accepted steps and finishers continue in M; manual dance buttons remain available."],
        [25] = ["Ley Lines initiation; normal elemental rotation continues.", "Conditional Addle on ST/AoE. Manaward remains deliberate.", Targeted, "Transpose / Umbral Soul preparation and movement keep their distinct controls."],
        [27] = ["Searing Light initiation; native demi and proc follow-through.", "Conditional Addle on ST/AoE and Radiant Aegis at the existing charge/timing conditions.", "Conditional Rekindle and Lux Solaris on ST/AoE. Astral Flow retains deliberate movement and targeted Rekindle access.", Native],
        [35] = ["Embolden / Manafication; ST/AoE follows through after confirmed initiation.", "Conditional Addle and Magick Barrier on ST/AoE with non-stacking checks.", "Vercure and resurrection remain deliberate; no extra automatic support.", "Melee sequence and finishers remain native; movement stays deliberate."],
        [42] = ["Starry Muse initiation; native portrait, hammer and palette handling.", "Conditional Addle and Tempera Coat / Grassa on ST/AoE. Retain manual shielding.", Targeted, "Motif preparation remains native. Hammer and movement retain deliberate controls."],
    };

    internal static string Describe(uint job, VariantId variant) => Jobs.TryGetValue(job, out var descriptions)
        ? descriptions[(int)variant]
        : "Manual noncombat layout; this variant has no additional effect.";
}
