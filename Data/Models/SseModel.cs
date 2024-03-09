namespace MigrateClient.Data.Models;

public class SseModel
{
    public string Name { get; set; }
    public object Data { get; set; }
    
    public string Id { get; set; }
    public int? Retry { get; set; }
    
     

    public SseModel(string name, object data)
    {
        Name = name;
        Data = data;
    }
}