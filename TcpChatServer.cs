using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Collections.Concurrent;

var clients = new ConcurrentDictionary<string, TcpClient>();
var server = new TcpListener(IPAddress.Any, 5000);

server.Start();
Console.WriteLine("TCP Chat Server started on port 5000");
Console.WriteLine("Waiting for clients...\n");

try
{
    while (true)
    {
        var client = await server.AcceptTcpClientAsync();
        _ = Task.Run(() => HandleClientAsync(client));
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Server error: {ex.Message}");
}
finally
{
    server.Stop();
}

async Task HandleClientAsync(TcpClient client)
{
    var stream = client.GetStream();
    var buffer = new byte[1024];
    string? clientId = null;

    try
    {
        // Get client username
        var bytesRead = await stream.ReadAsync(buffer);
        clientId = Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();

        if (string.IsNullOrWhiteSpace(clientId) || clientId.Contains(' ') || !clients.TryAdd(clientId, client))
        {
            var errorMsg = clientId?.Contains(' ') == true
                ? "ERROR: Username cannot contain spaces\n"
                : "ERROR: Username already taken or invalid\n";
            await stream.WriteAsync(Encoding.UTF8.GetBytes(errorMsg));
            client.Close();
            return;
        }
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {clientId} joined the chat");
        await BroadcastMessageAsync($"*** {clientId} joined the chat ***", clientId);
        await stream.WriteAsync(Encoding.UTF8.GetBytes("CONNECTED\n"));

        // Handle messages
        while (true)
        {
            bytesRead = await stream.ReadAsync(buffer);
            if (bytesRead == 0) break;

            var message = Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();
            if (string.IsNullOrEmpty(message)) continue;

            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {clientId}: {message}");
            await BroadcastMessageAsync($"{clientId}: {message}", clientId);
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Client {clientId} error: {ex.Message}");
    }
    finally
    {
        if (clientId != null)
        {
            clients.TryRemove(clientId, out _);
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {clientId} left the chat");
            await BroadcastMessageAsync($"*** {clientId} left the chat ***", clientId);
        }
        client.Close();
    }
}

async Task BroadcastMessageAsync(string message, string senderId)
{
    var messageBytes = Encoding.UTF8.GetBytes(message + "\n");
    var tasks = new List<Task>();

    foreach (var (id, client) in clients)
    {
        if (id != senderId && client.Connected)
        {
            try
            {
                tasks.Add(client.GetStream().WriteAsync(messageBytes).AsTask());
            }
            catch
            {
                // Client disconnected, will be removed in HandleClientAsync
            }
        }
    }

    await Task.WhenAll(tasks);
}
