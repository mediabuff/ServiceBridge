﻿using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

namespace ServiceBridge.Interception
{
    /// <summary>
    /// A collection of <see cref="InterceptorPipeline"/> objects, indexed
    /// by <see cref="MethodBase"/>. Returns an empty pipeline if a
    /// MethodBase is requested that isn't in the dictionary.
    /// </summary>
    public class PipelineManager
    {
        private readonly Dictionary<InterceptorPipelineKey, InterceptorPipeline> _pipelines =
            new Dictionary<InterceptorPipelineKey, InterceptorPipeline>();

        /// <summary>
        /// Retrieve the pipeline associated with the requested <paramref name="method"/>.
        /// </summary>
        /// <param name="method">The method for which the pipeline is being requested.</param>
        /// <returns>The handler pipeline for the given method. If no pipeline has
        /// been set, returns a new empty pipeline.</returns>
        public InterceptorPipeline GetPipeline(MethodBase method)
        {
            InterceptorPipeline pipeline;
            return _pipelines.TryGetValue(InterceptorPipelineKey.ForMethod(method), out pipeline) ? pipeline : InterceptorPipeline.Empty;
        }

        /// <summary>
        /// Set a new pipeline for a method.
        /// </summary>
        /// <param name="method">The method on which the pipeline should be set.</param>
        /// <param name="pipeline">The new pipeline.</param>
        public void SetPipeline(MethodBase method, InterceptorPipeline pipeline)
        {
            _pipelines[InterceptorPipelineKey.ForMethod(method)] = pipeline;
        }

        /// <summary>
        /// Initialize the pipeline for the given method, creating it if necessary.
        /// </summary>
        /// <param name="interfaceMethod"><see cref="MethodInfo"/> for the interface method (may be null if no interface).</param>
        /// <param name="implementMethod"><see cref="MethodInfo"/> for implementing method.</param>
        /// <param name="container">Service container that can be used to resolve interceptors.</param>
        /// <returns>True if the pipeline has any interceptor in it, false if not.</returns>
        public bool InitializePipeline(MethodInfo interfaceMethod, MethodInfo implementMethod, IServiceContainer container)
        {
            if (implementMethod == null)
            {
                throw new ArgumentNullException(nameof(implementMethod));
            }
            var pipeline = CreatePipeline(implementMethod,
                from attribute in GetInterceptorAttributes(interfaceMethod, implementMethod)
                orderby attribute.Order
                select attribute.CreateInterceptor(container));
            if (interfaceMethod != null)
            {
                _pipelines[InterceptorPipelineKey.ForMethod(interfaceMethod)] = pipeline;
            }

            return pipeline.Count > 0;
        }

        /// <summary>
        /// Initialize the pipeline for the given type, creating it if necessary.
        /// </summary>
        /// <param name="implementType">The implementation type.</param>
        /// <param name="container">Service container that can be used to resolve interceptors.</param>
        public void InitializePipeline(Type implementType, IServiceContainer container)
        {
            var methodMappings = new Dictionary<MethodInfo, MethodInfo>();
#if NetCore
            foreach (var itf in implementType.GetTypeInfo().ImplementedInterfaces)
            {
                var mapping = implementType.GetTypeInfo().GetRuntimeInterfaceMap(itf);
                for (int i = 0; i < mapping.InterfaceMethods.Length; ++i)
                {
                    if (!methodMappings.ContainsKey(mapping.TargetMethods[i]))
                    {
                        methodMappings[mapping.TargetMethods[i]] = mapping.InterfaceMethods[i];
                        InitializePipeline(mapping.InterfaceMethods[i], mapping.TargetMethods[i], container);
                    }
                }
            }
#else
            foreach (Type itf in implementType.GetInterfaces())
            {
                var mapping = implementType.GetInterfaceMap(itf);
                for (int i = 0; i < mapping.InterfaceMethods.Length; ++i)
                {
                    if (!methodMappings.ContainsKey(mapping.TargetMethods[i]))
                    {
                        methodMappings[mapping.TargetMethods[i]] = mapping.InterfaceMethods[i];
                        InitializePipeline(mapping.InterfaceMethods[i], mapping.TargetMethods[i], container);
                    }
                }
            }
#endif
        }

        private IEnumerable<InterceptorAttribute> GetInterceptorAttributes(MethodInfo interfaceMethod, MethodInfo implementMethod)
        {
            if (interfaceMethod != null)
            {
                foreach (var attr in InterceptorAttribute.GetAttributes(interfaceMethod, true))
                {
                    yield return attr;
                }
            }
            foreach (var attr in InterceptorAttribute.GetAttributes(implementMethod, true))
            {
                yield return attr;
            }
        }

        private InterceptorPipeline CreatePipeline(MethodInfo method, IEnumerable<IInterceptor> interceptors)
        {
            InterceptorPipelineKey key = InterceptorPipelineKey.ForMethod(method);
            if (_pipelines.ContainsKey(key))
            {
                return _pipelines[key];
            }
#if NetCore
            var baseMethod = method.GetRuntimeBaseDefinition();
#else
            var baseMethod = method.GetBaseDefinition();
#endif
            if (Equals(baseMethod, method))
            {
                _pipelines[key] = new InterceptorPipeline(interceptors);
                return _pipelines[key];
            }

            var basePipeline = CreatePipeline(baseMethod, interceptors);
            _pipelines[key] = basePipeline;
            return basePipeline;
        }
    }
}
