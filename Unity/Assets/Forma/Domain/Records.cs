using System;
using System.Collections.Generic;
namespace Forma.Domain
{
    [Serializable] public sealed class ManualEntry
    {
        public string exerciseId;
        public int repetitions;
        public float weightKg;
    }
    [Serializable] public sealed class SessionRecord
    {
        public int schemaVersion=1;
        public bool isTestData;
        public string sessionId;
        public string planId;
        public string startedUtc;
        public string endedUtc;
        public string outcome;
        public double activeSeconds;
        public int completedSets;
        public List<ManualEntry> manualEntries=new List<ManualEntry>();
    }
    [Serializable] public sealed class LocalProfile
    {
        public int schemaVersion=1;
        public string profileId="local";
        public bool selectMarch=true;
        public bool selectShoulders=true;
    }
    public interface ISessionRepository
    {
        IReadOnlyList<SessionRecord> Load();
        void Save(SessionRecord session);
        void DeleteAll();
    }
    // Future module contracts only. No network, model, sensor or immersive implementations.
    public interface IPersonalizedCoach { string SuggestPlan(LocalProfile profile); }
    public interface IBodyTracking { bool IsAvailable { get; } } // No accuracy or full-body guarantee.
    public interface IExerciseRecognition { bool TryGetValidatedRepetitions(string exerciseId, out int count); }
    public interface IFormaWorlds { bool CanEnter(bool sessionActive, bool handlingEquipment); }
    public interface IFormaMirror { bool TryLoadComparison(string sessionId, out SessionRecord previous); }
    public interface IEquipmentProfiles { bool TryGetManuallyConfiguredLoad(string equipmentId, out float kilograms); }
    public interface IVoiceInteraction { void CancelListening(); }
    public interface IProgressAnalytics { IReadOnlyList<SessionRecord> ReadLocalHistory(); }
}
