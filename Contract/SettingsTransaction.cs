using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json.Linq;

namespace WrathFork.Contract;

// Shared protocol mechanics; persistence and runtime mutation are engine-owned callbacks.
public static class SettingsTransaction
{
    public static string Hash(JToken token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(Canonical(token).ToString(Newtonsoft.Json.Formatting.None))));
    private static JToken Canonical(JToken token) => token is JObject obj
        ? new JObject(obj.Properties().OrderBy(p => p.Name, StringComparer.Ordinal).Select(p => new JProperty(p.Name, Canonical(p.Value))))
        : token is JArray array ? new JArray(array.Select(Canonical)) : token.DeepClone();

    public static void PersistWithRollback(JObject updated, JObject receipts, JObject before,
        Action<JObject> populate, Action refresh, Action save, Action backup)
    {
        backup(); // A backup failure must precede any state mutation.
        try
        {
            var next = (JObject)updated.DeepClone();
            next["ForkOperationReceipts"] = receipts.DeepClone();
            populate(next); refresh(); save();
        }
        catch (Exception commitError)
        {
            try { populate(before); refresh(); save(); }
            catch (Exception rollbackError) { throw new AggregateException("Settings update failed and recovery could not be persisted. Restore the configuration backup before resuming.", commitError, rollbackError); }
            throw;
        }
    }

    public static JObject Execute(JObject request, string instance, string configurationId, JObject current,
        JObject receipts, Func<JObject, JObject> validate, Action<JObject, JObject> persist)
    {
        if (request.Properties().Any(p => p.Name is not ("OperationId" or "Instance" or "ConfigurationId" or "ExpectedRevision" or "ExpectedSequence" or "Patch"))) throw new ArgumentException("Unknown request field.");
        if (!Guid.TryParse(request.Value<string>("OperationId"), out var id)) throw new ArgumentException("A UUID OperationId is required.");
        if (request.Value<string>("ConfigurationId") != configurationId) throw new InvalidOperationException("Configuration changed; review before retrying.");
        string key = id.ToString("N");
        string fingerprint = Hash(request);
        if (receipts[key] is JObject previous)
        {
            if (previous.Value<string>("RequestHash") != fingerprint) throw new InvalidOperationException("Operation ID was reused with a different request.");
            var replay = (JObject)previous.DeepClone(); replay["Replay"] = true; return replay;
        }
        long sequence = receipts.Value<long?>("_Sequence") ?? 0;
        if (request.Value<long?>("ExpectedSequence") != sequence) throw new InvalidOperationException("Operation history changed; review before retrying.");
        if (request.Value<string>("Instance") != instance || request.Value<string>("ExpectedRevision") != Hash(current))
            throw new InvalidOperationException("Engine state changed. Read and review it before applying.");
        var updated = validate(request["Patch"] as JObject ?? throw new ArgumentException("Missing patch."));
        var receipt = new JObject { ["OperationId"] = id.ToString(), ["RequestHash"] = fingerprint,
            ["CommittedRevision"] = Hash(updated), ["ConfigurationId"] = configurationId, ["Replay"] = false };
        var next = (JObject)receipts.DeepClone();
        next[key] = receipt.DeepClone();
        next["_Sequence"] = checked(sequence + 1);
        while (next.Count > 257) next.Properties().First(p => p.Name != "_Sequence").Remove();
        // The receipt is saved in the same configuration write as the settings.
        persist(updated, next);
        return receipt;
    }
}
