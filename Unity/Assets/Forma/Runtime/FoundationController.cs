using Forma.Product;
using UnityEngine;
namespace Forma.Runtime
{
    [DefaultExecutionOrder(100)]
    public sealed class FoundationController:MonoBehaviour,ISafePause
    {
        public StudioPlatform platform;
        public StudioPointer pointer;
        public ManualStudioPlacement placement;
        public TextMesh status;
        public StudioButton floorButton,spaceButton,pauseButton,recenterButton,lowerFloorButton,raiseFloorButton,exitButton;
        public bool editorSimulation;
        readonly NativeFoundationSafety safety=new NativeFoundationSafety();
        bool focus=true,appPaused,exitPending;
        string notice="",renderedNotice;
        int renderedFlags=-1;
        void Start()
        {
            floorButton.Click=()=>{UpdateSignals();if(placement.HasPlacement && safety.ConfirmFloor())notice="Zemin kullanıcı tarafından onaylandı.";};
            spaceButton.Click=()=>{UpdateSignals();if(!safety.ConfirmSpace())notice="Önce zemini ve görünür çevreni kontrol et.";};
            pauseButton.Click=()=>{if(safety.IsPaused){UpdateSignals();if(!safety.Resume())notice="Girdiyi, zemini ve açık alanı yeniden onayla.";else notice="";}else Pause();};
            recenterButton.Click=Recenter;
            lowerFloorButton.Click=()=>Adjust(-.02f);raiseFloorButton.Click=()=>Adjust(.02f);
            exitButton.Click=Exit;
            notice="XR ve giriş hazır olduğunda yerleşim açılır.";
        }
        void UpdateSignals()
        {
            safety.UpdateReadiness(platform!=null && platform.PassthroughReady,
                platform!=null && platform.TrackingReady,focus && !appPaused && platform!=null && platform.HasFocus,
                pointer!=null && pointer.PointerAvailable);
        }
        void Update()
        {
            UpdateSignals();
            if(!placement.HasPlacement && safety.DevicesReady && !placement.Place())notice="Düz ileri bak ve yeniden yerleştir.";
            int flags=((int)safety.State<<4)|(safety.FloorConfirmed?1:0)|(safety.SpaceConfirmed?2:0)|(safety.IsPaused?4:0)|(exitPending?8:0)|(editorSimulation?128:0);
            if(flags==renderedFlags && notice==renderedNotice)return;
            renderedFlags=flags;renderedNotice=notice;
            string state=safety.State==FoundationState.Unavailable?"XR / giriş hazır değil":
                safety.State==FoundationState.FloorConfirmation?"Zemini gözünle kontrol et":
                safety.State==FoundationState.SpaceConfirmation?"Alanını ve görüşünü onayla":
                safety.State==FoundationState.Paused?"DURAKLATILDI":"Foundation hazır — antrenman yok";
            status.text=(editorSimulation?"EDITOR SİMÜLASYONU — passthrough yok\n":"FORMA / NATIVE FOUNDATION A\n")+state+
                "\nÇizgi engel algılamaz; Quest sınırını kullan.\nEkipmansız. Koç/egzersiz bu aşamada yok.\n"+notice;
            floorButton.Label.text=safety.FloorConfirmed?"Zemin onaylı":"Zemin burada: onayla";
            spaceButton.Label.text=safety.SpaceConfirmed?"Alan/görüş onaylı":"Alan/görüş: onayla";
            pauseButton.Label.text=safety.IsPaused?"Devam et":"DURAKLAT";
            exitButton.Label.text=exitPending?"Çıkışı onayla":"Uygulamadan çık";
            var color=safety.State==FoundationState.Ready?new Color(.96f,.94f,.85f):new Color(.93f,.73f,.28f);
            placement.exerciseBoundary.startColor=color;placement.exerciseBoundary.endColor=color;
        }
        void Adjust(float delta){safety.InvalidatePlacement();bool placed=placement.AdjustFloor(delta);exitPending=false;notice=placed?"Yeni zemini ve alanı yeniden onayla.":"Düz ileri bak; yeni yerleşim yapılmadı.";}
        void Recenter(){safety.InvalidatePlacement();exitPending=false;notice=placement.Place()?"Yerleşim değişti. Zemini ve alanı yeniden onayla.":"Düz ileri bak; yerleştirme yapılmadı.";}
        public void Pause(){safety.Pause();notice="Dur. Devam otomatik başlamaz.";}
        void Exit()
        {
            if(!exitPending){Pause();exitPending=true;notice="Duraklatıldı. Çıkışı onayla'yı seç.";return;}
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying=false;
#else
            Application.Quit();
#endif
        }
        void OnApplicationFocus(bool value){focus=value;if(!value)Pause();}
        void OnApplicationPause(bool value){appPaused=value;if(value)Pause();}
    }
}
