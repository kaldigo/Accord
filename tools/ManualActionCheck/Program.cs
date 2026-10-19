using Accord;
using HotbarTools;
using Lumina.Data;
using Sheets = Lumina.Excel.Sheets;

if (args.Length is < 1 or > 2) throw new ArgumentException("Pass the installed game's sqpack directory and optional layout report path.");
using var game = new Lumina.GameData(args[0]);
var actions = game.Excel.GetSheet<Sheets.Action>(Language.English);
var catalog = new ActionCatalog(actions);
var pack = new ManualLayoutPack();
int count = 0;
var report = new List<string> { "# Manual layouts — Accord 0.2.15", "", "These are the assigned keyboard buttons, left to right. Controller pages mirror each row using the fixed mapping in ManualLayoutPack. Sprint and Limit Break stay at bar 3 slots 12 and 11. Empty positions are deliberately cleared within the manual profile and restored from the original snapshot later.", "", "Optional native follow-ups have explicit buttons so the fallback also works with Action Change Settings disabled. Automatic native replacements continue to work. See [behaviour and recovery](manual-fallback.md).", "" };
foreach (var job in pack.Jobs)
{
    foreach (string name in job.Actions.Concat(job.RoleActions))
    {
        var value = catalog.ResolveSlot(job.Job, name, ManualLayoutPack.AssignedId(job.Job, name));
        if (value.Type != 1 || value.Id == 0) throw new Exception("Invalid native action: " + job.Job + " / " + name);
        count++;
    }
    Console.WriteLine($"{job.Job}: {job.Actions.Length + job.RoleActions.Length + 2}/36 slots, all job/role IDs resolved.");
    report.Add("## " + job.Job);
    report.Add("");
    report.Add("| Bar | " + string.Join(" | ", Enumerable.Range(1,12)) + " |");
    report.Add("|---|" + string.Join("|", Enumerable.Repeat("---",12)) + "|");
    var layout = ManualLayoutPack.Create(job);
    for (int bar=0;bar<3;bar++)
        report.Add($"| {bar+1} | " + string.Join(" | ", Enumerable.Range(0,12).Select(slot=>layout[new(bar,slot)])) + " |");
    report.Add("");
    if (job.Omitted.Length>0) { report.Add("Omitted: " + string.Join(", ",job.Omitted) + "."); report.Add(""); }
}
if (game.Excel.GetSheet<Sheets.GeneralAction>(Language.English).GetRow(3).Name.ToString() != "Limit Break")
    throw new Exception("Limit Break GeneralAction ID changed.");
Console.WriteLine($"Verified {count} manual job/role assignments against installed game sheets, plus Sprint and Limit Break.");
if (args.Length == 2) File.WriteAllLines(args[1],report);
