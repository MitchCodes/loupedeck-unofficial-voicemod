namespace Loupedeck.UnofficialVoicemodPlugin
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Net.WebSockets;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    public class VoicemodApiWrapper
    {
        private const String ServerUrl = "ws://localhost:{0}/v1/";
        private static HashSet<int> Ports = new HashSet<int>() { 59129, 20000, 39273, 42152, 43782, 46667, 35679, 37170, 38501, 33952, 30546 };
        private static int LastValidPort = 59129;
        private const String ClientKey = "";
        private static ClientWebSocket _webSocket;
        private static readonly SemaphoreSlim WebSocketLock = new SemaphoreSlim(1, 1);
        // Dictionary to store messages with their received timestamp
        private static readonly ConcurrentDictionary<String, (String Message, DateTime Timestamp)> _responseDictionary
            = new ConcurrentDictionary<String, (String Message, DateTime Timestamp)>();
        private static readonly ConcurrentDictionary<String, (String Message, DateTime Timestamp)> _responseDictionaryByType
            = new ConcurrentDictionary<String, (String Message, DateTime Timestamp)>();
        private static readonly TimeSpan ResponseTimeout = TimeSpan.FromSeconds(5);
        private static readonly TimeSpan ResponseCleanupInterval = TimeSpan.FromMinutes(5);
        private static Task ReadTask;
        private static Task CleanupTask;
        private static Boolean _isConnected = false;

        public VoicemodApiWrapper()
        {
            StartMessageProcessing();

            InitializeWebSocket();
        }

        private static async Task InitializeWebSocket()
        {
            if (_webSocket == null)
            {
                _webSocket = new ClientWebSocket();
                await ConnectWebSocketAsync();
            }
        }

        private static async Task ConnectWebSocketAsync()
        {
            await WebSocketLock.WaitAsync();
            try
            {
                if (_webSocket.State == WebSocketState.Open)
                {
                    return;
                }

                await ConnectCyclePortsAsync();

                _isConnected = true;

                // Register the client
                await RegisterClientAsync();
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, "Failed to connect to the Voicemod WebSocket server.");

                throw;
            }
            finally
            {
                WebSocketLock.Release();
            }
        }

        private static async Task ConnectCyclePortsAsync()
        {
            int firstPortToTry = LastValidPort;

            bool connected = await ConnectCyclePortAsync(firstPortToTry);

            if (connected)
            {
                return;
            }

            foreach (int port in Ports)
            {
                if (port == firstPortToTry)
                {
                    continue;
                }

                connected = await ConnectCyclePortAsync(port);

                if (connected)
                {
                    LastValidPort = port;
                    return;
                }
            }

            throw new Exception("Unable to connect via any port");
        }

        private static async Task<bool> ConnectCyclePortAsync(int port)
        {
            try
            {
                _webSocket.Dispose();
                _webSocket = new ClientWebSocket();
                await _webSocket.ConnectAsync(new Uri(string.Format(ServerUrl, port.ToString())), CancellationToken.None);

                return true;
            } catch (Exception)
            {
                return false;
            }
        }

        private static async Task RegisterClientAsync()
        {
            var registerMessage = $"{{\"action\":\"registerClient\",\"id\":\"{Guid.NewGuid()}\",\"payload\":{{\"clientKey\":\"{ClientKey}\"}}}}";
            await SendMessageAsync(registerMessage);
            var registerResponse = await ReceiveAndProcessResponse<RegisterClientResponse>(registerMessage);
            if (registerResponse.Payload.Status.Code != 200)
            {
                throw new InvalidOperationException("Failed to register client.");
            }
        }

        private static async Task SendMessageAsync(String message)
        {
            if (_webSocket.State != WebSocketState.Open)
            {
                await ConnectWebSocketAsync();
            }
            var messageBytes = Encoding.UTF8.GetBytes(message);
            await _webSocket.SendAsync(new ArraySegment<Byte>(messageBytes), WebSocketMessageType.Text, true, CancellationToken.None);
        }

        private static async Task<T> ReceiveAndProcessResponse<T>(String sentMessage, TimeSpan timeout = default)
        {
            if (timeout == default)
            {
                timeout = ResponseTimeout;
            }

            var id = ExtractIdFromMessage(sentMessage);
            if (id == null)
            {
                return default;
            }

            var giveUpTime = DateTime.Now.Add(timeout);

            //https://control-api.voicemod.net/api-reference/#pub-getbackgroundeffectstatus-operation
            //https://loupedeck.github.io/Marketplace-Approval-Guidelines/
            //chatgpt
            //look for 'voiceChangedEvent' in the response and remove it from dictionary if it exists
            // or just dont even confirm it.. just let it go
            // also, make sure TImeoutexception doesn't bubble up to the actual command/adjustment, causes whole plugin to crasha nd be disabled
            // if plugin crashes like that, needs to be reenabled in settings for debugging and it to work

            while (DateTime.Now < giveUpTime)
            {
                if (_responseDictionary.TryRemove(id, out var response))
                {
                    return JsonConvert.DeserializeObject<T>(response.Message);
                }

                await Task.Delay(100); // Delay to prevent tight looping
            }

            throw new TimeoutException("Response timed out.");
        }

        /// <summary>
        /// This function assumes the action type has been cleaned from the dictionary and we are looking for the first one.
        /// </summary>
        private static async Task<T> ReceiveAndProcessResponseByActionType<T>(String sentMessage, String actionType, TimeSpan timeout = default)
        {
            if (timeout == default)
            {
                timeout = ResponseTimeout;
            }

            var giveUpTime = DateTime.Now.Add(timeout);

            while (DateTime.Now < giveUpTime)
            {
                if (_responseDictionaryByType.TryRemove(actionType, out var response))
                {
                    return JsonConvert.DeserializeObject<T>(response.Message);
                }

                await Task.Delay(100); // Delay to prevent tight looping
            }

            throw new TimeoutException("Response timed out.");
        }

        private static String ExtractIdFromMessage(String message)
        {
            JObject messageObj = null;
            try
            {
                messageObj = JObject.Parse(message);
            }
            catch (Exception)
            {
                return null;
            }

            // Try to get the 'id' property first, if not found, then get the 'actionId' property
            var id = messageObj["id"]?.ToString() ?? messageObj["actionId"]?.ToString() ?? messageObj["actionID"]?.ToString();

            if (id == null)
            {
                return null;
                //throw new InvalidOperationException("No valid 'id' or 'actionId' found in the message.");
            }

            return id;
        }

        private static String ExtractActionTypeFromMessage(String message)
        {
            JObject messageObj = null;
            try
            {
                messageObj = JObject.Parse(message);
            }
            catch (Exception)
            {
                return null;
            }

            // Try to get the 'id' property first, if not found, then get the 'actionId' property
            //var actionType = messageObj["actionType"]?.ToString() ?? messageObj["actionId"]?.ToString();
            var actionType = messageObj["actionType"]?.ToString();

            if (actionType == null)
            {
                return null;
                //throw new InvalidOperationException("No valid 'id' or 'actionId' found in the message.");
            }

            return actionType;
        }

        private static void StartMessageProcessing()
        {
            if (ReadTask == null)
            {
                // Start the message processing task
                ReadTask = Task.Run(async () =>
                {
                    while (true)
                    {
                        while (_webSocket != null && _isConnected)
                        {
                            try
                            {
                                var message = await ReceiveMessageAsync();

                                try
                                {
                                    var id = ExtractIdFromMessage(message);
                                    if (id != null)
                                    {
                                        _responseDictionary[id] = (message, DateTime.UtcNow);
                                    }

                                    var actionType = ExtractActionTypeFromMessage(message);
                                    if (actionType != null)
                                    {
                                        _responseDictionaryByType[actionType] = (message, DateTime.UtcNow);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    PluginLog.Error(ex, ex.Message);
                                }
                            }
                            catch (WebSocketException)
                            {
                                _isConnected = false;
                                await ConnectWebSocketAsync(); // Reconnect if the connection drops
                            }
                        }

                        await Task.Delay(100); // Delay to prevent tight looping
                    }
                });
            }

            if (CleanupTask == null)
            {
                // Start the cleanup task
                CleanupTask = Task.Run(async () =>
                {
                    while (true)
                    {
                        try
                        {
                            CleanupOldResponses();
                        }
                        catch (Exception ex)
                        {
                            // Log or handle cleanup exceptions
                        }

                        await Task.Delay(ResponseCleanupInterval);
                    }
                });
            }
        }

        private static void CleanActionType(String actionType)
        {
            if (_responseDictionaryByType != null)
            {
                _responseDictionaryByType.TryRemove(actionType, out _);
            }
        }

        private static void CleanupOldResponses()
        {
            var expirationTime = DateTime.UtcNow - ResponseCleanupInterval;

            if (_responseDictionary != null)
            {
                foreach (var key in _responseDictionary.Keys)
                {
                    if (_responseDictionary.TryGetValue(key, out var entry) && entry.Timestamp < expirationTime)
                    {
                        _responseDictionary.TryRemove(key, out _);
                    }
                }
            }

            if (_responseDictionaryByType != null)
            {
                foreach (var key in _responseDictionaryByType.Keys)
                {
                    if (_responseDictionaryByType.TryGetValue(key, out var entry) && entry.Timestamp < expirationTime)
                    {
                        _responseDictionaryByType.TryRemove(key, out _);
                    }
                }
            }
        }

        private static async Task<String> ReceiveMessageAsync()
        {
            string message = "";
            bool receivedMessage = false;

            DateTime receivedNotValidGiveUp = DateTime.MinValue;

            while (!receivedMessage)
            {
                var buffer = new Byte[1024 * 36];
                var result = await _webSocket.ReceiveAsync(new ArraySegment<Byte>(buffer), CancellationToken.None);

                if (receivedNotValidGiveUp != DateTime.MinValue && DateTime.Now > receivedNotValidGiveUp)
                {
                    message = "";
                    receivedNotValidGiveUp = DateTime.MinValue;
                }

                message += Encoding.UTF8.GetString(buffer, 0, result.Count);

                JObject messageObj = null;
                try
                {
                    messageObj = JObject.Parse(message);

                    receivedMessage = true;
                }
                catch (Exception)
                {
                    // If the message is not valid JSON, keep reading until we get a valid JSON or giveup on timeout
                    if (receivedNotValidGiveUp == DateTime.MinValue)
                    {
                        receivedNotValidGiveUp = DateTime.Now.AddSeconds(5);
                    }
                }
            }

            return message;
        }

        public async Task<GetUserResponse> GetUserAsync()
        {
            var message = $"{{\"action\":\"getUser\",\"id\":\"{Guid.NewGuid()}\",\"payload\":{{}}}}";
            await SendMessageAsync(message);
            return await ReceiveAndProcessResponse<GetUserResponse>(message);
        }

        public async Task<GetUserLicenseResponse> GetUserLicenseAsync()
        {
            var message = $"{{\"action\":\"getUserLicense\",\"id\":\"{Guid.NewGuid()}\",\"payload\":{{}}}}";
            await SendMessageAsync(message);
            return await ReceiveAndProcessResponse<GetUserLicenseResponse>(message);
        }

        public async Task<GetVoicesResponse> GetVoicesAsync()
        {
            var message = $"{{\"action\":\"getVoices\",\"id\":\"{Guid.NewGuid()}\",\"payload\":{{}}}}";
            await SendMessageAsync(message);
            return await ReceiveAndProcessResponse<GetVoicesResponse>(message);
        }

        public async Task<GetCurrentVoiceResponse> GetCurrentVoiceAsync()
        {
            var message = $"{{\"action\":\"getCurrentVoice\",\"id\":\"{Guid.NewGuid()}\",\"payload\":{{}}}}";
            await SendMessageAsync(message);
            return await ReceiveAndProcessResponse<GetCurrentVoiceResponse>(message);
        }

        public async Task<GetAllSoundboardResponse> GetAllSoundboardAsync()
        {
            var message = $"{{\"action\":\"getAllSoundboard\",\"id\":\"{Guid.NewGuid()}\",\"payload\":{{}}}}";
            await SendMessageAsync(message);
            return await ReceiveAndProcessResponse<GetAllSoundboardResponse>(message);
        }

        public async Task<GetActiveSoundboardProfileResponse> GetActiveSoundboardProfileAsync()
        {
            var message = $"{{\"action\":\"getActiveSoundboardProfile\",\"id\":\"{Guid.NewGuid()}\",\"payload\":{{}}}}";
            await SendMessageAsync(message);
            return await ReceiveAndProcessResponse<GetActiveSoundboardProfileResponse>(message);
        }

        public async Task<GetMemesResponse> GetMemesAsync()
        {
            var message = $"{{\"action\":\"getMemes\",\"id\":\"{Guid.NewGuid()}\",\"payload\":{{}}}}";
            await SendMessageAsync(message);
            return await ReceiveAndProcessResponse<GetMemesResponse>(message);
        }

        public async Task<GetBitmapResponse> GetBitmapAsync(String voiceID = null, String memeID = null)
        {
            var payload = voiceID != null ? $"\"voiceID\":\"{voiceID}\"" : $"\"memeId\":\"{memeID}\"";
            var message = $"{{\"action\":\"getBitmap\",\"id\":\"{Guid.NewGuid()}\",\"payload\":{{{payload}}}}}";
            await SendMessageAsync(message);
            return await ReceiveAndProcessResponse<GetBitmapResponse>(message);
        }

        public async Task LoadVoiceAsync(String voiceID)
        {
            var message = $"{{\"action\":\"loadVoice\",\"id\":\"{Guid.NewGuid()}\",\"payload\":{{\"voiceID\":\"{voiceID}\"}}}}";
            await SendMessageAsync(message);
        }

        public async Task<Boolean> SelectRandomVoiceAsync(String mode = null)
        {
            var payload = mode != null ? $"{{\"mode\":\"{mode}\"}}" : "{}";
            var message = $"{{\"action\":\"selectRandomVoice\",\"id\":\"{Guid.NewGuid()}\",\"payload\":{payload}}}";
            await SendMessageAsync(message);

            //return await ReceiveAndProcessResponse<SelectRandomVoiceResponse>(message);

            return true;
        }

        public async Task<GetHearMyselfStatusResponse> GetHearMyselfStatusAsync()
        {
            var message = $"{{\"action\":\"getHearMyselfStatus\",\"id\":\"{Guid.NewGuid()}\",\"payload\":{{}}}}";
            await SendMessageAsync(message);
            return await ReceiveAndProcessResponse<GetHearMyselfStatusResponse>(message);
        }

        public async Task<ToggleHearMyVoiceResponse> ToggleHearMyVoiceAsync()
        {
            var message = $"{{\"action\":\"toggleHearMyVoice\",\"id\":\"{Guid.NewGuid()}\",\"payload\":{{}}}}";
            await SendMessageAsync(message);
            return await ReceiveAndProcessResponse<ToggleHearMyVoiceResponse>(message);
        }

        public async Task<GetVoiceChangerStatusResponse> GetVoiceChangerStatusAsync()
        {
            var message = $"{{\"action\":\"getVoiceChangerStatus\",\"id\":\"{Guid.NewGuid()}\",\"payload\":{{}}}}";
            await SendMessageAsync(message);
            return await ReceiveAndProcessResponse<GetVoiceChangerStatusResponse>(message);
        }

        public async Task<ToggleVoiceChangerResponse> ToggleVoiceChangerAsync()
        {
            var message = $"{{\"action\":\"toggleVoiceChanger\",\"id\":\"{Guid.NewGuid()}\",\"payload\":{{}}}}";
            await SendMessageAsync(message);
            return await ReceiveAndProcessResponse<ToggleVoiceChangerResponse>(message);
        }

        public async Task<ToggleBackgroundEffectResponse> ToggleBackgroundEffectAsync()
        {
            var message = $"{{\"action\":\"toggleBackground\",\"id\":\"{Guid.NewGuid()}\",\"payload\":{{}}}}";
            await SendMessageAsync(message);
            return await ReceiveAndProcessResponse<ToggleBackgroundEffectResponse>(message);
        }

        public async Task<ToggleMuteMicResponse> ToggleMuteMicAsync()
        {
            var message = $"{{\"action\":\"toggleMuteMic\",\"id\":\"{Guid.NewGuid()}\",\"payload\":{{}}}}";
            await SendMessageAsync(message);
            return await ReceiveAndProcessResponse<ToggleMuteMicResponse>(message);
        }

        public async Task<PlayMemeResponse> PlayMemeAsync(String fileName, Boolean isKeyDown)
        {
            var message = $"{{\"action\":\"playMeme\",\"id\":\"{Guid.NewGuid()}\",\"payload\":{{\"FileName\":\"{fileName}\",\"IsKeyDown\":{isKeyDown.ToString().ToLower()}}}}}";
            await SendMessageAsync(message);
            return await ReceiveAndProcessResponse<PlayMemeResponse>(message);
        }

        public async Task StopAllMemeSoundsAsync()
        {
            var message = $"{{\"action\":\"stopAllMemeSounds\",\"id\":\"{Guid.NewGuid()}\",\"payload\":{{}}}}";
            await SendMessageAsync(message);
        }

        public async Task<GetMuteMemeForMeStatusResponse> GetMuteMemeForMeStatusAsync()
        {
            var message = $"{{\"action\":\"getMuteMemeForMeStatus\",\"id\":\"{Guid.NewGuid()}\",\"payload\":{{}}}}";
            await SendMessageAsync(message);
            return await ReceiveAndProcessResponse<GetMuteMemeForMeStatusResponse>(message);
        }

        public async Task<ToggleMuteMemeForMeResponse> ToggleMuteMemeForMeAsync()
        {
            var message = $"{{\"action\":\"toggleMuteMemeForMe\",\"id\":\"{Guid.NewGuid()}\",\"payload\":{{}}}}";
            await SendMessageAsync(message);
            return await ReceiveAndProcessResponse<ToggleMuteMemeForMeResponse>(message);
        }

        public async Task<SetCurrentVoiceParameterResponse> SetCurrentVoiceParameterAsync(String parameterName, Double value)
        {
            //string actionType = "setCurrentVoiceParameter";
            //CleanActionType(actionType);

            var message = $"{{\"action\":\"setCurrentVoiceParameter\",\"id\":\"{Guid.NewGuid()}\",\"payload\":{{\"parameterName\":\"{parameterName}\",\"parameterValue\":{{\"value\":{value}}}}}}}";
            await SendMessageAsync(message);

            var currentVoiceResponse = await ReceiveAndProcessResponse<SetCurrentVoiceParameterResponse>(message);

            return currentVoiceResponse;
        }

        private T ProcessResponse<T>(String response)
        {
            return JsonConvert.DeserializeObject<T>(response);
        }
    }

    public class RegisterClientResponse
    {
        [JsonProperty(Required = Required.Always)]
        public String Action { get; set; }
        public String Id { get; set; }
        public RegisterClientResponsePayload Payload { get; set; }

        public class RegisterClientResponsePayload
        {
            public PayloadStatus Status { get; set; }

            public class PayloadStatus
            {
                public Int32 Code { get; set; }
                public String Description { get; set; }
            }
        }
    }

    public class GetUserResponse
    {
        [JsonProperty(Required = Required.Always)]
        public String ActionType { get; set; }
        public GetUserResponseActionObject ActionObject { get; set; }

        public class GetUserResponseActionObject
        {
            public String UserId { get; set; }
        }
    }

    public class GetUserLicenseResponse
    {
        [JsonProperty(Required = Required.Always)]
        public String ActionType { get; set; }
        public GetUserLicenseResponseActionObject ActionObject { get; set; }

        public class GetUserLicenseResponseActionObject
        {
            public String LicenseType { get; set; }
        }
    }
    public class GetVoicesResponse
    {
        [JsonProperty(Required = Required.Always)]
        public String ActionType { get; set; }
        public GetVoicesResponseActionObject ActionObject { get; set; }

        public class GetVoicesResponseActionObject
        {
            public List<Voice> Voices { get; set; }

            public class Voice
            {
                public String Id { get; set; }
                public String FriendlyName { get; set; }
                public Boolean Enabled { get; set; }
                public Boolean Favorited { get; set; }
                public Boolean IsNew { get; set; }
                public Boolean IsCustom { get; set; }
                public String BitmapChecksum { get; set; }
            }
        }
    }

    public class GetCurrentVoiceResponse
    {
        [JsonProperty(Required = Required.Always)]
        public String ActionType { get; set; }
        public GetCurrentVoiceResponseActionObject ActionObject { get; set; }

        public class GetCurrentVoiceResponseActionObject
        {
            public String VoiceID { get; set; }
            public Parameter[] Parameters { get; set; }

            public class Parameter
            {
                public String Name { get; set; }
                public Double Value { get; set; }
            }
        }
    }

    public class GetAllSoundboardResponse
    {
        [JsonProperty(Required = Required.Always)]
        public String ActionType { get; set; }
        public GetAllSoundboardResponseActionObject ActionObject { get; set; }

        public class GetAllSoundboardResponseActionObject
        {
            public Soundboard[] Soundboards { get; set; }

            public class Soundboard
            {
                public String Id { get; set; }
                public String Name { get; set; }
                public Boolean IsCustom { get; set; }
                public Boolean Enabled { get; set; }
                public Boolean ShowProLogo { get; set; }
                public Sound[] Sounds { get; set; }

                public class Sound
                {
                    public String Id { get; set; }
                    public String Name { get; set; }
                    public Boolean IsCustom { get; set; }
                    public Boolean Enabled { get; set; }
                    public String PlaybackMode { get; set; }
                    public Boolean Loop { get; set; }
                    public Boolean MuteOtherSounds { get; set; }
                    public Boolean MuteVoice { get; set; }
                    public Boolean StopOtherSounds { get; set; }
                    public Boolean ShowProLogo { get; set; }
                    public String BitmapChecksum { get; set; }
                }
            }
        }
    }

    public class GetActiveSoundboardProfileResponse
    {
        [JsonProperty(Required = Required.Always)]
        public String Type { get; set; }
        public GetActiveSoundboardProfileResponsePayload Payload { get; set; }

        public class GetActiveSoundboardProfileResponsePayload
        {
            public String ProfileId { get; set; }
        }
    }

    public class GetMemesResponse
    {
        [JsonProperty(Required = Required.Always)]
        public String ActionType { get; set; }
        public String ActionId { get; set; }
        public GetMemesResponseActionObject ActionObject { get; set; }

        public class GetMemesResponseActionObject
        {
            public List<Meme> ListOfMemes { get; set; }

            public class Meme
            {
                public String Name { get; set; }
                public String FileName { get; set; }
                public String Type { get; set; }
                public String Image { get; set; }
            }
        }
    }

    public class GetBitmapResponse
    {
        [JsonProperty(Required = Required.Always)]
        public String ActionType { get; set; }
        public GetBitmapResponseActionObject ActionObject { get; set; }

        public class GetBitmapResponseActionObject
        {
            public String Default { get; set; }
            public String Selected { get; set; }
            public String Transparent { get; set; }
        }
    }

    public class LoadVoiceResponse
    {
        [JsonProperty(Required = Required.Always)]
        public String Action { get; set; }
        public String Id { get; set; }
        public Payload Payload { get; set; }
    }

    //public class SelectRandomVoiceResponse
    //{
    //    [JsonProperty(Required = Required.Always)]
    //    public String ActionType { get; set; }
    //    public String ActionId { get; set; }
    //    public SelectRandomVoiceResponseActionObject ActionObject { get; set; }

    //    public class SelectRandomVoiceResponseActionObject
    //    {
    //        public String VoiceID { get; set; }
    //    }
    //}

    public class GetHearMyselfStatusResponse
    {
        [JsonProperty(Required = Required.Always)]
        public String ActionType { get; set; }
        public GetHearMyselfStatusResponseActionObject ActionObject { get; set; }

        public class GetHearMyselfStatusResponseActionObject
        {
            public Boolean Value { get; set; }
        }
    }

    public class ToggleHearMyVoiceResponse
    {
        [JsonProperty(Required = Required.Always)]
        public String ActionType { get; set; }
        public String ActionId { get; set; }
        public ToggleHearMyVoiceResponseActionObject ActionObject { get; set; }

        public class ToggleHearMyVoiceResponseActionObject
        {
            public Boolean Value { get; set; }
        }
    }

    public class GetVoiceChangerStatusResponse
    {
        [JsonProperty(Required = Required.Always)]
        public String ActionType { get; set; }
        public GetVoiceChangerStatusResponseActionObject ActionObject { get; set; }

        public class GetVoiceChangerStatusResponseActionObject
        {
            public Boolean Value { get; set; }
        }
    }

    public class ToggleVoiceChangerResponse
    {
        [JsonProperty(Required = Required.Always)]
        public String ActionType { get; set; }
        public String ActionId { get; set; }
        public ToggleVoiceChangerResponseActionObject ActionObject { get; set; }

        public class ToggleVoiceChangerResponseActionObject
        {
            public Boolean Value { get; set; }
        }
    }

    public class ToggleBackgroundEffectResponse
    {
        [JsonProperty(Required = Required.Always)]
        public String ActionType { get; set; }
        public String ActionId { get; set; }
        public ToggleBackgroundEffectResponseActionObject ActionObject { get; set; }

        public class ToggleBackgroundEffectResponseActionObject
        {
            public Boolean Value { get; set; }
        }
    }

    public class GetMuteMicStatusResponse
    {
        [JsonProperty(Required = Required.Always)]
        public String ActionType { get; set; }
        public GetMuteMicStatusResponseActionObject ActionObject { get; set; }

        public class GetMuteMicStatusResponseActionObject
        {
            public Boolean Value { get; set; }
        }
    }

    public class ToggleMuteMicResponse
    {
        [JsonProperty(Required = Required.Always)]
        public String ActionType { get; set; }
        public String ActionId { get; set; }
        public ToggleMuteMicResponseActionObject ActionObject { get; set; }

        public class ToggleMuteMicResponseActionObject
        {
            public Boolean Value { get; set; }
        }
    }

    public class SetBeepSoundResponse
    {
        [JsonProperty(Required = Required.Always)]
        public String Action { get; set; }
        public String Id { get; set; }
        public Payload Payload { get; set; }
    }

    public class PlayMemeResponse
    {
        [JsonProperty(Required = Required.Always)]
        public String Action { get; set; }
        public String Id { get; set; }
        public Payload Payload { get; set; }
    }

    public class StopAllMemeSoundsResponse
    {
        [JsonProperty(Required = Required.Always)]
        public String Action { get; set; }
        public String Id { get; set; }
        public Payload Payload { get; set; }
    }

    public class GetMuteMemeForMeStatusResponse
    {
        [JsonProperty(Required = Required.Always)]
        public String ActionType { get; set; }
        public String ActionId { get; set; }
        public GetMuteMemeForMeStatusResponseActionObject ActionObject { get; set; }

        public class GetMuteMemeForMeStatusResponseActionObject
        {
            public Boolean Value { get; set; }
        }
    }

    public class ToggleMuteMemeForMeResponse
    {
        [JsonProperty(Required = Required.Always)]
        public String Action { get; set; }
        public String Id { get; set; }
        public Payload Payload { get; set; }
    }

    public class SetCurrentVoiceParameterResponse
    {
        [JsonProperty(Required = Required.Always)]
        public String ActionType { get; set; }
        public String ActionID { get; set; }
        public SetCurrentVoiceParameterResponseActionType ActionObject { get; set; }

        public class SetCurrentVoiceParameterResponseActionType
        {
            public String VoiceID { get; set; }
            public List<SetCurrentVoiceParameterResponseActionTypeParameter> Parameters { get; set; }

            public class SetCurrentVoiceParameterResponseActionTypeParameter
            {
                public String Name { get; set; }
                public Single Default { get; set; }
                public Single MaxValue { get; set; }
                public Single MinValue { get; set; }
                public Boolean DisplayNormalized { get; set; }
                public String TypeController { get; set; }
                public Single Value { get; set; }
            }
        }
    }

    public class Payload
    {
        public PayloadStatus Status { get; set; }

        public class PayloadStatus
        {
            public Int32 Code { get; set; }
            public String Description { get; set; }
        }
    }
}
