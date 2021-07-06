using LiteDB;

namespace EventViewer.Interfaces
{
    public interface ILiteDbContext
    {
        LiteDatabase Database { get; }
    }
}