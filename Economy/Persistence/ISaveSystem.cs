namespace Dystopia.Economy.Persistence
{
    public interface ISaveSystem
    {
        void Save(string key, string json);
        string Load(string key);
        bool Exists(string key);
        void Delete(string key);
    }
}