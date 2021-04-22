using Goober.Core.Attributes;
using Goober.Core.Services;
using Goober.Core.Services.Implementation;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Goober.Core.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static void AddGooberDateTimeService(this IServiceCollection services)
        {
            services.AddSingleton<IDateTimeService, DateTimeService>();
        }

        private static readonly List<string> _serviceAndRepositoryPostfix = new List<string> { "Service", "Repository" };

        public static void RegisterClasses(this IServiceCollection services,
            Assembly assembly,
            List<string> classesPostfix,
            ServiceLifetime serviceLifetime,
            bool optional)
        {
            if (classesPostfix == null || classesPostfix.Any() == false)
                throw new InvalidOperationException();
            var implementTypes = new List<Type>();

            foreach (var assemblyDefinedType in assembly.GetTypes())
            {
                if (classesPostfix.Any(x => assemblyDefinedType.Name.EndsWith(x)))
                {
                    var attribute = assemblyDefinedType.GetCustomAttributes(typeof(ServiceCollectionIgnoreRegistrationAttribute), false).FirstOrDefault();

                    if (attribute != null)
                        continue;

                    if (assemblyDefinedType.IsClass == false)
                        continue;

                    implementTypes.Add(assemblyDefinedType);
                }
            }

            foreach (var implementType in implementTypes)
            {
                var interfaceName = "I" + implementType.Name;
                var interfaceType = implementType.GetInterface(interfaceName);

                if (interfaceType == null && optional == false)
                    throw new InvalidOperationException($"Can't find interface = {interfaceName} for class.Name = {implementType.Name}, class.FullName = {implementType.FullName}.");

                services.Add(new ServiceDescriptor(interfaceType, implementType, serviceLifetime));
            }
        }

        public static void RegisterAssemblyClasses<TAssemblyMember>(this IServiceCollection services, List<string> classesPostfix = null, ServiceLifetime serviceLifetime = ServiceLifetime.Scoped, bool optional = false)
        {
            RegisterClasses(services: services,
                assembly: typeof(TAssemblyMember).Assembly,
                classesPostfix: classesPostfix ?? _serviceAndRepositoryPostfix,
                serviceLifetime: serviceLifetime,
                optional: optional);
        }
    }
}
