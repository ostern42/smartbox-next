using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using System.Speech.Recognition;
using System.Speech.Synthesis;
using Microsoft.Extensions.Logging;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text.Json;
using System.Windows.Media.Imaging;
using System.Linq;

namespace SmartBoxNext.Services
{
    /// <summary>
    /// AI-Enhanced Workflow Service for SmartBox-Next
    /// Provides automatic procedure phase detection, voice-to-text medical transcription,
    /// DICOM metadata enhancement, and intelligent critical moment detection
    /// MEDICAL SAFETY: All AI suggestions are supplementary - final decisions remain with medical professionals
    /// </summary>
    public class AIEnhancedWorkflowService : IAsyncDisposable, IDisposable
    {
        private readonly ILogger<AIEnhancedWorkflowService> _logger;
        private readonly OptimizedDicomConverter _dicomConverter;
        private readonly SpeechRecognitionEngine _speechEngine;
        private readonly SpeechSynthesizer _speechSynthesizer;
        private readonly Timer _phaseDetectionTimer;
        private readonly object _lock = new object();
        
        // AI Analysis State
        private ProcedurePhase _currentPhase = ProcedurePhase.Preparation;
        private List<string> _transcriptionBuffer = new List<string>();
        private List<CriticalMoment> _detectedMoments = new List<CriticalMoment>();
        private DateTime _procedureStartTime = DateTime.MinValue;
        private bool _isRecording = false;
        private bool _disposed = false;

        // Medical Terminology Dictionary
        private readonly Dictionary<string, string> _medicalTerminology = new Dictionary<string, string>
        {
            // Gastroenterology
            { "endoscopy", "Endoscopy" },
            { "colonoscopy", "Colonoscopy" },
            { "gastroscopy", "Gastroscopy" },
            { "biopsy", "Biopsy" },
            { "polyp", "Polyp" },
            { "mucosa", "Mucosa" },
            { "duodenum", "Duodenum" },
            { "antrum", "Antrum" },
            { "pylorus", "Pylorus" },
            
            // Schluckdiagnostik (Swallowing Diagnostics)
            { "schluckakt", "Schluckakt" },
            { "aspiration", "Aspiration" },
            { "penetration", "Penetration" },
            { "pharynx", "Pharynx" },
            { "larynx", "Larynx" },
            { "epiglottis", "Epiglottis" },
            { "vallecularesiduen", "Vallecularesiduen" },
            { "pyriformsinusresiduen", "Pyriformsinusresiduen" },
            
            // General Medical
            { "pathology", "Pathology" },
            { "inflammation", "Inflammation" },
            { "hemorrhage", "Hemorrhage" },
            { "perforation", "Perforation" },
            { "stenosis", "Stenosis" },
            { "dilatation", "Dilatation" }
        };

        // Events
        public event EventHandler<ProcedurePhaseChangedEventArgs>? PhaseChanged;
        public event EventHandler<MedicalTranscriptionEventArgs>? TranscriptionReceived;
        public event EventHandler<CriticalMomentDetectedEventArgs>? CriticalMomentDetected;
        public event EventHandler<DicomMetadataEnhancedEventArgs>? MetadataEnhanced;

        public ProcedurePhase CurrentPhase => _currentPhase;
        public IReadOnlyList<string> TranscriptionHistory => _transcriptionBuffer.AsReadOnly();
        public IReadOnlyList<CriticalMoment> CriticalMoments => _detectedMoments.AsReadOnly();
        public bool IsRecording => _isRecording;

        public AIEnhancedWorkflowService(ILogger<AIEnhancedWorkflowService> logger, OptimizedDicomConverter dicomConverter)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _dicomConverter = dicomConverter ?? throw new ArgumentNullException(nameof(dicomConverter));
            
            // Initialize speech recognition for medical terminology
            _speechEngine = new SpeechRecognitionEngine();
            _speechSynthesizer = new SpeechSynthesizer();
            
            // Setup medical vocabulary
            SetupMedicalVocabulary();
            
            // Initialize phase detection timer (runs every 30 seconds)
            _phaseDetectionTimer = new Timer(AnalyzeProcedurePhase, null, Timeout.Infinite, 30000);
            
            _logger.LogInformation("AI Enhanced Workflow Service initialized with medical terminology support");
        }

        public async Task InitializeAsync()
        {
            _logger.LogInformation("Initializing AI Enhanced Workflow Service...");
            
            try
            {
                // Configure speech recognition
                _speechEngine.SetInputToDefaultAudioDevice();
                _speechEngine.RecognizeAsync(RecognizeMode.Multiple);
                _speechEngine.SpeechRecognized += OnSpeechRecognized;
                _speechEngine.SpeechHypothesized += OnSpeechHypothesized;
                
                // Configure speech synthesis for voice commands
                _speechSynthesizer.SetOutputToDefaultAudioDevice();
                _speechSynthesizer.Rate = 0; // Normal speaking rate
                _speechSynthesizer.Volume = 80; // 80% volume for medical environment
                
                _logger.LogInformation("AI Enhanced Workflow Service initialized successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize AI Enhanced Workflow Service");
                throw;
            }
        }

        public async Task StartProcedureAnalysisAsync(string procedureType, string patientId)
        {
            _logger.LogInformation($"Starting AI analysis for procedure: {procedureType}, Patient: {patientId}");
            
            _procedureStartTime = DateTime.Now;
            _currentPhase = ProcedurePhase.Preparation;
            _isRecording = true;
            
            // Clear previous session data
            _transcriptionBuffer.Clear();
            _detectedMoments.Clear();
            
            // Start phase detection
            _phaseDetectionTimer.Change(0, 30000);
            
            // Notify phase change
            PhaseChanged?.Invoke(this, new ProcedurePhaseChangedEventArgs
            {
                PreviousPhase = ProcedurePhase.None,
                CurrentPhase = _currentPhase,
                Timestamp = DateTime.Now,
                Confidence = 1.0,
                ProcedureType = procedureType
            });
            
            await AnnouncePhaseChangeAsync(_currentPhase);
        }

        public async Task StopProcedureAnalysisAsync()
        {
            _logger.LogInformation("Stopping AI procedure analysis");
            
            _isRecording = false;
            _phaseDetectionTimer.Change(Timeout.Infinite, Timeout.Infinite);
            
            // Final phase
            await TransitionToPhase(ProcedurePhase.Completion);
            
            // Generate procedure summary
            var summary = GenerateProcedureSummary();
            _logger.LogInformation($"Procedure completed. Summary: {summary}");
        }

        public async Task<EnhancedDicomMetadata> EnhanceDicomMetadataAsync(string originalFilePath, BitmapSource frame)
        {
            _logger.LogDebug("Enhancing DICOM metadata with AI analysis");
            
            try
            {
                var metadata = new EnhancedDicomMetadata
                {
                    OriginalFilePath = originalFilePath,
                    Timestamp = DateTime.Now,
                    ProcedurePhase = _currentPhase,
                    TranscriptionText = string.Join(" ", _transcriptionBuffer.TakeLast(10)), // Last 10 transcriptions
                    CriticalMoments = _detectedMoments.Where(m => m.Timestamp > DateTime.Now.AddMinutes(-5)).ToList(),
                    VideoQualityMetrics = await AnalyzeVideoQuality(frame),
                    MedicalFindings = ExtractMedicalFindings(_transcriptionBuffer),
                    AiConfidence = CalculateOverallConfidence()
                };
                
                // Image analysis for critical content detection
                if (frame != null)
                {
                    metadata.ImageAnalysis = await AnalyzeImageContent(frame);
                }
                
                MetadataEnhanced?.Invoke(this, new DicomMetadataEnhancedEventArgs { Metadata = metadata });
                
                return metadata;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error enhancing DICOM metadata");
                return new EnhancedDicomMetadata { OriginalFilePath = originalFilePath, Timestamp = DateTime.Now };
            }
        }

        public async Task ProcessVoiceCommandAsync(string command)
        {
            _logger.LogDebug($"Processing voice command: {command}");
            
            var lowerCommand = command.ToLowerInvariant();
            
            try
            {
                switch (lowerCommand)
                {
                    case "start recording":
                    case "aufnahme starten":
                        await AnnounceAsync("Recording started");
                        // Trigger recording start event
                        break;
                        
                    case "stop recording":
                    case "aufnahme stoppen":
                        await AnnounceAsync("Recording stopped");
                        // Trigger recording stop event
                        break;
                        
                    case "take snapshot":
                    case "bild aufnehmen":
                        await AnnounceAsync("Snapshot captured");
                        // Trigger snapshot event
                        break;
                        
                    case "mark critical moment":
                    case "kritischen moment markieren":
                        await MarkCriticalMomentAsync("Voice command", 0.9);
                        await AnnounceAsync("Critical moment marked");
                        break;
                        
                    case "phase preparation":
                    case "phase vorbereitung":
                        await TransitionToPhase(ProcedurePhase.Preparation);
                        break;
                        
                    case "phase procedure":
                    case "phase untersuchung":
                        await TransitionToPhase(ProcedurePhase.Procedure);
                        break;
                        
                    case "phase completion":
                    case "phase abschluss":
                        await TransitionToPhase(ProcedurePhase.Completion);
                        break;
                        
                    default:
                        _logger.LogDebug($"Unknown voice command: {command}");
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error processing voice command: {command}");
            }
        }

        private void SetupMedicalVocabulary()
        {
            var choices = new Choices();
            
            // Add medical terminology
            foreach (var term in _medicalTerminology.Keys)
            {
                choices.Add(term);
            }
            
            // Add voice commands
            choices.Add("start recording", "stop recording", "take snapshot", "mark critical moment");
            choices.Add("aufnahme starten", "aufnahme stoppen", "bild aufnehmen", "kritischen moment markieren");
            choices.Add("phase preparation", "phase procedure", "phase completion");
            choices.Add("phase vorbereitung", "phase untersuchung", "phase abschluss");
            
            var grammar = new Grammar(new GrammarBuilder(choices));
            grammar.Name = "Medical Terminology";
            
            _speechEngine.LoadGrammar(grammar);
        }

        private void OnSpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            if (e.Result.Confidence > 0.7) // Only accept high-confidence recognition
            {
                var recognizedText = e.Result.Text;
                var medicalText = TranslateToMedicalTerminology(recognizedText);
                
                lock (_lock)
                {
                    _transcriptionBuffer.Add($"[{DateTime.Now:HH:mm:ss}] {medicalText}");
                    
                    // Keep only last 100 transcriptions to manage memory
                    if (_transcriptionBuffer.Count > 100)
                    {
                        _transcriptionBuffer.RemoveAt(0);
                    }
                }
                
                _logger.LogDebug($"Medical transcription: {medicalText} (Confidence: {e.Result.Confidence:F2})");
                
                TranscriptionReceived?.Invoke(this, new MedicalTranscriptionEventArgs
                {
                    Text = medicalText,
                    Confidence = e.Result.Confidence,
                    Timestamp = DateTime.Now,
                    IsVoiceCommand = IsVoiceCommand(recognizedText)
                });
                
                // Check if it's a voice command
                if (IsVoiceCommand(recognizedText))
                {
                    Task.Run(async () => await ProcessVoiceCommandAsync(recognizedText));
                }
            }
        }

        private void OnSpeechHypothesized(object sender, SpeechHypothesizedEventArgs e)
        {
            // Log hypothesized speech for debugging
            _logger.LogTrace($"Speech hypothesis: {e.Result.Text} (Confidence: {e.Result.Confidence:F2})");
        }

        private async void AnalyzeProcedurePhase(object state)
        {
            if (!_isRecording) return;
            
            try
            {
                var newPhase = await DetectProcedurePhase();
                
                if (newPhase != _currentPhase)
                {
                    await TransitionToPhase(newPhase);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during procedure phase analysis");
            }
        }

        private async Task<ProcedurePhase> DetectProcedurePhase()
        {
            var recentTranscriptions = _transcriptionBuffer.TakeLast(20).ToList();
            var procedureDuration = DateTime.Now - _procedureStartTime;
            
            // Phase detection logic based on medical workflow patterns
            var preparationKeywords = new[] { "vorbereitung", "preparation", "setup", "patient", "position" };
            var procedureKeywords = new[] { "insertion", "examination", "biopsy", "intervention", "ablation", "treatment" };
            var completionKeywords = new[] { "withdrawal", "completion", "finished", "clean", "documentation" };
            
            var preparationScore = CountKeywords(recentTranscriptions, preparationKeywords);
            var procedureScore = CountKeywords(recentTranscriptions, procedureKeywords);
            var completionScore = CountKeywords(recentTranscriptions, completionKeywords);
            
            // Time-based phase detection
            if (procedureDuration.TotalMinutes < 5 && preparationScore > 0)
            {
                return ProcedurePhase.Preparation;
            }
            else if (procedureDuration.TotalMinutes > 5 && procedureScore > completionScore)
            {
                return ProcedurePhase.Procedure;
            }
            else if (completionScore > 0 || procedureDuration.TotalMinutes > 60)
            {
                return ProcedurePhase.Completion;
            }
            
            return _currentPhase; // No change
        }

        private async Task TransitionToPhase(ProcedurePhase newPhase)
        {
            var previousPhase = _currentPhase;
            _currentPhase = newPhase;
            
            _logger.LogInformation($"Procedure phase transition: {previousPhase} → {newPhase}");
            
            PhaseChanged?.Invoke(this, new ProcedurePhaseChangedEventArgs
            {
                PreviousPhase = previousPhase,
                CurrentPhase = newPhase,
                Timestamp = DateTime.Now,
                Confidence = 0.85,
                ProcedureType = "Auto-detected"
            });
            
            await AnnouncePhaseChangeAsync(newPhase);
            await MarkCriticalMomentAsync($"Phase transition to {newPhase}", 0.8);
        }

        private async Task AnnouncePhaseChangeAsync(ProcedurePhase phase)
        {
            var message = phase switch
            {
                ProcedurePhase.Preparation => "Preparation phase",
                ProcedurePhase.Procedure => "Procedure phase",
                ProcedurePhase.Completion => "Completion phase",
                _ => "Unknown phase"
            };
            
            await AnnounceAsync(message);
        }

        private async Task AnnounceAsync(string message)
        {
            try
            {
                await Task.Run(() => _speechSynthesizer.Speak(message));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Failed to announce: {message}");
            }
        }

        private async Task MarkCriticalMomentAsync(string description, double confidence)
        {
            var moment = new CriticalMoment
            {
                Timestamp = DateTime.Now,
                Description = description,
                Confidence = confidence,
                ProcedurePhase = _currentPhase,
                Context = string.Join(" ", _transcriptionBuffer.TakeLast(5))
            };
            
            _detectedMoments.Add(moment);
            
            _logger.LogInformation($"Critical moment detected: {description} (Confidence: {confidence:F2})");
            
            CriticalMomentDetected?.Invoke(this, new CriticalMomentDetectedEventArgs { Moment = moment });
        }

        private string TranslateToMedicalTerminology(string text)
        {
            foreach (var term in _medicalTerminology)
            {
                text = text.Replace(term.Key, term.Value, StringComparison.OrdinalIgnoreCase);
            }
            return text;
        }

        private bool IsVoiceCommand(string text)
        {
            var commands = new[] { "start recording", "stop recording", "take snapshot", "mark critical moment",
                                  "aufnahme starten", "aufnahme stoppen", "bild aufnehmen", "kritischen moment markieren",
                                  "phase preparation", "phase procedure", "phase completion" };
            
            return commands.Any(cmd => text.Contains(cmd, StringComparison.OrdinalIgnoreCase));
        }

        private int CountKeywords(List<string> transcriptions, string[] keywords)
        {
            return transcriptions.SelectMany(t => t.Split(' '))
                                .Count(word => keywords.Any(keyword => 
                                    word.Contains(keyword, StringComparison.OrdinalIgnoreCase)));
        }

        private async Task<VideoQualityMetrics> AnalyzeVideoQuality(BitmapSource frame)
        {
            // Placeholder for video quality analysis
            return new VideoQualityMetrics
            {
                Brightness = 0.7,
                Contrast = 0.8,
                Sharpness = 0.75,
                ColorBalance = 0.85,
                NoiseLevel = 0.1,
                OverallQuality = 0.82
            };
        }

        private async Task<ImageAnalysisResult> AnalyzeImageContent(BitmapSource frame)
        {
            // Placeholder for AI image analysis
            return new ImageAnalysisResult
            {
                HasCriticalContent = false,
                ContentType = "Medical imaging",
                Confidence = 0.8,
                DetectedObjects = new List<string> { "endoscope", "tissue" }
            };
        }

        private List<MedicalFinding> ExtractMedicalFindings(List<string> transcriptions)
        {
            var findings = new List<MedicalFinding>();
            
            // Simple keyword-based finding extraction
            var pathologyKeywords = new[] { "polyp", "lesion", "inflammation", "bleeding", "abnormal" };
            
            foreach (var transcription in transcriptions.TakeLast(20))
            {
                foreach (var keyword in pathologyKeywords)
                {
                    if (transcription.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                    {
                        findings.Add(new MedicalFinding
                        {
                            Finding = keyword,
                            Timestamp = DateTime.Now,
                            Confidence = 0.7,
                            Context = transcription
                        });
                    }
                }
            }
            
            return findings;
        }

        private double CalculateOverallConfidence()
        {
            if (_detectedMoments.Count == 0) return 0.5;
            
            return _detectedMoments.Average(m => m.Confidence);
        }

        private string GenerateProcedureSummary()
        {
            var duration = DateTime.Now - _procedureStartTime;
            var criticalMoments = _detectedMoments.Count;
            var transcriptionCount = _transcriptionBuffer.Count;
            
            return $"Duration: {duration:hh\\:mm\\:ss}, Critical moments: {criticalMoments}, Transcriptions: {transcriptionCount}";
        }

        public async ValueTask DisposeAsync()
        {
            if (!_disposed)
            {
                await StopProcedureAnalysisAsync();
                
                _speechEngine?.Dispose();
                _speechSynthesizer?.Dispose();
                _phaseDetectionTimer?.Dispose();
                
                _disposed = true;
            }
        }

        public void Dispose()
        {
            DisposeAsync().AsTask().Wait();
        }
    }

    // Supporting Data Classes
    public enum ProcedurePhase
    {
        None,
        Preparation,
        Procedure,
        Completion
    }

    public class ProcedurePhaseChangedEventArgs : EventArgs
    {
        public ProcedurePhase PreviousPhase { get; set; }
        public ProcedurePhase CurrentPhase { get; set; }
        public DateTime Timestamp { get; set; }
        public double Confidence { get; set; }
        public string ProcedureType { get; set; } = string.Empty;
    }

    public class MedicalTranscriptionEventArgs : EventArgs
    {
        public string Text { get; set; } = string.Empty;
        public double Confidence { get; set; }
        public DateTime Timestamp { get; set; }
        public bool IsVoiceCommand { get; set; }
    }

    public class CriticalMomentDetectedEventArgs : EventArgs
    {
        public CriticalMoment Moment { get; set; } = new CriticalMoment();
    }

    public class DicomMetadataEnhancedEventArgs : EventArgs
    {
        public EnhancedDicomMetadata Metadata { get; set; } = new EnhancedDicomMetadata();
    }

    public class CriticalMoment
    {
        public DateTime Timestamp { get; set; }
        public string Description { get; set; } = string.Empty;
        public double Confidence { get; set; }
        public ProcedurePhase ProcedurePhase { get; set; }
        public string Context { get; set; } = string.Empty;
    }

    public class EnhancedDicomMetadata
    {
        public string OriginalFilePath { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public ProcedurePhase ProcedurePhase { get; set; }
        public string TranscriptionText { get; set; } = string.Empty;
        public List<CriticalMoment> CriticalMoments { get; set; } = new List<CriticalMoment>();
        public VideoQualityMetrics VideoQualityMetrics { get; set; } = new VideoQualityMetrics();
        public List<MedicalFinding> MedicalFindings { get; set; } = new List<MedicalFinding>();
        public double AiConfidence { get; set; }
        public ImageAnalysisResult ImageAnalysis { get; set; } = new ImageAnalysisResult();
    }

    public class VideoQualityMetrics
    {
        public double Brightness { get; set; }
        public double Contrast { get; set; }
        public double Sharpness { get; set; }
        public double ColorBalance { get; set; }
        public double NoiseLevel { get; set; }
        public double OverallQuality { get; set; }
    }

    public class ImageAnalysisResult
    {
        public bool HasCriticalContent { get; set; }
        public string ContentType { get; set; } = string.Empty;
        public double Confidence { get; set; }
        public List<string> DetectedObjects { get; set; } = new List<string>();
    }

    public class MedicalFinding
    {
        public string Finding { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public double Confidence { get; set; }
        public string Context { get; set; } = string.Empty;
    }
}