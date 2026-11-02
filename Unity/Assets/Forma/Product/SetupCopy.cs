using System;
using System.Collections.Generic;
namespace Forma.Product
{
    // Single keyed source. Setup supports EN/TR; Phase 1 workout text remains English and is disclosed.
    public static class SetupCopy
    {
        static readonly Dictionary<string,string[]> Strings=new Dictionary<string,string[]>{
            {"welcome",new[]{"Your space. Your pace.","Senin alanın. Senin tempon."}},
            {"welcomeBody",new[]{"A short, gentle workout with your virtual coach.\nYour room stays visible. You choose when to move.\nUse gaze + either trigger to select; B or Y pauses.","Sanal koçunla kısa ve hafif bir egzersiz.\nOdan görünür kalır. Ne zaman başlayacağını sen seç.\nBakış + tetik ile seç; B veya Y ile duraklat."}},
            {"coach",new[]{"Meet your coach","Koçunla tanış"}},
            {"coachBody",new[]{"Choose a look and a coaching style.\nText guidance works offline; no voice or listening.\nSetup supports English / Türkçe. Workouts: English.","Görünüm ve koçluk tarzı seç.\nMetin çevrimdışı çalışır; ses ve dinleme yok.\nKurulum: Türkçe / English. Egzersizler: İngilizce."}},
            {"profile",new[]{"Make it yours","Sana göre ayarla"}},
            {"profileBody",new[]{"All fields are optional and stay on this device.\nGoals and time store preferences for future planning.\nLimits filter the starter movements; no diagnosis needed.","Tüm alanlar isteğe bağlıdır, cihazda saklanır.\nHedef ve süre ileride planlama için tercih olarak tutulur.\nKısıtlar hareketleri filtreler; tanı bilgisi gerekmez."}},
            {"safety",new[]{"Make room to move","Harekete alan aç"}},
            {"safetyBody",new[]{"Clear people, pets and obstacles. Keep Quest boundaries on.\nUse no equipment; choose small, comfortable movements.\nSeated: use a stable chair with feet supported.\nStop for discomfort, dizziness or loss of visibility.","İnsan, evcil hayvan ve engellerden uzak alan aç.\nQuest sınırları açık kalsın. Ekipman kullanma.\nOtururken sabit sandalye kullan, ayaklarını destekle.\nRahatsızlık, baş dönmesi veya görüş kaybında dur."}},
            {"review",new[]{"Ready when you are","Hazır olduğunda başlayalım"}},
            {"next",new[]{"Continue","Devam"}}, {"back",new[]{"Back","Geri"}},
            {"skip",new[]{"Skip optional fields","İsteğe bağlı alanları atla"}},
            {"finish",new[]{"Save and enter Studio","Kaydet ve Stüdyoya gir"}},
            {"posture",new[]{"Workout position","Egzersiz pozisyonu"}}, {"audio",new[]{"Sound","Ses"}},
            {"volume",new[]{"Volume","Ses düzeyi"}}, {"motion",new[]{"Reduce idle motion","Boşta hareketi azalt"}},
            {"language",new[]{"Setup language","Kurulum dili"}}, {"appearance",new[]{"Appearance","Görünüm"}},
            {"personality",new[]{"Coaching style","Koçluk tarzı"}}, {"voice",new[]{"Voice: text only (offline)","Ses: yalnızca metin (çevrimdışı)"}},
            {"goal",new[]{"Goal (planning preference)","Hedef (planlama tercihi)"}},
            {"experience",new[]{"Experience (planning preference)","Deneyim (planlama tercihi)"}},
            {"minutes",new[]{"Minutes (future session preference)","Dakika (gelecek seans tercihi)"}},
            {"equipment",new[]{"Own equipment (inventory only)","Ekipmanım var (yalnızca envanter)"}},
            {"type",new[]{"Preferred movement type","Tercih edilen hareket türü"}},
            {"limits",new[]{"Optional movement exclusions","İsteğe bağlı hareket kısıtları"}},
            {"ack",new[]{"I understand when to stop","Ne zaman duracağımı anladım"}},
            {"space",new[]{"My clear space and surroundings are visible","Alanım açık, çevremi görebiliyorum"}},
            {"place",new[]{"Place panel in front","Paneli önüme yerleştir"}},
            {"moveCoach",new[]{"Move coach","Koçu taşı"}}, {"exit",new[]{"Exit","Çıkış"}},
            {"confirmExit",new[]{"Confirm exit","Çıkışı onayla"}},
            {"on",new[]{"On","Açık"}}, {"off",new[]{"Off","Kapalı"}}, {"skipped",new[]{"Skipped","Atlandı"}},
            {"noPlan",new[]{"No compatible starter movement. Update your preferences.\nThis is not a medical suitability assessment.","Uyumlu başlangıç hareketi yok. Tercihlerini güncelle.\nBu bir tıbbi uygunluk değerlendirmesi değildir."}},
            {"reviewNote",new[]{"First workout: equipment-free, about 1–3 minutes.\nPreferences do not extend this fixed starter session.\nConfirm clear space again before every workout.","İlk egzersiz: ekipmansız, yaklaşık 1–3 dakika.\nTercihler bu sabit başlangıç seansını uzatmaz.\nHer egzersizden önce alanını yeniden onayla."}},
            {"needSafety",new[]{"Confirm both safety checks to continue.","Devam etmek için iki güvenlik kontrolünü onayla."}},
            {"saveError",new[]{"Could not save. Free storage or retry; setup stays open.","Kaydedilemedi. Yer açıp tekrar dene; kurulum açık kalır."}},
            {"loadError",new[]{"Stored setup could not be read. Your original file is kept.\nRetry, or explicitly reset setup. History is separate.","Kayıtlı kurulum okunamadı. Asıl dosya korundu.\nTekrar dene veya kurulumu sıfırla. Geçmiş ayrıdır."}},
            {"retry",new[]{"Retry loading","Yüklemeyi yeniden dene"}}, {"reset",new[]{"Reset setup","Kurulumu sıfırla"}},
            {"confirmReset",new[]{"Confirm reset setup","Kurulumu sıfırlamayı onayla"}},
            {"edit",new[]{"Edit coach and preferences","Koç ve tercihleri düzenle"}},
            {"export",new[]{"Export setup profile","Kurulum profilini dışa aktar"}},
            {"Standing",new[]{"Standing","Ayakta"}}, {"Seated",new[]{"Seated","Oturarak"}},
            {"Skip",new[]{"Skip","Atla"}}, {"BuildRoutine",new[]{"Build a routine","Rutin oluştur"}},
            {"GentleMovement",new[]{"Gentle movement","Hafif hareket"}}, {"GeneralFitness",new[]{"General fitness","Genel kondisyon"}},
            {"New",new[]{"New","Yeni"}}, {"SomeExperience",new[]{"Some experience","Biraz deneyimli"}},
            {"Regular",new[]{"Regular","Düzenli"}}, {"Mobility",new[]{"Mobility","Hareketlilik"}},
            {"GentleCardio",new[]{"Gentle cardio","Hafif kardiyo"}}, {"Mixed",new[]{"Mixed","Karma"}},
            {"Motivator",new[]{"Motivator","Motive eden"}}, {"CalmGuide",new[]{"Calm guide","Sakin rehber"}},
            {"TechnicalCoach",new[]{"Technical coach","Teknik koç"}},
            {"Undisclosed",new[]{"Not disclosed","Paylaşılmadı"}},
            {"AvoidLowerBody",new[]{"Avoid lower-body work","Alt beden hareketini çıkar"}},
            {"AvoidShoulders",new[]{"Avoid shoulder work","Omuz hareketini çıkar"}},
            {"bothLimits",new[]{"Avoid lower body and shoulders","Alt beden ve omuzları çıkar"}}
        };
        public static string Get(string key,string language)
        { if(!Strings.TryGetValue(key,out var v))throw new ArgumentException("Missing setup text key: "+key);return v[language=="tr"?1:0]; }
        public static string Value(Enum value,string language)
        { string key=value.ToString();return Get(key=="AvoidLowerBody, AvoidShoulders"?"bothLimits":key,language); }
    }
}
