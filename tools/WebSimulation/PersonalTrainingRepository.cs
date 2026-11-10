using Forma.Product;
using System.Text.Json;
public sealed class PersonalTrainingDocument
{
    public int schemaVersion=2;
    public List<BodyMeasurement> measurements=new();
    public List<PersonalWorkoutRecord> sessions=new();
    public List<PersonalRecommendation> recommendations=new();
}
public sealed class PersonalTrainingRepository
{
    readonly string root;
    string PathName=>Path.Combine(root,"personal-training.json");
    public bool FailWrites { get; set; }
    public PersonalTrainingRepository(string root){this.root=root;}
    public PersonalTrainingDocument Load()
    {
        if(!File.Exists(PathName))return new PersonalTrainingDocument();
        try
        {
            var d=JsonSerializer.Deserialize<PersonalTrainingDocument>(File.ReadAllText(PathName),SimulationJson.Options);
            if(d==null || (d.schemaVersion!=1 && d.schemaVersion!=2) || d.measurements==null || d.sessions==null || d.recommendations==null || d.sessions.Select(x=>x?.sessionId).Distinct().Count()!=d.sessions.Count)throw new AthleteStorageException("Unsupported training data.");
            foreach(var s in d.sessions)
            {
                if(s==null || !Guid.TryParseExact(s.sessionId,"N",out _) || !Guid.TryParseExact(s.athleteId,"N",out _) || !Guid.TryParseExact(s.recommendationId,"N",out _) ||
                    !DateTimeOffset.TryParse(s.startedUtc,out _) || (s.endedUtc!=null && !DateTimeOffset.TryParse(s.endedUtc,out _)) ||
                    (s.outcome!="Completed" && s.outcome!="Cancelled" && s.outcome!="Interrupted" && s.outcome!="Skipped") || !double.IsFinite(s.elapsedSeconds) || !double.IsFinite(s.timedActiveSeconds) || s.elapsedSeconds<0 || s.timedActiveSeconds<0 || s.timedActiveSeconds>s.elapsedSeconds || s.blocks==null || s.blocks.Count<3 || s.blocks.Count>14 || s.feedback==null || s.feedback.Count>s.blocks.Count || s.feedback.Select(x=>x?.blockIndex).Distinct().Count()!=s.feedback.Count)
                    throw new AthleteStorageException("Invalid training record.");
                foreach(var block in s.blocks)ValidateBlock(block);
                foreach(var f in s.feedback){ExerciseFeedback.Validate(f);if(f.blockIndex>=s.blocks.Count || s.blocks[f.blockIndex].exerciseId!=f.exerciseId || !DateTimeOffset.TryParse(f.recordedUtc,out _))throw new AthleteStorageException("Invalid feedback record.");}
                if(s.outcome=="Completed" && (s.feedback.Count!=s.blocks.Count || s.endedUtc==null))throw new AthleteStorageException("Incomplete session cannot be marked complete.");
            }
            foreach(var r in d.recommendations)
            {
                if(r==null || !Guid.TryParseExact(r.recommendationId,"N",out _) || !Guid.TryParseExact(r.athleteId,"N",out _) || r.profileRevision<1 || r.blocks==null || r.blocks.Count>14 || r.reasons==null || r.evidenceSessionIds==null || !DateTimeOffset.TryParse(r.createdUtc,out _) || (r.status!="Ready" && r.status!="SafetyStop" && r.status!="NoCompatibleWorkout"))throw new AthleteStorageException("Invalid recommendation.");
                foreach(var b in r.blocks)ValidateBlock(b);
            }
            if(d.measurements.Select(m=>m?.measurementId).Distinct().Count()!=d.measurements.Count)throw new AthleteStorageException("Duplicate measurement IDs.");
            foreach(var m in d.measurements){if(m==null || !Guid.TryParseExact(m.measurementId,"N",out _) || !Guid.TryParseExact(m.athleteId,"N",out _) || !DateTimeOffset.TryParse(m.recordedUtc,out _))throw new AthleteStorageException("Invalid measurement identity.");BodyMeasurementInput.Validate(m.entry,DateTimeOffset.UtcNow);}
            if(d.schemaVersion==1){d.schemaVersion=2;Save(d);}
            return d;
        }
        catch(Exception e) when(e is JsonException || e is ArgumentException){throw new AthleteStorageException("Training data is unreadable. Original file preserved.");}
    }
    static void ValidateBlock(PersonalBlock b)
    {
        if(b==null || !double.IsFinite(b.workSeconds) || b.workSeconds<10 || b.workSeconds>45 || !double.IsFinite(b.restSeconds) || b.restSeconds<0 || b.restSeconds>60 || (b.stage!="WarmUp" && b.stage!="Exercise" && b.stage!="Cooldown"))throw new AthleteStorageException("Invalid training block.");
        PersonalWorkoutCatalog.Find(b.exerciseId);
    }
    public void Save(PersonalTrainingDocument d)
    {
        if(FailWrites)throw new AthleteStorageException("Training write failed.");
        Directory.CreateDirectory(root);string temporary=PathName+".tmp";
        using(var stream=new FileStream(temporary,FileMode.Create,FileAccess.Write,FileShare.None)){JsonSerializer.Serialize(stream,d,SimulationJson.Options);stream.Flush(true);}
        File.Move(temporary,PathName,true);
    }
    public string Raw()=>File.Exists(PathName)?File.ReadAllText(PathName):null;
    public void Delete(){if(FailWrites)throw new AthleteStorageException("Training deletion failed.");foreach(var p in new[]{PathName,PathName+".tmp"})if(File.Exists(p))File.Delete(p);}
}
