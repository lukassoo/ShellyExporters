namespace Utilities.Configs;

public class Config<T>
{
    public bool logToFile = false;
    public string logLevel = "Information";
    public int listenPort = -1;
    
    public List<T> targets = new(1);
}