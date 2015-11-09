
#region Copyright(c) 2015 Cluster One
// Copyright(c) 2015 Cluster One

// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:

// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.

// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.  IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
#endregion

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ClusterOne.Pipelines
{
    public interface ITaskEngine
    {
        void Transform(Uri strategyUri,
                       BlockingCollection<IPropertySet> initialStateBag,
                       CancellationToken cancelToken);
    }

    public class TaskEngine : ITaskEngine
    {
        public TaskEngine(IStrategyRepository strategyRespository, IStageResolver stageResolver)
        {
            if (strategyRespository == null)
            {
                throw new ArgumentNullException("strategyRespository");
            }

            m_stategyRespository = strategyRespository;

            if (stageResolver == null)
            {
                throw new ArgumentNullException("stageResolver");
            }

            m_stageResolver = stageResolver;
        }

        public void Transform(Uri uri,
                              BlockingCollection<IPropertySet> initialStateBag,
                              CancellationToken cancelToken)
        {
            using (CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(cancelToken))
            {
                TaskFactory tf = new TaskFactory(TaskCreationOptions.LongRunning, TaskContinuationOptions.None);
                IStrategy strategy = Strategy.Create(uri, m_stategyRespository.GetStrategies(), m_stageResolver);
                IList<Task> tasks = new List<Task>();

                BlockingCollection<IPropertySet> lastOutput = initialStateBag;

                foreach (IStage stage in strategy.Stages)
                {
                    BlockingCollection<IPropertySet> input = lastOutput;
                    BlockingCollection<IPropertySet> output = new BlockingCollection<IPropertySet>();

                    Task t = tf.StartNew(() => stage.Action(input, output, cts));

                    lastOutput = output;

                    tasks.Add(t);
                }

                // TODO: add try...finally here to clean up any resources
                Task.WaitAll(tasks.ToArray(), cts.Token);
            }
        }

        private readonly IStrategyRepository m_stategyRespository;
        private readonly IStageResolver m_stageResolver;
    }
}
