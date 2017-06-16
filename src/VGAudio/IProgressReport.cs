namespace VGAudio
{
    public interface IProgressReport
    {
        void Report(int value);
        void ReportAdd(int value);
        void ReportTotal(int value);
        void ReportMessage(string message);
    }
}