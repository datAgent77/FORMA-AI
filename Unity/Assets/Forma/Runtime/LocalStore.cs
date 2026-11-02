using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Forma.Domain;
using UnityEngine;
namespace Forma.Runtime
{
    // One writer on the main thread. Atomic per-file replacement on the same volume.
    public sealed class LocalStore : ISessionRepository
    {
        readonly string root;
        public LocalStore(string root) { this.root=root; Directory.CreateDirectory(root); }
        string Sessions => Path.Combine(root,"sessions");
        public IReadOnlyList<SessionRecord> Load()
        {
            Directory.CreateDirectory(Sessions);
            var records=new List<SessionRecord>();
            foreach (string path in Directory.GetFiles(Sessions,"*.json"))
            {
                try
                {
                    var r=JsonUtility.FromJson<SessionRecord>(File.ReadAllText(path));
                    if (r==null || r.schemaVersion!=1 || string.IsNullOrEmpty(r.sessionId)) continue;
                    records.Add(r);
                }
                catch (ArgumentException) { Debug.LogWarning("Ignored malformed local session file."); }
                catch (IOException) { Debug.LogWarning("Could not read a local session file."); }
            }
            return records.OrderByDescending(x=>x.startedUtc, StringComparer.Ordinal).ToList();
        }
        public void Save(SessionRecord r)
        {
            if (r==null || !Guid.TryParseExact(r.sessionId,"N",out _)) throw new ArgumentException("Invalid session ID.");
            Directory.CreateDirectory(Sessions);
            Atomic(Path.Combine(Sessions,r.sessionId+".json"),JsonUtility.ToJson(r,true));
        }
        public LocalProfile LoadProfile()
        {
            string p=Path.Combine(root,"profile.json");
            if (!File.Exists(p)) return new LocalProfile();
            try { var v=JsonUtility.FromJson<LocalProfile>(File.ReadAllText(p)); return v!=null && v.schemaVersion==1 ? v : new LocalProfile(); }
            catch (ArgumentException) { return new LocalProfile(); }
        }
        public void SaveProfile(LocalProfile p) => Atomic(Path.Combine(root,"profile.json"),JsonUtility.ToJson(p,true));
        public void DeleteAll()
        {
            if (Directory.Exists(Sessions)) Directory.Delete(Sessions,true);
            string p=Path.Combine(root,"profile.json"); if (File.Exists(p)) File.Delete(p);
        }
        static void Atomic(string target,string json)
        {
            string temp=target+".tmp";
            using (var stream=new FileStream(temp,FileMode.Create,FileAccess.Write,FileShare.None))
            {
                byte[] bytes=Encoding.UTF8.GetBytes(json); stream.Write(bytes,0,bytes.Length); stream.Flush(true);
            }
            if (File.Exists(target)) File.Replace(temp,target,null);
            else File.Move(temp,target);
        }
    }
}
