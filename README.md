# fwd

fwd is a personal command line tool to help with dev work. This is my first F# project, and I'm using it as a springboard to learn the language.

## Setup

This requires dotnet. All instructions are written for/on macOS 13.2.1.

```
# Build the project and/or clean
dotnet clean
dotnet build

# Run the CLI
dotnet run --project src/ForwardCli -- [command] ...
dotnet run --project src/ForwardCli -- init
dotnet run --project src/ForwardCli -- switch -b new-env
dotnet run --project src/ForwardCli -- list

# Run tests
dotnet test tests/Forward.Tests

# Release
dotnet publish src/ForwardCli -c Release -r osx-arm64 --self-contained true -p:PublishSingleFile=true -p:PublishReadyToRun=true
cp src/ForwardCli/bin/Release/net8.0/osx-arm64/publish/ForwardCli [wherever]
```

Here's a rundown of the layout:

```
.
├── Forward.sln
├── README.md
├── ...
├── src
│   ├── Forward       # Primary code for app/biz logic.
│   └── ForwardCli    # Command line interface code for Forward.
└── tests
    └── Forward.Tests # Holds tests for src/Forward.
```
