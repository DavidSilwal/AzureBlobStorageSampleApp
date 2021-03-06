﻿using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

using AzureBlobStorageSampleApp.Shared;

namespace AzureBlobStorageSampleApp
{
    public static class DatabaseSyncService
    {
        public static async Task SyncRemoteAndLocalDatabases()
        {
            var (photoListFromLocalDatabase, photoListFromRemoteDatabase) = await GetAllSavedPhotos().ConfigureAwait(false);

            var (photosInLocalDatabaseButNotStoredRemotely, photosInRemoteDatabaseButNotStoredLocally, photosInBothDatabases) = GetMatchingModels(photoListFromLocalDatabase, photoListFromRemoteDatabase);

            var (photosToPatchToLocalDatabase, photosToPatchToRemoteDatabase) = GetModelsThatNeedUpdating(photoListFromLocalDatabase, photoListFromRemoteDatabase, photosInBothDatabases);

            await Savephotos(photosInRemoteDatabaseButNotStoredLocally.Concat(photosToPatchToLocalDatabase).ToList());
        }

		static async Task<(List<PhotoModel> photoListFromLocalDatabase,
	        List<PhotoModel> photoListFromRemoteDatabase)> GetAllSavedPhotos()
		{
			var photoListFromLocalDatabaseTask = PhotoDatabase.GetAllPhotos();
			var photoListFromRemoteDatabaseTask = APIService.GetAllPhotoModels();

			await Task.WhenAll(photoListFromLocalDatabaseTask, photoListFromRemoteDatabaseTask).ConfigureAwait(false);

			return (await photoListFromLocalDatabaseTask ?? new List<PhotoModel>(),
					await photoListFromRemoteDatabaseTask ?? new List<PhotoModel>());
		}

        static (List<T> photosInLocalDatabaseButNotStoredRemotely,
            List<T> photosInRemoteDatabaseButNotStoredLocally,
            List<T> photosInBothDatabases) GetMatchingModels<T>(List<T> modelListFromLocalDatabase, 
                                                                      List<T> modelListFromRemoteDatabase) where T: IBaseModel
        {
            var modelIdFromRemoteDatabaseList = modelListFromRemoteDatabase?.Select(x => x.Id).ToList() ?? new List<string>();
            var modelIdFromLocalDatabaseList = modelListFromLocalDatabase?.Select(x => x.Id).ToList() ?? new List<string>();

            var modelIdsInRemoteDatabaseButNotStoredLocally = modelIdFromRemoteDatabaseList?.Except(modelIdFromLocalDatabaseList)?.ToList() ?? new List<string>();
            var modelIdsInLocalDatabaseButNotStoredRemotely = modelIdFromLocalDatabaseList?.Except(modelIdFromRemoteDatabaseList)?.ToList() ?? new List<string>();
            var modelIdsInBothDatabases = modelIdFromRemoteDatabaseList?.Where(x => modelIdFromLocalDatabaseList?.Contains(x) ?? false).ToList() ?? new List<string>();

            var modelsInRemoteDatabaseButNotStoredLocally = modelListFromRemoteDatabase?.Where(x => modelIdsInRemoteDatabaseButNotStoredLocally?.Contains(x?.Id) ?? false).ToList() ?? new List<T>();
            var modelsInLocalDatabaseButNotStoredRemotely = modelListFromLocalDatabase?.Where(x => modelIdsInLocalDatabaseButNotStoredRemotely?.Contains(x?.Id) ?? false).ToList() ?? new List<T>();

            var modelsInBothDatabases = modelListFromLocalDatabase?.Where(x => modelIdsInBothDatabases?.Contains(x?.Id) ?? false)
                                            .Concat(modelListFromRemoteDatabase?.Where(x => modelIdsInBothDatabases?.Contains(x?.Id) ?? false)).ToList() ?? new List<T>();

            return (modelsInLocalDatabaseButNotStoredRemotely ?? new List<T>(),
                    modelsInRemoteDatabaseButNotStoredLocally ?? new List<T>(),
                    modelsInBothDatabases ?? new List<T>());

        }

		static (List<T> photosToPatchToLocalDatabase,
		    List<T> photosToPatchToRemoteDatabase) GetModelsThatNeedUpdating<T>(List<T> modelListFromLocalDatabase,
																			  List<T> modelListFromRemoteDatabase,
																			  List<T> modelsFoundInBothDatabases) where T : IBaseModel
		{
            var modelsToPatchToRemoteDatabase = new List<T>();
            var modelsToPatchToLocalDatabase = new List<T>();
			foreach (var photo in modelsFoundInBothDatabases)
			{
                var modelFromLocalDatabase = modelListFromLocalDatabase.Where(x => x.Id.Equals(photo.Id)).FirstOrDefault();
                var modelFromRemoteDatabase = modelListFromRemoteDatabase.Where(x => x.Id.Equals(photo.Id)).FirstOrDefault();

				if (modelFromLocalDatabase?.UpdatedAt.CompareTo(modelFromRemoteDatabase?.UpdatedAt ?? default(DateTimeOffset)) > 0)
					modelsToPatchToRemoteDatabase.Add(photo);
				else if (modelFromLocalDatabase?.UpdatedAt.CompareTo(modelFromRemoteDatabase?.UpdatedAt ?? default(DateTimeOffset)) < 0)
					modelsToPatchToLocalDatabase.Add(photo);
			}

			return (modelsToPatchToLocalDatabase ?? new List<T>(),
					modelsToPatchToRemoteDatabase ?? new List<T>());
		}

        static Task Savephotos(List<PhotoModel> photosToSaveToLocalDatabase)
        {
            var savephotoTaskList = new List<Task>();

            foreach (var photo in photosToSaveToLocalDatabase)
                savephotoTaskList.Add(PhotoDatabase.SavePhoto(photo));

            return Task.WhenAll(savephotoTaskList);
        }
    }
}
