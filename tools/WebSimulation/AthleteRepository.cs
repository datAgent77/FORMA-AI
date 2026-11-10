using Forma.Product;
using System.Text.Json;

// HTTP and JSON stay outside the shared domain. No file is exposed by static middleware.
public sealed class AthleteDocument
{
    public int schemaVersion=2;
    public int revision;
    public AthleteProfile profile;
}
public sealed class AthleteStorageException : IOException
{ public AthleteStorageException(string message):base(message){} }
public sealed class AthleteConflictException : InvalidOperationException
{ public AthleteConflictException():base("Athlete profile changed in another tab. Reload it before saving."){} }
public sealed class AthleteRepository
{
    readonly string root;
    string PathName=>Path.Combine(root,"athlete-profile.json");
    public bool FailWrites { get; set; }
    public AthleteRepository(string root){this.root=root;}
    public AthleteDocument Load()
    {
        if(!File.Exists(PathName))return new AthleteDocument();
        try
        {
            var json=File.ReadAllText(PathName);var doc=JsonSerializer.Deserialize<AthleteDocument>(json,SimulationJson.Options);
            if(doc==null || (doc.schemaVersion!=1 && doc.schemaVersion!=2) || doc.revision<0 || !AthleteProfile.Valid(doc.profile))
                throw new AthleteStorageException("Unreadable or unsupported athlete data. Original file was preserved; export or restore it before editing.");
            // Schema 1 already had identity/timestamps/preferences; schema 2 adds optional units/environment/limitations defaults.
            if(doc.schemaVersion==1){doc.schemaVersion=2;Write(doc);}
            return doc;
        }
        catch(JsonException){throw new AthleteStorageException("Invalid athlete data. Original file was preserved.");}
    }
    public AthleteDocument Save(AthleteProfileInput input,int expectedRevision)
    {
        AthleteProfileInput.Validate(input);var doc=Load();
        if(doc.revision!=expectedRevision)throw new AthleteConflictException();
        doc.profile=AthleteProfile.Update(doc.profile,input,DateTimeOffset.UtcNow);doc.revision++;
        Write(doc);return doc;
    }
    void Write(AthleteDocument document)
    {
        if(FailWrites)throw new AthleteStorageException("Local athlete storage write failed. Nothing was saved.");
        Directory.CreateDirectory(root);string temporary=PathName+".tmp";
        using(var stream=new FileStream(temporary,FileMode.Create,FileAccess.Write,FileShare.None))
        {JsonSerializer.Serialize(stream,document,SimulationJson.Options);stream.Flush(true);}
        File.Move(temporary,PathName,true);
    }
    public string Raw()=>File.Exists(PathName)?File.ReadAllText(PathName):null;
    public void Delete()
    {
        if(FailWrites)throw new AthleteStorageException("Local deletion failed. Please retry.");
        foreach(var path in new[]{PathName,PathName+".tmp"})if(File.Exists(path))File.Delete(path);
    }
}
