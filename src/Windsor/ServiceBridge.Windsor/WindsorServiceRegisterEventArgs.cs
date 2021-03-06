﻿using System;
using Castle.MicroKernel.Registration;

namespace ServiceBridge.Windsor
{
    /// <summary>
    /// Event argument for the <see cref="IServiceContainer.Registering"/> event raised in <see cref="WindsorServiceContainer"/>.
    /// </summary>
    public class WindsorServiceRegisterEventArgs : ServiceRegisterEventArgs
    {
        internal WindsorServiceRegisterEventArgs(Type serviceType, Type implementType, string serviceName, ServiceLifetime lifetime, ComponentRegistration<object> registration)
            : base(serviceType, implementType, serviceName, lifetime)
        {
            Registration = registration;
        }

        /// <summary>
        /// Gets the injection members for the Windsor service mapping.
        /// </summary>
        public ComponentRegistration<object> Registration { get; }
    }
}
