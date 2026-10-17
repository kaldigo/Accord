"""Build executable profiles from reviewed intent ownership and installed game sheets."""
import json
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
ledger=json.loads((ROOT/'docs/job-research/pvp-action-ledger.json').read_text())
sheets=json.loads((ROOT/'docs/job-research/pvp-native-bindings.json').read_text())
ids=dict(zip('PLD WAR DRK GNB WHM SCH AST SGE MNK DRG NIN SAM RPR VPR BRD MCH DNC BLM SMN RDM PCT'.split(),[19,21,32,37,24,28,33,40,20,22,30,34,39,41,23,31,38,25,27,35,42]))
combos=dict(zip('PLD WAR DRK GNB DNC MNK DRG NIN SAM RPR VPR'.split(),[50,51,52,53,54,55,56,57,58,59,65]))
# Exact source identifiers: baseline, burst, protection, support, mechanics, explicitly manual options.
rules={
'PLD':['PLDPvP_Burst PLDPvP_Confiteor PLDPvP_PhalanxCombo','PLDPvP_Imperator','PLDPvP_Sheltron PLDPvP_HolySpirit PLDPvP_Rampart','','','PLDPvP_Intervene PLDPvP_Intervene_Melee PLDPvP_ShieldSmite'],
'WAR':['WARPvP_BurstMode WARPvP_BurstMode_Bloodwhetting WARPvP_BurstMode_PrimalRend WARPvP_BurstMode_InnerChaos WARPvP_BurstMode_PrimalScream','WARPvP_BurstMode_Onslaught WARPvP_BurstMode_Orogeny','WARPvP_Rampart','','','WARPvP_BurstMode_Blota'],
'DRK':['DRKPvP_Burst DRKPvP_SaltAndDarkness DRKPvP_Scorn','DRKPvP_Shadowbringer','DRKPvP_BlackestNight DRKPvP_SaltedEarth DRKPvP_Impalement DRKPvP_Rampart','','','DRKPvP_Plunge DRKPvP_PlungeMelee'],
'GNB':['GNBPvP_Burst GNBPvP_ST_GnashingFang GNBPvP_ST_Continuation','GNBPvP_RoughDivide GNBPvP_FatedCircle GNBPvP_BlastingZone','GNBPvP_Corundum GNBPvP_Rampart','','',''],
'WHM':['WHMPvP_Burst WHMPvP_Heals WHMPvP_Cure3 WHMPvP_Aquaveil','WHMPvP_Seraph_Strike WHMPvP_Afflatus_Misery WHMPvP_Diabrosis','','WHMPvP_Burst_Heals','','WHMPvP_AfflatusPurgation WHMPvP_Mirace_of_Nature WHMPvP_Seraphstrike'],
'SCH':['SCHPvP_Burst SCHPvP_Biolysis SCHPvP_RetargetAdlo','SCHPvP_ChainStratagem SCHPvP_Diabrosis','','SCHPvP_Adlo SCHPvP_Expedient','','SCHPvP_DeploymentTactics'],
'AST':['ASTPvP_Burst ASTPvP_Heal ASTPvP_Burst_PlayCard ASTPvP_Burst_DoubleGravity ASTPvP_Burst_DoubleMalefic ASTPvP_Burst_Oracle ASTPvP_Burst_Gravity','ASTPvP_Burst_Macrocosmos ASTPvP_Diabrosis','','ASTPvP_Burst_Heal','ASTPvP_Burst_DrawCard','ASTPvP_Epicycle'],
'SGE':['SGEPvP_BurstMode SGEPvP_BurstMode_Psyche SGEPvP_BurstMode_Toxikon SGEPvP_BurstMode_Toxikon2','SGEPvP_BurstMode_Phlegma SGEPvP_Diabrosis','','SGEPvP_BurstMode_Pneuma SGEPvP_BurstMode_KardiaReminder','SGEPvP_BurstMode_Eukrasia','SGEPvP_RetargetKardia'],
'MNK':['MNKPvP_Burst MNKPvP_Burst_FiresReply MNKPvP_Burst_WindsReply','MNKPvP_Burst_RisingPhoenix MNKPvP_Smite','MNKPvP_Burst_RiddleOfEarth','','','MNKPvP_Burst_Meteodrive MNKPvP_Burst_Thunderclap'],
'DRG':['DRGPvP_Burst DRGPvP_Geirskogul DRGPvP_Nastrond DRGPvP_WyrmwindThrust','DRGPvP_HighJump DRGPvP_Smite','DRGPvP_HorridRoar DRGPvP_ChaoticSpringSustain','','',''],
'NIN':['NINPvP_ST_BurstMode NINPvP_AoE_BurstMode NINPvP_ST_MudraMode NINPvP_AoE_MudraMode NINPvP_ST_FumaShuriken NINPvP_AoE_FumaShuriken','NINPvP_ST_Bunshin NINPvP_AoE_Bunshin NINPvP_ST_Dokumori NINPvP_AoE_Dokumori NINPvP_Smite','NINPvP_ST_Meisui NINPvP_AoE_Meisui','','NINPvP_ST_ThreeMudra NINPvP_AoE_ThreeMudra','NINPvP_ST_SeitonTenchu NINPvP_AoE_SeitonTenchu'],
'SAM':['SAMPvP_Burst','SAMPvP_Meikyo SAMPvP_Smite','SAMPvP_Chiten','','','SAMPvP_Zantetsuken SAMPvP_Mineuchi SAMPvP_Soten'],
'RPR':['RPRPvP_Burst RPRPvP_Burst_DeathWarrant RPRPvP_Burst_Enshrouded RPRPvP_Burst_Enshrouded_Communio RPRPvP_Burst_Enshrouded_DeathWarrant RPRPvP_Burst_GrimSwathe RPRPvP_Burst_RangedHarvest','RPRPvP_Smite','RPRPvP_Burst_ArcaneCircle','','',''],
'VPR':['VPRPvP_Burst','VPRPvP_Bloodcoil VPRPvP_UncoiledFury VPRPvP_Smite','','','VPRPvP_RattlingCoil','VPRPvP_Slither'],
'BRD':['BRDPvP_BurstMode BRDPvP_BlastArrow BRDPvP_EncoreOfLight','BRDPvP_HarmonicArrow BRDPvP_Eagle','BRDPvP_Wardens','','BRDPvP_ApexArrow','BRDPvP_SilentNocturne'],
'MCH':['MCHPvP_BurstMode','MCHPvP_BurstMode_FullMetalField MCHPvP_BurstMode_Wildfire MCHPvP_Eagle','','','MCHPvP_BurstMode_Analysis MCHPvP_BurstMode_Drill MCHPvP_BurstMode_BioBlaster MCHPvP_BurstMode_AirAnchor MCHPvP_BurstMode_ChainSaw','MCHPvP_BurstMode_MarksmanSpite'],
'DNC':['DNCPvP_BurstMode DNCPvP_BurstMode_HoningDance','DNCPvP_Eagle','','DNCPvP_BurstMode_CuringWaltz','DNCPvP_BurstMode_Partner','DNCPvP_BurstMode_Dash'],
'BLM':['BLMPvP_BurstMode BLMPvP_Burst BLMPvP_ElementalWeave BLMPvP_Xenoglossy BLMPvP_ElementalStar','BLMPvP_PhantomDart','','','','BLMPvP_Lethargy'],
'SMN':['SMNPvP_BurstMode SMNPvP_BurstMode_CrimsonStrike SMNPvP_BurstMode_DeathFlare SMNPvP_BurstMode_BrandofPurgatory','SMNPvP_BurstMode_CrimsonCyclone SMNPvP_BurstMode_MountainBuster SMNPvP_BurstMode_Slipstream SMNPvP_BurstMode_Necrotize SMNPvP_PhantomDart','SMNPvP_BurstMode_RadiantAegis','','',''],
'RDM':['RDMPvP_BurstMode RDMPvP_Embolden RDMPvP_Forte RDMPvP_Riposte','RDMPvP_PhantomDart','','','','RDMPvP_Corps RDMPvP_Displacement RDMPvP_Resolution'],
'PCT':['PCTPvP_Burst PCTPvP_LivingMuse PCTPvP_MogOfTheAges PCTPvP_TemperaCoat PCTPvP_StarPrism','PCTPvP_HolyInWhite PCTPvP_PhantomDart','','','PCTPvP_CreatureMotif PCTPvP_SubtractivePalette',''],
}
features={
'AST':{'PvPAstGravityInitiation':'Burst'},
'WAR':{'PvPWarBloodwhettingInitiation':'Protection','PvPWarRendInitiation':'Burst'},
'GNB':{'PvPGnbGnashingInitiation':'Burst'},'DRG':{'PvPDrgGeirskogulInitiation':'Burst'},
'SAM':{'PvPSamOgiInitiation':'Burst'},'RPR':{'PvPRprPlentifulInitiation':'Burst','PvPRprWarrantInitiation':'Burst'},
'DNC':{'PvPDncStarfallInitiation':'Burst','PvPDncFanInitiation':'Protection','PvPDncHoningInitiation':'ClassMechanics'},
'BLM':{f'PvPBlm{branch}{action}Initiation':owner for branch,owner in [('Offensive','Burst'),('Defensive','Protection')] for action in ['Burst','Weave','Xeno']},
'RDM':{'PvPRdmEmboldenInitiation':'Burst','PvPRdmRiposteInitiation':'Burst','PvPRdmForteInitiation':'Protection'},
'PCT':{'PvPPctCoatInitiation':'Protection'}}
variant={'burst':'Burst','protection':'Protection','support':'Support','mechanics':'ClassMechanics'}
def binding(job,name):
    found=[a for a in sheets['Actions'] if a['Name'].casefold()==name.casefold() and (job in a['Jobs'] or name in ['Standard-issue Elixir','Recuperate','Purify','Guard','Sprint'])]
    found=[a for a in found if a['IsPlayerAction']]
    assert len(found)==1,(job,name,found)
    return {'Type':1,'Id':found[0]['RowId']}
descriptions={}
for line in (ROOT/'docs/job-research/pvp-variant-mappings.md').read_text(encoding='utf-8').splitlines():
    parts=[p.strip() for p in line.split('|')[1:-1]]
    if len(parts)==5 and parts[0] in ids:descriptions[parts[0]]=dict(zip(variant.values(),parts[1:]))
profiles=[]
for j in ledger['jobs']:
    job=j['job']; primary=j['primary_control']
    b={'Type':26,'Id':combos[job]} if job in combos else binding(job,primary)
    controls=[{'Name':primary,'Binding':b,'Intent':'Maintain damage','Custom':'SingleTargetDPS'}]
    for a in j['actions']:
        if not a['assignable'] or a['access'] in ['primary-selector','baseline-selector','selected-role']:continue
        c={'Name':a['name'],'Binding':binding(job,a['name']),'Intent':a['intent']}
        if a.get('variant'):c['Variant']=variant[a['variant']]
        if job=='RPR' and a['name']=='Death Warrant':
            c.pop('Variant',None);c['Intent']='Burst entry and deliberate Fate Sealed release outside Enshroud'
        # Pooling is personal policy; retain explicit harvest if pooling is disabled.
        if job=='RPR' and a['name']=='Plentiful Harvest':c['RequiredPreset']='RPRPvP_Burst_ImmortalPooling'
        positive={('DNC','Curing Waltz'):['DNCWaltzThreshold'],('RPR','Arcane Crest'):['RPRPvPArcaneCircleOption'],('RDM','Forte'):['RDMPvP_Forte_PlayerHP'],('SAM','Hissatsu: Chiten'):['SAMPvP_Chiten_PlayerHP'],('VPR','Bloodcoil'):['VPRPvP_Bloodcoil_TargetHP','VPRPvP_Bloodcoil_PlayerHP']}
        if (job,a['name']) in positive:c['AnyPositive']=positive[(job,a['name'])]
        if job=='VPR' and a['name']=='Rattling Coil':c['AnyOption']='VPRPvP_RattlingCoil_SubOptions'
        if job=='MCH' and a['name']=='Full Metal Field':c['TimingOption']='MCHPvP_FMFOption'
        controls.append(c)
    controls.append({'Name':'Selected PvP role action','Binding':{'Type':1,'Id':43259},'Intent':'Use selected role action'})
    assert len(controls)<=20,(job,len(controls))
    r=rules[job]
    profiles.append({'JobId':ids[job],'Job':job,'Descriptions':descriptions[job],'Controls':controls,'Always':r[0].split(),'Variants':{v:r[i+1].split() for i,v in enumerate(variant.values())},'Manual':r[5].split(),'Features':features.get(job,{}),
      'RequiredBools':(['RDMPvP_Embolden_SubOption','RDMPvP_Forte_SubOption'] if job=='RDM' else ['BLMPvP_Burst_SubOption','BLMPvP_ElementalWeave_SubOption','BLMPvP_Xenoglossy_SubOption'] if job=='BLM' else []),
      'RequiredInts':{'ASTPvP_Burst_PlayCardOption':1} if job=='AST' else {},
      'RecipientFeatures':['PvPDncPartnerRecipient'] if job=='DNC' else ['PvPSgeKardiaRecipient'] if job=='SGE' else []})
(ROOT/'Accord.Plugin/pvp-presets.json').write_text(json.dumps(profiles,indent=2)+'\n',encoding='utf-8')
print('Generated',len(profiles),'profiles:',[(p['Job'],len(p['Controls'])) for p in profiles])
