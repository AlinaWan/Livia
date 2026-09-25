using Windows.Devices.Radios;

namespace Livia.Utils;

public static class NetworkUtils
{
    /// <summary>
    /// Requests a change to the operational state of the specified radio.
    /// </summary>
    /// <param name="kind">
    /// The type of radio whose state should be changed.
    /// </param>
    /// <param name="enable">
    /// <see langword="true"/> to request that the radio be enabled;
    /// <see langword="false"/> to request that the radio be disabled.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if Windows accepted the state change request;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    /// <remarks>
    /// The returned value indicates that Windows accepted the request, not that
    /// the radio has already reached the requested state. The radio transitions
    /// asynchronously after the request is accepted.
    /// </remarks>
    public static async Task<bool> SetRadioStateAsync(
        RadioKind kind,
        bool enable)
    {
        RadioAccessStatus accessStatus = await Radio.RequestAccessAsync();

        if (accessStatus != RadioAccessStatus.Allowed)
        {
            return false;
        }

        IReadOnlyList<Radio> radios = await Radio.GetRadiosAsync();
        RadioState targetState = enable
            ? RadioState.On
            : RadioState.Off;

        foreach (Radio radio in radios)
        {
            if (radio.Kind != kind)
            {
                continue;
            }

            return await radio.SetStateAsync(targetState) == RadioAccessStatus.Allowed;
        }

        return false;
    }

    /// <summary>
    /// Gets the current operational state of the specified radio.
    /// </summary>
    /// <param name="kind">
    /// The type of radio whose state should be retrieved.
    /// </param>
    /// <returns>
    /// The current state of the radio, or <see langword="null"/> if no radio
    /// of the specified type is available.
    /// </returns>
    /// <remarks>
    /// Reading radio state does not require radio-control permission.
    /// </remarks>
    public static async Task<RadioState?> GetRadioStateAsync(
        RadioKind kind)
    {
        IReadOnlyList<Radio> radios = await Radio.GetRadiosAsync();

        foreach (Radio radio in radios)
        {
            if (radio.Kind == kind)
            {
                return radio.State;
            }
        }

        return null;
    }
}