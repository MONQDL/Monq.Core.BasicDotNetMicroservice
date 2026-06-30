# Migration guide from v8 to v9

1. Replace `services.ConfigureSMAuthentication()` with `services.ConfigureMonqAuthentication()`.
2. Replace `RestHttpClientFromOptions<T>` with `RestHttpClient` and remove unnecessary injections in implementation class constructor.
3. Replace DI registration for all classes that are derived from `RestHttpClient` with

```csharp
services.AddRestHttpPreConfiguredClient<IService, Service>();
```
