namespace Loupedeck.UnofficialVoicemodPlugin
{
    using System;
    using Loupedeck.UnofficialVoicemodPlugin;
    // seems to be currently broken
    /*
    public class BassAdjustment : PluginDynamicAdjustment
    {
        private VoicemodApiWrapper _apiWrapper;
        private Double _bass = 0.0; // Default noise reduction value

        public BassAdjustment()
            : base(displayName: "Bass", description: "Adjust bass level", groupName: "Voicemod Adjustments", hasReset: true)
        {
        }

        protected override async void ApplyAdjustment(String actionParameter, Int32 diff)
        {
            try
            {
                _bass = Math.Max(-5, Math.Min(5, _bass + diff * 0.2)); // Adjust noise reduction within the range 0 to 1

                if (_apiWrapper == null)
                {
                    _apiWrapper = new VoicemodApiWrapper();
                }

                var response = await _apiWrapper.SetCurrentVoiceParameterAsync("Bass", _bass);

                if (response != null && response.ActionObject != null && response.ActionObject.Parameters != null)
                {
                    foreach (var parameter in response.ActionObject.Parameters)
                    {
                        if (parameter.Name == "Bass")
                        {
                            //_bass = parameter.Value;
                            break;
                        }
                    }
                }

                this.AdjustmentValueChanged();
            } catch (Exception ex)
            {
                PluginLog.Error(ex, ex.Message);
            }
        }

        protected override void RunCommand(String actionParameter)
        {
            _bass = 0.0; // Reset to default noise reduction level
            this.AdjustmentValueChanged();
        }

        protected override String GetAdjustmentValue(String actionParameter) => _bass.ToString("0.0");
    }
    */
}
