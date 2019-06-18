using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using static SyncSaberLib.Utilities;
using SyncSaberLib.Data;

namespace SyncSaberLib.Web
{
    class DownloadBatch
    {
        public async Task WorkDownloadQueue()
        {
            BatchComplete = false;
            int maxConcurrentDownloads = Config.MaxConcurrentDownloads; // Set it here so it doesn't error
            var actionBlock = new ActionBlock<DownloadJob>(job => {
                Logger.Debug($"Running job {job.Song.key} in ActionBlock");
                Task newTask = job.RunJobAsync();
                try
                {
                    newTask.Wait();
                }
                catch (AggregateException ae)
                {
                    foreach (var ex in ae.InnerExceptions)
                    {
                        Logger.Exception($"Error while running job {job.Song.key}-{job.Song.songName} by {job.Song.authorName}\n", ex);
                    }
                }
                finally
                {
                    TaskComplete(job.Song, job);
                }
            }, new ExecutionDataflowBlockOptions {
                BoundedCapacity = 500,
                MaxDegreeOfParallelism = maxConcurrentDownloads
            });
            while (_songDownloadQueue.Count > 0)
            {
                var job = _songDownloadQueue.Pop();
                Logger.Trace($"Adding job for {job.Song.key}");
                await actionBlock.SendAsync(job);
            }

            actionBlock.Complete();
            await actionBlock.Completion;
            Logger.Trace($"Actionblock complete");
            BatchComplete = true;
        }

        public void TaskComplete(SongInfo song, DownloadJob job)
        {
            bool successful = job.Result == DownloadJob.JobResult.SUCCESS;
            //string failReason;
            switch (job.Result)
            {
                case DownloadJob.JobResult.SUCCESS:
                    Logger.Info($"Finished job {song.key}-{song.songName} by {song.authorName} successfully.");
                    break;
                case DownloadJob.JobResult.TIMEOUT:
                    Logger.Warning($"Job {song.key} failed due to download timeout.");
                    break;
                case DownloadJob.JobResult.NOTFOUND:
                    Logger.Warning($"Job failed, {song.key} could not be found on Beat Saver."); // TODO: Put song in history so we don't try to download it again.
                    break;
                case DownloadJob.JobResult.UNZIPFAILED:
                    Logger.Warning($"Job failed, {song.key} failed during unzipping.");
                    break;
                case DownloadJob.JobResult.OTHERERROR:
                    Logger.Warning($"Job {song.key} failed for...reasons.");
                    break;
                default:
                    break;
            }


            JobCompleted(job);
        }

        public async Task RunJobs()
        {
            await WorkDownloadQueue();
        }

        public void AddJob(DownloadJob job)
        {
            if (_songDownloadQueue.Where(j => j.Song.key == job.Song.key).Count() == 0)
                _songDownloadQueue.Push(job);
            else
                Logger.Warning($"{job.Song.key} is already in the queue");
        }

        public event Action<DownloadJob> JobCompleted;

        private Stack<DownloadJob> _songDownloadQueue = new Stack<DownloadJob>();
        public bool BatchComplete { get; private set; } = false;

    }
}
