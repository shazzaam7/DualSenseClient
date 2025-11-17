using DualSenseClient.Core.DualSense.Actions;
using DualSenseClient.Core.Settings;

namespace DualSenseClient.Core.DualSense;

public static class DualSenseServiceLocator
{
    private static ISettingsManager? _settingsManager;
    private static DualSenseManager? _dualSenseManager;
    private static DualSenseProfileManager? _profileManager;
    private static SpecialActionService? _specialActionService;

    public static void RegisterSettingsManager(ISettingsManager settingsManager) => _settingsManager = settingsManager;
    public static void RegisterDualSenseManager(DualSenseManager dualSenseManager) => _dualSenseManager = dualSenseManager;
    public static void RegisterProfileManager(DualSenseProfileManager profileManager) => _profileManager = profileManager;
    public static void RegisterSpecialActionService(SpecialActionService specialActionService) => _specialActionService = specialActionService;

    public static ISettingsManager? GetSettingsManager() => _settingsManager;
    public static DualSenseManager? GetDualSenseManager() => _dualSenseManager;
    public static DualSenseProfileManager? GetProfileManager() => _profileManager;
    public static SpecialActionService? GetSpecialActionService() => _specialActionService;
}