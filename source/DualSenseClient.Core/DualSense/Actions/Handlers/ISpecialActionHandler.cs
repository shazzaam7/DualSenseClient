using DualSenseClient.Core.DualSense.Devices;
using DualSenseClient.Core.Settings.Models;

namespace DualSenseClient.Core.DualSense.Actions.Handlers;

public interface ISpecialActionHandler
{
    void Execute(DualSenseController controller, SpecialActionSettings action);
}