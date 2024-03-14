using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Globalization;
using System.Reflection;
using System.Resources;

namespace BlazorServerUrlRequestCultureProvider.Example;

public interface ILtdStringLocalizer : IStringLocalizer
{
    /// <summary>
    /// Gets the string resource with the given name.
    /// </summary>
    /// <param name="name">The name of the string resource.</param>
    /// <param name="culture">The Culture to search translation for.</param>
    /// <returns>string resource as a <see cref="LocalizedString"/>.</returns>
    LocalizedString this[string name, CultureInfo culture] { get; }
}

public interface ILtdStringLocalizer<out T> : ILtdStringLocalizer
{
}

public interface ILtdStringLocalizerFactory : IStringLocalizerFactory
{
    new ILtdStringLocalizer Create(Type resourceType);
}

public class LtdResourceManagerStringLocalizer : ResourceManagerStringLocalizer, ILtdStringLocalizer
{
    private readonly string resourceBaseName;

    public LtdResourceManagerStringLocalizer(
        ResourceManager resourceManager,
        Assembly resourceAssembly,
        string baseName,
        IResourceNamesCache resourceNamesCache,
        ILogger logger)
        : base(
            resourceManager,
            resourceAssembly,
            baseName,
            resourceNamesCache,
            logger)
    {
        resourceBaseName = baseName;
    }

    public virtual LocalizedString this[string name, CultureInfo culture]
    {
        get
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            string? value = GetStringSafely(name, culture);

            return new LocalizedString(name, value ?? name, resourceNotFound: value == null, searchedLocation: resourceBaseName);
        }
    }
}

public class LtdResourceManagerStringLocalizerFactory : ResourceManagerStringLocalizerFactory, ILtdStringLocalizerFactory
{
    private readonly ILoggerFactory loggerFactory;
    private readonly IResourceNamesCache resourceNamesCache = new ResourceNamesCache();
    private readonly ConcurrentDictionary<string, LtdResourceManagerStringLocalizer> localizerCache = new ConcurrentDictionary<string, LtdResourceManagerStringLocalizer>();

    public LtdResourceManagerStringLocalizerFactory(
        IOptions<LocalizationOptions> localizationOptions,
        ILoggerFactory loggerFactory) : base(localizationOptions, loggerFactory)
    {
        this.loggerFactory = loggerFactory;
    }

    public new ILtdStringLocalizer Create(Type resourceSource)
    {
        if (resourceSource == null)
        {
            throw new ArgumentNullException(nameof(resourceSource));
        }

        var typeInfo = resourceSource.GetTypeInfo();
        var baseName = GetResourcePrefix(typeInfo);
        var assembly = typeInfo.Assembly;

        return localizerCache.GetOrAdd(baseName, _ => CreateLtdResourceManagerStringLocalizer(assembly, baseName));
    }

    protected LtdResourceManagerStringLocalizer CreateLtdResourceManagerStringLocalizer(
        Assembly assembly,
        string baseName)
    {
        return new LtdResourceManagerStringLocalizer(
            new ResourceManager(baseName, assembly),
            assembly,
            baseName,
            resourceNamesCache,
            loggerFactory.CreateLogger<ResourceManagerStringLocalizer>());
    }
}

public class LtdStringLocalizer<TResourceSource> : StringLocalizer<TResourceSource>, ILtdStringLocalizer<TResourceSource>
{
    private readonly ILtdStringLocalizer localizer;

    /// <inheritdoc/>
    public LtdStringLocalizer(ILtdStringLocalizerFactory factory) : base(factory)
    {
        localizer = factory.Create(typeof(TResourceSource));
    }

    public LocalizedString this[string name, CultureInfo culture]
    {
        get
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            return localizer[name, culture];
        }
    }
}

public static class ServiceCollectionExtensions
{

    public static IServiceCollection AddLTDLocalization(this IServiceCollection services, Action<LocalizationOptions>? setupAction = null)
    {
        services.AddOptions();

        services.TryAddSingleton<ILtdStringLocalizerFactory, LtdResourceManagerStringLocalizerFactory>();
        services.TryAddTransient(typeof(ILtdStringLocalizer<>), typeof(LtdStringLocalizer<>));
        if (setupAction != null)
        {
            services.Configure(setupAction);
        }

        return services;
    }
}