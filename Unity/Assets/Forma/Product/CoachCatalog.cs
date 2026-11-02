using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
namespace Forma.Product
{
    public enum PersonalityStyle { Motivator, CalmGuide, TechnicalCoach }
    [Serializable] public sealed class CoachDefinition
    {
        public string id,displayName,avatarAsset,voiceReference,animationConfiguration;
        public PersonalityStyle personality;
        public string[] languages,supportedModes;
        public string primaryColor,accentColor;
        public CoachDefinition Copy()
        {
            var c=(CoachDefinition)MemberwiseClone();
            c.languages=(string[])languages.Clone();c.supportedModes=(string[])supportedModes.Clone();return c;
        }
    }
    [Serializable] public sealed class CoachCatalogDocument
    { public int schemaVersion=1; public CoachDefinition[] coaches; }
    public sealed class CoachCatalog
    {
        readonly Dictionary<string,CoachDefinition> values=new Dictionary<string,CoachDefinition>();
        public static bool KnownId(string id) => id=="sol" || id=="mira" || id=="atlas";
        public CoachCatalog(CoachCatalogDocument document)
        {
            if(document==null || document.schemaVersion!=1 || document.coaches==null || document.coaches.Length!=3)
                throw new ArgumentException("Expected three coach presets.");
            foreach(var c in document.coaches)
            {
                if(c==null || !KnownId(c.id) || values.ContainsKey(c.id) || string.IsNullOrWhiteSpace(c.displayName) ||
                    !Enum.IsDefined(typeof(PersonalityStyle),c.personality) || c.avatarAsset!="procedural:forma-guide-v1" ||
                    c.voiceReference!="text-only" || c.animationConfiguration!="gentle-v1" ||
                    c.languages==null || c.languages.Length!=2 || !Array.Exists(c.languages,x=>x=="en") || !Array.Exists(c.languages,x=>x=="tr") ||
                    c.supportedModes==null || c.supportedModes.Length!=1 || c.supportedModes[0]!="Studio" ||
                    !Hex(c.primaryColor) || !Hex(c.accentColor)) throw new ArgumentException("Invalid coach preset.");
                values.Add(c.id,c.Copy());
            }
        }
        static bool Hex(string s)
        { if(s==null || s.Length!=7 || s[0]!='#') return false;for(int i=1;i<s.Length;i++)if(!Uri.IsHexDigit(s[i]))return false;return true; }
        public CoachDefinition Get(string id)
        { if(id==null || !values.TryGetValue(id,out var c)) throw new ArgumentException("Unknown coach.");return c.Copy(); }
        public ReadOnlyCollection<CoachDefinition> All
        { get { var list=new List<CoachDefinition>();foreach(string id in new[]{"sol","mira","atlas"})list.Add(Get(id));return list.AsReadOnly(); } }
    }
}
