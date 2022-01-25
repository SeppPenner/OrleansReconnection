An example project for the issue https://github.com/dotnet/orleans/issues/7436.

My solution to this issue can be found under the `src_resolved` folder.

In my specific implementation, I have added an extra method called `CheckClusterConnection` to the grains I want to check to not load the data, but just check the connection
and added this call to the check mechanism (`CheckClusterConnection()` then gets called instead of `GetAllData()` in the service):

```csharp
[AlwaysInterleave]
public Task CheckClusterConnection()
{
    return Task.CompletedTask;
}
```

All changes made to fix the issue can be found under https://github.com/SeppPenner/OrleansReconnection/commit/f1cde7ba230b4ed7692b8d0024e1aaef86b4e97b. (Don't mind the removed calls to `.ConfigureAwait(false)`).