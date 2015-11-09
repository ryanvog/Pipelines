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
using System.Globalization;
using System.Reflection;
using System.Threading;
using log4net;
using IContainer = StructureMap.IContainer;

namespace ClusterOne.Pipelines
{
    public abstract class BaseStage : IStage
    {
        public virtual Guid Id
        {
            get { return m_id; }
        }

        public virtual Action<BlockingCollection<IPropertySet>, BlockingCollection<IPropertySet>, CancellationTokenSource> Action
        {
            get { return InvokeAction; }
        }

        protected IContainer Container { get { return m_container; } }

        public virtual ILog Log
        {
            get { return m_log; }
        }

        protected BaseStage(ILog log, IContainer container)
        {
            if (log == null)
            {
                throw new ArgumentNullException(nameof(log));
            }

            if (container == null)
            {
                throw new ArgumentNullException(nameof(container));
            }

            m_id = Guid.NewGuid();
            m_log = log;
            m_container = container;
            m_keyProvider = RequiredKeyProvider.Create(this);
        }

        protected abstract IEnumerable<IPropertySet> Execute(CancellationTokenSource cancelTokenSource);

        private void InvokeAction(BlockingCollection<IPropertySet> blockingInput,
                                    BlockingCollection<IPropertySet> blockingOutput,
                                    CancellationTokenSource cancelTokenSource)
        {
            Log.DebugFormat(CultureInfo.InvariantCulture, "{0}::Execute - BEGIN", GetType().Name);

            try
            {
                IEnumerable<IPropertySet> input = ValidateStage(blockingInput, cancelTokenSource);

                if (cancelTokenSource.IsCancellationRequested)
                {
                    Log.DebugFormat(CultureInfo.InvariantCulture,
                        "{0}::Execute - CANCELLATION REQUESTED",
                        this.GetType().Name);
                }
                else
                {
                    IEnumerable<IPropertySet> output = Execute(cancelTokenSource);
                    try
                    {
                        if (input != null)
                        {
                            foreach (IPropertySet property in input)
                            {
                                blockingOutput.Add(property);
                            }
                        }

                        if (output != null)
                        {
                            foreach (IPropertySet property in output)
                            {
                                blockingOutput.Add(property);
                            }
                        }
                    }
                    finally
                    {
                        blockingOutput.CompleteAdding();
                    }
                }

                Log.DebugFormat(CultureInfo.InvariantCulture, "{0}::Execute - END", this.GetType().Name);
            }
            catch (Exception e)
            {
                //TODO : Log the exception
                Log.DebugFormat(CultureInfo.InvariantCulture, "EXCEPTION OCCURED IN STAGE ::{0}", this.GetType().Name);
                Log.DebugFormat(CultureInfo.InvariantCulture, "Exception Type ::{0}", e.GetType().Name);
                Log.DebugFormat(CultureInfo.InvariantCulture, "Exception Message ::{0}", e.Message);
                Log.DebugFormat(CultureInfo.InvariantCulture, "{0}::Execute CANCELLATION - END", this.GetType().Name);

                //TODO : Do we want to log the exception in History ?

                cancelTokenSource.Cancel();
            }
        }

        private IEnumerable<IPropertySet> ValidateStage(BlockingCollection<IPropertySet> input, CancellationTokenSource cancelTokenSource)
        {
            string thisTypeName = this.GetType().Name;
            ICollection<IPropertySet> inputCache = new List<IPropertySet>();

            Log.DebugFormat(CultureInfo.InvariantCulture, "{0}::VALIDATE - BEGIN", thisTypeName);
            
            IDictionary<string, PropertyInfo> requiredKeys = m_keyProvider.GetKeys();
            ISet<string> keysFound = new HashSet<string>();

            foreach (IPropertySet property in input.GetConsumingEnumerable())
            {
                Log.DebugFormat(CultureInfo.InvariantCulture, "Reading property {0} from input stream", property.Name);

                inputCache.Add(property);
                PropertyInfo propertyInfo;

                if (requiredKeys.TryGetValue(property.Name, out propertyInfo))
                {
                    Log.DebugFormat(CultureInfo.InvariantCulture, "Property {0} found in required list.", property.Name);
                    keysFound.Add(property.Name);

                    try
                    {
                        propertyInfo.SetValue(this, property.Value);
                    }
                    catch (Exception ex)
                    {
                        Log.DebugFormat("Exception thrown while setting value '{0}' for property '{1}' on type '{2}': {3}",
                                        property.Value,
                                        property.Name,
                                        thisTypeName,
                                        ex.Message);

                        cancelTokenSource.Cancel();
                        throw new InvalidOperationException(String.Format(CultureInfo.CurrentCulture,
                                                                          Resources.CannotSetStagePropertyValue,
                                                                          property.Name,
                                                                          thisTypeName));
                    }
                }
            }

            if (keysFound.Count != requiredKeys.Count)
            {
                Log.Debug("Not all required fields satisfied.");

                foreach (KeyValuePair<string, PropertyInfo> kvp in requiredKeys)
                {
                    if (!keysFound.Contains(kvp.Key))
                    {
                        cancelTokenSource.Cancel();
                        throw new PropertyNotFoundException(kvp.Key);
                    }
                }
            }

            Log.DebugFormat(CultureInfo.InvariantCulture, "{0}::VALIDATE - END", thisTypeName);

            return inputCache;
        }

        private readonly Guid m_id;
        private readonly ILog m_log;
        private readonly IRequiredKeyProvider m_keyProvider;
        private readonly IContainer m_container;
    }
}
