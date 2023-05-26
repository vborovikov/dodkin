# Dodkin
Thin MSMQ wrapper library

[![Downloads](https://img.shields.io/nuget/dt/Dodkin.svg)](https://www.nuget.org/packages/Dodkin)
[![NuGet](https://img.shields.io/nuget/v/Dodkin.svg)](https://www.nuget.org/packages/Dodkin)
[![MIT](https://img.shields.io/badge/license-MIT-blue.svg)](https://github.com/vborovikov/dodkin/blob/main/LICENSE)

*This is a beta version not intended for use in production. The API is subject to change.*

## MSMQ Message Queue Library
This is a C# library for working with Microsoft Message Queue (MSMQ) that provides classes for creating, reading, writing, and managing message queues and messages.

## Features
- Create and manage message queues
- Read messages from a message queue
- Write messages to a message queue
- Move messages to a message subqueue
- Navigate through a message queue using a cursor
- Manage transactions for a message queue
- Handle exceptions that may occur when working with a message queue

## Getting Started
Once you have referenced the library from NuGet, you can start using its classes and interfaces in your code. Here is an example of how to create and use a message queue:

```csharp
using Dodkin;

// Create a new message queue
var queueName = MessageQueue.TryCreate(MessageQueueName.FromPathName(".\\private$\\myqueue"));
var queueFactory = new MessageQueueFactory();

// Create a new message
var messageBody = "Hello, world!"u8;
var message = new Message(messageBody);

// Write the message to the queue
var writer = queueFactory.CreateWriter(queueName);
writer.Write(message);
```

## Documentation
The library includes XML documentation comments for all classes and interfaces, which can be viewed in Visual Studio by hovering over a class or interface name or by pressing F12.

## Contributing
Contributions to this library are welcome! If you find a bug or would like to suggest a new feature, please open an issue on the GitHub repository. If you would like to contribute code, please fork the repository and submit a pull request with your changes.

## License
This library is licensed under the MIT License. See the LICENSE file for details.