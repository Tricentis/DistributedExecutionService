// See https://aka.ms/new-console-template for more information

using CliWrap;
using Cocona;
using Docker.DotNet;
using Docker.DotNet.Models;
using Microsoft.Extensions.DependencyInjection;

static void CopyFilesRecursively(string sourcePath, string targetPath)
{
    if (!Directory.Exists(targetPath))
        Directory.CreateDirectory(targetPath);
    
    foreach (string newPath in Directory.GetFiles(sourcePath, "*.*",SearchOption.AllDirectories))
    {
        File.Copy(newPath, newPath.Replace(sourcePath, targetPath), true);
    }
}

var builder = CoconaApp.CreateBuilder();
builder.Services.AddSingleton(_ => new DockerClientConfiguration().CreateClient());

using var app = builder.Build();

app.AddCommand("images", async (DockerClient client) =>
{
    var images = await client.Images.ListImagesAsync(new ImagesListParameters());
    foreach (var image in images)
    {
        Console.WriteLine(image.RepoTags.FirstOrDefault());
    }
});

app.AddCommand("create", async (DockerClient client, 
    [Option] string licenseServer, 
    [Option] string licenseServerPort, 
    [Option] string serverUrl , 
    [Option] string clientId, 
    [Option] string clientSecret   , 
    [Option] string version) =>
{
    CopyFilesRecursively("Template", "Target");
    ModifyToscaAgentConfig(serverUrl,clientId,clientSecret);
    SetLicenseServer(licenseServer, licenseServerPort);
    var result=await Build(version);
    Console.WriteLine(result.ToString());
});

app.AddCommand("start", async (DockerClient client, [Option] int numberOfAgents, [Option] string version) =>
{
    var ids = new List<string>();
    for (int i = 0; i < numberOfAgents; i++)
    {
        var container=await client.Containers.CreateContainerAsync(new CreateContainerParameters() { Image = $"dex_agent:{version}" });
        await client.Containers.StartContainerAsync(container.ID, new ContainerStartParameters());
        ids.Add(container.ID);
    }

    Console.ReadLine();
    foreach (var id in ids)
    {
        await client.Containers.StopContainerAsync(id,new ContainerStopParameters());
    }
    
});

await app.RunAsync();

async Task<CommandResult> Build(string version)
{
    return await Cli.Wrap("docker")
        .WithArguments($"build -t dex_agent:{version} .")
        .WithWorkingDirectory(@".\Target")
        .WithStandardOutputPipe(PipeTarget.ToStream(Console.OpenStandardOutput()))
        .ExecuteAsync();
}
void ModifyToscaAgentConfig(string serverUrl , string clientId, string clientSecret)
{
    var content=File.ReadAllText(Path.Combine("Target", "ToscaDistributionAgent.exe.config"));
    content= content.Replace("{server_URL}", serverUrl);

    content= content.Replace("{Client_Id}", clientId);
    content= content.Replace("{Client_Secret}", clientSecret);

    File.WriteAllText(Path.Combine("Target", "ToscaDistributionAgent.exe.config"),content);
}
void SetLicenseServer(string licenseServer,string licenseServerPort)
{
    var content=File.ReadAllText(Path.Combine("Target", "DockerFile"));
    content= content.Replace("connect-on-premise -a licsrv -o 7070", $"connect-on-premise -a {licenseServer} -o {licenseServerPort}");
    File.WriteAllText(Path.Combine("Target", "DockerFile"),content);
}

