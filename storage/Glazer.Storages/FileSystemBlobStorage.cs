using Glazer.Storages.Abstractions;
using Glazer.Storages.Internals.FileSystem;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Glazer.Storages
{
    public class FileSystemBlobStorage : IBlobStorage
    {
        private static readonly Task<BlobStatus> FIXED_OK = Task.FromResult(BlobStatus.Ok);
        private static readonly Task<BlobStatus> FIXED_NOT_FOUND = Task.FromResult(BlobStatus.NotFound);
        private static readonly Task<BlobStatus> FIXED_CONFLICT = Task.FromResult(BlobStatus.Conflict);
        private static readonly Task<BlobStatus> FIXED_FORBIDDEN = Task.FromResult(BlobStatus.Forbidden);
        private static readonly Task<BlobStatus> FIXED_STORAGE_ERROR = Task.FromResult(BlobStatus.StorageError);

        internal static readonly TimeSpan LOCK_EXPIRATION = TimeSpan.FromSeconds(30);
        private const int LOCK_RETRY_DELAY = 1; // 0.001 s.

        private DirectoryInfo m_BasePath;
        private bool m_ReadOnly;

        /// <summary>
        /// Initialize a new <see cref="FileSystemBlobStorage"/> instance.
        /// </summary>
        /// <param name="BasePath"></param>
        /// <param name="ReadOnly"></param>
        public FileSystemBlobStorage(DirectoryInfo BasePath, bool ReadOnly = false)
        {
            m_ReadOnly = ReadOnly;
            m_BasePath = BasePath;

            if (!BasePath.Exists && !ReadOnly)
                Directory.CreateDirectory(m_BasePath.FullName);
        }

        /// <summary>
        /// Indicates whether the storage is local or not.
        /// </summary>
        public bool IsLocalStorage { get; } = true;

        /// <inheritdoc/>
        public Task<BlobStatus> TestAsync(CancellationToken Token = default)
        {
            if (m_BasePath.Exists)
                return FIXED_OK;

            try { Directory.CreateDirectory(m_BasePath.FullName); }
            catch
            {
                return FIXED_NOT_FOUND;
            }

            return FIXED_OK;
        }

        /// <summary>
        /// Translate the key to the path.
        /// </summary>
        /// <param name="Key"></param>
        /// <returns></returns>
        private string ToFullPath(string Key)
        {
            return Path.Combine(m_BasePath.FullName, StorageHelpers
                .Normalize(Key).Replace('/', Path.DirectorySeparatorChar));
        }

        /// <inheritdoc/>
        public async Task<IBlobLockResult> LockAsync(string Key, TimeSpan? Expiration = null, CancellationToken Token = default)
        {
            var Path = ToFullPath(Key);

            while (!Token.IsCancellationRequested)
            {
                FileStream Stream;
                bool DeleteOnClose = !File.Exists(Path);

                try
                {
                    if (DeleteOnClose)
                        Stream = new FileStream(Path, FileMode.Create, FileAccess.ReadWrite, FileShare.None, 4096, FileOptions.DeleteOnClose);

                    else
                        Stream = new FileStream(Path, FileMode.Create, FileAccess.ReadWrite, FileShare.None);
                }
                catch
                {
                    if (await TestAsync(Token) != BlobStatus.Ok)
                        return new LockResult(BlobStatus.NotFound);

                    try { await Task.Delay(LOCK_RETRY_DELAY, Token); }
                    catch
                    {
                    }

                    continue;
                }

                if (Expiration.HasValue)
                    return new LockResult(Stream, Expiration.Value);

                return new LockResult(Stream, LOCK_EXPIRATION);
            }

            return new LockResult(BlobStatus.Canceled);
        }

        /// <summary>
        /// Make the ETag value from the file information.
        /// </summary>
        /// <param name="FileInfo"></param>
        /// <returns></returns>
        private static string MakeEtag(FileInfo FileInfo)
        {
            using(var Md5 = MD5.Create())
            {
                try
                {
                    var Text = $"{FileInfo.Name}; {FileInfo.Length}; {FileInfo.LastWriteTimeUtc.Ticks}";
                    return new Guid(Md5.ComputeHash(Encoding.UTF8.GetBytes(Text))).ToString();
                }
                catch { }
            }

            return Guid.Empty.ToString();
        }

        /// <summary>
        /// Make <see cref="DataResult"/> from the path and its data.
        /// </summary>
        /// <param name="Path"></param>
        /// <param name="Data"></param>
        /// <returns></returns>
        private static DataResult MakeDataResult(FileInfo FileInfo, byte[] Data)
        {
            try
            {
                return new DataResult(new BlobData
                {
                    Etag = MakeEtag(FileInfo),
                    Data = Data
                },
                FileInfo.CreationTimeUtc,
                FileInfo.LastWriteTimeUtc);
            }

            catch { }
            return new DataResult(BlobStatus.NotFound);
        }

        /// <inheritdoc/>
        public async Task<IBlobResult> GetAsync(string Key, CancellationToken Token = default)
        {
            var Path = ToFullPath(Key);
            if (!File.Exists(Path))
                return new DataResult(BlobStatus.NotFound);

            while(!Token.IsCancellationRequested)
            {
                var FileInfo = new FileInfo(Path);
                try
                {
                    return MakeDataResult(FileInfo, await File.ReadAllBytesAsync(Path, Token));
                }
                catch (OperationCanceledException) { break; }
                catch (Exception Ex)
                {
                    if (Ex is FileNotFoundException || Ex is DirectoryNotFoundException)
                        return new DataResult(BlobStatus.NotFound);
                }

                try { await Task.Delay(LOCK_RETRY_DELAY, Token); }
                catch
                {
                }
            }

            return new DataResult(BlobStatus.Canceled);
        }

        /// <inheritdoc/>
        public async Task<IBlobResult> GetAsync(string Key, string Etag, CancellationToken Token = default)
        {
            var Path = ToFullPath(Key);
            if (!File.Exists(Path))
                return new DataResult(BlobStatus.NotFound);

            while (!Token.IsCancellationRequested)
            {
                var FileInfo = new FileInfo(Path);
                if (!string.IsNullOrWhiteSpace(Etag) && MakeEtag(FileInfo) != Etag)
                    return new DataResult(BlobStatus.Conflict);

                try
                {
                    return MakeDataResult(FileInfo, await File.ReadAllBytesAsync(Path, Token));
                }
                catch (OperationCanceledException) { break; }
                catch (Exception Ex)
                {
                    if (Ex is FileNotFoundException || Ex is DirectoryNotFoundException)
                        return new DataResult(BlobStatus.NotFound);
                }

                try { await Task.Delay(LOCK_RETRY_DELAY, Token); }
                catch
                {
                }
            }

            return new DataResult(BlobStatus.Canceled);
        }

        /// <inheritdoc/>
        public async Task<IBlobResult> PostAsync(string Key, BlobData Data, CancellationToken Token = default)
        {
            var Path = ToFullPath(Key);

            if (m_ReadOnly || File.Exists(Path))
                return new DataResult(BlobStatus.Forbidden);

            var Bytes = Data.Data.ToArray();

            try { await File.WriteAllBytesAsync(Path, Bytes, Token); }
            catch (OperationCanceledException)
            {
                try { File.Delete(Path); }
                catch { }

                return new DataResult(BlobStatus.Canceled);
            }

            catch (Exception Ex)
            {
                if (Ex is FileNotFoundException || Ex is DirectoryNotFoundException)
                    return new DataResult(BlobStatus.NotFound);

                return new DataResult(BlobStatus.StorageError);
            }

            return MakeDataResult(new FileInfo(Path), Bytes);
        }

        /// <inheritdoc/>
        public async Task<IBlobResult> PutAsync(string Key, BlobData Data, CancellationToken Token = default)
        {
            var Path = ToFullPath(Key);

            if (m_ReadOnly || !File.Exists(Path))
                return new DataResult(BlobStatus.Forbidden);

            if (string.IsNullOrWhiteSpace(Data.Etag) || MakeEtag(new FileInfo(Path)) == Data.Etag)
            {
                var Bytes = Data.Data.ToArray();

                try { await File.WriteAllBytesAsync(Path, Bytes, Token); }
                catch (OperationCanceledException)
                {
                    try { File.Delete(Path); }
                    catch { }

                    return new DataResult(BlobStatus.Canceled);
                }

                catch (Exception Ex)
                {
                    if (Ex is FileNotFoundException || Ex is DirectoryNotFoundException)
                        return new DataResult(BlobStatus.NotFound);

                    return new DataResult(BlobStatus.StorageError);
                }

                return MakeDataResult(new FileInfo(Path), Bytes);
            }

            return new DataResult(BlobStatus.Conflict);
        }

        /// <inheritdoc/>
        public Task<BlobStatus> DeleteAsync(string Key, string Etag = null, CancellationToken Token = default)
        {
            var Path = ToFullPath(Key);

            if (!File.Exists(Path))
                return FIXED_NOT_FOUND;

            if (m_ReadOnly)
                return FIXED_FORBIDDEN;

            if (string.IsNullOrWhiteSpace(Etag) || MakeEtag(new FileInfo(Path)) == Etag)
            {
                try { File.Delete(Path); }
                catch (Exception Ex)
                {
                    if (Ex is FileNotFoundException || Ex is DirectoryNotFoundException)
                        return FIXED_NOT_FOUND;

                    return FIXED_STORAGE_ERROR;
                }

                if (!File.Exists(Path))
                    return FIXED_OK;
            }

            return FIXED_CONFLICT;
        }


        /// <inheritdoc/>
        public void Dispose() { }

        /// <inheritdoc/>
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    }
}
