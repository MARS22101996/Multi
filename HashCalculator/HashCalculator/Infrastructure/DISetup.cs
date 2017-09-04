using Autofac;
using HashCalculator.BLL.Interfaces;
using HashCalculator.BLL.Services;
using HashCalculator.Infrastructure.Interfaces;

namespace HashCalculator.Infrastructure
{
    public static class DiSetup
    {
        public static IContainer Container { get; set; }

        public static void Initialize()
        {
            var builder = new ContainerBuilder();

            builder.RegisterType<CalculatorService>().As<ICalculatorService>().InstancePerLifetimeScope();

            builder.RegisterType<FileDialogConfigurer>().As<IFileDialogConfigurer>().InstancePerLifetimeScope();

            Container = builder.Build();
        }
    }
}