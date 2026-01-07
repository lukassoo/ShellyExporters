namespace Utilities.Deserializers;

public interface IDeserializer
{
    public bool Deserialize(string jsonString);
}