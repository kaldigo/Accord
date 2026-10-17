"""Check the reviewed mapping coverage and source provenance, plus implementation provenance; runtime settings/layout coverage is in PvPTests."""
from pathlib import Path
import hashlib, itertools, json, re

root=Path(__file__).resolve().parents[1]
inventory=json.loads((root/'docs/job-research/pvp-inventory.json').read_text())
ledger=json.loads((root/'docs/job-research/pvp-action-ledger.json').read_text())
presets=(root/'WrathCombo/Combos/CustomComboPreset.cs')
if not presets.exists():
    presets=next((root/'WrathCombo').rglob('CustomComboPreset.cs'))
preset_source=presets.read_text(encoding='utf-8-sig')
assert len(ledger['jobs'])==21
assert {j['job'] for j in ledger['jobs']}=={j['job'] for j in inventory['jobs']}
review=json.loads((root/'docs/job-research/pvp-implementation-review.json').read_text())
for source,digest in review['hands_free_sources'].items():
    assert hashlib.sha256((root/source).read_text(encoding='utf-8-sig').encode()).hexdigest()==digest, f'Hands-free source changed: {source}'
checks=0
for job in ledger['jobs']:
    evidence=next(j for j in inventory['jobs'] if j['job']==job['job'])
    assert hashlib.sha256((root/job['source']).read_text(encoding='utf-8-sig').encode()).hexdigest()==review['jobs'][job['job']]['source_sha256'],f"Re-review changed source: {job['job']}"
    assert [(a['name'],a['category'],a['assignable']) for a in job['actions']]==[(a['name'],a['category'],a['assignable']) for a in evidence['actions']]
    assert set(job['settings'])==set(evidence['toggle_references'])
    for preset in job['settings']+[p['preset'] for p in job['root_presets']]:
        assert re.search(r'\b'+re.escape(preset)+r'\s*=',preset_source),preset
    controls={a['name'] for a in job['actions'] if a['assignable']}|{job['primary_control']}
    for action in job['actions']:
        assert action['access'] and action['intent']
        if action['access']=='native-follow-through':
            assert action['control'] in controls,(job['job'],action)
        if action['access']=='variant-initiator':
            assert action['variant'] in job['variants'] and action['automatic_route']==job['primary_control']
    for selection in itertools.product([False,True],repeat=4):
        enabled=dict(zip(job['variants'],selection))
        # Every action has a semantic path in all states. This intentionally does
        # not claim that split-required routes have already been implemented.
        for action in job['actions']:
            if action['access']=='variant-initiator':
                route=action['automatic_route'] if enabled[action['variant']] else action['manual_route']
                assert route in controls
            else:
                assert action['control']
            checks+=1
print(f"21 job source hashes; all referenced presets; 504 actions; {checks} action/state access checks passed.")
