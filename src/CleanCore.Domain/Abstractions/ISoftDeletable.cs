namespace CleanCore.Domain.Abstractions;

// Soft delete: Kayıt gerçekten silinmez, IsDeleted=true yapılır.
// Global query filter sorgularda otomatik WHERE IsDeleted=false ekler (Faz 2).
public interface ISoftDeletable
{
    bool IsDeleted { get; set; }
    DateTime? DeletedAt { get; set; }
    string? DeletedBy { get; set; }
}
