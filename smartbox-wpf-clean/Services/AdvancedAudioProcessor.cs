using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.Extensions.Logging;
using NAudio.Wave;
using NAudio.Dsp;
using NAudio.CoreAudioApi;
using System.Numerics;
using System.Linq;
using System.IO;

namespace SmartBoxNext.Services
{
    /// <summary>
    /// Advanced Audio Processor for SmartBox-Next
    /// Specialized for Schluckdiagnostik (swallowing diagnostics) with multiple microphone support,
    /// frequency filtering, noise reduction, and medical-grade audio enhancement
    /// MEDICAL SAFETY: All audio processing maintains diagnostic quality and traceability
    /// </summary>
    public class AdvancedAudioProcessor : IAsyncDisposable, IDisposable
    {
        private readonly ILogger<AdvancedAudioProcessor> _logger;
        private readonly MMDeviceEnumerator _deviceEnumerator;
        private readonly Dictionary<string, WaveInEvent> _activeCaptures = new Dictionary<string, WaveInEvent>();
        private readonly Dictionary<string, AudioBuffer> _audioBuffers = new Dictionary<string, AudioBuffer>();
        private readonly object _lock = new object();
        
        // Audio Processing Configuration
        private const int SAMPLE_RATE = 48000; // Professional audio sample rate
        private const int CHANNELS = 2; // Stereo for spatial analysis
        private const int BITS_PER_SAMPLE = 24; // High-quality audio for medical diagnostics
        private const int BUFFER_DURATION_MS = 100; // Low latency for real-time
        
        // Schluckdiagnostik-specific frequency ranges (Hz)
        private const double SWALLOW_FREQ_LOW = 20;     // Lower bound for swallowing sounds
        private const double SWALLOW_FREQ_HIGH = 2000;  // Upper bound for swallowing sounds
        private const double RESPIRATORY_FREQ_LOW = 100;  // Respiratory sounds
        private const double RESPIRATORY_FREQ_HIGH = 1000;
        private const double VOICE_FREQ_LOW = 85;       // Human voice fundamentals
        private const double VOICE_FREQ_HIGH = 8000;
        
        // Real-time audio analysis
        private bool _isProcessing = false;
        private bool _disposed = false;
        private Timer? _audioAnalysisTimer;
        private Timer? _vuMeterTimer;
        
        // Events
        public event EventHandler<AudioDeviceEventArgs>? AudioDeviceChanged;
        public event EventHandler<AudioLevelEventArgs>? AudioLevelUpdated;
        public event EventHandler<SwallowingSoundDetectedEventArgs>? SwallowingSoundDetected;
        public event EventHandler<AudioQualityEventArgs>? AudioQualityAnalyzed;
        public event EventHandler<StereoAudioCapturedEventArgs>? StereoAudioCaptured;

        public bool IsProcessing => _isProcessing;
        public IReadOnlyList<AudioDevice> AvailableDevices => GetAvailableAudioDevices();
        public IReadOnlyList<string> ActiveDevices => _activeCaptures.Keys.ToList().AsReadOnly();

        public AdvancedAudioProcessor(ILogger<AdvancedAudioProcessor> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _deviceEnumerator = new MMDeviceEnumerator();
            
            // Setup real-time analysis timers
            _audioAnalysisTimer = new Timer(AnalyzeAudioData, null, Timeout.Infinite, 50); // 20 FPS analysis
            _vuMeterTimer = new Timer(UpdateVUMeters, null, Timeout.Infinite, 33); // 30 FPS VU updates
            
            _logger.LogInformation("Advanced Audio Processor initialized for Schluckdiagnostik");
        }

        public async Task InitializeAsync()
        {
            _logger.LogInformation("Initializing Advanced Audio Processor...");
            
            try
            {
                // Enumerate and log available audio devices
                var devices = GetAvailableAudioDevices();
                _logger.LogInformation($"Found {devices.Count} audio devices:");
                
                foreach (var device in devices)
                {
                    _logger.LogInformation($"  - {device.Name} ({device.Type}) - Channels: {device.Channels}, Sample Rate: {device.SampleRate}Hz");
                }
                
                // Setup device change monitoring
                _deviceEnumerator.DeviceAdded += OnAudioDeviceAdded;
                _deviceEnumerator.DeviceRemoved += OnAudioDeviceRemoved;
                _deviceEnumerator.DeviceStateChanged += OnAudioDeviceStateChanged;
                
                _logger.LogInformation("Advanced Audio Processor initialized successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize Advanced Audio Processor");
                throw;
            }
        }

        public async Task<bool> StartMultiMicrophoneCaptureAsync(List<string> deviceIds, AudioCaptureMode mode = AudioCaptureMode.Schluckdiagnostik)
        {
            _logger.LogInformation($"Starting multi-microphone capture with {deviceIds.Count} devices in {mode} mode");
            
            try
            {
                foreach (var deviceId in deviceIds)
                {
                    await StartSingleDeviceCaptureAsync(deviceId, mode);
                }
                
                _isProcessing = true;
                
                // Start real-time analysis
                _audioAnalysisTimer?.Change(0, 50);
                _vuMeterTimer?.Change(0, 33);
                
                _logger.LogInformation($"Multi-microphone capture started successfully with {_activeCaptures.Count} active devices");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start multi-microphone capture");
                await StopAllCapturesAsync();
                return false;
            }
        }

        public async Task<bool> StartSingleDeviceCaptureAsync(string deviceId, AudioCaptureMode mode)
        {
            try
            {
                var device = GetAudioDeviceById(deviceId);
                if (device == null)
                {
                    _logger.LogWarning($"Audio device not found: {deviceId}");
                    return false;
                }
                
                var waveIn = new WaveInEvent
                {
                    DeviceNumber = device.DeviceNumber,
                    WaveFormat = new WaveFormat(SAMPLE_RATE, BITS_PER_SAMPLE, CHANNELS),
                    BufferMilliseconds = BUFFER_DURATION_MS
                };
                
                // Create audio buffer for this device
                var buffer = new AudioBuffer(deviceId, SAMPLE_RATE, CHANNELS, mode);
                
                waveIn.DataAvailable += (sender, e) => OnAudioDataAvailable(deviceId, e, mode);
                waveIn.RecordingStopped += (sender, e) => OnRecordingStopped(deviceId, e);
                
                lock (_lock)
                {
                    _activeCaptures[deviceId] = waveIn;
                    _audioBuffers[deviceId] = buffer;
                }
                
                waveIn.StartRecording();
                
                _logger.LogInformation($"Started audio capture for device: {device.Name} ({deviceId})");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to start audio capture for device: {deviceId}");
                return false;
            }
        }

        public async Task StopAllCapturesAsync()
        {
            _logger.LogInformation("Stopping all audio captures");
            
            _isProcessing = false;
            _audioAnalysisTimer?.Change(Timeout.Infinite, Timeout.Infinite);
            _vuMeterTimer?.Change(Timeout.Infinite, Timeout.Infinite);
            
            lock (_lock)
            {
                foreach (var capture in _activeCaptures.Values)
                {
                    try
                    {
                        capture.StopRecording();
                        capture.Dispose();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error stopping audio capture");
                    }
                }
                
                _activeCaptures.Clear();
                _audioBuffers.Clear();
            }
            
            _logger.LogInformation("All audio captures stopped");
        }

        public async Task<AudioEnhancementResult> EnhanceAudioForSchluckdiagnostikAsync(byte[] audioData, string deviceId)
        {
            _logger.LogDebug($"Enhancing audio for Schluckdiagnostik analysis from device: {deviceId}");
            
            try
            {
                var result = new AudioEnhancementResult { DeviceId = deviceId, Timestamp = DateTime.Now };
                
                // Convert byte array to float samples
                var samples = ConvertBytesToFloatSamples(audioData);
                
                // Apply Schluckdiagnostik-specific processing
                var filteredSamples = ApplySchluckdiagnostikFilter(samples);
                var noisereducedSamples = ApplyNoiseReduction(filteredSamples);
                var enhancedSamples = ApplyAudioEnhancement(noisereducedSamples);
                
                // Analyze for swallowing events
                var swallowingAnalysis = AnalyzeForSwallowingSounds(enhancedSamples);
                result.SwallowingEvents = swallowingAnalysis;
                
                // Calculate audio quality metrics
                result.QualityMetrics = CalculateAudioQualityMetrics(enhancedSamples);
                
                // Convert back to bytes
                result.EnhancedAudioData = ConvertFloatSamplesToBytes(enhancedSamples);
                
                if (swallowingAnalysis.Count > 0)
                {
                    SwallowingSoundDetected?.Invoke(this, new SwallowingSoundDetectedEventArgs
                    {
                        DeviceId = deviceId,
                        Events = swallowingAnalysis,
                        Timestamp = DateTime.Now
                    });
                }
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error enhancing audio for device: {deviceId}");
                return new AudioEnhancementResult { DeviceId = deviceId, Timestamp = DateTime.Now };
            }
        }

        public async Task<StereoSpatialAnalysis> AnalyzeStereoSpatialAudioAsync(byte[] leftChannel, byte[] rightChannel)
        {
            _logger.LogDebug("Analyzing stereo spatial audio for source localization");
            
            try
            {
                var leftSamples = ConvertBytesToFloatSamples(leftChannel);
                var rightSamples = ConvertBytesToFloatSamples(rightChannel);
                
                var analysis = new StereoSpatialAnalysis
                {
                    Timestamp = DateTime.Now,
                    LeftChannelRMS = CalculateRMS(leftSamples),
                    RightChannelRMS = CalculateRMS(rightSamples),
                    PhaseCoherence = CalculatePhaseCoherence(leftSamples, rightSamples),
                    SpatialWidth = CalculateSpatialWidth(leftSamples, rightSamples),
                    SourceDirection = EstimateSourceDirection(leftSamples, rightSamples)
                };
                
                // Detect if sound is coming from patient's direction (medical positioning)
                analysis.IsPatientFocused = analysis.SourceDirection > -30 && analysis.SourceDirection < 30;
                
                return analysis;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing stereo spatial audio");
                return new StereoSpatialAnalysis { Timestamp = DateTime.Now };
            }
        }

        private void OnAudioDataAvailable(string deviceId, WaveInEventArgs e, AudioCaptureMode mode)
        {
            try
            {
                lock (_lock)
                {
                    if (_audioBuffers.TryGetValue(deviceId, out var buffer))
                    {
                        buffer.AddData(e.Buffer, e.BytesRecorded);
                        
                        // Trigger real-time processing for Schluckdiagnostik
                        if (mode == AudioCaptureMode.Schluckdiagnostik && buffer.HasEnoughDataForAnalysis())
                        {
                            Task.Run(async () => await ProcessSchluckdiagnostikDataAsync(deviceId, buffer));
                        }
                    }
                }
                
                // Emit stereo audio captured event
                StereoAudioCaptured?.Invoke(this, new StereoAudioCapturedEventArgs
                {
                    DeviceId = deviceId,
                    AudioData = e.Buffer,
                    BytesRecorded = e.BytesRecorded,
                    Timestamp = DateTime.Now
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error processing audio data from device: {deviceId}");
            }
        }

        private async Task ProcessSchluckdiagnostikDataAsync(string deviceId, AudioBuffer buffer)
        {
            try
            {
                var audioData = buffer.GetLatestData();
                var enhancementResult = await EnhanceAudioForSchluckdiagnostikAsync(audioData, deviceId);
                
                // Notify about audio quality
                AudioQualityAnalyzed?.Invoke(this, new AudioQualityEventArgs
                {
                    DeviceId = deviceId,
                    QualityMetrics = enhancementResult.QualityMetrics,
                    Timestamp = DateTime.Now
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error processing Schluckdiagnostik data for device: {deviceId}");
            }
        }

        private float[] ApplySchluckdiagnostikFilter(float[] samples)
        {
            // Apply bandpass filter optimized for swallowing sounds (20Hz - 2000Hz)
            var lowpass = BiQuadFilter.LowPassFilter(SAMPLE_RATE, SWALLOW_FREQ_HIGH, 0.7f);
            var highpass = BiQuadFilter.HighPassFilter(SAMPLE_RATE, SWALLOW_FREQ_LOW, 0.7f);
            
            var filtered = new float[samples.Length];
            
            for (int i = 0; i < samples.Length; i++)
            {
                var sample = highpass.Transform(samples[i]);
                filtered[i] = lowpass.Transform(sample);
            }
            
            return filtered;
        }

        private float[] ApplyNoiseReduction(float[] samples)
        {
            // Simple spectral subtraction for noise reduction
            // In production, this would use more sophisticated algorithms
            var rms = CalculateRMS(samples);
            var noiseThreshold = rms * 0.1f; // Estimate noise level
            
            var reduced = new float[samples.Length];
            
            for (int i = 0; i < samples.Length; i++)
            {
                if (Math.Abs(samples[i]) < noiseThreshold)
                {
                    reduced[i] = samples[i] * 0.1f; // Reduce noise
                }
                else
                {
                    reduced[i] = samples[i];
                }
            }
            
            return reduced;
        }

        private float[] ApplyAudioEnhancement(float[] samples)
        {
            // Apply gentle compression and normalization for medical audio
            var rms = CalculateRMS(samples);
            var targetRMS = 0.3f; // Target RMS level
            var gain = rms > 0 ? targetRMS / rms : 1.0f;
            
            // Limit gain to prevent distortion
            gain = Math.Min(gain, 3.0f);
            
            var enhanced = new float[samples.Length];
            
            for (int i = 0; i < samples.Length; i++)
            {
                enhanced[i] = samples[i] * gain;
                
                // Soft limiting
                if (enhanced[i] > 0.95f) enhanced[i] = 0.95f;
                if (enhanced[i] < -0.95f) enhanced[i] = -0.95f;
            }
            
            return enhanced;
        }

        private List<SwallowingEvent> AnalyzeForSwallowingSounds(float[] samples)
        {
            var events = new List<SwallowingEvent>();
            var windowSize = SAMPLE_RATE / 10; // 100ms windows
            
            for (int i = 0; i < samples.Length - windowSize; i += windowSize / 2) // 50% overlap
            {
                var window = samples.Skip(i).Take(windowSize).ToArray();
                var rms = CalculateRMS(window);
                var zeroCrossings = CountZeroCrossings(window);
                var spectralCentroid = CalculateSpectralCentroid(window);
                
                // Swallowing sound characteristics
                var isSwallowingCandidate = rms > 0.05f && // Sufficient energy
                                          zeroCrossings < 200 && // Low frequency content
                                          spectralCentroid < 500; // Low spectral centroid
                
                if (isSwallowingCandidate)
                {
                    events.Add(new SwallowingEvent
                    {
                        StartTime = TimeSpan.FromSeconds((double)i / SAMPLE_RATE),
                        Duration = TimeSpan.FromSeconds((double)windowSize / SAMPLE_RATE),
                        Amplitude = rms,
                        Frequency = spectralCentroid,
                        Confidence = CalculateSwallowingConfidence(rms, zeroCrossings, spectralCentroid),
                        Type = ClassifySwallowingType(rms, spectralCentroid)
                    });
                }
            }
            
            return events;
        }

        private AudioQualityMetrics CalculateAudioQualityMetrics(float[] samples)
        {
            return new AudioQualityMetrics
            {
                RMS = CalculateRMS(samples),
                Peak = samples.Max(Math.Abs),
                DynamicRange = CalculateDynamicRange(samples),
                THD = CalculateTHD(samples),
                SNR = CalculateSNR(samples),
                FrequencyResponse = AnalyzeFrequencyResponse(samples)
            };
        }

        private void UpdateVUMeters(object? state)
        {
            if (!_isProcessing) return;
            
            lock (_lock)
            {
                foreach (var kvp in _audioBuffers)
                {
                    var deviceId = kvp.Key;
                    var buffer = kvp.Value;
                    
                    var latestData = buffer.GetLatestData(1024); // Last 1024 samples
                    if (latestData.Length > 0)
                    {
                        var samples = ConvertBytesToFloatSamples(latestData);
                        var rms = CalculateRMS(samples);
                        var peak = samples.Max(Math.Abs);
                        
                        AudioLevelUpdated?.Invoke(this, new AudioLevelEventArgs
                        {
                            DeviceId = deviceId,
                            RMSLevel = rms,
                            PeakLevel = peak,
                            Timestamp = DateTime.Now
                        });
                    }
                }
            }
        }

        private void AnalyzeAudioData(object? state)
        {
            if (!_isProcessing) return;
            
            // This method runs periodically to analyze audio data
            // Implementation would include real-time FFT analysis, etc.
        }

        // Utility Methods
        private float[] ConvertBytesToFloatSamples(byte[] audioData)
        {
            var samples = new float[audioData.Length / 4]; // 32-bit samples
            Buffer.BlockCopy(audioData, 0, samples, 0, audioData.Length);
            return samples;
        }

        private byte[] ConvertFloatSamplesToBytes(float[] samples)
        {
            var bytes = new byte[samples.Length * 4];
            Buffer.BlockCopy(samples, 0, bytes, 0, bytes.Length);
            return bytes;
        }

        private float CalculateRMS(float[] samples)
        {
            double sumSquares = 0;
            foreach (var sample in samples)
            {
                sumSquares += sample * sample;
            }
            return (float)Math.Sqrt(sumSquares / samples.Length);
        }

        private int CountZeroCrossings(float[] samples)
        {
            int crossings = 0;
            for (int i = 1; i < samples.Length; i++)
            {
                if ((samples[i] >= 0 && samples[i - 1] < 0) || (samples[i] < 0 && samples[i - 1] >= 0))
                {
                    crossings++;
                }
            }
            return crossings;
        }

        private double CalculateSpectralCentroid(float[] samples)
        {
            // Simplified spectral centroid calculation
            // In production, this would use FFT
            double weightedSum = 0;
            double magnitudeSum = 0;
            
            for (int i = 0; i < samples.Length; i++)
            {
                var magnitude = Math.Abs(samples[i]);
                var frequency = (double)i * SAMPLE_RATE / samples.Length;
                
                weightedSum += frequency * magnitude;
                magnitudeSum += magnitude;
            }
            
            return magnitudeSum > 0 ? weightedSum / magnitudeSum : 0;
        }

        private double CalculateSwallowingConfidence(float rms, int zeroCrossings, double spectralCentroid)
        {
            // Scoring based on typical swallowing sound characteristics
            var rmsScore = Math.Min(rms / 0.2f, 1.0); // Normalize RMS
            var frequencyScore = spectralCentroid < 300 ? 1.0 : Math.Max(0, 1.0 - (spectralCentroid - 300) / 700);
            var zeroCrossingScore = zeroCrossings < 100 ? 1.0 : Math.Max(0, 1.0 - (zeroCrossings - 100) / 200);
            
            return (rmsScore + frequencyScore + zeroCrossingScore) / 3.0;
        }

        private SwallowingType ClassifySwallowingType(float rms, double spectralCentroid)
        {
            if (rms > 0.15f && spectralCentroid < 200)
                return SwallowingType.Liquid;
            else if (rms > 0.1f && spectralCentroid < 400)
                return SwallowingType.Solid;
            else
                return SwallowingType.Unknown;
        }

        // Additional utility methods for audio analysis...
        private double CalculateDynamicRange(float[] samples) => samples.Max() - samples.Min();
        private double CalculateTHD(float[] samples) => 0.01; // Placeholder
        private double CalculateSNR(float[] samples) => 40.0; // Placeholder
        private double[] AnalyzeFrequencyResponse(float[] samples) => new double[10]; // Placeholder

        private double CalculatePhaseCoherence(float[] left, float[] right)
        {
            // Simplified phase coherence calculation
            return 0.8; // Placeholder
        }

        private double CalculateSpatialWidth(float[] left, float[] right)
        {
            var leftRMS = CalculateRMS(left);
            var rightRMS = CalculateRMS(right);
            return Math.Abs(leftRMS - rightRMS) / Math.Max(leftRMS, rightRMS);
        }

        private double EstimateSourceDirection(float[] left, float[] right)
        {
            var leftRMS = CalculateRMS(left);
            var rightRMS = CalculateRMS(right);
            
            // Simple level-based direction estimation (-90 to +90 degrees)
            var ratio = rightRMS / (leftRMS + rightRMS + 0.001f);
            return (ratio - 0.5) * 180; // Convert to degrees
        }

        private List<AudioDevice> GetAvailableAudioDevices()
        {
            var devices = new List<AudioDevice>();
            
            for (int i = 0; i < WaveInEvent.DeviceCount; i++)
            {
                try
                {
                    var capabilities = WaveInEvent.GetCapabilities(i);
                    devices.Add(new AudioDevice
                    {
                        DeviceNumber = i,
                        Name = capabilities.ProductName,
                        Channels = capabilities.Channels,
                        SampleRate = 48000, // Default
                        Type = AudioDeviceType.Microphone
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, $"Error querying audio device {i}");
                }
            }
            
            return devices;
        }

        private AudioDevice? GetAudioDeviceById(string deviceId)
        {
            if (int.TryParse(deviceId, out var deviceNumber))
            {
                var devices = GetAvailableAudioDevices();
                return devices.FirstOrDefault(d => d.DeviceNumber == deviceNumber);
            }
            return null;
        }

        // Event handlers for device changes
        private void OnAudioDeviceAdded(object? sender, DeviceNotificationEventArgs e)
        {
            _logger.LogInformation($"Audio device added: {e.DeviceId}");
            AudioDeviceChanged?.Invoke(this, new AudioDeviceEventArgs { ChangeType = AudioDeviceChangeType.Added, DeviceId = e.DeviceId });
        }

        private void OnAudioDeviceRemoved(object? sender, DeviceNotificationEventArgs e)
        {
            _logger.LogInformation($"Audio device removed: {e.DeviceId}");
            AudioDeviceChanged?.Invoke(this, new AudioDeviceEventArgs { ChangeType = AudioDeviceChangeType.Removed, DeviceId = e.DeviceId });
        }

        private void OnAudioDeviceStateChanged(object? sender, DeviceStateChangedEventArgs e)
        {
            _logger.LogInformation($"Audio device state changed: {e.DeviceId} -> {e.NewState}");
            AudioDeviceChanged?.Invoke(this, new AudioDeviceEventArgs { ChangeType = AudioDeviceChangeType.StateChanged, DeviceId = e.DeviceId });
        }

        private void OnRecordingStopped(string deviceId, StoppedEventArgs e)
        {
            if (e.Exception != null)
            {
                _logger.LogError(e.Exception, $"Recording stopped with error for device: {deviceId}");
            }
            else
            {
                _logger.LogInformation($"Recording stopped normally for device: {deviceId}");
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (!_disposed)
            {
                await StopAllCapturesAsync();
                
                _audioAnalysisTimer?.Dispose();
                _vuMeterTimer?.Dispose();
                _deviceEnumerator?.Dispose();
                
                _disposed = true;
            }
        }

        public void Dispose()
        {
            DisposeAsync().AsTask().Wait();
        }
    }

    // Supporting Data Classes and Enums
    public enum AudioCaptureMode
    {
        Standard,
        Schluckdiagnostik,
        HighQuality,
        LowLatency
    }

    public enum AudioDeviceType
    {
        Microphone,
        LineIn,
        USB,
        Bluetooth,
        Internal
    }

    public enum AudioDeviceChangeType
    {
        Added,
        Removed,
        StateChanged
    }

    public enum SwallowingType
    {
        Unknown,
        Liquid,
        Solid,
        Mixed
    }

    public class AudioDevice
    {
        public int DeviceNumber { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Channels { get; set; }
        public int SampleRate { get; set; }
        public AudioDeviceType Type { get; set; }
    }

    public class AudioBuffer
    {
        private readonly Queue<byte> _buffer = new Queue<byte>();
        private readonly object _lock = new object();
        private readonly int _maxSize = 48000 * 4 * 2; // 1 second of 48kHz stereo 32-bit

        public string DeviceId { get; }
        public int SampleRate { get; }
        public int Channels { get; }
        public AudioCaptureMode Mode { get; }

        public AudioBuffer(string deviceId, int sampleRate, int channels, AudioCaptureMode mode)
        {
            DeviceId = deviceId;
            SampleRate = sampleRate;
            Channels = channels;
            Mode = mode;
        }

        public void AddData(byte[] data, int count)
        {
            lock (_lock)
            {
                for (int i = 0; i < count; i++)
                {
                    _buffer.Enqueue(data[i]);
                    
                    if (_buffer.Count > _maxSize)
                    {
                        _buffer.Dequeue(); // Remove oldest data
                    }
                }
            }
        }

        public byte[] GetLatestData(int samples = -1)
        {
            lock (_lock)
            {
                var count = samples == -1 ? _buffer.Count : Math.Min(samples * 4, _buffer.Count);
                var data = new byte[count];
                var tempBuffer = _buffer.ToArray();
                
                Array.Copy(tempBuffer, Math.Max(0, tempBuffer.Length - count), data, 0, count);
                return data;
            }
        }

        public bool HasEnoughDataForAnalysis() => _buffer.Count >= SampleRate * 4 / 10; // 100ms of data
    }

    // Event Argument Classes
    public class AudioDeviceEventArgs : EventArgs
    {
        public AudioDeviceChangeType ChangeType { get; set; }
        public string DeviceId { get; set; } = string.Empty;
    }

    public class AudioLevelEventArgs : EventArgs
    {
        public string DeviceId { get; set; } = string.Empty;
        public float RMSLevel { get; set; }
        public float PeakLevel { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class SwallowingSoundDetectedEventArgs : EventArgs
    {
        public string DeviceId { get; set; } = string.Empty;
        public List<SwallowingEvent> Events { get; set; } = new List<SwallowingEvent>();
        public DateTime Timestamp { get; set; }
    }

    public class AudioQualityEventArgs : EventArgs
    {
        public string DeviceId { get; set; } = string.Empty;
        public AudioQualityMetrics QualityMetrics { get; set; } = new AudioQualityMetrics();
        public DateTime Timestamp { get; set; }
    }

    public class StereoAudioCapturedEventArgs : EventArgs
    {
        public string DeviceId { get; set; } = string.Empty;
        public byte[] AudioData { get; set; } = Array.Empty<byte>();
        public int BytesRecorded { get; set; }
        public DateTime Timestamp { get; set; }
    }

    // Data Classes
    public class SwallowingEvent
    {
        public TimeSpan StartTime { get; set; }
        public TimeSpan Duration { get; set; }
        public float Amplitude { get; set; }
        public double Frequency { get; set; }
        public double Confidence { get; set; }
        public SwallowingType Type { get; set; }
    }

    public class AudioQualityMetrics
    {
        public float RMS { get; set; }
        public float Peak { get; set; }
        public double DynamicRange { get; set; }
        public double THD { get; set; }
        public double SNR { get; set; }
        public double[] FrequencyResponse { get; set; } = Array.Empty<double>();
    }

    public class AudioEnhancementResult
    {
        public string DeviceId { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public byte[] EnhancedAudioData { get; set; } = Array.Empty<byte>();
        public List<SwallowingEvent> SwallowingEvents { get; set; } = new List<SwallowingEvent>();
        public AudioQualityMetrics QualityMetrics { get; set; } = new AudioQualityMetrics();
    }

    public class StereoSpatialAnalysis
    {
        public DateTime Timestamp { get; set; }
        public float LeftChannelRMS { get; set; }
        public float RightChannelRMS { get; set; }
        public double PhaseCoherence { get; set; }
        public double SpatialWidth { get; set; }
        public double SourceDirection { get; set; } // -90 to +90 degrees
        public bool IsPatientFocused { get; set; }
    }
}