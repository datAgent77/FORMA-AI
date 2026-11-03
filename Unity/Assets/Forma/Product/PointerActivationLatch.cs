namespace Forma.Product
{
    // Never turn a held pinch/trigger into a click after tracking/source recovery.
    public sealed class PointerActivationLatch
    {
        string source;
        bool armed,lastPressed;
        public bool Update(bool available,bool pressed,string sourceId)
        {
            if(!available || string.IsNullOrEmpty(sourceId)){source=null;armed=false;lastPressed=false;return false;}
            if(source!=sourceId){source=sourceId;armed=false;lastPressed=pressed;}
            if(!pressed){armed=true;lastPressed=false;return false;}
            bool click=armed && !lastPressed;lastPressed=true;return click;
        }
    }
}
