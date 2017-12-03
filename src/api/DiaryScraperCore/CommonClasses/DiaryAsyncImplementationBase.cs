using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace DiaryScraperCore
{
    public abstract class DiaryAsyncImplementationBase
    {
        public event EventHandler<DiaryAsyncImplementationFinishedArguments> WorkFinished;
        public Task Worker;
        public CancellationTokenSource TokenSource;

        public DiaryAsyncImplementationBase()
        {
            TokenSource = new CancellationTokenSource();
        }

        public abstract void DoWork(CancellationToken cancellationToken);
        public abstract void SetError(string error);
        protected abstract ILogger Logger { get; }

        public Task Run()
        {
            Worker = new Task(() => DoWorkWrapped(TokenSource.Token));
            Worker.Start();
            return Worker;
        }

        public void DoWorkWrapped(CancellationToken cancellationToken)
        {
            try
            {
                DoWork(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                SetError("Операция прервана пользователем");
            }
            catch (AggregateException e)
            {
                if (e.InnerException is TaskCanceledException)
                {
                    SetError("Операция прервана пользователем");
                }
                else
                {
                    SetError(e.InnerException.Message);
                    Logger.LogError(e.InnerException, "Error");
                    throw;
                }
            }
            catch (Exception e)
            {
                SetError(e.Message);
                Logger.LogError(e, "Error");
                throw;
            }
            finally
            {
                WorkFinished?.Invoke(this, new DiaryAsyncImplementationFinishedArguments());
            }
        }
    }

    public class DiaryAsyncImplementationFinishedArguments
    { }

}
