using BLL.Interfaces;
using Core.DTOs.Drive;
using Core.Entities;
using DAL.Interfaces;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace BLL.Services
{
	/// <summary>
	/// Service for handling business logic related to the drive (folders and documents).
	/// </summary>
	public class DriveService : IDriveService
	{
		private readonly IFolderRepository _folderRepository;
		private readonly IDocumentRepository _documentRepository;
		private readonly ISupabaseStorageProvider _storageProvider;
		private readonly string _supabaseUrl;
		private readonly string _bucket;

		public DriveService(
			IFolderRepository folderRepository, 
			IDocumentRepository documentRepository,
			ISupabaseStorageProvider storageProvider,
			IConfiguration configuration)
		{
			_folderRepository = folderRepository;
			_documentRepository = documentRepository;
			_storageProvider = storageProvider;

			var supabaseUrl = configuration["Supabase:Url"] ?? "";
			if (Uri.TryCreate(supabaseUrl, UriKind.Absolute, out var uri))
			{
				_supabaseUrl = $"{uri.Scheme}://{uri.Host}";
				if (uri.Port != 80 && uri.Port != 443)
				{
					_supabaseUrl += $":{uri.Port}";
				}
			}
			else
			{
				_supabaseUrl = supabaseUrl.TrimEnd('/');
			}
			_bucket = configuration["Supabase:Bucket"] ?? "Document";
		}

		/// <summary>
		/// Retrieves the contents of a specific folder (both subfolders and documents).
		/// </summary>
		public async Task<(List<Folder> Folders, List<Document> Documents)> GetDriveContentsAsync(Guid? folderId, Guid userId)
		{
			var (folders, documents) = await _folderRepository.GetFolderContentsAsync(folderId, userId);
			NormalizeStorageUrls(documents);
			return (folders, documents);
		}

		/// <summary>
		/// Retrieves a subject folder's sub-folders (chapters) with document counts per chapter.
		/// </summary>
		public async Task<(Folder? SubjectFolder, List<Folder> Chapters, List<Document> Documents, Dictionary<Guid, int> DocumentCounts)> GetChaptersAsync(Guid subjectFolderId, Guid userId)
		{
			var subjectFolder = await _folderRepository.GetByIdWithOwnerAsync(subjectFolderId, userId);
			if (subjectFolder == null)
				return (null, new List<Folder>(), new List<Document>(), new Dictionary<Guid, int>());

			var (chapters, documents) = await _folderRepository.GetFolderContentsAsync(subjectFolderId, userId);
			NormalizeStorageUrls(documents);
			var folderIds = chapters.Select(c => c.Id);
			var documentCounts = await _folderRepository.GetDocumentCountsForFoldersAsync(folderIds, userId);

			return (subjectFolder, chapters, documents, documentCounts);
		}

		/// <summary>
		/// Retrieves all root-level folders (Môn học) with chapter count and total document count.
		/// Uses 3 efficient queries: root folders → child folders → document counts.
		/// </summary>
		public async Task<List<SubjectStatsDto>> GetSubjectsAsync(Guid userId)
		{
			// 1. Get all root folders (ParentFolderId == null)
			var (rootFolders, _) = await _folderRepository.GetFolderContentsAsync(null, userId);
			if (!rootFolders.Any())
				return new List<SubjectStatsDto>();

			var rootIds = rootFolders.Select(f => f.Id).ToList();

			// 2. Get all chapter folders (direct children of root folders) in one query
			var chapterFolders = await _folderRepository.GetChildFoldersAsync(rootIds, userId);

			// 3. Chapter count per subject
			var chapterCountBySubject = chapterFolders
				.GroupBy(f => f.ParentFolderId!.Value)
				.ToDictionary(g => g.Key, g => g.Count());

			// 4. Document count per chapter folder
			var chapterIds = chapterFolders.Select(f => f.Id).ToList();
			var docCountByChapter = chapterIds.Any()
				? await _folderRepository.GetDocumentCountsForFoldersAsync(chapterIds, userId)
				: new Dictionary<Guid, int>();

			// 5. Sum document counts per subject (including files directly inside the subject and inside its chapters)
			var docCountBySubject = new Dictionary<Guid, int>();
			
			// 5.1. Fetch counts for documents directly inside the subject folders
			var rootDocCounts = rootIds.Any()
				? await _folderRepository.GetDocumentCountsForFoldersAsync(rootIds, userId)
				: new Dictionary<Guid, int>();

			foreach (var rootId in rootIds)
			{
				docCountBySubject[rootId] = rootDocCounts.TryGetValue(rootId, out var directCnt) ? directCnt : 0;
			}

			// 5.2. Add counts from documents inside each chapter folder
			foreach (var chapter in chapterFolders)
			{
				var subjectId = chapter.ParentFolderId!.Value;
				var cnt = docCountByChapter.TryGetValue(chapter.Id, out var c) ? c : 0;
				docCountBySubject[subjectId] = docCountBySubject.GetValueOrDefault(subjectId) + cnt;
			}

			// 6. Build result list
			return rootFolders.Select(f => new SubjectStatsDto
			{
				Folder = f,
				ChapterCount = chapterCountBySubject.GetValueOrDefault(f.Id),
				DocumentCount = docCountBySubject.GetValueOrDefault(f.Id)
			}).ToList();
		}

		/// <summary>
		/// Retrieves the breadcrumb trail for navigation.
		/// </summary>
		public async Task<List<BreadcrumbDto>> GetBreadcrumbsAsync(Guid? folderId, Guid userId)
		{
			if (!folderId.HasValue) return new List<BreadcrumbDto>();

			var folders = await _folderRepository.GetBreadcrumbsAsync(folderId.Value, userId);
			return folders.Select(f => new BreadcrumbDto { FolderId = f.Id, Name = f.Name }).ToList();
		}

		/// <summary>
		/// Creates a new folder.
		/// </summary>
		public async Task<(bool Success, string ErrorMessage)> CreateFolderAsync(FolderCreateDto dto, Guid userId)
		{
			if (dto.ParentFolderId.HasValue)
			{
				var parent = await _folderRepository.GetByIdWithOwnerAsync(dto.ParentFolderId.Value, userId);
				if (parent == null) return (false, "Parent folder does not exist or you do not have permission.");
			}

			var folder = new Folder
			{
				Id = Guid.NewGuid(),
				UserId = userId,
				ParentFolderId = dto.ParentFolderId,
				Name = dto.Name,
				Icon = dto.Icon,
				Color = dto.Color,
				CreatedAt = DateTime.UtcNow,
				UpdatedAt = DateTime.UtcNow
			};

			await _folderRepository.AddAsync(folder);
			return (true, string.Empty);
		}

		/// <summary>
		/// Updates an existing folder.
		/// </summary>
		public async Task<(bool Success, string ErrorMessage)> UpdateFolderAsync(Guid folderId, FolderUpdateDto dto, Guid userId)
		{
			var folder = await _folderRepository.GetByIdWithOwnerAsync(folderId, userId);
			if (folder == null) return (false, "Folder does not exist.");

			folder.Name = dto.Name;
			folder.Icon = dto.Icon;
			folder.Color = dto.Color;

			await _folderRepository.UpdateAsync(folder);
			return (true, string.Empty);
		}

		/// <summary>
		/// Deletes a folder and potentially its contents.
		/// </summary>
		public async Task<(bool Success, string ErrorMessage)> DeleteFolderAsync(Guid folderId, Guid userId)
		{
			var folder = await _folderRepository.GetByIdWithOwnerAsync(folderId, userId);
			if (folder == null) return (false, "Folder does not exist.");

			await _folderRepository.DeleteAsync(folder);
			return (true, string.Empty);
		}

		/// <summary>
		/// Moves a resource (folder or document) to a different destination folder.
		/// </summary>
		public async Task<(bool Success, string ErrorMessage)> MoveResourceAsync(MoveResourceDto dto, Guid userId)
		{
			if (dto.DestinationFolderId.HasValue)
			{
				var destination = await _folderRepository.GetByIdWithOwnerAsync(dto.DestinationFolderId.Value, userId);
				if (destination == null) return (false, "Destination folder does not exist.");
			}

			if (dto.IsFolder)
			{
				var folder = await _folderRepository.GetByIdWithOwnerAsync(dto.ResourceId, userId);
				if (folder == null) return (false, "The folder to move does not exist.");

				if (dto.DestinationFolderId.HasValue)
				{
					if (folder.Id == dto.DestinationFolderId.Value)
						return (false, "Cannot move a folder into itself.");

					bool isSubfolder = await _folderRepository.IsSubfolderOfAsync(folder.Id, dto.DestinationFolderId.Value);
					if (isSubfolder)
						return (false, "Cannot move a parent folder into its own subfolder.");
				}

				folder.ParentFolderId = dto.DestinationFolderId;
				await _folderRepository.UpdateAsync(folder);
			}
			else
			{
				var doc = await _documentRepository.GetByIdWithOwnerAsync(dto.ResourceId, userId);
				if (doc == null) return (false, "The document to move does not exist.");

				if (!dto.DestinationFolderId.HasValue)
					return (false, "A document must belong to a folder.");

				doc.FolderId = dto.DestinationFolderId.Value;
				await _documentRepository.UpdateAsync(doc);
			}

			return (true, string.Empty);
		}

		/// <summary>
		/// Deletes a document.
		/// </summary>
		public async Task<(bool Success, string ErrorMessage)> DeleteDocumentAsync(Guid docId, Guid userId)
		{
			var doc = await _documentRepository.GetByIdWithOwnerAsync(docId, userId);
			if (doc == null) return (false, "Document does not exist.");

			await _documentRepository.DeleteAsync(doc);
			return (true, string.Empty);
		}

		/// <summary> 
		/// Retrieves all documents belonging to the user. 
		/// </summary>
		public async Task<List<Document>> GetAllDocumentsAsync(Guid userId)
		{
			var docs = await _documentRepository.GetAllWithOwnerAsync(userId);
			NormalizeStorageUrls(docs);
			return docs;
		}

		private void NormalizeStorageUrls(IEnumerable<Document> documents)
		{
			if (documents == null) return;
			foreach (var doc in documents)
			{
				if (!string.IsNullOrEmpty(doc.StorageUrl) && !doc.StorageUrl.StartsWith("http://") && !doc.StorageUrl.StartsWith("https://"))
				{
					doc.StorageUrl = $"{_supabaseUrl}/storage/v1/object/public/{_bucket}/{doc.StorageUrl.TrimStart('/')}";
				}
			}
		}

		/// <summary>
		/// Downloads a single document file stream, returning the stream, original file name, and MIME type.
		/// </summary>
		public async Task<(Stream FileStream, string FileName, string MimeType)> DownloadDocumentAsync(Guid documentId, Guid userId)
		{
			var doc = await _documentRepository.GetByIdWithOwnerAsync(documentId, userId);
			if (doc == null)
				throw new KeyNotFoundException("Tài liệu không tồn tại hoặc bạn không có quyền truy cập.");

			var stream = await _storageProvider.DownloadAsync(doc.StoragePath);
			return (stream, doc.OriginalFileName, doc.MimeType);
		}

		/// <summary>
		/// Downloads all contents of a folder (including its documents and subfolders/chapters) as a ZIP archive.
		/// </summary>
		public async Task<(byte[] ZipBytes, string FolderName)> DownloadFolderAsZipAsync(Guid folderId, Guid userId)
		{
			var folder = await _folderRepository.GetByIdWithOwnerAsync(folderId, userId);
			if (folder == null)
				throw new KeyNotFoundException("Thư mục không tồn tại hoặc bạn không có quyền truy cập.");

			using (var memoryStream = new MemoryStream())
			{
				using (var archive = new System.IO.Compression.ZipArchive(memoryStream, System.IO.Compression.ZipArchiveMode.Create, true))
				{
					// 1. Download and zip files directly inside the subject folder
					var (_, documents) = await _folderRepository.GetFolderContentsAsync(folderId, userId);
					foreach (var doc in documents)
					{
						try
						{
							using (var docStream = await _storageProvider.DownloadAsync(doc.StoragePath))
							{
								var safeFileName = Path.GetFileName(doc.OriginalFileName).Replace("/", "_").Replace("\\", "_");
								var entry = archive.CreateEntry(safeFileName);
								using (var entryStream = entry.Open())
								{
									await docStream.CopyToAsync(entryStream);
								}
							}
						}
						catch (Exception ex)
						{
							// Log or skip single file error
							Console.WriteLine($"Error zipping file {doc.OriginalFileName}: {ex.Message}");
						}
					}

					// 2. Download and zip files inside each chapter (subfolder)
					var chapterFolders = await _folderRepository.GetChildFoldersAsync(new[] { folderId }, userId);
					foreach (var chapter in chapterFolders)
					{
						var (_, chapterDocs) = await _folderRepository.GetFolderContentsAsync(chapter.Id, userId);
						foreach (var doc in chapterDocs)
						{
							try
							{
								using (var docStream = await _storageProvider.DownloadAsync(doc.StoragePath))
								{
									// Use forward slashes for ZIP structure compatibility
									var safeChapterName = chapter.Name.Replace("/", "_").Replace("\\", "_").Replace("..", "_");
									var safeFileName = Path.GetFileName(doc.OriginalFileName).Replace("/", "_").Replace("\\", "_");
									var entryPath = $"{safeChapterName}/{safeFileName}";
									var entry = archive.CreateEntry(entryPath);
									using (var entryStream = entry.Open())
									{
										await docStream.CopyToAsync(entryStream);
									}
								}
							}
							catch (Exception ex)
							{
								Console.WriteLine($"Error zipping file {doc.OriginalFileName} inside chapter {chapter.Name}: {ex.Message}");
							}
						}
					}
				}

				return (memoryStream.ToArray(), folder.Name);
			}
		}
	}
}
