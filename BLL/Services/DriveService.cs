using BLL.Interfaces;
using Core.DTOs.Drive;
using Core.Entities;
using DAL.Interfaces;
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

		public DriveService(IFolderRepository folderRepository, IDocumentRepository documentRepository)
		{
			_folderRepository = folderRepository;
			_documentRepository = documentRepository;
		}

		/// <summary>
		/// Retrieves the contents of a specific folder (both subfolders and documents).
		/// </summary>
		public async Task<(List<Folder> Folders, List<Document> Documents)> GetDriveContentsAsync(Guid? folderId, Guid userId)
		{
			return await _folderRepository.GetFolderContentsAsync(folderId, userId);
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

			// 5. Sum document counts per subject (across all its chapters)
			var docCountBySubject = new Dictionary<Guid, int>();
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
			return await _documentRepository.GetAllWithOwnerAsync(userId);
		}
	}
}
