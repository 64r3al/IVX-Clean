using System.Management;

namespace IvxClean.Core.SystemOps;

/// <summary>The result of trying to create a System Restore point.</summary>
public enum RestorePointOutcome
{
    /// <summary>A restore point was created.</summary>
    Created,

    /// <summary>Windows silently skipped it due to the 24-hour frequency limit (a point already exists).</summary>
    SkippedByFrequencyLimit,

    /// <summary>System Protection is turned off for the system drive.</summary>
    ProtectionDisabled,

    /// <summary>Creation failed for another reason.</summary>
    Failed,
}

/// <summary>Creates a System Restore point before a clean (never changes protection settings).</summary>
public interface IRestorePointService
{
    /// <summary>Attempts to create a restore point with the given description.</summary>
    RestorePointOutcome CreateRestorePoint(string description);
}

/// <summary>WMI-backed restore-point service (<c>root\default:SystemRestore</c>).</summary>
public sealed class WmiRestorePointService : IRestorePointService
{
    private const uint ErrorServiceDisabled = 1058; // SystemRestore protection off
    private const int ApplicationInstall = 0;
    private const int BeginSystemChange = 100;

    /// <inheritdoc />
    public RestorePointOutcome CreateRestorePoint(string description)
    {
        try
        {
            var scope = new ManagementScope(@"\\.\root\default");
            scope.Connect();

            using var systemRestore = new ManagementClass(scope, new ManagementPath("SystemRestore"), null);
            int before = CountRestorePoints(scope);

            using ManagementBaseObject inParams = systemRestore.GetMethodParameters("CreateRestorePoint");
            inParams["Description"] = string.IsNullOrWhiteSpace(description) ? "IVX Clean" : description;
            inParams["RestorePointType"] = ApplicationInstall;
            inParams["EventType"] = BeginSystemChange;

            using ManagementBaseObject outParams = systemRestore.InvokeMethod("CreateRestorePoint", inParams, null);
            uint returnValue = Convert.ToUInt32(outParams["ReturnValue"], System.Globalization.CultureInfo.InvariantCulture);
            int after = CountRestorePoints(scope);

            return DecideOutcome(returnValue, before, after);
        }
        catch (ManagementException)
        {
            return RestorePointOutcome.Failed;
        }
        catch (UnauthorizedAccessException)
        {
            return RestorePointOutcome.Failed;
        }
    }

    /// <summary>Pure decision used by production and tested directly.</summary>
    internal static RestorePointOutcome DecideOutcome(uint returnValue, int countBefore, int countAfter) => returnValue switch
    {
        0 when countAfter > countBefore => RestorePointOutcome.Created,
        0 => RestorePointOutcome.SkippedByFrequencyLimit,
        ErrorServiceDisabled => RestorePointOutcome.ProtectionDisabled,
        _ => RestorePointOutcome.Failed,
    };

    private static int CountRestorePoints(ManagementScope scope)
    {
        try
        {
            using var searcher = new ManagementObjectSearcher(scope, new ObjectQuery("SELECT SequenceNumber FROM SystemRestore"));
            using ManagementObjectCollection results = searcher.Get();
            return results.Count;
        }
        catch (ManagementException)
        {
            return 0;
        }
    }
}
