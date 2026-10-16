using Newtonsoft.Json.Linq;
using WrathFork.Contract;

internal static class SeparationTests
{
    internal static int Run()
    {
        int count = 0;
        void Check(bool value, string why) { count++; if (!value) throw new Exception(why); }
        void Reject(Action operation, string why) { try { operation(); } catch (ArgumentException) { count++; return; } catch (InvalidOperationException) { count++; return; } throw new Exception(why); }
        var current = JObject.Parse("{'EnabledActionsV6':[1], 'CustomIntValuesV6':{'threshold':55}, 'CustomBoolValuesV6':{'keep':true}, 'ForkFeatures':{}}");
        var receipts = new JObject();
        var patch = JObject.Parse("{'Settings':{'CustomIntValuesV6':{'threshold':72}},'Enable':[2],'Disable':[1]}");
        JObject Request() => new() { ["OperationId"] = Guid.NewGuid().ToString(), ["Instance"] = "session-a", ["ConfigurationId"] = "config-a", ["ExpectedRevision"] = SettingsTransaction.Hash(current), ["ExpectedSequence"] = receipts.Value<long?>("_Sequence") ?? 0, ["Patch"] = patch.DeepClone() };
        int writes = 0;
        JObject Validate(JObject p) => SettingsPatch.Apply(current, p, new HashSet<int> {1,2});
        void Save(JObject next, JObject nextReceipts) { current = (JObject)next.DeepClone(); receipts = (JObject)nextReceipts.DeepClone(); writes++; }
        var request = Request();
        var receipt = SettingsTransaction.Execute(request, "session-a", "config-a", current, receipts, Validate, Save);
        Check(writes == 1 && current["CustomIntValuesV6"]!.Value<int>("threshold") == 72, "Commit did not apply settings.");
        current["CustomIntValuesV6"]!["threshold"] = 81; // External edit after a lost response.
        var replay = SettingsTransaction.Execute(request, "session-b", "config-a", current, receipts, Validate, Save);
        Check(writes == 1 && replay.Value<bool>("Replay") && current["CustomIntValuesV6"]!.Value<int>("threshold") == 81, "Replay overwrote newer settings or repeated persistence.");
        Check(replay.Value<string>("CommittedRevision") == receipt.Value<string>("CommittedRevision"), "Replay did not identify the original committed revision.");
        var reused = (JObject)request.DeepClone(); reused["Patch"]!["Enable"] = new JArray(1);
        Reject(() => SettingsTransaction.Execute(reused, "session-b", "config-a", current, receipts, Validate, Save), "Reused operation ID accepted different content.");
        Reject(() => SettingsTransaction.Execute(request, "session-a", "other-config", current, receipts, Validate, Save), "Receipt crossed a configuration boundary.");
        var stale = Request(); stale["ExpectedRevision"] = "stale";
        Reject(() => SettingsTransaction.Execute(stale, "session-a", "config-a", current, receipts, Validate, Save), "Stale update accepted.");
        var oldSession = Request();
        Reject(() => SettingsTransaction.Execute(oldSession, "session-b", "config-a", current, receipts, Validate, Save), "Uncommitted request from old instance accepted.");
        var rejected = Request(); rejected["Patch"]!["Settings"]!["UnknownField"] = true;
        Reject(() => SettingsTransaction.Execute(rejected, "session-a", "config-a", current, receipts, Validate, Save), "Unknown field accepted.");
        Check(writes == 1 && receipts.Count == 2, "Rejected requests wrote state or receipts.");
        var failure = Request();
        try { SettingsTransaction.Execute(failure, "session-a", "config-a", current, receipts, Validate, (_,_) => throw new IOException("Disk failure")); }
        catch (IOException) { count++; }
        Check(receipts.Count == 2, "Failed persistence left a successful receipt.");
        Check(SettingsTransaction.Hash(JObject.Parse("{'b':1,'a':2}")) == SettingsTransaction.Hash(JObject.Parse("{'a':2,'b':1}")), "Property ordering changed revision.");
        var source = JObject.Parse("{'Version':6,'EnabledActionsV6':[2,999], 'CustomIntValuesV6':{'threshold':83,'retired':10}, 'CustomBoolValuesV6':{'keep':false}, 'Accord':{'LiveMode':true}, 'RotationConfig':{'Enabled':true}, 'OpCodes':{'private':1}}");
        var snapshot = new JObject { ["Settings"] = current.DeepClone(), ["KnownPresets"] = new JArray(1,2), ["PresetIds"] = new JObject { ["One"] = 1, ["Two"] = 2 }, ["Revision"] = SettingsTransaction.Hash(current) };
        var sourceBefore = source.ToString();
        var plan = SettingsMigration.Plan(source, snapshot);
        var imported = SettingsPatch.Apply(current, (JObject)plan["Patch"]!, new HashSet<int> {1,2});
        Check(imported["CustomIntValuesV6"]!.Value<int>("threshold") == 83 && !imported["CustomBoolValuesV6"]!.Value<bool>("keep"), "Migration replaced personal values with defaults.");
        Check(plan["Omitted"]!.Values<string>().Contains("CustomIntValuesV6.retired") && plan["Omitted"]!.Values<string>().Contains("EnabledActionsV6.999"), "Retired settings were silently dropped.");
        Check(plan["Patch"]!["Settings"]!["RotationConfig"] is null && plan["Patch"]!["Settings"]!["OpCodes"] is null, "Migration imported hands-free or private engine internals.");
        Check(source.ToString() == sourceBefore, "Migration modified its source.");
        source["Version"] = 999;
        Reject(() => SettingsMigration.Plan(source, snapshot), "Unknown source version accepted.");
        source["Version"] = 6; source["CustomIntValuesV6"]!["threshold"] = "bad";
        Reject(() => SettingsMigration.Plan(source, snapshot), "Malformed personal value accepted.");
        var wrongJob = (JObject)patch.DeepClone(); wrongJob["Settings"] = JObject.Parse("{'ForkFeatures':{'37':{'DncStandardInitiation':true}}}");
        Reject(() => SettingsPatch.Apply(current, wrongJob, new HashSet<int> {1,2}), "Dancer feature accepted for Gunbreaker.");
        var original = JObject.Parse("{'CustomIntValuesV6':{'threshold':81},'ForkOperationReceipts':{}}");
        foreach (var failAt in new[] { "backup", "populate", "refresh", "save" })
        {
            JObject live = (JObject)original.DeepClone();
            bool restored = false;
            int refreshes = 0, saves = 0;
            try
            {
                SettingsTransaction.PersistWithRollback(current, receipts, original,
                    value => { live = (JObject)value.DeepClone(); if (JToken.DeepEquals(value, original)) restored = true; else if (failAt == "populate") throw new IOException("Partial population failure"); },
                    () => { if (++refreshes == 1 && failAt == "refresh") throw new IOException("Cache failure"); },
                    () => { if (++saves == 1 && failAt == "save") throw new IOException("Save failure"); },
                    () => { if (failAt == "backup") throw new IOException("Backup failure"); });
                throw new Exception("Injected failure was not reached.");
            }
            catch (IOException) { Check(JToken.DeepEquals(live, original), "Failed commit did not restore configuration and receipts."); }
            Check(failAt == "backup" ? !restored && refreshes == 0 && saves == 0 : restored, "Rollback lifecycle is incorrect.");
        }
        try
        {
            SettingsTransaction.PersistWithRollback(current, receipts, original, _ => {}, () => {}, () => throw new IOException("Disk remains unavailable"), () => {});
            throw new Exception("Rollback failure was hidden.");
        }
        catch (AggregateException e) { Check(e.InnerExceptions.Count == 2, "Commit and recovery errors must both survive."); }
        var expired = Request();
        var older = (JObject)expired.DeepClone();
        receipts["_Sequence"] = 257;
        Reject(() => SettingsTransaction.Execute(older, "session-a", "config-a", current, receipts, Validate, Save), "Expired operation could replay when settings returned to the same revision.");
        var floatState = JObject.Parse("{'EnabledActionsV6':[], 'CustomFloatValuesV6':{'threshold':0.5}}");
        var floatPatch = JObject.Parse("{'Settings':{'CustomFloatValuesV6':{'threshold':1}},'Enable':[],'Disable':[]}");
        var floats = SettingsPatch.Apply(floatState, floatPatch, new HashSet<int>());
        Check(floats["CustomFloatValuesV6"]!["threshold"]!.Type == JTokenType.Float, "Float normalisation would make the committed receipt stale.");
        return count;
    }
}
