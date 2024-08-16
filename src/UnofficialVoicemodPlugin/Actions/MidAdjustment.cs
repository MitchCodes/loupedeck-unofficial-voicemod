namespace Loupedeck.UnofficialVoicemodPlugin
{
    using System;

    using Loupedeck.UnofficialVoicemodPlugin;

    // seems to be currently broken
    /*
    public class MidAdjustment : PluginDynamicAdjustment
    {
        private VoicemodApiWrapper _apiWrapper;
        private Double _mid = 50.0; // Default noise reduction value

        public MidAdjustment()
            : base(displayName: "Mid", description: "Adjust mid level", groupName: "Voicemod Adjustments", hasReset: true)
        {
        }

        protected override async void ApplyAdjustment(String actionParameter, Int32 diff)
        {
            try
            {
                _mid = Math.Max(0, Math.Min(100, _mid + diff * 1)); // Adjust noise reduction within the range 0 to 1

                if (_apiWrapper == null)
                {
                    _apiWrapper = new VoicemodApiWrapper();
                }

                var response = await _apiWrapper.SetCurrentVoiceParameterAsync("Mid", _mid);

                if (response != null && response.ActionObject != null && response.ActionObject.Parameters != null)
                {
                    foreach (var parameter in response.ActionObject.Parameters)
                    {
                        if (parameter.Name == "Mid")
                        {
                            //_mid = parameter.Value;
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
            _mid = 50.0; // Reset to default noise reduction level
            this.AdjustmentValueChanged();
        }

        protected override String GetAdjustmentValue(String actionParameter) => _mid.ToString("0.0");
    }
    */
}
