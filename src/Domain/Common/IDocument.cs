
namespace Domain.Common;

public interface IDocument : IMustHaveTenant
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    string Id { get; set; }
}