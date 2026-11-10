using System;
using System.Collections.Generic;
using System.Linq;
using Forma.Domain;
namespace Forma.Product
{
    public enum PersonalSessionState { NOT_STARTED, PREPARING, ACTIVE, RESTING, AWAITING_FEEDBACK, PAUSED, SKIPPED, COMPLETED, CANCELLED }
    public enum ExerciseReport { Completed, Partial, Skipped }
    [Serializable] public sealed class PersonalBlock
    {
        public string exerciseId;
        public string stage; // WarmUp / Exercise / Cooldown
        public double workSeconds;
        public double restSeconds;
    }
    public sealed class PersonalExercise
    {
        public string Id { get; }
        public string Category { get; }
        public string Difficulty=>"Beginner";
        public string Equipment=>"None";
        public string AnimationReference { get; }
        public MovementLimits ExcludedBy { get; }
        readonly string nameTr,nameEn,guideTr,guideEn,safetyTr,safetyEn;
        public string[] Alternatives { get; }
        public PersonalExercise(string id,string tr,string en,string category,string guideTr,string guideEn,string safetyTr,string safetyEn,MovementLimits excluded,string animation,params string[] alternatives)
        {Id=id;nameTr=tr;nameEn=en;Category=category;this.guideTr=guideTr;this.guideEn=guideEn;this.safetyTr=safetyTr;this.safetyEn=safetyEn;ExcludedBy=excluded;AnimationReference=animation;Alternatives=alternatives;}
        public string Name(string language)=>language=="tr"?nameTr:nameEn;
        public string Guidance(string language)=>language=="tr"?guideTr:guideEn;
        public string Safety(string language)=>language=="tr"?safetyTr:safetyEn;
    }
    public static class PersonalWorkoutCatalog
    {
        public static readonly PersonalExercise[] All={
            new PersonalExercise("march","Yerinde yürüyüş","Marching in place","GentleCardio","Ayakta rahat dur. Ayaklarını sırayla az kaldır; aynı yerde, küçük adımlarla ilerle.","Stand comfortably. Lift each foot a little in turn. Keep small steps in place.","Çevren açık olsun. Dengesizlik veya rahatsızlıkta dur.","Use a clear space. Stop for unsteadiness or discomfort.",MovementLimits.AvoidLowerBody,"procedural:march-v1","mobility"),
            new PersonalExercise("shoulders","Omuz çevirme","Shoulder circles","Mobility","Kolların rahatça yanda olsun. Omuzlarını yavaşça küçük dairelerle geriye çevir.","Relax your arms at your sides. Slowly roll shoulders backward in small circles.","Hareket açıklığını zorlama. Rahatsızlıkta dur.","Do not force your range. Stop for discomfort.",MovementLimits.AvoidShoulders,"procedural:shoulders-v1","mobility"),
            new PersonalExercise("knees","Sırayla diz kaldırma","Alternating knee raises","GentleCardio","Dengeli dik dur. Bir dizini rahat yüksekliğe kaldırıp indir, sonra diğerini kaldır. Acele etme.","Stand steady. Lift one knee to a comfortable height, lower it, then alternate. Move at your own pace.","Dengesizlikte veya rahatsızlıkta dur; yüksekliğini zorlama.","Stop for unsteadiness or discomfort; do not force height.",MovementLimits.AvoidLowerBody,"skeletal:knees-v1","march"),
            new PersonalExercise("squat","Sığ vücut ağırlığı çömelmesi","Shallow bodyweight squat","StrengthFoundation","Ayakların rahatça açık olsun. Kalçanı az geriye alıp dizlerini hafif bük; rahat olduğun derinlikten yavaşça doğrul.","Stand with feet comfortably apart. Move hips slightly back, bend knees a little, then slowly stand from a comfortable shallow depth.","Yalnızca dengeli hissediyorsan dene; ağırlık kullanma. Diz veya bel rahatsızlığında dur.","Try only if steady; use no weights. Stop for knee or back discomfort.",MovementLimits.AvoidLowerBody,null,"march","mobility"),
            new PersonalExercise("calf","Ayakta topuk yükseltme","Standing calf raises","StrengthFoundation","Ayakların yerde ve rahatça açıkken topuklarını az kaldır. Yavaşça yere indir.","With feet on the floor and comfortably apart, lift your heels a little. Lower them slowly.","Dengeni koruyamıyorsan atla. Yakındaki eşyanın yerini takip edebildiğimizi varsayma.","Skip if you cannot stay steady. Do not assume the system tracks nearby support objects.",MovementLimits.AvoidLowerBody,null,"mobility"),
            new PersonalExercise("side","Küçük yan adımlar","Standing side steps","GentleCardio","Boş alanında küçük bir yana adım at, diğer ayağını yaklaştır. Diğer yöne tekrarla; sınırlarından uzaklaşma.","Take a small side step in your clear space, bring the other foot closer, then repeat the other way. Stay well inside your boundaries.","Alan dar ise atla veya yerinde yürüyüş seç. Çevreni göremiyorsan dur.","Skip or use in-place marching if space is tight. Stop if surroundings are not visible.",MovementLimits.AvoidLowerBody,null,"march","mobility"),
            new PersonalExercise("mobility","Rahat ayakta hareketlilik","Gentle standing mobility","Mobility","Rahat dik dur, nefesini tutma. Ayaklarını yerde tutarak gövdeni ve kollarını çok az iki yana hareket ettir; hareket küçük ve yavaş olsun.","Stand comfortably without holding your breath. Keep feet on the floor and move your torso and arms slightly side to side, slowly and gently.","Baş dönmesi, ağrı veya dengesizlikte hareketi bırak. Hareket zorunlu değil.","Stop for dizziness, pain or unsteadiness. Movement is optional.",MovementLimits.AvoidLowerBody,"skeletal:mobility-v1","shoulders")
        };
        public static PersonalExercise Find(string id)=>All.FirstOrDefault(x=>x.Id==id)??throw new ArgumentException("Unknown exercise.");
        public static bool Allowed(PersonalExercise e,AthleteProfileInput p)=>(e.ExcludedBy&p.movementLimits)==0;
        public static void ValidatePlan(IEnumerable<PersonalBlock> blocks,AthleteProfileInput p)
        {
            AthleteProfileInput.Validate(p);var list=blocks?.ToList();
            if(list==null || list.Count<3 || list.Count>14 || list.Any(x=>x==null) || list[0].stage!="WarmUp" || list[list.Count-1].stage!="Cooldown")throw new ArgumentException("Invalid workout structure.");
            foreach(var b in list)if(b==null || !Allowed(Find(b.exerciseId),p) || (b.stage!="WarmUp" && b.stage!="Exercise" && b.stage!="Cooldown") ||
                double.IsNaN(b.workSeconds) || double.IsInfinity(b.workSeconds) || b.workSeconds<10 || b.workSeconds>45 ||
                double.IsNaN(b.restSeconds) || double.IsInfinity(b.restSeconds) || b.restSeconds<0 || b.restSeconds>60 || (b!=list[list.Count-1] && b.restSeconds<10))throw new ArgumentException("Unsupported workout dose.");
        }
    }
    [Serializable] public sealed class ExerciseFeedback
    {
        public int blockIndex;
        public string exerciseId;
        public ExerciseReport outcome;
        public int? difficulty;
        public int? repetitions;
        public bool discomfort;
        public string recordedUtc;
        public static void Validate(ExerciseFeedback f)
        {
            if(f==null || f.blockIndex<0 || f.blockIndex>13 || !Enum.IsDefined(typeof(ExerciseReport),f.outcome) ||
                (f.outcome!=ExerciseReport.Skipped && !f.difficulty.HasValue) || f.difficulty.HasValue && (f.difficulty<1 || f.difficulty>5) ||
                f.repetitions.HasValue && (f.repetitions<0 || f.repetitions>1000))throw new ArgumentException("Invalid exercise feedback.");
            PersonalWorkoutCatalog.Find(f.exerciseId);
        }
    }
    [Serializable] public sealed class PersonalWorkoutRecord
    {
        public string sessionId,athleteId,recommendationId,startedUtc,endedUtc;
        public string workoutId="personal-v1",trackingMode="user-confirmed";
        public double plannedDurationSeconds;
        public string environment="HolographicStudio";
        public bool isTestData;
        public string outcome="Interrupted";
        public string language="tr";
        public AthleteGoal goal;
        public double elapsedSeconds,timedActiveSeconds;
        public List<PersonalBlock> blocks=new List<PersonalBlock>();
        public List<ExerciseFeedback> feedback=new List<ExerciseFeedback>();
    }
    // Owns feedback gates and recording; delegates interval transitions and timing to the existing machine.
    public sealed class PersonalSessionEngine
    {
        public WorkoutMachine Clock { get; }
        public PersonalWorkoutRecord Record { get; }
        public bool AwaitingFeedback { get; private set; }
        bool skipped,completed,cancelled,sessionSkipped;
        public int FeedbackIndex { get; private set; }
        public int DisplayIndex=>AwaitingFeedback?FeedbackIndex:Clock.ExerciseIndex;
        public bool IsRunning=>!AwaitingFeedback && Clock.IsRunning;
        public bool IsActive=>!completed && !cancelled && Clock.State!=WorkoutState.Ready;
        public PersonalSessionState State=>cancelled?PersonalSessionState.CANCELLED:completed?(sessionSkipped?PersonalSessionState.SKIPPED:PersonalSessionState.COMPLETED):
            AwaitingFeedback?(skipped?PersonalSessionState.SKIPPED:PersonalSessionState.AWAITING_FEEDBACK):
            Clock.State==WorkoutState.Ready?PersonalSessionState.NOT_STARTED:Clock.State==WorkoutState.Introduction?PersonalSessionState.PREPARING:
            Clock.State==WorkoutState.Work?PersonalSessionState.ACTIVE:Clock.State==WorkoutState.Rest?PersonalSessionState.RESTING:PersonalSessionState.PAUSED;
        public PersonalSessionEngine(string athleteId,string recommendationId,IEnumerable<PersonalBlock> blocks,AthleteProfileInput profile,DateTimeOffset now,string workoutId="personal-v1")
        {
            bool living=workoutId==LivingCoachWorkout.Id;
            if(living)LivingCoachWorkout.Validate(blocks,profile);else PersonalWorkoutCatalog.ValidatePlan(blocks,profile);
            Record=new PersonalWorkoutRecord{workoutId=workoutId,sessionId=Guid.NewGuid().ToString("N"),athleteId=athleteId,recommendationId=recommendationId,startedUtc=now.ToString("O"),language=profile.language,goal=profile.primaryGoal,environment=profile.preferredEnvironment==TrainingEnvironment.Forest?"Forest":"HolographicStudio",
                blocks=blocks.Select(b=>new PersonalBlock{exerciseId=b.exerciseId,stage=b.stage,workSeconds=b.workSeconds,restSeconds=b.restSeconds}).ToList()};
            Record.plannedDurationSeconds=(living?LivingCoachWorkout.WelcomeSeconds:WorkoutMachine.IntroductionSeconds)+Record.blocks.Sum(b=>b.workSeconds+b.restSeconds);
            Clock=new WorkoutMachine(new WorkoutPlan(workoutId,Record.blocks.Select(b=>{var e=PersonalWorkoutCatalog.Find(b.exerciseId);return new Exercise(e.Id,e.Name(profile.language),e.Guidance(profile.language)+" "+e.Safety(profile.language),b.workSeconds,b.restSeconds,1);})),living?LivingCoachWorkout.WelcomeSeconds:WorkoutMachine.IntroductionSeconds);
        }
        public bool Start(bool ready)=>Clock.Start(ready);
        public void Tick(double seconds,bool ready)
        {
            if(double.IsNaN(seconds)||double.IsInfinity(seconds)||seconds<0||seconds>600)throw new ArgumentException("Invalid clock increment.");
            if(!ready){Pause();return;}
            while(IsRunning && seconds>0)
            {
                double step=Math.Min(seconds,Clock.Remaining);int before=Clock.CompletedSets;int index=Clock.ExerciseIndex;Clock.Tick(step);Record.elapsedSeconds+=step;Record.timedActiveSeconds=Clock.ActiveSeconds;seconds-=step;
                if(Clock.CompletedSets>before){FeedbackIndex=index;AwaitingFeedback=true;Clock.Pause();}
            }
        }
        public void Pause()=>Clock.Pause();
        public bool Resume(bool ready)=>!AwaitingFeedback && Clock.Resume(ready);
        public bool Skip()
        {if(AwaitingFeedback)return false;int index=Clock.ExerciseIndex;if(!Clock.SkipCurrentWork())return false;FeedbackIndex=index;skipped=true;AwaitingFeedback=true;Clock.Pause();return true;}
        public bool Report(ExerciseFeedback input,bool ready,DateTimeOffset now)
        {
            ExerciseFeedback.Validate(input);
            if(!AwaitingFeedback || input.blockIndex!=FeedbackIndex || input.exerciseId!=Record.blocks[FeedbackIndex].exerciseId || skipped && input.outcome!=ExerciseReport.Skipped)return false;
            Record.feedback.Add(new ExerciseFeedback{blockIndex=input.blockIndex,exerciseId=input.exerciseId,outcome=input.outcome,difficulty=input.difficulty,repetitions=input.repetitions,discomfort=input.discomfort,recordedUtc=now.ToString("O")});
            AwaitingFeedback=false;skipped=false;
            if(input.discomfort){Cancel(now);return true;}
            if(Clock.State==WorkoutState.Completed){completed=true;sessionSkipped=Record.feedback.All(f=>f.outcome==ExerciseReport.Skipped);Record.outcome=sessionSkipped?"Skipped":Record.workoutId==LivingCoachWorkout.Id && Record.feedback.Any(f=>f.outcome!=ExerciseReport.Completed)?"Partial":"Completed";Record.endedUtc=now.ToString("O");}
            else Clock.Resume(ready);return true;
        }
        public void Cancel(DateTimeOffset now)
        {if(completed||cancelled)return;cancelled=true;AwaitingFeedback=false;Clock.Abort();Record.outcome="Cancelled";Record.endedUtc=now.ToString("O");}
    }
}
