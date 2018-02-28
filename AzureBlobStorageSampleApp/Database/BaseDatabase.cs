﻿using System;
using System.Threading.Tasks;

using SQLite;

using Xamarin.Forms;

using AzureBlobStorageSampleApp.Shared;

namespace AzureBlobStorageSampleApp
{
    public abstract class BaseDatabase
    {
        #region Constant Fields
        static readonly Lazy<SQLiteAsyncConnection> _databaseConnectionHolder = new Lazy<SQLiteAsyncConnection>(DependencyService.Get<ISQLite>().GetConnection);
        #endregion

        #region Fields
        static bool _isInitialized;
        #endregion

        #region Properties
        static SQLiteAsyncConnection DatabaseConnection => _databaseConnectionHolder.Value;
        #endregion

        #region Methods
        protected static async ValueTask<SQLiteAsyncConnection> GetDatabaseConnectionAsync()
        {
            if (!_isInitialized)
                await Initialize().ConfigureAwait(false);

            return DatabaseConnection;
        }

        static async Task Initialize()
        {
            await DatabaseConnection.CreateTableAsync<PhotoModel>().ConfigureAwait(false);
            _isInitialized = true;
        }
        #endregion

    }
}
