using Lumina.Excel.Sheets;
using System.Text.Json;
if(args.Length!=2)throw new ArgumentException("Supply the game sqpack directory and an output JSON path.");
using var data=new Lumina.GameData(args[0],new Lumina.LuminaOptions{DefaultExcelLanguage=Lumina.Data.Language.English});
var rows=data.GetExcelSheet<Lumina.Excel.Sheets.Action>().Where(a=>a.IsPvP).Select(a=>new {a.RowId,Name=a.Name.ToString(),a.IsPlayerAction,Jobs=typeof(ClassJobCategory).GetProperties().Where(p=>p.PropertyType==typeof(bool) && (bool)p.GetValue(a.ClassJobCategory.Value)!).Select(p=>p.Name).ToArray()});
var combos=data.GetExcelSheet<ActionComboRoute>().Where(a=>!a.Name.IsEmpty && a.Action.Any(x=>x.RowId>0 && x.Value.IsPvP)).Select(a=>new {a.RowId,Name=a.Name.ToString(),Actions=a.Action.Select(x=>x.RowId).Where(x=>x>0).ToArray()});
File.WriteAllText(args[1],JsonSerializer.Serialize(new {Actions=rows.ToArray(),Combos=combos.ToArray()},new JsonSerializerOptions{WriteIndented=true}));
