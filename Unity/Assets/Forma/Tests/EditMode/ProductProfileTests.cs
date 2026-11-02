using System;
using System.IO;
using Forma.Product;
using Forma.Storage;
using Forma.Runtime;
using NUnit.Framework;
using UnityEngine;
namespace Forma.Tests
{
    public sealed class ProductProfileTests
    {
        string directory;
        ProductProfileRepository repository;
        [SetUp] public void Setup(){directory=Path.Combine(Path.GetTempPath(),"forma-product-test-"+Guid.NewGuid().ToString("N"));repository=new ProductProfileRepository(directory,new UnityProfileCodec());}
        [TearDown] public void Cleanup(){if(Directory.Exists(directory))Directory.Delete(directory,true);}
        [Test] public void ActualUnityCodecRoundTripsAllSettings()
        {
            var p=new ProductProfile{onboardingComplete=true,language="tr",coachId="mira",personality=PersonalityStyle.TechnicalCoach,
                posture=Posture.Seated,movementLimits=MovementLimits.AvoidLowerBody,preferredMinutes=5,reducedMotion=true};
            repository.Save(p);var copy=repository.Load().Profile;
            Assert.True(copy.onboardingComplete);Assert.AreEqual(p.language,copy.language);Assert.AreEqual(p.coachId,copy.coachId);
            Assert.AreEqual(p.personality,copy.personality);Assert.AreEqual(p.posture,copy.posture);Assert.AreEqual(p.movementLimits,copy.movementLimits);
            Assert.AreEqual(p.preferredMinutes,copy.preferredMinutes);Assert.True(copy.reducedMotion);
        }
        [Test] public void UnknownSchemaIsPreservedUntilExplicitReset()
        {
            Directory.CreateDirectory(directory);string file=Path.Combine(directory,"product-profile.json");File.WriteAllText(file,"{\"schemaVersion\":77}");
            Assert.AreEqual(ProfileLoadStatus.Unsupported,repository.Load().Status);
            Assert.Throws<InvalidOperationException>(()=>repository.Save(new ProductProfile()));
            Assert.AreEqual("{\"schemaVersion\":77}",File.ReadAllText(file));repository.Delete();repository.Save(new ProductProfile());
            Assert.AreEqual(ProfileLoadStatus.Loaded,repository.Load().Status);
        }
        [Test] public void MissingSchemaIsNotAssumedCurrent()
        {Directory.CreateDirectory(directory);File.WriteAllText(Path.Combine(directory,"product-profile.json"),"{}");Assert.AreEqual(ProfileLoadStatus.Unsupported,repository.Load().Status);}
        [Test] public void BundledCoachDefinitionsDeserializeInUnity()
        {
            var data=Resources.Load<TextAsset>("Forma/Coaches");Assert.NotNull(data);
            var catalog=new CoachCatalog(JsonUtility.FromJson<CoachCatalogDocument>(data.text));Assert.AreEqual(3,catalog.All.Count);
        }
    }
}
