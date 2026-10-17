"""Render the reviewed semantic action-access ledger; does not generate game bindings.

Ownership/parents below are reviewed data, not inferred from preset names. Native
command IDs must be resolved from game sheets by the future PvP layout compiler.
"""
from pathlib import Path
import hashlib, json

ROOT = Path(__file__).resolve().parents[1]
inventory = json.loads((ROOT / 'docs/job-research/pvp-inventory.json').read_text())

# Main native combo intent, followed by explicit non-main transformation families.
MAIN = dict(zip('PLD WAR DRK GNB WHM SCH AST SGE MNK DRG NIN SAM RPR VPR BRD MCH DNC BLM SMN RDM PCT'.split(),
    ['Fast Blade combo','Heavy Swing combo','Hard Slash combo','Keen Edge combo','Glare III','Broil IV','Fall Malefic','Dosis III','Dragon Kick combo','Raiden Thrust combo','Spinning Edge combo','Yukikaze combo','Slice combo','Steel Fangs combo','Powerful Shot','Blast Charge','Cascade combo','Fire','Ruin III','Jolt III','Fire in Red']))
FAMILIES = {
 'PLD': {'Imperator':'Confiteor','Phalanx':'Blade of Faith|Blade of Truth|Blade of Valor'},
 'WAR': {'Primal Rend':'Primal Ruination','Blota':'Inner Chaos','Bloodwhetting':'Chaotic Cyclone','Primal Scream':'Primal Wrath'},
 'DRK': {'Salted Earth':'Salt and Darkness','Eventide':'Disesteem'},
 'GNB': {'Gnashing Fang':'Savage Claw|Wicked Talon','Continuation':'Hypervelocity|Fated Brand|Jugular Rip|Abdomen Tear|Eye Gouge','Relentless Rush':'Terminal Trigger'},
 'WHM': {'Cure II':'Cure III'},
 'SCH': {'Summon Seraph':'Seraphic Veil','Seraphism':'Accession'},
 'AST': {'Minor Arcana':'Lady of Crowns|Lord of Crowns','Epicycle':'Retrograde','Macrocosmos':'Microcosmos','Celestial River':'Oracle'},
 'SGE': {'Phlegma III':'Psyche','Toxikon':'Toxikon II'},
 'MNK': {'Riddle of Earth':"Earth's Reply"},
 'DRG': {'Geirskogul':'Nastrond','Elusive Jump':'Wyrmwind Thrust','Sky High':'Sky Shatter'},
 'NIN': {'Fuma Shuriken':'Hyosho Ranryu','Dokumori':'Goka Mekkyaku','Three Mudra':'Meisui','Bunshin':'Huton','Shukuchi':'Doton|Hollow Nozuchi'},
 'SAM': {'Ogi Namikiri':'Kaeshi: Namikiri','Hissatsu: Chiten':'Zanshin','Meikyo Shisui':'Tendo Setsugekka|Tendo Kaeshi Setsugekka'},
 'RPR': {'Grim Swathe':"Lemure's Slice",'Death Warrant':'Fate Sealed',"Hell's Ingress":'Regress','Tenebrae Lemurum':"Communio|Perfectio"},
 'VPR': {'Bloodcoil':'Sanguine Feast|Ouroboros',"Serpent's Tail":'Death Rattle|Twinfang Bite|Twinblood Bite|Uncoiled Twinfang|Uncoiled Twinblood|First Legacy|Second Legacy|Third Legacy|Fourth Legacy','Snake Scales':'Backlash'},
 'BRD': {'Apex Arrow':'Blast Arrow','Final Fantasia':'Encore of Light'},
 'MCH': {'Drill':'Bioblaster|Air Anchor|Chain Saw','Wildfire':'Detonator','Bishop Autoturret':'Aether Mortar'},
 'DNC': {'Honing Dance':'Honing Ovation'},
 'BLM': {'Blizzard':'Blizzard III|Blizzard IV|High Blizzard II|Freeze','Elemental Weave':'Wreath of Fire|Wreath of Ice','Soul Resonance':'Flare Star|Frost Star'},
 'SMN': {'Crimson Cyclone':'Crimson Strike','Summon Bahamut':'Deathflare|Wyrmwave|Megaflare','Summon Phoenix':'Brand of Purgatory|Scarlet Flame|Everlasting Flight'},
 'RDM': {'Enchanted Riposte':'Enchanted Zwerchhau|Enchanted Redoublement|Scorch','Embolden':'Prefulgence','Forte':'Vice of Thorns'},
 'PCT': {'Holy in White':'Comet in Black','Creature Motif':'Pom Motif|Wing Motif|Claw Motif|Maw Motif','Living Muse':'Pom Muse|Winged Muse|Clawed Muse|Fanged Muse','Mog of the Ages':'Retribution of the Madeen','Tempera Coat':'Tempera Grassa','Advent of Chocobastion':'Star Prism'},
}
# Actions eligible to move off the normal page in the specified automatic state.
# Split-required entries describe the target design, not capabilities shipped today.
DELEGATED = {
 'PLD': {'burst':'Imperator','protection':'Holy Sheltron'},
 'WAR': {'burst':'Primal Rend|Orogeny','protection':'Bloodwhetting'},
 'DRK': {'burst':'Shadowbringer','protection':'Salted Earth|Impalement'},
 'GNB': {'burst':'Gnashing Fang|Fated Circle|Blasting Zone'},
 'WHM': {'burst':'Afflatus Misery'},
 'SCH': {'burst':'Chain Stratagem'},
 'AST': {'burst':'Gravity II','mechanics':'Minor Arcana'},
 'SGE': {'burst':'Phlegma III','mechanics':'Eukrasia'},
 'MNK': {'burst':'Rising Phoenix'},
 'DRG': {'burst':'Geirskogul|High Jump','protection':'Horrid Roar'},
 'NIN': {},
 'SAM': {'burst':'Ogi Namikiri|Meikyo Shisui','protection':'Hissatsu: Chiten'},
 'RPR': {'burst':'Plentiful Harvest','protection':'Arcane Crest'},
 'VPR': {'burst':'Bloodcoil','mechanics':'Rattling Coil'},
 'BRD': {'burst':'Harmonic Arrow','mechanics':'Apex Arrow'},
 'MCH': {'burst':'Full Metal Field','mechanics':'Drill|Analysis'},
 'DNC': {'burst':'Starfall Dance','protection':'Fan Dance','support':'Curing Waltz'},
 'BLM': {},
 'SMN': {'burst':'Slipstream|Necrotize'},
 'RDM': {'burst':'Embolden|Enchanted Riposte','protection':'Forte'},
 'PCT': {'burst':'Holy in White','mechanics':'Creature Motif|Subtractive Palette'},
}
GAPS = {
 'WAR':'Separate Bloodwhetting and Primal Rend entry from their transformed continuation.',
 'GNB':'Separate Gnashing Fang entry from Savage Claw/Wicked Talon continuation.',
 'SGE':'Resolve first Kardia reminder recipient; retain direct Kardia.',
 'DRG':'Separate Geirskogul entry from Starcross continuation.',
 'NIN':'Native ninjutsu source controls remain available for deliberate alternative choice; LB repeat needs its own entry split if moved to combat selector.',
 'SAM':'Add entry-only Ogi gate; preserve Kaeshi.',
 'RPR':'Separate Plentiful and Death Warrant entry from pooling policy and Fate Sealed.',
 'DNC':'Add Starfall/Fan entry gates; retain manual Honing channel guard and resolve partner recipient.',
 'BLM':'Split offensive/defensive Burst, Elemental Weave and Xenoglossy branch ownership.',
 'RDM':'Split Embolden/Forte/Riposte entry from Prefulgence/Vice of Thorns/melee/Scorch continuation.',
 'PCT':'Separate Tempera Coat entry from Grassa expiry; manual motif entry must keep muse/portrait follow-through on combat button.',
}
INTENTS = {
 'PLD': 'Guardian=protect chosen ally|Intervene=move to target|Shield Smite=stun now|Holy Spirit=ranged damage or self healing',
 'WAR': 'Blota=pull target|Onslaught=move to target',
 'DRK': 'Plunge=move to target|The Blackest Night=shield chosen ally',
 'GNB': 'Rough Divide=move to target|Heart of Corundum=protect chosen ally|Continuation=direct ready weave fallback',
 'WHM': 'Cure II=heal chosen ally|Aquaveil=barrier and cleanse chosen ally|Miracle of Nature=interrupt/control now|Seraph Strike=move to enemy',
 'SCH': 'Adloquium=shield chosen ally|Deployment Tactics=spread chosen enemy debuff or ally shield|Expedient=party movement and protection|Summon Seraph=deploy healing summon|Biolysis=baseline damage maintenance',
 'AST': 'Aspected Benefic=heal chosen ally|Double Cast=duplicate chosen spell|Macrocosmos=group healing and timed Microcosmos release|Epicycle=move and return',
 'SGE': 'Pneuma=line damage and party barrier|Icarus=move to ally or enemy|Toxikon=ranged debuff and proc spending|Kardia=choose healing recipient',
 'MNK': "Wind's Reply=ranged damage and knockback|Riddle of Earth=timed survival and early reply|Thunderclap=move to ally or enemy|Fire's Reply=baseline follow-through",
 'DRG': 'Chaotic Spring=deliberate sustain|Elusive Jump=escape and ranged Wyrmwind access',
 'NIN': 'Fuma Shuriken=ranged and AoE combat intent|Shukuchi=move or hide|Bunshin=burst entry and deliberate Huton choice|Dokumori=burst entry and deliberate Goka choice|Three Mudra=ninjutsu entry and deliberate Meisui choice',
 'SAM': 'Hissatsu: Soten=move to target|Mineuchi=interrupt now',
 'RPR': "Death Warrant=burst entry and deliberate Fate Sealed release outside Enshroud|Harvest Moon=ranged damage|Grim Swathe=baseline resource generation|Hell's Ingress=move and return",
 'VPR': "Uncoiled Fury=ranged damage|Serpent's Tail=direct ready weave fallback|Slither=move|Snake Scales=channel protection and choose Backlash release",
 'BRD': "Silent Nocturne=interrupt now|Repelling Shot=displace and escape|The Warden's Paean=cleanse chosen ally",
 'MCH': 'Scattergun=knockback|Wildfire=burst entry or early Detonator|Bishop Autoturret=place turret and mortar',
 'DNC': 'Honing Dance=channel and choose Ovation release|En Avant=move|Closed Position=choose partner',
 'BLM': 'Blizzard=ice elemental choice|Burst=close-range damage or shield|Paradox=elemental follow-through|Xenoglossy=offensive or defensive spending|Lethargy=control target|Aetherial Manipulation=move to ally|Elemental Weave=choose elemental offensive or defensive effect',
 'SMN': 'Mountain Buster=close-range control|Crimson Cyclone=move to enemy|Radiant Aegis=shield chosen ally',
 'RDM': 'Resolution=control target|Corps-a-corps=move to enemy|Displacement=escape',
 'PCT': 'Smudge=move|Tempera Coat=shield and choose Grassa release',
}

BASELINE = {'GNB':['Continuation'], 'SCH':['Biolysis'], 'SGE':['Toxikon'], 'MNK':["Fire's Reply"], 'RPR':['Grim Swathe'], 'VPR':["Serpent's Tail"], 'BLM':['Paradox'], 'PCT':['Living Muse','Mog of the Ages']}

jobs=[]
for evidence in inventory['jobs']:
    job=evidence['job']; main=MAIN[job]
    native={child:parent for parent,children in FAMILIES[job].items() for child in children.split('|')}
    delegated={action:variant for variant,actions in DELEGATED[job].items() for action in actions.split('|')}
    intents=dict(pair.split('=',1) for pair in INTENTS[job].split('|'))
    entries=[]
    for a in evidence['actions']:
        name=a['name']; entry=dict(a)
        if a['category']=='common':
            entry.update(access='direct-common',control=name,intent='Deliberate recovery, defence, cleanse or movement; never removed by variants.')
        elif a['category']=='role':
            entry.update(access='selected-role',control='Selected PvP role action',intent='Available when this role action is selected; do not place all choices simultaneously.')
        elif not a['assignable']:
            entry.update(access='native-follow-through',control=native.get(name,main),intent='Native transformed/combo action or summon effect; preserve its source control or proven combat continuation.')
        elif a['category']=='limit_break':
            entry.update(access='direct-limit-break',control=name,intent='Deliberate limit-break initiation, with its native follow-through available.')
        elif name==main:
            entry.update(access='primary-selector',control=main,intent='Maintain the damage intent in every variant combination.')
        elif name in BASELINE.get(job,[]):
            entry.update(access='baseline-selector',control=main,intent='Ready continuation or baseline maintenance remains on the primary selector in all variant states; preserve its listed setting.')
        elif name in delegated:
            entry.update(access='variant-initiator',control=name,variant=delegated[name],automatic_route=main,manual_route=name,intent='Manual entry signals this intent; retain appropriate follow-through on the combat selector.',requires_gap_resolution=job in GAPS)
        else:
            entry.update(access='direct-distinct-intent',control=name,intent=intents.get(name,'Primary combat or native follow-through source; see job evidence.'))
        entries.append(entry)
    assert set(delegated)<=set(a['name'] for a in evidence['actions']),job
    assert set(native)<=set(a['name'] for a in evidence['actions']),job
    jobs.append(dict(job=job,primary_control=main,source=evidence['source'],source_sha256=evidence['source_sha256'],official_url=evidence['official_url'],variants=['burst','protection','support','mechanics'],settings=evidence['toggle_references'],root_presets=evidence['root_presets'],gap=GAPS.get(job),actions=entries))
output=dict(schema_version=1,status='Reviewed semantic access ledger. Executable bindings/settings are in Accord.Plugin/pvp-presets.json; source verification is pinned in pvp-implementation-review.json.',reviewed='2026-10-17',jobs=jobs)
(ROOT/'docs/job-research/pvp-action-ledger.json').write_text(json.dumps(output,indent=2)+'\n',encoding='utf-8')
print(f'Rendered {len(jobs)} jobs and {sum(len(j["actions"]) for j in jobs)} action access entries.')
