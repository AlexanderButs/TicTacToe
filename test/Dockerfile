FROM mcr.microsoft.com/dotnet/core/sdk:3.1 as build
WORKDIR /src
COPY TicTacToe.sln ./
COPY test/TicTacToe.Test/TicTacToe.Test.csproj test/TicTacToe.Test/
COPY src/TicTacToe/TicTacToe.csproj src/TicTacToe/
RUN dotnet restore
COPY . .
WORKDIR /src/test/TicTacToe.Test
RUN dotnet build -c Release 
RUN dotnet test