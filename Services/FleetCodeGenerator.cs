using System.Text.RegularExpressions;
using SmartWasteManagement.Models;

namespace SmartWasteManagement.Services;

/// <summary>
/// Generates unique fleet identifiers: TRK-001, BIN-001, etc.
/// </summary>
public static class FleetCodeGenerator
{
    private const string TruckPrefix = "TRK-";
    private const string BinPrefix = "BIN-";

    public static string GenerateTruckNumber(IReadOnlyList<Truck> existing)
    {
        var next = NextSequence(existing.Select(t => t.TruckNumber), TruckPrefix);
        return $"{TruckPrefix}{next:D3}";
    }

    public static string GenerateBinCode(IReadOnlyList<SmartBin> existing, string? location = null)
    {
        _ = location;
        var next = NextSequence(existing.Select(b => b.BinCode), BinPrefix);
        return $"{BinPrefix}{next:D3}";
    }

    private static int NextSequence(IEnumerable<string> codes, string prefix)
    {
        var pattern = new Regex($"^{Regex.Escape(prefix)}(\\d+)$", RegexOptions.IgnoreCase);
        var max = codes
            .Select(c => pattern.Match(c.Trim()))
            .Where(m => m.Success)
            .Select(m => int.Parse(m.Groups[1].Value))
            .DefaultIfEmpty(0)
            .Max();

        return max + 1;
    }
}
