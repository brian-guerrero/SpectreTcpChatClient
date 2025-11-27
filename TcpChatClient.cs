#:package Spectre.Console@*

using System.Net.Sockets;
using System.Text;
using Spectre.Console;

AnsiConsole.Write(
    new FigletText("TCP Chat Client")
        .Centered()
        .Color(Color.Cyan1));

var username = AnsiConsole.Prompt(
    new TextPrompt<string>("Enter your [green]username[/]:")
        .PromptStyle("green")
        .ValidationErrorMessage("[red]Username cannot be empty or contain spaces[/]")
        .Validate(name =>
        {
            if (string.IsNullOrWhiteSpace(name))
                return ValidationResult.Error("[red]Username cannot be empty[/]");
            if (name.Contains(' '))
                return ValidationResult.Error("[red]Username cannot contain spaces[/]");
            return ValidationResult.Success();
        }));

var host = AnsiConsole.Prompt(
    new TextPrompt<string>("Enter [yellow]server address[/]:")
        .DefaultValue("127.0.0.1")
        .PromptStyle("yellow"));

var port = AnsiConsole.Prompt(
    new TextPrompt<int>("Enter [blue]port[/]:")
        .DefaultValue(5000)
        .PromptStyle("blue")
        .ValidationErrorMessage("[red]Invalid port number[/]")
        .Validate(p => p > 0 && p <= 65535));

TcpClient? client = null;
NetworkStream? stream = null;
var cts = new CancellationTokenSource();

try
{
    await AnsiConsole.Status()
        .StartAsync("Connecting to server...", async ctx =>
        {
            client = new TcpClient();
            await client.ConnectAsync(host, port);
            stream = client.GetStream();

            // Send username
            var usernameBytes = Encoding.UTF8.GetBytes(username + "\n");
            await stream.WriteAsync(usernameBytes);

            // Wait for confirmation
            var buffer = new byte[1024];
            var bytesRead = await stream.ReadAsync(buffer);
            var response = Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();

            if (response == "CONNECTED")
            {
                ctx.Status("Connected!");
            }
            else
            {
                throw new Exception(response);
            }
        });

    AnsiConsole.MarkupLine("[green]Connected to chat server![/]");
    AnsiConsole.MarkupLine("[dim]Type your message and press Enter. Use @username to mention someone. Type 'exit' to quit.[/]\n");

    // Start receiving messages
    var receiveTask = Task.Run(async () => await ReceiveMessagesAsync(stream, cts.Token));

    // Send messages
    while (true)
    {
        var message = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(message))
            continue;

        if (message.Equals("exit", StringComparison.OrdinalIgnoreCase))
            break;

        var messageBytes = Encoding.UTF8.GetBytes(message + "\n");
        await stream.WriteAsync(messageBytes, cts.Token);
    }

    cts.Cancel();
    await receiveTask;
}
catch (Exception ex)
{
    AnsiConsole.MarkupLine($"[red]Error: {ex.Message}[/]");
}
finally
{
    stream?.Close();
    client?.Close();
    AnsiConsole.MarkupLine("\n[yellow]Disconnected from server. Press any key to exit...[/]");
    Console.ReadKey();
}

async Task ReceiveMessagesAsync(NetworkStream stream, CancellationToken token)
{
    var buffer = new byte[1024];

    try
    {
        while (!token.IsCancellationRequested)
        {
            var bytesRead = await stream.ReadAsync(buffer, token);
            if (bytesRead == 0) break;

            var message = Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();

            if (message.StartsWith("***"))
            {
                AnsiConsole.MarkupLine($"[grey]{message}[/]");
            }
            else if (message.Contains($"@{username}"))
            {
                // Highlight messages that mention this user
                var highlighted = message.Replace($"@{username}", $"[black on yellow]@{username}[/]");
                AnsiConsole.MarkupLine($"[bold cyan]{highlighted}[/]");
            }
            else
            {
                AnsiConsole.MarkupLine($"[cyan]{message}[/]");
            }
        }
    }
    catch (OperationCanceledException)
    {
        // Expected when cancelling
    }
    catch (Exception ex)
    {
        if (!token.IsCancellationRequested)
        {
            AnsiConsole.MarkupLine($"[red]Connection error: {ex.Message}[/]");
        }
    }
}
