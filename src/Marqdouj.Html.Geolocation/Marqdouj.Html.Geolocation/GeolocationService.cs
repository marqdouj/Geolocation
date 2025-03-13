using Marqdouj.Html.Geolocation.Models;
using Microsoft.JSInterop;

namespace Marqdouj.Html.Geolocation
{
    /// <summary>
    /// An implementation of <see cref="IGeolocationService"/> that provides 
    /// an interop layer for the device's Geolocation API.
    /// </summary>
    public class GeolocationService(IJSRuntime jsRuntime) : IGeolocationService, IAsyncDisposable
    {
        private readonly Lazy<Task<IJSObjectReference>> moduleTask = new(() => 
            jsRuntime.InvokeAsync<IJSObjectReference>(
                "import", "./_content/Marqdouj.Html.Geolocation/js/Geolocation.js").AsTask());

        public event EventHandler<GeolocationEventArgs> WatchPositionReceived;

        /// <inheritdoc />
        public async ValueTask<GeolocationResult> GetCurrentPosition(PositionOptions options = null)
        {
            var module = await moduleTask.Value;
            return await module.InvokeAsync<GeolocationResult>("Geolocation.getCurrentPosition", options);
        }

        /// <inheritdoc />
        public async ValueTask<long?> WatchPosition(PositionOptions options = null)
        {
            var module = await moduleTask.Value;
            var callbackObj = DotNetObjectReference.Create(this);
            return await module.InvokeAsync<int>("Geolocation.watchPosition",
                callbackObj, nameof(SetWatchPosition), options);
        }

        /// <inheritdoc />
        [JSInvokable]
        public void SetWatchPosition(GeolocationResult watchResult)
        {
            WatchPositionReceived?.Invoke(this, new GeolocationEventArgs
            {
                GeolocationResult = watchResult
            });
        }

        /// <inheritdoc />
        public async ValueTask ClearWatch(long watchId)
        {
            var module = await moduleTask.Value;
            await module.InvokeVoidAsync("Geolocation.clearWatch", watchId);
        }

        public async ValueTask DisposeAsync()
        {
            if (moduleTask.IsValueCreated)
            {
                var module = await moduleTask.Value;
                await module.DisposeAsync();
            }
        }
    }
}
