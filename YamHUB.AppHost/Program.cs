using Projects;

var builder = DistributedApplication.CreateBuilder(args);

var authApi = builder.AddProject<YamHUB_AuthService>("apiservice-auth");
var notificationApi = builder.AddProject<YamHUB_NotificationService>("apiservice-notification");

builder.Build().Run();
