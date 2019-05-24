using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using static SyncSaberService.Utilities;
namespace SyncSaberService.Web
{
    class DownloadBatch
    {
        public async Task WorkDownloadQueue()
        {
            _batchComplete = false;
            int maxConcurrentDownloads = Config.MaxConcurrentDownloads; // Set it here so it doesn't error
            var actionBlock = new ActionBlock<DownloadJob>(job => {
                Logger.Trace($"Running job {job.Song.Index} in ActionBlock");
                Task newTask = job.RunJob();
                newTask.Wait();
                TaskComplete(job.Song, job);
            }, new ExecutionDataflowBlockOptions {
                BoundedCapacity = 500,
                MaxDegreeOfParallelism = maxConcurrentDownloads
            });
            while (_songDownloadQueue.Count > 0)
            {
                var job = _songDownloadQueue.Pop();
                Logger.Trace($"Adding job for {job.Song.Index}");
                await actionBlock.SendAsync(job);
            }

            actionBlock.Complete();
            await actionBlock.Completion;
            Logger.Trace($"Actionblock complete");
            _batchComplete = true;
        }

        public void TaskComplete(SongInfo song, DownloadJob job)
        {
            bool successful = job.Result == DownloadJob.JobResult.SUCCESS;
            //string failReason;
            switch (job.Result)
            {
                case DownloadJob.JobResult.SUCCESS:
                    Logger.Info($"Finished job {song.Index}-{song.Name} by {song.Author} successfully.");
                    break;
                case DownloadJob.JobResult.TIMEOUT:
                    Logger.Warning($"Job {song.Index} failed due to download timeout.");
                    break;
                case DownloadJob.JobResult.NOTFOUND:
                    Logger.Info($"Job failed, {song.Index} could not be found on Beat Saver.");
                    break;
                case DownloadJob.JobResult.UNZIPFAILED:
                    Logger.Warning($"Job failed, {song.Index} failed during unzipping.");
                    break;
                case DownloadJob.JobResult.OTHER:
                    Logger.Warning($"Job {song.Index} failed for...reasons.");
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
            if (_songDownloadQueue.Where(j => j.Song.Index == job.Song.Index).Count() == 0)
                _songDownloadQueue.Push(job);
            else
                Logger.Warning($"{job.Song.Index} is already in the queue");
        }

        public event Action<DownloadJob> JobCompleted;

        private Stack<DownloadJob> _songDownloadQueue = new Stack<DownloadJob>();
        private bool _batchComplete = false;
        public bool BatchComplete
        {
            get { return _batchComplete; }
        }

    }
}
