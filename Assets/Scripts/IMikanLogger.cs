using MikanXR;

namespace MikanXRPlugin
{
    public interface IMikanLogger
    {
        void Log(MikanLogLevel mikanLogLevel, string log_message);
    }
}