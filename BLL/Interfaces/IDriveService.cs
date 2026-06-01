using Core.DTOs.Drive;
using Core.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BLL.Interfaces
{
    /// <summary>
    /// Service for handling business logic related to the drive (folders and documents).
    /// </summary>
    public interface IDriveService
    {
        /// <summary>
        /// Retrieves the contents of a specific folder (both subfolders and documents).
        /// </summary>
        Task<(List<Folder> Folders, List<Document> Documents)> GetDriveContentsAsync(Guid? folderId, Guid userId);

        /// <summary>
        /// Retrieves a subject folder's sub-folders (chapters) along with document counts per chapter.
        /// </summary>
        Task<(Folder? SubjectFolder, List<Folder> Chapters, List<Document> Documents, Dictionary<Guid, int> DocumentCounts)> GetChaptersAsync(Guid subjectFolderId, Guid userId);

        /// <summary>
        /// Retrieves all root-level folders (subjects / môn học) with their chapter and document counts.
        /// </summary>
        Task<List<SubjectStatsDto>> GetSubjectsAsync(Guid userId);
        
        /// <summary>
        /// Retrieves the breadcrumb trail for navigation.
        /// </summary>
        Task<List<BreadcrumbDto>> GetBreadcrumbsAsync(Guid? folderId, Guid userId);
        
        /// <summary>
        /// Creates a new folder.
        /// </summary>
        Task<(bool Success, string ErrorMessage)> CreateFolderAsync(FolderCreateDto dto, Guid userId);
        
        /// <summary>
        /// Updates an existing folder.
        /// </summary>
        Task<(bool Success, string ErrorMessage)> UpdateFolderAsync(Guid folderId, FolderUpdateDto dto, Guid userId);
        
        /// <summary>
        /// Deletes a folder and potentially its contents.
        /// </summary>
        Task<(bool Success, string ErrorMessage)> DeleteFolderAsync(Guid folderId, Guid userId);
        
        /// <summary>
        /// Moves a resource (folder or document) to a different destination folder.
        /// </summary>
        Task<(bool Success, string ErrorMessage)> MoveResourceAsync(MoveResourceDto dto, Guid userId);
        
        /// <summary>
        /// Deletes a document.
        /// </summary>
        Task<(bool Success, string ErrorMessage)> DeleteDocumentAsync(Guid docId, Guid userId);
        
        // Task<(bool Success, string ErrorMessage)> UploadDocumentAsync(System.IO.Stream fileStream, string fileName, string contentType, long fileLength, Guid folderId, Guid userId);

        /// <summary>
        /// Retrieves all documents belonging to the user.
        /// </summary>
        Task<List<Document>> GetAllDocumentsAsync(Guid userId);

        /// <summary>
        /// Downloads all contents of a folder (including its documents and subfolders/chapters) as a ZIP archive.
        /// </summary>
        Task<(byte[] ZipBytes, string FolderName)> DownloadFolderAsZipAsync(Guid folderId, Guid userId);

        /// <summary>
        /// Downloads a single document file stream, returning the stream, original file name, and MIME type.
        /// </summary>
        Task<(System.IO.Stream FileStream, string FileName, string MimeType)> DownloadDocumentAsync(Guid documentId, Guid userId);
    }
}
