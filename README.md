# SpectreTcpChatClient

A TCP-based chat client and server application built with Spectre.Console for enhanced terminal UI.

## Features

- TCP socket communication
- Interactive console interface
- Real-time messaging
- Cross-platform compatibility

## Usage

### Running the Server

1. Navigate to the project directory

2. Start the server:
    ```bash
    dotnet run TcpChatServer.cs
    ```
    The server will listen for incoming connections on the default port.

### Running the Chat Client

1. Ensure the server is running first
2. In a new terminal, start the client:
    ```bash
    dotnet run TcpChatClient.cs
    ```
3. Enter your username when prompted
4. Start chatting with other connected users

### Multiple Clients

You can run multiple client instances by opening additional terminals and repeating the client startup process. Each client will connect to the same server instance.

## Requirements

- .NET 10 or later