using Backrole.Crypto;
using Glazer.Nodes.Contracts.Storages.Messages;
using Glazer.Nodes.Helpers;
using Glazer.Nodes.Models;
using Glazer.Nodes.Models.Contracts;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Glazer.Nodes.Contracts.Storages.Implementations
{
    public class FileSystemStorageFeature : StorageFeature
    {
        private string m_DataPath;

        /// <summary>
        /// Initialize a new <see cref="MemoryStorageFeature"/> instance.
        /// </summary>
        /// <param name="Account"></param>
        public FileSystemStorageFeature(Account Account, string DataPath)
        {
            if (!Directory.Exists(m_DataPath = DataPath))
                 Directory.CreateDirectory(m_DataPath);

            this.Account = Account;
            SetStatus(NodeStatus.Ready);
        }

        /// <inheritdoc/>
        public override bool IsRemote => false;

        /// <inheritdoc/>
        public override bool IsRemoteInitiated => false;

        /// <inheritdoc/>
        public override Account Account { get; }

        /// <inheritdoc/>
        public override ValueTask DisposeAsync() => ValueTask.CompletedTask;

        /// <summary>
        /// Make Name Hash.
        /// </summary>
        /// <param name="BlobName"></param>
        /// <returns></returns>
        private static HashValue MakeNameHash(string BlobName) => Hashes.Default.Hash("MD5", Encoding.UTF8.GetBytes(BlobName ?? ""));

        /// <summary>
        /// Make Full Path.
        /// </summary>
        /// <param name="ClassId"></param>
        /// <param name="NameHash"></param>
        /// <returns></returns>
        private string MakeFullPath(Guid ClassId, HashValue NameHash)
        {
            var Name = new Guid(NameHash.Value).ToString();
            return Path.Combine(m_DataPath, ClassId.ToString(), Name);
        }

        /// <summary>
        /// Make Tag by.
        /// </summary>
        /// <param name="ClassId"></param>
        /// <param name="NameHash"></param>
        /// <returns></returns>
        private Guid MakeTag(Guid ClassId, HashValue NameHash)
        {
            var FileInfo = new FileInfo(MakeFullPath(ClassId, NameHash));
            var TagBytes = BitConverter.GetBytes(FileInfo.LastWriteTimeUtc.ToUnixSeconds());
            return new Guid(Hashes.Default.Hash("MD5", TagBytes).Value);
        }

        /// <inheritdoc/>
        public override Task<NewBlobReply> NewAsync(NewBlob Request, CancellationToken Token = default)
        {
            var NameHash = MakeNameHash(Request.BlobName);
            var FullPath = MakeFullPath(Request.ClassId, NameHash);

            try
            {
                using (var Stream = new FileStream(FullPath, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None))
                {
                    Stream.Write(Request.Data);
                }

                return Task.FromResult(new NewBlobReply
                {
                    Status = HttpStatusCode.OK,
                    BlobTag = MakeTag(Request.ClassId, NameHash)
                });
            }

            catch (IOException)
            {
                return Task.FromResult(new NewBlobReply { Status = HttpStatusCode.Forbidden });
            }
            catch
            {
                return Task.FromResult(new NewBlobReply { Status = HttpStatusCode.InternalServerError });
            }
        }

        /// <inheritdoc/>
        public override async Task<GetBlobReply> GetAsync(GetBlob Request, CancellationToken Token = default)
        {
            var NameHash = MakeNameHash(Request.BlobName);
            var FullPath = MakeFullPath(Request.ClassId, NameHash);
            
            while(true)
            {
                try
                {
                    using (var Stream = new FileStream(FullPath, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        var Tag = MakeTag(Request.ClassId, NameHash);
                        if (Tag != Request.BlobTag && Request.BlobTag != Guid.Empty)
                            return new GetBlobReply { Status = HttpStatusCode.NotModified };

                        var Bytes = new Queue<(byte[] Buffer, int Index)>();
                        var Buffer = new byte[2048];
                        var TotalLength = 0;

                        while (true)
                        {
                            int Length;

                            try { Length = await Stream.ReadAsync(Buffer, Token); }
                            catch (IOException) { Length = 0; }
                            catch (OperationCanceledException)
                            {
                                return new GetBlobReply { Status = HttpStatusCode.RequestTimeout };
                            }

                            if (Length <= 0)
                                break;

                            Bytes.Enqueue((new ArraySegment<byte>(Buffer, 0, Length).ToArray(), TotalLength));
                            TotalLength += Length;
                        }

                        Array.Resize(ref Buffer, TotalLength);
                        while (Bytes.TryDequeue(out var Data))
                        {
                            System.Buffer.BlockCopy(Data.Buffer, 0, Buffer, Data.Index, Data.Buffer.Length);
                        }

                        return new GetBlobReply
                        {
                            Status = HttpStatusCode.OK,
                            BlobTag = Tag,
                            Data = Buffer
                        };
                    }
                }
                catch(FileNotFoundException) { }
                catch(DirectoryNotFoundException) { }
                catch(IOException)
                {
                    if (Token.IsCancellationRequested)
                        return new GetBlobReply { Status = HttpStatusCode.RequestTimeout };

                    await Task.Delay(5);
                    continue;
                }

                return new GetBlobReply
                {
                    Status = HttpStatusCode.NotFound
                };
            }
        }

        /// <inheritdoc/>
        public override async Task<SetBlobReply> SetAsync(SetBlob Request, CancellationToken Token = default)
        {
            var NameHash = MakeNameHash(Request.BlobName);
            var FullPath = MakeFullPath(Request.ClassId, NameHash);

            while (true)
            {
                try
                {
                    using (var Stream = new FileStream(FullPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                    {
                        var Tag = MakeTag(Request.ClassId, NameHash);
                        if (Tag != Request.BlobTag && Request.BlobTag != Guid.Empty)
                            return new SetBlobReply { Status = HttpStatusCode.Conflict };

                        Stream.SetLength(Request.Data.Length);
                        Stream.Write(Request.Data);
                        await Stream.FlushAsync();

                        return new SetBlobReply
                        {
                            Status = HttpStatusCode.OK,
                            BlobTag = MakeTag(Request.ClassId, NameHash)
                        };
                    }
                }
                catch (FileNotFoundException) { }
                catch (DirectoryNotFoundException) { }
                catch (IOException)
                {
                    if (Token.IsCancellationRequested)
                        return new SetBlobReply { Status = HttpStatusCode.RequestTimeout };

                    await Task.Delay(5);
                    continue;
                }

                return new SetBlobReply
                {
                    Status = HttpStatusCode.NotFound
                };
            }
        }

        /// <inheritdoc/>
        public override async Task<RemoveBlobReply> RemoveAsync(RemoveBlob Request, CancellationToken Token = default)
        {
            var NameHash = MakeNameHash(Request.BlobName);
            var FullPath = MakeFullPath(Request.ClassId, NameHash);

            while (true)
            {
                FileStream Stream = null;
                try
                {
                    Stream = new FileStream(FullPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);

                    var Tag = MakeTag(Request.ClassId, NameHash);
                    if (Tag != Request.BlobTag && Request.BlobTag != Guid.Empty)
                        return new RemoveBlobReply { Status = HttpStatusCode.Conflict };

                    Stream.SetLength(0);

                    try { await Stream.DisposeAsync(); } catch { }
                    try { File.Delete(FullPath); } catch { }

                    return new RemoveBlobReply
                    {
                        Status = HttpStatusCode.OK,
                        BlobTag = Tag
                    };
                }

                catch (FileNotFoundException) { }
                catch (DirectoryNotFoundException) { }
                catch (IOException)
                {
                    if (Token.IsCancellationRequested)
                        return new RemoveBlobReply { Status = HttpStatusCode.RequestTimeout };

                    await Task.Delay(5);
                    continue;
                }

                finally
                {
                    if (Stream != null)
                    {
                        try { await Stream.DisposeAsync(); }
                        catch { }
                    }
                }

                return new RemoveBlobReply { Status = HttpStatusCode.NotFound };
            }
        }
    }
}
