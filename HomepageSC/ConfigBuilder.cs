using System.Text;
using k8s;
using k8s.Models;
using Microsoft.Extensions.Options;

namespace HomepageSC;

public class ConfigBuilder(IKubernetes kubeClientObject, IOptions<SidecarOptions> sidecarOptions)
{
    private readonly SidecarOptions _options = sidecarOptions.Value;

    public async Task<Dictionary<string, Dictionary<string, Service>>> Build(V1IngressList ingressData,
        CancellationToken token)
    {
        Dictionary<string, Dictionary<string, Service>> flatConfig = new();
        foreach (var ingress in ingressData.Items)
        {
            foreach (var rule in ingress.Spec.Rules)
                if (rule.Http is { Paths.Count: 1 })
                {
                    await CreateService(ingress, rule, rule.Http.Paths[0], flatConfig, singlePath: true, token);
                }
                else
                {
                    foreach (var path in rule.Http.Paths)
                    {
                        await CreateService(ingress, rule, path, flatConfig, singlePath: false, token);
                    }
                }
        }

        return flatConfig;
    }

    private async Task CreateService(V1Ingress ingress, V1IngressRule rule, V1HTTPIngressPath path, Dictionary<string, Dictionary<string, Service>> flatConfig, bool singlePath, CancellationToken token)
    {
        var (enabled, groupName, serviceName, description, icon, target, healthcheck, widgetType, widgetUrl1, widgetSecret, widgetUsernamePasswordSecret) = GetValues(ingress, singlePath ? null : path);

        if (!enabled) return;

        var scheme =
            ingress.Spec.Tls != null && ingress.Spec.Tls.Any(tls => tls.Hosts.Contains(rule.Host))
                ? "https"
                : "http";
        var url = $"{scheme}://{rule.Host}{path.Path}";

        Widget? widget = null;
        if (!string.IsNullOrEmpty(widgetType))
        {
            string? apiKey = null;

            if (!string.IsNullOrEmpty(widgetSecret))
            {
                var secretParts = widgetSecret.Split('/');
                // TODO: improved formatting
                var secret = await kubeClientObject.CoreV1.ReadNamespacedSecretAsync(secretParts[1],
                    secretParts[0], cancellationToken: token);
                apiKey = Encoding.Default.GetString(secret.Data[secretParts[2]]);
            }

            string? username = null;
            string? password = null;
            if (!string.IsNullOrEmpty(widgetUsernamePasswordSecret))
            {
                var secretParts = widgetUsernamePasswordSecret.Split('/');
                var secret = await kubeClientObject.CoreV1.ReadNamespacedSecretAsync(secretParts[1],
                    secretParts[0], cancellationToken: token);
                username = Encoding.Default.GetString(secret.Data["username"]);
                password = Encoding.Default.GetString(secret.Data["password"]);
            }

            if (widgetUrl1 is not { } widgetUrl)
            {
                var port = path.Backend.Service.Port.Number;

                if (port == null)
                {
                    var service = await kubeClientObject.CoreV1.ReadNamespacedServiceAsync(
                        path.Backend.Service.Name,
                        ingress.Metadata.NamespaceProperty, cancellationToken: token);
                    port = service.Spec.Ports.Single(p => p.Name == path.Backend.Service.Port.Name)
                        .Port;
                }
                widgetUrl = $"http://{path.Backend.Service.Name}.{ingress.Metadata.NamespaceProperty}.svc.cluster.local:{port}";
            }


            widget = new Widget(widgetType,
                widgetUrl,
                apiKey,
                username,
                password);
        }

        var newValue = new Service(url)
        {
            Description = description,
            Icon = icon,
            Ping = healthcheck,
            Target = target ?? (_options.DefaultTarget != Target.Default  ? _options.DefaultTarget.ToString() : null),
            Widget = widget
        };

        var group = flatConfig.GetOrAdd(groupName, new Dictionary<string, Service>());
        group[serviceName] = newValue;
    }

    private AnnotationValues GetValues(V1Ingress ingress, V1HTTPIngressPath? path)
    {
        var enabled = _options.IncludeByDefault;
        var enabledStr = GetAnnotationValue(AnnotationKey.Enable, ingress, path);
        if (enabledStr is not null)
        {
            enabled = bool.TryParse(enabledStr, out enabled) && enabled;
        }
        return new AnnotationValues(
            enabled,
            GetAnnotationValue(AnnotationKey.Group, ingress, path) ?? "Default",
            GetAnnotationValue(AnnotationKey.AppName, ingress, path) ?? ExtractAppName(ingress, path),
            GetAnnotationValue(AnnotationKey.Description, ingress, path),
            GetAnnotationValue(AnnotationKey.Icon, ingress, path),
            GetAnnotationValue(AnnotationKey.Target, ingress, path),
            GetAnnotationValue(AnnotationKey.Healthcheck, ingress, path),
            GetAnnotationValue(AnnotationKey.WidgetType, ingress, path),
            GetAnnotationValue(AnnotationKey.WidgetUrl, ingress, path),
            GetAnnotationValue(AnnotationKey.WidgetSecret, ingress, path),
            GetAnnotationValue(AnnotationKey.WidgetUsernamePasswordSecret, ingress, path)
        );
    }

    private static string ExtractAppName(V1Ingress ingress, V1HTTPIngressPath? path) =>
        $"{ingress.Metadata.Name}{(path?.Path.Trim('/') is {Length: > 0} p ? $"/{p}" : string.Empty)}";

    private static string? GetAnnotationValue(Func<string?, string> annotationFunc, V1Ingress ingress, V1HTTPIngressPath? path) =>
        Get(ingress, annotationFunc(path?.Path));

    private static string? Get(V1Ingress ingress, string attributeName)
    {
        return ingress.Metadata.Annotations.TryGetValue(attributeName, out var annotation)
            ? annotation
            : null;
    }

    private record AnnotationValues(
        bool Enabled,
        string Group,
        string AppName,
        string? Description,
        string? Icon,
        string? Target,
        string? Healthcheck,
        string? WidgetType,
        string? WidgetUrl,
        string? WidgetSecret,
        string? WidgetUsernamePasswordSecret);
}