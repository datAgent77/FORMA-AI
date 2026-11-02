using System;
using System.IO;
using Forma.Domain;
using Forma.Runtime;
using NUnit.Framework;
namespace Forma.Tests
{
    public sealed class StorageTests
    {
        string directory;
        LocalStore store;
        [SetUp] public void Setup() { directory=Path.Combine(Path.GetTempPath(),"forma-test-"+Guid.NewGuid().ToString("N"));store=new LocalStore(directory); }
        [TearDown] public void Cleanup() { if(Directory.Exists(directory)) Directory.Delete(directory,true); }
        [Test] public void SaveIsIdempotentAndSurvivesReload()
        {
            var r=new SessionRecord{sessionId=Guid.NewGuid().ToString("N"),startedUtc="2026-10-31T00:00:00Z",outcome="Completed",activeSeconds=20};
            store.Save(r);r.activeSeconds=40;store.Save(r);
            var rows=new LocalStore(directory).Load();Assert.AreEqual(1,rows.Count);Assert.AreEqual(40,rows[0].activeSeconds);
        }
        [Test] public void CorruptAndUnknownVersionFilesDoNotHideValidSession()
        {
            store.Save(new SessionRecord{sessionId=Guid.NewGuid().ToString("N"),startedUtc="2026-10-31"});
            File.WriteAllText(Path.Combine(directory,"sessions","broken.json"),"this is not json");
            File.WriteAllText(Path.Combine(directory,"sessions","future.json"),"{\"schemaVersion\":99,\"sessionId\":\"future\"}");
            Assert.AreEqual(1,store.Load().Count);
        }
        [Test] public void PathTraversalRejected() { Assert.Throws<ArgumentException>(()=>store.Save(new SessionRecord{sessionId="../../escape"})); }
        [Test] public void DeleteRemovesProfileAndSessions()
        { store.SaveProfile(new LocalProfile());store.Save(new SessionRecord{sessionId=Guid.NewGuid().ToString("N")});store.DeleteAll();Assert.AreEqual(0,store.Load().Count);Assert.False(File.Exists(Path.Combine(directory,"profile.json"))); }
    }
}
