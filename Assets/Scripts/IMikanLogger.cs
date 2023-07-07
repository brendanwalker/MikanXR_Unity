using Mikan;

namespace MikanXR
{
    public interface IMikanLogger 
    {
        void Log(MikanLogLevel mikanLogLevel, string log_message);
    }
}