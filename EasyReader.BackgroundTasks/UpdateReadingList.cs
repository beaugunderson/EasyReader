using System.Threading;

using Windows.ApplicationModel.Background;

namespace EasyReader.BackgroundTasks
{
    public sealed class UpdateReadingList : IBackgroundTask
    {
        private int _globalCount;

        void IBackgroundTask.Run(IBackgroundTaskInstance taskInstance)
        {
            //BackgroundTaskDeferral deferral = taskInstance.GetDeferral();

            //await SomeOperationAsync();
            //await SomeOtherOperationAsync();
            
            //deferral.Complete(); 

            _globalCount = 0;

            for (int i = 0; i < 100000; ++i)
            {
                Interlocked.Increment(ref _globalCount);

                taskInstance.Progress = (uint)_globalCount;
            }
        }
    }
}