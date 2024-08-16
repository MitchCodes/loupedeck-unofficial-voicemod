namespace Loupedeck.UnofficialVoicemodPlugin
{
    using System;
    using Loupedeck.UnofficialVoicemodPlugin;
    // seems to be currently broken
    /*
    public class VolumeAdjustment : PluginDynamicAdjustment
    {
        private VoicemodApiWrapper _apiWrapper;
        private Double _volume = 1.0; // Default volume value

        public VolumeAdjustment()
            : base(displayName: "Volume", description: "Adjust the volume", groupName: "Voicemod Adjustments", hasReset: true)
        {
        }

        protected override async void ApplyAdjustment(String actionParameter, Int32 diff)
        {
            try
            {
                if (_apiWrapper == null)
                {
                    _apiWrapper = new VoicemodApiWrapper();
                }

                _volume = Math.Max(0, Math.Min(1, _volume + diff * 0.1)); // Adjust volume within the range 0 to 1
                var response = await _apiWrapper.SetCurrentVoiceParameterAsync("volume", _volume);

                if (response != null && response.ActionObject != null && response.ActionObject.Parameters != null)
                {
                    foreach (var parameter in response.ActionObject.Parameters)
                    {
                        if (parameter.Name == "Voice Volume")
                        {
                            //_volume = parameter.Value;
                            break;
                        }
                    }
                }

                this.AdjustmentValueChanged();
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, ex.Message);
            }
        }

        protected override void RunCommand(String actionParameter)
        {
            _volume = 1.0; // Reset to default volume
            this.AdjustmentValueChanged();
        }

        protected override String GetAdjustmentValue(String actionParameter) => _volume.ToString("0.0");
    }
    */
}
