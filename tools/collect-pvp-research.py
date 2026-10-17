"""Collect official PvP action names and pinned Wrath entry points; not a layout generator."""
from pathlib import Path
import concurrent.futures, hashlib, html, json, re, subprocess, urllib.request
ROOT=Path(__file__).resolve().parents[1]
JOBS=dict(zip('PLD WAR DRK GNB WHM SCH AST SGE MNK DRG NIN SAM RPR VPR BRD MCH DNC BLM SMN RDM PCT'.split(), 'paladin warrior darkknight gunbreaker whitemage scholar astrologian sage monk dragoon ninja samurai reaper viper bard machinist dancer blackmage summoner redmage pictomancer'.split()))
def text(s): return html.unescape(re.sub('<[^>]*>', ' ', s)).strip()
def collect(item):
    job,slug=item
    url=f'https://na.finalfantasyxiv.com/jobguide/{slug}/'
    raw=urllib.request.urlopen(url,timeout=40).read()
    page=raw.decode('utf-8')
    official=[]
    for anchor,body in re.findall(r'<tr id="(pvp[^"]+)">(.*?)</tr>',page,re.S):
        title=re.search(r'<strong[^>]*>(.*?)</strong>',body,re.S)
        if title:
            official.append(dict(name=text(title.group(1)),anchor=anchor,assignable='cannot be assigned to a hotbar' not in text(body),category='limit_break' if 'limitbreak' in anchor else 'common' if 'commmon' in anchor else 'role' if 'role' in anchor else 'job'))
    assert official and any(x['category']=='limit_break' for x in official),job
    source=next(p for p in (ROOT/'WrathCombo/Combos/PvP').glob('*.cs') if p.stem.upper().startswith(job+'PVP'))
    code=source.read_text(encoding='utf-8-sig')
    lines=code.splitlines()
    roots=[dict(line=i+1,preset=m.group(1)) for i,line in enumerate(lines) if (m:=re.search(r'override Preset Preset => Preset\.(\w+)',line))]
    flags=sorted(set(re.findall(r'Is(?:Not)?Enabled\(Preset\.(\w+)\)',code)))
    settings=sorted(set(re.findall(r'new\("([^"]+)"',code)))
    entries=[dict(line=i+1,code=line.strip()) for i,line in enumerate(lines) if 'OneButtonRotationChecker(' in line or 'actionID is' in line or 'actionID !=' in line]
    return dict(job=job,official_url=url+'#anchor__pvp_onlyaction',official_sha256=hashlib.sha256(raw).hexdigest(),actions=official,source=str(source.relative_to(ROOT)).replace('\\','/'),source_sha256=hashlib.sha256(source.read_bytes()).hexdigest(),root_presets=roots,toggle_references=flags,setting_keys=settings,entry_checks=entries)
if __name__=='__main__':
    with concurrent.futures.ThreadPoolExecutor(max_workers=4) as pool: jobs=list(pool.map(collect,JOBS.items()))
    data=dict(retrieved='2026-10-17',upstream_commit=subprocess.check_output(['git','rev-parse','upstream/main'],cwd=ROOT,text=True).strip(),scope='Source/action inventory. Variant ownership and complete manual continuation require a separate branch-level design pass. Names are not automation recommendations.',jobs=jobs)
    (ROOT/'docs/job-research/pvp-inventory.json').write_text(json.dumps(data,indent=2)+'\n',encoding='utf-8')
    for j in jobs: print(j['job'],len(j['actions']),'official actions;',len(j['root_presets']),'selectors;',len(j['toggle_references']),'toggles')
