namespace BLL.Interfaces
{
    /// <summary>
    /// Abstracts Supabase Storage operations so document business logic is not tied directly to HTTP calls.
    /// </summary>
    public interface ISupabaseStorageService
    {
        Task UploadAsync(string storagePath, Stream content, string contentType, CancellationToken cancellationToken = default);

        Task DeleteAsync(string storagePath, CancellationToken cancellationToken = default);
    }
}
